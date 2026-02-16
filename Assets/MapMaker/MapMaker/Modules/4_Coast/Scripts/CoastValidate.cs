using System;
using System.Collections.Generic;
using MapMaker.Core.Logging;
using MapMaker.Modules.Coast3.Config;
using MapMaker.Shared.Data;

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
                $"Coast config validated (ShelfDepth={cfg.CoastalShelfDepth}, MinInlandSeaSize={cfg.MinLakeSize}, MaxCoastDist={cfg.MaxCoastDistanceTiles})");
        }

        public static void ValidateResults(WorldArrays world, LogEmitter emit)
        {
            if (world == null)
            {
                emit(LogLevel.ERROR, LogContext.Module, LogPhase.Validation, "COAST_WORLD_NULL",
                    "WorldArrays is null");
                return;
            }

            if (world.IsOcean == null || world.IsDeepOcean == null || world.IsCoastalShelf == null || world.IsInlandLake == null)
            {
                emit(LogLevel.ERROR, LogContext.Module, LogPhase.Validation, "COAST_WORLD_MISSING",
                    "Missing required world arrays (IsOcean/IsDeepOcean/IsCoastalShelf/IsInlandLake).");
                return;
            }

            ValidateShelfInvariants(world, emit);
            ValidateInlandSeaInvariants(world, emit);
            ValidateCoastDistance(world, emit);
        }

        private static void ValidateShelfInvariants(WorldArrays world, LogEmitter emit)
        {
            int n = world.Count;
            int bad = 0;
            for (int i = 0; i < n; i++)
            {
                if (!world.IsCoastalShelf[i]) continue;
                if (!world.IsOcean[i]) { bad++; continue; }
                if (world.IsDeepOcean[i]) { bad++; continue; }
            }

            if (bad > 0)
                emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "COAST_SHELF_BAD",
                    $"Found {bad} shelf tiles that violate invariants (must be ocean and not deep).");
        }

        private static void ValidateInlandSeaInvariants(WorldArrays world, LogEmitter emit)
        {
            // Inland sea tiles must be ocean
            int n = world.Count;
            int bad = 0;
            for (int i = 0; i < n; i++)
            {
                if (!world.IsInlandLake[i]) continue;
                if (!world.IsOcean[i]) bad++;
            }

            if (bad > 0)
                emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "COAST_INLANDSEA_BAD",
                    $"Found {bad} inland-sea tiles that are not marked ocean.");
        }

        private static void ValidateCoastDistance(WorldArrays world, LogEmitter emit)
        {
            if (world.CoastDistance01 == null) return;

            int n = world.Count;
            int bad = 0;
            for (int i = 0; i < n; i++)
            {
                float v = world.CoastDistance01[i];
                if (float.IsNaN(v) || v < -0.001f || v > 1.001f) bad++;
            }

            if (bad > 0)
                emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "COAST_DISTANCE_BAD",
                    $"Found {bad} CoastDistance01 entries outside [0,1] or NaN.");
        }
    }
}
