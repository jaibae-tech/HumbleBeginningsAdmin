using System;
using System.Collections.Generic;
using UnityEngine;
using MapMaker.Shared.Data;
using MapMaker.Modules.Coast3.Config;
using MapMaker.Shared.Utils;
using MapMaker.Core.Logging;

namespace MapMaker.Modules.Coast3.Scripts
{
    /// <summary>
    /// Coast module (HEX GRID).
    /// Derives coastal properties from authoritative ocean masks without mutating terrain.
    ///
    /// Writes:
    /// - IsInlandLake[] (legacy name): ocean components NOT connected to map edge ("inland seas")
    /// - IsCoastalShelf[]: shallow ocean near shoreline (controlled width)
    /// - CoastDistance01[]: normalized distance inland from ocean coastline (0=coast, 1=far inland)
    /// </summary>
    public sealed class CoastGenerator
    {
        private readonly HB_CoastConfig _cfg;
        private readonly SeedContext _seed; // kept for signature consistency (not used)
        private readonly LogEmitter _emit;

        public CoastGenerator(HB_CoastConfig cfg, SeedContext seed, LogEmitter emit)
        {
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
            _seed = seed ?? throw new ArgumentNullException(nameof(seed));
            _emit = emit ?? throw new ArgumentNullException(nameof(emit));
        }

        public void Execute(WorldArrays world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            int width = world.Width;
            int height = world.Height;
            int count = world.Count;

            if (world.IsOcean == null || world.IsDeepOcean == null)
                throw new InvalidOperationException("Coast requires WorldArrays.IsOcean and WorldArrays.IsDeepOcean (produced by Elevation).");

            EnsureBuffers(world);

            Array.Clear(world.IsCoastalShelf, 0, count);
            Array.Clear(world.IsInlandLake, 0, count);
            if (world.CoastDistance01 != null) Array.Clear(world.CoastDistance01, 0, count);

            // 1) Edge-connected ocean flood fill
            bool[] edgeOcean = ComputeEdgeConnectedOcean(world.IsOcean, width, height);

            // 2) Inland seas (ocean components not connected to edge)
            int inlandComponents = 0;
            int inlandTiles = 0;
            if (_cfg.DetectInlandLakes)
            {
                MarkInlandSeas(world.IsOcean, edgeOcean, width, height, _cfg.MinLakeSize, world.IsInlandLake, out inlandComponents, out inlandTiles);
            }

            // 3) CoastDistance01 for LAND tiles (distance to TRUE ocean shoreline)
            int coastSeeds = 0;
            if (_cfg.ComputeCoastDistance && world.CoastDistance01 != null)
            {
                coastSeeds = ComputeCoastDistance01(world, edgeOcean, width, height, _cfg.MaxCoastDistanceTiles);
            }

            // 4) Coastal shelf in TRUE ocean (distance to land along ocean graph)
            int shelfTiles = ComputeShelf(world, edgeOcean, width, height);

            // Logging summary
            int oceanCount = 0;
            int deepCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (world.IsOcean[i]) oceanCount++;
                if (world.IsDeepOcean[i]) deepCount++;
            }

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "COAST_SUMMARY",
                $"Ocean={oceanCount} DeepOcean={deepCount} InlandSeaComponents={inlandComponents} InlandSeaTiles={inlandTiles} ShelfTiles={shelfTiles} CoastSeeds={coastSeeds}");
        }

        private static void EnsureBuffers(WorldArrays world)
        {
            int n = world.Count;
            if (world.IsCoastalShelf == null || world.IsCoastalShelf.Length != n) world.IsCoastalShelf = new bool[n];
            if (world.IsInlandLake == null || world.IsInlandLake.Length != n) world.IsInlandLake = new bool[n];
            if (world.CoastDistance01 == null || world.CoastDistance01.Length != n) world.CoastDistance01 = new float[n];
        }

        private static bool[] ComputeEdgeConnectedOcean(bool[] isOcean, int width, int height)
        {
            int n = width * height;
            bool[] edgeOcean = new bool[n];
            var q = new Queue<int>(Math.Min(n, 4096));

            // Seed with any ocean tile on boundary
            for (int x = 0; x < width; x++)
            {
                int top = x;
                int bot = (height - 1) * width + x;
                if (isOcean[top] && !edgeOcean[top]) { edgeOcean[top] = true; q.Enqueue(top); }
                if (isOcean[bot] && !edgeOcean[bot]) { edgeOcean[bot] = true; q.Enqueue(bot); }
            }
            for (int y = 0; y < height; y++)
            {
                int left = y * width;
                int right = y * width + (width - 1);
                if (isOcean[left] && !edgeOcean[left]) { edgeOcean[left] = true; q.Enqueue(left); }
                if (isOcean[right] && !edgeOcean[right]) { edgeOcean[right] = true; q.Enqueue(right); }
            }

            while (q.Count > 0)
            {
                int idx = q.Dequeue();
                int x = idx % width;
                int y = idx / width;
                for (int d = 0; d < GridSystem.NEIGHBOR_COUNT; d++)
                {
                    int nidx = GridSystem.GetNeighborIndex(x, y, width, height, (HexDirection)d);
                    if (nidx < 0) continue;
                    if (!isOcean[nidx]) continue;
                    if (edgeOcean[nidx]) continue;
                    edgeOcean[nidx] = true;
                    q.Enqueue(nidx);
                }
            }

            return edgeOcean;
        }

        private static void MarkInlandSeas(
            bool[] isOcean,
            bool[] edgeOcean,
            int width,
            int height,
            int minSize,
            bool[] outInlandSea,
            out int componentCount,
            out int tileCount)
        {
            int n = width * height;
            bool[] visited = new bool[n];
            componentCount = 0;
            tileCount = 0;

            var q = new Queue<int>(Math.Min(n, 4096));
            var component = new List<int>(1024);

            for (int i = 0; i < n; i++)
            {
                if (visited[i]) continue;
                if (!isOcean[i]) { visited[i] = true; continue; }
                if (edgeOcean[i]) { visited[i] = true; continue; } // true ocean, not inland
                // BFS component
                component.Clear();
                visited[i] = true;
                q.Enqueue(i);
                component.Add(i);

                while (q.Count > 0)
                {
                    int idx = q.Dequeue();
                    int x = idx % width;
                    int y = idx / width;
                    for (int d = 0; d < GridSystem.NEIGHBOR_COUNT; d++)
                    {
                        int nidx = GridSystem.GetNeighborIndex(x, y, width, height, (HexDirection)d);
                        if (nidx < 0) continue;
                        if (visited[nidx]) continue;
                        if (!isOcean[nidx]) { visited[nidx] = true; continue; }
                        if (edgeOcean[nidx]) { visited[nidx] = true; continue; } // connected to edge, not inland
                        visited[nidx] = true;
                        q.Enqueue(nidx);
                        component.Add(nidx);
                    }
                }

                if (component.Count >= minSize)
                {
                    componentCount++;
                    tileCount += component.Count;
                    for (int k = 0; k < component.Count; k++)
                        outInlandSea[component[k]] = true;
                }
            }
        }

        private static int ComputeCoastDistance01(WorldArrays world, bool[] edgeOcean, int width, int height, int maxDistTiles)
        {
            int n = width * height;

            // Distance array (int), initialize to -1
            int[] dist = new int[n];
            for (int i = 0; i < n; i++) dist[i] = -1;

            var q = new Queue<int>(Math.Min(n, 4096));
            int seeds = 0;

            // Seed: LAND tiles adjacent to TRUE ocean
            for (int idx = 0; idx < n; idx++)
            {
                if (world.IsOcean[idx]) continue; // only land
                int x = idx % width;
                int y = idx / width;

                bool touchesOcean = false;
                for (int d = 0; d < GridSystem.NEIGHBOR_COUNT; d++)
                {
                    int nidx = GridSystem.GetNeighborIndex(x, y, width, height, (HexDirection)d);
                    if (nidx < 0) continue;
                    if (world.IsOcean[nidx] && edgeOcean[nidx]) { touchesOcean = true; break; }
                }

                if (touchesOcean)
                {
                    dist[idx] = 0;
                    q.Enqueue(idx);
                    seeds++;
                }
            }

            // BFS across land only
            while (q.Count > 0)
            {
                int idx = q.Dequeue();
                int x = idx % width;
                int y = idx / width;
                int d0 = dist[idx];

                for (int dir = 0; dir < GridSystem.NEIGHBOR_COUNT; dir++)
                {
                    int nidx = GridSystem.GetNeighborIndex(x, y, width, height, (HexDirection)dir);
                    if (nidx < 0) continue;
                    if (world.IsOcean[nidx]) continue;
                    if (dist[nidx] >= 0) continue;
                    dist[nidx] = d0 + 1;
                    q.Enqueue(nidx);
                }
            }

            float inv = 1f / Mathf.Max(1, maxDistTiles);
            for (int i = 0; i < n; i++)
            {
                if (world.IsOcean[i]) { world.CoastDistance01[i] = 0f; continue; }
                int d = dist[i];
                if (d < 0) { world.CoastDistance01[i] = 1f; continue; } // isolated (shouldn't happen)
                float v = Mathf.Clamp01(d * inv);
                world.CoastDistance01[i] = v;
            }

            return seeds;
        }

        private int ComputeShelf(WorldArrays world, bool[] edgeOcean, int width, int height)
        {
            int n = width * height;

            // Only compute shelf in TRUE ocean by default
            bool excludeInlandSeas = _cfg.ExcludeInlandSeasFromShelf;

            int[] dist = new int[n];
            for (int i = 0; i < n; i++) dist[i] = -1;

            var q = new Queue<int>(Math.Min(n, 4096));
            // Seeds: TRUE ocean tiles adjacent to land
            for (int idx = 0; idx < n; idx++)
            {
                if (!world.IsOcean[idx]) continue;
                if (!edgeOcean[idx] && excludeInlandSeas) continue;

                int x = idx % width;
                int y = idx / width;
                bool touchesLand = false;
                for (int d = 0; d < GridSystem.NEIGHBOR_COUNT; d++)
                {
                    int nidx = GridSystem.GetNeighborIndex(x, y, width, height, (HexDirection)d);
                    if (nidx < 0) continue;
                    if (!world.IsOcean[nidx]) { touchesLand = true; break; }
                }

                if (touchesLand)
                {
                    dist[idx] = 0;
                    q.Enqueue(idx);
                }
            }

            // BFS in ocean
            while (q.Count > 0)
            {
                int idx = q.Dequeue();
                int x = idx % width;
                int y = idx / width;
                int d0 = dist[idx];
                if (d0 >= _cfg.CoastalShelfDepth) continue; // don't propagate beyond needed range

                for (int dir = 0; dir < GridSystem.NEIGHBOR_COUNT; dir++)
                {
                    int nidx = GridSystem.GetNeighborIndex(x, y, width, height, (HexDirection)dir);
                    if (nidx < 0) continue;
                    if (!world.IsOcean[nidx]) continue;
                    if (!edgeOcean[nidx] && excludeInlandSeas) continue;
                    if (dist[nidx] >= 0) continue;
                    dist[nidx] = d0 + 1;
                    q.Enqueue(nidx);
                }
            }

            int shelfCount = 0;
            for (int i = 0; i < n; i++)
            {
                if (!world.IsOcean[i]) continue;
                if (!edgeOcean[i] && excludeInlandSeas) continue;

                int d = dist[i];
                if (d < 0 || d > _cfg.CoastalShelfDepth) continue;

                if (_cfg.RequireNotDeepOcean && world.IsDeepOcean[i]) continue;

                world.IsCoastalShelf[i] = true;
                shelfCount++;
            }

            return shelfCount;
        }
    }
}
