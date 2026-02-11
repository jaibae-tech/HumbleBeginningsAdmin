# ModuleSpec — 01 Elevation

## Purpose
Generate base terrain elevation using Perlin noise combined with continental gradients.
Creates realistic continents that flow from biased edge(s) toward the interior, with natural terrain variation.

## ScriptableObject Inputs

### HB_ElevationConfig
**Noise Parameters:**
- NoiseScale: Perlin sampling scale (default: 120)
- Octaves: Number of noise octaves 1-8 (default: 4)
- Persistence: Amplitude falloff per octave 0.1-1 (default: 0.5)
- Lacunarity: Frequency increase per octave 1-4 (default: 2.0)

**Elevation Bands (must sum to 1.0):**
- OceanTotalPercent: Combined DeepOcean + Ocean (default: 0.15)
- OceanMaxPercent: Maximum ocean for validation (default: 0.40)
- DeepOceanShareWithinOcean: Fraction of ocean that is deep 0-1 (default: 0.30)
- LowlandPercent: (default: 0.40)
- HighlandsPercent: (default: 0.23)
- LowMountainsPercent: (default: 0.14)
- HighMountainsPercent: (default: 0.08)

**Continental Gradient:**
- EdgeBias: Which edge(s) have land (None/West/East/North/South/All)
- ContinentalGradientStrength: 0-2, strength of continent (0=island, 1=moderate, 2=strong)
- ContinentalGradientReach: 0.1-1, how far gradient extends (0.1=coastal, 1=full span)
- ContinentalGradientPower: 0.5-3, gradient curve shape (1=linear, >1=sharp)

**Edge Ocean Guarantee:**
- OppositeEdgeOceanMargin: Tiles of guaranteed ocean at opposite edge (0-20)

**Coastline Variation:**
- CoastlineNoiseScale: Scale for coastline irregularity noise (default: 0.02)
- CoastlineNoiseStrength: 0-0.5, strength of coastal irregularity (default: 0.15)

### HB_MapConfig (from pipeline)
- MapWidth: Map width in tiles
- MapHeight: Map height in tiles
- RootSeed: Master seed for deterministic generation

### HB_ExportConfig (from pipeline)
- ExportFolderName: Where to save PNG exports
- ExportTilePixelSize: Pixels per tile in exports
- ExportFlipVertical: Whether to flip Y-axis in exports

## Runtime Inputs

### WorldArrays
- ElevationRaw (written): Raw elevation values 0-1
- ElevationBands (written): Discrete elevation bands (DeepOcean/Ocean/Lowland/Highlands/LowMountains/HighMountains)

### SeedContext
- ElevationRng: RNG stream for deterministic noise offsets

## Algorithm

### Phase 1: Raw Elevation Generation (ElevationGenerator)
For each tile (x, y):
1. **Sample base Perlin noise**: Multi-octave noise for terrain variation
2. **Compute continental gradient**: Based on EdgeBias direction
   - West bias: High at x=0 (west edge), low at x=width-1 (east edge)
   - East bias: High at x=width-1, low at x=0
   - North bias: High at y=height-1 (north edge), low at y=0 (south edge)
   - South bias: High at y=0, low at y=height-1
   - All bias: High at all edges, low at center (ring continent)
   - None: Neutral (pure noise, archipelago mode)
3. **Combine noise + gradient**: elevation = noise * 0.4 + gradient * 0.6
4. **Add coastline variation**: Modulate transition zone with additional noise
5. **Apply ocean margin**: Guarantee ocean at opposite edge via smooth falloff

Result: ElevationRaw[] contains values 0-1 representing terrain height

### Phase 2: Band Assignment (ElevationBandAssigner)
1. Normalize band percentages to sum to 1.0
2. Compute quantile thresholds for each band based on target percentages
3. Assign each tile to a discrete band based on its raw elevation value

Result: ElevationBands[] contains discrete terrain types

## Validation (WARN only)
- Count of LowMountains+HighMountains adjacent to Ocean/DeepOcean
- Log if percentages don't sum to 1.0
- Log if opposite edge has land touching it

## Exports
- WorldPreview_01_ElevationBands.png: Color-coded elevation bands
- WorldPreview_Stacked.png: Cumulative layer visualization (excluding latitude)

## How EdgeBias Creates Different Worlds

### EdgeBias = West
- **Landmass**: Starts at west edge (x=0), extends eastward
- **Ocean**: Concentrated at east edge
- **Result**: Continent flowing west → east
- **Use Case**: Players start at west coast, travel east inland to higher elevations

### EdgeBias = East
- **Landmass**: Starts at east edge, extends westward
- **Ocean**: Concentrated at west edge
- **Result**: Continent flowing east → west

### EdgeBias = All
- **Landmass**: Starts at all four edges, extends toward center
- **Ocean**: Can be in center OR landmass closes to form solid continent (depends on ContinentalGradientReach)
- **Result**: Ring continent or solid central landmass
- **Use Case**: Island continent surrounded by ocean

### EdgeBias = None
- **Landmass**: Pure noise distribution (archipelago)
- **Ocean**: Scattered throughout
- **Result**: Many islands of various sizes
- **Use Case**: Naval gameplay, scattered island chains

## Parameter Tuning Guide

### For Solid Continent (West/East/North/South):
```
ContinentalGradientStrength = 1.2
ContinentalGradientReach = 0.85
ContinentalGradientPower = 1.5
OppositeEdgeOceanMargin = 5
CoastlineNoiseStrength = 0.15
```

### For Ring Continent (All):
```
ContinentalGradientStrength = 1.0
ContinentalGradientReach = 0.6  # Don't reach all the way to center
ContinentalGradientPower = 1.2
OppositeEdgeOceanMargin = 5
CoastlineNoiseStrength = 0.2  # More irregular for interesting shape
```

### For Archipelago (None):
```
ContinentalGradientStrength = 0
# Other gradient params ignored
CoastlineNoiseStrength = 0.3  # High variation for many small islands
```

## Design Rationale

### Why Combine Noise + Gradient?
- **Noise alone**: Creates random islands with no directional flow
- **Gradient alone**: Creates boring linear transitions
- **Combined**: Realistic continents with natural terrain variation

The 40/60 split (40% noise, 60% gradient) ensures:
- Gradient dominates for overall continent shape
- Noise provides enough variation for interesting terrain
- Coastlines are natural and irregular (not straight lines)

### Why Coastline Noise Layer?
Creates realistic irregular coastlines by varying elevation in the transition zone.
Without it, coastlines would follow the smooth gradient curve too closely.

### Why Opposite Edge Ocean Margin?
Guarantees that land never touches forbidden edges, even with high noise rolls.
This prevents:
- Land "wrapping around" the map edge
- Disconnected landmasses later removed by Coast module
- Confusing map boundaries

### Why Power Curve for Gradient?
Allows fine control over continent shape:
- Power < 1: Gentle slope (coastal plains extend far inland)
- Power = 1: Linear gradient
- Power > 1: Sharp drop-off (steep coastal mountains, then plateau)

## Integration with Downstream Modules

### Module 2 (Latitude)
- Reads: Nothing from Elevation
- Independent: Assigns latitude bands based on Y-coordinate only

### Module 3 (Coast)
- Reads: ElevationBands[]
- Classifies: DeepOcean, Ocean, CoastalShelf, InlandLakes
- Note: With proper gradient, coast module should NOT need to remove edge-touching landmasses

### Module 5 (Hydrology)
- Reads: ElevationBands[]
- Creates: Rivers and lakes WITHIN the landmass
- Note: Elevation creates NO inland seas - those are added by Hydrology

## Known Limitations

### Perlin Noise Periodicity
Unity's Perlin noise repeats at large coordinates. For very large maps (>4000 tiles), 
patterns may repeat. Solution: Use multiple noise layers with different offsets.

### Quantile-Based Band Assignment
If noise + gradient creates very flat terrain (all values clustered), 
band assignment may not hit target percentages exactly. Solution: Increase noise octaves
or adjust ContinentalGradientStrength.

### Edge Cases
- All ocean map: Possible if ContinentalGradientStrength = 0 and low noise
- All land map: Possible if ContinentalGradientStrength >> 2 and high noise
- Validation will warn but not block these cases

## Performance Notes
- Generation: O(W × H) single pass
- Band Assignment: O(W × H log(W × H)) due to sorting for quantiles
- Typical 1000×1000 map: ~50ms total

## Changelog
- 2026-02-07: Rewrote to use noise + continental gradients instead of cellular growth
- 2026-02-06: Added directional growth (REMOVED - overcomplicated)
- 2026-02-05: Initial noise-based implementation
