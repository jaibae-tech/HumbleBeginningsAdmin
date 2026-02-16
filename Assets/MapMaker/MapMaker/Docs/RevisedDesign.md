Updated generation pipeline (12 stages) aligned to your needs

This is “full scope but implementable.” It adds what’s missing (Landmass Topology + Run Context + Contracts), and keeps the module style you already have.

For each stage: Inputs, Outputs (WorldData fields), Artifacts, and Not trying to create.

Stage 0 — Pipeline Run Context (Driver-owned)

Goal: create a stable, run-scoped execution context before any module or logging system initializes.

Inputs:
HB_MapConfig.RootSeed
System time (UTC)
HB_ExportConfig.ExportFolderName (base export root)

Outputs (WorldData / Run State):

RunId = <seed>_<timestamp> (string)
RootSeed
TimestampUtc

Artifacts / Directories Created:

<ExportRoot>/<RunId>/
    Logs/
    WorldData/
    Bake/

All exported files for the run MUST reside within this directory tree.

Logging Contract (Critical):

Driver MUST call:

MapMakerLogBinder.SetRuntimeLogFolder(<ExportRoot>/<RunId>/Logs)

This MUST occur BEFORE emitter binding.

No fallback log locations are permitted.

Artifacts Produced:

<RunId>/Logs/<LogFile>
<RunId>/WorldData/...
<RunId>/*.png

Not trying to create:

Terrain
Elevation
Any derived world layers

Implementation Constraints:

Stage 0 executes before any module
Stage 0 executes before logging binder
HB_ExportConfig.ExportFolderName is never mutated at runtime
If run export or log folder is undefined → THROW

Stage 1 — Landmass Topology (NEW module, before Elevation)

Goal: enforce edge-ocean, land coverage %, directional coast, and “no cut-off islands.”

Inputs: HB_MapConfig (size/seed), new HB_TopologyConfig:

WorldMode (ALL / DIRECTIONAL)

OriginSide (if directional)

EdgeOceanWidthTiles

TargetLandCoverage01

TopologyStyle (pangaea/archipelago/etc.)

Outputs (WorldData fields):

LandMask (u8 0/1)

DistanceToEdge01 (optional but extremely useful)

Artifacts:

Bake/WorldPreview_00_LandMask.png

Bake/WorldPreview_00_DistanceToEdge.png (optional)

Not trying to create: elevation, mountains, rivers

Why it matters: you cannot reliably achieve “maximize land + edge ocean + directional coast” by tweaking elevation noise. This stage makes those constraints deterministic.

Stage 2 — Base Elevation (your existing Elevation module, but constrained by LandMask)

Goal: generate a believable macro heightfield that respects topology.

Inputs: LandMask, HB_ElevationConfig noise + ridge knobs

Outputs (WorldData fields):

ElevationRaw (f32)

ElevationBands (existing)

SeaLevel01 must be computed and stored (see “fix” below)

Artifacts:

Existing elevation band previews

Add: Bake/WorldPreview_01_ElevationRaw.png (optional)

Not trying to create: coherent mountain chains as systems; that’s next

Your current code already computes quantile thresholds in ElevationBandAssigner. The missing part is exporting the derived ocean threshold as SeaLevel01 rather than hard-coding it later.

Stage 3 — Mountain Systems (NEW, separate from base elevation)

Goal: coherent chains + foothills + limited scattered peaks + 3–5 volcano peaks.

Inputs: ElevationRaw, LandMask, new HB_MountainsConfig:

RangeCount, RangeLength, RangeCoherence

FoothillFalloff, ScatteredPeakRate

VolcanoCount (3–5), VolcanoPlacementRules

Outputs (WorldData fields):

MountainFactor01 (f32)

RangeId (int)

VolcanoMask (u8) and VolcanoId (optional)

(optional) ElevationUplift or write uplift into ElevationRaw

Artifacts:

Bake/WorldPreview_03_Ranges.png

Bake/WorldPreview_03_MountainFactor.png

Bake/WorldPreview_03_Volcanoes.png

Not trying to create: rock/snow visuals; only structure

Stage 4 — Landforms (derivatives + passes)

Goal: give the map “basins, valleys, passes” as explicit data, not inferred.

Inputs: final elevation (after mountains)

Outputs (WorldData fields):

Slope01 (f32 or packed)

Curvature01 (f32 or packed)

Ruggedness01 (f32)

PassMask (u8) and optionally PassId

(optional) ValleyIndex01

Artifacts: slope/ruggedness/passes previews

Not trying to create: hydrology; only geometry semantics

Stage 5 — Coast & Shelf Classification (your Coast module, expanded)

Goal: coast types become visible (cliff/beach/marsh/fjord tendencies).

Inputs: elevation + sea level + slope/ruggedness near shore

Outputs (WorldData fields):

WaterMask ocean component (or IsOcean/IsShelf)

CoastType (u8 enum)

DistanceToCoast01 (f32)

Artifacts: coast type + distance previews

Not trying to create: rivers/lakes

Stage 6 — Hydrology (your Hydrology module, but with viewer-semantic outputs)

Goal: rivers/lakes/basins + rapids/waterfalls/crossings that the viewer can show.

Inputs: elevation, coast masks, optional valley/passes

Outputs (WorldData fields):

FlowDirection (u8)

FlowAccumulation (f32/u32)

DrainageBasinId (int)

WaterMask expanded (ocean/lake/river)

RiverOrder + RiverWidthClass + RiverType

WaterfallMask (u8), RapidsMask (u8)

CrossingMask (u8) (recommended)

Artifacts: rivers/lakes/basins/waterfalls/rapids/crossings previews

Not trying to create: climate/biomes

Note: In your current WorldArrays, IsWaterfall and IsRapids exist but are not allocated. Either allocate them or remove until implemented.

Stage 7 — Climate (NEW module)

Goal: jungles/swamps on equator when hot+wet; rain shadows behind ranges.

Inputs: latitude, elevation, mountain/ruggedness, distance-to-coast

Outputs (WorldData fields):

Temperature01 (f32)

Moisture01 (f32)

Aridity01 (f32)

(optional) WindDir (u8)

Artifacts: temp/moisture/aridity previews

Not trying to create: biome identities (that’s next)

Stage 8 — Biomes & Materials (NEW module)

Goal: the viewer can tell “pine vs oak,” swamp vs jungle, rock vs soil, snowline.

Inputs: climate + hydrology + landforms

Outputs (WorldData fields):

BiomeId (u8) + optional blend weights

VegetationType (u8) + VegetationDensity01

RockExposure01

SnowMask01 + TreeLineMask

(optional) SoilFertility01

Artifacts: biome/vegetation/rock/snow previews

Not trying to create: POIs/ruins (that’s next)

Stage 9 — Regions & Landmark Candidates (NEW module)

Goal: “old ruins” and landmark candidates based on terrain predicates.

Inputs: ranges, basins, rivers, biomes, ruggedness

Outputs (WorldData fields):

RegionId (int)

LandformTags (bitset)

Landmark candidates emitted to a separate file (not per-tile arrays)

Artifacts:

Features/Regions.json

Features/Landmarks.json

landmark previews

Not trying to create: full gameplay mission content

Stage 10 — Gameplay Suitability (NEW module, optional now)

Goal: “a hex tile tells me everything” for routing, placement, encounters.

Inputs: traversal drivers + chokepoints + fertility + water + biomes

Outputs (WorldData fields):

TraversalCost01

RoadSuitability01

SettlementSuitability01

Resource*01 layers

Artifacts: suitability previews, optional RouteGraph

Not trying to create: dynamic world state (World Updater owns that)

Stage 11 — Bake/Export (your 99_MapBake + core export pass)

Goal: stable viewer contract across zoom levels.

Inputs: all WorldData layers

Outputs (WorldData exports):

all .f32/.u8 tiles you choose to persist

Manifest.json listing every exported layer and packing

Artifacts:

Packed textures (RGBA channels) for the viewer

Existing hillshade/bathymetry as debug or optional