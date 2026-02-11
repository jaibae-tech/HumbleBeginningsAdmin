# ModuleSpec Template

## Module Name
- Module #: (e.g., 01)
- Module Key: (e.g., ELEV)
- Purpose:

## Inputs
### ScriptableObject Inputs
List every field read and what it does.

### Runtime Inputs
- WorldArrays fields read
- SeedContext RNG streams used
- Any shared helpers used

## Outputs
### Runtime Outputs
List arrays written and invariants.

### Exports
List PNGs written by WorldExportPass.

## Validation
- What is checked
- What emits WARN vs what aborts (default: WARN + continue)
