using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MapMaker.Shared.Data;
using MapMaker.Shared.Utils;
using MapMaker.Modules.Hydrology4.Config;
using MapMaker.Core.Logging;

namespace MapMaker.Modules.Hydrology4.Scripts
{
    /// <summary>
    /// Flow accumulation based hydrology generation for hex grid.
    /// Natural river networks emerge from physics-based flow simulation.
    /// </summary>
    public class HydrologyGenerator
    {
        private readonly HB_HydrologyConfig _cfg;
        private readonly SeedContext _seed;
        private readonly LogEmitter _emit;

        private int _width;
        private int _height;
        private float[] _elevation;
        private ElevationBandFinal[] _elevationBands;
        private bool[] _isLand;
        private float _oceanLevel;

        public HydrologyGenerator(
            HB_HydrologyConfig cfg,
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

            _width = world.Width;
            _height = world.Height;
            _elevation = world.ElevationRaw;
            _elevationBands = world.ElevationBands;

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Init, "HYDRO_CONFIG",
                $"Flow Accumulation: StreamThreshold={_cfg.StreamThreshold}, RiverThreshold={_cfg.RiverThreshold}");

            // Build land mask
            BuildLandMask(world);

            // Determine ocean level
            _oceanLevel = DetermineOceanLevel(world);
            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Init, "HYDRO_CONFIG",
                $"Ocean Level: {_oceanLevel:F3}");

            // Phase 1: Calculate flow directions
            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_PHASE1",
                "Calculating flow directions (D6 steepest descent)");
            CalculateFlowDirections(world);

            // Phase 2: Calculate flow accumulation
            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_PHASE2",
                "Calculating flow accumulation (drainage areas)");
            CalculateFlowAccumulation(world);

            // Phase 3: Classify rivers
            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_PHASE3",
                "Classifying rivers by drainage area");
            ClassifyRivers(world);

            // Phase 4: Detect basins
            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_PHASE4",
                "Detecting natural basins");
            DetectBasins(world);

            // Phase 5: Detect features (waterfalls, rapids)
            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_PHASE5",
                "Detecting waterfalls and rapids");
            DetectFeatures(world);

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_COMPLETE",
                "Flow-based hydrology generation complete");
        }

        private void BuildLandMask(WorldArrays world)
        {
            _isLand = new bool[_width * _height];
            for (int i = 0; i < _isLand.Length; i++)
            {
                var band = world.ElevationBands[i];
                _isLand[i] = (band != ElevationBandFinal.Ocean && band != ElevationBandFinal.DeepOcean);
            }
        }

        private float DetermineOceanLevel(WorldArrays world)
        {
            // Find first ocean tile
            for (int i = 0; i < world.ElevationBands.Length; i++)
            {
                if (world.ElevationBands[i] == ElevationBandFinal.Ocean)
                {
                    return world.ElevationRaw[i];
                }
            }
            return 0.15f; // Fallback
        }

        /// <summary>
        /// Phase 1: Calculate flow direction for each tile (D6 steepest descent).
        /// </summary>
        private void CalculateFlowDirections(WorldArrays world)
        {
            world.FlowDirection = new byte[_width * _height];

            int flatCount = 0;
            int oceanSinkCount = 0;

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int idx = y * _width + x;

                    // Ocean tiles don't flow anywhere
                    if (!_isLand[idx])
                    {
                        world.FlowDirection[idx] = 255; // No flow
                        oceanSinkCount++;
                        continue;
                    }

                    float currentElev = _elevation[idx];
                    float lowestElev = currentElev;
                    int lowestDir = -1;

                    // Check all 6 hex neighbors for steepest descent
                    for (int d = 0; d < GridSystem.NEIGHBOR_COUNT; d++)
                    {
                        int nidx = GridSystem.GetNeighborIndex(x, y, _width, _height, (HexDirection)d);
                        if (nidx < 0)
                            continue;

                        float neighborElev = _elevation[nidx];

                        // Find steepest downhill neighbor
                        if (neighborElev < lowestElev - _cfg.MinSlopeDifference)
                        {
                            lowestElev = neighborElev;
                            lowestDir = d;
                        }
                    }

                    if (lowestDir >= 0)
                    {
                        // Flows to neighbor in direction lowestDir
                        world.FlowDirection[idx] = (byte)lowestDir;
                    }
                    else
                    {
                        // Flat area or local minimum (potential basin)
                        world.FlowDirection[idx] = 255; // No flow
                        flatCount++;
                    }
                }
            }

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_FLOW_DIR",
                $"Flow directions calculated: {flatCount} flat/basin tiles, {oceanSinkCount} ocean sinks");
        }

        /// <summary>
        /// Phase 2: Calculate flow accumulation using topological sort.
        /// </summary>
        private void CalculateFlowAccumulation(WorldArrays world)
        {
            world.FlowAccumulation = new float[_width * _height];

            // Initialize: each tile starts with accumulation of 1 (itself)
            for (int i = 0; i < world.FlowAccumulation.Length; i++)
            {
                world.FlowAccumulation[i] = 1f;
            }

            // Topological sort: process tiles from high elevation to low
            var sortedTiles = TopologicalSort(world);

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_TOPO_SORT",
                $"Topological sort: {sortedTiles.Count} tiles ordered by elevation");

            // Accumulate flow from high to low
            foreach (int idx in sortedTiles)
            {
                int x = idx % _width;
                int y = idx / _width;

                byte flowDir = world.FlowDirection[idx];
                if (flowDir == 255)
                    continue; // No outflow (sink or flat)

                // Get downstream neighbor
                int downstreamIdx = GridSystem.GetNeighborIndex(x, y, _width, _height, (HexDirection)flowDir);
                if (downstreamIdx < 0)
                    continue;

                // Transfer this tile's accumulation to downstream tile
                world.FlowAccumulation[downstreamIdx] += world.FlowAccumulation[idx];
            }

            // Find max accumulation for logging
            float maxAccum = world.FlowAccumulation.Max();
            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_FLOW_ACCUM",
                $"Flow accumulation complete: max drainage = {maxAccum} tiles");
        }

        /// <summary>
        /// Topological sort of tiles by elevation (high to low).
        /// </summary>
        private List<int> TopologicalSort(WorldArrays world)
        {
            // Create list of (index, elevation) pairs for land tiles
            var tiles = new List<(int idx, float elev)>();
            for (int i = 0; i < _isLand.Length; i++)
            {
                if (_isLand[i])
                {
                    tiles.Add((i, _elevation[i]));
                }
            }

            // Sort by elevation descending (high to low)
            tiles.Sort((a, b) => b.elev.CompareTo(a.elev));

            return tiles.Select(t => t.idx).ToList();
        }

        /// <summary>
        /// Phase 3: Classify rivers based on flow accumulation thresholds.
        /// </summary>
        private void ClassifyRivers(WorldArrays world)
        {
            world.RiverTypes = new RiverType[_width * _height];
            if (world.IsLake == null)
            {
                world.IsLake = new bool[_width * _height];
            }

            int streamCount = 0;
            int riverCount = 0;
            int largeRiverCount = 0;
            int majorRiverCount = 0;

            for (int i = 0; i < world.FlowAccumulation.Length; i++)
            {
                if (!_isLand[i])
                    continue;

                float accum = world.FlowAccumulation[i];

                if (accum >= _cfg.MajorRiverThreshold)
                {
                    world.RiverTypes[i] = RiverType.MajorRiver;
                    majorRiverCount++;
                }
                else if (accum >= _cfg.LargeRiverThreshold)
                {
                    world.RiverTypes[i] = RiverType.River;
                    largeRiverCount++;
                }
                else if (accum >= _cfg.RiverThreshold)
                {
                    world.RiverTypes[i] = RiverType.Creek;
                    riverCount++;
                }
                else if (accum >= _cfg.StreamThreshold)
                {
                    world.RiverTypes[i] = RiverType.Stream;
                    streamCount++;
                }
            }

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_RIVERS",
                $"Rivers: {streamCount} streams, {riverCount} creeks, {largeRiverCount} rivers, {majorRiverCount} major rivers");
        }

        /// <summary>
        /// Phase 4: Detect natural basins (local minima where water pools).
        /// </summary>
        private void DetectBasins(WorldArrays world)
        {
            world.DrainageBasinId = new int[_width * _height];
            for (int i = 0; i < world.DrainageBasinId.Length; i++)
            {
                world.DrainageBasinId[i] = -1;
            }

            // Find tiles with no outflow (potential basins)
            List<int> sinks = new List<int>();
            for (int i = 0; i < world.FlowDirection.Length; i++)
            {
                if (!_isLand[i])
                    continue;

                if (world.FlowDirection[i] == 255) // No flow
                {
                    sinks.Add(i);
                }
            }

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_SINKS",
                $"Found {sinks.Count} flow sinks (potential basins)");

            // Group sinks into basins
            HashSet<int> unprocessed = new HashSet<int>(sinks);
            List<HashSet<int>> basins = new List<HashSet<int>>();

            while (unprocessed.Count > 0)
            {
                int seed = GetFirst(unprocessed);
                var basin = ExpandBasin(seed, world);
                basins.Add(basin);
                unprocessed.ExceptWith(basin);
            }

            // Process basins
            int basinId = 0;
            int keptBasins = 0;
            int filledBasins = 0;

            foreach (var basin in basins)
            {
                if (basin.Count < _cfg.MinBasinSize)
                {
                    // Too small - don't mark as lake
                    filledBasins++;
                    continue;
                }

if (basin.Count > _cfg.MaxBasinSize)
{
    // Too large - skip it (probably a plateau, not a lake)
    filledBasins++;
    continue;
}

// Mark as lake (only if size is valid)
foreach (int idx in basin)
{
    world.IsLake[idx] = true;
    world.DrainageBasinId[idx] = basinId;
}

                basinId++;
                keptBasins++;
            }

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_BASINS",
                $"Basins: {keptBasins} natural lakes formed, {filledBasins} too small");
        }

        /// <summary>
        /// Expand a basin from a sink point to include nearby flat areas.
        /// </summary>
        private HashSet<int> ExpandBasin(int sinkIdx, WorldArrays world)
        {
            var basin = new HashSet<int>();
            var queue = new Queue<int>();

            queue.Enqueue(sinkIdx);
            basin.Add(sinkIdx);

            float sinkElev = _elevation[sinkIdx];

            while (queue.Count > 0)
            {
                int idx = queue.Dequeue();
                int x = idx % _width;
                int y = idx / _width;

                // Expand to neighbors with similar elevation (flat areas pool together)
                for (int d = 0; d < GridSystem.NEIGHBOR_COUNT; d++)
                {
                    int nidx = GridSystem.GetNeighborIndex(x, y, _width, _height, (HexDirection)d);
                    if (nidx < 0 || basin.Contains(nidx))
                        continue;

                    if (!_isLand[nidx])
                        continue;

                    // Include if elevation is very close (within threshold)
                    float elevDiff = Mathf.Abs(_elevation[nidx] - sinkElev);
                    if (elevDiff < 0.01f) // Same elevation pool
                    {
                        basin.Add(nidx);
                        queue.Enqueue(nidx);
                    }
                }
            }

            return basin;
        }

        /// <summary>
        /// Phase 5: Detect waterfalls and rapids based on elevation drops.
        /// </summary>
        private void DetectFeatures(WorldArrays world)
        {
            world.IsWaterfall = new bool[_width * _height];
            world.IsRapids = new bool[_width * _height];

            int waterfallCount = 0;
            int rapidsCount = 0;

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int idx = y * _width + x;

                    // Only check river tiles
                    if (world.RiverTypes[idx] == RiverType.None)
                        continue;

                    byte flowDir = world.FlowDirection[idx];
                    if (flowDir == 255)
                        continue;

                    // Get downstream neighbor
                    int downstreamIdx = GridSystem.GetNeighborIndex(x, y, _width, _height, (HexDirection)flowDir);
                    if (downstreamIdx < 0)
                        continue;

                    // Calculate elevation drop
                    float drop = _elevation[idx] - _elevation[downstreamIdx];

                    if (drop >= _cfg.WaterfallThreshold)
                    {
                        world.IsWaterfall[idx] = true;
                        waterfallCount++;
                    }
                    else if (drop >= _cfg.RapidsThreshold)
                    {
                        world.IsRapids[idx] = true;
                        rapidsCount++;
                    }
                }
            }

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_FEATURES",
                $"Features: {waterfallCount} waterfalls, {rapidsCount} rapids");
        }

        private static T GetFirst<T>(HashSet<T> set)
        {
            foreach (T item in set)
                return item;
            throw new InvalidOperationException("Set is empty");
        }
    }
}
