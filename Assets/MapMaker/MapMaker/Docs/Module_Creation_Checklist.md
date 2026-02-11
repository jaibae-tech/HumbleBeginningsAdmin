# Module Creation Checklist

Use this checklist when creating a new MapMaker module to ensure consistency and completeness.

---

## Pre-Development

### 1. Design Phase
- [ ] Module purpose clearly defined
- [ ] Input dependencies identified (which arrays/configs needed)
- [ ] Output arrays specified (what gets written to WorldArrays)
- [ ] Algorithm approach selected (avoid overengineering)
- [ ] Performance considerations noted

### 2. Documentation Setup
- [ ] Create `Modules/N_ModuleName/Docs/` folder
- [ ] Create `ModuleSpec.md` (inputs, outputs, algorithm overview)
- [ ] Create `ModuleNotes.md` (design decisions, constraints)
- [ ] Create `PatchLog.md` (empty, ready for entries)
- [ ] Create `CHANGELOG.md` (initial version entry)

---

## Implementation Phase

### 3. Config ScriptableObject
**Location:** `Modules/N_ModuleName/Config/HB_ModuleNameConfig.cs`

Required elements:
- [ ] Namespace: `MapMaker.Modules.ModuleName.Config`
- [ ] Class name: `HB_<ModuleName>Config : ScriptableObject`
- [ ] `[CreateAssetMenu]` attribute with proper menu path
- [ ] All tunables as public fields (no hardcoded values in code)
- [ ] `[Header]` attributes for organization
- [ ] `[Range]`, `[Tooltip]` attributes for designer guidance
- [ ] `OnValidate()` method for config validation
- [ ] Public helper methods if needed (e.g., `TotalPercentSum()`)

**OnValidate Template:**
```csharp
private void OnValidate()
{
    // Validate ranges
    if (SomeValue < 0f || SomeValue > 1f)
    {
        Debug.LogWarning($"[HB_ModuleConfig] SomeValue should be 0-1, got {SomeValue:F3}");
    }
    
    // Validate sums
    float sum = Field1 + Field2 + Field3;
    if (Mathf.Abs(sum - 1f) > 0.01f)
    {
        Debug.LogWarning($"[HB_ModuleConfig] Fields sum to {sum:F3}, not 1.0");
    }
    
    // Validate dependencies
    if (EnableFeature && RequiredConfig == null)
    {
        Debug.LogWarning("[HB_ModuleConfig] Feature enabled but config not assigned");
    }
}
```

### 4. Validation Class
**Location:** `Modules/N_ModuleName/Scripts/ModuleNameValidate.cs`

Required elements:
- [ ] Namespace: `MapMaker.Modules.ModuleName.Scripts`
- [ ] Static class: `public static class ModuleNameValidate`
- [ ] Main method signature:
```csharp
public static bool Validate(
    HB_ModuleConfig cfg,
    WorldArrays arrays,
    LogEmitter emit)
```
- [ ] Validation logic (structural checks, dependency checks)
- [ ] Warnings for soft constraints (with clamping/normalization)
- [ ] Errors for hard constraints (return false on failure)
- [ ] Clear, actionable log messages

**Validation Pattern:**
```csharp
public static bool Validate(HB_ModuleConfig cfg, WorldArrays arrays, LogEmitter emit)
{
    if (cfg == null)
    {
        emit(LogLevel.ERROR, LogContext.Module, LogPhase.Validation, "CFG_NULL", 
            "Config is null; cannot validate");
        return false;
    }
    
    // Check dependencies on prior modules
    if (arrays.SomeRequiredArray == null)
    {
        emit(LogLevel.ERROR, LogContext.Module, LogPhase.Validation, "DEPENDENCY_MISSING",
            "Module depends on PriorModule but array is null");
        return false;
    }
    
    // Soft constraints - warn and continue
    if (cfg.SomePercent > 0.8f)
    {
        emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "HIGH_VALUE",
            $"SomePercent is very high ({cfg.SomePercent:F2}); may cause unexpected results");
    }
    
    emit(LogLevel.INFO, LogContext.Module, LogPhase.Validation, "VALIDATION_OK",
        "Module validation passed");
    return true;
}
```

### 5. Execution Class(es)
**Location:** `Modules/N_ModuleName/Scripts/ModuleNameGenerator.cs` (or `Pass`, `Processor`, etc.)

Required elements:
- [ ] Namespace: `MapMaker.Modules.ModuleName.Scripts`
- [ ] Class name: descriptive (Generator, Pass, Processor, Assigner)
- [ ] Constructor takes config, SeedContext, LogEmitter
- [ ] Execute method signature:
```csharp
public void Execute(WorldArrays arrays)
```
- [ ] Uses config fields (no hardcoded values)
- [ ] Uses appropriate RNG stream from SeedContext
- [ ] Logs progress milestones
- [ ] No direct file I/O (use WorldExportPass)

**Execution Pattern:**
```csharp
public sealed class ModuleNameGenerator
{
    private readonly HB_ModuleConfig _cfg;
    private readonly Random _rng;
    private readonly LogEmitter _emit;
    
    public ModuleNameGenerator(HB_ModuleConfig cfg, SeedContext seed, LogEmitter emit)
    {
        _cfg = cfg;
        _rng = seed.ModuleRng;  // Use dedicated stream
        _emit = emit;
    }
    
    public void Execute(WorldArrays arrays)
    {
        _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "BEGIN",
            "Module generation starting");
        
        // Use GridHelpers for algorithms
        for (int y = 0; y < arrays.Height; y++)
        {
            for (int x = 0; x < arrays.Width; x++)
            {
                int idx = GridHelpers.ToIndex(x, y, arrays.Width);
                
                // Read from dependencies
                var dependency = arrays.SomePriorArray[idx];
                
                // Generate using config
                float value = ComputeValue(x, y, dependency);
                
                // Write to output
                arrays.SomeNewArray[idx] = value;
            }
        }
        
        _emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "END",
            "Module generation completed");
    }
    
    private float ComputeValue(int x, int y, float dependency)
    {
        // Use _cfg fields, not hardcoded values
        return dependency * _cfg.SomeMultiplier + (float)_rng.NextDouble() * _cfg.NoiseAmount;
    }
}
```

### 6. Export Integration
**Location:** Add to `Shared/Export/WorldExportPass.cs`

Required elements:
- [ ] Static export method for module-specific PNG
- [ ] Color mapping for visualization
- [ ] Update stacked PNG to include new layer (if applicable)
- [ ] Export called from Driver after module execution

**Export Pattern:**
```csharp
public static void ExportModuleNamePng(
    HB_ExportConfig cfg,
    int width,
    int height,
    WorldArrays arrays,
    LogEmitter emit)
{
    emit(LogLevel.INFO, LogContext.Export, LogPhase.Export, "MODULE_EXPORT_BEGIN",
        "Exporting Module preview PNG");
    
    Color32[] pixels = new Color32[width * height];
    
    for (int i = 0; i < pixels.Length; i++)
    {
        // Map data to color
        pixels[i] = MapValueToColor(arrays.SomeNewArray[i]);
    }
    
    SavePng(cfg, width, height, pixels, "WorldPreview_N_ModuleName.png", emit);
}
```

---

## Integration Phase

### 7. Driver Integration
**Location:** `Core/Driver/MapMakerDriver.cs`

Required changes:
- [ ] Add module toggle to HB_PipelineConfig
- [ ] Add module config reference to HB_PipelineConfig
- [ ] Add module execution block in Driver.Run()
- [ ] Follow existing pattern (validate → execute → export)
- [ ] Add performance timing

**Driver Integration Pattern:**
```csharp
// Module N: ModuleName
if (Pipeline.EnableModuleName && Pipeline.ModuleNameConfig != null)
{
    using (emitter.BeginTimer(LogContext.Module, "MODULE_NAME", "Module N (ModuleName)"))
    {
        if (!ModuleNameValidate.Validate(Pipeline.ModuleNameConfig, _world, emitter))
        {
            emitter(LogLevel.ERROR, LogContext.Module, LogPhase.Validation, "VALIDATION_FAILED",
                "Module N validation failed; skipping");
        }
        else
        {
            var gen = new ModuleNameGenerator(Pipeline.ModuleNameConfig, seeds, emitter);
            gen.Execute(_world);
            
            if (Pipeline.Export != null)
            {
                WorldExportPass.ExportModuleNamePng(Pipeline.Export, 
                    DriverConfig.MapWidth, DriverConfig.MapHeight, _world, emitter);
            }
        }
    }
}
else
{
    emitter(LogLevel.INFO, LogContext.Pipeline, LogPhase.Skip, "MODULE_NAME",
        "Module N (ModuleName) skipped (disabled or missing config)");
}
```

### 8. WorldArrays Extension
**Location:** `Shared/Data/WorldArrays.cs`

Required changes:
- [ ] Add new array fields for module outputs
- [ ] Initialize in `Allocate()` method
- [ ] Clear in `Clear()` method (if it exists)

**WorldArrays Pattern:**
```csharp
public sealed class WorldArrays
{
    // Existing arrays...
    
    // Module N outputs
    public float[] ModuleNameData;
    public SomeEnum[] ModuleNameResult;
    
    public void Allocate(int width, int height)
    {
        int count = width * height;
        Width = width;
        Height = height;
        Count = count;
        
        // Existing allocations...
        
        // Module N
        ModuleNameData = new float[count];
        ModuleNameResult = new SomeEnum[count];
    }
}
```

---

## Testing Phase

### 9. Manual Testing
- [ ] Create test config asset in Editor
- [ ] Set reasonable default values
- [ ] Enable module in pipeline
- [ ] Run MapMaker
- [ ] Verify PNG export created
- [ ] Check log file for expected messages
- [ ] Verify no errors/warnings in Console
- [ ] Test with different seeds (verify determinism)
- [ ] Test with extreme config values

### 10. Validation Testing
- [ ] Test with null config (should error gracefully)
- [ ] Test with invalid config values (should warn)
- [ ] Test with missing dependencies (should error)
- [ ] Verify OnValidate warnings appear in Inspector

### 11. Performance Testing
- [ ] Run on small map (50x50)
- [ ] Run on medium map (250x250)
- [ ] Run on large map (1000x1000)
- [ ] Check timing logs
- [ ] Profile if timing is unexpectedly high

---

## Documentation Phase

### 12. Finalize Documentation
- [ ] Update `ModuleSpec.md` with actual implementation details
- [ ] Add usage examples to `ModuleNotes.md`
- [ ] Document any deviations from initial design
- [ ] Add first entry to `PatchLog.md`
- [ ] Update `CHANGELOG.md` with completion date

### 13. Update Project Documentation
- [ ] Mark module as complete in `Docs/DevPlan.md`
- [ ] Update `Docs/FlowDiagram.md` if needed
- [ ] Add module to README (if project has one)

---

## PatchLog Entry Template

Every change should get an entry in `PatchLog.md`:

```markdown
## YYYY-MM-DD HH:mm - Initial Implementation

**Files Changed:**
- `Config/HB_ModuleNameConfig.cs` (created)
- `Scripts/ModuleNameValidate.cs` (created)
- `Scripts/ModuleNameGenerator.cs` (created)
- `Shared/Export/WorldExportPass.cs` (added ExportModuleNamePng)
- `Shared/Data/WorldArrays.cs` (added module arrays)
- `Core/Driver/MapMakerDriver.cs` (added module execution block)

**Reason:**
Implement Module N (ModuleName) per development plan.

**Summary:**
- Generates [what it generates] from [dependencies]
- Uses [algorithm approach]
- Exports preview PNG with [color scheme]
- Validation checks [what constraints]

**Context:**
Following standard module pattern. See ModuleSpec.md for details.
```

---

## Common Pitfalls to Avoid

❌ **Don't:** Hardcode tunable values in execution code
✅ **Do:** Put all tunables in ScriptableObject config

❌ **Don't:** Use `UnityEngine.Random`
✅ **Do:** Use dedicated `System.Random` stream from SeedContext

❌ **Don't:** Write files directly in module code
✅ **Do:** Use centralized WorldExportPass

❌ **Don't:** Guess at enum values for logging
✅ **Do:** Use existing LogLevel, LogContext, LogPhase values

❌ **Don't:** Create new logging wrappers
✅ **Do:** Use provided LogEmitter delegate

❌ **Don't:** Hard-fail on soft constraints
✅ **Do:** Warn and clamp/normalize when possible

❌ **Don't:** Mix generation logic into Driver
✅ **Do:** Keep Driver as thin orchestrator only

---

## Completion Criteria

A module is complete when:
- ✅ Compiles without errors or warnings
- ✅ Validation passes with valid configs
- ✅ Validation fails gracefully with invalid configs
- ✅ Executes and writes to WorldArrays
- ✅ PNG export(s) created successfully
- ✅ Deterministic (same seed = same output)
- ✅ Performance acceptable for target map sizes
- ✅ Documentation complete and accurate
- ✅ PatchLog has initial entry
- ✅ Driver integration complete
- ✅ Follows all directives in `Docs/Directives.md`

---

## Next Steps After Completion

1. Mark module complete in DevPlan.md
2. Create config asset in project for testing
3. Generate test maps with various seeds
4. Move to next module in sequence
5. Do NOT start next module until current is 100% complete

---

## References

- **Design:** `Docs/DesignScope.md`, `Docs/DevPlan.md`
- **Contracts:** `Docs/Directives.md`
- **Flow:** `Docs/FlowDiagram.md`
- **Template:** `Modules/Sample_Module/` (copy structure)
- **Utilities:** `Shared/Docs/GridHelpers_README.md`
