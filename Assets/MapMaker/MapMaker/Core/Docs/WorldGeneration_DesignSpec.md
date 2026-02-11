# World Generation Design Specification

**Version:** 1.0  
**Last Updated:** 2026-02-10  
**Status:** Foundation - Hex grid confirmed

---

## Overview

This document defines the complete world generation system for a 2.5D strategy game with rotatable terrain view and hex-based tactical gameplay.

---

## End Goal Visualization

**Reference Images:** `Core/Docs/ReferenceImages/`
- `terrain_3d_relief.png` - Target visual quality
- `river_networks.png` - Natural dendritic rivers
- `coastal_features.png` - Ocean depth, shelves, inlets
- `mountain_valleys.png` - Elevation-based terrain
- `world_overview.png` - Full map context

**Key Visual Features:**
- 3D relief-shaded terrain with realistic hillshading
- Natural river networks flowing from mountains to ocean
- Proper coastal transitions (deep ocean → shelf → land)
- Rotatable/zoomable camera (2.5D perspective)
- Grid overlay appears at tactical zoom (50×50 tiles visible)

---

## Scale & Performance

### World Size
- **Minimum:** 800×800 tiles (640,000 tiles)
- **Target:** 1600×1600 tiles (2,560,000 tiles)
- **Maximum:** 2500×2500 tiles (6,250,000 tiles)

### Memory Budget
- **WorldArrays (temporary):** <300 MB during generation
- **TileDesc (permanent):** <200 MB for 2500×2500 world
- **Per-tile storage:** 24 bytes in TileDesc

### Generation Time
- **Target:** <5 minutes for 1600×1600
- **Maximum acceptable:** <15 minutes for 2500×2500

---

## Grid System: Hexagonal

**Decision:** Hexagonal tiles (6 neighbors per tile)

**Rationale:**
- Equidistant neighbors (no diagonal distance problem)
- Better for tactical movement/combat
- More natural terrain flow
- Industry standard for strategy games

**Coordinate System:** Offset coordinates (odd-r)
- Odd rows offset +0.5 in X direction
- Standard 2D array indexing: `index = y * width + x`
- Neighbor calculation varies by row parity

**Direction Encoding:**
```
Direction 0 = E   (East)
Direction 1 = SE  (Southeast)  
Direction 2 = SW  (Southwest)
Direction 3 = W   (West)
Direction 4 = NW  (Northwest)
Direction 5 = NE  (Northeast)
```

**See:** `Core/Docs/GridSystem_Specification.md` for complete implementation details

---

## Module Pipeline

Modules execute in strict order. Each module reads from and writes to **WorldArrays**.

### Generation Phases

| # | Module | Input | Output | Purpose |
|---|--------|-------|--------|---------|
| 1 | Elevation | Seed | ElevationRaw[], ElevationBands[] | Terrain height, land/ocean |
| 2 | Latitude | Height | LatitudeBands[] | Climate zones |
| 3 | Coast | Elevation, Land | IsCoastalShelf[], IsInlandLake[] | Ocean features, lakes |
| 4 | Hydrology | Elevation | FlowDirection[], FlowAccumulation[] | Rivers, drainage |
| 5 | Moisture | Hydro, Latitude | MoistureLevel[] | Precipitation |
| 6 | Biome | All climate | BiomeId[] | Terrain types |
| 7 | Features | All previous | FeatureMask[], FeatureRefs[] | Landmarks, dungeons |
| 8 | Roads | Features, Elevation | RoadNetwork[] | Path between cities |
| 9 | BakeTiles | All WorldArrays | TileDesc[] | Final gameplay data |

### Module Dependencies

```
Elevation (seed-based, no deps)
    ↓
Latitude (Y-coordinate based)
    ↓
Coast (needs elevation + land mask)
    ↓
Hydrology (needs elevation, uses GridSystem)
    ↓
Moisture (needs hydrology + latitude)
    ↓
Biome (needs temperature + moisture)
    ↓
Features (needs biome + elevation + hydrology)
    ↓
Roads (needs features + pathfinding)
    ↓
BakeTiles (needs everything)
```

**See:** `Core/Docs/ModulePipeline_Interfaces.md` for detailed contracts

---

## Data Architecture

### Two-Layer System

**Layer 1: WorldArrays (Generation)**
- Purpose: Temporary working data during generation
- Precision: Full float precision, uncompressed
- Lifetime: Discarded after BakeTiles completes
- Size: ~200-300 MB for 1600×1600

**Layer 2: TileDesc (Gameplay)**
- Purpose: Authoritative data for all gameplay systems
- Precision: Quantized, compressed, optimized
- Lifetime: Permanent, serialized in save games
- Size: 24 bytes × tile count

**Critical Rule:** Gameplay systems read ONLY TileDesc, never WorldArrays.

**See:** `Core/Docs/TileDesc_Format.md` for complete structure

---

## Rendering System

### World View (Zoomed Out)
- Render as continuous 3D heightmap
- Dynamic hillshading based on camera angle
- Rivers rendered as flow lines (not discrete tiles)
- No grid visible
- Smooth LOD transitions

### Tactical View (50×50 Zoom)
- Hex grid overlay appears
- Tiles are discrete, clickable units
- Each tile has defined boundaries
- Highlighting, selection, movement preview

### Data Flow
```
TileDesc[] 
  → Extract elevation → Build 3D mesh
  → Extract rivers → Render flow lines
  → Extract biomes → Apply textures
  → Extract features → Place 3D models
```

---

## Hydrology System: Flow Accumulation

**Approach:** Physics-based flow simulation (not pathfinding)

**Algorithm:** D6 flow direction (6 hex neighbors)

### Phase 1: Flow Direction
- Each tile determines steepest descent neighbor
- Stores direction (0-5) water flows TO

### Phase 2: Flow Accumulation  
- Topological sort (high elevation → low)
- Count tiles draining through each tile
- Result: Drainage area per tile

### Phase 3: River Classification
- Threshold accumulation → river exists
- riverOrder = log scale of accumulation
- Encodes IN/OUT edges for tile boundaries

### Phase 4: Basin Detection
- Basins emerge as local minima
- No pre-placement required
- Natural lake formation

**Benefits:**
- Guaranteed edge continuity (physics-based)
- Natural dendritic networks
- Matches reference image aesthetics
- No pathfinding complexity

**See:** `Core/Docs/Hydrology_FlowAccumulation.md` for implementation

---

## Gameplay Integration

### Encounter Generation

When player clicks tile at (x, y):
```csharp
TileDesc tile = world.GetTile(x, y);

// Determine encounter type
if (tile.FeatureMask & FeatureFlags.Dungeon)
    → Generate dungeon entrance
else if (tile.WaterFlags & WaterFlags.River)
    → Generate river crossing
else
    → Generate standard terrain encounter

// Build combat map
CombatMap map = GenerateCombatArena(
    seed: tile.TileSeed,
    elevation: tile.ElevationQ,
    slope: tile.SlopeQ,
    biome: tile.BiomeId,
    moisture: tile.MoistureQ,
    riverFlow: tile.RiverFlowIn | tile.RiverFlowOut,
    roads: tile.RoadEdges
);
```

### Feature Continuity

Rivers, roads, and features MUST align at tile edges:
```
If Tile A has river exiting EAST:
  → Tile B (to the east) MUST have river entering from WEST
  
Validation occurs in BakeTiles phase.
Any misalignment = generation bug, must fix.
```

---

## Export Visualizations

### Required Exports (for debugging/tuning)

| File | Purpose | Shows |
|------|---------|-------|
| WorldPreview_01_ElevationBands.png | Elevation classification | Ocean, lowland, highlands, mountains |
| WorldPreview_02_LatitudeBands.png | Climate zones | Arctic, temperate, tropical |
| WorldPreview_03_Coast.png | Ocean features | Deep ocean, shelf, inland lakes |
| WorldPreview_04_Hydrology.png | Water systems | Rivers, basins, flow |
| WorldPreview_05_Moisture.png | Precipitation | Dry, moderate, wet areas |
| WorldPreview_06_Biomes.png | Terrain types | Forest, desert, tundra, etc. |
| WorldPreview_ShadedRelief.png | 3D visualization | Hillshaded terrain + features |
| WorldPreview_Topographic.png | Grayscale elevation | Pure heightmap |

### Shaded Relief Requirements
- Hillshading from NW at 45° angle
- Hypsometric tinting (elevation-based color)
- River overlay (blue lines)
- Lake overlay (cyan)
- Waterfall markers (red)
- Rapids markers (orange)

---

## Validation Requirements

### Module Output Validation

Each module must validate its output before proceeding:
- **Elevation:** No mountains adjacent to ocean, proper land/ocean ratio
- **Coast:** All ocean connected, coastal shelf present, no isolated lakes <MinSize
- **Hydrology:** No circular flow, all rivers reach ocean or terminal basin, edge continuity
- **Biomes:** No invalid combinations (e.g., tundra at equator)

### BakeTiles Validation

Before finalizing TileDesc[], must verify:
- ✅ All river edges align with neighbors
- ✅ All road edges align with neighbors  
- ✅ All feature crossings align
- ✅ No invalid tile states
- ✅ All required fields populated

If validation fails → Log errors, abort generation, fix bug.

---

## Future Expansion

### Not Yet Implemented (Phase 2)
- Weather systems (rain, snow, wind)
- Seasonal changes
- Player-caused terrain modification
- Dynamic feature destruction
- Erosion simulation

### Design Constraints
- Terrain is IMMUTABLE post-generation
- Features can be destroyed (flip bit in FeatureMask)
- Rivers/roads never change
- Elevation never changes

---

## References

- `Core/Docs/GridSystem_Specification.md` - Hex coordinate system details
- `Core/Docs/TileDesc_Format.md` - Gameplay data structure
- `Core/Docs/Hydrology_FlowAccumulation.md` - River generation algorithm
- `Core/Docs/ModulePipeline_Interfaces.md` - Module contracts
- `Shared/Data/GridSystem.cs` - Core grid utilities
- `Shared/Data/TileDesc.cs` - Tile descriptor struct
