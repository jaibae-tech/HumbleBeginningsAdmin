# Module 01 — Elevation

## Purpose
Generate base terrain elevation substrate for layered world-building.

## Approach
**Noise + Continental Gradients**
- Perlin noise provides terrain variation
- Directional gradients create continental flow
- Coastline noise adds irregular coasts
- Ocean margins prevent edge-touching land

## Non-goals
- No coastline shelf (Module 3)
- No hydrology/rivers/lakes (Module 5)
- No climate simulation
- No inland seas (those are intentional water bodies from Hydrology)

## Key Concept
The elevation layer creates the **base landmass shape** that all other modules build upon.
- EdgeBias determines WHERE land exists (west edge, all edges, etc.)
- Gradient determines HOW MUCH land (strength/reach/power)
- Noise determines WHAT IT LOOKS LIKE (mountains, valleys, plains)

## Configuration Quick Start

### Solid West Continent:
```
EdgeBias = West
ContinentalGradientStrength = 1.2
ContinentalGradientReach = 0.85
OppositeEdgeOceanMargin = 5
```

### Ring Continent (All Edges):
```
EdgeBias = All
ContinentalGradientStrength = 1.0
ContinentalGradientReach = 0.6
OppositeEdgeOceanMargin = 5
```

### Archipelago (Islands):
```
EdgeBias = None
ContinentalGradientStrength = 0
CoastlineNoiseStrength = 0.3
```

## Important Notes
- No inland seas are created at this stage
- All water bodies visible are OCEAN
- Lakes/rivers come later from Hydrology module
- Latitude is independent and doesn't affect elevation
