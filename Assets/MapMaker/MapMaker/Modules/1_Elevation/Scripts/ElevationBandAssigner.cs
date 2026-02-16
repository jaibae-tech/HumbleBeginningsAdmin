using System;
using UnityEngine;
using MapMaker.Shared.Data;
using MapMaker.Modules.Elevation1.Config;
using MapMaker.Core.Logging;

namespace MapMaker.Modules.Elevation1.Scripts
{
    /// <summary>
    /// Assigns 6 elevation bands for preview / downstream steps.
    /// This is classification only (NOT geology generation).
    ///
    /// Water/land split is driven by OceanPercent (sea level by percentile).
    /// Remaining splits are within water and within land.
    /// </summary>
    public class ElevationBandAssigner
    {
        private readonly int _width;
        private readonly int _height;
        private readonly HB_ElevationConfig _cfg;
        private readonly LogEmitter _emit;

        public ElevationBandAssigner(int width, int height, HB_ElevationConfig cfg, LogEmitter emit)
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

            int total = _width * _height;

            float waterPercent = Mathf.Clamp01(_cfg.OceanPercent);
            float targetLand = 1f - waterPercent;

            // Optional Step 4 (Module 1): ensure there are no inland seas after elevation generation.
            // We do this BEFORE final banding by iterating: compute sea threshold by percentile, lift any
            // below-sea tiles not connected to map edge, then recompute threshold.
            if (_cfg.NoInlandWaterAfterElevation && waterPercent > 0f)
            {
                const int maxIters = 3;
                for (int iter = 0; iter < maxIters; iter++)
                {
                    float[] tmp = new float[total];
                    Array.Copy(elevationRaw, tmp, total);
                    Array.Sort(tmp);

                    float tOceanIter = GetQuantile(tmp, waterPercent);
                    int lifted = LiftInlandSeas(elevationRaw, _width, _height, tOceanIter, 1e-4f);

                    if (lifted <= 0)
                    {
                        break;
                    }

                    _emit(LogLevel.INFO, LogContext.Module, LogPhase.Assigned, "ELEV_OCEAN_CONNECT",
                        $"Removed inland seas: lifted {lifted} tiles above sea (iter {iter + 1}/{maxIters}).");
                }
            }

            // Sort elevations for quantiles (final)
            float[] sorted = new float[total];
            Array.Copy(elevationRaw, sorted, total);
            Array.Sort(sorted);

            float deepWaterPercent = waterPercent * Mathf.Clamp01(_cfg.DeepOceanShareWithinOcean);

            // Land splits (within land)
            float landHighMtn = Mathf.Clamp(_cfg.HighMountainsPercentOfLand, 0.0f, 0.49f);
            float landLowMtn = Mathf.Clamp(_cfg.LowMountainsPercentOfLand, 0.0f, 0.80f);
            float landHighlands = Mathf.Clamp(_cfg.HighlandsPercentOfLand, 0.0f, 0.90f);

            // Convert within-land shares into cumulative percent of whole map
            float tDeep = GetQuantile(sorted, deepWaterPercent);
            float tOcean = GetQuantile(sorted, waterPercent);

            // Within land: lowland is remainder after highlands + lowmtn + highmtn
            float landLowlandShare = Mathf.Clamp01(1f - (landHighlands + landLowMtn + landHighMtn));
            // Cumulatives within land
            float tLowland = GetQuantile(sorted, waterPercent + landLowlandShare * targetLand);
            float tHighlands = GetQuantile(sorted, waterPercent + (landLowlandShare + landHighlands) * targetLand);
            float tLowMtn = GetQuantile(sorted, waterPercent + (landLowlandShare + landHighlands + landLowMtn) * targetLand);

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Assigned, "ELEVATION_THRESHOLDS",
                $"Quantiles: DeepOcean<={tDeep:F4}, Ocean<={tOcean:F4}, Lowland<={tLowland:F4}, " +
                $"Highlands<={tHighlands:F4}, LowMtn<={tLowMtn:F4}");

            for (int i = 0; i < total; i++)
            {
                float e = elevationRaw[i];
                if (e <= tDeep) elevationBands[i] = ElevationBandFinal.DeepOcean;
                else if (e <= tOcean) elevationBands[i] = ElevationBandFinal.Ocean;
                else if (e <= tLowland) elevationBands[i] = ElevationBandFinal.Lowland;
                else if (e <= tHighlands) elevationBands[i] = ElevationBandFinal.Highlands;
                else if (e <= tLowMtn) elevationBands[i] = ElevationBandFinal.LowMountains;
                else elevationBands[i] = ElevationBandFinal.HighMountains;
            }

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Assigned, "ELEVATION_BANDS",
                $"Bands assigned (Land={targetLand:P0}, Ocean={waterPercent:P0}, DeepOceanShare={_cfg.DeepOceanShareWithinOcean:P0}).");
        }

        /// <summary>
        /// Flood-fills "ocean" from the map border and lifts any below-sea tiles not connected to the border.
        /// Returns number of tiles lifted.
        /// </summary>
        private static int LiftInlandSeas(float[] elevationRaw, int width, int height, float seaThreshold, float eps)
        {
            int total = width * height;
            bool[] visited = new bool[total];
            int[] queue = new int[total];
            int qh = 0, qt = 0;

            // Seed queue with edge tiles that are below sea.
            void EnqueueIfOcean(int x, int y)
            {
                int idx = y * width + x;
                if (visited[idx]) return;
                if (elevationRaw[idx] > seaThreshold) return;
                visited[idx] = true;
                queue[qt++] = idx;
            }

            for (int x = 0; x < width; x++)
            {
                EnqueueIfOcean(x, 0);
                EnqueueIfOcean(x, height - 1);
            }
            for (int y = 0; y < height; y++)
            {
                EnqueueIfOcean(0, y);
                EnqueueIfOcean(width - 1, y);
            }

            // BFS 4-neighborhood.
            while (qh < qt)
            {
                int idx = queue[qh++];
                int x = idx % width;
                int y = idx / width;

                if (x > 0) EnqueueIfOcean(x - 1, y);
                if (x < width - 1) EnqueueIfOcean(x + 1, y);
                if (y > 0) EnqueueIfOcean(x, y - 1);
                if (y < height - 1) EnqueueIfOcean(x, y + 1);
            }

            // Any below-sea tile not visited is an inland sea tile.
            int lifted = 0;
            float liftTo = seaThreshold + Mathf.Max(1e-6f, eps);
            for (int i = 0; i < total; i++)
            {
                if (elevationRaw[i] <= seaThreshold && !visited[i])
                {
                    elevationRaw[i] = liftTo;
                    lifted++;
                }
            }
            return lifted;
        }

        private static float GetQuantile(float[] sorted, float percentile)
        {
            percentile = Mathf.Clamp01(percentile);
            if (percentile <= 0f) return sorted[0];
            if (percentile >= 1f) return sorted[sorted.Length - 1];
            int idx = Mathf.Clamp((int)(percentile * sorted.Length), 0, sorted.Length - 1);
            return sorted[idx];
        }
    }
}
