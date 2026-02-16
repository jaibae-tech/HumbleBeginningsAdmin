using System;
using MapMaker.Core.Logging;
using MapMaker.Modules.Elevation1.Config;
using MapMaker.Shared.Data;
using UnityEngine;

namespace MapMaker.Modules.Elevation
{
    /// <summary>
    /// Module 1 - Step 7: Micro-relief
    ///
    /// Adds subtle, small-scale elevation variation on land without altering macro geography.
    /// This is intentionally low amplitude and masked to land.
    ///
    /// Runs after macro composition + basin embedding.
    /// Followed by Final Elevation Preparation (Step 8) to keep ranges stable.
    /// </summary>
    public static class ElevationMicroReliefPass
    {
        public static void Apply(HB_ElevationConfig cfg, WorldArrays world, int w, int h, int seed, LogEmitter log,
            LogContext ctx = LogContext.Module)
        {
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (world.ElevationRaw == null || world.ElevationRaw.Length == 0) return;
            if (!cfg.MicroReliefEnabled) return;

            float tileMiles = Mathf.Max(0.0001f, cfg.TileSizeMiles);
            float scaleMiles = Mathf.Max(1f, cfg.MicroReliefScaleMiles);
            float freq = 1f / scaleMiles;
            float amp = Mathf.Max(0f, cfg.MicroReliefHeight) * Mathf.Clamp01(cfg.MicroReliefStrength);
            if (amp <= 0.000001f) return;

            bool hasLandMask = world.LandMask01 != null && world.LandMask01.Length == (w * h);

            // Deterministic offsets from seed.
            float ox1 = (seed * 0.000123f) % 1000f;
            float oy1 = (seed * 0.000917f) % 1000f;
            float ox2 = (seed * 0.001731f) % 1000f;
            float oy2 = (seed * 0.000391f) % 1000f;

            log?.Invoke(LogLevel.INFO, ctx, LogPhase.Progress, "ELEV_MICRO",
                $"Micro-relief: scale={scaleMiles:F0}mi, amp={amp:F3}");

            int n = w * h;
            float[] e = world.ElevationRaw;

            // Two octave blend to avoid obvious Perlin artifacts.
            const float o2Scale = 0.45f;
            const float o2Weight = 0.55f;

            float maxDelta = 0f;
            for (int y = 0; y < h; y++)
            {
                float my = y * tileMiles;
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;

                    if (hasLandMask)
                    {
                        float lm = Mathf.Clamp01(world.LandMask01[idx]);
                        if (lm <= 0.001f) continue;
                    }

                    float mx = x * tileMiles;

                    float n1 = Mathf.PerlinNoise(ox1 + mx * freq, oy1 + my * freq) * 2f - 1f;
                    float n2 = Mathf.PerlinNoise(ox2 + mx * (freq / o2Scale), oy2 + my * (freq / o2Scale)) * 2f - 1f;
                    float nMix = (n1 * (1f - o2Weight)) + (n2 * o2Weight);

                    // Mildly compress extremes.
                    nMix = Mathf.Clamp(nMix, -0.9f, 0.9f);

                    float delta = nMix * amp;
                    e[idx] += delta;
                    float ad = Mathf.Abs(delta);
                    if (ad > maxDelta) maxDelta = ad;
                }
            }

            log?.Invoke(LogLevel.INFO, ctx, LogPhase.Progress, "ELEV_MICRO",
                $"Micro-relief applied (maxDelta~{maxDelta:F3}).");
        }
    }
}
