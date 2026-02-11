using System;
using UnityEngine;
using MapMaker.Shared.Data;
using MapMaker.Shared.Utils;
using MapMaker.Modules.Elevation1.Config;
using MapMaker.Core.Logging;

namespace MapMaker.Modules.Elevation1.Scripts
{
    /// <summary>
    /// Generates natural terrain elevation using multi-scale noise,
    /// then applies edge bias to guide land placement without forcing ocean.
    /// Clean architecture: terrain generation → edge modification → band assignment.
    /// </summary>
    public class ElevationGenerator
    {
        private readonly HB_ElevationConfig _cfg;
        private readonly SeedContext _seed;
        private readonly LogEmitter _emit;

        public ElevationGenerator(
            HB_ElevationConfig cfg,
            SeedContext seed,
            LogEmitter emit)
        {
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
            _seed = seed ?? throw new ArgumentNullException(nameof(seed));
            _emit = emit ?? throw new ArgumentNullException(nameof(emit));
        }

        public void Execute(WorldArrays world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));

            int w = world.Width;
            int h = world.Height;

            // Log configuration
            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Init, "ELEV_CONFIG",
                $"Noise: Scale={_cfg.NoiseScale:F1}, Octaves={_cfg.Octaves}, Persistence={_cfg.Persistence:F2}");
            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Init, "ELEV_CONFIG",
                $"Gradient: Bias={_cfg.EdgeBias}, Strength={_cfg.ContinentalGradientStrength:F2}, EdgeFalloff={_cfg.EdgeFalloffPercent:P0}");
            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Init, "ELEV_CONFIG",
                $"Features: MountainRidge={_cfg.MountainRidgeStrength:F2}, CoastalComplexity={_cfg.CoastalComplexity:F2}");

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "ELEV_START",
                $"Generating natural terrain (Bias={_cfg.EdgeBias})");

            // Deterministic noise offsets
            float offsetX = (float)_seed.ElevationRng.NextDouble() * 10000f;
            float offsetY = (float)_seed.ElevationRng.NextDouble() * 10000f;
            float ridgeOffsetX = (float)_seed.ElevationRng.NextDouble() * 10000f;
            float ridgeOffsetY = (float)_seed.ElevationRng.NextDouble() * 10000f;

            float scale = Mathf.Max(0.0001f, _cfg.NoiseScale);
            int octaves = Mathf.Clamp(_cfg.Octaves, 1, 8);
            float persistence = Mathf.Clamp(_cfg.Persistence, 0.05f, 0.99f);
            float lacunarity = Mathf.Clamp(_cfg.Lacunarity, 1.0f, 4.0f);

            // ===== PHASE 1: GENERATE PURE NATURAL TERRAIN =====
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;

                    // Layer 1: Geological zones (where do mountain regions vs plains occur?)
                    float geological = SampleNoise(
                        x + offsetX, y + offsetY,
                        scale * 1.5f, 2, 0.5f, 2.0f  // Reduced from 3x to 1.5x
                    );

                    // Layer 2: Mountain ridges (linear high-elevation features)
                    float ridges = 0f;
                    if (_cfg.MountainRidgeStrength > 0f)
                    {
                        float ridgeScale = Mathf.Clamp(_cfg.MountainRidgeScale, 80f, 300f);  // Reduced from 100-400
                        float rx = (x + ridgeOffsetX) / ridgeScale;
                        float ry = (y + ridgeOffsetY) / ridgeScale;
                        
                        float r1 = Mathf.Abs(Mathf.PerlinNoise(rx, ry) * 2f - 1f);
                        float r2 = Mathf.Abs(Mathf.PerlinNoise(rx * 2f, ry * 2f) * 2f - 1f) * 0.5f;
                        
                        ridges = (1f - r1) + (1f - r2) * 0.5f;
                        ridges = Mathf.Pow(ridges, 1.5f);
                    }

                    // Layer 3: Regional variation (hills, valleys)
                    float regional = SampleNoise(
                        x + offsetX + 1000f, y + offsetY + 1000f,
                        scale * 1.2f, 3, 0.5f, 2.0f  // Reduced from 2x to 1.2x
                    );

                    // Layer 4: Local detail
                    float detail = SampleNoise(
                        x + offsetX + 2000f, y + offsetY + 2000f,
                        scale, octaves, persistence, lacunarity
                    );

                    // Combine: geological zones control where ridges appear
                    float baseHeight = geological * 0.4f + regional * 0.3f + detail * 0.3f;
                    
                    if (_cfg.MountainRidgeStrength > 0f)
                    {
                        // Ridges only appear in high geological zones
                        float ridgeInfluence = Mathf.SmoothStep(0.4f, 0.8f, geological);
                        baseHeight += ridges * _cfg.MountainRidgeStrength * ridgeInfluence * 0.3f;
                    }

                    world.ElevationRaw[idx] = Mathf.Clamp01(baseHeight);
                }
            }

            // ===== PHASE 2: POST-PROCESS EDGE CORRECTION =====
            // If land touches forbidden edges, push it back inland with gradient
            if (_cfg.EdgeBias != EdgeBiasDirection.None)
            {
                CorrectEdgeOverflow(world, w, h);
            }

            // ===== PHASE 3: ADD COASTAL IRREGULARITY =====
            if (_cfg.CoastalComplexity > 0f)
            {
                AddCoastalComplexity(world, w, h, offsetX, offsetY);
            }

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "ELEV_COMPLETE",
                $"Terrain generation complete");
        }

        /// <summary>
        /// Corrects land that touches forbidden edges by pushing it back inland.
        /// Creates natural bays/inlets at correction points.
        /// </summary>
        private void CorrectEdgeOverflow(WorldArrays world, int w, int h)
        {
            int correctionDepth = Mathf.RoundToInt(Mathf.Min(w, h) * _cfg.EdgeFalloffPercent);
            if (correctionDepth == 0) return;

            float landThreshold = 0.35f; // Tiles above this are "land"
            
            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "EDGE_CORRECT",
                $"Correcting edge overflow with {correctionDepth} tile gradient");

            // Scan edges based on EdgeBias
            bool[] needsCorrection = new bool[w * h];
            int correctionCount = 0;

            // Determine which edges are "forbidden" based on EdgeBias
            bool checkWest = _cfg.EdgeBias == EdgeBiasDirection.East || _cfg.EdgeBias == EdgeBiasDirection.All;
            bool checkEast = _cfg.EdgeBias == EdgeBiasDirection.West || _cfg.EdgeBias == EdgeBiasDirection.All;
            bool checkNorth = _cfg.EdgeBias == EdgeBiasDirection.South || _cfg.EdgeBias == EdgeBiasDirection.All;
            bool checkSouth = _cfg.EdgeBias == EdgeBiasDirection.North || _cfg.EdgeBias == EdgeBiasDirection.All;

            // Scan forbidden edges for land
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isForbiddenEdge = false;
                    
                    if (checkWest && x == 0) isForbiddenEdge = true;
                    if (checkEast && x == w - 1) isForbiddenEdge = true;
                    if (checkNorth && y == 0) isForbiddenEdge = true;
                    if (checkSouth && y == h - 1) isForbiddenEdge = true;

                    if (isForbiddenEdge)
                    {
                        int idx = y * w + x;
                        if (world.ElevationRaw[idx] > landThreshold)
                        {
                            needsCorrection[idx] = true;
                            correctionCount++;
                        }
                    }
                }
            }

            if (correctionCount == 0)
            {
                _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "EDGE_CORRECT",
                    "No edge corrections needed - land contained within bounds");
                return;
            }

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "EDGE_CORRECT",
                $"Correcting {correctionCount} edge tiles with land overflow");

            // Apply gradient correction inland from each flagged edge tile
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    if (!needsCorrection[idx]) continue;

                    // Apply gradient inward from this edge point
                    ApplyInwardGradient(world, x, y, w, h, correctionDepth);
                }
            }
        }

        /// <summary>
        /// Applies an inward gradient from an edge point, smoothly lowering elevation.
        /// Uses noise to create varied bay depths based on BayDepthVariation setting.
        /// </summary>
        private void ApplyInwardGradient(WorldArrays world, int edgeX, int edgeY, int w, int h, int depth)
        {
            // Determine inward direction based on which edge
            int dx = 0, dy = 0;
            
            if (edgeX == 0) dx = 1;           // West edge, go east
            else if (edgeX == w - 1) dx = -1; // East edge, go west
            
            if (edgeY == 0) dy = 1;           // North edge, go south
            else if (edgeY == h - 1) dy = -1; // South edge, go north

            // Vary depth based on BayDepthVariation setting
            int actualDepth = depth;
            if (_cfg.BayDepthVariation > 0f)
            {
                float bayNoise = Mathf.PerlinNoise(edgeX * 0.02f, edgeY * 0.02f);
                float depthMultiplier = 1f + (bayNoise - 0.5f) * _cfg.BayDepthVariation * 2f;
                actualDepth = Mathf.RoundToInt(depth * depthMultiplier);
                actualDepth = Mathf.Clamp(actualDepth, depth / 2, depth * 2);
            }

            // Apply gradient for 'actualDepth' tiles inward
            for (int d = 0; d < actualDepth; d++)
            {
                int x = edgeX + dx * d;
                int y = edgeY + dy * d;

                if (x < 0 || x >= w || y < 0 || y >= h) break;

                int idx = y * w + x;
                
                // Gradient: strong reduction at edge, fades to no effect at depth
                float gradientStrength = 1f - ((float)d / actualDepth);
                gradientStrength = Mathf.Pow(gradientStrength, 1.5f);

                // Add lateral noise variation if enabled
                float noiseVariation = 1f;
                if (_cfg.BayDepthVariation > 0f)
                {
                    float lateralNoise = Mathf.PerlinNoise(x * 0.03f, y * 0.03f);
                    noiseVariation = 1f + (lateralNoise - 0.5f) * _cfg.BayDepthVariation;
                }

                // Reduce elevation
                float targetReduction = gradientStrength * noiseVariation * 0.7f;
                world.ElevationRaw[idx] *= (1f - targetReduction);
            }
        }

        /// <summary>
        /// Gets distance to nearest edge based on EdgeBias setting.
        /// For All bias, uses circular distance to create island continent.
        /// </summary>
        private float GetDistanceToEdge(int x, int y, int w, int h)
        {
            switch (_cfg.EdgeBias)
            {
                case EdgeBiasDirection.West:
                    return x; // Distance from west edge

                case EdgeBiasDirection.East:
                    return (w - 1) - x; // Distance from east edge

                case EdgeBiasDirection.North:
                    return y; // Distance from north edge

                case EdgeBiasDirection.South:
                    return (h - 1) - y; // Distance from south edge

                case EdgeBiasDirection.All:
                    // Use CIRCULAR distance for island continent
                    // This creates a round shape instead of square
                    float centerX = (w - 1) / 2f;
                    float centerY = (h - 1) / 2f;
                    float dx = x - centerX;
                    float dy = y - centerY;
                    float distFromCenter = Mathf.Sqrt(dx * dx + dy * dy);
                    float maxRadius = Mathf.Min(centerX, centerY); // Radius to nearest edge
                    
                    // Return distance from edge: 0 at circular edge, maxRadius at center
                    return maxRadius - distFromCenter;

                default:
                    return float.MaxValue; // No edge bias
            }
        }

        /// <summary>
        /// Adds noise-based coastal complexity for irregular coastlines.
        /// </summary>
        private void AddCoastalComplexity(WorldArrays world, int w, int h, float offsetX, float offsetY)
        {
            float complexityScale = Mathf.Max(50f, _cfg.CoastalComplexityScale);
            
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    float elev = world.ElevationRaw[idx];
                    
                    // Only affect the coastal transition zone
                    if (elev > 0.1f && elev < 0.5f)
                    {
                        float coastNoise = Mathf.PerlinNoise(
                            (x + offsetX) / complexityScale,
                            (y + offsetY) / complexityScale
                        );
                        
                        float variation = (coastNoise - 0.5f) * _cfg.CoastalComplexity * 0.2f;
                        world.ElevationRaw[idx] = Mathf.Clamp01(elev + variation);
                    }
                }
            }
        }

        /// <summary>
        /// Multi-octave Perlin noise sampling.
        /// </summary>
        private float SampleNoise(float x, float y, float scale, int octaves, float persistence, float lacunarity)
        {
            float total = 0f;
            float frequency = 1f / scale;
            float amplitude = 1f;
            float maxValue = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float sampleX = x * frequency;
                float sampleY = y * frequency;
                
                float noise = Mathf.PerlinNoise(sampleX, sampleY);
                total += noise * amplitude;
                
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return total / maxValue;
        }
    }
}
