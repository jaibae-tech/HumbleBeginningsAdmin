using System;
using MapMaker.Core.Logging;
using MapMaker.Modules.Latitude2.Config;
using MapMaker.Shared.Data;

namespace MapMaker.Modules.Latitude2.Scripts
{
    public static class LatitudeValidate
    {
        public static void Validate(HB_LatitudeConfig cfg, bool useFiveBands, LogEmitter emit)
        {
            if (cfg == null)
            {
                emit(LogLevel.ERROR, LogContext.Module, LogPhase.Validation, "LATITUDE_CFG_NULL",
                    "HB_LatitudeConfig is null");
                return;
            }

            if (useFiveBands)
            {
                float sum = cfg.FiveBandSum();
                if (Math.Abs(sum - 1f) > 0.05f)
                {
                    emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "LATITUDE_5BAND_SUM",
                        $"5-band percentages sum to {sum:F3}, expected 1.0. System will normalize at runtime.");
                }
            }
            else
            {
                float sum = cfg.ThreeBandSum();
                if (Math.Abs(sum - 1f) > 0.05f)
                {
                    emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "LATITUDE_3BAND_SUM",
                        $"3-band percentages sum to {sum:F3}, expected 1.0. System will normalize at runtime.");
                }
            }

            if (cfg.BandWarpNoiseScale <= 0f)
            {
                emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "LATITUDE_WARP_SCALE",
                    $"BandWarpNoiseScale must be positive, got {cfg.BandWarpNoiseScale}");
            }

            if (cfg.BandWarpStrength < 0f || cfg.BandWarpStrength > 0.2f)
            {
                emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "LATITUDE_WARP_STRENGTH",
                    $"BandWarpStrength should be 0-0.2, got {cfg.BandWarpStrength}");
            }
        }

        public static void LogBandDistribution(WorldArrays world, LogEmitter emit)
        {
            if (world == null || world.LatitudeBands == null) return;

            int arcticCount = 0;
            int temperateCount = 0;
            int tropicalCount = 0;

            for (int i = 0; i < world.LatitudeBands.Length; i++)
            {
                switch (world.LatitudeBands[i])
                {
                    case LatitudeBandType.Arctic:
                        arcticCount++;
                        break;
                    case LatitudeBandType.Temperate:
                        temperateCount++;
                        break;
                    case LatitudeBandType.Tropical:
                        tropicalCount++;
                        break;
                }
            }

            int total = world.LatitudeBands.Length;
            float arcticPct = (float)arcticCount / total;
            float temperatePct = (float)temperateCount / total;
            float tropicalPct = (float)tropicalCount / total;

            emit(LogLevel.INFO, LogContext.Module, LogPhase.Validation, "LATITUDE_DISTRIBUTION",
                $"Arctic: {arcticPct:P1} ({arcticCount}/{total}), " +
                $"Temperate: {temperatePct:P1} ({temperateCount}/{total}), " +
                $"Tropical: {tropicalPct:P1} ({tropicalCount}/{total})");
        }
    }
}
