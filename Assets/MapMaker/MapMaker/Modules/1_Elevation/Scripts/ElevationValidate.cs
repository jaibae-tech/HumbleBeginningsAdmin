using System;
using MapMaker.Core.Logging;
using MapMaker.Modules.Elevation1.Config;
using MapMaker.Shared.Data;

namespace MapMaker.Modules.Elevation1.Scripts
{
    public static class ElevationValidate
    {
        public static void Validate(HB_ElevationConfig cfg, LogEmitter emit)
        {
            if (cfg == null)
            {
                emit(LogLevel.ERROR, LogContext.Module, LogPhase.Validation, "ELEV_VALIDATE", "HB_ElevationConfig is null");
                return;
            }

            float sum =
                cfg.OceanTotalPercent +
                cfg.LowlandPercent +
                cfg.HighlandsPercent +
                cfg.LowMountainsPercent +
                cfg.HighMountainsPercent;
                
            if (Math.Abs(sum - 1f) > 0.001f)
            {
                emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "ELEV_VALIDATE", 
                    $"Band percents sum to {sum:0.###} (expected 1.0). This will affect quantile calculation.");
            }
        }

        public static void LogMountainOceanAdjacency(ElevationBandFinal[] bands, int width, int height, LogEmitter emit)
        {
            int adjacent = CountMountainAdjacentToOcean(bands, width, height);
            emit(LogLevel.INFO, LogContext.Module, LogPhase.Validation, "ELEV_ADJ", $"Mountain tiles adjacent to ocean: {adjacent}");
        }

        private static int CountMountainAdjacentToOcean(ElevationBandFinal[] bands, int width, int height)
        {
            int count = 0;

            bool IsOcean(ElevationBandFinal b) => b == ElevationBandFinal.DeepOcean || b == ElevationBandFinal.Ocean;
            bool IsMountain(ElevationBandFinal b) => b == ElevationBandFinal.LowMountains || b == ElevationBandFinal.HighMountains;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    if (!IsMountain(bands[idx])) continue;

                    // Check 4-connected neighbors
                    if (x > 0 && IsOcean(bands[idx - 1])) { count++; continue; }
                    if (x < width - 1 && IsOcean(bands[idx + 1])) { count++; continue; }
                    if (y > 0 && IsOcean(bands[idx - width])) { count++; continue; }
                    if (y < height - 1 && IsOcean(bands[idx + width])) { count++; continue; }
                }
            }

            return count;
        }
    }
}
