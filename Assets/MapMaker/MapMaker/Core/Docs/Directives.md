# MapMaker Directives (Contract)

This document is the contract for MapMaker module development. It exists to prevent drift.

## 0) Non-negotiable rules

1. **Do not modify Core/Logging.**
   - Use the existing logging implementation exactly as-is:
     - `Core/Logging/MapMakerLogging.cs`
     - `Core/Logging/MapMakerLogBinder.cs`
     - `Core/Logging/LogSource_MapMaker.asset`
   - If something does not compile, fix the calling code to match logging, not the other way around.

2. **No hardcoded tuning values in execution code.**
   - Do not hardcode **any** data values/figures that might be adjusted during runs (map sizes, seeds, percents, thresholds, noise scales, etc).
   - All adjustable values must live in **ScriptableObjects** in the module’s `Config/` folder (or driver config SO for driver-level settings).

3. **Driver is a thin orchestrator.**
   - Driver responsibilities:
     - Bind logging
     - Load pipeline config + module configs
     - Allocate shared runtime buffers (WorldArrays) once
     - Call module passes in order (if enabled)
     - Call per-step validation (if enabled)
     - Call exporter methods
   - Driver must not contain generation logic (no Perlin sampling, no flood fills, no feature placement).

4. **Modules do the work; Core does orchestration and export.**
   - Modules:
     - Read their own config SO
     - Operate on `WorldArrays`
     - Use RNG streams from `SeedContext`
     - Emit logs through provided `emit(...)` delegate
   - Export is centralized in `Core/Shared/Export/WorldExportPass.cs`.

5. **Deliveries are zip + full file replacements.**
   - No manual edits in chat.
   - If a file changes, provide it as a full replacement in the zip.

6. **Every code change requires a patch log entry.**
   - Update `Modules/<Module>/Docs/PatchLog.md` with:
     - timestamp
     - file(s) changed
     - summary reason
     - brief chat context
     - any invariants impacted

## 1) Folder structure (must follow)

`Assets/MapMaker/MapMaker/`
- `Core/`
  - `Logging/` **(FROZEN)**
  - `Driver/` (thin orchestrator + trigger)
  - `Pipeline/` (pipeline SO + definitions)
  - `Shared/`
    - `Data/` (WorldArrays + shared enums)
    - `Export/` (PNG export)
    - `Utils/` (SeedContext, GridHelpers, etc)
  - `Docs/` (this contract + flow + module spec)
- `Modules/`
  - `Sample_Module/` (template; copy for new modules)
  - `1_Elevation/`, `2_Latitude/`, `3_Coast/`, ...

## 2) Logging contract

### 2.1 What modules receive
Modules must not call logging internals. They receive:

```csharp
Action<LogLevel, LogContext, LogPhase, string, string> emit
```

They log via:

```csharp
emit(LogLevel.INFO, LogContext.<MODULE>, LogPhase.<PHASE>, "Key", "Message");
```

### 2.2 No convenience wrappers
- Do not add `.Info()`, `.Warn()`, etc on the emitter.
- Do not add new enums or phases inside module code.

## 3) Module execution contract

Each module provides:
- **Config SO** in `Modules/<Module>/Config/`
- **Pass script(s)** in `Modules/<Module>/Scripts/`
- **Validate script** in `Modules/<Module>/Scripts/`
- **Docs** in `Modules/<Module>/Docs/`

### 3.1 Preferred signatures

Pass (work):
```csharp
public static void Execute(
    WorldArrays a,
    <ModuleConfig> cfg,
    SeedContext seed,
    Action<LogLevel, LogContext, LogPhase, string, string> emit)
```

Validation:
```csharp
public static bool Validate(
    WorldArrays a,
    <ModuleConfig> cfg,
    Action<LogLevel, LogContext, LogPhase, string, string> emit)
```

### 3.2 Validation philosophy
- Prefer **self-contained per-module validation** after each module runs.
- Cross-module validation (only if needed) belongs in `Core/Validation/` later.
- Each module validation must:
  - return `true/false`
  - log **specific reasons** on failure
  - never mutate arrays

## 4) Export contract

Exporter outputs per module:
- `WorldPreview_<StepName>.png` (e.g., `WorldPreview_Elevation.png`)
- Optional additional masks for the module (e.g., `WorldPreview_OceanMask.png`)

Additional required image:
- `WorldPreview_Stacked.png` = stacked visualization of all completed layers **excluding latitude**.

Stacked order (as layers exist):
1. Elevation bands (land only)
2. Coast overlays (deep ocean + shelf)
3. Mountains/Hills later
4. Hydrology later
5. Moisture later
6. Biomes later

Latitude is never drawn on the stacked image.

## 5) RNG contract

- Use `System.Random` (not `UnityEngine.Random`) inside passes.
- RNG streams come from `SeedContext` (e.g., `seed.Elevation`, `seed.Latitude`, etc).
- No module creates its own random seed ad-hoc.

## 6) Documentation requirement for every module

Each module must include a `Docs/ModuleNotes.md` that contains:
- Purpose
- Inputs: exact config fields read
- Inputs: exact arrays read
- Outputs: exact arrays written
- Export outputs produced
- Validation rules & invariants

# MapMaker – Engineering Directives (Authoritative)

This document is binding. Violations are bugs.

---

## Absolute Rules

1. **Do not hardcode any data values or figures in execution code that need to be adjusted at runtime.**
   - All tunables must live in ScriptableObjects.

2. **Do not modify logging infrastructure.**
   - Use existing MapMakerLogging.cs and MapMakerLogBinder.cs only.

3. **Do not invent new logging APIs.**
   - All logging uses the provided emitter delegate.

4. **Do not invent new LogContext or LogPhase values.**
   - Use only the enums defined in Core/Logging.

5. **No compile fixes by changing unrelated systems.**
   - Fix the calling code, not the callee.

6. **No manual code edits in chat deliveries.**
   - All deliveries are ZIPs.
   - Either single-file replacement or full-folder replacement.

---

## Logging Contract

Modules receive a logging delegate:

Action<LogLevel, LogContext, LogPhase, string, string>

Usage example:

emit(
  LogLevel.Info,
  LogContext.Module,
  LogPhase.Generation,
  "Elevation",
  "Assigned elevation bands"
);

---

## Available LogPhase Values

The following phases are currently allowed:

`Init, Validation, Generation, Export, Shutdown, Begin, End, Skip, Progress, Evaluated, Assigned`


```csharp
Init,
Validation,
Generation,
Export,
Shutdown,
Begin,
End,
Skip,
Progress,
Evaluated,
Assigned
