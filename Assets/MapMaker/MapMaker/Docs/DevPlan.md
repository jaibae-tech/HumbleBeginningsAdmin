# DevPlan

This is the current strawman development plan for MapMaker. Numbering starts at Module 1.

## Milestone 0: Infrastructure (complete)
- Frozen logging (Core/Logging) with file output.
- Thin driver + trigger/button hookup.
- Pipeline ScriptableObjects.
- Sample module template folder.

## Shared/Core work (incremental)
These items are shared and should not be duplicated across modules:
- **World arrays**: allocate once per run and pass to all modules.
- **SeedContext**: derived `System.Random` streams per module.
- **Export**: centralized PNG export and one "stacked" PNG that shows everything except latitude.
- **Grid helpers**: neighbors, bounds checks, edge flood-fill, etc.
- **Global export config**: `ExportFolderName`, `ExportTilePixelSize`, `ExportFlipVertical` (ScriptableObject in Core).

## Module 1: Elevation
Goal: produce `ElevationBandFinal` map from raw noise using quantile thresholding + optional edge bias.
Outputs: `elevationRaw[]`, `elevationBandFinal[]`, preview PNG.

## Module 2: Latitude
Goal: produce latitude bands (3-band small maps, 5-band large maps) with optional warp.
Outputs: `latitudeBandType[]`, preview PNG.

## Module 3: Coast
Goal: derive edge-connected ocean vs inland water (if any later), and build coastal shelf mask.
Outputs: `isDeepOcean[]`, `isOcean[]`, `isCoastalShelf[]`, plus any temporary masks for debug; preview PNGs; updates `ElevationBandFinal` where required.

## Module 4: Mountains & Hills
Goal: identify mountain ranges and hill zones from elevation gradients / bands; avoid heavy "tectonics" simulation.
Outputs: `isMountain[]`, `isHill[]`, optional ridge lines; preview PNG.

## Module 5: Hydrology
Goal: rivers, lakes (if allowed), drainage basins derived from elevation; keep it deterministic and lightweight.
Outputs: `riverMask[]`, `lakeMask[]`, flow directions/accumulation (as needed); preview PNG.

## Module 6: Moisture
Goal: moisture field from distance-to-water + elevation/orographic effects (lightweight; no full climate).
Outputs: `moisture[]`, preview PNG.

## Module 7: Biomes
Goal: biome assignment using elevation + latitude + moisture; deterministic mapping.
Outputs: `biomeType[]`, preview PNG.

## Module 8: Landmarks
Goal: place landmark features based on terrain constraints (mountain passes, caves, ruins, watch cairns, etc.).
Outputs: landmark instances/list + per-tile landmark mask (debug), preview PNG optional.

## Module 9: Regions
Goal: region partitioning/labels for later gameplay systems (admin tooling + mission hooks).
Outputs: `regionId[]`, optional border preview.


## Module 10: Anchors
Goal: place anchor cities/factions and influence seeds.
Outputs: anchor instances + influence seeds (arrays or lists), preview optional.

## Module 11: Scenario
Goal: scenario initialization step after landmarks and before anchors when needed (per design), or between anchors and missions depending on final rules.
Outputs: initial world state deltas for gameplay systems.

## Module 12: Export polish
Goal: stable filenames, consistent previews, JSON dump (optional), and documentation completeness.

## Per-module documentation requirement
Each module must maintain:
- `Docs/ModuleNotes.md` (design + contracts)
- `Docs/PatchLog.md` (timestamped change log)
- `Docs/ModuleSpec.md` (inputs/outputs + field documentation)
