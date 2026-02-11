# Hydrology: Flow Accumulation Algorithm

**Version:** 1.0  
**Approach:** Physics-based flow simulation (D6 for hex grid)  
**Output:** Natural dendritic river networks with guaranteed edge continuity

---

## Overview

This hydrology system generates rivers by simulating water flow based on terrain elevation. Water flows downhill following gravity, naturally creating realistic river networks.

---

## Algorithm Phases

### Phase 1: Flow Direction

**Purpose:** Determine which direction water flows from each tile.

**Algorithm:** D6 Steepest Descent
```
For each land tile:
  1. Check all 6 hex neighbors
  2. Find neighbor with lowest elevation
  3. If neighbor is lower than current tile:
     → Store flow direction (0-5)
  Else:
     → Mark as sink/flat (255 = no flow)
```

**Output:**
- `FlowDirection[]` - byte per tile (0-5 = direction, 255 = no flow)

**Edge Cases:**
- **Flat areas:** Multiple neighbors at same elevation → no flow (potential basin)
- **Local minima:** All neighbors higher → no flow (definite basin)
- **Ocean tiles:** Never flow (already at destination)

---

### Phase 2: Flow Accumulation

**Purpose:** Count how many tiles drain through each tile (drainage area).

**Algorithm:** Topological Sort + Accumulation
```
1. Initialize all tiles with accumulation = 1 (the tile itself)

2. Sort all land tiles by elevation (highest first)

3. For each tile (high to low):
   If tile has flow direction:
     downstream = neighbor in flow direction
     downstream.accumulation += current.accumulation

Result: Each tile knows total drainage area
```

**Output:**
- `FlowAccumulation[]` - uint per tile (number of tiles draining through)

**Why This Works:**
- Processing high→low ensures upstream is always done first
- Each tile passes its total accumulation downstream
- Naturally creates dendritic patterns (tributaries merge)

**Example:**
```
Mountain tile (A): accum = 1 (just itself)
Flows to tile (B): accum = 1 + any other upstream = 5
Flows to tile (C): accum = 5 + other tributaries = 50
→ Tile C is a river (drains 50 tiles)
```

---

### Phase 3: River Classification

**Purpose:** Determine which tiles are rivers based on drainage area.

**Thresholds (tunable):**
```
accumulation >= 100    → Stream
accumulation >= 1000   → Creek
accumulation >= 5000   → River
accumulation >= 20000  → Major River
```

**Output:**
- `RiverTypes[]` - enum per tile (None, Stream, Creek, River, MajorRiver)

**River Width:** Implicitly defined by river type
- Stream: 1-2m wide
- Creek: 3-5m
- River: 10-20m
- Major River: 50-100m

This is used during rendering and combat map generation.

---

### Phase 4: Basin Detection

**Purpose:** Find natural lakes (areas where water pools).

**Algorithm:** Sink Detection + Expansion
```
1. Find all tiles with no outflow (FlowDirection = 255)
   → These are potential basin centers

2. Expand each sink to include nearby flat areas:
   - BFS from sink
   - Include neighbors with similar elevation (±0.01)
   - Result: Flat depression where water would pool

3. Size filtering:
   - If basin < MinBasinSize (20) → Too small, ignore
   - If basin > MaxBasinSize (200) → Plateau, might not be lake
   - Otherwise → Mark as lake
```

**Output:**
- `IsLake[]` - bool per tile
- `DrainageBasinId[]` - int per tile (which lake system)

**Why Basins Form:**
- Local minima in terrain
- Flat areas surrounded by higher ground
- No downstream path for water

---

### Phase 5: Feature Detection

**Purpose:** Mark waterfalls and rapids based on elevation drops.

**Algorithm:** Downstream Elevation Comparison
```
For each river tile:
  downstream = neighbor in flow direction
  drop = current.elevation - downstream.elevation
  
  If drop >= WaterfallThreshold (0.05):
    → Mark as waterfall
  Else if drop >= RapidsThreshold (0.02):
    → Mark as rapids
```

**Output:**
- `IsWaterfall[]` - bool per tile
- `IsRapids[]` - bool per tile

**Usage:**
- Landmark placement (waterfall icons on map)
- Combat encounters (waterfall battle arenas)
- Visual rendering (waterfall sprites/effects)

---

## Edge Continuity

**Rivers automatically have perfect edge continuity because:**

1. **Flow is physics-based:** Water flows from tile A → tile B
2. **Direction is explicit:** Tile A stores "flows EAST"
3. **Neighbor knows source:** Tile B can check "who flows into me from WEST"

**Converting to TileDesc:**
```csharp
// For tile at (x,y):
byte flowOut = world.FlowDirection[idx]; // 0-5 or 255

// Find which neighbors flow INTO this tile
byte flowIn = 0;
for (int d = 0; d < 6; d++)
{
    var neighbor = GetNeighbor(x, y, d);
    if (neighbor.FlowDirection == OppositeDirection(d))
    {
        flowIn |= (1 << d); // Set bit for this direction
    }
}

// Store in TileDesc
tileDesc.RiverFlowIn = flowIn;
tileDesc.RiverFlowOut = flowOut;
```

**Validation:** If tile A flows EAST, tile B (to the east) MUST receive from WEST.
This is guaranteed by the flow direction algorithm.

---

## Comparison to Old Approach

### Basin-First Pathfinding (OLD):
```
1. Detect basins manually
2. Pathfind between basins (A*)
3. Create river paths
```
❌ Rivers went in circles
❌ No guarantee of continuity
❌ Complex merging logic
❌ Basins pre-placed (artificial)

### Flow Accumulation (NEW):
```
1. Calculate flow directions (physics)
2. Accumulate drainage (math)
3. Rivers emerge naturally
4. Basins form at sinks
```
✅ Natural dendritic networks
✅ Perfect edge continuity (by construction)
✅ Simple, elegant algorithm
✅ Basins are result, not input

---

## Performance

**Time Complexity:**
- Phase 1 (Flow Direction): O(N) - check 6 neighbors per tile
- Phase 2 (Accumulation): O(N log N) - sorting + linear pass
- Phase 3 (Classification): O(N) - threshold checks
- Phase 4 (Basins): O(N) - BFS expansions
- Phase 5 (Features): O(N) - elevation checks

**Total:** O(N log N) dominated by topological sort

**For 1600×1600 map:**
- N = 2,560,000 tiles
- Expected time: <5 seconds

---

## Tuning Guide

### Too Few Rivers
→ Lower `StreamThreshold` (currently 100)

### Too Many Rivers
→ Raise `StreamThreshold`

### Rivers Too Narrow (No Major Rivers)
→ Lower `MajorRiverThreshold` (currently 20000)

### Too Many Tiny Lakes
→ Raise `MinBasinSize` (currently 20)

### No Lakes Forming
→ Check terrain - might not have local minima
→ Lower `MinBasinSize`

### Waterfalls Everywhere
→ Raise `WaterfallThreshold` (currently 0.05)

---

## Known Limitations

### 1. Flat Terrains
If terrain is perfectly flat:
- No flow directions calculated
- No rivers form
- Expected behavior (no slope = no flow)

**Solution:** Ensure terrain has elevation variation

### 2. Multiple Outlets
If a basin has multiple low points at same elevation:
- Only one outlet is chosen (first found)
- Other potential outlets ignored

**Solution:** Accept this (real lakes also have single outlet usually)

### 3. Circular Flow
**Cannot happen** with this algorithm because:
- Flow always goes to LOWER neighbor
- Topological sort ensures upstream→downstream processing
- No tile can flow to itself or create cycle

---

## Validation Checks

The algorithm should validate:

✅ **No circular flow:** Every flow path eventually reaches ocean or sink
✅ **Accumulation consistency:** Sum of all river accumulations = number of land tiles
✅ **Edge continuity:** For every tile flowing East, eastern neighbor receives from West
✅ **Basin coverage:** All flat areas assigned to a basin
✅ **Feature validity:** Waterfalls only where rivers actually flow

---

## Integration with TileDesc

After hydrology generation, BakeTiles converts to TileDesc:

```csharp
// For each tile:
tileDesc.WaterFlags = DetermineWaterFlags(tile);
tileDesc.RiverFlowIn = CalculateInflowEdges(tile);
tileDesc.RiverFlowOut = world.FlowDirection[tile];
tileDesc.RiverOrder = CalculateRiverOrder(world.FlowAccumulation[tile]);
tileDesc.LakeId = world.DrainageBasinId[tile];

// If waterfall/rapids:
if (world.IsWaterfall[tile])
    tileDesc.FeatureMask |= FeatureFlags.Waterfall;
if (world.IsRapids[tile])
    tileDesc.FeatureMask |= FeatureFlags.Rapids;
```

---

## References

- **Topological Sorting:** https://en.wikipedia.org/wiki/Topological_sorting
- **Flow Accumulation:** Standard GIS algorithm, used in ArcGIS, QGIS
- **D8 vs D6:** We use D6 (6 directions) because hex grid, same principle as D8 (square)
- **Strahler Order:** Alternative to accumulation-based classification (future enhancement)

---

## Future Enhancements

**Not yet implemented:**

1. **Strahler Stream Order:** More sophisticated river classification
2. **Lake Elevation Filling:** Simulate water level rise in basins
3. **Multiple Outlets:** Allow basins to drain to multiple neighbors
4. **Groundwater Flow:** Subsurface flow in flat areas
5. **Seasonal Flow:** Variable river volumes (dry season vs wet)

These can be added later without changing core algorithm.
