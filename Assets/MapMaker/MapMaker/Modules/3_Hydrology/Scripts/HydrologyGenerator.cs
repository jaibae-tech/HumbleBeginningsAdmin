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
    /// Deterministic hydrology for hex grids.
    /// - D6 steepest-descent flow routing
    /// - Flow accumulation (drainage area)
    /// - Optional lake formation in closed local basins with strict caps
    /// - Outlet "carving" by forcing a spill path when lakes would grow too large
    ///
    /// Notes:
    /// - Does not assign biomes.
    /// - Does not decide initial ocean/land; uses ElevationBands.
    /// </summary>
    public sealed class HydrologyGenerator
    {
        private readonly HB_HydrologyConfig _cfg;
        private readonly SeedContext _seed;
        private readonly LogEmitter _emit;

        private int _w;
        private int _h;
        private float[] _elev;
        private ElevationBandFinal[] _bands;
        private bool[] _isLand;

        public HydrologyGenerator(HB_HydrologyConfig cfg, SeedContext seed, LogEmitter emit)
        {
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
            _seed = seed ?? throw new ArgumentNullException(nameof(seed));
            _emit = emit ?? throw new ArgumentNullException(nameof(emit));
        }

        public void Execute(WorldArrays world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (world.ElevationRaw == null) throw new ArgumentNullException(nameof(world.ElevationRaw));
            if (world.ElevationBands == null) throw new ArgumentNullException(nameof(world.ElevationBands));

            _w = world.Width;
            _h = world.Height;
            _elev = world.ElevationRaw;
            _bands = world.ElevationBands;

            BuildLandMask(world);

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Init, "HYDRO_CONFIG",
                $"Streams={_cfg.StreamThreshold}, Rivers={_cfg.RiverThreshold}, Large={_cfg.LargeRiverThreshold}, Major={_cfg.MajorRiverThreshold}; Lakes={( _cfg.EnableLakes ? "ON" : "OFF" )}");

            // Phase 1: flow directions
            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_PHASE1",
                "Calculating flow directions (D6 steepest descent)");
            CalculateFlowDirections(world);

            // Phase 2: flow accumulation
            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_PHASE2",
                "Calculating flow accumulation (drainage areas)");
            CalculateFlowAccumulation(world);

            // Phase 3: lakes + outlets (optional)
            if (_cfg.EnableLakes)
            {
                _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_PHASE3",
                    "Forming lakes in local basins (fill-to-spill with caps; overflow carves outlets)");
                BuildLakesAndOutlets(world);

                // Recompute accumulation after modifying flow for outlets
                _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_PHASE3B",
                    "Recomputing flow accumulation after lakes/outlets");
                CalculateFlowAccumulation(world);
            }
            else
            {
                // Ensure lake outputs are cleared
                ClearLakeOutputs(world);
            }

            // Phase 4: classify rivers + masks
            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_PHASE4",
                "Classifying rivers by drainage area and building masks");
            ClassifyRivers(world);

            // Phase 5: features
            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_PHASE5",
                "Detecting waterfalls and rapids");
            DetectFeatures(world);

            FinalizeWaterMasks(world);

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_COMPLETE",
                "Hydrology generation complete");
        }

        private void BuildLandMask(WorldArrays world)
        {
            _isLand = new bool[_w * _h];
            for (int i = 0; i < _isLand.Length; i++)
            {
                var b = world.ElevationBands[i];
                _isLand[i] = (b != ElevationBandFinal.Ocean && b != ElevationBandFinal.DeepOcean);
            }
        }

        private void CalculateFlowDirections(WorldArrays world)
        {
            if (world.FlowDirection == null || world.FlowDirection.Length != _w * _h)
                world.FlowDirection = new byte[_w * _h];

            int sinks = 0;
            int ocean = 0;

            for (int y = 0; y < _h; y++)
            {
                for (int x = 0; x < _w; x++)
                {
                    int idx = y * _w + x;

                    if (!_isLand[idx])
                    {
                        world.FlowDirection[idx] = 255;
                        ocean++;
                        continue;
                    }

                    float cur = _elev[idx];
                    float best = cur;
                    int bestDir = -1;

                    for (int d = 0; d < GridSystem.NEIGHBOR_COUNT; d++)
                    {
                        int nidx = GridSystem.GetNeighborIndex(x, y, _w, _h, (HexDirection)d);
                        if (nidx < 0) continue;

                        float ne = _elev[nidx];
                        if (ne < best - _cfg.MinSlopeDifference)
                        {
                            best = ne;
                            bestDir = d;
                        }
                    }

                    if (bestDir >= 0)
                    {
                        world.FlowDirection[idx] = (byte)bestDir;
                    }
                    else
                    {
                        world.FlowDirection[idx] = 255;
                        sinks++;
                    }
                }
            }

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_FLOW_DIR",
                $"Flow directions: {sinks} sinks (no outflow), {ocean} ocean tiles");
        }

        private void CalculateFlowAccumulation(WorldArrays world)
        {
            if (world.FlowAccumulation == null || world.FlowAccumulation.Length != _w * _h)
                world.FlowAccumulation = new float[_w * _h];

            // Reset
            for (int i = 0; i < world.FlowAccumulation.Length; i++)
                world.FlowAccumulation[i] = 1f;

            // Process land tiles high->low (simple sort; deterministic)
            var tiles = new List<(int idx, float elev)>(_w * _h);
            for (int i = 0; i < _isLand.Length; i++)
                if (_isLand[i]) tiles.Add((i, _elev[i]));

            tiles.Sort((a, b) => b.elev.CompareTo(a.elev));

            foreach (var t in tiles)
            {
                int idx = t.idx;
                byte dir = world.FlowDirection[idx];
                if (dir == 255) continue;

                int x = idx % _w;
                int y = idx / _w;
                int down = GridSystem.GetNeighborIndex(x, y, _w, _h, (HexDirection)dir);
                if (down < 0) continue;

                world.FlowAccumulation[down] += world.FlowAccumulation[idx];
            }

            float max = 0f;
            for (int i = 0; i < world.FlowAccumulation.Length; i++)
                if (_isLand[i]) max = Mathf.Max(max, world.FlowAccumulation[i]);

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_FLOW_ACCUM",
                $"Flow accumulation: max drainage = {max:0} tiles");
        }

        private void ClearLakeOutputs(WorldArrays world)
        {
            EnsureLakeArrays(world);
            Array.Clear(world.IsLake, 0, world.IsLake.Length);
            Array.Clear(world.IsDeepLake, 0, world.IsDeepLake.Length);
            Array.Clear(world.WaterDepth01, 0, world.WaterDepth01.Length);
            for (int i = 0; i < world.LakeId.Length; i++) world.LakeId[i] = -1;
        }

        private void EnsureLakeArrays(WorldArrays world)
        {
            int n = _w * _h;
            if (world.IsLake == null || world.IsLake.Length != n) world.IsLake = new bool[n];
            if (world.IsDeepLake == null || world.IsDeepLake.Length != n) world.IsDeepLake = new bool[n];
            if (world.LakeId == null || world.LakeId.Length != n) world.LakeId = new int[n];
            if (world.WaterDepth01 == null || world.WaterDepth01.Length != n) world.WaterDepth01 = new float[n];
        }

        private void BuildLakesAndOutlets(WorldArrays world)
        {
            EnsureLakeArrays(world);
            Array.Clear(world.IsLake, 0, world.IsLake.Length);
            Array.Clear(world.IsDeepLake, 0, world.IsDeepLake.Length);
            Array.Clear(world.WaterDepth01, 0, world.WaterDepth01.Length);
            for (int i = 0; i < world.LakeId.Length; i++) world.LakeId[i] = -1;

            // Identify sinks, sort by catchment (largest first) for stability.
            var sinks = new List<int>();
            for (int i = 0; i < world.FlowDirection.Length; i++)
                if (_isLand[i] && world.FlowDirection[i] == 255)
                    sinks.Add(i);

            sinks.Sort((a, b) => world.FlowAccumulation[b].CompareTo(world.FlowAccumulation[a]));

            int lakeId = 0;
            int lakesKept = 0;
            int lakesCarved = 0;
            int lakesRejected = 0;

            // Scratch buffers reused per sink
            var queue = new Queue<int>();
            var dist = new Dictionary<int, int>(1024);
            var parent = new Dictionary<int, int>(1024);
            var region = new HashSet<int>();

            foreach (var sink in sinks)
            {
                if (world.IsLake[sink]) continue; // already claimed by earlier lake

                var b = _bands[sink];
                bool alpine = (int)b >= (int)_cfg.AlpineBandStart;

                int minCatch = alpine ? _cfg.MinCatchmentForLakeHigh : _cfg.MinCatchmentForLakeLow;
                int maxArea = alpine ? _cfg.MaxLakeAreaTilesHigh : _cfg.MaxLakeAreaTilesLow;
                float maxDepth = alpine ? _cfg.MaxLakeFillDepthHigh : _cfg.MaxLakeFillDepthLow;

                if (world.FlowAccumulation[sink] < minCatch)
                {
                    lakesRejected++;
                    continue;
                }

                float sinkElev = _elev[sink];
                float capLevel = sinkElev + maxDepth;

                // Build a local basin candidate around sink (bounded radius, bounded by capLevel).
                region.Clear();
                queue.Clear();
                dist.Clear();
                parent.Clear();

                queue.Enqueue(sink);
                region.Add(sink);
                dist[sink] = 0;
                parent[sink] = -1;

                while (queue.Count > 0)
                {
                    int cur = queue.Dequeue();
                    int cx = cur % _w;
                    int cy = cur / _w;
                    int cd = dist[cur];
                    if (cd >= _cfg.MaxLakeSearchRadius) continue;

                    for (int d = 0; d < GridSystem.NEIGHBOR_COUNT; d++)
                    {
                        int nidx = GridSystem.GetNeighborIndex(cx, cy, _w, _h, (HexDirection)d);
                        if (nidx < 0) continue;
                        if (!_isLand[nidx]) continue;
                        if (region.Contains(nidx)) continue;

                        float ne = _elev[nidx];
                        if (ne > capLevel) continue;

                        // Respect cap early to avoid huge fills.
                        if (region.Count >= maxArea) break;

                        region.Add(nidx);
                        queue.Enqueue(nidx);
                        dist[nidx] = cd + 1;
                        parent[nidx] = cur;
                    }
                }

                if (region.Count < 2)
                {
                    lakesRejected++;
                    continue;
                }

                // Find spill candidate: lowest boundary neighbor just outside region.
                var spill = FindLowestBoundary(region);
                int spillTile = spill.spillTile;
                int spillOut = spill.outsideNeighbor;
                float spillElev = spill.spillElev;

                float waterLevel = Mathf.Min(spillElev, capLevel);
                bool needsCarve = false;

                if (spillTile < 0)
                {
                    // No boundary (should be rare). Treat as reject.
                    lakesRejected++;
                    continue;
                }

                // If spill is above cap, lake cannot naturally overflow within limits.
                if (spillElev > capLevel)
                    needsCarve = _cfg.CarveOutletOnLimits;

                // If we hit area cap during expansion, carve.
                if (region.Count >= maxArea)
                    needsCarve = _cfg.CarveOutletOnLimits;

                // Commit lake tiles
                foreach (var idx in region)
                {
                    world.IsLake[idx] = true;
                    world.LakeId[idx] = lakeId;
                    float depth = Mathf.Clamp01((waterLevel - _elev[idx]) / Mathf.Max(1e-6f, maxDepth));
                    world.WaterDepth01[idx] = depth;
                    // Lake tiles are still water, no per-tile outflow; outlet is forced below.
                    world.FlowDirection[idx] = 255;
                }

                if (region.Count >= 200)
                {
                    foreach (var idx in region) world.IsDeepLake[idx] = true;
                }

                // Force an outlet from lake when appropriate.
                if (needsCarve)
                {
                    ForceOutlet(world, region, sink, spillTile, spillOut);
                    lakesCarved++;
                }
                else
                {
                    // Natural spill: force outlet on the spill tile (lake drains via river).
                    ForceOutlet(world, region, sink, spillTile, spillOut);
                    lakesKept++;
                }

                lakeId++;
            }

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_LAKES",
                $"Lakes: {lakeId} total; {lakesKept} overflow outlets; {lakesCarved} carved outlets; {lakesRejected} rejected (low catchment / tiny)");
        }

        private (int spillTile, int outsideNeighbor, float spillElev) FindLowestBoundary(HashSet<int> region)
        {
            int bestTile = -1;
            int bestOut = -1;
            float bestElev = float.PositiveInfinity;

            foreach (var idx in region)
            {
                int x = idx % _w;
                int y = idx / _w;
                for (int d = 0; d < GridSystem.NEIGHBOR_COUNT; d++)
                {
                    int nidx = GridSystem.GetNeighborIndex(x, y, _w, _h, (HexDirection)d);
                    if (nidx < 0) continue;
                    if (!_isLand[nidx]) continue;
                    if (region.Contains(nidx)) continue;

                    float ne = _elev[nidx];
                    if (ne < bestElev)
                    {
                        bestElev = ne;
                        bestTile = idx;
                        bestOut = nidx;
                    }
                }
            }

            return (bestTile, bestOut, bestElev);
        }

        private void ForceOutlet(WorldArrays world, HashSet<int> region, int sink, int spillTile, int outsideNeighbor)
        {
            // Choose a deterministic outlet tile: the spillTile (inside region) will flow to outsideNeighbor.
            // If outsideNeighbor is invalid, fall back to lowest neighbor overall.
            int outIdx = outsideNeighbor;
            if (outIdx < 0)
            {
                outIdx = GetLowestNeighbor(spillTile);
            }

            if (outIdx < 0)
            {
                // Can't find anywhere to go.
                return;
            }

            // Ensure spill tile is treated as a river outlet (not lake standing water).
            world.IsLake[spillTile] = false;
            world.WaterDepth01[spillTile] = 0f;
            world.LakeId[spillTile] = -1;

            // Set flow direction for spill tile.
            int sx = spillTile % _w;
            int sy = spillTile / _w;
            int dir = FindNeighborDir(sx, sy, outIdx);
            if (dir >= 0)
                world.FlowDirection[spillTile] = (byte)dir;
            else
                world.FlowDirection[spillTile] = 255;

            // If the sink itself is not the spill tile, route sink toward spill tile by stepping through region.
            // We do this by greedily moving toward spillTile along the lowest-elevation neighbor inside the region.
            if (sink != spillTile)
            {
                int cur = sink;
                int safety = 0;
                while (cur != spillTile && safety++ < _cfg.MaxOutletPathSteps)
                {
                    int next = GetBestStepToward(cur, spillTile, region);
                    if (next < 0) break;
                    int cx = cur % _w;
                    int cy = cur / _w;
                    int ndir = FindNeighborDir(cx, cy, next);
                    if (ndir < 0) break;
                    world.FlowDirection[cur] = (byte)ndir;
                    cur = next;
                }
            }
        }

        private int GetBestStepToward(int fromIdx, int targetIdx, HashSet<int> region)
        {
            int fx = fromIdx % _w;
            int fy = fromIdx / _w;

            float bestScore = float.PositiveInfinity;
            int best = -1;

            int tx = targetIdx % _w;
            int ty = targetIdx / _w;

            for (int d = 0; d < GridSystem.NEIGHBOR_COUNT; d++)
            {
                int nidx = GridSystem.GetNeighborIndex(fx, fy, _w, _h, (HexDirection)d);
                if (nidx < 0) continue;
                if (!region.Contains(nidx)) continue;

                // Heuristic: prefer lower elevation and closer to target.
                float elevScore = _elev[nidx];
                float distScore = Mathf.Abs((nidx % _w) - tx) + Mathf.Abs((nidx / _w) - ty);
                float score = elevScore * 10f + distScore;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = nidx;
                }
            }

            return best;
        }

        private int GetLowestNeighbor(int idx)
        {
            int x = idx % _w;
            int y = idx / _w;
            float best = float.PositiveInfinity;
            int bestIdx = -1;

            for (int d = 0; d < GridSystem.NEIGHBOR_COUNT; d++)
            {
                int nidx = GridSystem.GetNeighborIndex(x, y, _w, _h, (HexDirection)d);
                if (nidx < 0) continue;
                float ne = _elev[nidx];
                if (ne < best)
                {
                    best = ne;
                    bestIdx = nidx;
                }
            }
            return bestIdx;
        }

        private int FindNeighborDir(int x, int y, int neighborIdx)
        {
            for (int d = 0; d < GridSystem.NEIGHBOR_COUNT; d++)
            {
                int nidx = GridSystem.GetNeighborIndex(x, y, _w, _h, (HexDirection)d);
                if (nidx == neighborIdx) return d;
            }
            return -1;
        }

        private void ClassifyRivers(WorldArrays world)
        {
            int n = _w * _h;
            if (world.RiverTypes == null || world.RiverTypes.Length != n) world.RiverTypes = new RiverType[n];
            if (world.RiverFlow01 == null || world.RiverFlow01.Length != n) world.RiverFlow01 = new float[n];
            if (world.IsStream == null || world.IsStream.Length != n) world.IsStream = new bool[n];
            if (world.IsMinorRiver == null || world.IsMinorRiver.Length != n) world.IsMinorRiver = new bool[n];
            if (world.IsRiver == null || world.IsRiver.Length != n) world.IsRiver = new bool[n];
            if (world.IsMainRiver == null || world.IsMainRiver.Length != n) world.IsMainRiver = new bool[n];

            Array.Clear(world.RiverTypes, 0, world.RiverTypes.Length);
            Array.Clear(world.IsStream, 0, world.IsStream.Length);
            Array.Clear(world.IsMinorRiver, 0, world.IsMinorRiver.Length);
            Array.Clear(world.IsRiver, 0, world.IsRiver.Length);
            Array.Clear(world.IsMainRiver, 0, world.IsMainRiver.Length);
            Array.Clear(world.RiverFlow01, 0, world.RiverFlow01.Length);

            float maxAccum = 1f;
            for (int i = 0; i < world.FlowAccumulation.Length; i++)
                if (_isLand[i]) maxAccum = Mathf.Max(maxAccum, world.FlowAccumulation[i]);

            int stream = 0, creek = 0, river = 0, major = 0;

            for (int i = 0; i < world.FlowAccumulation.Length; i++)
            {
                if (!_isLand[i]) continue;
                if (world.IsLake != null && world.IsLake[i]) continue; // standing water, not river tile

                float a = world.FlowAccumulation[i];
                world.RiverFlow01[i] = Mathf.Clamp01(a / maxAccum);

                if (a >= _cfg.MajorRiverThreshold)
                {
                    world.RiverTypes[i] = RiverType.MajorRiver;
                    world.IsMainRiver[i] = true;
                    major++;
                }
                else if (a >= _cfg.LargeRiverThreshold)
                {
                    world.RiverTypes[i] = RiverType.River;
                    world.IsRiver[i] = true;
                    river++;
                }
                else if (a >= _cfg.RiverThreshold)
                {
                    world.RiverTypes[i] = RiverType.Creek;
                    world.IsMinorRiver[i] = true;
                    creek++;
                }
                else if (a >= _cfg.StreamThreshold)
                {
                    world.RiverTypes[i] = RiverType.Stream;
                    world.IsStream[i] = true;
                    stream++;
                }
            }

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_RIVERS",
                $"Rivers: {stream} streams, {creek} creeks, {river} rivers, {major} major rivers");
        }

        private void DetectFeatures(WorldArrays world)
        {
            int n = _w * _h;
            if (world.IsWaterfall == null || world.IsWaterfall.Length != n) world.IsWaterfall = new bool[n];
            if (world.IsRapids == null || world.IsRapids.Length != n) world.IsRapids = new bool[n];
            Array.Clear(world.IsWaterfall, 0, world.IsWaterfall.Length);
            Array.Clear(world.IsRapids, 0, world.IsRapids.Length);

            int waterfalls = 0;
            int rapids = 0;

            for (int idx = 0; idx < n; idx++)
            {
                if (world.RiverTypes == null || world.RiverTypes[idx] == RiverType.None) continue;
                byte dir = world.FlowDirection[idx];
                if (dir == 255) continue;

                int x = idx % _w;
                int y = idx / _w;
                int down = GridSystem.GetNeighborIndex(x, y, _w, _h, (HexDirection)dir);
                if (down < 0) continue;

                float drop = _elev[idx] - _elev[down];
                if (drop >= _cfg.WaterfallThreshold)
                {
                    world.IsWaterfall[idx] = true;
                    waterfalls++;
                }
                else if (drop >= _cfg.RapidsThreshold)
                {
                    world.IsRapids[idx] = true;
                    rapids++;
                }
            }

            _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "HYDRO_FEATURES",
                $"Features: {waterfalls} waterfalls, {rapids} rapids");
        }

        private void FinalizeWaterMasks(WorldArrays world)
        {
            int n = _w * _h;
            if (world.IsWater == null || world.IsWater.Length != n) world.IsWater = new bool[n];
            Array.Clear(world.IsWater, 0, world.IsWater.Length);

            for (int i = 0; i < n; i++)
            {
                bool ocean = (world.IsOcean != null && world.IsOcean[i]);
                bool lake = (world.IsLake != null && world.IsLake[i]);
                bool river = (world.RiverTypes != null && world.RiverTypes[i] != RiverType.None);
                world.IsWater[i] = ocean || lake || river;
            }
        }
    }
}
