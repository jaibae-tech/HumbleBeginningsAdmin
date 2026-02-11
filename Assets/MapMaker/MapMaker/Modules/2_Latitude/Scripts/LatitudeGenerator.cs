using System;
using UnityEngine;
using MapMaker.Core.Logging;
using MapMaker.Modules.Latitude2.Config;
using MapMaker.Shared.Data;
using MapMaker.Shared.Utils;

namespace MapMaker.Modules.Latitude2.Scripts
{
    public sealed class LatitudeGenerator
    {
        private readonly HB_LatitudeConfig _cfg;
        private readonly int _heightThreshold;
        private readonly SeedContext _seed;
        private readonly LogEmitter _emit;

        public LatitudeGenerator(HB_LatitudeConfig cfg, int heightThreshold, SeedContext seed, LogEmitter emit)
        {
            _cfg = cfg;
            _heightThreshold = heightThreshold;
            _seed = seed;
            _emit = emit;
        }

        public void Execute(WorldArrays world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));

            int w = world.Width;
            int h = world.Height;

            bool useFiveBands = h >= _heightThreshold;
            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "LATITUDE_MODE",
                $"Using {(useFiveBands ? "5" : "3")}-band mode (height={h}, threshold={_heightThreshold})");

            float ox = (float)_seed.LatitudeRng.NextDouble() * 10000f;
            float oy = (float)_seed.LatitudeRng.NextDouble() * 10000f;
            float warpScale = Mathf.Max(0.0001f, _cfg.BandWarpNoiseScale);
            float warpStrength = Mathf.Clamp(_cfg.BandWarpStrength, 0f, 0.2f);

            if (useFiveBands)
            {
                Generate5Bands(world, w, h, ox, oy, warpScale, warpStrength);
            }
            else
            {
                Generate3Bands(world, w, h, ox, oy, warpScale, warpStrength);
            }

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "LATITUDE_COMPLETE",
                $"Latitude bands assigned for {w}x{h} map");
        }

        private void Generate3Bands(WorldArrays world, int w, int h, float ox, float oy, float warpScale, float warpStrength)
        {
            float sum = _cfg.ThreeBandSum();
            if (sum <= 0f)
            {
                _emit(LogLevel.WARN, LogContext.Module, LogPhase.Generation, "LATITUDE_SUM_ZERO",
                    "3-band percentages sum to 0. Using equal distribution.");
                sum = 1f;
            }

            float arcticNorm = _cfg.ThreeBandArcticPercent / sum;
            float temperateNorm = _cfg.ThreeBandTemperatePercent / sum;
            float tropicalNorm = _cfg.ThreeBandTropicalPercent / sum;

            float tropicalEnd = tropicalNorm;
            float temperateEnd = tropicalEnd + temperateNorm;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float normalizedY = (float)y / Mathf.Max(1f, h - 1);
                    float warp = Mathf.PerlinNoise((x + ox) * warpScale, (y + oy) * warpScale);
                    float warpOffset = (warp - 0.5f) * 2f * warpStrength;
                    float adjustedY = Mathf.Clamp01(normalizedY + warpOffset);

                    LatitudeBandType band;
                    if (adjustedY < tropicalEnd)
                    {
                        band = LatitudeBandType.Tropical;
                    }
                    else if (adjustedY < temperateEnd)
                    {
                        band = LatitudeBandType.Temperate;
                    }
                    else
                    {
                        band = LatitudeBandType.Arctic;
                    }

                    world.LatitudeBands[(y * w) + x] = band;
                }
            }
        }

        private void Generate5Bands(WorldArrays world, int w, int h, float ox, float oy, float warpScale, float warpStrength)
        {
            float sum = _cfg.FiveBandSum();
            if (sum <= 0f)
            {
                _emit(LogLevel.WARN, LogContext.Module, LogPhase.Generation, "LATITUDE_SUM_ZERO",
                    "5-band percentages sum to 0. Using equal distribution.");
                sum = 1f;
            }

            float southArcticNorm = _cfg.FiveBandSouthArcticPercent / sum;
            float southTemperateNorm = _cfg.FiveBandSouthTemperatePercent / sum;
            float tropicalNorm = _cfg.FiveBandTropicalPercent / sum;
            float northTemperateNorm = _cfg.FiveBandNorthTemperatePercent / sum;
            float northArcticNorm = _cfg.FiveBandNorthArcticPercent / sum;

            float southArcticEnd = southArcticNorm;
            float southTemperateEnd = southArcticEnd + southTemperateNorm;
            float tropicalEnd = southTemperateEnd + tropicalNorm;
            float northTemperateEnd = tropicalEnd + northTemperateNorm;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float normalizedY = (float)y / Mathf.Max(1f, h - 1);
                    float warp = Mathf.PerlinNoise((x + ox) * warpScale, (y + oy) * warpScale);
                    float warpOffset = (warp - 0.5f) * 2f * warpStrength;
                    float adjustedY = Mathf.Clamp01(normalizedY + warpOffset);

                    LatitudeBandType band;
                    if (adjustedY < southArcticEnd)
                    {
                        band = LatitudeBandType.Arctic;
                    }
                    else if (adjustedY < southTemperateEnd)
                    {
                        band = LatitudeBandType.Temperate;
                    }
                    else if (adjustedY < tropicalEnd)
                    {
                        band = LatitudeBandType.Tropical;
                    }
                    else if (adjustedY < northTemperateEnd)
                    {
                        band = LatitudeBandType.Temperate;
                    }
                    else
                    {
                        band = LatitudeBandType.Arctic;
                    }

                    world.LatitudeBands[(y * w) + x] = band;
                }
            }
        }
    }
}
