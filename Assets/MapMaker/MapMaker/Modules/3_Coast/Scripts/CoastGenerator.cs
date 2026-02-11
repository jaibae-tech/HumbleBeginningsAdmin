using System;
using System.Collections.Generic;
using UnityEngine;
using MapMaker.Shared.Data;
using MapMaker.Shared.Utils;
using MapMaker.Modules.Coast3.Config;
using MapMaker.Core.Logging;

namespace MapMaker.Modules.Coast3.Scripts
{
    /// <summary>
    /// Coast module (UPDATED FOR HEX GRID).
    /// Classifies ocean features and detects inland lakes.
    /// </summary>
    public class CoastGenerator
    {
        private readonly HB_CoastConfig _cfg;
        private readonly SeedContext _seed;
        private readonly LogEmitter _emit;

        public CoastGenerator(
            HB_CoastConfig cfg,
            SeedContext seed,
            LogEmitter emit)
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

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Validation, "COAST_CFG_VALID",
                $"Coast config validated (ShelfDepth={_cfg.CoastalShelfDepth}, MinLakeSize={_cfg.MinLakeSize})");

            // Step 1: Build land mask and count ocean
            bool[] isLand = BuildLandMask(world);
            int oceanCount = CountOcean(world);
            int deepOceanCount = 0;

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "COAST_OCEAN_CLASS",
                $"Total Ocean: {(oceanCount * 100.0 / world.ElevationBands.Length):F1} % ({oceanCount}/{world.ElevationBands.Length})");

            // Step 2: Classify deep ocean (far from land)
            ClassifyDeepOcean(world, isLand, out deepOceanCount);

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "COAST_OCEAN_CLASS",
                $"Deep Ocean: {(deepOceanCount * 100.0 / world.ElevationBands.Length):F1} % ({deepOceanCount}/{world.ElevationBands.Length}), " +
                $"Total Ocean: {(oceanCount * 100.0 / world.ElevationBands.Length):F1} % ({oceanCount}/{world.ElevationBands.Length})");

            // Step 3: Classify coastal shelf (near land)
            int shelfCount = ClassifyCoastalShelf(world, isLand);

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "COAST_SHELF_CLASS",
                $"Coastal Shelf: {(shelfCount * 100.0 / world.ElevationBands.Length):F1} % ({shelfCount}/{world.ElevationBands.Length}) " +
                $"within {_cfg.CoastalShelfDepth} tiles of land");

            // Step 4: Detect inland lakes
            var (lakeCount, lakeTiles, tinyConverted) = DetectInlandLakes(world, isLand);

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "COAST_LAKE_DETECT",
                $"Inland Lakes: {lakeCount} detected ({lakeTiles} tiles), {tinyConverted} tiny lakes converted to land");

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "COAST_COMPLETE",
                $"Coast classification complete for {width}x{height} map");

            // Validation
            ValidateCoast(world, isLand);
        }

        private bool[] BuildLandMask(WorldArrays world)
        {
            bool[] mask = new bool[world.ElevationBands.Length];
            for (int i = 0; i < mask.Length; i++)
            {
                var band = world.ElevationBands[i];
                mask[i] = (band != ElevationBandFinal.Ocean && band != ElevationBandFinal.DeepOcean);
            }
            return mask;
        }

        private int CountOcean(WorldArrays world)
        {
            int count = 0;
            for (int i = 0; i < world.ElevationBands.Length; i++)
            {
                var band = world.ElevationBands[i];
                if (band == ElevationBandFinal.Ocean || band == ElevationBandFinal.DeepOcean)
                    count++;
            }
            return count;
        }

        private void ClassifyDeepOcean(WorldArrays world, bool[] isLand, out int deepCount)
        {
            int width = world.Width;
            int height = world.Height;
            deepCount = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    if (isLand[idx])
                        continue;

                    var band = world.ElevationBands[idx];
                    if (band != ElevationBandFinal.Ocean)
                        continue;

                    // Check if far from land AND not in enclosed area
                    int distToLand = DistanceToLand(x, y, width, height, isLand, maxDist: 10);
                    int landNeighbors = CountLandNeighbors(x, y, width, height, isLand);

                    // Deep ocean criteria:
                    // - At least 5 tiles from land
                    // - Not enclosed (< 3 land neighbors in close range)
                    if (distToLand >= 5 && landNeighbors < 3)
                    {
                        world.IsDeepOcean[idx] = true;
                        world.ElevationBands[idx] = ElevationBandFinal.DeepOcean;
                        deepCount++;
                    }
                }
            }
        }

        private int CountLandNeighbors(int x, int y, int width, int height, bool[] isLand)
        {
            // Count land tiles within distance 2 (includes immediate hex ring + next ring)
            var tilesInRange = GridSystem.GetTilesInRadius(x, y, 2, width, height);
            int count = 0;
            
            foreach (int idx in tilesInRange)
            {
                if (idx != y * width + x && isLand[idx])
                    count++;
            }
            
            return count;
        }

        private int DistanceToLand(int x, int y, int width, int height, bool[] isLand, int maxDist)
        {
            // BFS to find nearest land
            var visited = new HashSet<int>();
            var queue = new Queue<(int idx, int dist)>();
            
            int startIdx = y * width + x;
            queue.Enqueue((startIdx, 0));
            visited.Add(startIdx);

            while (queue.Count > 0)
            {
                var (idx, dist) = queue.Dequeue();
                
                if (dist >= maxDist)
                    return maxDist;
                
                if (isLand[idx])
                    return dist;

                int cx = idx % width;
                int cy = idx / width;
                
                // Check all 6 hex neighbors
                for (int d = 0; d < GridSystem.NEIGHBOR_COUNT; d++)
                {
                    int nidx = GridSystem.GetNeighborIndex(cx, cy, width, height, (HexDirection)d);
                    if (nidx < 0 || visited.Contains(nidx))
                        continue;

                    visited.Add(nidx);
                    queue.Enqueue((nidx, dist + 1));
                }
            }

            return maxDist;
        }

        private int ClassifyCoastalShelf(WorldArrays world, bool[] isLand)
        {
            int width = world.Width;
            int height = world.Height;
            int shelfCount = 0;
            int shelfDepth = _cfg.CoastalShelfDepth;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    if (isLand[idx])
                        continue;

                    var band = world.ElevationBands[idx];
                    if (band != ElevationBandFinal.Ocean)
                        continue;

                    // Check distance to land
                    int distToLand = DistanceToLand(x, y, width, height, isLand, maxDist: shelfDepth + 1);

                    if (distToLand <= shelfDepth)
                    {
                        world.IsCoastalShelf[idx] = true;
                        shelfCount++;
                    }
                }
            }

            return shelfCount;
        }

        private (int lakeCount, int lakeTiles, int tinyConverted) DetectInlandLakes(
            WorldArrays world, bool[] isLand)
        {
            int width = world.Width;
            int height = world.Height;
            
            HashSet<int> unprocessed = new HashSet<int>();
            
            // Find all ocean tiles (potential lakes)
            for (int i = 0; i < world.ElevationBands.Length; i++)
            {
                var band = world.ElevationBands[i];
                if (band == ElevationBandFinal.Ocean || band == ElevationBandFinal.DeepOcean)
                {
                    unprocessed.Add(i);
                }
            }

            List<HashSet<int>> lakes = new List<HashSet<int>>();
            
            // Flood fill from main ocean to find disconnected water bodies
            if (unprocessed.Count > 0)
            {
                // Start from an edge ocean tile (guaranteed to be main ocean)
                int mainOceanSeed = FindEdgeOceanTile(world);
                
                if (mainOceanSeed >= 0)
                {
                    var mainOcean = GridSystem.FloodFill(mainOceanSeed, width, height, 
                        idx => unprocessed.Contains(idx));
                    
                    unprocessed.ExceptWith(mainOcean);
                }
            }

            // Remaining water bodies are inland lakes
            while (unprocessed.Count > 0)
            {
                int seed = GetFirst(unprocessed);
                var lake = GridSystem.FloodFill(seed, width, height, 
                    idx => unprocessed.Contains(idx));
                
                lakes.Add(lake);
                unprocessed.ExceptWith(lake);
            }

            // Process lakes
            int lakeTiles = 0;
            int tinyConverted = 0;
            int deepLakeCount = 0;

            foreach (var lake in lakes)
            {
                if (lake.Count < _cfg.MinLakeSize)
                {
                    // Convert tiny lake to land
                    foreach (int idx in lake)
                    {
                        world.ElevationRaw[idx] = 0.25f; // Raise to lowland
                        world.ElevationBands[idx] = ElevationBandFinal.Lowland;
                    }
                    tinyConverted++;
                }
                else
                {
                    // Mark as inland lake (no max size limit)
                    bool isDeepLake = lake.Count >= _cfg.DeepLakeThreshold;
                    if (isDeepLake) deepLakeCount++;

                    foreach (int idx in lake)
                    {
                        world.IsInlandLake[idx] = true;
                        world.IsLake[idx] = true;  // Set for moisture module
                        if (isDeepLake)
                        {
                            world.IsDeepLake[idx] = true;  // Large lake
                        }
                        world.IsOcean[idx] = false;
                        world.IsDeepOcean[idx] = false;
                    }
                    lakeTiles += lake.Count;
                }
            }

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "COAST_LAKE_DETECT",
                $"Inland Lakes: {lakes.Count - tinyConverted} detected ({lakeTiles} tiles), {deepLakeCount} deep lakes (>{_cfg.DeepLakeThreshold} tiles), {tinyConverted} tiny lakes converted to land");

            return (lakes.Count - tinyConverted, lakeTiles, tinyConverted);
        }

        private int FindEdgeOceanTile(WorldArrays world)
        {
            int width = world.Width;
            int height = world.Height;

            // Check all 4 edges for ocean
            for (int x = 0; x < width; x++)
            {
                // Top edge
                int idx = x;
                if (IsOcean(world, idx)) return idx;
                
                // Bottom edge
                idx = (height - 1) * width + x;
                if (IsOcean(world, idx)) return idx;
            }

            for (int y = 0; y < height; y++)
            {
                // Left edge
                int idx = y * width;
                if (IsOcean(world, idx)) return idx;
                
                // Right edge
                idx = y * width + (width - 1);
                if (IsOcean(world, idx)) return idx;
            }

            return -1;
        }

        private bool IsOcean(WorldArrays world, int idx)
        {
            var band = world.ElevationBands[idx];
            return band == ElevationBandFinal.Ocean || band == ElevationBandFinal.DeepOcean;
        }

        private void ValidateCoast(WorldArrays world, bool[] isLand)
        {
            int width = world.Width;
            int height = world.Height;

            // Count connected components of land
            HashSet<int> unvisitedLand = new HashSet<int>();
            for (int i = 0; i < isLand.Length; i++)
            {
                if (isLand[i])
                    unvisitedLand.Add(i);
            }

            int landComponents = 0;
            while (unvisitedLand.Count > 0)
            {
                int seed = GetFirst(unvisitedLand);
                var component = GridSystem.FloodFill(seed, width, height, 
                    idx => unvisitedLand.Contains(idx));
                unvisitedLand.ExceptWith(component);
                landComponents++;
            }

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Validation, "COAST_LAND_ISLANDS",
                $"Land has {landComponents} connected components (main continent + {landComponents - 1} islands)");

            // Check ocean connectivity (excluding inland lakes)
            HashSet<int> oceanTiles = new HashSet<int>();
            for (int i = 0; i < world.ElevationBands.Length; i++)
            {
                if (IsOcean(world, i) && !world.IsInlandLake[i])
                    oceanTiles.Add(i);
            }

            if (oceanTiles.Count > 0)
            {
                int oceanSeed = GetFirst(oceanTiles);
                var connectedOcean = GridSystem.FloodFill(oceanSeed, width, height, 
                    idx => oceanTiles.Contains(idx));

                if (connectedOcean.Count == oceanTiles.Count)
                {
                    _emit(LogLevel.INFO, LogContext.Module, LogPhase.Validation, "COAST_OCEAN_CONNECTED",
                        "All ocean is connected (single component, excluding inland lakes)");
                }
                else
                {
                    _emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "COAST_OCEAN_DISCONNECTED",
                        $"Ocean has multiple components: {connectedOcean.Count}/{oceanTiles.Count} in main body");
                }
            }

            // Validate coastal shelf coverage
            int shelfCount = 0;
            int oceanNonLakeCount = 0;
            for (int i = 0; i < world.IsCoastalShelf.Length; i++)
            {
                if (world.IsCoastalShelf[i])
                    shelfCount++;
                if (IsOcean(world, i) && !world.IsInlandLake[i])
                    oceanNonLakeCount++;
            }

            if (oceanNonLakeCount > 0)
            {
                _emit(LogLevel.INFO, LogContext.Module, LogPhase.Validation, "COAST_SHELF_COVERAGE",
                    $"Coastal shelf covers {(shelfCount * 100.0 / oceanNonLakeCount):F1} % of ocean tiles ({shelfCount}/{oceanNonLakeCount})");
            }
        }

        private static T GetFirst<T>(HashSet<T> set)
        {
            foreach (T item in set)
                return item;
            throw new InvalidOperationException("Set is empty");
        }
    }
}
