# Inputs / Outputs

## Config
- **Enabled** (bool): Whether module runs.
- **Note** (string): Debug note (template only).

## Runtime Inputs
- **mapWidth, mapHeight**: Taken from Driver Config (ScriptableObject).
- **WorldArrays**: Shared buffers; modules read/write their relevant arrays.
- **SeedContext**: Deterministic RNG streams. Use `seed.Root` or add named streams in SeedContext (do not use UnityEngine.Random).

## Runtime Outputs
- Module writes into WorldArrays. Exports are handled centrally.

