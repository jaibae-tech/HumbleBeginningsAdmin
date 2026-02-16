using System;
using System.Collections.Generic;
using MapMaker.Core.Logging;
using MapMaker.Shared.Data;
using MapMaker.Modules.Elevation1.Config;
using UnityEngine;

namespace MapMaker.Modules.Elevation
{
    /// <summary>
    /// Module 1 - Step 6 (Plan): Basin embedding.
    ///
    /// Purpose:
    /// - Add broad, shallow interior depressions and gentle rims to serve as later hydrology scaffolding.
    /// - This is NOT water classification. Inland seas are still removed later by the ocean-connect pass.
    ///
    /// Notes:
    /// - Applied after macro composition + relief coherence + conditioning so basins remain readable.
    /// - Basins are constrained to land (LandMask01) and tapered near coasts.
    /// </summary>
    public static class ElevationBasinEmbeddingPass
    {
        public static void Apply(HB_ElevationConfig cfg, WorldArrays world, int w, int h, int seed, LogEmitter log, LogContext ctx = LogContext.Module)
        {
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (world.ElevationRaw == null || world.ElevationRaw.Length != w * h)
                throw new InvalidOperationException("ElevationBasinEmbeddingPass requires world.ElevationRaw to be allocated.");
            if (world.LandMask01 == null || world.LandMask01.Length != w * h)
            {
                // Without LandMask we cannot safely avoid oceans; fail soft.
                return;
            }

            int n = w * h;

            float tileMiles = Mathf.Max(1e-6f, cfg.TileSizeMiles);
            float worldMilesX = w * tileMiles;
            float worldMilesY = h * tileMiles;
            float areaMmi2 = (worldMilesX * worldMilesY) / 1000000f;
            int basinCount = Mathf.Max(0, Mathf.RoundToInt(cfg.BasinDensity * areaMmi2));
            if (basinCount <= 0 || cfg.BasinStrength <= 1e-5f)
                return;

            // We deliberately scale down the raw config strengths here.
            // Rationale: these are subtle scaffolds, not dominant macro features.
            // If you want stronger basins, increase cfg.BasinStrength / cfg.BasinRimStrength.
            float depressionAmp = cfg.BasinStrength * 0.25f;
            float rimAmp = cfg.BasinRimStrength * 0.25f;

            float basinScaleMiles = Mathf.Max(20f, cfg.BasinScaleMiles);

            log?.Invoke(LogLevel.INFO, ctx, LogPhase.Progress, "ELEV_BASIN_EMBED",
                $"Basin embedding: count={basinCount}, scale={basinScaleMiles:0}mi, amp={depressionAmp:0.000}/{rimAmp:0.000}");

            // Pick centers (on land) deterministically.
            var rng = new System.Random(seed ^ 0x6D3A21); // stable but different from other streams

            var centers = new List<Vector2>(basinCount);
            int attemptsMax = Mathf.Max(64, basinCount * 64);
            for (int a = 0; a < attemptsMax && centers.Count < basinCount; a++)
            {
                int x = rng.Next(0, w);
                int y = rng.Next(0, h);
                int idx = y * w + x;

                // Prefer interior land (avoid the coast band).
                float land = world.LandMask01[idx];
                if (land < 0.60f) continue;

                // Keep some separation (approx).
                bool ok = true;
                Vector2 p = new Vector2(x * tileMiles, y * tileMiles);
                for (int i = 0; i < centers.Count; i++)
                {
                    if (Vector2.Distance(p, centers[i]) < basinScaleMiles * 0.75f) { ok = false; break; }
                }
                if (!ok) continue;

                centers.Add(p);
            }

            if (centers.Count == 0)
            {
                log?.Invoke(LogLevel.WARN, ctx, LogPhase.Progress, "ELEV_BASIN_EMBED", "No valid basin centers found on land; skipping.");
                return;
            }

            // Apply a smooth radial depression + rim per center.
            // Use an elliptical distance (world aspect) so basins look consistent.
            float sx = Mathf.Max(1e-6f, basinScaleMiles);
            float sy = Mathf.Max(1e-6f, basinScaleMiles * (worldMilesY / worldMilesX));

            float maxDeltaAbs = 0f;

            for (int y = 0; y < h; y++)
            {
                float py = y * tileMiles;
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;

                    float land = world.LandMask01[idx];
                    if (land <= 0.50f) continue; // ocean / shelf

                    float px = x * tileMiles;

                    // Taper near coasts so basins stay interior.
                    float coastTaper = Mathf.Clamp01((land - 0.50f) / 0.20f); // 0 at coast band, 1 inland

                    float v = 0f;
                    for (int i = 0; i < centers.Count; i++)
                    {
                        Vector2 c = centers[i];
                        float dx = (px - c.x) / sx;
                        float dy = (py - c.y) / sy;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);

                        // Depression: smoothstep-like bump centered at r=0.
                        float dep = Mathf.Exp(-r * r * 0.9f); // 0..1

                        // Rim: ring around r~1.
                        float rim = Mathf.Exp(-(r - 1.0f) * (r - 1.0f) * 6.0f);

                        v += (-depressionAmp * dep) + (rimAmp * rim);
                    }

                    v *= coastTaper;

                    world.ElevationRaw[idx] += v;
                    float av = Mathf.Abs(v);
                    if (av > maxDeltaAbs) maxDeltaAbs = av;
                }
            }

            log?.Invoke(LogLevel.INFO, ctx, LogPhase.Progress, "ELEV_BASIN_EMBED", $"Basin embedding applied (maxDelta~{maxDeltaAbs:0.000}).");
        }
    }
}
