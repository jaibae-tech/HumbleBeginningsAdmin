# ModuleSpec — 03 Coast

## Overview
Module 3 classifies ocean tiles, removes edge-touching landmasses while preserving interior islands, computes coastal shelf zones via distance field, and detects landlocked ocean components as inland lakes. Provides foundational ocean/land classification for downstream modules.

## ScriptableObject Inputs

### HB_CoastConfig
**Island Removal:**
- RemoveEdgeTouchingIslands: Remove landmasses touching map edges (default: true)
- AllowMainContinentEdgeTouch: Allow largest landmass to touch map edges (default: false)
  - If `false`: All edge-touching land removed, including main continent → clean island map
  - If `true`: Largest component kept even if edge-touching → may have edge-connected land

**Coastal Shelf Classification:**
- CoastalShelfDepth: Distance in tiles from land to classify as shelf vs deep ocean (default: 2, range: 1-10)

**Inland Lake Detection:**
- DetectInlandLakes: Enable landlocked ocean component detection (default: true)
- MinLakeSize: Minimum tiles for ocean component to be a lake; smaller converted to land (default: 4, range: 1-100)

### HB_MapConfig (from pipeline)
- MapWidth/MapHeight/RootSeed

### HB_ExportConfig (from pipeline)
- ExportFolderName/ExportTilePixelSize/ExportFlipVertical

## Runtime Inputs

### WorldArrays
- ElevationBands (read) - Source for ocean/land classification
- IsDeepOcean (written)
- IsOcean (written)
- IsCoastalShelf (written)
- IsInlandLake (written)

### SeedContext
- CoastRng (reserved for future stochastic features)

## Algorithm

### 1. Island Removal (if RemoveEdgeTouchingIslands = true)

**Input:** ElevationBands array

**Process:**
```
1. Build land mask: landMask[i] = (ElevationBands[i] >= Lowland)
2. Find connected components via flood-fill BFS:
   - For each unvisited land tile:
     - Run BFS to collect all connected land tiles
     - Add component to list
3. Identify largest component by tile count
4. For each component:
   - Check if component touches any map edge
   - If component == largest:
     - If touches edge AND AllowMainContinentEdgeTouch == false:
       - Convert all tiles to Ocean (remove main continent)
       - Log COAST_MAIN_REMOVED
     - Else:
       - Keep component (largest preserved)
       - If touches edge: Log COAST_MAIN_EDGE
   - Else (smaller component):
     - If touches edge:
       - Convert all tiles to Ocean (remove island)
       - Increment removedCount
     - Else:
       - Keep component (interior island)
       - Increment interiorIslandCount
5. Log: COAST_ISLAND_STATS (removedCount, keptCount, interiorIslandCount)
```

**Edge Detection:** Tile touches edge if `x == 0 || x == width-1 || y == 0 || y == height-1`

**Result with AllowMainContinentEdgeTouch = false (default):**
- No land touches map edges
- Creates clean "island continent" surrounded by ocean
- Only interior islands preserved

**Result with AllowMainContinentEdgeTouch = true:**
- Largest landmass kept even if touching edges
- Smaller edge-touching islands removed
- May result in edge-connected land

### 2. Deep Ocean Classification

**Input:** Updated ElevationBands array

**Process:**
```
For each tile i in WorldArrays:
    IsDeepOcean[i] = (ElevationBands[i] == DeepOcean)
    IsOcean[i] = (ElevationBands[i] <= Ocean)  // Deep + Regular ocean

Log: Deep ocean percentage, total ocean percentage
```

### 3. Coastal Shelf Detection

**Input:** IsOcean array

**Process:**
```
1. Build land mask: landMask[i] = !IsOcean[i]
2. Compute distance field from land:
   distanceField = GridHelpers.ComputeDistanceField(landMask, width, height, CoastalShelfDepth + 1)
3. For each tile i:
   If IsOcean[i] && distanceField[i] <= CoastalShelfDepth:
       IsCoastalShelf[i] = true

Log: Shelf percentage, shelf tile count
```

**Distance Field:** BFS-based Manhattan distance from nearest land tile

### 4. Inland Lake Detection (if DetectInlandLakes = true)

**Input:** IsOcean array

**Process:**
```
1. Build ocean mask: oceanMask[i] = IsOcean[i]
2. Find ocean components via flood-fill BFS
3. For each component:
   - If component does NOT touch any map edge:
     - If component.Count < MinLakeSize:
       - Convert to land:
         - ElevationBands[i] = Lowland
         - IsOcean[i] = false
         - IsDeepOcean[i] = false
         - IsCoastalShelf[i] = false
       - Increment tinyLakeConversions
     - Else:
       - For each tile in component:
         - IsInlandLake[i] = true
       - Increment lakeCount, lakeTileCount

Log: lakeCount, lakeTileCount, tinyLakeConversions
```

## Validation

### Config Validation (WARN)
- CoastalShelfDepth must be >= 1
- MinLakeSize must be >= 1

### Result Validation (INFO/WARN)
**Land Connectivity:**
- Count land connected components
- If 0: WARN "No land tiles found"
- If 1: INFO "All land is connected"
- If >1: INFO "Land has N components (main + N-1 islands)"

**Ocean Connectivity:**
- Count ocean connected components (excluding IsInlandLake tiles)
- If 0: WARN "No ocean tiles found"
- If 1: INFO "All ocean is connected"
- If >1: WARN "Ocean has N disconnected components (possible landlocked seas)"

**Coastal Shelf Coverage:**
- Compute shelf percentage: shelfCount / oceanCount
- INFO: "Coastal shelf covers X% of ocean tiles"

## Outputs

### WorldArrays (all boolean arrays)
- IsDeepOcean[] - Tiles with ElevationBandFinal == DeepOcean
- IsOcean[] - All ocean tiles (deep + regular ocean)
- IsCoastalShelf[] - Ocean tiles within CoastalShelfDepth of land
- IsInlandLake[] - Ocean tiles in landlocked components >= MinLakeSize

## Exports

### PNG Files
- **WorldPreview_03_Coast.png** - Isolated coast classification
  - Deep Ocean: Dark blue (0.0, 0.2, 0.4)
  - Coastal Shelf: Light blue (0.4, 0.7, 1.0)
  - Inland Lakes: Cyan-blue (0.4, 0.7, 0.9)
  - Land: Gray (0.5, 0.5, 0.5)

- **WorldPreview_Stacked.png** - Updated with coast overlay
  - Blends elevation + latitude (if available) + coast (40% blend for ocean tiles)

## Dependencies

### Inputs
- **Required**: Module 1 (Elevation) for ElevationBands array
- Uses GridHelpers.ComputeDistanceField() for shelf detection
- Uses flood-fill BFS for component detection

### Outputs Used By
- Module 4 (Mountains): Coastal proximity checks
- Module 5 (Hydrology): Lake vs ocean classification
- Module 7 (Biomes): Coastal/inland biome assignment
- Module 8+ (Civilizations): Coastal settlement placement

## Performance Notes

### Complexity
- Island Removal: O(W × H) flood-fill + component iteration
- Ocean Classification: O(W × H) single pass
- Shelf Detection: O(W × H × D) BFS distance field (D = CoastalShelfDepth)
- Lake Detection: O(W × H) flood-fill + component iteration
- **Total**: O(W × H × D) dominated by distance field computation

### Memory
- Temporary boolean masks: landMask, oceanMask (2 × W × H bytes)
- Distance field: W × H floats (4 × W × H bytes)
- Visited masks for flood-fill: W × H bytes
- Component lists: O(C) where C = component count

### Typical Runtime
- 1000×1000 map: ~25ms
- 2000×2000 map: ~100ms
- 4000×4000 map: ~400ms

### Optimization Notes
- Distance field uses BFS early termination (maxDistance = CoastalShelfDepth + 1)
- Flood-fill uses queue-based BFS (no recursion stack overflow)
- Single-pass ocean classification minimizes array traversals

## Design Rationale

### Why Remove Edge-Touching Islands?
Edge-touching landmasses often represent map boundary artifacts from noise generation. Removing them creates cleaner, more realistic world maps with a single main continent plus optional interior islands.

### Why Preserve Interior Islands?
Interior islands add geographic variety and strategic gameplay value (isolated resources, defensive positions, naval routes). They emerge naturally from elevation noise and should be preserved.

### Why Distance-Based Shelf Classification?
Real-world coastal shelves extend 10-200km from shore. Distance-based classification is simple, efficient, and produces realistic shallow-water zones for biomes, resources, and gameplay mechanics.

### Why Detect Inland Lakes?
Landlocked oceans (like the Caspian Sea) are geologically distinct from open ocean. Flagging them allows future modules to treat them differently (freshwater vs saltwater, different biomes, separate ecology).

### Why Convert Tiny Lakes to Land?
Sub-MinLakeSize ocean components are usually noise artifacts or unrealistic puddles. Converting them to land improves map cleanliness and prevents gameplay issues (e.g., single-tile impassable water).

## Usage Example

```csharp
var cfg = Resources.Load<HB_CoastConfig>("Configs/HB_Coast_Default");
var seed = new SeedContext(mapConfig.RootSeed);
var emit = /* logging emitter */;

// Validate config
CoastValidate.Validate(cfg, emit);

// Generate coast classification
var gen = new CoastGenerator(cfg, seed, emit);
gen.Execute(worldArrays);

// Validate results
CoastValidate.ValidateResults(worldArrays, emit);
```

## Known Limitations

### 4-Connected Components Only
Flood-fill uses 4-connectivity (N/S/E/W neighbors). Diagonal connections don't link components. This can split visually-connected landmasses if they only touch at corners.

**Workaround:** Use elevation noise parameters that favor larger, more connected landmasses.

### Uniform Shelf Depth
CoastalShelfDepth is global. Real shelves vary by latitude, ocean basin, and tectonic activity.

**Future Enhancement:** Add latitude-based shelf depth modulation (wider shelves in temperate zones).

### Size-Only Lake Conversion
MinLakeSize only considers tile count. Doesn't account for lake shape (elongated vs circular) or location (mountain vs lowland).

**Future Enhancement:** Add shape factor and elevation-based lake retention logic.

### No Ocean Depth Variation
Deep ocean is binary (deep vs regular ocean bands). No gradual depth gradient.

**Future Enhancement:** Module 5 (Hydrology) could add bathymetry data for ocean depth variation.

## Edge Cases

### All Land Map
- IslandRemoval: All land marked as largest component, nothing removed
- OceanClassification: All arrays remain false
- ShelfDetection: No shelf tiles
- LakeDetection: No lakes
- Validation: WARN "No ocean tiles found"

### All Ocean Map
- IslandRemoval: No land components, skipped
- OceanClassification: All tiles marked as ocean
- ShelfDetection: No shelf (no land to measure distance from)
- LakeDetection: Single ocean component touching all edges, no lakes
- Validation: WARN "No land tiles found"

### Single Tiny Island (< MinLakeSize)
- If island touches edge: Removed
- If interior island: Preserved (island removal only affects ocean components)

### Archipelago (Many Small Islands)
- Largest component kept
- Edge-touching islands removed
- Interior islands preserved
- Result: Main continent + scattered interior islands

## Testing Checklist
- [ ] Large continent + interior islands preserved
- [ ] Edge-touching islands removed
- [ ] Coastal shelf correctly computed (distance <= CoastalShelfDepth)
- [ ] Inland lakes detected and flagged
- [ ] Tiny lakes (< MinLakeSize) converted to land
- [ ] Validation logs correct connectivity stats
- [ ] PNG exports match classification data
- [ ] All land map: no crashes, appropriate warnings
- [ ] All ocean map: no crashes, appropriate warnings
- [ ] Deterministic: same seed produces same results
