# World Build to Runtime - Development Plan

## Executive Summary

This plan bridges MapMaker's world generation output to a playable hex-based strategy game where players click tiles to trigger battles on 2.5D terrain representations.

**Current State:** MapMaker generates elevation data and exports PNGs  
**Target State:** Interactive hex map with clickable tiles, region system, anchors, POIs, and battle integration

---

## Phase 1: Complete World Generation (MapMaker Modules)

### 1.1 Module 2 - Latitude ✅ Ready to Start

**Effort:** 2-4 hours

**Inputs:**

- WorldArrays dimensions
- SeedContext
- HB_LatitudeConfig

**Outputs:**

- WorldArrays.latitudeBands[] (Arctic, Temperate, Tropical)

**Tasks:**

- [ ] Create HB_LatitudeConfig.cs with latitude band percentages
- [ ] Create LatitudeGenerator.cs with Y-coordinate + noise logic
- [ ] Add validation pass
- [ ] Add PNG export (color-coded latitude bands)
- [ ] Update driver to execute latitude module
- [ ] Test and document

---

### 1.2 Module 3 - Coast

**Effort:** 4-6 hours

**Inputs:**

- elevationBands
- SeedContext
- HB_CoastConfig

**Outputs:**

- WorldArrays.isDeepOcean[]
- WorldArrays.isCoastalShelf[]

**Tasks:**

- [ ] Create HB_CoastConfig.cs
- [ ] Create CoastGenerator.cs using GridHelpers.FloodFillEdges()
- [ ] Define deep ocean vs coastal shelf rules
- [ ] Add validation pass
- [ ] Add PNG export showing ocean/shelf/land
- [ ] Update stacked PNG to include coast
- [ ] Test and document

---

### 1.3 Module 4 - Mountains

**Effort:** 4-6 hours

**Inputs:**

- elevationBands
- SeedContext
- HB_MountainsConfig

**Outputs:**

- WorldArrays.mountainFeatures[]

**Tasks:**

- [ ] Create HB_MountainsConfig.cs
- [ ] Create MountainsGenerator.cs using GridHelpers neighbor analysis
- [ ] Define mountain chain logic vs isolated peaks
- [ ] Add validation pass
- [ ] Add PNG export
- [ ] Update stacked PNG
- [ ] Test and document

---

### 1.4 Module 5 - Hydrology

**Effort:** 8-12 hours (complex)

**Inputs:**

- elevationRaw
- elevationBands
- mountainFeatures
- SeedContext
- HB_HydrologyConfig

**Outputs:**

- WorldArrays.rivers[]
- WorldArrays.lakes[]

**Rules:**

- Rivers originate in Mountains/High Mountains
- Flow downhill only
- Terminate in ocean or lakes
- Continuous paths
- No unnatural splits

**Tasks:**

- [ ] Create HB_HydrologyConfig.cs
- [ ] Create river flow algorithm (downhill pathfinding)
- [ ] Create lake formation logic
- [ ] Add validation (no uphill flow, continuous paths)
- [ ] Add PNG export
- [ ] Update stacked PNG
- [ ] Test and document

---

### 1.5 Module 6 - Moisture

**Effort:** 4-6 hours

**Inputs:**

- rivers
- lakes
- isDeepOcean
- isCoastalShelf
- SeedContext
- HB_MoistureConfig

**Outputs:**

- WorldArrays.moisture[]

**Tasks:**

- [ ] Create HB_MoistureConfig.cs
- [ ] Create MoistureGenerator.cs using GridHelpers.ComputeDistanceField()
- [ ] Calculate distance from water sources
- [ ] Apply moisture falloff
- [ ] Add validation pass
- [ ] Add PNG export
- [ ] Update stacked PNG
- [ ] Test and document

---

### 1.6 Module 7 - Biomes

**Effort:** 6-8 hours

**Inputs:**

- elevationBands
- latitudeBands
- moisture
- isDeepOcean
- SeedContext
- HB_BiomesConfig

**Outputs:**

- WorldArrays.biomes[] (forest, plains, desert, swamp, jungle, tundra, mountains, etc.)

**Tasks:**

- [ ] Create BiomeType enum
- [ ] Create HB_BiomesConfig.cs with biome assignment rules
- [ ] Create BiomeGenerator.cs with lookup tables
- [ ] Implement biome assignment logic (elevation + latitude + moisture → biome)
- [ ] Add validation pass
- [ ] Add PNG export with distinct biome colors
- [ ] Update stacked PNG as final world preview
- [ ] Test and document

---

## Phase 2: Region System

### 2.1 Region Generation

**Effort:** 8-12 hours

**Purpose:** Create contiguous terrain clusters for game mechanics

**Rules from BackGround.md:**

- Regions are contiguous tiles with similar terrain/elevation
- Maximum 10% of total tiles per region (configurable)
- Oversized clusters must be split
- Regions absorb lakes (lakes don't form own regions)
- Each region gets ONE underground entrance

**Outputs:**

- WorldArrays.regionIDs[] (tile → region mapping)
- List regions with metadata

**Region Data Structure:**

```csharp
public class Region
{
    public int RegionID;
    public BiomeType DominantBiome;
    public ElevationBand ElevationFamily;
    public List<int> TileIndices;
    
    // Initial values (static at generation)
    public float StartingDanger;
    public float StartingThreat;
    public float MinDanger;
    public float MaxDanger;
    public float MinThreat;
    public float MaxThreat;
    public bool Active; // starts false
    
    // POI
    public int UndergroundEntranceTile;
    
    // Runtime (not generated here)
    public Anchor AssignedAnchor; // max one
}
```

**Tasks:**

- [ ] Create Region.cs data class
- [ ] Create HB_RegionConfig.cs with size limits and rules
- [ ] Create RegionGenerator.cs
- [ ] Implement flood-fill region clustering by biome + elevation family
- [ ] Implement region splitting for oversized clusters
- [ ] Validate one underground entrance per region
- [ ] Export region map PNG (each region unique color)
- [ ] Serialize regions to JSON/ScriptableObject
- [ ] Test and document

---

### 2.2 Danger & Threat Initialization

**Effort:** 4-6 hours

**Purpose:** Calculate initial danger/threat values for each region

**Inputs:**

- Regions
- Future anchor positions (positive/hostile)
- Terrain hostility config

**Danger Calculation:**

- Distance to positive anchors (farther = higher danger)
- Terrain hostility (mountains/swamps higher)
- Distance to hostile anchors (closer = higher danger)

**Threat Calculation:**

- Proximity to hostile anchors
- Terrain capacity for spawns

**Tasks:**

- [ ] Create DangerThreatConfig.cs
- [ ] Create DangerThreatCalculator.cs
- [ ] Implement distance-based danger calculation (placeholder until anchors exist)
- [ ] Implement threat calculation
- [ ] Assign min/max caps per region
- [ ] Export danger heatmap PNG
- [ ] Export threat heatmap PNG
- [ ] Test and document

---

## Phase 3: Anchor & Hierarchy System

### 3.1 Surface Anchor Placement

**Effort:** 8-12 hours

**Rules from BackGround.md:**

- 3 starting positive anchors (bias to one map edge)
- 3-5 hostile anchors (bias to opposite edge)
- Maximum ONE anchor per region
- Terrain-appropriate placement

**Positive Anchor Examples:**

- Elf city → largest forest
- Dwarf city → largest/deepest mountain range
- Human coastal → coast near river

**Hostile Anchor Examples:**

- Orc stronghold → mountains on hostile edge
- Dark forest fortress → corrupted forest
- Desert raiders → hostile edge desert

**Tasks:**

- [ ] Create Anchor.cs data class (type, position, faction, hierarchy)
- [ ] Create HB_AnchorConfig.cs (placement rules, factions)
- [ ] Create AnchorPlacer.cs
- [ ] Implement positive anchor placement (find largest suitable regions on player edge)
- [ ] Implement hostile anchor placement (opposite edge, terrain-appropriate)
- [ ] Validate max one anchor per region
- [ ] Update Region.AssignedAnchor references
- [ ] Export anchor map PNG
- [ ] Serialize anchors
- [ ] Test and document

---

### 3.2 Underground Anchor Generation

**Effort:** 4-6 hours

**Rules from BackGround.md:**

- Permanent, unkillable
- No surface entrances
- Mission-only access
- Used for threat/danger calculations

**Tasks:**

- [ ] Create UndergroundAnchor.cs (Dwarven deep city, Drow city, Abyssal nodes)
- [ ] Create HB_UndergroundConfig.cs
- [ ] Create UndergroundAnchorPlacer.cs
- [ ] Link underground anchors to surface regions (influence)
- [ ] Update danger/threat calculations to include underground influence
- [ ] Serialize underground anchors
- [ ] Test and document

---

### 3.3 Hierarchy Generation

**Effort:** 6-8 hours

**Rules from BackGround.md:**

- One dominant hierarchy per anchor
- Leader (highest difficulty)
- Lieutenants
- Subordinates
- Strength scaled by region danger/threat
- Start UNDISCOVERED

**Tasks:**

- [ ] Create Hierarchy.cs data class
- [ ] Create HierarchyMember.cs (Leader/Lieutenant/Subordinate)
- [ ] Create HB_HierarchyConfig.cs (scaling rules, difficulty)
- [ ] Create HierarchyGenerator.cs
- [ ] Generate hierarchies for each anchor
- [ ] Scale difficulty by danger/threat
- [ ] Mark all as undiscovered
- [ ] Serialize hierarchies
- [ ] Test and document

---

## Phase 4: POI (Points of Interest) System

### 4.1 POI Placement

**Effort:** 6-8 hours

**Rules from BackGround.md:**

- ONE POI per tile maximum (excluding landmarks)
- Camps, lairs, temporary strongholds near hostile anchors
- All start UNDISCOVERED
- Difficulty scaled to region danger

**Tasks:**

- [ ] Create POI.cs data class (type, position, difficulty, discovered flag)
- [ ] Create POIType enum (Camp, Lair, Stronghold, Cave, Ruin, etc.)
- [ ] Create HB_POIConfig.cs (density, placement rules)
- [ ] Create POIPlacer.cs
- [ ] Place POIs near hostile anchors (density by threat)
- [ ] Assign difficulty by region danger
- [ ] Mark underground entrances as POIs (one per region)
- [ ] Validate one POI per tile
- [ ] Export POI map PNG
- [ ] Serialize POIs
- [ ] Test and document

---

## Phase 5: Data Serialization & Export

### 5.1 World Data Format

**Effort:** 4-6 hours

**Purpose:** Save generated world to disk for runtime loading

**Tasks:**

- [ ] Create WorldData.cs master container
- [ ] Include: tiles, regions, anchors, hierarchies, POIs, rivers, lakes
- [ ] Create JSON serializer/deserializer
- [ ] Export to `<ExportPath>/WorldData_<Seed>.json`
- [ ] Create binary format option (if JSON too large)
- [ ] Add world data validation on load
- [ ] Test round-trip save/load
- [ ] Document format

---

### 5.2 Tile Data Export

**Effort:** 2-4 hours

**Purpose:** Export per-tile data for runtime

**Tile Data Structure:**

```csharp
public struct TileData
{
    public int X, Y;
    public BiomeType Biome;
    public ElevationBand Elevation;
    public float ElevationRaw;
    public int RegionID;
    public bool IsOcean;
    public bool IsRiver;
    public bool IsLake;
    public int POIID; // -1 if none
}
```

**Tasks:**

- [ ] Create TileData struct
- [ ] Export tile array to JSON
- [ ] Create tile lookup utilities
- [ ] Test and document

---

## Phase 6: Runtime Integration (Game Side)

### 6.1 World Loader

**Effort:** 4-6 hours

**Purpose:** Load generated world data into game runtime

**Tasks:**

- [ ] Create WorldLoader.cs
- [ ] Load WorldData JSON at game start
- [ ] Deserialize tiles, regions, anchors, POIs
- [ ] Create in-memory world state
- [ ] Add error handling for missing/corrupt data
- [ ] Test with multiple seeds
- [ ] Document API

---

### 6.2 Hex Grid Renderer

**Effort:** 12-16 hours

**Purpose:** Display hex grid in game window

**Tasks:**

- [ ] Research hex grid rendering in Unity (flat-top or pointy-top)
- [ ] Create HexGrid.cs component
- [ ] Create HexCell prefab with sprite/material
- [ ] Implement hex → world position conversion
- [ ] Implement camera controls (pan, zoom)
- [ ] Render biomes with distinct colors/sprites
- [ ] Render rivers/lakes overlay
- [ ] Add fog of war (undiscovered tiles hidden)
- [ ] Optimize for large maps (chunk loading, culling)
- [ ] Test performance with 300×300 maps
- [ ] Document

---

### 6.3 Tile Click & Selection System

**Effort:** 6-8 hours

**Purpose:** Allow players to click tiles for interaction

**Tasks:**

- [ ] Implement hex click detection (raycasts or colliders)
- [ ] Create TileSelector.cs
- [ ] Highlight selected tile
- [ ] Show tile info panel (biome, region, POI, etc.)
- [ ] Handle adjacent tile selection (movement range)
- [ ] Add selection sound/VFX
- [ ] Test click accuracy
- [ ] Document

---

### 6.4 2.5D Battle Terrain Generator

**Effort:** 16-24 hours (complex)

**Purpose:** Generate 2.5D battle scenes from tile data

**Rules from BackGround.md:**

- Battles take place on 2.5D representation of tiles
- Terrain affects combat (elevation, cover, etc.)

**Tasks:**

- [ ] Create BattleTerrainGenerator.cs
- [ ] Map biomes to 3D terrain prefabs/tilesets
- [ ] Generate battle scene from TileData
- [ ] Add elevation variation within tile (micro-terrain)
- [ ] Place 3D props (trees, rocks, ruins) by biome
- [ ] Create transition logic (player clicks tile → load battle scene)
- [ ] Implement camera setup for 2.5D view
- [ ] Test with all biome types
- [ ] Optimize scene generation time
- [ ] Document

---

### 6.5 Region & Anchor Visualization

**Effort:** 4-6 hours

**Purpose:** Show regions and anchors on hex map

**Tasks:**

- [ ] Render region boundaries (subtle outlines)
- [ ] Display anchor icons on map (city/fortress markers)
- [ ] Show discovered anchors vs undiscovered
- [ ] Color-code anchors (friendly/hostile/neutral)
- [ ] Add tooltip on hover (anchor name, faction)
- [ ] Test and document

---

### 6.6 POI Markers & Discovery

**Effort:** 4-6 hours

**Purpose:** Show POIs on map when discovered

**Tasks:**

- [ ] Create POI marker prefabs (camp icon, lair icon, etc.)
- [ ] Show POIs only when discovered
- [ ] Handle POI click (show info, start mission, etc.)
- [ ] Add discovery animation/sound
- [ ] Test and document

---

## Phase 7: Game Systems Integration

### 7.1 Movement System

**Effort:** 8-12 hours

**Purpose:** Allow player to move units on hex map

**Tasks:**

- [ ] Create Unit.cs (position, movement range, faction)
- [ ] Implement hex pathfinding (A* on hex grid)
- [ ] Calculate movement cost by terrain
- [ ] Show movement range overlay
- [ ] Handle unit movement animation
- [ ] Test and document

---

### 7.2 Danger & Threat Runtime

**Effort:** 6-8 hours

**Purpose:** Track danger/threat changes during gameplay

**Tasks:**

- [ ] Load initial danger/threat from world data
- [ ] Create runtime danger/threat modifiers (missions, tile control)
- [ ] Update region danger/threat when anchors are affected
- [ ] Visualize danger zones on map (heatmap overlay)
- [ ] Test and document

---

### 7.3 Mission System Hooks

**Effort:** 4-6 hours

**Purpose:** Connect map to mission system

**Tasks:**

- [ ] Create mission trigger points (click POI → start mission)
- [ ] Pass tile/region/POI data to mission system
- [ ] Handle mission completion (POI destruction, danger reduction)
- [ ] Test and document

---

## Phase 8: Polish & Optimization

### 8.1 Performance Optimization

**Effort:** 8-12 hours

**Tasks:**

- [ ] Profile large map rendering (300×300 tiles)
- [ ] Implement tile chunking/streaming
- [ ] Optimize PNG export (parallel processing)
- [ ] Reduce memory footprint
- [ ] Test on target hardware

---

### 8.2 UI/UX Polish

**Effort:** 8-12 hours

**Tasks:**

- [ ] Create map legend (biome colors, icons)
- [ ] Add minimap
- [ ] Improve tile info panel (rich tooltips)
- [ ] Add map filters (show/hide layers)
- [ ] Implement search/goto location
- [ ] Test usability

---

### 8.3 Documentation

**Effort:** 4-6 hours

**Tasks:**

- [ ] Write user guide (how to generate worlds)
- [ ] Write integration guide (how to use world data in game)
- [ ] Document all config options
- [ ] Create tutorial scene
- [ ] Add tooltips to all configs

---

## Validation Against BackGround.md

### ✅ Aligned Requirements

### 🔧 Additions Needed (Not in Current Plan)

**None** - Current plan fully supports BackGround.md requirements

---

## Timeline Estimates

### Fast Track (Minimal Features)

- **Phase 1 (Modules 2-7):** 28-44 hours (~1-2 weeks)
- **Phase 2 (Regions):** 12-18 hours (~3-5 days)
- **Phase 3 (Anchors/Hierarchies):** 18-26 hours (~5-7 days)
- **Phase 4 (POIs):** 6-8 hours (~2 days)
- **Phase 5 (Serialization):** 6-10 hours (~2 days)
- **Phase 6 (Runtime Integration):** 42-60 hours (~2-3 weeks)
- **Phase 7 (Game Systems):** 18-26 hours (~5-7 days)
- **Phase 8 (Polish):** 20-30 hours (~1 week)

**Total: 150-222 hours (~4-6 weeks full-time, or 2-3 months part-time)**

---

## Dependencies & Critical Path

```
Phase 1 (Modules) → MUST complete first (no runtime without biomes)
    ↓
Phase 2 (Regions) → Depends on biomes
    ↓
Phase 3 (Anchors) → Depends on regions
    ↓
Phase 4 (POIs) → Depends on regions + anchors
    ↓
Phase 5 (Serialization) → Depends on all generation complete
    ↓
Phase 6 (Runtime) → Can start in parallel after Phase 5 data format locked
    ↓
Phase 7 (Game Systems) → Depends on Phase 6 hex grid
    ↓
Phase 8 (Polish) → Ongoing
```

---

## Risk Assessment

### High Risk

- **Hydrology (Phase 1.4):** River flow algorithm complex, may take longer
- **Hex Grid Performance (Phase 6.2):** Large maps (300×300) may need heavy optimization
- **Battle Terrain Generator (Phase 6.4):** 2.5D scene generation is complex

### Medium Risk

- **Region Splitting:** Oversized region split logic may be tricky
- **Pathfinding:** Hex A* requires careful testing

### Low Risk

- All other modules/phases are well-scoped

---

## Recommended Start Order

1. **Start here:** Module 2 - Latitude (fast win, builds confidence)
2. **Then:** Module 3 - Coast (uses new GridHelpers utilities)
3. **Then:** Module 6 - Moisture (distance fields, good practice)
4. **Then:** Module 4 - Mountains (neighbor analysis)
5. **Then:** Module 5 - Hydrology (save complex for when experienced)
6. **Then:** Module 7 - Biomes (integration finale)
7. **Then:** Phase 2+ in order

---

## Success Criteria

### Phase 1 Success

- ✅ All 7 modules complete
- ✅ Stacked PNG shows final world
- ✅ 0 compilation errors
- ✅ Same seed = same world

### Phase 2-5 Success

- ✅ Regions generated and validated
- ✅ Anchors placed correctly
- ✅ POIs seeded
- ✅ WorldData.json exports successfully
- ✅ Round-trip save/load works

### Phase 6-7 Success

- ✅ Hex map renders in game
- ✅ Player can click tiles
- ✅ Battle scenes generate from tile data
- ✅ Movement works on hex grid
- ✅ Performance acceptable (60fps on 300×300 map)

### Final Success

- ✅ Player loads world
- ✅ Clicks tile
- ✅ Battle starts on 2.5D terrain matching tile biome
- ✅ All systems integrated
- ✅ Deterministic world generation proven

---

## Next Immediate Action

**Start Module 2 - Latitude** (2-4 hours)

This is the fastest path to progress and validates the module creation workflow before tackling harder modules.