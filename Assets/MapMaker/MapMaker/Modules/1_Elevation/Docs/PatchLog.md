# Elevation Module — Patch Log

## 2026-02-07 — Complete Rewrite: Noise + Gradients

**Changed Files:**
- Config/HB_ElevationConfig.cs
- Scripts/ElevationGenerator.cs
- Docs/ModuleSpec.md
- Docs/ModuleNotes.md

**Summary:**
Completely rewrote elevation generation to use **Perlin noise + continental gradients** instead of the previous cellular growth approach.

**Reason:**
Previous directional growth implementation created fragmented archipelagos with too many inland seas. 
The new approach combines:
1. Base Perlin noise for terrain variation
2. Directional gradients for continental shape
3. Coastline noise for irregular coasts
4. Ocean margin guarantees for clean edges

**Key Changes:**

### Config
- Removed: UseDirectionalGrowth, SeedStripDepth, SeedStripVariation, GrowthDirectionality, GrowthClumping, GrowthNoiseScale, GrowthNoiseStrength
- Removed: EdgeBiasStrength, EdgeBiasFalloff, EdgeBiasExponent, EdgeMarginTiles (noise-based params)
- Added: ContinentalGradientStrength, ContinentalGradientReach, ContinentalGradientPower
- Added: OppositeEdgeOceanMargin (replaces EdgeMarginTiles with clearer naming)
- Added: CoastlineNoiseScale, CoastlineNoiseStrength

### Generator
- Removed: All cellular growth/frontier/distance field logic
- Added: ComputeContinentalGradient() - creates directional land/ocean gradient
- Added: ComputeNormalizedDistanceFromBiasEdge() - measures distance from bias edge
- Added: ComputeOppositeEdgeMarginFalloff() - guarantees ocean at forbidden edges
- Algorithm now: noise + gradient + coastline variation + ocean margin

### Results
- EdgeBias = West: Solid continent from west edge flowing eastward
- EdgeBias = All: Ring continent from all edges toward center
- EdgeBias = None: Pure noise archipelago
- NO inland seas (those are added later by Hydrology)
- Clean edges (no land touching forbidden boundaries)

**Invariants Impacted:**
- Elevation generation is now fully deterministic based on seed (was always true, remains true)
- No landmasses touch opposite edges (now guaranteed by OppositeEdgeOceanMargin)
- Elevation flows from bias edge toward interior (NEW capability)

**Testing:**
- Verified with EdgeBias = West: Clean continent from west edge
- Verified with EdgeBias = All: Ring continent shape
- Verified opposite edge has no land within margin distance
- Verified band assignment produces expected percentages

---

## 2026-02-06 — Added Directional Growth (REMOVED)

**Status:** This approach was removed in 2026-02-07 rewrite. Kept for historical record.

**What it was:**
Attempted to create landmasses via cellular growth from seed strips at edges.

**Why it failed:**
- Too probabilistic, created Swiss cheese landmasses
- Many inland seas that shouldn't exist at elevation layer
- Growth was slow and unpredictable
- Didn't match the "layered world-building" approach

**Lessons learned:**
- Cellular growth is good for organic shapes but bad for continents
- Need deterministic gradient-based approach
- Noise should provide variation, not primary shape

---

## 2026-02-05 — Initial Implementation

**Files:**
- Config/HB_ElevationConfig.cs
- Scripts/ElevationGenerator.cs
- Scripts/ElevationBandAssigner.cs
- Scripts/ElevationValidate.cs

**Implementation:**
Original noise-based elevation with edge bias via soft reduction near edges.

**Issues:**
- Edge bias was too soft, land could still touch edges randomly
- No directional "flow" from edge to interior
- Created random islands instead of solid continents

---

## Changelog Notes

All changes follow MapMaker directives:
- No hardcoded values (all in ScriptableObjects)
- Uses existing logging system (emit delegate)
- No modifications to Core/Logging
- Single-pass generation for performance
- Deterministic via SeedContext.ElevationRng
