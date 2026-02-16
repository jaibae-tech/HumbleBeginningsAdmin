using System;
using MapMaker.Core.Logging;
using MapMaker.Modules.Elevation1.Config;
using MapMaker.Shared.Data;
using UnityEngine;

namespace MapMaker.Modules.Elevation
{
    /// <summary>
    /// Module 1 - Step 8: Final Elevation Preparation
    ///
    /// Performs stability checks and a lightweight final normalization step
    /// (clamp percentile + optional remap to 0..1) without smoothing.
    /// </summary>
    public static class ElevationFinalPreparationPass
    {
        public static void Apply(HB_ElevationConfig cfg, WorldArrays world, int w, int h, LogEmitter log,
            LogContext ctx = LogContext.Module)
        {
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (world.ElevationRaw == null || world.ElevationRaw.Length == 0) return;
            if (!cfg.FinalPrepEnabled) return;

            int n = w * h;
            float[] e = world.ElevationRaw;

            // Stability check: NaNs / infinities.
            int bad = 0;
            float min = float.PositiveInfinity;
            float max = float.NegativeInfinity;
            for (int i = 0; i < n; i++)
            {
                float v = e[i];
                if (float.IsNaN(v) || float.IsInfinity(v))
                {
                    bad++;
                    e[i] = 0f;
                    v = 0f;
                }
                if (v < min) min = v;
                if (v > max) max = v;
            }

            if (bad > 0)
                log?.Invoke(LogLevel.WARN, ctx, LogPhase.Progress, "ELEV_FINAL",
                    $"Final prep: replaced {bad} invalid elevation samples (NaN/Inf) with 0.");

            // Clamp extremes by percentile (reuses the conditioning clamp knob, but no smoothing).
            float clampP = Mathf.Clamp01(cfg.ConditioningClampPercent);
            if (clampP > 0.0001f)
            {
                int step = 4;
                int sampleCount = ((w + step - 1) / step) * ((h + step - 1) / step);
                float[] samples = new float[sampleCount];
                int s = 0;
                for (int y = 0; y < h; y += step)
                {
                    int row = y * w;
                    for (int x = 0; x < w; x += step)
                        samples[s++] = e[row + x];
                }
                Array.Sort(samples);

                float lo = PercentileSorted(samples, clampP);
                float hi = PercentileSorted(samples, 1f - clampP);
                if (hi <= lo + 1e-6f) { lo = min; hi = max; }

                for (int i = 0; i < n; i++)
                    e[i] = Mathf.Clamp(e[i], lo, hi);

                min = lo;
                max = hi;
            }

            // Optional remap to 0..1.
            if (cfg.FinalPrepRemap01)
            {
                float denom = max - min;
                if (denom <= 1e-6f) denom = 1f;
                float inv = 1f / denom;
                for (int i = 0; i < n; i++)
                    e[i] = (e[i] - min) * inv;
                min = 0f;
                max = 1f;
            }

            log?.Invoke(LogLevel.INFO, ctx, LogPhase.Progress, "ELEV_FINAL",
                $"Final prep complete: clampP={cfg.ConditioningClampPercent * 100f:F1}%, remap={cfg.FinalPrepRemap01}, range={min:F3}..{max:F3}");
        }

        private static float PercentileSorted(float[] sorted, float p)
        {
            if (sorted == null || sorted.Length == 0) return 0f;
            p = Mathf.Clamp01(p);
            float idx = p * (sorted.Length - 1);
            int i0 = Mathf.FloorToInt(idx);
            int i1 = Mathf.Min(i0 + 1, sorted.Length - 1);
            float t = idx - i0;
            return Mathf.Lerp(sorted[i0], sorted[i1], t);
        }
    }
}
