# Sample Module Notes

Purpose: Template for creating new MapMaker modules.

## Replace-me checklist
- Rename folder `Sample_Module` to your module name (e.g., `3_Coast`).
- Rename namespaces under `MapMaker.Modules.<YourModule>`.
- Replace config fields in `HB_SampleModuleConfig` with module settings.
- Replace `SampleModulePass` + `SampleModuleValidate` with real logic.

## Inputs
- Config: `HB_SampleModuleConfig` (ScriptableObject)
- World buffers: `WorldArrays`
- RNG: `SeedContext` (System.Random streams)
- Logger: `LogEmitter emit`

## Outputs
- Mutations to `WorldArrays` buffers owned by this module (define explicitly in your module docs).

## Logging contract
Use ONLY:
`emit(LogLevel.<...>, LogContext.<...>, LogPhase.<...>, "KEY", "message")`

Do not add new enum values to `LogContext` or `LogPhase`.
