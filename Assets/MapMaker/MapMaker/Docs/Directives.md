# MapMaker Directives (Contract)

This document is the contract for all MapMaker coding work.

## 0) Baseline-first rule (prevents drift)
- **Before writing or modifying code**, open the current baseline project (zip/repo) and verify:
  - namespaces and folder paths
  - enum values (logging, band types, etc.)
  - public APIs (method signatures, config fields)
- If anything needed is not present in the baseline, **stop and ask**. Do not guess.
- Recommended workflow for multi-turn work: **user uploads the current baseline zip before any coding step**.

## 1) Architecture boundaries
### 1.1 Driver
- `Core/Driver/MapMakerDriver.cs` stays a **thin orchestrator**.
- The driver:
  - binds logging
  - reads ScriptableObject configs from the pipeline
  - allocates/clears shared buffers
  - calls module Validate/Execute in order (per pipeline toggles)
  - calls exporter(s)
- The driver **must not**:
  - generate terrain/noise
  - perform flood fills / neighbor iteration
  - implement module logic

### 1.2 Shared
- Shared utilities live under `MapMaker/Shared/*`.
- Modules may use Shared helpers, but Shared must stay generic (no module-specific logic).

### 1.3 Modules
- Each module owns:
  - `Config/` ScriptableObjects (only that module’s tunables)
  - `Scripts/` pass + validate code
  - `Docs/` notes + patch log entries
- Module scripts must not touch logging internals (no direct file handling, no binder changes).

## 2) Tunables rule (no hardcoding)
- Do not hardcode **any** data values or figures in execution code that need to be adjusted during runtime/configuration.
  - Examples: map size, seed, percents, thresholds, bias direction/strength, pixel sizes, output folders.
- All tunables must live in ScriptableObject configs (Core or Module Config).
- Acceptable hardcoded constants are limited to:
  - array indexing helpers
  - small epsilon values for numerical stability
  - fixed enum mappings that are not intended to be user-configurable

## 3) Logging contract (frozen system)
### 3.1 How to log
- Modules receive an emitter delegate:
  - `MapMakerLogEmitter emit`
- Use:
  - `emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "<KEY>", "<MESSAGE>")`

### 3.2 No convenience wrappers
- Do not add `.Info()`, `.Warn()`, etc.
- Do not add new overloads to the emitter.

### 3.3 Allowed enum values (must use existing)
**LogLevel**
- `INFO`
- `WARN`
- `ERROR`

**LogContext**
- `Driver`
- `Module`
- `Export`
- `Logging`

**LogPhase**
- `Init`
- `Validation`
- `Generation`
- `Export`
- `Shutdown`
- `Begin`
- `End`
- `Skip`
- `Progress`
- `Evaluated`
- `Assigned`

### 3.4 Module identification
- Do not invent new LogContext values.
- Identify the module using the log `key`, for example:
  - `"ELEVATION"`, `"LATITUDE"`, `"COAST"`, `"SAMPLE"`.

## 4) Validation philosophy
- Prefer **warnings + clamping** over hard failures for content-shaping constraints.
  - Example: if percentages are slightly off due to rounding, normalize and warn.
- Use hard errors only for structural impossibilities:
  - null required configs
  - non-positive map dimensions
  - sums that make classification impossible (e.g., total percent <= 0 or > 1 without a normalization path)

## 5) Export rules
- Centralized exporters live under `MapMaker/Shared/Export`.
- Each module may request exports, but module code should not write files directly.
- PNG requirements:
  - per-module focus images (e.g., Elevation, Latitude, Coast masks)
  - one cumulative **stacked** PNG that includes all layers added so far **except latitude**

## 6) Documentation and patch logging
- Each module has `Modules/<ModuleName>/Docs/PatchLog.md`.
- Every change to code or config in a module must add a patch log entry:
  - `YYYY-MM-DD HH:mm | <file> | <change> | <reason> | <summary>`
- Core changes go in `Core/Docs/PatchLog.md` (if present) or `Core/Docs/DevPlan.md` change notes.

## 7) Delivery rules
- Deliveries are **zip files**.
- For any changed script, provide a **full-file replacement**, not partial edits.
- Avoid manual edit instructions in chat.
