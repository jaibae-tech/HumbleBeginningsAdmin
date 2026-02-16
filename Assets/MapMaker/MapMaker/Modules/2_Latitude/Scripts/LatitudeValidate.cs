using System;
using MapMaker.Core.Logging;
using MapMaker.Modules.Latitude2.Config;
using MapMaker.Shared.Data;

namespace MapMaker.Modules.Latitude2.Scripts
{
    public static class LatitudeValidate
    {
        public static void Validate(HB_LatitudeConfig cfg, LogEmitter emit)
        {
            if (cfg == null)
            {
                emit(LogLevel.ERROR, LogContext.Module, LogPhase.Validation, "LATITUDE_CFG_NULL",
                    "HB_LatitudeConfig is null");
                return;
            }

            if (cfg.LatitudeMin01 < 0f || cfg.LatitudeMin01 > 1f || cfg.LatitudeMax01 < 0f || cfg.LatitudeMax01 > 1f)
            {
                emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "LATITUDE_RANGE",
                    $"LatitudeMin01/LatitudeMax01 should be within 0..1 (min={cfg.LatitudeMin01:F3}, max={cfg.LatitudeMax01:F3})");
            }

            if (cfg.LatitudeMax01 <= cfg.LatitudeMin01)
            {
                emit(LogLevel.ERROR, LogContext.Module, LogPhase.Validation, "LATITUDE_MINMAX",
                    $"LatitudeMax01 must be > LatitudeMin01 (min={cfg.LatitudeMin01:F3}, max={cfg.LatitudeMax01:F3})");
            }

            if (cfg.EnableGlobalWarp && cfg.WarpAmplitude > 0.05f)
            {
                emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "LATITUDE_WARP_AMPLITUDE",
                    $"WarpAmplitude should be <= 0.05 to avoid fragmentation (got {cfg.WarpAmplitude:F3})");
            }

            if (cfg.SeasonAmpMax01 < cfg.SeasonAmpMin01)
            {
                emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "LATITUDE_SEASON_RANGE",
                    $"SeasonAmpMax01 < SeasonAmpMin01 (min={cfg.SeasonAmpMin01:F3}, max={cfg.SeasonAmpMax01:F3})");
            }
        }

        public static void LogLatitudeStats(WorldArrays world, LogEmitter emit)
        {
            if (world == null || world.LatitudeEnergy01 == null || world.LatitudeEnergy01.Length == 0) return;

            float min = float.PositiveInfinity;
            float max = float.NegativeInfinity;
            double sum = 0;
            int n = world.LatitudeEnergy01.Length;

            for (int i = 0; i < n; i++)
            {
                float v = world.LatitudeEnergy01[i];
                if (v < min) min = v;
                if (v > max) max = v;
                sum += v;
            }

            float mean = (float)(sum / n);

            // Simple monotonic sanity check: average south row should be warmer than average north row.
            float south = 0f;
            float north = 0f;
            int w = world.Width;
            int h = world.Height;
            if (w > 0 && h > 0)
            {
                int ySouth = 0;
                int yNorth = h - 1;
                for (int x = 0; x < w; x++)
                {
                    south += world.LatitudeEnergy01[(ySouth * w) + x];
                    north += world.LatitudeEnergy01[(yNorth * w) + x];
                }
                south /= Math.Max(1, w);
                north /= Math.Max(1, w);
            }

            emit(LogLevel.INFO, LogContext.Module, LogPhase.Validation, "LATITUDE_STATS",
                $"LatitudeEnergy01 min={min:F3} mean={mean:F3} max={max:F3} | southRow={south:F3} northRow={north:F3}");

            if (south <= north)
            {
                emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "LATITUDE_MONOTONIC",
                    "LatitudeEnergy01 appears non-monotonic (south row not warmer than north row). Check module settings or coordinate conventions.");
            }
        }
    }
}
