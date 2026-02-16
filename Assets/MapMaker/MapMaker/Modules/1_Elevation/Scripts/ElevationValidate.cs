using UnityEngine;
using MapMaker.Shared.Data;
using MapMaker.Modules.Elevation1.Config;
using MapMaker.Core.Logging;

namespace MapMaker.Modules.Elevation1.Scripts
{
    /// <summary>
    /// Lightweight validation for Step 1 elevation scaffolding.
    /// </summary>
    public static class ElevationValidate
    {
        public static void Validate(HB_ElevationConfig cfg, LogEmitter emit)
        {
            if (cfg == null) return;

            float water = Mathf.Clamp01(cfg.OceanPercent);
            float land = 1f - water;

            if (water < 0.05f || water > 0.40f)
            {
                emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "ELEV_CFG",
                    $"OceanPercent={water:P0} implies Land={land:P0}. This is unusual for a global world map.");
            }

            if (cfg.EdgeOceanWidthMiles <= 0.001f)
            {
                emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "ELEV_CFG",
                    "EdgeOceanWidthMiles is 0. Land may touch edges. This is fine early, but some later modules may assume edge-ocean.");
            }
        }

        public static void LogMountainOceanAdjacency(ElevationBandFinal[] bands, int width, int height, LogEmitter emit)
        {
            if (bands == null) return;
            int n = width * height;
            int count = 0;

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;
                var b = bands[idx];
                if (b != ElevationBandFinal.LowMountains && b != ElevationBandFinal.HighMountains) continue;

                bool adjacentOcean = false;
                // 4-neighborhood
                if (x > 0 && (bands[idx - 1] == ElevationBandFinal.Ocean || bands[idx - 1] == ElevationBandFinal.DeepOcean)) adjacentOcean = true;
                else if (x < width - 1 && (bands[idx + 1] == ElevationBandFinal.Ocean || bands[idx + 1] == ElevationBandFinal.DeepOcean)) adjacentOcean = true;
                else if (y > 0 && (bands[idx - width] == ElevationBandFinal.Ocean || bands[idx - width] == ElevationBandFinal.DeepOcean)) adjacentOcean = true;
                else if (y < height - 1 && (bands[idx + width] == ElevationBandFinal.Ocean || bands[idx + width] == ElevationBandFinal.DeepOcean)) adjacentOcean = true;

                if (adjacentOcean) count++;
            }

            emit(LogLevel.INFO, LogContext.Module, LogPhase.Validation, "ELEV_ADJ",
                $"Mountain tiles adjacent to ocean: {count}");
        }
    }
}
