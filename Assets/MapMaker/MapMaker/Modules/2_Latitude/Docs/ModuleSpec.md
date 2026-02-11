# ModuleSpec — 02 Latitude

## Overview
Module 2 generates latitude bands (Arctic, Temperate, Tropical) based on Y-coordinate with Perlin noise warping for natural boundary variation. Supports 3-band mode for small maps and 5-band mode for large maps.

## ScriptableObject Inputs

### HB_LatitudeConfig
**3-Band Mode (Map Height < Threshold):**
- ThreeBandArcticPercent: Percentage of map for Arctic zones (default 15%)
- ThreeBandTemperatePercent: Percentage for Temperate zone (default 60%)
- ThreeBandTropicalPercent: Percentage for Tropical zone (default 25%)

**5-Band Mode (Map Height >= Threshold):**
- FiveBandNorthArcticPercent: North polar region (default 12%)
- FiveBandNorthTemperatePercent: North temperate zone (default 29%)
- FiveBandTropicalPercent: Equatorial zone (default 18%)
- FiveBandSouthTemperatePercent: South temperate zone (default 29%)
- FiveBandSouthArcticPercent: South polar region (default 12%)

**Band Warping:**
- BandWarpNoiseScale: Scale for Perlin noise that warps boundaries (default 0.02)
- BandWarpStrength: Maximum offset as fraction of map height (default 0.05 = ±5%)

### HB_MapConfig (from pipeline)
- MapWidth/MapHeight/RootSeed
- ThreeToFiveBandHeightThreshold: Height threshold for band mode selection (default 1500)

### HB_ExportConfig (from pipeline)
- ExportFolderName/ExportTilePixelSize/ExportFlipVertical

## Runtime Inputs

### WorldArrays
- LatitudeBands (written)

### SeedContext
- LatitudeRng (used for Perlin noise offsets)

## Algorithm

### Band Mode Selection
```
if (MapHeight < ThreeToFiveBandHeightThreshold):
    use 3-band mode (Arctic/Temperate/Tropical)
else:
    use 5-band mode (Arctic/Temperate/Tropical/Temperate/Arctic)
```

### Band Assignment
1. Normalize Y-coordinate: `normalizedY = y / (height - 1)` (0 = bottom/south, 1 = top/north)
2. Apply Perlin warp: `warp = Perlin((x + ox) * scale, (y + oy) * scale)`
3. Add offset: `adjustedY = clamp(normalizedY + warpOffset, 0, 1)`
4. Assign band based on threshold ranges

### 3-Band Layout (South to North)
```
Y = 0 (bottom)    → Tropical (25%)
                  → Temperate (60%)
Y = Height-1 (top) → Arctic (15%)
```

### 5-Band Layout (South to North, Equator at Center)
```
Y = 0 (bottom)     → Arctic (12%)
                   → Temperate (29%)
Y = Height/2       → Tropical (18%)  ← Equator
                   → Temperate (29%)
Y = Height-1 (top) → Arctic (12%)
```

## Validation (WARN only)
- Percentage sums for 3-band and 5-band modes should equal 1.0 (±0.05 tolerance)
- BandWarpNoiseScale must be positive
- BandWarpStrength should be 0-0.2

## Outputs
- WorldArrays.LatitudeBands[] populated with LatitudeBandType enum values

## Exports
- WorldPreview_02_LatitudeBands.png (Arctic=White, Temperate=Green, Tropical=Yellow)
- WorldPreview_Stacked.png (updated to include latitude overlay)

## Dependencies
- **Inputs:** None (standalone module, only uses map dimensions and seed)
- **Outputs Used By:** Module 7 (Biomes) for biome assignment logic

## Performance Notes
- Linear O(width × height) complexity
- Perlin noise called once per tile
- No neighbor analysis or pathfinding

## Design Rationale

### Why 3 vs 5 Bands?
Small maps (< 1500 tiles height) don't need full biome diversity. 3-band mode simplifies generation and prevents tiny, unplayable biome regions.

### Why Perlin Warping?
Hard latitude lines look artificial. Perlin warping creates natural-looking climate zone boundaries that interact organically with coastlines and elevation.

### Why Equator at Center (5-band)?
Symmetrical layout prevents gameplay bias toward one map edge. Players can start in temperate zones on either side of the equator.

## Usage Example
```csharp
var cfg = Resources.Load<HB_LatitudeConfig>("Configs/HB_Latitude_Default");
var seed = new SeedContext(mapConfig.RootSeed);
var emit = /* logging emitter */;

var gen = new LatitudeGenerator(cfg, mapConfig.ThreeToFiveBandHeightThreshold, seed, emit);
gen.Execute(worldArrays);

LatitudeValidate.LogBandDistribution(worldArrays, emit);
```

## Known Limitations
- Latitude is purely Y-coordinate based (no planetary curvature simulation)
- Warp strength is global (cannot vary by region)
- Band percentages must be manually adjusted if they don't sum to 1.0
