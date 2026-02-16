# Module 3 — Coast Execution Flow

## High-Level Pipeline Integration

```
MapMakerDriver.Run()
    │
    ├─> Module 1: Elevation ✓
    │   └─> ElevationBands[] populated
    │
    ├─> Module 2: Latitude ✓
    │   └─> LatitudeBands[] populated
    │
    ├─> Module 3: Coast  ← YOU ARE HERE
    │   │
    │   ├─> CoastValidate.Validate(config)
    │   │
    │   ├─> CoastGenerator.Execute(worldArrays)
    │   │   │
    │   │   ├─> RemoveEdgeIslands()
    │   │   ├─> ClassifyDeepOcean()
    │   │   ├─> ClassifyCoastalShelf()
    │   │   └─> DetectInlandLakes()
    │   │
    │   ├─> CoastValidate.ValidateResults(worldArrays)
    │   │
    │   └─> Export Coast PNGs
    │       ├─> WorldPreview_03_Coast.png
    │       └─> WorldPreview_Stacked.png (updated)
    │
    └─> Module 4: Mountains (future)
```

---

## Detailed Execution Flow

### Entry Point: CoastGenerator.Execute()

```
┌─────────────────────────────────────────────┐
│  CoastGenerator.Execute(WorldArrays world)  │
└─────────────────┬───────────────────────────┘
                  │
                  ├─> [1] RemoveEdgeIslands()
                  │
                  ├─> [2] ClassifyDeepOcean()
                  │
                  ├─> [3] ClassifyCoastalShelf()
                  │
                  ├─> [4] DetectInlandLakes()
                  │
                  └─> Log: "COAST_COMPLETE"
```

---

### [1] Island Removal Workflow

```
RemoveEdgeIslands(world, width, height)
    │
    ├─> Build land mask
    │   │
    │   └─> For each tile:
    │           isLand[i] = (ElevationBands[i] >= Lowland)
    │
    ├─> Find connected land components
    │   │
    │   └─> FindConnectedComponents(isLand, w, h)
    │           │
    │           ├─> Initialize: visited[] = false, components = []
    │           │
    │           └─> For each tile:
    │                   │
    │                   └─> If isLand && !visited:
    │                           │
    │                           ├─> FloodFillComponent(tile)
    │                           │   │
    │                           │   ├─> BFS queue starting at tile
    │                           │   ├─> Mark visited
    │                           │   ├─> Collect all connected tiles
    │                           │   └─> Return component list
    │                           │
    │                           └─> Add component to components[]
    │
    ├─> Identify largest component
    │   │
    │   └─> largestIdx = argmax(components[i].Count)
    │
    ├─> Process each component
    │   │
    │   └─> For each component:
    │           │
    │           ├─> ComponentTouchesEdge(component)?
    │           │   │
    │           │   └─> For each tile in component:
    │           │           if (x == 0 || x == w-1 || y == 0 || y == h-1)
    │           │               return true
    │           │
    │           ├─> If component == largest:
    │           │   │
    │           │   ├─> If touches edge AND !AllowMainContinentEdgeTouch:
    │           │   │   │
    │           │   │   └─> For each tile in component:
    │           │   │           ElevationBands[tile] = Ocean
    │           │   │           removedCount++
    │           │   │       Log: "COAST_MAIN_REMOVED"
    │           │   │
    │           │   └─> Else (keep main continent):
    │           │           keptCount += component.Count
    │           │           If touches edge:
    │           │               Log: "COAST_MAIN_EDGE"
    │           │
    │           ├─> Else if touches edge (smaller island):
    │           │   │
    │           │   └─> For each tile in component:
    │           │           ElevationBands[tile] = Ocean
    │           │           removedCount++
    │           │
    │           └─> Else (interior island):
    │                   keptCount += component.Count
    │                   interiorIslandCount++
    │
    └─> Log: "COAST_ISLAND_STATS"
            (removedCount, keptCount, interiorIslandCount)
```

---

### [2] Ocean Classification Workflow

```
ClassifyDeepOcean(world, width, height)
    │
    ├─> Single pass over all tiles
    │   │
    │   └─> For i in [0..world.Count):
    │           │
    │           ├─> IsDeepOcean[i] = (ElevationBands[i] == DeepOcean)
    │           │
    │           └─> IsOcean[i] = (ElevationBands[i] <= Ocean)
    │
    ├─> Count statistics
    │   │
    │   ├─> deepCount = sum(IsDeepOcean)
    │   └─> oceanCount = sum(IsOcean)
    │
    └─> Log: "COAST_OCEAN_CLASS"
            (deepPct, deepCount, oceanPct, oceanCount)
```

---

### [3] Coastal Shelf Detection Workflow

```
ClassifyCoastalShelf(world, width, height)
    │
    ├─> Build land mask
    │   │
    │   └─> For each tile:
    │           landMask[i] = !IsOcean[i]
    │
    ├─> Compute distance field
    │   │
    │   └─> GridHelpers.ComputeDistanceField(landMask, w, h, maxDist)
    │           │
    │           ├─> Initialize: distance[] = infinity
    │           │
    │           ├─> BFS from all land tiles
    │           │   │
    │           │   ├─> For each land tile: queue.enqueue(tile), distance[tile] = 0
    │           │   │
    │           │   └─> While queue not empty:
    │           │           │
    │           │           ├─> tile = queue.dequeue()
    │           │           │
    │           │           ├─> For each 4-neighbor:
    │           │           │       │
    │           │           │       ├─> newDist = distance[tile] + 1
    │           │           │       │
    │           │           │       ├─> If newDist < distance[neighbor] && newDist <= maxDist:
    │           │           │       │       distance[neighbor] = newDist
    │           │           │       │       queue.enqueue(neighbor)
    │           │           │
    │           │           └─> Early termination when exceeding maxDist
    │           │
    │           └─> Return distance[]
    │
    ├─> Classify shelf tiles
    │   │
    │   └─> For each tile:
    │           if (IsOcean[i] && distance[i] <= CoastalShelfDepth):
    │               IsCoastalShelf[i] = true
    │               shelfCount++
    │
    └─> Log: "COAST_SHELF_CLASS"
            (shelfPct, shelfCount, CoastalShelfDepth)
```

---

### [4] Inland Lake Detection Workflow

```
DetectInlandLakes(world, width, height)
    │
    ├─> Build ocean mask
    │   │
    │   └─> For each tile:
    │           oceanMask[i] = IsOcean[i]
    │
    ├─> Find ocean components
    │   │
    │   └─> FindConnectedComponents(oceanMask, w, h)
    │           (same BFS flood-fill as island removal)
    │
    ├─> Process each ocean component
    │   │
    │   └─> For each component:
    │           │
    │           ├─> ComponentTouchesEdge(component)?
    │           │
    │           ├─> If touches edge:
    │           │       Skip (open ocean, not a lake)
    │           │
    │           └─> Else (landlocked component):
    │                   │
    │                   ├─> If component.Count < MinLakeSize:
    │                   │   │
    │                   │   └─> Convert to land:
    │                   │           │
    │                   │           └─> For each tile in component:
    │                   │                   ElevationBands[tile] = Lowland
    │                   │                   IsOcean[tile] = false
    │                   │                   IsDeepOcean[tile] = false
    │                   │                   IsCoastalShelf[tile] = false
    │                   │                   tinyLakeConversions++
    │                   │
    │                   └─> Else (valid lake):
    │                           │
    │                           └─> For each tile in component:
    │                                   IsInlandLake[tile] = true
    │                                   lakeCount++
    │                                   lakeTileCount++
    │
    └─> Log: "COAST_LAKE_DETECT"
            (lakeCount, lakeTileCount, tinyLakeConversions)
```

---

## Data Flow Diagram

```
┌──────────────────────────────────────────────────────────────┐
│                    MODULE 3 DATA FLOW                        │
└──────────────────────────────────────────────────────────────┘

INPUTS (read-only):
    ┌─────────────────────┐
    │  ElevationBands[]   │  (from Module 1)
    │  ├─ DeepOcean       │
    │  ├─ Ocean           │
    │  ├─ Lowland         │
    │  ├─ Highlands       │
    │  ├─ LowMountains    │
    │  └─ HighMountains   │
    └──────────┬──────────┘
               │
               ├───────────────────────────────────┐
               │                                   │
               ▼                                   ▼
    ┌──────────────────┐              ┌──────────────────┐
    │ RemoveEdgeIslands│              │ClassifyDeepOcean │
    │                  │              │                  │
    │ Modifies:        │              │ Writes:          │
    │ ElevationBands[] │              │ IsDeepOcean[]    │
    │ (Ocean ← removed)│              │ IsOcean[]        │
    └──────────┬───────┘              └──────────┬───────┘
               │                                 │
               └────────────┬────────────────────┘
                            │
                            ▼
               ┌──────────────────────┐
               │ ClassifyCoastalShelf │
               │                      │
               │ Reads: IsOcean[]     │
               │ Writes:              │
               │ IsCoastalShelf[]     │
               └──────────┬───────────┘
                          │
                          ▼
               ┌──────────────────────┐
               │  DetectInlandLakes   │
               │                      │
               │ Reads: IsOcean[]     │
               │ Writes:              │
               │ IsInlandLake[]       │
               │                      │
               │ Modifies (tiny):     │
               │ ElevationBands[]     │
               │ IsOcean[]            │
               │ IsDeepOcean[]        │
               │ IsCoastalShelf[]     │
               └──────────┬───────────┘
                          │
                          ▼
OUTPUTS (written):
    ┌──────────────────────┐
    │   IsDeepOcean[]      │  (deep ocean tiles)
    │   IsOcean[]          │  (all ocean tiles)
    │   IsCoastalShelf[]   │  (shallow near land)
    │   IsInlandLake[]     │  (landlocked ocean)
    └──────────────────────┘
```

---

## Validation Flow

```
┌─────────────────────────────────────────────┐
│         VALIDATION CHECKPOINTS              │
└─────────────────────────────────────────────┘

PRE-GENERATION:
    CoastValidate.Validate(config, emit)
        │
        ├─> Check: config != null
        ├─> Check: CoastalShelfDepth >= 1
        ├─> Check: MinLakeSize >= 1
        │
        └─> Log: "COAST_CFG_VALID"

POST-GENERATION:
    CoastValidate.ValidateResults(world, emit)
        │
        ├─> ValidateLandConnectivity()
        │   │
        │   ├─> Build land mask: !IsOcean[]
        │   ├─> Count components via flood-fill
        │   │
        │   ├─> If 0 components: WARN "COAST_NO_LAND"
        │   ├─> If 1 component: INFO "COAST_LAND_CONNECTED"
        │   └─> If >1 components: INFO "COAST_LAND_ISLANDS"
        │
        ├─> ValidateOceanConnectivity()
        │   │
        │   ├─> Build ocean mask: IsOcean[] && !IsInlandLake[]
        │   ├─> Count components via flood-fill
        │   │
        │   ├─> If 0 components: WARN "COAST_NO_OCEAN"
        │   ├─> If 1 component: INFO "COAST_OCEAN_CONNECTED"
        │   └─> If >1 components: WARN "COAST_OCEAN_FRAGMENTED"
        │
        └─> ValidateCoastalShelfCoverage()
            │
            ├─> Count: shelfCount, oceanCount
            ├─> Compute: shelfPct = shelfCount / oceanCount
            │
            └─> Log: INFO "COAST_SHELF_COVERAGE"
```

---

## Export Flow

```
┌─────────────────────────────────────────────┐
│            PNG EXPORT PIPELINE              │
└─────────────────────────────────────────────┘

WorldExportPass.ExportCoastPng()
    │
    ├─> Create Texture2D (width × tileSize) × (height × tileSize)
    │
    ├─> FillTiled with ColorForCoast()
    │   │
    │   └─> For each tile (x, y):
    │           │
    │           ├─> If IsInlandLake[idx]: return Cyan-Blue (0.4, 0.7, 0.9)
    │           ├─> Else if IsDeepOcean[idx]: return Dark Blue (0.0, 0.2, 0.4)
    │           ├─> Else if IsCoastalShelf[idx]: return Light Blue (0.4, 0.7, 1.0)
    │           └─> Else (land): return Gray (0.5, 0.5, 0.5)
    │
    ├─> FlipVertical (if ExportFlipVertical == true)
    │
    └─> SavePng("WorldPreview_03_Coast.png")

WorldExportPass.ExportStackedPng_WithCoast()
    │
    ├─> Create Texture2D (width × tileSize) × (height × tileSize)
    │
    ├─> FillTiled with blended colors:
    │   │
    │   └─> For each tile (x, y):
    │           │
    │           ├─> elevColor = ColorForElevation(ElevationBands[idx])
    │           │
    │           ├─> If LatitudeBands exists:
    │           │       latColor = ColorForLatitude(LatitudeBands[idx])
    │           │       elevColor = Lerp(elevColor, latColor, 0.3)
    │           │
    │           ├─> If IsOcean[idx] && coast data exists:
    │           │       coastColor = ColorForCoast(...)
    │           │       elevColor = Lerp(elevColor, coastColor, 0.4)
    │           │
    │           └─> Return elevColor
    │
    ├─> FlipVertical (if ExportFlipVertical == true)
    │
    └─> SavePng("WorldPreview_Stacked.png")
```

---

## Timing Breakdown (Example: 2000×2000 map)

```
┌────────────────────────────────────────────┐
│      TYPICAL PERFORMANCE PROFILE           │
└────────────────────────────────────────────┘

Total Module Time: ~100ms
    │
    ├─> RemoveEdgeIslands():        ~30ms
    │   ├─ Build land mask:         5ms
    │   ├─ Flood-fill components:   20ms
    │   └─ Process components:      5ms
    │
    ├─> ClassifyDeepOcean():        ~5ms
    │   └─ Single array pass
    │
    ├─> ClassifyCoastalShelf():     ~50ms
    │   ├─ Build land mask:         5ms
    │   ├─ BFS distance field:      40ms  ← Dominant cost
    │   └─ Classify shelf tiles:    5ms
    │
    └─> DetectInlandLakes():        ~15ms
        ├─ Build ocean mask:        5ms
        ├─ Flood-fill components:   8ms
        └─ Process components:      2ms
```

---

## Error Handling & Edge Cases

```
┌────────────────────────────────────────────┐
│          EDGE CASE HANDLING                │
└────────────────────────────────────────────┘

All Land Map (No Ocean):
    ├─> RemoveEdgeIslands: All land → largest component, nothing removed
    ├─> ClassifyDeepOcean: All arrays false
    ├─> ClassifyCoastalShelf: No shelf (no ocean to classify)
    ├─> DetectInlandLakes: No lakes
    └─> Validation: WARN "COAST_NO_OCEAN"

All Ocean Map (No Land):
    ├─> RemoveEdgeIslands: Skip (WARN "COAST_NO_LAND")
    ├─> ClassifyDeepOcean: All ocean marked
    ├─> ClassifyCoastalShelf: No shelf (distance to land = infinity)
    ├─> DetectInlandLakes: Single ocean touching all edges → no lakes
    └─> Validation: WARN "COAST_NO_LAND"

Archipelago (Many Islands):
    ├─> RemoveEdgeIslands:
    │   ├─ Identify largest island
    │   ├─ Remove edge-touching islands (except largest)
    │   └─ Keep interior islands
    ├─> Result: Main continent + scattered interior islands

Single Tiny Island:
    ├─> If edge-touching: Removed (converted to ocean)
    └─> If interior: Preserved (even if small)

Fragmented Ocean (Multiple Seas):
    ├─> DetectInlandLakes: Each non-edge ocean → inland lake
    └─> Validation: WARN "COAST_OCEAN_FRAGMENTED"
```

---

## Summary

Module 3 executes in **4 main stages**:
1. **Island Removal** - Clean up edge artifacts, preserve interior islands
2. **Ocean Classification** - Mark deep ocean and all ocean tiles
3. **Shelf Detection** - Distance-based coastal zone classification
4. **Lake Detection** - Identify landlocked ocean components

All stages use efficient **BFS flood-fill** and **distance field** algorithms with early termination for optimal performance. The module integrates seamlessly into the pipeline between Latitude and future modules, providing foundational ocean/land classification for biomes, hydrology, and gameplay systems.
