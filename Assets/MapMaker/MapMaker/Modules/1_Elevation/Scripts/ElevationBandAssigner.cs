using System;
using System.Linq;
using UnityEngine;
using MapMaker.Shared.Data;
using MapMaker.Modules.Elevation1.Config;
using MapMaker.Core.Logging;

namespace MapMaker.Modules.Elevation1.Scripts
{
    /// <summary>
    /// Assigns elevation bands using pure quantiles.
    /// NO overrides, NO thresholds - just respect the configured percentages exactly.
    /// </summary>
    public class ElevationBandAssigner
    {
        private readonly int _width;
        private readonly int _height;
        private readonly HB_ElevationConfig _cfg;
        private readonly LogEmitter _emit;

        public ElevationBandAssigner(
            int width, 
            int height,
            HB_ElevationConfig cfg,
            LogEmitter emit)
        {
            _width = width;
            _height = height;
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
            _emit = emit ?? throw new ArgumentNullException(nameof(emit));
        }

        public void Execute(float[] elevationRaw, ElevationBandFinal[] elevationBands)
        {
            if (elevationRaw == null) throw new ArgumentNullException(nameof(elevationRaw));
            if (elevationBands == null) throw new ArgumentNullException(nameof(elevationBands));

            int totalTiles = _width * _height;

            // Sort all elevation values to find quantiles
            float[] sorted = new float[totalTiles];
            Array.Copy(elevationRaw, sorted, totalTiles);
            Array.Sort(sorted);

            // Calculate quantile thresholds based on configured percentages
            // Ocean percentages are cumulative
            float deepOceanPercent = _cfg.OceanTotalPercent * _cfg.DeepOceanShareWithinOcean;
            float oceanPercent = _cfg.OceanTotalPercent;

            // Land percentages start after ocean
            float lowlandPercent = oceanPercent + _cfg.LowlandPercent;
            float highlandsPercent = lowlandPercent + _cfg.HighlandsPercent;
            float lowMtnPercent = highlandsPercent + _cfg.LowMountainsPercent;
            // Remaining is high mountains

            // Find threshold elevations at each quantile
            float tDeepOcean = GetQuantile(sorted, deepOceanPercent);
            float tOcean = GetQuantile(sorted, oceanPercent);
            float tLowland = GetQuantile(sorted, lowlandPercent);
            float tHighlands = GetQuantile(sorted, highlandsPercent);
            float tLowMtn = GetQuantile(sorted, lowMtnPercent);

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Assigned, "ELEVATION_THRESHOLDS",
                $"Quantiles: DeepOcean<={tDeepOcean:F4}, Ocean<={tOcean:F4}, Lowland<={tLowland:F4}, " +
                $"Highlands<={tHighlands:F4}, LowMtn<={tLowMtn:F4}");

            // Assign bands
            for (int i = 0; i < totalTiles; i++)
            {
                float elev = elevationRaw[i];

                if (elev <= tDeepOcean)
                    elevationBands[i] = ElevationBandFinal.DeepOcean;
                else if (elev <= tOcean)
                    elevationBands[i] = ElevationBandFinal.Ocean;
                else if (elev <= tLowland)
                    elevationBands[i] = ElevationBandFinal.Lowland;
                else if (elev <= tHighlands)
                    elevationBands[i] = ElevationBandFinal.Highlands;
                else if (elev <= tLowMtn)
                    elevationBands[i] = ElevationBandFinal.LowMountains;
                else
                    elevationBands[i] = ElevationBandFinal.HighMountains;
            }

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Assigned, "ELEVATION_BANDS",
                $"Bands assigned. OceanTotal={_cfg.OceanTotalPercent:P1} DeepShare={_cfg.DeepOceanShareWithinOcean:P1}.");
        }

        private float GetQuantile(float[] sortedValues, float percentile)
        {
            if (percentile <= 0f) return sortedValues[0];
            if (percentile >= 1f) return sortedValues[sortedValues.Length - 1];

            int index = (int)(percentile * sortedValues.Length);
            index = Mathf.Clamp(index, 0, sortedValues.Length - 1);
            
            return sortedValues[index];
        }
    }
}
