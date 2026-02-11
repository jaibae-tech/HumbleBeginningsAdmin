# Elevation Module — Changelog

## v2.0 — 2026-02-07
### Complete Rewrite: Noise + Continental Gradients

**Breaking Changes:**
- Completely new config parameters
- Removed all directional growth parameters
- Removed old edge bias parameters

**New Features:**
- Continental gradient system for realistic continent placement
- Configurable gradient strength, reach, and power curve
- Coastline noise layer for irregular coasts
- Opposite edge ocean margin with smooth falloff
- Support for ring continents (EdgeBias = All)

**Improvements:**
- Solid continents instead of fragmented archipelagos
- No unwanted inland seas at elevation layer
- Predictable continent shapes based on EdgeBias
- Better parameter control over continent size/shape

**Parameters Added:**
- ContinentalGradientStrength (0-2)
- ContinentalGradientReach (0.1-1)
- ContinentalGradientPower (0.5-3)
- OppositeEdgeOceanMargin (0-20 tiles)
- CoastlineNoiseScale
- CoastlineNoiseStrength (0-0.5)

**Parameters Removed:**
- UseDirectionalGrowth
- SeedStripDepth, SeedStripVariation
- GrowthDirectionality, GrowthClumping
- GrowthNoiseScale, GrowthNoiseStrength
- EdgeBiasStrength, EdgeBiasFalloff, EdgeBiasExponent
- EdgeBiasNoiseScale
- EdgeMarginTiles

---

## v1.1 — 2026-02-06 (Deprecated)
### Attempted Directional Growth

**Status:** Removed in v2.0

Attempted cellular growth algorithm to create directional landmasses.
Failed to produce solid continents - created fragmented archipelagos instead.

---

## v1.0 — 2026-02-05
### Initial Implementation

Basic Perlin noise elevation with soft edge bias.
Created random islands but lacked directional continental flow.
