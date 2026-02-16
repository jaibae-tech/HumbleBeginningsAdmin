using System;
using System.Collections.Generic;
using UnityEngine;
using MapMaker.Shared.Data;
using MapMaker.Shared.Utils;
using MapMaker.Modules.Elevation1.Config;
using MapMaker.Modules.Elevation;
using MapMaker.Core.Logging;

namespace MapMaker.Modules.Elevation1.Scripts
{
    /// <summary>
    /// Elevation Step 1 (Macro Geography, macro-first, map-scale-aware).
    ///
    /// Outputs (WorldArrays):
    ///   LandMask01, PlateId, Uplift01, Ruggedness01, ElevationRaw
    ///
    /// This module is responsible for macro structure only:
    ///   - landmass from a continuous crust field (percentile sea level)
    ///   - tectonic uplift belts from plate boundaries
    ///   - broad relief + basins (negative relief) for downstream hydrology
    ///   - approximate bathymetry (shelf) so oceans are not flat blobs
    ///
    /// IMPORTANT: This module does not "place lakes". It only makes basins.
    /// </summary>
    public sealed class ElevationGenerator
    {
        private readonly HB_ElevationConfig _cfg;
        private readonly SeedContext _seed;
        private readonly LogEmitter _emit;

        public ElevationGenerator(HB_ElevationConfig cfg, SeedContext seed, LogEmitter emit)
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
            float tileMiles = Mathf.Max(0.01f, _cfg.TileSizeMiles);
            float worldMilesX = w * tileMiles;
            float worldMilesY = h * tileMiles;
            float areaMiles2 = worldMilesX * worldMilesY;

            // Derived counts (densities are per 1,000,000 sq miles)
            int crustOrigins = Mathf.Clamp(Mathf.RoundToInt(_cfg.CrustOriginDensity * areaMiles2 / 1_000_000f), 3, 256);
            int plateCount = Mathf.Clamp(Mathf.RoundToInt(_cfg.PlateDensity * areaMiles2 / 1_000_000f), 2, 255);
            int basinCount = Mathf.Clamp(Mathf.RoundToInt(_cfg.BasinDensity * areaMiles2 / 1_000_000f), 0, 512);

            // Coarse crust grid derived from miles
            float macroCellMiles = Mathf.Max(2f, _cfg.MacroCellSizeMiles);
            int cw = Mathf.Clamp(Mathf.RoundToInt(worldMilesX / macroCellMiles), 64, 1024);
            int ch = Mathf.Clamp(Mathf.RoundToInt(worldMilesY / macroCellMiles), 64, 1024);

            if (_cfg.DebugEnabled)
            {
                DumpConfigAndDerived(w, h, tileMiles, worldMilesX, worldMilesY, areaMiles2, cw, ch, crustOrigins, plateCount, basinCount);
            }

            // ---- 1) Continuous crust field on coarse grid ----
            float[] crustCoarse = new float[cw * ch];
            BuildCrustFieldCoarse(crustCoarse, cw, ch, crustOrigins, worldMilesX, worldMilesY);

            // Sea level from percentile (OceanPercent)
            float oceanPct = Mathf.Clamp01(_cfg.OceanPercent);
            float seaT0 = ComputeQuantile(crustCoarse, oceanPct);

            // Coast carve: only near sea level, preserves overall land/ocean shares
            if (_cfg.CoastCarveStrength > 0.0001f)
            {
                ApplyCoastCarveNearSeaLevel(crustCoarse, cw, ch, seaT0, worldMilesX, worldMilesY);
            }

            float seaT = ComputeQuantile(crustCoarse, oceanPct);

            // ---- 2) Select dominant supercontinent + islands (coarse) ----
            bool[] landCoarse = new bool[cw * ch];
            for (int i = 0; i < crustCoarse.Length; i++)
                landCoarse[i] = crustCoarse[i] >= seaT;

            bool[] mainCoarse = new bool[cw * ch];
            KeepLargestComponentInPlace(landCoarse, cw, ch, mainCoarse, out int mainTiles, out int totalLandTiles);

            // Add islands as fraction of MAIN land, within a max distance
            if (_cfg.IslandFractionOfLand > 0.0001f)
            {
                int targetExtra = Mathf.RoundToInt(mainTiles * Mathf.Clamp01(_cfg.IslandFractionOfLand));
                AddNearbyIslandsFromCrust(
                    crustCoarse,
                    mainCoarse,
                    landCoarse,
                    cw,
                    ch,
                    targetExtra,
                    Mathf.Max(0f, _cfg.MaxIslandDistanceFromMainMiles),
                    Mathf.Max(1f, _cfg.MaxIslandAreaMiles2),
                    Mathf.Clamp01(_cfg.ArchipelagoClustering),
                    worldMilesX,
                    worldMilesY,
                    tileMiles);
            }

            // ---- 3) Upsample crust -> LandMask01 (smooth coast fade) ----
            float coastFadeTiles = Mathf.Max(0.001f, _cfg.CoastFadeMiles / tileMiles);
            UpsampleCrustToLandMask01(crustCoarse, cw, ch, seaT, coastFadeTiles, world.LandMask01, w, h);

            // Optional: edge ocean bias
            if (_cfg.EdgeOceanWidthMiles > 0.001f)
            {
                float edgeTiles = _cfg.EdgeOceanWidthMiles / tileMiles;
                ApplyEdgeOceanMiles(world.LandMask01, w, h, edgeTiles);
            }

            // ---- 4) Plates + uplift belts ----
            GeneratePlatesAndUplift(world, w, h, plateCount, worldMilesX, worldMilesY);

            // ---- 5) Compose elevation (land + shelf bathymetry) ----
            // Basin embedding is applied later (Plan Step 6) so it does not get diluted by
            // macro composition blending. See ElevationBasinEmbeddingPass.
            float[] basinField = null;

            float[] oceanDistCoarse = null;
            if (_cfg.ShelfWidthMiles > 0.001f)
            {
                oceanDistCoarse = BuildOceanDistanceCoarse(world.LandMask01, w, h, tileMiles, _cfg.ShelfWidthMiles);
            }

            ComposeElevation(world, w, h, tileMiles, worldMilesX, worldMilesY, basinField, oceanDistCoarse);

            // ---- 5b) Step 5: Relief Coherence Pass (foothills / reduce harsh adjacency) ----
            ElevationReliefCoherencePass.Apply(_cfg, world, w, h, _emit);

            // ---- 6) Step 3: Field Conditioning (normalize / clamp / macro-safe smoothing) ----
            ConditionElevationField(world, w, h, tileMiles);

            // ---- 6b) Step 6: Basin embedding (hydrology scaffold: shallow depressions + rims) ----
            ElevationBasinEmbeddingPass.Apply(_cfg, world, w, h, _seed.RootSeed, _emit);

            // ---- 7) Step 7: Micro-relief (small variation; masked to land) ----
            ElevationMicroReliefPass.Apply(_cfg, world, w, h, _seed.RootSeed, _emit);

            // ---- 8) Step 8: Final elevation preparation (stability + clamp/remap; no smoothing) ----
            ElevationFinalPreparationPass.Apply(_cfg, world, w, h, _emit);

// ---- STEP 2: Land Elevation Contrast / Variance Expansion ----
// Must run AFTER FinalPreparation so later passes cannot dilute it

if (_cfg.EnableLandContrast && _cfg.LandContrastStrength01 > 0f)
{
    ApplyLandContrast01(
        world.ElevationRaw,
        world.LandMask01,
        landMaskThreshold01: Mathf.Clamp01(_cfg.LandContrast_LandMaskThreshold01),
        sCurveStrength01: Mathf.Clamp01(_cfg.LandContrastStrength01),
        gamma: Mathf.Max(0.01f, _cfg.LandContrastGamma)
    );
}

            // ---- 7) Ruggedness proxy ----
            ComputeRuggednessProxy(world, w, h);

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "ELEV_COMPLETE",
                "Macro-first elevation generation complete (scale-aware)");
        }

        private void DumpConfigAndDerived(
            int w, int h,
            float tileMiles,
            float worldMilesX,
            float worldMilesY,
            float areaMiles2,
            int cw,
            int ch,
            int crustOrigins,
            int plateCount,
            int basinCount)
        {
            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Init, "ELEV_CFG_DERIVED",
                $"Map={w}x{h} tiles, TileSizeMiles={tileMiles:F3}, WorldMiles={worldMilesX:F1}x{worldMilesY:F1}, Area={areaMiles2:F0} mi^2\n" +
                $"CoarseCrust={cw}x{ch} (MacroCell={_cfg.MacroCellSizeMiles:F1}mi)\n" +
                $"Counts: CrustOrigins={crustOrigins} (density={_cfg.CrustOriginDensity:F2}/Mmi^2), Plates={plateCount} (density={_cfg.PlateDensity:F2}/Mmi^2), Basins={basinCount} (density={_cfg.BasinDensity:F2}/Mmi^2)\n" +
                $"Sea: OceanPercent={_cfg.OceanPercent:P0}, CoastFade={_cfg.CoastFadeMiles:F1}mi, CoastCarveStrength={_cfg.CoastCarveStrength:F2}, CoastCarveScale={_cfg.CoastCarveScaleMiles:F1}mi\n" +
                $"Islands: FractionOfLand={_cfg.IslandFractionOfLand:P0}, MaxDist={_cfg.MaxIslandDistanceFromMainMiles:F0}mi, MaxIslandArea={_cfg.MaxIslandAreaMiles2:F0}mi^2, Cluster={_cfg.ArchipelagoClustering:F2}\n" +
                $"Plates: MinSep={_cfg.PlateMinSeparationMiles:F0}mi, BoundaryWidth={_cfg.BoundaryWidthMiles:F0}mi, Seg={_cfg.BoundarySegmentation:F2}@{_cfg.BoundarySegmentationScaleMiles:F0}mi, Uplift(conv/div/trans)={_cfg.ConvergentUpliftStrength:F2}/{_cfg.DivergentUpliftStrength:F2}/{_cfg.TransformUpliftStrength:F2}\n" +
                $"Relief: OceanBaseDepth={_cfg.OceanBaseDepth:F2}, LandBase={_cfg.LandBaseHeight:F2}, MountainH={_cfg.MountainHeight:F2}, PlateauH={_cfg.PlateauHeight:F2}^pow{_cfg.PlateauPower:F2}\n" +
                $"Noise: Regional={_cfg.RegionalReliefStrength:F2}*{_cfg.RegionalReliefHeight:F2}@{_cfg.RegionalReliefScaleMiles:F0}mi, Detail={_cfg.DetailReliefStrength:F2}*{_cfg.DetailReliefHeight:F2}@{_cfg.DetailReliefScaleMiles:F0}mi\n" +
                $"Basins: Scale={_cfg.BasinScaleMiles:F0}mi, Strength={_cfg.BasinStrength:F2}, Rim={_cfg.BasinRimStrength:F2}\n" +
                $"Shelf: Width={_cfg.ShelfWidthMiles:F0}mi, CurvePow={_cfg.OceanDepthCurvePower:F2}, EdgeOceanWidth={_cfg.EdgeOceanWidthMiles:F0}mi");
        }

        // =====================================================================================
        // Crust field (macro landmass)
        // =====================================================================================

        private struct Kernel
        {
            public Vector2 Center01;
            public float A01; // major axis in 01 space
            public float B01; // minor axis in 01 space
            public float Cos;
            public float Sin;
            public float Weight;

            public float Eval(float u, float v)
            {
                float dx = u - Center01.x;
                float dy = v - Center01.y;

                float rx = dx * Cos - dy * Sin;
                float ry = dx * Sin + dy * Cos;

                float nx = rx / Mathf.Max(1e-6f, A01);
                float ny = ry / Mathf.Max(1e-6f, B01);
                float r2 = nx * nx + ny * ny;
                return Weight * Mathf.Exp(-r2 * 1.25f);
            }
        }

        private void BuildCrustFieldCoarse(float[] crust, int cw, int ch, int originCount, float worldMilesX, float worldMilesY)
        {
            Array.Clear(crust, 0, crust.Length);
            var rng = _seed.ElevationRng;

            var kernels = new Kernel[originCount];

            // Convert typical axis lengths (miles) to 01 units.
            float major01 = Mathf.Clamp01(_cfg.CrustOriginMajorAxisMiles / Mathf.Max(1f, worldMilesX));
            float minor01 = Mathf.Clamp01(_cfg.CrustOriginMinorAxisMiles / Mathf.Max(1f, worldMilesY));
            float jitter = Mathf.Clamp01(_cfg.CrustOriginAxisJitter);
            float pull = Mathf.Clamp01(_cfg.CrustCenterPull);

            for (int i = 0; i < originCount; i++)
            {
                float px = (float)rng.NextDouble();
                float py = (float)rng.NextDouble();
                // Center pull
                px = Mathf.Lerp(px, 0.5f, pull);
                py = Mathf.Lerp(py, 0.5f, pull);

                float a = major01 * Mathf.Lerp(1f - 0.55f * jitter, 1f + 0.85f * jitter, (float)rng.NextDouble());
                float b = minor01 * Mathf.Lerp(1f - 0.55f * jitter, 1f + 0.85f * jitter, (float)rng.NextDouble());
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;

                // Keep weight bounded so summed field doesn't become a single "superblob".
                float w = Mathf.Lerp(0.75f, 1.25f, (float)rng.NextDouble());

                kernels[i] = new Kernel
                {
                    Center01 = new Vector2(px, py),
                    A01 = Mathf.Max(1e-4f, a),
                    B01 = Mathf.Max(1e-4f, b),
                    Cos = Mathf.Cos(ang),
                    Sin = Mathf.Sin(ang),
                    Weight = w
                };
            }

            float warpS = Mathf.Clamp01(_cfg.MacroWarpStrength);
            float warpScaleMiles = Mathf.Max(10f, _cfg.MacroWarpScaleMiles);
            float ox = (float)rng.NextDouble() * 10000f;
            float oy = (float)rng.NextDouble() * 10000f;

            float min = float.MaxValue;
            float max = float.MinValue;

            for (int y = 0; y < ch; y++)
            for (int x = 0; x < cw; x++)
            {
                float u = (x + 0.5f) / cw;
                float v = (y + 0.5f) / ch;

                // Domain warp in mile-space for scale invariance
                if (warpS > 0f)
                {
                    float xm = u * worldMilesX;
                    float ym = v * worldMilesY;
                    float nx = Mathf.PerlinNoise((xm + ox) / warpScaleMiles, (ym + oy) / warpScaleMiles) * 2f - 1f;
                    float ny = Mathf.PerlinNoise((xm + ox + 53.1f) / warpScaleMiles, (ym + oy + 97.7f) / warpScaleMiles) * 2f - 1f;
                    u = Mathf.Clamp01(u + nx * warpS * 0.08f);
                    v = Mathf.Clamp01(v + ny * warpS * 0.08f);
                }

                float val = 0f;
                for (int i = 0; i < kernels.Length; i++)
                    val += kernels[i].Eval(u, v);

                int idx = y * cw + x;
                crust[idx] = val;
                if (val < min) min = val;
                if (val > max) max = val;
            }

            float inv = 1f / Mathf.Max(1e-6f, max - min);
            for (int i = 0; i < crust.Length; i++)
                crust[i] = Mathf.Clamp01((crust[i] - min) * inv);
        }

        private void ApplyCoastCarveNearSeaLevel(float[] crust, int cw, int ch, float seaThreshold, float worldMilesX, float worldMilesY)
        {
            float strength = Mathf.Clamp01(_cfg.CoastCarveStrength);
            float scaleMiles = Mathf.Max(5f, _cfg.CoastCarveScaleMiles);
            float fadeMiles = Mathf.Max(0.001f, _cfg.CoastFadeMiles);

            // In crust space, define a band around sea level based on a small fraction of range.
            // We approximate by translating CoastFadeMiles into a normalized band width.
            float band01 = Mathf.Clamp01(fadeMiles / Mathf.Max(1f, Mathf.Min(worldMilesX, worldMilesY)) * 6f);
            float band = Mathf.Max(0.005f, band01);

            var rng = _seed.CoastRng;
            float ox = (float)rng.NextDouble() * 10000f;
            float oy = (float)rng.NextDouble() * 10000f;

            for (int y = 0; y < ch; y++)
            for (int x = 0; x < cw; x++)
            {
                int idx = y * cw + x;
                float v = crust[idx];
                float d = Mathf.Abs(v - seaThreshold);
                if (d > band) continue;

                float u01 = (x + 0.5f) / cw;
                float v01 = (y + 0.5f) / ch;
                float xm = u01 * worldMilesX;
                float ym = v01 * worldMilesY;

                float n = Mathf.PerlinNoise((xm + ox) / scaleMiles, (ym + oy) / scaleMiles) * 2f - 1f;
                // Gate strongest near sea level.
                float gate = 1f - Mathf.Clamp01(d / band);
                gate = gate * gate * (3f - 2f * gate);

                crust[idx] = Mathf.Clamp01(v + n * strength * 0.18f * gate);
            }
        }

        private static void UpsampleCrustToLandMask01(
            float[] crustCoarse,
            int cw,
            int ch,
            float seaThreshold,
            float coastFadeTiles,
            float[] landMask01,
            int w,
            int h)
        {
            // Smooth coastal ramp:
            // 0 at/below seaThreshold, 1 well above. coastFadeTiles sets the transition width.
            // We implement the fade in "crust value" space; coastFadeTiles maps to a fraction of coarse sampling.
            float coastFade = Mathf.Clamp(coastFadeTiles * 0.0025f, 0.002f, 0.12f);

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w;
                float v = (y + 0.5f) / h;

                float cx = u * cw - 0.5f;
                float cy = v * ch - 0.5f;
                int x0 = Mathf.Clamp((int)Mathf.Floor(cx), 0, cw - 1);
                int y0 = Mathf.Clamp((int)Mathf.Floor(cy), 0, ch - 1);
                int x1 = Mathf.Clamp(x0 + 1, 0, cw - 1);
                int y1 = Mathf.Clamp(y0 + 1, 0, ch - 1);
                float tx = Mathf.Clamp01(cx - x0);
                float ty = Mathf.Clamp01(cy - y0);

                float a = crustCoarse[y0 * cw + x0];
                float b = crustCoarse[y0 * cw + x1];
                float c = crustCoarse[y1 * cw + x0];
                float d = crustCoarse[y1 * cw + x1];
                float ab = Mathf.Lerp(a, b, tx);
                float cd = Mathf.Lerp(c, d, tx);
                float crust = Mathf.Lerp(ab, cd, ty);

                float t = (crust - seaThreshold) / Mathf.Max(1e-6f, coastFade);
                t = Mathf.Clamp01(0.5f + 0.5f * t);
                t = t * t * (3f - 2f * t);
                landMask01[y * w + x] = t;
            }
        }

        private static void ApplyEdgeOceanMiles(float[] landMask01, int w, int h, float edgeWidthTiles)
        {
            if (edgeWidthTiles <= 0f) return;

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                float dx = Mathf.Min(x, w - 1 - x);
                float dy = Mathf.Min(y, h - 1 - y);
                float d = Mathf.Min(dx, dy);
                if (d >= edgeWidthTiles) continue;
                float t = Mathf.Clamp01(d / Mathf.Max(1e-6f, edgeWidthTiles));
                t = t * t * (3f - 2f * t);
                landMask01[idx] *= t;
            }
        }

        // =====================================================================================
        // Connectivity / islands
        // =====================================================================================

        private static void KeepLargestComponentInPlace(bool[] land, int w, int h, bool[] mainOut, out int mainCount, out int totalLand)
        {
            Array.Clear(mainOut, 0, mainOut.Length);
            totalLand = 0;
            for (int i = 0; i < land.Length; i++) if (land[i]) totalLand++;

            int[] labels = new int[land.Length];
            int label = 0;
            int bestLabel = -1;
            mainCount = 0;

            var q = new Queue<int>(1024);

            for (int i = 0; i < land.Length; i++)
            {
                if (!land[i] || labels[i] != 0) continue;
                label++;
                int count = 0;
                labels[i] = label;
                q.Enqueue(i);
                while (q.Count > 0)
                {
                    int idx = q.Dequeue();
                    count++;
                    int x = idx % w;
                    int y = idx / w;

                    Enqueue(x - 1, y);
                    Enqueue(x + 1, y);
                    Enqueue(x, y - 1);
                    Enqueue(x, y + 1);

                    void Enqueue(int nx, int ny)
                    {
                        if ((uint)nx >= (uint)w || (uint)ny >= (uint)h) return;
                        int ni = ny * w + nx;
                        if (!land[ni] || labels[ni] != 0) return;
                        labels[ni] = label;
                        q.Enqueue(ni);
                    }
                }

                if (count > mainCount)
                {
                    mainCount = count;
                    bestLabel = label;
                }
            }

            for (int i = 0; i < land.Length; i++)
            {
                bool isMain = labels[i] == bestLabel;
                mainOut[i] = isMain;
                land[i] = isMain;
            }
        }

        private static void AddNearbyIslandsFromCrust(
            float[] crust,
            bool[] main,
            bool[] land,
            int w,
            int h,
            int targetExtraTiles,
            float maxDistMiles,
            float maxIslandAreaMiles2,
            float clustering,
            float worldMilesX,
            float worldMilesY,
            float tileMiles)
        {
            if (targetExtraTiles <= 0) return;

            int n = w * h;
            int[] dist = new int[n];
            const int INF = 1_000_000;
            for (int i = 0; i < n; i++) dist[i] = INF;

            var q = new Queue<int>(4096);
            for (int i = 0; i < n; i++)
            {
                if (!main[i]) continue;
                dist[i] = 0;
                q.Enqueue(i);
            }
            while (q.Count > 0)
            {
                int idx = q.Dequeue();
                int x = idx % w;
                int y = idx / w;
                int d0 = dist[idx];
                int d1 = d0 + 1;
                Visit(x - 1, y);
                Visit(x + 1, y);
                Visit(x, y - 1);
                Visit(x, y + 1);

                void Visit(int nx, int ny)
                {
                    if ((uint)nx >= (uint)w || (uint)ny >= (uint)h) return;
                    int ni = ny * w + nx;
                    if (dist[ni] <= d1) return;
                    dist[ni] = d1;
                    q.Enqueue(ni);
                }
            }

            int maxDistTiles = Mathf.Max(1, Mathf.RoundToInt(maxDistMiles / Mathf.Max(0.01f, tileMiles) / Mathf.Max(1, Mathf.RoundToInt(Mathf.Min(worldMilesX, worldMilesY) / Mathf.Min(worldMilesX, worldMilesY)))));
            maxDistTiles = Mathf.Max(1, Mathf.RoundToInt(maxDistMiles / tileMiles));

            // Candidate tiles: within distance, currently ocean.
            var candidates = new List<int>(n / 16);
            for (int i = 0; i < n; i++)
            {
                if (land[i]) continue;
                if (dist[i] > maxDistTiles) continue;
                candidates.Add(i);
            }

            // Score: crust value + optional clustering toward existing islands
            candidates.Sort((a, b) => crust[b].CompareTo(crust[a]));

            int added = 0;
            for (int ci = 0; ci < candidates.Count && added < targetExtraTiles; ci++)
            {
                int idx = candidates[ci];

                if (clustering > 0f)
                {
                    int x = idx % w;
                    int y = idx / w;
                    int nearLand = 0;
                    if (x > 0 && land[idx - 1]) nearLand++;
                    if (x < w - 1 && land[idx + 1]) nearLand++;
                    if (y > 0 && land[idx - w]) nearLand++;
                    if (y < h - 1 && land[idx + w]) nearLand++;
                    float p = Mathf.Lerp(0.35f, 0.95f, Mathf.Clamp01(nearLand / 2f));
                    if (UnityEngine.Random.value > Mathf.Lerp(1f, p, clustering))
                        continue;
                }

                land[idx] = true;
                added++;
            }

            // Prune island components exceeding MaxIslandArea
            int maxIslandTiles = Mathf.Max(1, Mathf.RoundToInt(maxIslandAreaMiles2 / (tileMiles * tileMiles)));
            PruneLargeNonMainComponents(land, main, w, h, maxIslandTiles);
        }

        private static void PruneLargeNonMainComponents(bool[] land, bool[] main, int w, int h, int maxIslandTiles)
        {
            int n = w * h;
            int[] visited = new int[n];
            int stamp = 1;
            var q = new Queue<int>(1024);

            for (int i = 0; i < n; i++)
            {
                if (!land[i] || main[i] || visited[i] == stamp) continue;

                int count = 0;
                var members = new List<int>(256);
                visited[i] = stamp;
                q.Enqueue(i);
                while (q.Count > 0)
                {
                    int idx = q.Dequeue();
                    members.Add(idx);
                    count++;
                    int x = idx % w;
                    int y = idx / w;
                    Enq(x - 1, y);
                    Enq(x + 1, y);
                    Enq(x, y - 1);
                    Enq(x, y + 1);

                    void Enq(int nx, int ny)
                    {
                        if ((uint)nx >= (uint)w || (uint)ny >= (uint)h) return;
                        int ni = ny * w + nx;
                        if (!land[ni] || main[ni] || visited[ni] == stamp) return;
                        visited[ni] = stamp;
                        q.Enqueue(ni);
                    }
                }

                stamp++;

                if (count > maxIslandTiles)
                {
                    for (int m = 0; m < members.Count; m++)
                        land[members[m]] = false;
                }
            }
        }

        // =====================================================================================
        // Plates + uplift
        // =====================================================================================

        private struct PlateSeed
        {
            public Vector2 Pos01;
            public Vector2 Vel;
        }

        private void GeneratePlatesAndUplift(WorldArrays world, int w, int h, int plateCount, float worldMilesX, float worldMilesY)
        {
            var rng = _seed.ElevationRng;
            var plates = new PlateSeed[plateCount];

            float minSepMiles = Mathf.Max(1f, _cfg.PlateMinSeparationMiles);
            // Convert min separation to 01 distance using min dimension
            float minDimMiles = Mathf.Max(1f, Mathf.Min(worldMilesX, worldMilesY));
            float minSep01 = Mathf.Clamp01(minSepMiles / minDimMiles);

            for (int i = 0; i < plateCount; i++)
            {
                Vector2 p = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());
                for (int t = 0; t < 128; t++)
                {
                    p = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());
                    bool ok = true;
                    for (int j = 0; j < i; j++)
                    {
                        if ((plates[j].Pos01 - p).sqrMagnitude < minSep01 * minSep01) { ok = false; break; }
                    }
                    if (ok) break;
                }

                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                float spd = Mathf.Lerp(_cfg.PlateSpeedMin, _cfg.PlateSpeedMax, (float)rng.NextDouble());
                plates[i] = new PlateSeed
                {
                    Pos01 = p,
                    Vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd
                };
            }

            float seg = Mathf.Clamp01(_cfg.BoundarySegmentation);
            float segScaleMiles = Mathf.Max(5f, _cfg.BoundarySegmentationScaleMiles);
            float segOX = (float)rng.NextDouble() * 10000f;
            float segOY = (float)rng.NextDouble() * 10000f;

            float bwMiles = Mathf.Max(1f, _cfg.BoundaryWidthMiles);
            float invW = 1f / w;
            float invH = 1f / h;

            // Plate boundary warping seeds (deterministic per world seed)
            float warpStrength = Mathf.Clamp01(_cfg.PlateWarpStrength);
            float warpAmpMiles = Mathf.Max(0f, _cfg.PlateWarpAmplitudeMiles);
            float warpScaleMiles = Mathf.Max(5f, _cfg.PlateWarpScaleMiles);
            int warpOct = Mathf.Clamp(_cfg.PlateWarpOctaves, 1, 4);
            float warpOX = (float)rng.NextDouble() * 10000f;
            float warpOY = (float)rng.NextDouble() * 10000f;

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                float land = world.LandMask01[idx];

                float fx = (x + 0.5f) * invW;
                float fy = (y + 0.5f) * invH;
                float pxMiles = fx * worldMilesX;
                float pyMiles = fy * worldMilesY;

                // Domain-warp the plate lookup so Voronoi edges do not remain straight.
                // Important: this must influence BOTH plate id assignment and boundary-distance sampling.
                float pfx = fx;
                float pfy = fy;
                if (warpStrength > 0.0001f && warpAmpMiles > 0.001f)
                {
                    WarpPlateDomain(
                        pxMiles, pyMiles,
                        worldMilesX, worldMilesY,
                        warpOX, warpOY,
                        warpAmpMiles, warpScaleMiles,
                        warpOct, warpStrength,
                        out pfx, out pfy);
                }

                int a = 0;
                int b = 1;
                float da = float.MaxValue;
                float db = float.MaxValue;

                for (int i = 0; i < plates.Length; i++)
                {
                    float dxMiles = (pfx - plates[i].Pos01.x) * worldMilesX;
                    float dyMiles = (pfy - plates[i].Pos01.y) * worldMilesY;
                    float d = dxMiles * dxMiles + dyMiles * dyMiles;
                    if (d < da)
                    {
                        db = da; b = a;
                        da = d; a = i;
                    }
                    else if (d < db)
                    {
                        db = d; b = i;
                    }
                }

                world.PlateId[idx] = (ushort)a;

                float dA = Mathf.Sqrt(Mathf.Max(0f, da));
                float dB = Mathf.Sqrt(Mathf.Max(0f, db));
                float dmMiles = Mathf.Abs(dB - dA);
                float boundary = Mathf.Exp(-(dmMiles * dmMiles) / Mathf.Max(1e-6f, bwMiles * bwMiles));

                if (seg > 0f)
                {
                    float g = Mathf.PerlinNoise((pxMiles + segOX) / segScaleMiles, (pyMiles + segOY) / segScaleMiles);
                    float gate = Mathf.SmoothStep(0.25f, 0.85f, g);
                    boundary *= Mathf.Lerp(1f, gate, seg);
                }

                // Boundary normal approx in 01 space (good enough for classifying convergence)
                Vector2 nrm = (plates[b].Pos01 - plates[a].Pos01);
                float nl = nrm.magnitude;
                if (nl > 1e-6f) nrm /= nl;
                else nrm = Vector2.right;

                Vector2 dv = plates[b].Vel - plates[a].Vel;
                float conv = Vector2.Dot(dv, nrm);

                float eps = Mathf.Clamp01(_cfg.BoundaryConvergenceEpsilon);
                float uplift;
                if (conv < -eps) uplift = _cfg.ConvergentUpliftStrength;
                else if (conv > eps) uplift = _cfg.DivergentUpliftStrength;
                else uplift = _cfg.TransformUpliftStrength;

                float landGate = Mathf.SmoothStep(0.10f, 0.85f, land);
                world.Uplift01[idx] = Mathf.Clamp01(boundary * uplift) * landGate;
            }
        }

        /// <summary>
        /// Warp the sampling domain used for plate assignment/boundary distance.
        /// This bends otherwise-straight Voronoi edges so tectonic partitions don't imprint as linear facets.
        ///
        /// Inputs are in miles (pxMiles/pyMiles), outputs are UV offsets (pfx/pfy) clamped to 0..1.
        /// </summary>
        private static void WarpPlateDomain(
            float pxMiles,
            float pyMiles,
            float worldMilesX,
            float worldMilesY,
            float ox,
            float oy,
            float amplitudeMiles,
            float scaleMiles,
            int octaves,
            float strength,
            out float pfx,
            out float pfy)
        {
            // Convert miles amplitude into UV scale (separately per axis).
            float ax = (worldMilesX > 1e-6f) ? (amplitudeMiles / worldMilesX) : 0f;
            float ay = (worldMilesY > 1e-6f) ? (amplitudeMiles / worldMilesY) : 0f;

            float nx = 0f;
            float ny = 0f;
            float amp = 1f;
            float freq = 1f;
            float norm = 0f;

            for (int o = 0; o < octaves; o++)
            {
                float sx = (pxMiles + ox) / (scaleMiles / freq);
                float sy = (pyMiles + oy) / (scaleMiles / freq);

                // Two decorrelated noise channels.
                float a = Mathf.PerlinNoise(sx, sy);
                float b = Mathf.PerlinNoise(sx + 37.13f, sy + 91.77f);

                nx += (a * 2f - 1f) * amp;
                ny += (b * 2f - 1f) * amp;

                norm += amp;
                amp *= 0.5f;
                freq *= 2f;
            }

            if (norm > 1e-6f)
            {
                nx /= norm;
                ny /= norm;
            }

            // Strength gates overall deformation.
            nx *= ax * Mathf.Clamp01(strength);
            ny *= ay * Mathf.Clamp01(strength);

            pfx = Mathf.Clamp01((pxMiles / Mathf.Max(1e-6f, worldMilesX)) + nx);
            pfy = Mathf.Clamp01((pyMiles / Mathf.Max(1e-6f, worldMilesY)) + ny);
        }

        // =====================================================================================
        // Basins
        // =====================================================================================

        private struct BasinSeed
        {
            public float X01;
            public float Y01;
            public float RadiusMiles;
            public float Depth;
        }

        private void BuildBasinField(float[] basin, int w, int h, int basinCount, float tileMiles, float worldMilesX, float worldMilesY)
        {
            Array.Clear(basin, 0, basin.Length);
            var rng = _seed.HydrologyRng;

            float baseRadius = Mathf.Max(5f, _cfg.BasinScaleMiles);
            float strength = Mathf.Clamp01(_cfg.BasinStrength);
            float rim = Mathf.Clamp01(_cfg.BasinRimStrength);

            var seeds = new BasinSeed[basinCount];
            for (int i = 0; i < basinCount; i++)
            {
                float x01 = (float)rng.NextDouble();
                float y01 = (float)rng.NextDouble();
                // Bias basins slightly toward interior
                x01 = Mathf.Lerp(x01, 0.5f, 0.25f);
                y01 = Mathf.Lerp(y01, 0.5f, 0.25f);

                float r = baseRadius * Mathf.Lerp(0.65f, 1.45f, (float)rng.NextDouble());
                float d = Mathf.Lerp(0.25f, 1.0f, (float)rng.NextDouble()) * strength;

                seeds[i] = new BasinSeed { X01 = x01, Y01 = y01, RadiusMiles = r, Depth = d };
            }

            float invW = 1f / w;
            float invH = 1f / h;

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float fx = (x + 0.5f) * invW;
                float fy = (y + 0.5f) * invH;
                float xm = fx * worldMilesX;
                float ym = fy * worldMilesY;

                float val = 0f;
                for (int i = 0; i < seeds.Length; i++)
                {
                    float cxm = seeds[i].X01 * worldMilesX;
                    float cym = seeds[i].Y01 * worldMilesY;
                    float dx = xm - cxm;
                    float dy = ym - cym;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float r = Mathf.Max(1f, seeds[i].RadiusMiles);

                    float t = Mathf.Clamp01(dist / r);
                    // Negative gaussian-ish depression
                    float bowl = -seeds[i].Depth * Mathf.Exp(-t * t * 2.0f);

                    // Optional rim (subtle positive ring to create closure)
                    float rimBump = 0f;
                    if (rim > 0f)
                    {
                        float ring = Mathf.Exp(-Mathf.Pow((t - 1.0f) * 3.0f, 2f));
                        rimBump = ring * seeds[i].Depth * 0.35f * rim;
                    }

                    val += bowl + rimBump;
                }

                basin[y * w + x] = val;
            }
        }

        // =====================================================================================
        // Ocean distance coarse (for shelf)
        // =====================================================================================

        private static float[] BuildOceanDistanceCoarse(float[] landMask01, int w, int h, float tileMiles, float shelfWidthMiles)
        {
            float shelfTiles = Mathf.Max(1f, shelfWidthMiles / tileMiles);
            int stride = Mathf.Clamp(Mathf.RoundToInt(shelfTiles / 8f), 1, 64);
            int cw = Mathf.Max(1, (w + stride - 1) / stride);
            int ch = Mathf.Max(1, (h + stride - 1) / stride);
            int n = cw * ch;

            int[] dist = new int[n];
            const int INF = 1_000_000;
            for (int i = 0; i < n; i++) dist[i] = INF;

            var q = new Queue<int>(n / 8);

            // Initialize: coastal boundary as sources.
            for (int cy = 0; cy < ch; cy++)
            for (int cx = 0; cx < cw; cx++)
            {
                int sx = Mathf.Min(w - 1, cx * stride);
                int sy = Mathf.Min(h - 1, cy * stride);
                float lm = landMask01[sy * w + sx];
                bool isLand = lm >= 0.5f;
                if (isLand) continue;

                // If any neighbor coarse cell is land, treat as coast.
                bool nearLand = false;
                if (cx > 0)
                {
                    int nx = Mathf.Min(w - 1, (cx - 1) * stride);
                    float nLm = landMask01[sy * w + nx];
                    if (nLm >= 0.5f) nearLand = true;
                }
                if (!nearLand && cx < cw - 1)
                {
                    int nx = Mathf.Min(w - 1, (cx + 1) * stride);
                    float nLm = landMask01[sy * w + nx];
                    if (nLm >= 0.5f) nearLand = true;
                }
                if (!nearLand && cy > 0)
                {
                    int ny = Mathf.Min(h - 1, (cy - 1) * stride);
                    float nLm = landMask01[ny * w + sx];
                    if (nLm >= 0.5f) nearLand = true;
                }
                if (!nearLand && cy < ch - 1)
                {
                    int ny = Mathf.Min(h - 1, (cy + 1) * stride);
                    float nLm = landMask01[ny * w + sx];
                    if (nLm >= 0.5f) nearLand = true;
                }

                if (nearLand)
                {
                    int i = cy * cw + cx;
                    dist[i] = 0;
                    q.Enqueue(i);
                }
            }

            int maxSteps = Mathf.RoundToInt(shelfTiles / stride) + 2;
            while (q.Count > 0)
            {
                int idx = q.Dequeue();
                int x = idx % cw;
                int y = idx / cw;
                int d0 = dist[idx];
                int d1 = d0 + 1;
                if (d1 > maxSteps) continue;

                Visit(x - 1, y);
                Visit(x + 1, y);
                Visit(x, y - 1);
                Visit(x, y + 1);

                void Visit(int nx, int ny)
                {
                    if ((uint)nx >= (uint)cw || (uint)ny >= (uint)ch) return;
                    int ni = ny * cw + nx;
                    if (dist[ni] <= d1) return;

                    int sx = Mathf.Min(w - 1, nx * stride);
                    int sy = Mathf.Min(h - 1, ny * stride);
                    if (landMask01[sy * w + sx] >= 0.5f) return; // don't propagate through land

                    dist[ni] = d1;
                    q.Enqueue(ni);
                }
            }

            // Convert to miles distance
            float[] distMiles = new float[n];
            for (int i = 0; i < n; i++)
            {
                distMiles[i] = (dist[i] >= INF) ? shelfWidthMiles * 2f : dist[i] * stride * tileMiles;
            }
            // Pack stride/cw/ch into first entries? Not needed; we return a simple array and re-derive.
            // We encode stride and cw in static fields? Not acceptable. Instead: recompute stride/cw/ch in ComposeElevation similarly.
            // We'll store stride/cw/ch by leveraging sentinel NaN pattern is risky; so we return distMiles and recompute stride/cw/ch identically.
            return distMiles;
        }

        // =====================================================================================
        // Compose elevation
        // =====================================================================================

        private void ComposeElevation(WorldArrays world, int w, int h, float tileMiles, float worldMilesX, float worldMilesY, float[] basinField, float[] oceanDistCoarse)
        {
            var rng = _seed.ElevationRng;
            float ox = (float)rng.NextDouble() * 10000f;
            float oy = (float)rng.NextDouble() * 10000f;

            float regionalAmp = Mathf.Clamp01(_cfg.RegionalReliefStrength) * _cfg.RegionalReliefHeight;
            float regionalScaleMiles = Mathf.Max(10f, _cfg.RegionalReliefScaleMiles);
            float detailAmp = Mathf.Clamp01(_cfg.DetailReliefStrength) * _cfg.DetailReliefHeight;
            float detailScaleMiles = Mathf.Max(1f, _cfg.DetailReliefScaleMiles);

            float oceanBase = -Mathf.Abs(_cfg.OceanBaseDepth);
            float landBase = _cfg.LandBaseHeight;
            float plateauHeight = Mathf.Max(0f, _cfg.PlateauHeight);
            float plateauPower = Mathf.Max(0.2f, _cfg.PlateauPower);
            float mountainHeight = Mathf.Max(0f, _cfg.MountainHeight);

            Vector2 tiltDir = _cfg.ContinentalTiltDirection.sqrMagnitude > 1e-6f ? _cfg.ContinentalTiltDirection.normalized : Vector2.right;
            float tiltAmt = _cfg.ContinentalTiltStrength;

            // Shelf shaping parameters
            float shelfWidthMiles = Mathf.Max(0f, _cfg.ShelfWidthMiles);
            float shelfTiles = shelfWidthMiles / tileMiles;
            float depthPow = Mathf.Clamp(_cfg.OceanDepthCurvePower, 0.8f, 4f);
            int stride = (shelfWidthMiles > 0.001f) ? Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(1f, shelfTiles) / 8f), 1, 64) : 1;
            int cW = (shelfWidthMiles > 0.001f) ? Mathf.Max(1, (w + stride - 1) / stride) : 0;
            int cH = (shelfWidthMiles > 0.001f) ? Mathf.Max(1, (h + stride - 1) / stride) : 0;

            float invW = 1f / w;
            float invH = 1f / h;

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                float land = world.LandMask01[idx];
                float uplift = world.Uplift01[idx];
                float fx = (x + 0.5f) * invW;
                float fy = (y + 0.5f) * invH;
                float xm = fx * worldMilesX;
                float ym = fy * worldMilesY;

                // Broad interior relief (mile-space)
                float n1 = Mathf.PerlinNoise((xm + ox) / regionalScaleMiles, (ym + oy) / regionalScaleMiles) * 2f - 1f;
                float n2 = Mathf.PerlinNoise((xm + ox + 371.3f) / (regionalScaleMiles * 0.55f), (ym + oy + 91.7f) / (regionalScaleMiles * 0.55f)) * 2f - 1f;
                float regional = (0.65f * n1 + 0.35f * n2) * regionalAmp;

                float d = Mathf.PerlinNoise((xm + ox + 11.1f) / detailScaleMiles, (ym + oy + 77.7f) / detailScaleMiles) * 2f - 1f;
                float detail = d * detailAmp;

                float plateau = Mathf.Pow(Mathf.Clamp01(land), plateauPower) * plateauHeight;
                float mtn = uplift * mountainHeight;

                float tilt = 0f;
                if (tiltAmt != 0f)
                {
                    float tx = fx - 0.5f; 
                    float ty = fy - 0.5f;
                    tilt = (tx * tiltDir.x + ty * tiltDir.y) * tiltAmt;
                }

                float baseHeight = Mathf.Lerp(oceanBase, landBase, land);
                float e = baseHeight + (plateau + regional + detail + tilt) * land + mtn;

                // Basins only affect land (and near-coast interior slightly)
                if (basinField != null)
                {
                    e += basinField[idx] * Mathf.SmoothStep(0.10f, 0.95f, land);
                }

                // Shelf bathymetry override for ocean so ocean isn't flat.
                if (shelfWidthMiles > 0.001f && land < 0.5f && oceanDistCoarse != null)
                {
                    int cx = Mathf.Clamp(x / stride, 0, cW - 1);
                    int cy = Mathf.Clamp(y / stride, 0, cH - 1);
                    float distMiles = oceanDistCoarse[cy * cW + cx];
                    float t = Mathf.Clamp01(distMiles / shelfWidthMiles);
                    // t=0 at coast -> shallow, t=1 at shelf edge -> deep
                    float depth = oceanBase * Mathf.Pow(t, depthPow);
                    // Blend toward this depth but keep some existing variation
                    e = Mathf.Lerp(e, depth, 0.85f);
                }

                world.ElevationRaw[idx] = e;
            }
        }

        // =====================================================================================
        // Step 3: Field Conditioning (post-compose)
        // =====================================================================================

        private void ConditionElevationField(WorldArrays world, int w, int h, float tileMiles)
        {
            if (!_cfg.ConditioningEnabled) return;

            float[] e = world.ElevationRaw;
            int n = e.Length;
            if (n == 0) return;

            // --- 1) Soft clamp extremes using symmetric percentiles ---
            float cp = Mathf.Clamp(_cfg.ConditioningClampPercent, 0f, 0.05f);
            float lo = (cp > 0f) ? EstimatePercentile(e, n, cp, _seed.RootSeed ^ unchecked((int)0xC0FFEE01)) : FindMin(e);
            float hi = (cp > 0f) ? EstimatePercentile(e, n, 1f - cp, _seed.RootSeed ^ unchecked((int)0xC0FFEE02)) : FindMax(e);

            if (hi <= lo + 1e-6f)
            {
                // Degenerate; nothing to do
                return;
            }

            for (int i = 0; i < n; i++)
                e[i] = Mathf.Clamp(e[i], lo, hi);

            // --- 2) Optional remap into a stable range derived from config ---
            if (_cfg.ConditioningRemapToConfigRange)
            {
                // Target range is derived from composition knobs; keeps runs comparable across seeds/sizes.
                float targetMin = -Mathf.Abs(_cfg.OceanBaseDepth);
                // Typical upper bound: land base + plateau + mountains + a portion of relief.
                float targetMax =
                    _cfg.LandBaseHeight +
                    _cfg.PlateauHeight +
                    _cfg.MountainHeight +
                    Mathf.Abs(_cfg.RegionalReliefHeight) +
                    Mathf.Abs(_cfg.DetailReliefHeight);

                // Add a little headroom so peaks don't hard-clip later.
                targetMax += 0.15f;

                float curMin = lo;
                float curMax = hi;
                float inv = 1f / Mathf.Max(1e-6f, (curMax - curMin));
                float span = Mathf.Max(1e-6f, targetMax - targetMin);

                for (int i = 0; i < n; i++)
                {
                    float t = (e[i] - curMin) * inv;
                    e[i] = targetMin + t * span;
                }

                lo = targetMin;
                hi = targetMax;
            }

            // --- 3) Macro-safe smoothing on a coarse grid, then blend back weakly ---
            float smoothStrength = Mathf.Clamp(_cfg.ConditioningSmoothingStrength, 0f, 0.25f);
            if (smoothStrength > 0.0001f)
            {
                float cellMiles = Mathf.Max(4f, _cfg.ConditioningSmoothingCellMiles);
                int cw = Mathf.Clamp(Mathf.RoundToInt((w * tileMiles) / cellMiles), 64, 512);
                int ch = Mathf.Clamp(Mathf.RoundToInt((h * tileMiles) / cellMiles), 64, 512);

                float[] coarse = new float[cw * ch];
                DownsampleBilinear(e, w, h, coarse, cw, ch);

                float radiusMiles = Mathf.Max(10f, _cfg.ConditioningSmoothingRadiusMiles);
                int rad = Mathf.Clamp(Mathf.RoundToInt(radiusMiles / cellMiles), 1, 64);

                // Separable box blur (2 passes) for speed & stability.
                BoxBlurSeparableInPlace(coarse, cw, ch, rad);

                // Blend smoothed field back into full-res
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    float s = SampleBilinear(coarse, cw, ch, (x + 0.5f) / w, (y + 0.5f) / h);
                    e[idx] = Mathf.Lerp(e[idx], s, smoothStrength);
                }
            }


            if (_cfg.DebugEnabled)
            {
                float mn = FindMin(e);
                float mx = FindMax(e);
                _emit(LogLevel.INFO, LogContext.Module, LogPhase.Progress, "ELEV_CONDITION",
                    $"Conditioning: ClampP={cp:P1}, Remap={_cfg.ConditioningRemapToConfigRange}, Smooth={smoothStrength:F3} @ R={_cfg.ConditioningSmoothingRadiusMiles:F0}mi (cell={_cfg.ConditioningSmoothingCellMiles:F0}mi). Range={mn:F3}..{mx:F3}");
            }
        }

        private static float FindMin(float[] a)
        {
            float m = float.MaxValue;
            for (int i = 0; i < a.Length; i++) if (a[i] < m) m = a[i];
            return m;
        }

        private static float FindMax(float[] a)
        {
            float m = float.MinValue;
            for (int i = 0; i < a.Length; i++) if (a[i] > m) m = a[i];
            return m;
        }

        /// <summary>
        /// Applies a non-linear contrast curve to LAND elevations only.
        /// Oceans are left unchanged so sea-level / shelf logic stays stable.
        ///
        /// Process:
        /// 1) Compute land min/max using LandMask01 threshold.
        /// 2) Remap land into 0..1.
        /// 3) Blend linear -> smoothstep (S-curve) for contrast.
        /// 4) Optional gamma.
        /// 5) Remap back into land min/max.
        /// </summary>
        private static void ApplyLandContrast01(
            float[] elev,
            float[] landMask01,
            float landMaskThreshold01,
            float sCurveStrength01,
            float gamma)
        {
            if (elev == null || landMask01 == null) return;
            if (elev.Length != landMask01.Length) return;
            if (sCurveStrength01 <= 0f) return;

            float landMin = float.PositiveInfinity;
            float landMax = float.NegativeInfinity;
            int landCount = 0;

            for (int i = 0; i < elev.Length; i++)
            {
                if (landMask01[i] < landMaskThreshold01) continue;
                float v = elev[i];
                if (v < landMin) landMin = v;
                if (v > landMax) landMax = v;
                landCount++;
            }

            if (landCount < 8) return;
            float range = landMax - landMin;
            if (range <= 1e-6f) return;

            float k = Mathf.Clamp01(sCurveStrength01);
            gamma = Mathf.Clamp(gamma, 0.01f, 10f);

            for (int i = 0; i < elev.Length; i++)
            {
                if (landMask01[i] < landMaskThreshold01) continue;

                float t = (elev[i] - landMin) / range;
                t = Mathf.Clamp01(t);

                // smoothstep
                float s = t * t * (3f - 2f * t);
                float u = Mathf.Lerp(t, s, k);

                if (Mathf.Abs(gamma - 1f) > 1e-4f)
                    u = Mathf.Pow(u, gamma);

                elev[i] = landMin + u * range;
            }
        }

        // Approximate percentile by sampling (fast, deterministic).
        private static float EstimatePercentile(float[] a, int n, float p, int seed)
        {
            p = Mathf.Clamp01(p);
            int sampleN = Mathf.Min(n, 200000);
            float[] samp = new float[sampleN];
            var rng = new System.Random(seed);

            for (int i = 0; i < sampleN; i++)
            {
                int idx = rng.Next(0, n);
                samp[i] = a[idx];
            }
            Array.Sort(samp);
            int k = Mathf.Clamp(Mathf.RoundToInt(p * (sampleN - 1)), 0, sampleN - 1);
            return samp[k];
        }

        private static void DownsampleBilinear(float[] src, int sw, int sh, float[] dst, int dw, int dh)
        {
            for (int y = 0; y < dh; y++)
            for (int x = 0; x < dw; x++)
            {
                float u = (x + 0.5f) / dw;
                float v = (y + 0.5f) / dh;

                float sx = u * sw - 0.5f;
                float sy = v * sh - 0.5f;
                int x0 = Mathf.Clamp((int)Mathf.Floor(sx), 0, sw - 1);
                int y0 = Mathf.Clamp((int)Mathf.Floor(sy), 0, sh - 1);
                int x1 = Mathf.Clamp(x0 + 1, 0, sw - 1);
                int y1 = Mathf.Clamp(y0 + 1, 0, sh - 1);
                float tx = Mathf.Clamp01(sx - x0);
                float ty = Mathf.Clamp01(sy - y0);

                float a = src[y0 * sw + x0];
                float b = src[y0 * sw + x1];
                float c = src[y1 * sw + x0];
                float d = src[y1 * sw + x1];

                float ab = Mathf.Lerp(a, b, tx);
                float cd = Mathf.Lerp(c, d, tx);
                dst[y * dw + x] = Mathf.Lerp(ab, cd, ty);
            }
        }

        private static float SampleBilinear(float[] src, int sw, int sh, float u, float v)
        {
            u = Mathf.Clamp01(u);
            v = Mathf.Clamp01(v);

            float sx = u * sw - 0.5f;
            float sy = v * sh - 0.5f;
            int x0 = Mathf.Clamp((int)Mathf.Floor(sx), 0, sw - 1);
            int y0 = Mathf.Clamp((int)Mathf.Floor(sy), 0, sh - 1);
            int x1 = Mathf.Clamp(x0 + 1, 0, sw - 1);
            int y1 = Mathf.Clamp(y0 + 1, 0, sh - 1);
            float tx = Mathf.Clamp01(sx - x0);
            float ty = Mathf.Clamp01(sy - y0);

            float a = src[y0 * sw + x0];
            float b = src[y0 * sw + x1];
            float c = src[y1 * sw + x0];
            float d = src[y1 * sw + x1];

            float ab = Mathf.Lerp(a, b, tx);
            float cd = Mathf.Lerp(c, d, tx);
            return Mathf.Lerp(ab, cd, ty);
        }

        private static void BoxBlurSeparableInPlace(float[] a, int w, int h, int r)
        {
            if (r <= 0) return;

            float[] tmp = new float[a.Length];
            float inv = 1f / (2 * r + 1);

            // horizontal
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                float sum = 0f;

                // initial window
                for (int ix = -r; ix <= r; ix++)
                {
                    int x = Mathf.Clamp(ix, 0, w - 1);
                    sum += a[row + x];
                }
                tmp[row + 0] = sum * inv;

                for (int x = 1; x < w; x++)
                {
                    int add = Mathf.Clamp(x + r, 0, w - 1);
                    int sub = Mathf.Clamp(x - r - 1, 0, w - 1);
                    sum += a[row + add] - a[row + sub];
                    tmp[row + x] = sum * inv;
                }
            }

            // vertical
            for (int x = 0; x < w; x++)
            {
                float sum = 0f;
                for (int iy = -r; iy <= r; iy++)
                {
                    int y = Mathf.Clamp(iy, 0, h - 1);
                    sum += tmp[y * w + x];
                }
                a[0 * w + x] = sum * inv;

                for (int y = 1; y < h; y++)
                {
                    int add = Mathf.Clamp(y + r, 0, h - 1);
                    int sub = Mathf.Clamp(y - r - 1, 0, h - 1);
                    sum += tmp[add * w + x] - tmp[sub * w + x];
                    a[y * w + x] = sum * inv;
                }
            }
        }


        private static void ComputeRuggednessProxy(WorldArrays world, int w, int h)
        {
            float[] e = world.ElevationRaw;
            float[] r = world.Ruggedness01;

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                int xm = Mathf.Max(0, x - 1);
                int xp = Mathf.Min(w - 1, x + 1);
                int ym = Mathf.Max(0, y - 1);
                int yp = Mathf.Min(h - 1, y + 1);

                float dx = e[y * w + xp] - e[y * w + xm];
                float dy = e[yp * w + x] - e[ym * w + x];
                float g = Mathf.Sqrt(dx * dx + dy * dy);

                float val = Mathf.Clamp01(g * 0.6f + world.Uplift01[idx] * 0.7f);
                r[idx] = val * Mathf.SmoothStep(0.05f, 0.85f, world.LandMask01[idx]);
            }
        }

        // =====================================================================================
        // Quantile helper
        // =====================================================================================

        private static float ComputeQuantile(float[] values, float percentile)
        {
            percentile = Mathf.Clamp01(percentile);
            float[] sorted = new float[values.Length];
            Array.Copy(values, sorted, values.Length);
            Array.Sort(sorted);
            if (percentile <= 0f) return sorted[0];
            if (percentile >= 1f) return sorted[sorted.Length - 1];
            int idx = Mathf.Clamp((int)(percentile * sorted.Length), 0, sorted.Length - 1);
            return sorted[idx];
        }
    }
}
