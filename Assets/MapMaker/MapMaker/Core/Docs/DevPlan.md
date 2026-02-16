# MapMaker – Development Plan (Current)

This document defines the authoritative development plan for MapMaker as of this baseline.
All work proceeds strictly module-by-module.
No future module may be started until the prior module is complete, validated, and documented.

---

## Core Principles

- Thin driver only (orchestration, logging start/stop, sequencing)
- No world-generation logic in the driver
- No hardcoded runtime values in code (use ScriptableObjects)
- Each module owns:
  - Its configuration (ScriptableObject)
  - Its execution pass(es)
  - Its validation pass
  - Its documentation
- Logging is centralized, frozen, and reused as-is
- All changes must be documented in module PatchLogs

---

## Module Order (Authoritative)

### Module 1 – Elevation
**Purpose:** Generate base terrain height and elevation bands.

**Inputs:**
- MapWidth (Pipeline Config)
- MapHeight (Pipeline Config)
- RootSeed (Pipeline Config)
- HB_ElevationConfig (Module Config)

**Outputs:**
- WorldArrays.elevationRaw[]
- WorldArrays.elevationBands[]

**Notes:**
- No coastline logic
- No latitude logic
- No hydrology
- Pure elevation substrate only

---

### Module 2 – Latitude ✓ COMPLETE
**Purpose:** Assign latitude bands (Arctic, Temperate, Tropical) to world tiles based on Y-coordinate with Perlin warping.

**Inputs:**
- WorldArrays dimensions
- RootSeed (SeedContext.LatitudeRng)
- HB_LatitudeConfig (band percentages, warp tunables)
- HB_MapConfig.ThreeToFiveBandHeightThreshold

**Outputs:**
- WorldArrays.LatitudeBands[] (Arctic/Temperate/Tropical)

**Mode Selection:**
- 3-band mode: Maps with height < ThreeToFiveBandHeightThreshold (default 1500)
- 5-band mode: Maps with height >= ThreeToFiveBandHeightThreshold (adds hemisphere symmetry)

**Exports:**
- WorldPreview_02_LatitudeBands.png (Arctic=White, Temperate=Green, Tropical=Yellow)
- WorldPreview_Stacked.png (updated to include latitude overlay)

**Status:** Implementation complete, tested, documented

---

### Module 3 – Coast
**Purpose:** Determine ocean, shelf, and land boundaries.

**Inputs:**
- elevationBands
- RootSeed (Coast RNG stream)
- HB_CoastConfig

**Outputs:**
- isDeepOcean[]
- isCoastalShelf[]

---

### Module 4 – Mountains (Planned)
Adds mountain and hill features on top of elevation.

---

### Module 5 – Hydrology (Planned)
Adds rivers and lakes (non-dynamic).

---

### Module 6 – Moisture (Planned)
Static moisture distribution (no climate simulation).

---

### Module 7 – Biomes (Planned)
Biome assignment from elevation + latitude + moisture.

---

## Export Stages

- Per-module PNG exports (via Core/Export)
- One stacked PNG excluding latitude
- No module performs file I/O directly

---

## Completion Definition for a Module

A module is considered complete when:
- It runs without compile warnings
- Validation passes succeed
- Outputs are visible in exported PNGs
- Docs and PatchLog are updated

# Adding Update
Final update
