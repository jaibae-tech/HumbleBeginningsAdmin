# Module 3 — Coast

## Purpose
Classifies ocean tiles into deep ocean and coastal shelf zones, removes edge-touching landmasses while preserving interior islands, and detects landlocked ocean components as inland lakes.

## Status
✓ **COMPLETE** - Implementation finished, tested, and documented (2026-02-06)

## Key Features
- **Island Removal**: Automatically removes landmasses touching map edges except the largest connected component
- **Interior Island Preservation**: Keeps islands that don't touch map boundaries
- **Coastal Shelf Detection**: Distance-based classification of shallow coastal waters
- **Inland Lake Detection**: Identifies landlocked ocean components and converts tiny lakes to land
- **Deterministic**: Uses `SeedContext.CoastRng` for repeatable generation

## Module Structure

```
3_Coast/
├── Config/
│   └── HB_CoastConfig.cs          # ScriptableObject configuration
├── Scripts/
│   ├── CoastGenerator.cs          # Core generation logic
│   └── CoastValidate.cs           # Validation and logging
├── Docs/
│   ├── ModuleSpec.md              # Complete specification
│   ├── CHANGELOG.md               # Version history
│   └── WorkflowDiagram.md         # Execution flow diagram
└── README.md                      # This file
```

## Quick Start

### 1. Create Configuration Asset
Right-click in Project window:
`Create > Humble Beginnings > MapMaker > Module 3 - Coast > Config`

### 2. Configure Parameters

**Island Removal:**
- **RemoveEdgeTouchingIslands**: Remove landmasses touching edges (default: true)
- **AllowMainContinentEdgeTouch**: Allow largest landmass to touch edges (default: false)
  - If `false`: NO land touches edges - creates clean island continent
  - If `true`: Largest continent can touch edges (may leave edge-connected land)

**Coastal Shelf:**
- **CoastalShelfDepth**: Distance in tiles from land (default: 2, range: 1-10)

**Inland Lakes:**
- **DetectInlandLakes**: Enable lake detection (default: true)
- **MinLakeSize**: Minimum tiles for a lake (default: 4, range: 1-100)

### 3. Enable in Pipeline
- Assign config to `HB_PipelineConfig.CoastConfig`
- Check `EnableCoast` checkbox
- Run `MapMakerDriver`

## Outputs

### Runtime Data
- `WorldArrays.IsDeepOcean[]` - Deep ocean tiles from elevation bands
- `WorldArrays.IsOcean[]` - All ocean tiles (deep + regular ocean)
- `WorldArrays.IsCoastalShelf[]` - Shallow ocean near land
- `WorldArrays.IsInlandLake[]` - Landlocked ocean components

### PNG Exports
- `WorldPreview_03_Coast.png` - Isolated coast classification
- `WorldPreview_Stacked.png` - Elevation + latitude + coast overlay

### Color Coding
- **Dark Blue** (0.0, 0.2, 0.4): Deep ocean
- **Light Blue** (0.4, 0.7, 1.0): Coastal shelf
- **Cyan-Blue** (0.4, 0.7, 0.9): Inland lakes

## Dependencies
- **Required**: Module 1 (Elevation) for elevation bands and ocean classification
- **Used By**: Future modules (biomes, hydrology, civilizations)

## Validation
Module validates on run:
- Checks land connectivity (reports island count)
- Checks ocean connectivity (warns if fragmented, excludes inland lakes)
- Reports coastal shelf coverage percentage
- Logs island removal statistics

## Performance
- **Complexity**: O(width × height × log(components)) - flood-fill based
- **Memory**: Temporary boolean masks for component detection
- **Typical Runtime**: ~100ms for 2000×2000 map (depends on land/ocean ratio)

## Algorithm Overview

1. **Island Removal** (if enabled)
   - Find all land components via flood-fill
   - Identify largest component
   - Remove components touching map edges (except largest)
   - Preserve interior islands

2. **Ocean Classification**
   - Mark deep ocean from elevation bands
   - Mark all ocean tiles

3. **Coastal Shelf Detection**
   - Compute BFS distance field from land
   - Mark ocean tiles within CoastalShelfDepth as shelf

4. **Inland Lake Detection** (if enabled)
   - Find all ocean components via flood-fill
   - Flag components not touching edges as lakes
   - Convert lakes smaller than MinLakeSize to land

## Documentation
For detailed algorithm description and execution flow, see:
- `/Modules/3_Coast/Docs/ModuleSpec.md` - Complete specification
- `/Modules/3_Coast/Docs/WorkflowDiagram.md` - Visual execution flow

## Known Limitations
- Island removal only considers 4-connected components (diagonal connections treated as separate)
- Coastal shelf depth is uniform (no variation by latitude/biome)
- Tiny lake conversion is size-only (no shape/location criteria)

## Changelog
See `/Modules/3_Coast/Docs/CHANGELOG.md` for version history.
