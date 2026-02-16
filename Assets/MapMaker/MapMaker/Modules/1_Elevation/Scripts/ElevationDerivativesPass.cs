using System;
using System.Collections.Generic;
using MapMaker.Core.Logging;
using MapMaker.Shared.Data;
using MapMaker.Modules.Elevation1.Config;
using UnityEngine;

namespace MapMaker.Modules.Elevation
{
    /// <summary>
    /// Module 1 - Step 5: Terrain derivatives used by later modules.
    ///
    /// Outputs:
    /// - Slope01 (0..1): local gradient magnitude proxy (robustly normalized)
    /// - Aspect01 (0..1): steepest descent direction (0=east, 0.25=north, 0.5=west, 0.75=south)
    /// - Curvature01 (0..1): signed curvature proxy (0.5 flat, <0.5 concave/valley, >0.5 convex/ridge)
    /// - CoastDistance01 (0..1): distance to ocean (0 coastline, 1 far inland)
    ///
    /// Intentionally deferred (not required to proceed to next modules):
    /// - Aspect
    /// - Curvature
    /// </summary>
    public static class ElevationDerivativesPass
    {
        // Convenience overload used by some driver variants.
        public static void Apply(WorldArrays world, int width, int height, HB_ElevationConfig cfg, LogEmitter log)
            => Apply(cfg, world, log, LogContext.Module);

        public static void Apply(HB_ElevationConfig cfg, WorldArrays world, LogEmitter log, LogContext ctx = LogContext.Module)
        {
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (world.ElevationRaw == null || world.ElevationRaw.Length == 0)
                throw new InvalidOperationException("ElevationDerivativesPass requires world.ElevationRaw to be populated.");

            int w = world.Width;
            int h = world.Height;
            int n = w * h;

            if (world.Slope01 == null || world.Slope01.Length != n) world.Slope01 = new float[n];
            if (world.Aspect01 == null || world.Aspect01.Length != n) world.Aspect01 = new float[n];
            if (world.Curvature01 == null || world.Curvature01.Length != n) world.Curvature01 = new float[n];
            if (world.CoastDistance01 == null || world.CoastDistance01.Length != n) world.CoastDistance01 = new float[n];

            log?.Invoke(LogLevel.INFO, ctx, LogPhase.Progress, "ELEV_DERIV", "Computing derivatives: Slope01 + Aspect01 + Curvature01 + CoastDistance01...");

            ComputeSlope01(world, w, h);
            ComputeAspectAndCurvature01(world, w, h);
            ComputeCoastDistance01(cfg, world, w, h);

            log?.Invoke(LogLevel.INFO, ctx, LogPhase.Progress, "ELEV_DERIV", "Derivatives computed.");
        }

        private static void ComputeSlope01(WorldArrays world, int w, int h)
        {
            float[] e = world.ElevationRaw;
            float[] slope = world.Slope01;

            // Sample max-neighbor-delta values to get a stable normalization factor.
            var samples = new List<float>(Mathf.Max(1024, (w * h) / 64));
            int step = 8;

            for (int y = 0; y < h; y += step)
            {
                for (int x = 0; x < w; x += step)
                {
                    int idx = y * w + x;
                    float z = e[idx];
                    float maxAbs = 0f;

                    for (int d = 0; d < GridSystem.NEIGHBOR_COUNT; d++)
                    {
                        int nidx = GridSystem.GetNeighborIndex(x, y, w, h, (HexDirection)d);
                        if (nidx < 0) continue;
                        float adz = Mathf.Abs(e[nidx] - z);
                        if (adz > maxAbs) maxAbs = adz;
                    }

                    samples.Add(maxAbs);
                }
            }

            float denom = Percentile(samples, 0.95f);
            if (denom <= 1e-6f) denom = 1f;
            float inv = 1f / denom;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    float z = e[idx];
                    float maxAbs = 0f;

                    for (int d = 0; d < GridSystem.NEIGHBOR_COUNT; d++)
                    {
                        int nidx = GridSystem.GetNeighborIndex(x, y, w, h, (HexDirection)d);
                        if (nidx < 0) continue;
                        float adz = Mathf.Abs(e[nidx] - z);
                        if (adz > maxAbs) maxAbs = adz;
                    }

                    slope[idx] = Mathf.Clamp01(maxAbs * inv);
                }
            }
        }

        private static void ComputeAspectAndCurvature01(WorldArrays world, int w, int h)
        {
            int n = w * h;
            float[] e = world.ElevationRaw;

            var absCurvSamples = new List<float>(Mathf.Max(1024, n / 64));
            int step = 8;

            // First pass: sample abs curvature for robust normalization.
            for (int y = 0; y < h; y += step)
            {
                for (int x = 0; x < w; x += step)
                {
                    int idx = y * w + x;
                    float z = e[idx];

                    float sum = 0f;
                    int count = 0;
                    for (int d = 0; d < GridSystem.NEIGHBOR_COUNT; d++)
                    {
                        int nidx = GridSystem.GetNeighborIndex(x, y, w, h, (HexDirection)d);
                        if (nidx < 0) continue;
                        sum += e[nidx];
                        count++;
                    }

                    if (count > 0)
                    {
                        float mean = sum / count;
                        absCurvSamples.Add(Mathf.Abs(z - mean));
                    }
                }
            }

            float cDenom = Percentile(absCurvSamples, 0.95f);
            if (cDenom <= 1e-6f) cDenom = 1f;
            float invC = 1f / cDenom;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    float z = e[idx];

                    float gx = 0f, gy = 0f;
                    float sum = 0f;
                    int count = 0;

                    for (int d = 0; d < GridSystem.NEIGHBOR_COUNT; d++)
                    {
                        int nidx = GridSystem.GetNeighborIndex(x, y, w, h, (HexDirection)d);
                        if (nidx < 0) continue;

                        float zn = e[nidx];
                        sum += zn;
                        count++;

                        float dh = z - zn; // downhill if positive
                        if (dh <= 0f) continue;

                        // Unit vectors for E,SE,SW,W,NW,NE (clockwise from east)
                        // angles: 0,60,120,180,240,300
                        switch ((HexDirection)d)
                        {
                            case HexDirection.E:  gx += dh * 1f;         gy += dh * 0f;          break;
                            case HexDirection.SE: gx += dh * 0.5f;       gy += dh * 0.8660254f;  break;
                            case HexDirection.SW: gx += dh * -0.5f;      gy += dh * 0.8660254f;  break;
                            case HexDirection.W:  gx += dh * -1f;        gy += dh * 0f;          break;
                            case HexDirection.NW: gx += dh * -0.5f;      gy += dh * -0.8660254f; break;
                            case HexDirection.NE: gx += dh * 0.5f;       gy += dh * -0.8660254f; break;
                        }
                    }

                    // Aspect
                    if (gx != 0f || gy != 0f)
                    {
                        float a = Mathf.Atan2(gy, gx);
                        if (a < 0f) a += Mathf.PI * 2f;
                        world.Aspect01[idx] = a / (Mathf.PI * 2f);
                    }
                    else
                    {
                        world.Aspect01[idx] = 0f;
                    }

                    // Curvature
                    float c = 0f;
                    if (count > 0)
                    {
                        float mean = sum / count;
                        c = z - mean; // + ridge, - valley
                    }
                    c = Mathf.Clamp(c * invC, -1f, 1f);
                    world.Curvature01[idx] = 0.5f + 0.5f * c;
                }
            }
        }

        private static void ComputeCoastDistance01(HB_ElevationConfig cfg, WorldArrays world, int w, int h)
        {
            int n = w * h;

            // Ocean-source priority:
            // 1) world.IsOcean / world.IsDeepOcean (if already computed by earlier steps)
            // 2) world.ElevationBands (Ocean + DeepOcean)
            bool hasOceanFlags = world.IsOcean != null && world.IsOcean.Length == n;
            bool hasDeepFlags  = world.IsDeepOcean != null && world.IsDeepOcean.Length == n;
            bool hasBands      = world.ElevationBands != null && world.ElevationBands.Length == n;

            if (!hasOceanFlags && !hasBands)
            {
                // Cannot derive coast distance without an ocean mask.
                Array.Clear(world.CoastDistance01, 0, world.CoastDistance01.Length);
                return;
            }

            var dist = new int[n];
            Array.Fill(dist, -1);

            var q = new Queue<int>(n / 8);

            for (int i = 0; i < n; i++)
            {
                bool isOcean = false;

                if (hasOceanFlags) isOcean |= world.IsOcean[i];
                if (hasDeepFlags)  isOcean |= world.IsDeepOcean[i];

                if (hasBands)
                {
                    var b = world.ElevationBands[i];
                    isOcean |= (b == ElevationBandFinal.DeepOcean || b == ElevationBandFinal.Ocean);
                }

                if (isOcean)
                {
                    dist[i] = 0;
                    q.Enqueue(i);
                }
            }


            // If there are no ocean seeds, the BFS will leave dist[]=-1 everywhere and the
            // exported PNG will appear fully black.
            if (q.Count == 0)
            {
                Array.Clear(world.CoastDistance01, 0, world.CoastDistance01.Length);
                return;
            }

            while (q.Count > 0)
            {
                int idx = q.Dequeue();
                int x = idx % w;
                int y = idx / w;

                int baseD = dist[idx];

                for (int d = 0; d < GridSystem.NEIGHBOR_COUNT; d++)
                {
                    int nidx = GridSystem.GetNeighborIndex(x, y, w, h, (HexDirection)d);
                    if (nidx < 0) continue;
                    if (dist[nidx] >= 0) continue;

                    dist[nidx] = baseD + 1;
                    q.Enqueue(nidx);
                }
            }

            // Normalize: 1.0 means "far inland".
            // Cap by ~600mi to keep contrast on 1600mi worlds.
            float maxMiles = Mathf.Max(200f, Mathf.Min(600f, w * cfg.TileSizeMiles * 0.5f));
            float invMax = 1f / maxMiles;

            for (int i = 0; i < n; i++)
            {
                int dTiles = dist[i];
                if (dTiles <= 0)
                {
                    world.CoastDistance01[i] = 0f;
                    continue;
                }

                float miles = dTiles * cfg.TileSizeMiles;
                world.CoastDistance01[i] = Mathf.Clamp01(miles * invMax);
            }
        }

        private static float Percentile(List<float> values, float p)
        {
            if (values == null || values.Count == 0) return 0f;
            values.Sort();
            p = Mathf.Clamp01(p);
            float idx = p * (values.Count - 1);
            int i0 = Mathf.FloorToInt(idx);
            int i1 = Mathf.Min(i0 + 1, values.Count - 1);
            float t = idx - i0;
            return Mathf.Lerp(values[i0], values[i1], t);
        }
    }
}
