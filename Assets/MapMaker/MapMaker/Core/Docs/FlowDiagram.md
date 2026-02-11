# MapMaker Execution Flow (Programmatic)

This describes the runtime order and the required method responsibilities.

## A) High-level run

1. **Trigger**
   - UI button calls `MapMakerTrigger.Run()`

2. **Driver initialization**
   - Binds logging (file + optional console if already supported by existing pipeline config)
   - Reads pipeline config SO
   - Logs run header (seed + dimensions + enabled steps)

3. **Allocate shared buffers**
   - Driver allocates `WorldArrays` once per run based on width/height. 

4. **Execute enabled modules in order**
   - For each enabled module:
     1) Log `Phase Begin`
     2) Run module pass(es)
     3) Run module validation
     4) Run exporter for that module’s PNG(s)
     5) Update stacked PNG (excluding latitude)
     6) Log `Phase End`

5. **Shutdown**
   - Logs completion line
   - Flushes/ends file logger

## B) Per-module micro-flow (mandatory)

For each module:

### 1) Validate inputs (pre)
- Module pass must validate config ranges that can be validated locally.
- On failure: log error and return early.

### 2) Execute pass
- Mutate only the module-owned outputs.
- Avoid touching arrays that belong to other modules unless explicitly part of the module contract.

### 3) Validate outputs (post)
- Validate invariants for that module.
- Return success/failure.

### 4) Export
- Exports are centralized in Core/Shared/Export.
- Module provides a lightweight “what to export” signal (driver decides).

## C) Stacked PNG rule

After each module completes:
- Driver calls exporter to re-render `WorldPreview_Stacked.png` as:
  - Elevation + Coast + Mountains + ...
  - Excluding latitude always.

Latitude still produces its own PNG.

# MapMaker – Execution Flow

1. UI Trigger
2. Driver.Run()
3. Bind Logger
4. Load Pipeline Config
5. Allocate WorldArrays
6. Build SeedContext
7. For each enabled module:
   a. Validate
   b. Execute
   c. Export (if enabled)
8. Export stacked PNG
9. Shutdown log