using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

using MapMaker.Core.Logging;
using MapMaker.Core.Pipeline;
using MapMaker.Core.Export;
using MapMaker.Shared.Data;
using MapMaker.Shared.Export;
using MapMaker.Shared.Utils;
using MapMaker.Modules.Elevation1.Scripts;
using MapMaker.Modules.Elevation;
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

private static string ResolveExportRoot(HB_ExportConfig exportConfig)
{
    // One way: always use ExportFolderName as the base root, then append <seed>_<timestamp>/.
    // If ExportFolderName is relative, resolve it under <ProjectRoot>/Logs/.
    if (exportConfig == null || string.IsNullOrWhiteSpace(exportConfig.ExportFolderName))
        throw new Exception("HB_ExportConfig.ExportFolderName is null/empty.");

    if (Path.IsPathRooted(exportConfig.ExportFolderName))
        return Path.GetFullPath(exportConfig.ExportFolderName);

    var projectRoot = GetProjectRootPath();
    var logsRoot = Path.Combine(projectRoot, "Logs");
    return Path.GetFullPath(Path.Combine(logsRoot, exportConfig.ExportFolderName));
}


        private void ExportWorldDataFiles(WorldArrays world, SeedContext seed, string runExportRoot, string timestampUtc)
{
    // Authoritative output for MapBake + Viewer, scoped to this run folder:
    //   <ExportRoot>/<seed>_<timestamp>/WorldData/...
    if (string.IsNullOrWhiteSpace(runExportRoot))
        throw new ArgumentException("runExportRoot is null/empty");

    string worldRoot = Path.Combine(runExportRoot, "WorldData");

    // --- 1) Meta.json ------------------------------------------------
    var meta = new WorldMeta
    {
        formatVersion = 1,
        width = world.Width,
        height = world.Height,
        rootSeed = seed.RootSeed,

        // IMPORTANT:
        // Keep this in the same normalized 0..1 space as ElevationRaw.
        // Stage 2 will replace this with the derived sea level from the Elevation pipeline.
        seaLevel01 = 0.329f,

        notes = $"Generated {timestampUtc}Z"
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

            var totalTimer = Stopwatch.StartNew();

            LogEmitter emitter = null;
            SeedContext seeds = null;
            string timestampUtc = null;
            string runId = null;
            string runExportRoot = null;


// ----------------------------------------------------------------
// Stage 0: Run-scoped export folder (<ExportRoot>/<seed>_<timestamp>/)
// - One way: all exports + logs MUST go under this run folder.
// ----------------------------------------------------------------
if (Pipeline.Export == null)
{
    UnityEngine.Debug.LogError("[MapMakerDriver] Pipeline.Export is not assigned.");
    return;
}

seeds = new SeedContext(DriverConfig.RootSeed);

var runUtc = DateTime.UtcNow;
timestampUtc = runUtc.ToString("yyyyMMdd_HHmmss");
runId = $"{seeds.RootSeed}_{timestampUtc}";

var exportRoot = ResolveExportRoot(Pipeline.Export);
runExportRoot = Path.Combine(exportRoot, runId);
Directory.CreateDirectory(runExportRoot);

var runLogsRoot = Path.Combine(runExportRoot, "Logs");
MapMakerLogBinder.SetRuntimeLogFolder(runLogsRoot);

emitter = MapMakerLogBinder.BindOrCreateEmitter(
    Pipeline.LogSource, DriverConfig);

MapMakerLogging.Emitter = emitter;

emitter(LogLevel.INFO, LogContext.Driver, LogPhase.Init, "START",
    $"MapMaker starting (Width={DriverConfig.MapWidth}, Height={DriverConfig.MapHeight}, Seed={DriverConfig.RootSeed})");

emitter(LogLevel.INFO, LogContext.Driver, LogPhase.Init, "RUN",
    $"RunId={runId} ExportRoot={runExportRoot}");

            try
            {
                // Allocate shared arrays
                var allocTimer = Stopwatch.StartNew();
                _world.Allocate(DriverConfig.MapWidth, DriverConfig.MapHeight);
                allocTimer.Stop();

                emitter(LogLevel.INFO, LogContext.Driver, LogPhase.Progress, "ALLOC",
                    $"WorldArrays allocated in {allocTimer.ElapsedMilliseconds}ms");
            emitter = MapMakerLogBinder.BindOrCreateEmitter(
                Pipeline.LogSource, DriverConfig);

Directory.CreateDirectory(runExportRoot);

emitter(LogLevel.INFO, LogContext.Driver, LogPhase.Init, "RUN",
    $"RunId={runId} ExportRoot={runExportRoot}");
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

                    // Step 5: Derive slope + distance-to-coast fields
                    ElevationDerivativesPass.Apply(
                        _world,
                        DriverConfig.MapWidth,
                        DriverConfig.MapHeight,
                        Pipeline.Elevation,
                        emitter);

                    ElevationValidate.LogMountainOceanAdjacency(
                        _world.ElevationBands,
                        DriverConfig.MapWidth, DriverConfig.MapHeight, emitter);

                    t.Stop();
                    emitter(LogLevel.INFO, LogContext.Module, LogPhase.Progress,
                        "ELEVATION_TIMING", $"Module 1 completed in {t.ElapsedMilliseconds}ms");

                    
                    // Snapshot elevation for consistent preview exports across later modules.
                    // Later modules may temporarily modify ElevationRaw (e.g., hydrology prep), but previews should reflect one authoritative stage.
                    if (_world.ElevationExport01 != null && _world.ElevationRaw != null && _world.ElevationExport01.Length == _world.ElevationRaw.Length)
                    {
                        Array.Copy(_world.ElevationRaw, _world.ElevationExport01, _world.ElevationRaw.Length);
                    }
if (Pipeline.Export != null)
                        WorldExportPass.ExportElevationBandsPng(
                            Pipeline.Export,
                            DriverConfig.MapWidth, DriverConfig.MapHeight,
                            _world, emitter, runExportRoot);

                     if (Pipeline.Export != null)
                        WorldExportPass.ExportElevationGrayscalePng(
                            Pipeline.Export,
                            DriverConfig.MapWidth, DriverConfig.MapHeight,
                            _world, emitter, runExportRoot);

                    // Optional debugging views for Module 1 (helps diagnose landmass/plates/uplift)
                    if (Pipeline.Export != null && Pipeline.Elevation != null && Pipeline.Elevation.DebugEnabled)
                    {
                        WorldExportPass.ExportLandMaskPng(
                            Pipeline.Export,
                            DriverConfig.MapWidth, DriverConfig.MapHeight,
                            _world, emitter, runExportRoot);

                        WorldExportPass.ExportPlatesPng(
                            Pipeline.Export,
                            DriverConfig.MapWidth, DriverConfig.MapHeight,
                            _world, emitter, runExportRoot);

                        WorldExportPass.ExportUpliftPng(
                            Pipeline.Export,
                            DriverConfig.MapWidth, DriverConfig.MapHeight,
                            _world, emitter, runExportRoot);
                    }
                            
                }

                // ---------------- Module 2: Latitude ----------------
                if (Pipeline.EnableLatitude && Pipeline.Latitude != null)
                {
                    var t = Stopwatch.StartNew();

                    LatitudeValidate.Validate(Pipeline.Latitude, emitter);

                    var gen = new LatitudeGenerator(
                        Pipeline.Latitude,
                        seeds, emitter);

                    gen.Execute(_world);
                    LatitudeValidate.LogLatitudeStats(_world, emitter);

                    t.Stop();
                    emitter(LogLevel.INFO, LogContext.Module, LogPhase.Progress,
                        "LATITUDE_TIMING", $"Module 2 completed in {t.ElapsedMilliseconds}ms");

                    if (Pipeline.Export != null)
                        WorldExportPass.ExportLatitudeEnergyPng(
                            Pipeline.Export,
                            DriverConfig.MapWidth, DriverConfig.MapHeight,
                            _world, emitter, runExportRoot);
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
                            _world, emitter, runExportRoot);
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
                            _world, emitter, runExportRoot);
                }

                // ----------------------------------------------------------------
                // AUTHORITATIVE WORLD EXPORT (for MapBake + Viewer)
                // ----------------------------------------------------------------
                ExportWorldDataFiles(_world, seeds, runExportRoot, timestampUtc);
// Optional debug exports (existing behavior)
                if (Pipeline.Export != null)
                {
                    WorldExportPass.ExportShadedReliefMap(
                        Pipeline.Export,
                        DriverConfig.MapWidth, DriverConfig.MapHeight,
                        _world, emitter, runExportRoot);

                    // Step 5: terrain derivative previews
                    WorldExportPass.ExportSlopeMap(
                        Pipeline.Export,
                        DriverConfig.MapWidth, DriverConfig.MapHeight,
                        _world, emitter, runExportRoot);

                    WorldExportPass.ExportCoastDistanceMap(
                        Pipeline.Export,
                        DriverConfig.MapWidth, DriverConfig.MapHeight,
                        _world, emitter, runExportRoot);

                    // Step 6: additional terrain diagnostics
                    WorldExportPass.ExportAspectMap(
                        Pipeline.Export,
                        DriverConfig.MapWidth, DriverConfig.MapHeight,
                        _world, emitter, runExportRoot);

                    WorldExportPass.ExportCurvatureMap(
                        Pipeline.Export,
                        DriverConfig.MapWidth, DriverConfig.MapHeight,
                        _world, emitter, runExportRoot);

                    WorldExportPass.ExportTopographicMap(
                        Pipeline.Export,
                        DriverConfig.MapWidth, DriverConfig.MapHeight,
                        _world, emitter, runExportRoot);
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
