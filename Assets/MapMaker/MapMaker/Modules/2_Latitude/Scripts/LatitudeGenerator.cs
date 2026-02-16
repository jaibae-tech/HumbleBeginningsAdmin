using System;
using UnityEngine;
using MapMaker.Core.Logging;
using MapMaker.Modules.Latitude2.Config;
using MapMaker.Shared.Data;
using MapMaker.Shared.Utils;

namespace MapMaker.Modules.Latitude2.Scripts
{
    /// <summary>
    /// Module 2: Latitude
    /// Produces a continuous latitude energy driver field (0..1).
    /// - 1.0 = warmest (south edge)
    /// - 0.0 = coldest (north edge)
    /// Also writes a per-tile seasonal amplitude proxy (0..1) derived only from latitude.
    ///
    /// This module intentionally does NOT assign biomes or discrete latitude bands.
    /// </summary>
    public sealed class LatitudeGenerator
    {
        private readonly HB_LatitudeConfig _cfg;
        private readonly SeedContext _seed;
        private readonly LogEmitter _emit;

        public LatitudeGenerator(HB_LatitudeConfig cfg, SeedContext seed, LogEmitter emit)
        {
            _cfg = cfg;
            _seed = seed;
            _emit = emit;
        }

        public void Execute(WorldArrays world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (_cfg == null) throw new ArgumentNullException(nameof(_cfg));

            int w = world.Width;
            int h = world.Height;

            if (world.LatitudeEnergy01 == null || world.LatitudeEnergy01.Length != world.Count)
                throw new InvalidOperationException("WorldArrays.LatitudeEnergy01 is not allocated or has incorrect length.");
            if (world.SeasonalAmplitude01 == null || world.SeasonalAmplitude01.Length != world.Count)
                throw new InvalidOperationException("WorldArrays.SeasonalAmplitude01 is not allocated or has incorrect length.");

            // Seed-stable global phase for the optional one-lobe warp.
            float phase = (float)(_seed.LatitudeRng.NextDouble() * (Math.PI * 2.0));

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "LATITUDE_DRIVER",
                $"Latitude driver field: Lmin={_cfg.LatitudeMin01:F2}, Lmax={_cfg.LatitudeMax01:F2}, curve={_cfg.CurvePower:F2}, warp={( _cfg.EnableGlobalWarp ? "ON" : "OFF" )}");

            float denomY = Mathf.Max(1f, h - 1);
            float denomX = Mathf.Max(1f, w - 1);

            for (int y = 0; y < h; y++)
            {
                // yN: 0 south (warm), 1 north (cold)
                float yN = y / denomY;
                float L0 = 1f - yN;

                for (int x = 0; x < w; x++)
                {
                    int idx = (y * w) + x;

                    float L = Mathf.Lerp(_cfg.LatitudeMin01, _cfg.LatitudeMax01, L0);

                    // Optional shaping (global curve, no spatial pattern)
                    if (Mathf.Abs(_cfg.CurvePower - 1f) > 0.0001f)
                        L = Mathf.Pow(L, _cfg.CurvePower);

                    // Optional single broad warp across X (one cycle) to avoid a perfectly uniform meridian slice.
                    if (_cfg.EnableGlobalWarp && _cfg.WarpAmplitude > 0f)
                    {
                        float xN = x / denomX;
                        float warp = Mathf.Sin((xN * Mathf.PI * 2f) + phase);
                        L = Mathf.Clamp01(L + (warp * _cfg.WarpAmplitude));
                    }

                    world.LatitudeEnergy01[idx] = L;

                    // Seasonal amplitude increases toward the cold north (low L).
                    float northness = Mathf.Clamp01(1f - L);
                    float a = Mathf.Pow(northness, _cfg.SeasonLatitudePower);
                    world.SeasonalAmplitude01[idx] = Mathf.Lerp(_cfg.SeasonAmpMin01, _cfg.SeasonAmpMax01, a);
                }
            }

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "LATITUDE_COMPLETE",
                $"Latitude energy computed for {w}x{h} tiles");
        }
    }
}
