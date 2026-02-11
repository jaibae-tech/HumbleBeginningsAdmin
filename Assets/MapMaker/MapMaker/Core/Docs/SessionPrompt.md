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

### Module 2 – Latitude
**Purpose:** Assign latitude bands to world tiles.

**Inputs:**
- WorldArrays dimensions
- RootSeed (SeedContext stream)
- HB_LatitudeConfig

**Outputs:**
- WorldArrays.latitudeBands[]

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
