# MapMaker Core — CHANGELOG

This file tracks changes to the MapMaker Core infrastructure (Driver, Pipeline, Export, Logging).
Module-specific changes are tracked in their respective CHANGELOG.md files.

---

## 2026-02-06 : Module 2 (Latitude) Integration

### Core/Pipeline
- **HB_MapConfig.cs**: Added `ThreeToFiveBandHeightThreshold` field for latitude band mode selection
- **HB_PipelineConfig.cs**: Added `HB_LatitudeConfig` reference and `EnableLatitude` toggle

### Core/Driver
- **MapMakerDriver.cs**: Integrated Module 2 execution flow with validation and export passes

### Shared/Export
- **WorldExportPass.cs**: Added latitude-specific export methods:
  - `ExportLatitudeBandsPng()` - Dedicated latitude band visualization
  - `ExportStackedPng_WithLatitude()` - Combined elevation + latitude overlay
  - `ColorForLatitude()` - Color mapping for latitude bands (White/Green/Yellow)

### Shared/Data
- **Enums.cs**: Updated `LatitudeBandType` enum:
  - Renamed `Tundra` → `Arctic` for clarity
  - Added XML documentation

### Documentation
- **Core/Docs/DevPlan.md**: Marked Module 2 as complete with full specification
- **Core/Docs/HB_MapConfig.md**: Created documentation for HB_MapConfig including new threshold field

---

## 2026-02-05 : Module 1 (Elevation) Complete

### Core Infrastructure Established
- Driver orchestration with thin design pattern
- Logging system with file output and console mirroring
- Pipeline ScriptableObject architecture
- WorldArrays centralized buffer system
- SeedContext deterministic RNG streams
- Export system with PNG generation

### Initial Modules
- Module 1 (Elevation) implemented and documented
