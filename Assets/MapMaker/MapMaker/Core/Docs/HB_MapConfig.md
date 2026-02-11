# HB_MapConfig Documentation

## Overview
`HB_MapConfig` is the central ScriptableObject that defines map-wide parameters shared across all modules. It is referenced by `HB_PipelineConfig` and passed to the `MapMakerDriver`.

## File Location
`/Assets/MapMaker/MapMaker/Core/Pipeline/HB_MapConfig.cs`

## Configuration Fields

### Map Size
- **MapWidth** (int, min 1, default 250): Width of the world map in tiles
- **MapHeight** (int, min 1, default 250): Height of the world map in tiles

### Seed
- **RootSeed** (int, default 123456): Master seed for deterministic world generation. All module RNG streams derive from this seed via `SeedContext`.

### Latitude Bands
- **ThreeToFiveBandHeightThreshold** (int, min 1, default 1500): 
  - If `MapHeight < ThreeToFiveBandHeightThreshold`: Use 3 latitude bands (Arctic/Temperate/Tropical)
  - If `MapHeight >= ThreeToFiveBandHeightThreshold`: Use 5 latitude bands (Arctic/Temperate/Tropical/Temperate/Arctic) for hemisphere symmetry

## Validation
`ValidateOrThrow()` ensures:
- MapWidth and MapHeight are positive
- Throws `InvalidOperationException` if validation fails

## Usage in Modules
- **Module 1 (Elevation)**: Uses MapWidth, MapHeight, RootSeed
- **Module 2 (Latitude)**: Uses MapHeight, RootSeed, ThreeToFiveBandHeightThreshold

## Design Rationale

### Why ThreeToFiveBandHeightThreshold?
Small maps (< 1500 tiles) don't benefit from 5 distinct climate zones. 3-band mode:
- Prevents tiny, unplayable biome regions
- Simplifies biome assignment for smaller worlds
- Reduces visual clutter on compact maps

Large maps (>= 1500 tiles) use 5-band mode:
- Adds northern and southern hemisphere symmetry
- Provides richer biome diversity
- Supports more realistic planetary climate simulation

## Example Asset
Default instance: `/Assets/MapMaker/MapMaker/Core/Pipeline/HB_MapConfig_Default.asset`

## Changelog
- **2026-02-06**: Added `ThreeToFiveBandHeightThreshold` for Module 2 (Latitude)
- **2026-02-05**: Initial creation for Module 1 (Elevation)
