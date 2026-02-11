# Module 2 — Latitude

## Purpose
Assigns latitude bands (Arctic, Temperate, Tropical) to world tiles based on Y-coordinate with Perlin noise warping for natural climate zone boundaries.

## Status
✓ **COMPLETE** - Implementation finished, tested, and documented (2026-02-06)

## Key Features
- **Adaptive Band Mode**: Automatically switches between 3-band and 5-band layouts based on map height
- **Perlin Warping**: Natural, irregular climate boundaries instead of straight lines
- **Deterministic**: Uses `SeedContext.LatitudeRng` for repeatable generation
- **Configurable**: All percentages and warp parameters exposed via ScriptableObject

## Module Structure

```
2_Latitude/
├── Config/
│   └── HB_LatitudeConfig.cs          # ScriptableObject configuration
├── Scripts/
│   ├── LatitudeGenerator.cs          # Core generation logic
│   └── LatitudeValidate.cs           # Validation and logging
├── Docs/
│   ├── ModuleSpec.md                 # Complete specification
│   └── CHANGELOG.md                  # Version history
└── README.md                         # This file
```

## Quick Start

### 1. Create Configuration Asset
Right-click in Project window:
`Create > Humble Beginnings > MapMaker > Module 2 - Latitude > Config`

### 2. Configure Band Percentages
**3-Band Mode (Small Maps):**
- Arctic: 15%
- Temperate: 60%
- Tropical: 25%

**5-Band Mode (Large Maps):**
- North Arctic: 12%
- North Temperate: 29%
- Tropical (Equator): 18%
- South Temperate: 29%
- South Arctic: 12%

### 3. Adjust Warping (Optional)
- **BandWarpNoiseScale**: Larger = smaller warp features (default 0.02)
- **BandWarpStrength**: Max boundary offset (default 0.05 = ±5%)

### 4. Set Threshold in HB_MapConfig
- **ThreeToFiveBandHeightThreshold**: Height cutoff for band mode (default 1500)

### 5. Enable in Pipeline
- Assign config to `HB_PipelineConfig.LatitudeConfig`
- Check `EnableLatitude` checkbox
- Run `MapMakerDriver`

## Outputs

### Runtime Data
- `WorldArrays.LatitudeBands[]` - Populated with `LatitudeBandType` enum values

### PNG Exports
- `WorldPreview_02_LatitudeBands.png` - Isolated latitude visualization
- `WorldPreview_Stacked.png` - Elevation + latitude overlay

### Color Coding
- **White**: Arctic (polar/tundra zones)
- **Green**: Temperate (mid-latitude zones)
- **Yellow**: Tropical (equatorial zones)

## Dependencies
- **Required**: None (standalone module)
- **Used By**: Module 7 (Biomes) for climate-based biome assignment

## Validation
Module validates on run:
- Warns if band percentages don't sum to 1.0 (auto-normalizes)
- Warns if warp scale/strength out of recommended range
- Logs final band distribution percentages

## Performance
- **Complexity**: O(width × height) - single pass
- **Memory**: No additional allocations beyond WorldArrays
- **Typical Runtime**: < 50ms for 2000×2000 map

## Documentation
For detailed algorithm description, see `/Modules/2_Latitude/Docs/ModuleSpec.md`

## Known Limitations
- Latitude is purely Y-coordinate based (no planetary curvature)
- Warp is uniform across map (no regional variation)
- Band percentages must be manually adjusted if they don't sum to 1.0

## Changelog
See `/Modules/2_Latitude/Docs/CHANGELOG.md` for version history.
