using System;
using MapMaker.Core.Logging;
using MapMaker.Modules.Elevation1.Config;
using MapMaker.Shared.Data;
using UnityEngine;

namespace MapMaker.Modules.Elevation
{
    /// <summary>
    /// Module 1 - Step 5 (Plan): Relief Coherence Pass.
    ///
    /// Purpose:
    /// - Reduce harsh mountain-to-lowland edges by spreading uplift outward into foothills.
    /// - Preserve macro geography (does not touch ocean carving or landmask).
    ///
    /// Implementation:
    /// - Computes a smoothed version of Uplift01 using a cheap separable box blur.
    /// - Adds a small fraction of (smoothedUplift - uplift) into ElevationRaw on land.
    ///
    /// Notes:
    /// - Applied AFTER ComposeElevation and BEFORE conditioning/banding.
    /// - Controlled by HB_ElevationConfig.ReliefCoherence* fields.
    /// </summary>
    public static class ElevationReliefCoherencePass
    {
        public static void Apply(
        HB_ElevationConfig cfg,
        WorldArrays world,
        int w,
        int h,
        LogEmitter log,
        LogContext ctx = LogContext.Module)
        {
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (!cfg.ReliefCoherenceEnabled) return;

            int n = w * h;
            if (world.Uplift01 == null || world.Uplift01.Length != n) return; // nothing to spread
            if (world.ElevationRaw == null || world.ElevationRaw.Length != n) return;

            float strength = Mathf.Clamp01(cfg.ReliefCoherenceStrength);
            if (strength <= 1e-6f) return;

            float tileMiles = Mathf.Max(1e-3f, cfg.TileSizeMiles);
            int radius = Mathf.Clamp(Mathf.RoundToInt(cfg.ReliefCoherenceRadiusMiles / tileMiles), 1, Mathf.Max(w, h) / 2);

            log?.Invoke(LogLevel.INFO, LogContext.Module, LogPhase.Progress, "ELEV_RELIEF_COH", $"Relief coherence: spreading uplift (strength={strength:0.###}, radius={radius} tiles ~{radius * tileMiles:0.#}mi)");

            // Temporary buffers.
            var tmp = new float[n];
            var sm  = new float[n];

            // Horizontal blur (wrap disabled).
            BoxBlur1D(world.Uplift01, tmp, w, h, radius, horizontal: true);
            // Vertical blur.
            BoxBlur1D(tmp, sm, w, h, radius, horizontal: false);

            float[] land = world.LandMask01;
            bool hasLand = land != null && land.Length == n;

            // Apply as a small additive adjustment on land only.
            float[] elev = world.ElevationRaw;
            float maxDelta = 0f;
            for (int i = 0; i < n; i++)
            {
                float lm = hasLand ? Mathf.Clamp01(land[i]) : 1f;
                if (lm <= 0.001f) continue;

                float d = (sm[i] - world.Uplift01[i]) * strength;
                elev[i] += d * lm;
                float ad = Mathf.Abs(d);
                if (ad > maxDelta) maxDelta = ad;
            }

            log?.Invoke(LogLevel.INFO, LogContext.Module, LogPhase.Progress, "ELEV_RELIEF_COH", $"Relief coherence applied (maxDelta~{maxDelta:0.####}).");
        }

        private static void BoxBlur1D(float[] src, float[] dst, int w, int h, int r, bool horizontal)
        {
            int n = w * h;
            if (dst.Length != n) throw new ArgumentException("dst size mismatch");

            if (horizontal)
            {
                for (int y = 0; y < h; y++)
                {
                    int row = y * w;
                    float sum = 0f;
                    int count = 0;

                    // init window [0..r]
                    for (int x = 0; x <= r; x++)
                    {
                        sum += src[row + x];
                        count++;
                    }

                    for (int x = 0; x < w; x++)
                    {
                        // window is [x-r .. x+r]
                        int add = x + r;
                        int sub = x - r - 1;

                        if (add < w)
                        {
                            sum += src[row + add];
                            count++;
                        }
                        if (sub >= 0)
                        {
                            sum -= src[row + sub];
                            count--;
                        }

                        dst[row + x] = sum / Mathf.Max(1, count);
                    }
                }
            }
            else
            {
                for (int x = 0; x < w; x++)
                {
                    float sum = 0f;
                    int count = 0;

                    // init window [0..r]
                    for (int y = 0; y <= r; y++)
                    {
                        sum += src[y * w + x];
                        count++;
                    }

                    for (int y = 0; y < h; y++)
                    {
                        int add = y + r;
                        int sub = y - r - 1;

                        if (add < h)
                        {
                            sum += src[add * w + x];
                            count++;
                        }
                        if (sub >= 0)
                        {
                            sum -= src[sub * w + x];
                            count--;
                        }

                        dst[y * w + x] = sum / Mathf.Max(1, count);
                    }
                }
            }
        }
    }
}
