using System;
using UnityEngine;

namespace MapMaker.Modules.Elevation1.Config
{
    /// <summary>
    /// Elevation Step 1: Macro geology scaffolding (macro-first).
    ///
    /// Design intent: 
    ///   - Scales are specified in MILES and internally converted to tiles/pixels using TileSizeMiles.
    ///   - Counts are specified as DENSITIES (per million square miles) and derived from map area.
    ///
    /// Outputs authored by 1_Elevation:
    ///   - LandMask01, PlateId, Uplift01, Ruggedness01, ElevationRaw
    ///
    /// Notes:
    ///   - Hydrology (lakes/rivers) is NOT authored here, but we provide basins (negative relief)
    ///     so downstream hydrology can fill interior lakes.
    ///   - Bathymetry isn't a dedicated module yet; we approximate shelf depth using distance-to-coast
    ///     on a coarse distance grid so Ocean isn't a flat blob.
    /// </summary>
    [CreateAssetMenu(fileName = "HB_Elevation_Config", menuName = "MapMaker/Module Configs/1 - Elevation Config", order = 1)]
    public sealed class HB_ElevationConfig : ScriptableObject
    {
        [Header("Global Units")]
        [Min(0.01f)]
        [Tooltip("Miles represented by a single tile/pixel. In current pipeline, 1 tile ~= 1 mile.")]
        public float TileSizeMiles = 1f;

        [Header("Sea Level")]
        [Range(0.05f, 0.60f)]
        [Tooltip("Fraction of the map that should be ocean (sea level is set by percentile).")]
        public float OceanPercent = 0.18f;

        [Tooltip("If enabled, any tiles below sea level that are not connected to the map edge are lifted above sea level before banding.\nThis enforces: no inland seas after Module 1 (use basins/rivers later for inland water).")]
        public bool NoInlandWaterAfterElevation = true;

        [Header("Macro Landmass")]
        [Min(2f)]
        [Tooltip("Coarse grid cell size used to build the crust field (in miles). Smaller = more coastline detail, slower.")]
        public float MacroCellSizeMiles = 12f;

        [Range(0.10f, 6.0f)]
        [Tooltip("Macro crust origins per million square miles. Derived into a count based on map area.")]
        public float CrustOriginDensity = 1.2f;

        [Min(10f)]
        [Tooltip("Typical major-axis size of crust origins (miles).")]
        public float CrustOriginMajorAxisMiles = 280f;
        [Min(10f)]
        [Tooltip("Typical minor-axis size of crust origins (miles).")]
        public float CrustOriginMinorAxisMiles = 160f;

        [Range(0f, 1f)]
        [Tooltip("Random variation applied to origin axes (0 = fixed, 1 = wide variance).")]
        public float CrustOriginAxisJitter = 0.35f;

        [Range(0f, 1f)]
        [Tooltip("Bias origin centers toward the map center (0 = uniform, 1 = strong center pull).")]
        public float CrustCenterPull = 0.35f;

        [Header("Macro Warping")]
        [Range(0f, 1f)]
        [Tooltip("Domain warp strength applied when sampling the crust field.")]
        public float MacroWarpStrength = 0.35f;

        [Min(10f)]
        [Tooltip("Domain warp wavelength (miles).")]
        public float MacroWarpScaleMiles = 220f;

        [Header("Coastline Complexity")]
        [Range(0.001f, 0.35f)]
        [Tooltip("Coastal transition width around sea level (miles).")]
        public float CoastFadeMiles = 10f;

        [Range(0f, 1f)]
        [Tooltip("How strongly to carve bays/peninsulas near sea level.")]
        public float CoastCarveStrength = 0.35f;

        [Min(5f)]
        [Tooltip("Coast carve noise wavelength (miles).")]
        public float CoastCarveScaleMiles = 60f;

        [Header("Islands / Archipelagos")]
        [Range(0f, 0.30f)]
        [Tooltip("Additional island land as a fraction of MAIN land area.")]
        public float IslandFractionOfLand = 0.08f;

        [Min(0f)]
        [Tooltip("Max distance from the main landmass where islands are allowed (miles).")]
        public float MaxIslandDistanceFromMainMiles = 160f;

        [Min(1f)]
        [Tooltip("Max area for a single island component (square miles). Oversized components are removed.")]
        public float MaxIslandAreaMiles2 = 6000f;

        [Range(0f, 1f)]
        [Tooltip("0 = scattered islands, 1 = clustered chains.")]
        public float ArchipelagoClustering = 0.5f;

        [Header("Optional Edge Ocean")]
        [Min(0f)]
        [Tooltip("If > 0, biases edges toward ocean within this distance (miles). Default 0 = disabled.")]
        public float EdgeOceanWidthMiles = 0f;

        [Header("Plates / Mountain Belts")]
        [Range(0.10f, 10.0f)]
        [Tooltip("Plate seeds per million square miles.")]
        public float PlateDensity = 2.0f;

        [Min(1f)]
        public float PlateMinSeparationMiles = 120f;

        [Range(0f, 5f)] public float PlateSpeedMin = 0.35f;
        [Range(0f, 5f)] public float PlateSpeedMax = 1.15f;

        [Min(1f)]
        [Tooltip("Belt width around plate sutures (miles).")]
        public float BoundaryWidthMiles = 80f;

        [Range(0f, 1f)]
        [Tooltip("Segmentation/breaks along strike (0 disables).")]
        public float BoundarySegmentation = 0.45f;

        [Min(5f)]
        public float BoundarySegmentationScaleMiles = 140f;

        [Range(0f, 1f)]
        public float BoundaryConvergenceEpsilon = 0.15f;

        [Header("Plate Boundary Warping")]
        [Range(0f, 1f)]
        [Tooltip("Domain warp applied to plate assignment / boundary distance sampling. This prevents straight Voronoi edges from imprinting into uplift/elevation. 0 disables.")]
        public float PlateWarpStrength = 0.35f;

        [Min(1f)]
        [Tooltip("Warp amplitude in miles (converted to UV offset). Larger values create more irregular plate boundaries.")]
        public float PlateWarpAmplitudeMiles = 120f;

        [Min(5f)]
        [Tooltip("Warp wavelength in miles. Larger values = gentler, continent-scale bending; smaller values can over-fragment belts.")]
        public float PlateWarpScaleMiles = 650f;

        [Range(1, 4)]
        [Tooltip("Warp octaves (1-4). 2 is usually enough; higher values add smaller wiggles.")]
        public int PlateWarpOctaves = 2;

        [Range(0f, 2f)] public float ConvergentUpliftStrength = 0.95f;
        [Range(0f, 2f)] public float DivergentUpliftStrength = 0.18f;
        [Range(0f, 2f)] public float TransformUpliftStrength = 0.45f;

        [Header("Elevation Composition")]
        [Tooltip("Magnitude of ocean base depth (applied as negative). Units are relative within your pipeline.")]
        public float OceanBaseDepth = 0.65f;

        [Tooltip("Base land height (before mountains/plateaus/noise).")]
        public float LandBaseHeight = 0.10f;

        [Tooltip("Added height for mountains (uplift belts).")]
        public float MountainHeight = 1.25f;

        [Tooltip("Added height for continental interiors/plateaus.")]
        public float PlateauHeight = 0.45f;

        [Min(0.2f)]
        public float PlateauPower = 1.55f;

        [Header("Broad Interior Relief")]
        [Range(0f, 1f)] public float RegionalReliefStrength = 0.35f;
        public float RegionalReliefHeight = 0.22f;
        [Min(10f)] public float RegionalReliefScaleMiles = 240f;

        [Header("Detail Relief")]
        [Range(0f, 1f)] public float DetailReliefStrength = 0.22f;
        public float DetailReliefHeight = 0.08f;
        [Min(1f)] public float DetailReliefScaleMiles = 22f;

        [Header("Step 7 - Micro Relief")]
        [Tooltip("Adds subtle small-scale variation after macro elevation + basins. Masked to land.")]
        public bool MicroReliefEnabled = true;

        [Range(0f, 1f)]
        [Tooltip("Overall micro-relief intensity (0..1).")]
        public float MicroReliefStrength = 0.25f;

        [Tooltip("Micro-relief amplitude in normalized elevation units.")]
        public float MicroReliefHeight = 0.030f;

        [Min(1f)]
        [Tooltip("Micro-relief noise scale in miles (smaller = more local variation).")]
        public float MicroReliefScaleMiles = 10f;

        [Header("Step 8 - Final Preparation")]
        [Tooltip("Runs a final clamp/remap with stability checks (no smoothing).")]
        public bool FinalPrepEnabled = true;

        [Tooltip("If true, remap final elevation to 0..1 after clamp.")]
        public bool FinalPrepRemap01 = false;

        [Header("Step 5: Relief Coherence")]
        [Tooltip("Spreads mountain uplift outward to create foothills and reduce harsh mountain-to-lowland edges. Applied AFTER composition and BEFORE conditioning/banding.")]
        public bool ReliefCoherenceEnabled = true;

        [Range(0f, 1f)]
        [Tooltip("Strength of the coherence blend. Typical 0.10–0.30.")]
        public float ReliefCoherenceStrength = 0.18f;

        [Min(10f)]
        [Tooltip("Coherence radius (miles). Larger values create broader foothills; too large can soften macro relief. Typical 80–200.")]
        public float ReliefCoherenceRadiusMiles = 140f;

        [Header("Basins (Negative Relief for Lakes)")]
        [Range(0f, 10.0f)]
        [Tooltip("Basin count per million square miles (converted to a count by map area).")]
        public float BasinDensity = 1.8f;

        [Min(5f)]
        [Tooltip("Typical basin radius scale (miles).")]
        public float BasinScaleMiles = 160f;

        [Range(0f, 1f)]
        [Tooltip("How strongly basins depress interior land.")]
        public float BasinStrength = 0.55f;

        [Range(0f, 1f)]
        [Tooltip("Adds a subtle rim around basins (helps create closed basins).")]
        public float BasinRimStrength = 0.35f;

        [Header("Optional Continental Tilt")]
        public Vector2 ContinentalTiltDirection = new Vector2(0.7f, -0.2f);
        public float ContinentalTiltStrength = 0.00f;

        [Header("Ocean Shelf / Bathymetry")]
        [Min(0f)]
        [Tooltip("Distance from coast treated as continental shelf (miles). 0 disables shelf shaping.")]
        public float ShelfWidthMiles = 25f;

        [Range(0.8f, 4f)]
        [Tooltip("Controls how quickly the shelf drops to deep ocean.")]
        public float OceanDepthCurvePower = 1.8f;

[Header("Step 3: Field Conditioning")]
[Tooltip("If enabled, applies post-compose conditioning: soft clamp + optional remap + very-low-frequency smoothing. This should NOT change macro geography.")]
public bool ConditioningEnabled = true;

[Range(0f, 0.05f)]
[Tooltip("Symmetric percentile used for soft-clamping extremes. 0.01 means clamp below P1 and above P99 before smoothing/remap.")]
public float ConditioningClampPercent = 0.01f;

[Tooltip("If enabled, remaps the clamped elevation range into a stable target range derived from this config (ocean depth to typical land+mountain height).")]
public bool ConditioningRemapToConfigRange = true;

[Range(0f, 0.25f)]
[Tooltip("Blend strength for macro-safe smoothing. 0.05–0.12 recommended. Higher values will change coastlines and macro structure.")]
public float ConditioningSmoothingStrength = 0.08f;

[Min(10f)]
[Tooltip("Approximate smoothing radius (miles). This is applied on a coarse grid and upsampled, so it stays macro-safe and fast.")]
public float ConditioningSmoothingRadiusMiles = 180f;

[Min(4f)]
[Tooltip("Coarse cell size (miles) used for the smoothing grid. Larger = faster and more macro-safe. 12–24 recommended.")]
public float ConditioningSmoothingCellMiles = 16f;

[Header("Step 3b: Land Contrast / Variance")]
[Tooltip("If enabled, applies a non-linear contrast curve to LAND elevations after Step 3 conditioning. Oceans are left unchanged.\n\nPurpose: expand the usable elevation range so banding/sea-level thresholds have stronger gradients to work with.")]
public bool EnableLandContrast = true;

[Range(0f, 1f)]
[Tooltip("0 = no change, 1 = full S-curve (more values pushed toward low/high).")]
public float LandContrastStrength01 = 0.55f;

[Range(0.25f, 2.0f)]
[Tooltip("Gamma applied after the S-curve. < 1 brightens mid/lows, > 1 darkens mid/highs.")]
public float LandContrastGamma = 0.9f;

[Range(0f, 1f)]
[Tooltip("Land mask threshold used to decide which tiles participate in the land contrast remap.")]
public float LandContrast_LandMaskThreshold01 = 0.5f;

        [Header("Debug")]
public bool DebugEnabled = false;

        [Header("Step 5: Terrain Derivatives")]
        [Tooltip("Compute slope + distance-to-coast fields for downstream modules / rendering.")]
        public bool DerivativesEnabled = true;

        [Tooltip("Distance (miles) at which CoastDistance01 saturates to 1.0.")]
        public float CoastDistanceMaxMiles = 300f;

        [Tooltip("Multiplier used to map local elevation gradient to Slope01.")]
        public float SlopeScale = 12f;

        // These are used by ElevationBandAssigner and/or preview output. Keep them stable.
        [Header("Banding / Preview")]
        [Range(0.10f, 0.60f)]
        public float DeepOceanShareWithinOcean = 0.42f;

        [Range(0.02f, 0.25f)]
        public float HighMountainsPercentOfLand = 0.08f;

        [Range(0.03f, 0.35f)]
        public float LowMountainsPercentOfLand = 0.16f;

        [Range(0.05f, 0.50f)]
        public float HighlandsPercentOfLand = 0.22f;

        /// <summary>
        /// Derived convenience: land percent = 1 - OceanPercent.
        /// </summary>
        public float LandPercent => 1f - Mathf.Clamp01(OceanPercent);

        private void OnValidate()
        {
            TileSizeMiles = Mathf.Max(0.01f, TileSizeMiles);
            MacroCellSizeMiles = Mathf.Max(2f, MacroCellSizeMiles);
            CoastFadeMiles = Mathf.Max(0.001f, CoastFadeMiles);
            MacroWarpScaleMiles = Mathf.Max(10f, MacroWarpScaleMiles);
            CoastCarveScaleMiles = Mathf.Max(5f, CoastCarveScaleMiles);
            PlateMinSeparationMiles = Mathf.Max(1f, PlateMinSeparationMiles);
            BoundaryWidthMiles = Mathf.Max(1f, BoundaryWidthMiles);
            BoundarySegmentationScaleMiles = Mathf.Max(5f, BoundarySegmentationScaleMiles);
            BasinScaleMiles = Mathf.Max(5f, BasinScaleMiles);
            RegionalReliefScaleMiles = Mathf.Max(10f, RegionalReliefScaleMiles);
            DetailReliefScaleMiles = Mathf.Max(1f, DetailReliefScaleMiles);
            ConditioningClampPercent = Mathf.Clamp(ConditioningClampPercent, 0f, 0.05f);
            ConditioningSmoothingStrength = Mathf.Clamp(ConditioningSmoothingStrength, 0f, 0.25f);
            ConditioningSmoothingRadiusMiles = Mathf.Max(10f, ConditioningSmoothingRadiusMiles);
            ConditioningSmoothingCellMiles = Mathf.Max(4f, ConditioningSmoothingCellMiles);
            ShelfWidthMiles = Mathf.Max(0f, ShelfWidthMiles);
            OceanDepthCurvePower = Mathf.Clamp(OceanDepthCurvePower, 0.8f, 4f);
        }
    }
}
