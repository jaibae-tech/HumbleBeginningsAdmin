# Module 1 Spec — Elevation

## Purpose

Module 1 generates the world’s **continuous elevation field** plus supporting fields (plates/uplift/derivatives) used by later modules.

This module is **scale-aware**:
- Feature sizes are expressed in **miles**.
- Seed counts are expressed as **densities** (per million square miles) and derived per-map.

## Inputs

- `HB_PipelineConfig` (width/height, seed, export toggles)
- `HB_ElevationConfig` (all tunable knobs for Module 1)

## Outputs (WorldArrays)

- `ElevationRaw : float[]`
- `ElevationBands : ElevationBandFinal[]`
- `LandMask01 : float[]`
- `PlateId : ushort[]`
- `Uplift01 : float[]`
- `Ruggedness01 : float[]`
- `IsOcean / IsDeepOcean / IsCoastalShelf : bool[]`
- `Slope01 / Aspect01 / Curvature01 / CoastDistance01 : float[]`

## Core pipeline (steps 1–8)

1. Macro scaffolding (land mask + plates + uplift)
2. Coastline resolution (sea-level percentile + fade/carve + shelf)
3. Field conditioning (clamp/remap + macro-safe smoothing)
4. Ocean connectivity (remove inland seas; no inland water after Module 1)
5. Relief coherence (foothills; reduce harsh adjacency)
6. Basin embedding (depressions + rims; hydrology scaffolding)
7. Micro-relief (small variation; update ruggedness)
8. Final preparation (derivatives + stability + exports)

---

## HB_ElevationConfig knobs

**Ranges below are guidance** (not enforced unless attributes exist in code). Defaults are the values currently in `HB_ElevationConfig.cs`.

### World scale

- **TileSizeMiles** (default **1.0**, suggested **0.25–2.0**)
  - Miles per tile. Changing this changes the world’s real-world scale while keeping tile resolution fixed.

- **MacroCellSizeMiles** (default **12**, suggested **8–24**)
  - Controls the coarse crust grid resolution.
  - Lower = more detail in macro structure; higher = smoother, larger features.

### Sea / coast

- **OceanPercent** (default **0.18**, suggested **0.15–0.35**)
  - Percent of tiles classified as ocean via percentile sea-level.
  - Higher = more water, smaller continents.

- **NoInlandWaterAfterElevation** (default **true**)
  - Forces all ocean tiles to connect to the map edge by lifting inland seas above sea level.

- **EdgeOceanWidthMiles** (default **0**, suggested **0–200**)
  - Optional edge bias for open-ocean framing.
  - 0 means land may touch edges.

- **CoastFadeMiles** (default **10**, suggested **0–25**)
  - Softens elevation near sea-level to create a gentler coastal transition.

- **CoastCarveStrength** (default **0.35**, suggested **0–0.45**)
  - Carves bays/peninsulas into the coastline.

- **CoastCarveScaleMiles** (default **60**, suggested **40–300**)
  - Larger = broader bays/peninsulas; smaller = more jagged micro-coast.

- **ShelfWidthMiles** (default **25**, suggested **10–80**)
  - Continental shelf width before the drop to deep ocean.

- **OceanDepthCurvePower** (default **1.8**, suggested **1.2–2.4**)
  - Higher = sharper shelf break / faster deepening; lower = smoother bathymetry.

### Macro landmass shaping

- **CrustOriginDensity** (default **1.2**, suggested **0.4–2.0**)
  - Number of macro land origins per million square miles.
  - Lower = fewer, larger continents; higher = more fragmented land.

- **CrustOriginMajorAxisMiles** (default **280**, suggested **120–500**)
- **CrustOriginMinorAxisMiles** (default **160**, suggested **80–350**)
  - Size of macro land “blobs” used to seed the crust.

- **CrustOriginAxisJitter** (default **0.35**, suggested **0–0.6**)
  - Adds irregularity to blob shapes (higher = less circular).

- **CrustCenterPull** (default **0.35**, suggested **0–0.6**)
  - Pulls landmass inward (helps avoid edge-hugging).
  - Too high can create overly centered continents.

- **MacroWarpStrength** (default **0.35**, suggested **0–0.5**)
- **MacroWarpScaleMiles** (default **220**, suggested **120–700**)
  - Warps the macro crust field.
  - Strength controls intensity; scale controls the size of warps.

- **ContinentalTiltDirection** (default **(0.7, -0.2)**)
- **ContinentalTiltStrength** (default **0.0**, suggested **0–0.25**)
  - Optional global tilt (can bias elevation N/S or along a prevailing direction).

### Islands

- **IslandFractionOfLand** (default **0.08**, suggested **0–0.12**)
  - Target fraction of land area that may become islands.

- **MaxIslandDistanceFromMainMiles** (default **160**, suggested **60–350**)
  - How far islands may spawn from the main landmass.

- **MaxIslandAreaMiles2** (default **6000**, suggested **200–12000**)
  - Caps individual island size.

- **ArchipelagoClustering** (default **0.5**, suggested **0–1**)
  - Higher clusters islands into archipelagos.

### Plates and uplift

- **PlateDensity** (default **2.0**, suggested **0.5–6.0**)
  - Plates per million square miles.
  - Higher = more boundaries, more ranges (can get noisy if too high).

- **PlateMinSeparationMiles** (default **120**, suggested **60–220**)
  - Prevents plate seeds from clustering.

- **PlateSpeedMin / PlateSpeedMax** (defaults **0.35 / 1.15**, suggested **0–2.0**)
  - Speed controls relative boundary intensity.

- **BoundaryWidthMiles** (default **80**, suggested **40–260**)
  - Width of tectonic boundary influence.
  - Wider = broader mountain belts; narrower = sharper ridges.

- **BoundarySegmentation** (default **0.45**, suggested **0–0.9**)
- **BoundarySegmentationScaleMiles** (default **140**, suggested **80–400**)
  - Breaks continuous ranges into segments.

- **BoundaryConvergenceEpsilon** (default **0.15**, suggested **0.05–0.25**)
  - Threshold to decide if boundary is convergent/divergent/transform.

- **ConvergentUpliftStrength** (default **0.95**, suggested **0.2–1.4**)
- **DivergentUpliftStrength** (default **0.18**, suggested **0–0.6**)
- **TransformUpliftStrength** (default **0.45**, suggested **0–0.9**)
  - Raise/lower strength by boundary type.

### Elevation composition

- **OceanBaseDepth** (default **0.65**, suggested **0.3–0.8**)
  - Sets baseline ocean depth (affects grayscale/shaded relief, not ocean extent).

- **LandBaseHeight** (default **0.10**, suggested **0–0.6**)
  - Raises overall land baseline.

- **MountainHeight** (default **1.25**, suggested **0.6–1.6**)
  - Peak height multiplier.

- **PlateauHeight** (default **0.45**, suggested **0.1–0.6**)
- **PlateauPower** (default **1.55**, suggested **1.0–2.2**)
  - Shapes mid/high elevation distribution.

### Relief noise

- **RegionalReliefStrength / Height / ScaleMiles** (defaults **0.35 / 0.22 / 240**)
  - Broad regional variation.
  - Increase strength/height for more rolling continents; decrease if too blobby.

- **DetailReliefStrength / Height / ScaleMiles** (defaults **0.22 / 0.08 / 22**)
  - Adds local variation.
  - If the map becomes speckled, increase scale and/or reduce strength.

### Step 5 — Relief coherence (foothills)

- **ReliefCoherenceEnabled** (default **true**)
- **ReliefCoherenceStrength** (default **0.18**, suggested **0–0.35**)
- **ReliefCoherenceRadiusMiles** (default **140**, suggested **60–240**)
  - Spreads uplift influence outward to form foothills and reduce mountain→lowland cliffs.

### Step 6 — Basin embedding (hydrology scaffolding)

- **BasinDensity** (default **1.8**, suggested **0–3.0**)
- **BasinScaleMiles** (default **160**, suggested **60–260**)
- **BasinStrength** (default **0.55**, suggested **0–0.8**)
- **BasinRimStrength** (default **0.35**, suggested **0–0.6**)
  - Creates gentle depressions + rims. If you see sharp circular pits, reduce strength or increase scale.

### Step 7 — Micro-relief

- **MicroReliefEnabled** (default **true**)
- **MicroReliefStrength** (default **0.25**, suggested **0–0.4**)
- **MicroReliefHeight** (default **0.030**, suggested **0–0.06**)
- **MicroReliefScaleMiles** (default **10**, suggested **6–30**)
  - Small variation without changing continent shape.

### Step 3 — Conditioning (clamp/remap/smooth)

- **ConditioningEnabled** (default **true**)
- **ConditioningClampPercent** (default **0.01**, suggested **0–0.02**)
- **ConditioningRemapToConfigRange** (default **true**)
- **ConditioningSmoothingStrength** (default **0.08**, suggested **0–0.15**)
- **ConditioningSmoothingRadiusMiles** (default **180**, suggested **80–260**)
- **ConditioningSmoothingCellMiles** (default **16**, suggested **8–32**)
  - Conditioning is the most likely place to accidentally change continent shape. Keep strength low.

### Derivatives + debug

- **DerivativesEnabled** (default **true**)
  - Computes Slope/Aspect/Curvature/CoastDistance.

- **CoastDistanceMaxMiles** (default **300**, suggested **200–800**)
  - Normalization cap for CoastDistance01.

- **SlopeScale** (default **12**, suggested **6–20**)
  - Affects slope normalization sensitivity.

- **DebugEnabled** (default **false**)
  - When enabled, logs both raw knobs and derived counts/scales at module start.

### Band targets

These are used when assigning `ElevationBandFinal` from the continuous elevation field:
- **DeepOceanShareWithinOcean** (default **0.42**, suggested **0.25–0.55**)
- **HighMountainsPercentOfLand** (default **0.08**, suggested **0.04–0.12**)
- **LowMountainsPercentOfLand** (default **0.16**, suggested **0.08–0.24**)
- **HighlandsPercentOfLand** (default **0.22**, suggested **0.12–0.30**)

---

## Validation checklist

- CoastDistance shows a coastline gradient (not fully black).
- No ocean tiles appear inland (if `NoInlandWaterAfterElevation=true`).
- Mountain belts have foothills after Step 5.
- Basins appear as broad shallow depressions (not pits) after Step 6.
- Micro-relief adds texture without speckling.

