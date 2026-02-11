using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

using MapMaker.Core.Logging;
using MapMaker.Core.Pipeline;
using MapMaker.Shared.Data;
using MapMaker.Shared.Export;
using MapMaker.Shared.Utils;
using MapMaker.Modules.Elevation1.Scripts;
using MapMaker.Modules.Latitude2.Scripts;
using MapMaker.Modules.Coast3.Scripts;
using MapMaker.Modules.Hydrology4.Scripts;

namespace MapMaker.Core.Driver 
{
    /// <summary>
    /// Thin orchestrator. Allocates shared buffers once, then runs enabled modules in order.
    /// Must not contain generation logic.
    /// </summary>
    public sealed class MapMakerDriver : MonoBehaviour
    {
        [Header("Pipeline")]
        public HB_PipelineConfig Pipeline;

        [Header("Driver Config")]
        public HB_MapConfig DriverConfig;

        private readonly WorldArrays _world = new WorldArrays();

        // --------------------------------------------------------------------
        // WorldData export (authoritative output for MapBake + Viewer)
        // --------------------------------------------------------------------

        [Serializable]
        private sealed class WorldMeta
        {
            public int formatVersion = 1;
            public int width;
            public int height;
            public int rootSeed;
            public float seaLevel01;
            public string notes;
        }

        private static string GetProjectRootPath()
        {
            // Application.dataPath -> <ProjectRoot>/Assets
            var assetsPath = Application.dataPath;
            return Directory.GetParent(assetsPath)?.FullName ?? assetsPath;
        }

        private void ExportWorldDataFiles(WorldArrays world, SeedContext seed)
        {
            const string worldDataRootFolder = "WorldData";
            string worldId = $"World_{seed.RootSeed}";

            string projectRoot = GetProjectRootPath();
            string worldRoot = Path.Combine(projectRoot, worldDataRootFolder, worldId);

            // --- 1) Meta.json ------------------------------------------------
            var meta = new WorldMeta
            {
                formatVersion = 1,
                width = world.Width,
                height = world.Height,
                rootSeed = seed.RootSeed,

                // IMPORTANT:
                // Keep this in the same normalized 0..1 space as ElevationRaw.
                // Replace with a real value from Hydrology if/when you expose it.
                seaLevel01 = 0.329f,

                notes = $"Generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
            };

            string metaPath = Path.Combine(worldRoot, "Meta.json");
            Directory.CreateDirectory(Path.GetDirectoryName(metaPath)!);
            File.WriteAllText(metaPath, JsonUtility.ToJson(meta, true));

            // --- 2) ElevationRaw.f32 -----------------------------------------
            // Format: little-endian float32, length = width * height
            string elevPath = Path.Combine(worldRoot, "Tiles", "ElevationRaw.f32");
            Directory.CreateDirectory(Path.GetDirectoryName(elevPath)!);

            var elev = world.ElevationRaw;
            int expected = world.Width * world.Height;

            if (elev == null || elev.Length != expected)
                throw new Exception(
                    $"ElevationRaw invalid. len={elev?.Length ?? 0}, expected={expected}");

            var bytes = new byte[elev.Length * sizeof(float)];
            Buffer.BlockCopy(elev, 0, bytes, 0, bytes.Length);
            File.WriteAllBytes(elevPath, bytes);

            UnityEngine.Debug.Log($"[MapMakerDriver] World data exported to: {worldRoot}");
        }

        // --------------------------------------------------------------------
        // Main pipeline
        // --------------------------------------------------------------------

        public void Run()
        {
            if (Pipeline == null)
            {
                UnityEngine.Debug.LogError("[MapMakerDriver] Pipeline is not assigned.");
                return;
            }

            if (DriverConfig == null)
            {
                UnityEngine.Debug.LogError("[MapMakerDriver] DriverConfig is not assigned.");
                return;
            }

            var emitter = MapMakerLogBinder.BindOrCreateEmitter(
                Pipeline.LogSource, DriverConfig);

            var totalTimer = Stopwatch.StartNew();

            emitter(LogLevel.INFO, LogContext.Driver, LogPhase.Init, "START",
                $"MapMaker starting (Width={DriverConfig.MapWidth}, Height={DriverConfig.MapHeight}, Seed={DriverConfig.RootSeed})");

            try
            {
                // Allocate shared arrays
                var allocTimer = Stopwatch.StartNew();
                _world.Allocate(DriverConfig.MapWidth, DriverConfig.MapHeight);
                allocTimer.Stop();

                emitter(LogLevel.INFO, LogContext.Driver, LogPhase.Progress, "ALLOC",
                    $"WorldArrays allocated in {allocTimer.ElapsedMilliseconds}ms");

                var seeds = new SeedContext(DriverConfig.RootSeed);

                // ---------------- Module 1: Elevation ----------------
                if (Pipeline.EnableElevation && Pipeline.Elevation != null)
                {
                    var t = Stopwatch.StartNew();

                    ElevationValidate.Validate(Pipeline.Elevation, emitter);

                    var gen = new ElevationGenerator(Pipeline.Elevation, seeds, emitter);
                    gen.Execute(_world);

                    var assign = new ElevationBandAssigner(
                        DriverConfig.MapWidth, DriverConfig.MapHeight,
                        Pipeline.Elevation, emitter);

                    assign.Execute(_world.ElevationRaw, _world.ElevationBands);

                    ElevationValidate.LogMountainOceanAdjacency(
                        _world.ElevationBands,
                        DriverConfig.MapWidth, DriverConfig.MapHeight, emitter);

                    t.Stop();
                    emitter(LogLevel.INFO, LogContext.Module, LogPhase.Progress,
                        "ELEVATION_TIMING", $"Module 1 completed in {t.ElapsedMilliseconds}ms");

                    if (Pipeline.Export != null)
                        WorldExportPass.ExportElevationBandsPng(
                            Pipeline.Export,
                            DriverConfig.MapWidth, DriverConfig.MapHeight,
                            _world, emitter);
                }

                // ---------------- Module 2: Latitude ----------------
                if (Pipeline.EnableLatitude && Pipeline.Latitude != null)
                {
                    var t = Stopwatch.StartNew();

                    bool useFiveBands =
                        DriverConfig.MapHeight >= DriverConfig.ThreeToFiveBandHeightThreshold;

                    LatitudeValidate.Validate(Pipeline.Latitude, useFiveBands, emitter);

                    var gen = new LatitudeGenerator(
                        Pipeline.Latitude,
                        DriverConfig.ThreeToFiveBandHeightThreshold,
                        seeds, emitter);

                    gen.Execute(_world);
                    LatitudeValidate.LogBandDistribution(_world, emitter);

                    t.Stop();
                    emitter(LogLevel.INFO, LogContext.Module, LogPhase.Progress,
                        "LATITUDE_TIMING", $"Module 2 completed in {t.ElapsedMilliseconds}ms");

                    if (Pipeline.Export != null)
                        WorldExportPass.ExportLatitudeBandsPng(
                            Pipeline.Export,
                            DriverConfig.MapWidth, DriverConfig.MapHeight,
                            _world, emitter);
                }

                // ---------------- Module 3: Coast ----------------
                if (Pipeline.EnableCoast && Pipeline.Coast != null)
                {
                    var t = Stopwatch.StartNew();

                    CoastValidate.Validate(Pipeline.Coast, emitter);

                    var gen = new CoastGenerator(Pipeline.Coast, seeds, emitter);
                    gen.Execute(_world);

                    CoastValidate.ValidateResults(_world, emitter);

                    t.Stop();
                    emitter(LogLevel.INFO, LogContext.Module, LogPhase.Progress,
                        "COAST_TIMING", $"Module 3 completed in {t.ElapsedMilliseconds}ms");

                    if (Pipeline.Export != null)
                        WorldExportPass.ExportCoastPng(
                            Pipeline.Export,
                            DriverConfig.MapWidth, DriverConfig.MapHeight,
                            _world, emitter);
                }

                // ---------------- Module 4: Hydrology ----------------
                if (Pipeline.EnableHydrology && Pipeline.Hydrology != null)
                {
                    var t = Stopwatch.StartNew();

                    var gen = new HydrologyGenerator(
                        Pipeline.Hydrology, seeds, emitter);

                    gen.Execute(_world);

                    t.Stop();
                    emitter(LogLevel.INFO, LogContext.Module, LogPhase.Progress,
                        "HYDROLOGY_TIMING", $"Module 4 completed in {t.ElapsedMilliseconds}ms");

                    if (Pipeline.Export != null)
                        WorldExportPass.ExportHydrologyPng(
                            Pipeline.Export,
                            DriverConfig.MapWidth, DriverConfig.MapHeight,
                            _world, emitter);
                }

                // ----------------------------------------------------------------
                // AUTHORITATIVE WORLD EXPORT (for MapBake + Viewer)
                // ----------------------------------------------------------------
                ExportWorldDataFiles(_world, seeds);

                // Optional debug exports (existing behavior)
                if (Pipeline.Export != null)
                {
                    WorldExportPass.ExportShadedReliefMap(
                        Pipeline.Export,
                        DriverConfig.MapWidth, DriverConfig.MapHeight,
                        _world, emitter);

                    WorldExportPass.ExportTopographicMap(
                        Pipeline.Export,
                        DriverConfig.MapWidth, DriverConfig.MapHeight,
                        _world, emitter);
                }

                totalTimer.Stop();
                emitter(LogLevel.INFO, LogContext.Driver, LogPhase.Shutdown, "END",
                    $"MapMaker completed run in {totalTimer.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                emitter(LogLevel.ERROR, LogContext.Driver, LogPhase.Shutdown,
                    "EXCEPTION", ex.ToString());
                throw;
            }
        }
    }
}
