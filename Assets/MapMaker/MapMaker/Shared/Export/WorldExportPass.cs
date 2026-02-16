using System;
using System.IO;
using UnityEngine;
using MapMaker.Core.Export;
using MapMaker.Core.Logging;
using MapMaker.Shared.Data;

namespace MapMaker.Shared.Export
{
    public static class WorldExportPass
    {
        public static void ExportElevationBandsPng(
            HB_ExportConfig exportConfig,
            int width,
            int height,
            WorldArrays arrays,
            LogEmitter emit,
            string runExportRoot = null)
        {
            if (exportConfig == null || arrays == null || arrays.ElevationBands == null)
            {
                emit(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Missing export config or elevation bands; skipping elevation PNG.");
                return;
            }

            var path = GetExportPath(exportConfig, runExportRoot);
            Directory.CreateDirectory(path);

            var tex = new Texture2D(width * exportConfig.ExportTilePixelSize, height * exportConfig.ExportTilePixelSize, TextureFormat.RGBA32, false);
            try
            {
                FillTiled(tex, width, height, exportConfig, (x, y) => ColorForElevation((ElevationBandFinal)arrays.ElevationBands[(y * width) + x]));

                if (exportConfig.ExportFlipVertical)
                {
                    tex = FlipVertical(tex);
                }

                SavePng(tex, Path.Combine(path, "WorldPreview_01_ElevationBands.png"));
                emit(LogLevel.INFO, LogContext.Driver, LogPhase.Export, "PNG", "Wrote WorldPreview_01_ElevationBands.png");
            }
            finally
            {
                UnityEngine.Object.Destroy(tex);
            }
        }

        public static void ExportStackedPng_ExcludeLatitude(
            HB_ExportConfig exportConfig,
            int width,
            int height,
            WorldArrays arrays,
            LogEmitter emit,
            string runExportRoot = null)
        {
            if (exportConfig == null || arrays == null || arrays.ElevationBands == null)
            {
                emit(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Missing export config or elevation bands; skipping stacked PNG.");
                return;
            }

            var path = GetExportPath(exportConfig, runExportRoot);
            Directory.CreateDirectory(path);

            var tex = new Texture2D(width * exportConfig.ExportTilePixelSize, height * exportConfig.ExportTilePixelSize, TextureFormat.RGBA32, false);
            try
            {
                FillTiled(tex, width, height, exportConfig, (x, y) => ColorForElevation((ElevationBandFinal)arrays.ElevationBands[(y * width) + x]));

                if (exportConfig.ExportFlipVertical)
                {
                    tex = FlipVertical(tex);
                }

                SavePng(tex, Path.Combine(path, "WorldPreview_Stacked.png"));
                emit(LogLevel.INFO, LogContext.Driver, LogPhase.Export, "PNG", "Wrote WorldPreview_Stacked.png (excluding latitude)");
            }
            finally
            {
                UnityEngine.Object.Destroy(tex);
            }
        }

        public static void ExportLatitudeEnergyPng(
            HB_ExportConfig exportConfig,
            int width,
            int height,
            WorldArrays arrays,
            LogEmitter emit,
            string runExportRoot = null)
        {
            if (exportConfig == null || arrays == null || arrays.LatitudeEnergy01 == null)
            {
                emit(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Missing export config or latitude energy; skipping latitude PNG.");
                return;
            }

            var path = GetExportPath(exportConfig, runExportRoot);
            Directory.CreateDirectory(path);

            var tex = new Texture2D(width * exportConfig.ExportTilePixelSize, height * exportConfig.ExportTilePixelSize, TextureFormat.RGBA32, false);
            try
            {
                FillTiled(tex, width, height, exportConfig, (x, y) => ColorForLatitudeEnergy(arrays.LatitudeEnergy01[(y * width) + x]));

                if (exportConfig.ExportFlipVertical)
                {
                    tex = FlipVertical(tex);
                }

                SavePng(tex, Path.Combine(path, "WorldPreview_02_LatitudeEnergy.png"));
                emit(LogLevel.INFO, LogContext.Driver, LogPhase.Export, "PNG", "Wrote WorldPreview_02_LatitudeEnergy.png");
            }
            finally
            {
                UnityEngine.Object.Destroy(tex);
            }
        }

        public static void ExportStackedPng_WithLatitude(
            HB_ExportConfig exportConfig,
            int width,
            int height,
            WorldArrays arrays,
            LogEmitter emit,
            string runExportRoot = null)
        {
            if (exportConfig == null || arrays == null || arrays.ElevationBands == null || arrays.LatitudeEnergy01 == null)
            {
                emit(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Missing export config or data; skipping stacked PNG with latitude.");
                return;
            }

            var path = GetExportPath(exportConfig, runExportRoot);
            Directory.CreateDirectory(path);

            var tex = new Texture2D(width * exportConfig.ExportTilePixelSize, height * exportConfig.ExportTilePixelSize, TextureFormat.RGBA32, false);
            try
            {
                FillTiled(tex, width, height, exportConfig, (x, y) =>
                {
                    var elevColor = ColorForElevation((ElevationBandFinal)arrays.ElevationBands[(y * width) + x]);
                    var latColor = ColorForLatitudeEnergy(arrays.LatitudeEnergy01[(y * width) + x]);
                    return Color.Lerp(elevColor, latColor, 0.25f);
                });

                if (exportConfig.ExportFlipVertical)
                {
                    tex = FlipVertical(tex);
                }

                SavePng(tex, Path.Combine(path, "WorldPreview_Stacked.png"));
                emit(LogLevel.INFO, LogContext.Driver, LogPhase.Export, "PNG", "Wrote WorldPreview_Stacked.png (with latitude overlay)");
            }
            finally
            {
                UnityEngine.Object.Destroy(tex);
            }
        }

        public static void ExportCoastPng(
            HB_ExportConfig exportConfig,
            int width,
            int height,
            WorldArrays arrays,
            LogEmitter emit,
            string runExportRoot = null)
        {
            var elevSrc = GetElevationForExport(arrays);

            if (exportConfig == null || arrays == null || arrays.IsOcean == null || arrays.ElevationBands == null || arrays.IsDeepOcean == null || arrays.IsCoastalShelf == null)
            {
                emit(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Missing export config or coast data; skipping coast PNG.");
                return;
            }

            var path = GetExportPath(exportConfig, runExportRoot);
            Directory.CreateDirectory(path);

            var tex = new Texture2D(width * exportConfig.ExportTilePixelSize, height * exportConfig.ExportTilePixelSize, TextureFormat.RGBA32, false);
            try
            {
                FillTiled(tex, width, height, exportConfig, (x, y) => ColorForCoast(arrays, x, y, width));

                if (exportConfig.ExportFlipVertical)
                {
                    tex = FlipVertical(tex);
                }

                SavePng(tex, Path.Combine(path, "WorldPreview_03_Coast.png"));
                emit(LogLevel.INFO, LogContext.Driver, LogPhase.Export, "PNG", "Wrote WorldPreview_03_Coast.png");
            }
            finally
            {
                UnityEngine.Object.Destroy(tex);
            }
        }

        public static void ExportStackedPng_WithCoast(
            HB_ExportConfig exportConfig,
            int width,
            int height,
            WorldArrays arrays,
            LogEmitter emit,
            string runExportRoot = null)
        {
            if (exportConfig == null || arrays == null || arrays.ElevationBands == null)
            {
                emit(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Missing export config or data; skipping stacked PNG with coast.");
                return;
            }

            var path = GetExportPath(exportConfig, runExportRoot);
            Directory.CreateDirectory(path);

            var tex = new Texture2D(width * exportConfig.ExportTilePixelSize, height * exportConfig.ExportTilePixelSize, TextureFormat.RGBA32, false);
            try
            {
                FillTiled(tex, width, height, exportConfig, (x, y) =>
                {
                    int idx = y * width + x;
                    var elevColor = ColorForElevation((ElevationBandFinal)arrays.ElevationBands[idx]);

                    if (arrays.LatitudeEnergy01 != null)
                    {
                        var latColor = ColorForLatitudeEnergy(arrays.LatitudeEnergy01[idx]);
                        elevColor = Color.Lerp(elevColor, latColor, 0.25f);
                    }

                    if (arrays.IsDeepOcean != null && arrays.IsCoastalShelf != null && arrays.IsOcean != null)
                    {
                        if (arrays.IsOcean[idx])
                        {
                            Color coastColor = ColorForCoast(arrays, x, y, width);
                            elevColor = Color.Lerp(elevColor, coastColor, 0.4f);
                        }
                    }

                    return elevColor;
                });

                if (exportConfig.ExportFlipVertical)
                {
                    tex = FlipVertical(tex);
                }

                SavePng(tex, Path.Combine(path, "WorldPreview_Stacked.png"));
                emit(LogLevel.INFO, LogContext.Driver, LogPhase.Export, "PNG", "Wrote WorldPreview_Stacked.png (with latitude and coast overlay)");
            }
            finally
            {
                UnityEngine.Object.Destroy(tex);
            }
        }

       /// <summary>
        /// Export rivers and lakes visualization.
        /// </summary>
        public static void ExportHydrologyPng(
            HB_ExportConfig exportConfig,
            int width,
            int height,
            WorldArrays arrays,
            LogEmitter emit,
            string runExportRoot = null)
        {
            var elevSrc = GetElevationForExport(arrays);

            if (exportConfig == null || arrays == null || arrays.RiverTypes == null)
            {
                emit(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Missing export config or hydrology data; skipping hydrology PNG.");
                return;
            }

            var path = GetExportPath(exportConfig, runExportRoot);
            Directory.CreateDirectory(path);

            var tex = new Texture2D(width * exportConfig.ExportTilePixelSize, height * exportConfig.ExportTilePixelSize, TextureFormat.RGBA32, false);
            try
            {
                FillTiled(tex, width, height, exportConfig, (x, y) => ColorForHydrology(arrays, x, y, width));

                if (exportConfig.ExportFlipVertical)
                {
                    tex = FlipVertical(tex);
                }

                SavePng(tex, Path.Combine(path, "WorldPreview_04_Hydrology.png"));
                emit(LogLevel.INFO, LogContext.Driver, LogPhase.Export, "PNG", "Wrote WorldPreview_04_Hydrology.png");
            }
            finally
            {
                UnityEngine.Object.Destroy(tex);
            }
        }

        /// <summary>
        /// Export basin visualization (Phase 2 only - before rivers).
        /// Shows detected basins colored by above/below ocean classification.
        /// </summary>
        public static void ExportBasinsPng(
            HB_ExportConfig exportConfig,
            int width,
            int height,
            WorldArrays arrays,
            LogEmitter emit,
            string runExportRoot = null)
        {
            if (exportConfig == null || arrays == null)
            {
                emit(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Missing export config; skipping basins PNG.");
                return;
            }

            var path = GetExportPath(exportConfig, runExportRoot);
            Directory.CreateDirectory(path);

            var tex = new Texture2D(width * exportConfig.ExportTilePixelSize, height * exportConfig.ExportTilePixelSize, TextureFormat.RGBA32, false);
            try
            {
                FillTiled(tex, width, height, exportConfig, (x, y) => ColorForBasins(arrays, x, y, width));

                if (exportConfig.ExportFlipVertical)
                {
                    tex = FlipVertical(tex);
                }

                SavePng(tex, Path.Combine(path, "WorldPreview_04_Basins.png"));
                emit(LogLevel.INFO, LogContext.Driver, LogPhase.Export, "PNG", "Wrote WorldPreview_04_Basins.png");
            }
            finally
            {
                UnityEngine.Object.Destroy(tex);
            }
        }

        /// <summary>
        /// Update stacked PNG with hydrology overlay.
        /// </summary>
        public static void ExportStackedPng_WithHydrology(
            HB_ExportConfig exportConfig,
            int width,
            int height,
            WorldArrays arrays,
            LogEmitter emit,
            string runExportRoot = null)
        {
            if (exportConfig == null || arrays == null)
            {
                emit(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Missing export config; skipping stacked PNG.");
                return;
            }

            var path = GetExportPath(exportConfig, runExportRoot);
            Directory.CreateDirectory(path);

            var tex = new Texture2D(width * exportConfig.ExportTilePixelSize, height * exportConfig.ExportTilePixelSize, TextureFormat.RGBA32, false);
            try
            {
                FillTiled(tex, width, height, exportConfig, (x, y) =>
                {
                    int idx = y * width + x;
                    
                    // Base: Elevation
                    var baseColor = ColorForElevation((ElevationBandFinal)arrays.ElevationBands[idx]);
                    
                    // Overlay: Latitude energy (if available)
                    if (arrays.LatitudeEnergy01 != null)
                    {
                        var latColor = ColorForLatitudeEnergy(arrays.LatitudeEnergy01[idx]);
                        baseColor = Color.Lerp(baseColor, latColor, 0.20f);
                    }
                    
                    // Overlay: Coast (if available)
                    if (arrays.IsCoastalShelf != null && arrays.IsCoastalShelf[idx])
                    {
                        var coastColor = new Color(0.4f, 0.7f, 1.0f, 1f);
                        baseColor = Color.Lerp(baseColor, coastColor, 0.3f);
                    }
                    
                    // Overlay: Hydrology
                    if (arrays.RiverTypes != null)
                    {
                        var hydroColor = ColorForHydrology(arrays, x, y, width);
                        
                        // Only overlay rivers and lakes (not land/ocean base)
                        if (arrays.RiverTypes[idx] != RiverType.None || (arrays.IsLake != null && arrays.IsLake[idx]))
                        {
                            baseColor = Color.Lerp(baseColor, hydroColor, 0.6f);
                        }
                    }
            
                    return baseColor;
                });

                if (exportConfig.ExportFlipVertical)
                {
                    tex = FlipVertical(tex);
                }

                SavePng(tex, Path.Combine(path, "WorldPreview_Stacked.png"));
                emit(LogLevel.INFO, LogContext.Driver, LogPhase.Export, "PNG", "Wrote WorldPreview_Stacked.png (with hydrology overlay)");
            }
            finally
            {
                UnityEngine.Object.Destroy(tex);
            }
        }

/// <summary>
/// Add this to WorldExportPass.cs to visualize rivers on terrain
/// </summary>
public static void ExportTopographicMap(
    HB_ExportConfig exportConfig,
    int width,
    int height,
    WorldArrays arrays,
    LogEmitter emit,
            string runExportRoot = null)
{
    if (exportConfig == null || arrays == null)
    {
        emit(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Missing config");
        return;
    }

    var path = GetExportPath(exportConfig, runExportRoot);
    Directory.CreateDirectory(path);


    var elevSrc = GetElevationForExport(arrays);
    var tex = new Texture2D(width * exportConfig.ExportTilePixelSize, 
                            height * exportConfig.ExportTilePixelSize, 
                            TextureFormat.RGBA32, false);
    try
    {
        FillTiled(tex, width, height, exportConfig, (x, y) =>
        {
            int idx = y * width + x;
            
            // Priority: Waterfalls > Rapids > Rivers > Basins > Terrain
            
            if (arrays.IsWaterfall != null && arrays.IsWaterfall[idx])
            {
                return new Color(1f, 0f, 0f, 1f); // RED - Waterfall
            }
            
            if (arrays.IsRapids != null && arrays.IsRapids[idx])
            {
                return new Color(1f, 0.5f, 0f, 1f); // ORANGE - Rapids
            }
            
            if (arrays.RiverTypes != null && arrays.RiverTypes[idx] == RiverType.Stream)
            {
                return new Color(0.1f, 0.3f, 1f, 1f); // BLUE - River
            }
            
            if (arrays.IsLake != null && arrays.IsLake[idx])
            {
                return new Color(0.3f, 0.7f, 1f, 1f); // CYAN - Basin/Lake
            }
            
            // Terrain by elevation (grayscale)
            var src = GetElevationForExport(arrays);
                float elev = (src != null && idx < src.Length) ? src[idx] : 0f;
            float gray = elev;
            return new Color(gray, gray, gray, 1f);
        });

        if (exportConfig.ExportFlipVertical)
        {
            tex = FlipVertical(tex);
        }

        SavePng(tex, Path.Combine(path, "WorldPreview_Topographic.png"));
        emit(LogLevel.INFO, LogContext.Driver, LogPhase.Export, "PNG", 
             "Wrote WorldPreview_Topographic.png");
    }
    finally
    {
        UnityEngine.Object.Destroy(tex);
    }
}


/// <summary>
/// Grayscale slope map (Module 1 Step 5). White = steep.
/// </summary>
public static void ExportSlopeMap(
    HB_ExportConfig exportConfig,
    int width,
    int height,
    WorldArrays arrays,
    LogEmitter emit,
    string runExportRoot = null)
{
    if (exportConfig == null || arrays == null)
    {
        emit(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Missing config");
        return;
    }

    if (arrays.Slope01 == null)
    {
        emit(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Slope01 is null (Step 5 not run?)");
        return;
    }

    var path = GetExportPath(exportConfig, runExportRoot);
    Directory.CreateDirectory(path);

    var tex = new Texture2D(width * exportConfig.ExportTilePixelSize,
                            height * exportConfig.ExportTilePixelSize,
                            TextureFormat.RGBA32, false);
    try
    {
        FillTiled(tex, width, height, exportConfig, (x, y) =>
        {
            int idx = y * width + x;
            float v = Mathf.Clamp01(arrays.Slope01[idx]);
            return new Color(v, v, v, 1f);
        });

        var png = tex.EncodeToPNG();
        File.WriteAllBytes(Path.Combine(path, "WorldPreview_Slope.png"), png);
        emit(LogLevel.INFO, LogContext.Driver, LogPhase.Export, "EXPORT", "Wrote WorldPreview_Slope.png");
    }
    finally
    {
        UnityEngine.Object.Destroy(tex);
    }
}


/// <summary>
/// Grayscale distance-to-coast map (Module 1 Step 5). Black = coastline, White = far inland.
/// </summary>
public static void ExportCoastDistanceMap(
    HB_ExportConfig exportConfig,
    int width,
    int height,
    WorldArrays arrays,
    LogEmitter emit,
    string runExportRoot = null)
{
    if (exportConfig == null || arrays == null)
    {
        emit(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Missing config");
        return;
    }

    if (arrays.CoastDistance01 == null)
    {
        emit(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "CoastDistance01 is null (Step 5 not run?)");
        return;
    }

    var path = GetExportPath(exportConfig, runExportRoot);
    Directory.CreateDirectory(path);

    var tex = new Texture2D(width * exportConfig.ExportTilePixelSize,
                            height * exportConfig.ExportTilePixelSize,
                            TextureFormat.RGBA32, false);
    try
    {
        FillTiled(tex, width, height, exportConfig, (x, y) =>
        {
            int idx = y * width + x;
            if (arrays.IsOcean != null && arrays.IsOcean[idx]) return new Color(0f, 0f, 0f, 1f);
            float v = Mathf.Clamp01(arrays.CoastDistance01[idx]);
            return new Color(v, v, v, 1f);
        });

        var png = tex.EncodeToPNG();
        File.WriteAllBytes(Path.Combine(path, "WorldPreview_03_CoastDistance.png"), png);
        emit(LogLevel.INFO, LogContext.Driver, LogPhase.Export, "EXPORT", "Wrote WorldPreview_03_CoastDistance.png");
    }
    finally
    {
        UnityEngine.Object.Destroy(tex);
    }
}

/// <summary>
/// Grayscale aspect map (Module 1 Step 6). 0..1 angle of steepest descent (wrapped).
/// </summary>
public static void ExportAspectMap(
    HB_ExportConfig exportConfig,
    int width,
    int height,
    WorldArrays arrays,
    LogEmitter emit,
    string runExportRoot = null)
{
    if (exportConfig == null || arrays == null)
    {
        emit(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Missing config");
        return;
    }

    if (arrays.Aspect01 == null)
    {
        emit(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Aspect01 is null (Step 6 not run?)");
        return;
    }

    var path = GetExportPath(exportConfig, runExportRoot);
    Directory.CreateDirectory(path);

    var tex = new Texture2D(width * exportConfig.ExportTilePixelSize,
                            height * exportConfig.ExportTilePixelSize,
                            TextureFormat.RGBA32, false);
    try
    {
        FillTiled(tex, width, height, exportConfig, (x, y) =>
        {
            int idx = y * width + x;
            float v = Mathf.Clamp01(arrays.Aspect01[idx]);
            return new Color(v, v, v, 1f);
        });

        var png = tex.EncodeToPNG();
        File.WriteAllBytes(Path.Combine(path, "WorldPreview_Aspect.png"), png);
        emit(LogLevel.INFO, LogContext.Driver, LogPhase.Export, "EXPORT", "Wrote WorldPreview_Aspect.png");
    }
    finally
    {
        UnityEngine.Object.Destroy(tex);
    }
}

/// <summary>
/// Grayscale curvature map (Module 1 Step 6). 0.5 ~ flat, darker = valley, lighter = ridge.
/// </summary>
public static void ExportCurvatureMap(
    HB_ExportConfig exportConfig,
    int width,
    int height,
    WorldArrays arrays,
    LogEmitter emit,
    string runExportRoot = null)
{
    if (exportConfig == null || arrays == null)
    {
        emit(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Missing config");
        return;
    }

    if (arrays.Curvature01 == null)
    {
        emit(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Curvature01 is null (Step 6 not run?)");
        return;
    }

    var path = GetExportPath(exportConfig, runExportRoot);
    Directory.CreateDirectory(path);

    var tex = new Texture2D(width * exportConfig.ExportTilePixelSize,
                            height * exportConfig.ExportTilePixelSize,
                            TextureFormat.RGBA32, false);
    try
    {
        FillTiled(tex, width, height, exportConfig, (x, y) =>
        {
            int idx = y * width + x;
            float v = Mathf.Clamp01(arrays.Curvature01[idx]);
            return new Color(v, v, v, 1f);
        });

        var png = tex.EncodeToPNG();
        File.WriteAllBytes(Path.Combine(path, "WorldPreview_Curvature.png"), png);
        emit(LogLevel.INFO, LogContext.Driver, LogPhase.Export, "EXPORT", "Wrote WorldPreview_Curvature.png");
    }
    finally
    {
        UnityEngine.Object.Destroy(tex);
    }
}


/// <summary>
/// Shaded relief map with hillshading, rivers, and hydrologic features
/// Add this to WorldExportPass.cs
/// </summary>
public static void ExportShadedReliefMap(
    HB_ExportConfig exportConfig,
    int width,
    int height,
    WorldArrays arrays,
    LogEmitter emit,
            string runExportRoot = null)
{
    if (exportConfig == null || arrays == null)
    {
        emit(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Missing config");
        return;
    }

    var path = GetExportPath(exportConfig, runExportRoot);
    Directory.CreateDirectory(path);


    var elevSrc = GetElevationForExport(arrays);
    var tex = new Texture2D(width * exportConfig.ExportTilePixelSize, 
                            height * exportConfig.ExportTilePixelSize, 
                            TextureFormat.RGBA32, false);
    try
    {
        FillTiled(tex, width, height, exportConfig, (x, y) =>
        {
            int idx = y * width + x;
            
            // Calculate hillshade (light from northwest)
            float hillshade = CalculateHillshade(elevSrc, width, height, x, y);
            
            // Base terrain color (elevation-based with hillshade)
            var band = (ElevationBandFinal)arrays.ElevationBands[idx];
            Color baseColor = ColorForElevation(band);
            float shade = Mathf.Lerp(0.65f, 1.10f, hillshade);
            baseColor *= shade;
            baseColor.a = 1f;
            
            // Overlay hydrologic features
            
            // Waterfalls (bright red)
            if (arrays.IsWaterfall != null && arrays.IsWaterfall[idx])
            {
                return Color.Lerp(baseColor, new Color(1f, 0.1f, 0.1f, 1f), 0.8f);
            }
            
            // Rapids (orange)
            if (arrays.IsRapids != null && arrays.IsRapids[idx])
            {
                return Color.Lerp(baseColor, new Color(1f, 0.5f, 0.1f, 1f), 0.7f);
            }
            
            // Rivers (bright blue)
            if (arrays.RiverTypes != null && arrays.RiverTypes[idx] == RiverType.Stream)
            {
                return Color.Lerp(baseColor, new Color(0.2f, 0.5f, 1f, 1f), 0.7f);
            }
            
            // Basins/Lakes (cyan)
            if (arrays.IsLake != null && arrays.IsLake[idx])
            {
                return Color.Lerp(baseColor, new Color(0.4f, 0.7f, 0.95f, 1f), 0.6f);
            }
            
            return baseColor;
        });

        if (exportConfig.ExportFlipVertical)
        {
            tex = FlipVertical(tex);
        }

        SavePng(tex, Path.Combine(path, "WorldPreview_ShadedRelief.png"));
        emit(LogLevel.INFO, LogContext.Driver, LogPhase.Export, "PNG", 
             "Wrote WorldPreview_ShadedRelief.png");
    }
    finally
    {
        UnityEngine.Object.Destroy(tex);
    }
}

/// <summary>
/// Calculate hillshade value for a tile (0=shadow, 1=fully lit)
/// Light source from northwest at 45 degrees
/// </summary>
private static float CalculateHillshade(float[] elevation, int width, int height, int x, int y)
{
    // Get neighboring elevations for slope calculation
    float center = GetElevationSafe(elevation, width, height, x, y);
    float east = GetElevationSafe(elevation, width, height, x + 1, y);
    float west = GetElevationSafe(elevation, width, height, x - 1, y);
    float north = GetElevationSafe(elevation, width, height, x, y - 1);
    float south = GetElevationSafe(elevation, width, height, x, y + 1);
    
    // Calculate slope in x and y directions
    float dzdx = (east - west) / 2f;
    float dzdy = (south - north) / 2f;
    
    // Calculate slope and aspect
    float slope = Mathf.Sqrt(dzdx * dzdx + dzdy * dzdy);
    float aspect = Mathf.Atan2(dzdy, dzdx);
    
    // Light direction (from northwest = 315 degrees = -45 degrees)
    float lightAngle = -45f * Mathf.Deg2Rad;
    float lightAltitude = 45f * Mathf.Deg2Rad;
    
    // Hillshade calculation
    float hillshade = Mathf.Cos(lightAltitude) * Mathf.Cos(slope) +
                      Mathf.Sin(lightAltitude) * Mathf.Sin(slope) * 
                      Mathf.Cos(lightAngle - aspect);
    
    // Normalize to 0-1 range with bias toward visible
    hillshade = (hillshade + 1f) / 2f;
    hillshade = Mathf.Clamp01(hillshade * 0.8f + 0.2f); // Range: 0.2 to 1.0
    
    return hillshade;
}

/// <summary>
/// Safely get elevation with bounds checking
/// </summary>
private static float GetElevationSafe(float[] elevation, int width, int height, int x, int y)
{
    x = Mathf.Clamp(x, 0, width - 1);
    y = Mathf.Clamp(y, 0, height - 1);
    return elevation[y * width + x];
}

/// <summary>
/// Get terrain color based on elevation with hillshade applied
/// </summary>
private static Color GetTerrainColor(float elevation, float hillshade)
{
    Color baseColor;
    
    // Hypsometric tinting (elevation-based coloring)
    if (elevation < 0.16f)
    {
        // Deep Ocean (dark blue)
        baseColor = new Color(0.1f, 0.2f, 0.4f, 1f);
    }
    else if (elevation < 0.24f)
    {
        // Ocean (medium blue)
        baseColor = new Color(0.2f, 0.4f, 0.7f, 1f);
    }
    else if (elevation < 0.59f)
    {
        // Lowlands (green)
        float t = (elevation - 0.24f) / (0.59f - 0.24f);
        baseColor = Color.Lerp(new Color(0.3f, 0.6f, 0.3f, 1f), 
                               new Color(0.5f, 0.7f, 0.4f, 1f), t);
    }
    else if (elevation < 0.67f)
    {
        // Highlands (yellow-green)
        float t = (elevation - 0.59f) / (0.67f - 0.59f);
        baseColor = Color.Lerp(new Color(0.6f, 0.7f, 0.4f, 1f),
                               new Color(0.7f, 0.7f, 0.5f, 1f), t);
    }
    else if (elevation < 0.74f)
    {
        // Low Mountains (brown)
        float t = (elevation - 0.67f) / (0.74f - 0.67f);
        baseColor = Color.Lerp(new Color(0.6f, 0.5f, 0.4f, 1f),
                               new Color(0.5f, 0.4f, 0.3f, 1f), t);
    }
    else
    {
        // High Mountains (white)
        float t = (elevation - 0.74f) / (1f - 0.74f);
        baseColor = Color.Lerp(new Color(0.7f, 0.7f, 0.7f, 1f),
                               new Color(1f, 1f, 1f, 1f), t);
    }
    
    // Apply hillshade
    return new Color(
        baseColor.r * hillshade,
        baseColor.g * hillshade,
        baseColor.b * hillshade,
        1f
    );
}



        public static void ExportLandMaskPng(
            HB_ExportConfig exportConfig,
            int width,
            int height,
            WorldArrays arrays,
            LogEmitter emit,
            string runExportRoot = null)
        {
            if (exportConfig == null || arrays?.LandMask01 == null)
            {
                emit?.Invoke(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Missing land mask data; skipping land mask PNG.");
                return;
            }

            var path = GetExportPath(exportConfig, runExportRoot);
            Directory.CreateDirectory(path);

            var tex = new Texture2D(
                width * exportConfig.ExportTilePixelSize,
                height * exportConfig.ExportTilePixelSize,
                TextureFormat.RGBA32,
                false);

            try
            {
                FillTiled(tex, width, height, exportConfig, (x, y) =>
                {
                    int idx = y * width + x;
                    float v = Mathf.Clamp01(arrays.LandMask01[idx]);
                    // Ocean -> dark, Land -> bright
                    return new Color(v, v, v, 1f);
                });

                if (exportConfig.ExportFlipVertical)
                    tex = FlipVertical(tex);

                SavePng(tex, Path.Combine(path, "WorldPreview_01_LandMask.png"));
                emit?.Invoke(LogLevel.INFO, LogContext.Driver, LogPhase.Export, "PNG", "Wrote WorldPreview_01_LandMask.png");
            }
            finally
            {
                UnityEngine.Object.Destroy(tex);
            }
        }

        public static void ExportElevationGrayscalePng(
            HB_ExportConfig exportConfig,
            int width,
            int height,
            WorldArrays arrays,
            LogEmitter emit,
            string runExportRoot = null)
        {
            var elevSrc = GetElevationForExport(arrays);
            if (exportConfig == null || elevSrc == null)
            {
                emit(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Missing elevation data; skipping grayscale PNG.");
                return;
            }

            var path = GetExportPath(exportConfig, runExportRoot);
            Directory.CreateDirectory(path);

            var tex = new Texture2D(
                width * exportConfig.ExportTilePixelSize,
                height * exportConfig.ExportTilePixelSize,
                TextureFormat.RGBA32,
                false);

            try
            {
                float min = float.MaxValue;
                float max = float.MinValue;

                for (int i = 0; i < elevSrc.Length; i++)
                {
                    float v = arrays.ElevationRaw[i];
                    if (v < min) min = v;
                    if (v > max) max = v;
                }

                float range = Mathf.Max(0.0001f, max - min);

                FillTiled(tex, width, height, exportConfig, (x, y) =>
                {
                    float v = elevSrc[(y * width) + x];
                    float n = (v - min) / range;
                    return new Color(n, n, n, 1f);
                });

                if (exportConfig.ExportFlipVertical)
                    tex = FlipVertical(tex);

                SavePng(tex, Path.Combine(path, "WorldPreview_00_Elevation_Grayscale.png"));

                emit(LogLevel.INFO, LogContext.Driver, LogPhase.Export, "PNG",
                    $"Wrote WorldPreview_00_Elevation_Grayscale.png (min={min:F3}, max={max:F3})");
            }
            finally
            {
                UnityEngine.Object.Destroy(tex);
            }
        }

        public static void ExportPlatesPng(
            HB_ExportConfig exportConfig,
            int width,
            int height,
            WorldArrays arrays,
            LogEmitter emit,
            string runExportRoot = null)
        {
            if (exportConfig == null || arrays?.PlateId == null)
            {
                emit?.Invoke(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Missing plate data; skipping plates PNG.");
                return;
            }

            var path = GetExportPath(exportConfig, runExportRoot);
            Directory.CreateDirectory(path);

            var tex = new Texture2D(
                width * exportConfig.ExportTilePixelSize,
                height * exportConfig.ExportTilePixelSize,
                TextureFormat.RGBA32,
                false);

            try
            {
                FillTiled(tex, width, height, exportConfig, (x, y) =>
                {
                    int idx = y * width + x;
                    ushort id = arrays.PlateId[idx];
                    // Deterministic pseudo-color from id (no palette dependency)
                    float r = ((id * 37) % 255) / 255f;
                    float g = ((id * 91) % 255) / 255f;
                    float b = ((id * 17) % 255) / 255f;
                    return new Color(r, g, b, 1f);
                });

                if (exportConfig.ExportFlipVertical)
                    tex = FlipVertical(tex);

                SavePng(tex, Path.Combine(path, "WorldPreview_01_Plates.png"));
                emit?.Invoke(LogLevel.INFO, LogContext.Driver, LogPhase.Export, "PNG", "Wrote WorldPreview_01_Plates.png");
            

                // --- Debug exports (derived from PlateId) ---
                // PlateBoundary01: 1 where neighbor plate id differs, else 0.
                // PlateBoundaryDistance01: distance-to-boundary normalized to 0..1 (0 at boundary).
                try
                {
                    float[] boundary01 = BuildPlateBoundaryMask01(arrays.PlateId, width, height);
                    float[] boundaryDist01 = ComputeDistanceToMask01(boundary01, width, height, maxDistTiles: 256);

                    ExportFloat01Png(exportConfig, width, height, boundary01, emit, path, "WorldPreview_01_PlateBoundary01.png");
                    ExportFloat01Png(exportConfig, width, height, boundaryDist01, emit, path, "WorldPreview_01_PlateBoundaryDistance01.png");
                }
                catch (Exception ex)
                {
                    emit?.Invoke(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT",
                        $"Failed debug plate exports: {ex.Message}");
                }
}
            finally
            {
                UnityEngine.Object.Destroy(tex);
            }
        }

        public static void ExportUpliftPng(
            HB_ExportConfig exportConfig,
            int width,
            int height,
            WorldArrays arrays,
            LogEmitter emit,
            string runExportRoot = null)
        {
            if (exportConfig == null || arrays?.Uplift01 == null)
            {
                emit?.Invoke(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT", "Missing uplift data; skipping uplift PNG.");
                return;
            }

            var path = GetExportPath(exportConfig, runExportRoot);
            Directory.CreateDirectory(path);

            var tex = new Texture2D(
                width * exportConfig.ExportTilePixelSize,
                height * exportConfig.ExportTilePixelSize,
                TextureFormat.RGBA32,
                false);

            try
            {
                FillTiled(tex, width, height, exportConfig, (x, y) =>
                {
                    int idx = y * width + x;
                    float v = Mathf.Clamp01(arrays.Uplift01[idx]);
                    // Uplift -> bright red, background -> dark
                    return new Color(v, 0f, 0f, 1f);
                });

                if (exportConfig.ExportFlipVertical)
                    tex = FlipVertical(tex);

                SavePng(tex, Path.Combine(path, "WorldPreview_01_Uplift.png"));
                emit?.Invoke(LogLevel.INFO, LogContext.Driver, LogPhase.Export, "PNG", "Wrote WorldPreview_01_Uplift.png");
            

                // --- Debug exports (derived from LandMask01 + Uplift01) ---
                // LandGate01: SmoothStep(0.10..0.85) of LandMask01 (matches elevation uplift gating).
                // UpliftRaw01: uplift with land-gate removed (approx; still includes segmentation + uplift type).
                try
                {
                    if (arrays.LandMask01 != null && arrays.LandMask01.Length == width * height)
                    {
                        float[] landGate01 = BuildLandGate01(arrays.LandMask01);
                        ExportFloat01Png(exportConfig, width, height, landGate01, emit, path, "WorldPreview_01_LandGate01.png");

                        float[] upliftRaw01 = BuildUpliftUngated01(arrays.Uplift01, landGate01);
                        ExportFloat01Png(exportConfig, width, height, upliftRaw01, emit, path, "WorldPreview_01_UpliftRaw01.png");
                    }
                    else
                    {
                        emit?.Invoke(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT",
                            "LandMask01 missing; skipping LandGate01/UpliftRaw01 debug exports.");
                    }
                }
                catch (Exception ex)
                {
                    emit?.Invoke(LogLevel.WARN, LogContext.Driver, LogPhase.Export, "EXPORT",
                        $"Failed debug uplift exports: {ex.Message}");
                }
}
            finally
            {
                UnityEngine.Object.Destroy(tex);
            }
        }

        
        private static float[] GetElevationForExport(WorldArrays arrays)
        {
            if (arrays == null) return null;

            // Prefer the snapshot captured after Module 1 for export consistency.
            if (arrays.ElevationExport01 != null &&
                arrays.ElevationRaw != null &&
                arrays.ElevationExport01.Length == arrays.ElevationRaw.Length)
            {
                return arrays.ElevationExport01;
            }

            return arrays.ElevationRaw;
        }

private static string GetExportPath(HB_ExportConfig exportConfig, string runExportRoot)
        {
            if (!string.IsNullOrWhiteSpace(runExportRoot))
            {
                return Path.GetFullPath(runExportRoot);
            }


            // If ExportFolderName is an absolute path (e.g., starts with drive letter or /), use it directly.
            // Otherwise, use it as a subdirectory under the project's Logs folder.
            if (Path.IsPathRooted(exportConfig.ExportFolderName))
            {
                return Path.GetFullPath(exportConfig.ExportFolderName);
            }
            else
            {
                var root = Path.Combine(Application.dataPath, "..", "Logs");
                return Path.GetFullPath(Path.Combine(root, exportConfig.ExportFolderName));
            }
        }

        private static void SavePng(Texture2D tex, string filePath)
        {
            var bytes = tex.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);
        }

        private static void FillTiled(Texture2D tex, int width, int height, HB_ExportConfig cfg, Func<int, int, Color> colorAt)
        {
            int px = cfg.ExportTilePixelSize;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var c = colorAt(x, y);
                    int sx = x * px;
                    int sy = y * px;
                    for (int oy = 0; oy < px; oy++)
                    {
                        for (int ox = 0; ox < px; ox++)
                        {
                            tex.SetPixel(sx + ox, sy + oy, c);
                        }
                    }
                }
            }
            tex.Apply(false);
        }

        private static Color ColorForElevation(ElevationBandFinal band)
        {
            return band switch
            {
                ElevationBandFinal.DeepOcean => new Color(0.02f, 0.08f, 0.30f, 1f),
                ElevationBandFinal.Ocean => new Color(0.05f, 0.20f, 0.65f, 1f),
                ElevationBandFinal.Lowland => new Color(0.10f, 0.55f, 0.15f, 1f),
                ElevationBandFinal.Highlands => new Color(0.45f, 0.65f, 0.20f, 1f),
                ElevationBandFinal.LowMountains => new Color(0.55f, 0.55f, 0.55f, 1f),
                ElevationBandFinal.HighMountains => new Color(0.90f, 0.90f, 0.90f, 1f),
                _ => Color.magenta
            };
        }

        private static Color ColorForLatitude(LatitudeBandType band)
        {
            return band switch
            {
                LatitudeBandType.Arctic => new Color(1.0f, 1.0f, 1.0f, 1f),
                LatitudeBandType.Temperate => new Color(0.2f, 0.8f, 0.2f, 1f),
                LatitudeBandType.Tropical => new Color(1.0f, 0.9f, 0.2f, 1f),
                _ => Color.magenta
            };
        }

        private static Color ColorForLatitudeEnergy(float latitudeEnergy01)
        {
            float v = Mathf.Clamp01(latitudeEnergy01);
            return new Color(v, v, v, 1f);
        }

        private static Color ColorForCoast(WorldArrays arrays, int x, int y, int width)
        {
            int idx = y * width + x;

            bool isOcean = arrays.IsOcean != null && arrays.IsOcean[idx];

            // Inland seas (edge-disconnected ocean components), if provided
            if (isOcean && arrays.IsInlandLake != null && arrays.IsInlandLake[idx])
            {
                // Cyan tint to make inland seas obvious
                return new Color(0.35f, 0.70f, 0.90f, 1f);
            }

            if (isOcean)
            {
                // Deep ocean
                if (arrays.IsDeepOcean != null && arrays.IsDeepOcean[idx])
                    return new Color(0.02f, 0.08f, 0.25f, 1f);

                // Coastal shelf
                if (arrays.IsCoastalShelf != null && arrays.IsCoastalShelf[idx])
                    return new Color(0.20f, 0.45f, 0.90f, 1f);

                // Regular ocean
                return new Color(0.05f, 0.20f, 0.55f, 1f);
            }

            // Land: render from authoritative elevation bands so it matches WorldPreview_01_ElevationBands.png
            if (arrays.ElevationBands != null && arrays.ElevationBands.Length > idx)
            {
                return ColorForElevation((ElevationBandFinal)arrays.ElevationBands[idx]);
            }

            // Fallback
            return new Color(0.50f, 0.50f, 0.50f, 1f);
        }

        /// <summary>
        /// Color mapping for hydrology visualization.
        /// </summary>
        private static Color ColorForHydrology(WorldArrays arrays, int x, int y, int width)
        {
            int idx = y * width + x;

            // Lakes (bright blue)
            if (arrays.IsLake != null && arrays.IsLake[idx])
            {
                return new Color(0.3f, 0.6f, 0.95f, 1f);
            }

            // Rivers by type
            if (arrays.RiverTypes != null)
            {
                switch (arrays.RiverTypes[idx])
                {
                    case RiverType.MajorRiver:
                        return new Color(0.1f, 0.3f, 0.8f, 1f);  // Dark blue
                    case RiverType.River:
                        return new Color(0.2f, 0.4f, 0.85f, 1f); // Medium blue
                    case RiverType.Creek:
                        return new Color(0.3f, 0.5f, 0.9f, 1f);  // Light blue
                    case RiverType.Stream:
                        return new Color(0.4f, 0.6f, 0.95f, 1f); // Very light blue
                }
            }

            // Base terrain (use elevation for context)
            if (arrays.ElevationBands != null)
            {
                return ColorForElevation((ElevationBandFinal)arrays.ElevationBands[idx]);
            }

            return new Color(0.5f, 0.5f, 0.5f, 1f); // Gray fallback
        }

        /// <summary>
        /// Color mapping for basin visualization (Phase 2 - before rivers).
        /// Shows basins colored by elevation classification.
        /// </summary>
        private static Color ColorForBasins(WorldArrays arrays, int x, int y, int width)
        {
            int idx = y * width + x;

            // If in a basin (lake)
            if (arrays.IsLake != null && arrays.IsLake[idx])
            {
                var src = GetElevationForExport(arrays);
                float elev = (src != null && idx < src.Length) ? src[idx] : 0f;
                float oceanLevel = 0.15f; // Approximate ocean level
                
                // Check lowest edge elevation if available
                // For now, use tile elevation as approximation
                if (elev <= oceanLevel)
                {
                    // Below ocean - terminal basin (Dead Sea yellow-blue tint)
                    return new Color(0.4f, 0.6f, 0.7f, 1f);
                }
                else
                {
                    // Above ocean - will drain (light cyan-blue)
                    return new Color(0.5f, 0.8f, 0.95f, 1f);
                }
            }

            // Not in basin - show elevation for context
            if (arrays.ElevationBands != null)
            {
                return ColorForElevation((ElevationBandFinal)arrays.ElevationBands[idx]);
            }

            return new Color(0.5f, 0.5f, 0.5f, 1f); // Gray fallback
        }

        
        // =====================================================================================
        // Debug helpers (derived exports; no new WorldArrays fields required)
        // =====================================================================================

        private static float[] BuildPlateBoundaryMask01(ushort[] plateId, int width, int height)
        {
            int n = width * height;
            var boundary01 = new float[n];

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;
                ushort id = plateId[idx];

                bool boundary = false;
                if (x > 0 && plateId[idx - 1] != id) boundary = true;
                else if (x < width - 1 && plateId[idx + 1] != id) boundary = true;
                else if (y > 0 && plateId[idx - width] != id) boundary = true;
                else if (y < height - 1 && plateId[idx + width] != id) boundary = true;

                boundary01[idx] = boundary ? 1f : 0f;
            }

            return boundary01;
        }

        /// <summary>
        /// Compute distance to a 0/1 mask (mask=1 are "sources").
        /// Returns 0..1 where 0 means on-mask (distance 0) and 1 means >= maxDistTiles away.
        /// </summary>
        private static float[] ComputeDistanceToMask01(float[] mask01, int width, int height, int maxDistTiles)
        {
            int n = width * height;
            const int INF = 1_000_000;

            var dist = new int[n];
            for (int i = 0; i < n; i++) dist[i] = INF;

            var q = new System.Collections.Generic.Queue<int>(Mathf.Max(32, n / 32));

            // init with sources
            for (int i = 0; i < n; i++)
            {
                if (mask01[i] >= 0.5f)
                {
                    dist[i] = 0;
                    q.Enqueue(i);
                }
            }

            // BFS in 4-neighborhood
            while (q.Count > 0)
            {
                int idx = q.Dequeue();
                int x = idx % width;
                int y = idx / width;

                int d0 = dist[idx];
                int d1 = d0 + 1;
                if (d1 > maxDistTiles) continue;

                Visit(x - 1, y);
                Visit(x + 1, y);
                Visit(x, y - 1);
                Visit(x, y + 1);

                void Visit(int nx, int ny)
                {
                    if ((uint)nx >= (uint)width || (uint)ny >= (uint)height) return;
                    int ni = ny * width + nx;
                    if (dist[ni] <= d1) return;
                    dist[ni] = d1;
                    q.Enqueue(ni);
                }
            }

            float inv = 1f / Mathf.Max(1, maxDistTiles);
            var out01 = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (dist[i] >= INF) ? 1f : Mathf.Clamp01(dist[i] * inv);
                out01[i] = t;
            }
            return out01;
        }

        private static float[] BuildLandGate01(float[] landMask01)
        {
            int n = landMask01.Length;
            var gate = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = Mathf.InverseLerp(0.10f, 0.85f, Mathf.Clamp01(landMask01[i]));
                // SmoothStep
                t = t * t * (3f - 2f * t);
                gate[i] = t;
            }
            return gate;
        }

        /// <summary>
        /// Approximate "raw uplift" by removing the land gate (but not segmentation / uplift-type scaling).
        /// </summary>
        private static float[] BuildUpliftUngated01(float[] uplift01, float[] landGate01)
        {
            int n = uplift01.Length;
            var raw = new float[n];
            for (int i = 0; i < n; i++)
            {
                float g = Mathf.Max(1e-6f, landGate01[i]);
                raw[i] = Mathf.Clamp01(uplift01[i] / g);
            }
            return raw;
        }

        private static void ExportFloat01Png(
            HB_ExportConfig exportConfig,
            int width,
            int height,
            float[] values01,
            LogEmitter emit,
            string exportPath,
            string fileName)
        {
            var tex = new Texture2D(
                width * exportConfig.ExportTilePixelSize,
                height * exportConfig.ExportTilePixelSize,
                TextureFormat.RGBA32,
                false);

            try
            {
                FillTiled(tex, width, height, exportConfig, (x, y) =>
                {
                    float v = Mathf.Clamp01(values01[(y * width) + x]);
                    return new Color(v, v, v, 1f);
                });

                if (exportConfig.ExportFlipVertical)
                    tex = FlipVertical(tex);

                SavePng(tex, Path.Combine(exportPath, fileName));
                emit?.Invoke(LogLevel.INFO, LogContext.Driver, LogPhase.Export, "PNG", $"Wrote {fileName}");
            }
            finally
            {
                UnityEngine.Object.Destroy(tex);
            }
        }

private static Texture2D FlipVertical(Texture2D input)
        {
            int w = input.width;
            int h = input.height;
            var output = new Texture2D(w, h, input.format, false);
            for (int y = 0; y < h; y++)
            {
                output.SetPixels(0, y, w, 1, input.GetPixels(0, h - 1 - y, w, 1));
            }
            output.Apply(false);
            UnityEngine.Object.Destroy(input);
            return output;
        }
    }
}
