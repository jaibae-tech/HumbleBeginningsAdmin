using System;
using System.Collections.Generic;
using MapMaker.Core.Logging;
using MapMaker.Modules.Coast3.Config;
using MapMaker.Shared.Data;
using MapMaker.Shared.Utils;

namespace MapMaker.Modules.Coast3.Scripts
{
    public static class CoastValidate
    {
        public static void Validate(HB_CoastConfig cfg, LogEmitter emit)
        {
            if (cfg == null)
            {
                emit(LogLevel.ERROR, LogContext.Module, LogPhase.Validation, "COAST_CFG_NULL",
                    "HB_CoastConfig is null");
                return;
            }

            emit(LogLevel.INFO, LogContext.Module, LogPhase.Validation, "COAST_CFG_VALID",
                $"Coast config validated (ShelfDepth={cfg.CoastalShelfDepth}, MinLakeSize={cfg.MinLakeSize})");
        }

        public static void ValidateResults(WorldArrays world, LogEmitter emit)
        {
            if (world == null)
            {
                emit(LogLevel.ERROR, LogContext.Module, LogPhase.Validation, "COAST_WORLD_NULL",
                    "WorldArrays is null");
                return;
            }

            ValidateLandConnectivity(world, emit);
            ValidateOceanConnectivity(world, emit);
            ValidateCoastalShelfCoverage(world, emit);
        }

        private static void ValidateLandConnectivity(WorldArrays world, LogEmitter emit)
        {
            int w = world.Width;
            int h = world.Height;

            bool[] isLand = new bool[w * h];
            for (int i = 0; i < isLand.Length; i++)
            {
                isLand[i] = !world.IsOcean[i];
            }

            int componentCount = CountConnectedComponents(isLand, w, h);

            if (componentCount == 0)
            {
                emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "COAST_NO_LAND",
                    "No land tiles found on map");
            }
            else if (componentCount == 1)
            {
                emit(LogLevel.INFO, LogContext.Module, LogPhase.Validation, "COAST_LAND_CONNECTED",
                    "All land is connected (single component)");
            }
            else
            {
                emit(LogLevel.INFO, LogContext.Module, LogPhase.Validation, "COAST_LAND_ISLANDS",
                    $"Land has {componentCount} connected components (main continent + {componentCount - 1} islands)");
            }
        }

        private static void ValidateOceanConnectivity(WorldArrays world, LogEmitter emit)
        {
            int w = world.Width;
            int h = world.Height;

            bool[] oceanMask = new bool[w * h];
            for (int i = 0; i < world.Count; i++)
            {
                oceanMask[i] = world.IsOcean[i] && !world.IsInlandLake[i];
            }

            int componentCount = CountConnectedComponents(oceanMask, w, h);

            if (componentCount == 0)
            {
                emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "COAST_NO_OCEAN",
                    "No ocean tiles found on map");
            }
            else if (componentCount == 1)
            {
                emit(LogLevel.INFO, LogContext.Module, LogPhase.Validation, "COAST_OCEAN_CONNECTED",
                    "All ocean is connected (single component, excluding inland lakes)");
            }
            else
            {
                emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "COAST_OCEAN_FRAGMENTED",
                    $"Ocean has {componentCount} disconnected components (possible landlocked seas)");
            }
        }

        private static void ValidateCoastalShelfCoverage(WorldArrays world, LogEmitter emit)
        {
            int shelfCount = 0;
            int oceanCount = 0;

            for (int i = 0; i < world.Count; i++)
            {
                if (world.IsOcean[i])
                {
                    oceanCount++;
                    if (world.IsCoastalShelf[i])
                        shelfCount++;
                }
            }

            if (oceanCount == 0)
            {
                emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "COAST_NO_OCEAN",
                    "No ocean tiles to validate shelf coverage");
                return;
            }

            float shelfPct = (float)shelfCount / oceanCount;

            emit(LogLevel.INFO, LogContext.Module, LogPhase.Validation, "COAST_SHELF_COVERAGE",
                $"Coastal shelf covers {shelfPct:P1} of ocean tiles ({shelfCount}/{oceanCount})");
        }

        private static int CountConnectedComponents(bool[] mask, int w, int h)
        {
            bool[] visited = new bool[mask.Length];
            int count = 0;

            for (int i = 0; i < mask.Length; i++)
            {
                if (mask[i] && !visited[i])
                {
                    FloodFillMark(mask, visited, w, h, i);
                    count++;
                }
            }

            return count;
        }

        private static void FloodFillMark(bool[] mask, bool[] visited, int w, int h, int startIdx)
        {
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(startIdx);
            visited[startIdx] = true;

            while (queue.Count > 0)
            {
                int idx = queue.Dequeue();
                var (x, y) = GridHelpers.ToCoords(idx, w);

                foreach (var (nx, ny) in GridHelpers.GetNeighbors4(x, y, w, h))
                {
                    int nIdx = GridHelpers.ToIndex(nx, ny, w);
                    if (mask[nIdx] && !visited[nIdx])
                    {
                        visited[nIdx] = true;
                        queue.Enqueue(nIdx);
                    }
                }
            }
        }
    }
}
