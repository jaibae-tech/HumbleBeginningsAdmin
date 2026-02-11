# MapMaker Utilities - Quick Reference

Quick lookup for shared utilities available to all modules.

---

## GridHelpers (`Shared/Utils/GridHelpers.cs`)

### Coordinates
```csharp
int idx = GridHelpers.ToIndex(x, y, width);
(int x, int y) = GridHelpers.ToCoords(idx, width);
bool valid = GridHelpers.IsInBounds(x, y, width, height);
```

### Neighbors
```csharp
foreach (var (nx, ny) in GridHelpers.GetNeighbors4(x, y, width, height))
    { /* 4-connected */ }

foreach (var (nx, ny) in GridHelpers.GetNeighbors8(x, y, width, height))
    { /* 8-connected */ }
```

### Flood Fill
```csharp
GridHelpers.FloodFill(mask, width, height, startX, startY, fillValue: true);
GridHelpers.FloodFillEdges(mask, width, height, edgeValue: false, fillValue: true);
```

### Distance Fields
```csharp
float[] dist = GridHelpers.ComputeDistanceField(sources, width, height, maxDistance: 100f);
float[] eucDist = GridHelpers.ComputeEuclideanDistanceField(sources, width, height);
```

### Counting
```csharp
int count4 = GridHelpers.CountNeighbors4Matching(array, x, y, width, height, value);
int count8 = GridHelpers.CountNeighbors8Matching(array, x, y, width, height, value);
```

---

## StatHelpers (`Shared/Utils/StatHelpers.cs`)

### Quantiles
```csharp
float[] thresholds = StatHelpers.ComputeQuantiles(values, new[] { 0.25f, 0.5f, 0.75f });
```

### Statistics
```csharp
var (min, max, mean, stdDev) = StatHelpers.ComputeStats(values);
```

### Histogram
```csharp
int[] hist = StatHelpers.ComputeHistogram(values, binCount: 10, minValue: 0f, maxValue: 1f);
```

### Normalization
```csharp
float originalSum = StatHelpers.NormalizeSum(values); // modifies in-place
```

### Clamping
```csharp
float clamped = StatHelpers.Clamp01(value);
int clampedInt = StatHelpers.Clamp(value, min: 0, max: 100);
```

---

## SeedContext (`Shared/Utils/SeedContext.cs`)

### RNG Streams
```csharp
var seeds = new SeedContext(rootSeed);

// Use dedicated streams (prevents cross-module interference)
Random elevationRng = seeds.ElevationRng;
Random latitudeRng = seeds.LatitudeRng;
Random coastRng = seeds.CoastRng;
Random mountainsRng = seeds.MountainsRng;
Random hydrologyRng = seeds.HydrologyRng;
Random moistureRng = seeds.MoistureRng;
Random biomesRng = seeds.BiomesRng;

// Get root seed
int root = seeds.RootSeed;
```

---

## PerformanceTimer (`Shared/Utils/PerformanceTimer.cs`)

### Using Pattern
```csharp
using (emitter.BeginTimer(LogContext.Module, "KEY", "Description"))
{
    // Timed code
    // Automatically logs elapsed time on scope exit
}
```

### Manual Usage
```csharp
var timer = new PerformanceTimer(emitter, LogLevel.INFO, 
    LogContext.Module, LogPhase.Progress, "KEY", "Description");
    
// Do work
long elapsed = timer.ElapsedMilliseconds;

timer.Dispose(); // Logs automatically
```

---

## WorldArrays (`Shared/Data/WorldArrays.cs`)

### Properties
```csharp
int width = arrays.Width;
int height = arrays.Height;
int count = arrays.Count;  // width × height
int length = arrays.Length; // alias for Count
```

### Elevation Arrays
```csharp
float[] raw = arrays.ElevationRaw;
ElevationBandFinal[] bands = arrays.ElevationBands;
```

### Future Arrays
```csharp
// Add as modules are implemented:
// arrays.LatitudeBands
// arrays.CoastMask
// arrays.MountainMask
// arrays.RiverMask
// arrays.Moisture
// arrays.Biomes
```

---

## Logging

### Emitter Signature
```csharp
LogEmitter emit = (LogLevel level, LogContext context, LogPhase phase, string key, string message) => { };
```

### Usage
```csharp
emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "KEY", "Message");
emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "WARNING_KEY", "Warning message");
emit(LogLevel.ERROR, LogContext.Module, LogPhase.Validation, "ERROR_KEY", "Error message");
```

### Available Enums

**LogLevel:**
- `INFO` - Informational messages
- `WARN` - Warnings (non-fatal)
- `ERROR` - Errors (may be fatal)

**LogContext:**
- `Driver` - Driver orchestration
- `Module` - Module execution
- `Export` - Export operations
- `Pipeline` - Pipeline-level
- `Logging` - Logging system

**LogPhase:**
- `Init` - Initialization
- `Validation` - Validation phase
- `Generation` - Generation phase
- `Export` - Export phase
- `Shutdown` - Cleanup/shutdown
- `Begin` - Start of operation
- `End` - End of operation
- `Skip` - Skipped operation
- `Progress` - Progress update
- `Evaluated` - Evaluation complete
- `Assigned` - Assignment complete

---

## Config Best Practices

### ScriptableObject Attributes
```csharp
[CreateAssetMenu(
    fileName = "HB_ModuleName_Default",
    menuName = "Humble Beginnings / MapMaker / Module N - Name / Config")]
public sealed class HB_ModuleConfig : ScriptableObject
{
    [Header("Section Name")]
    [Tooltip("Helpful description")]
    [Range(0f, 1f)]
    public float SomeValue = 0.5f;
    
    [Min(1)]
    public int PositiveInt = 10;
}
```

### OnValidate Pattern
```csharp
private void OnValidate()
{
    // Validate ranges
    if (value < min || value > max)
        Debug.LogWarning($"[ConfigName] value out of range: {value}");
    
    // Validate sums
    float sum = a + b + c;
    if (Mathf.Abs(sum - 1f) > 0.01f)
        Debug.LogWarning($"[ConfigName] values sum to {sum:F3}, not 1.0");
    
    // Validate dependencies
    if (enabled && dependency == null)
        Debug.LogWarning("[ConfigName] enabled but dependency not assigned");
}
```

---

## Export Utilities

### WorldExportPass Static Methods
```csharp
WorldExportPass.ExportElevationBandsPng(exportCfg, width, height, arrays, emitter);
WorldExportPass.ExportStackedPng_ExcludeLatitude(exportCfg, width, height, arrays, emitter);

// Add module exports as needed
```

### Color Mapping Helpers
```csharp
// Common pattern for enum-based visualization
Color32 GetColorForBand(ElevationBandFinal band)
{
    return band switch
    {
        ElevationBandFinal.DeepOcean => new Color32(10, 20, 80, 255),
        ElevationBandFinal.Ocean => new Color32(20, 60, 160, 255),
        ElevationBandFinal.Lowland => new Color32(80, 160, 80, 255),
        // ... etc
    };
}
```

---

## Common Patterns

### Module Execution Template
```csharp
public sealed class ModuleNameGenerator
{
    private readonly HB_ModuleConfig _cfg;
    private readonly Random _rng;
    private readonly LogEmitter _emit;
    
    public ModuleNameGenerator(HB_ModuleConfig cfg, SeedContext seed, LogEmitter emit)
    {
        _cfg = cfg;
        _rng = seed.ModuleRng;
        _emit = emit;
    }
    
    public void Execute(WorldArrays arrays)
    {
        using (_emit.BeginTimer(LogContext.Module, "MODULE", "Module execution"))
        {
            for (int y = 0; y < arrays.Height; y++)
            {
                for (int x = 0; x < arrays.Width; x++)
                {
                    int idx = GridHelpers.ToIndex(x, y, arrays.Width);
                    // Process cell
                }
            }
        }
    }
}
```

### Validation Template
```csharp
public static bool Validate(HB_ModuleConfig cfg, WorldArrays arrays, LogEmitter emit)
{
    if (cfg == null)
    {
        emit(LogLevel.ERROR, LogContext.Module, LogPhase.Validation, "CFG_NULL", "Config null");
        return false;
    }
    
    // Check dependencies
    if (arrays.RequiredArray == null)
    {
        emit(LogLevel.ERROR, LogContext.Module, LogPhase.Validation, "DEP_MISSING", "Dependency missing");
        return false;
    }
    
    // Soft constraints
    if (cfg.Value > threshold)
    {
        emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "HIGH_VALUE",
            $"Value is high: {cfg.Value:F2}");
    }
    
    return true;
}
```

### Distance-Based Effects
```csharp
// Moisture from water sources
bool[] waterSources = new bool[arrays.Count];
for (int i = 0; i < arrays.Count; i++)
    waterSources[i] = arrays.IsOcean[i] || arrays.IsRiver[i];

float[] distToWater = GridHelpers.ComputeDistanceField(waterSources, 
    arrays.Width, arrays.Height, maxDistance: 200f);

for (int i = 0; i < arrays.Count; i++)
{
    float normalizedDist = distToWater[i] / 200f;
    arrays.Moisture[i] = 1f - StatHelpers.Clamp01(normalizedDist);
}
```

### Region Marking with Flood Fill
```csharp
// Separate true ocean from inland lakes
bool[] waterMask = new bool[arrays.Count];
for (int i = 0; i < arrays.Count; i++)
    waterMask[i] = arrays.ElevationBands[i] == ElevationBandFinal.Ocean;

// Flood from edges - marks edge-connected ocean
GridHelpers.FloodFillEdges(waterMask, arrays.Width, arrays.Height, 
    edgeValue: true, fillValue: true);

// Now waterMask has true ocean; invert to get lakes
for (int i = 0; i < arrays.Count; i++)
{
    if (arrays.ElevationBands[i] == ElevationBandFinal.Ocean)
        arrays.IsInlandLake[i] = !waterMask[i];
}
```

---

## Performance Tips

### Memory
- Reuse arrays when possible (don't allocate in loops)
- Use `arrays.Count` for array sizes (width × height)
- Consider local variables for frequently-accessed properties

### CPU
- Prefer `GetNeighbors4` over `GetNeighbors8` when possible
- Use Manhattan distance over Euclidean for large grids
- Cache `ToIndex(x, y, width)` if used multiple times
- Profile with PerformanceTimer to find bottlenecks

### Determinism
- Always use `System.Random` from SeedContext
- Never use `UnityEngine.Random`
- Process cells in consistent order (y-loop outer, x-loop inner)
- Don't use parallel processing (breaks determinism)

---

## File Locations Reference

```
/Assets/MapMaker/MapMaker/
├── Core/
│   ├── Driver/MapMakerDriver.cs
│   ├── Logging/
│   ├── Pipeline/HB_PipelineConfig.cs
│   └── Export/HB_ExportConfig.cs
├── Shared/
│   ├── Data/WorldArrays.cs
│   ├── Export/WorldExportPass.cs
│   └── Utils/
│       ├── GridHelpers.cs          ← Grid algorithms
│       ├── StatHelpers.cs          ← Statistics
│       ├── SeedContext.cs          ← RNG streams
│       └── PerformanceTimer.cs     ← Timing utility
├── Modules/
│   ├── 1_Elevation/
│   ├── 2_Latitude/
│   └── Sample_Module/              ← Template to copy
└── Docs/
    ├── DesignScope.md
    ├── DevPlan.md
    ├── Directives.md
    ├── Module_Creation_Checklist.md
    └── Enhancements_Summary.md
```

---

## Quick Troubleshooting

**"Type not found"** → Missing `using` statement
**"Array index out of range"** → Check `IsInBounds` before access
**"Non-deterministic output"** → Check for `UnityEngine.Random` usage
**"Config changes don't apply"** → Check config asset is assigned in Pipeline
**"OnValidate not running"** → Only runs in Editor, not at runtime
**"PNG not exported"** → Check Export config assigned and folder permissions

---

## See Also

- Full API: `Shared/Docs/GridHelpers_README.md`
- Module Template: `Modules/Sample_Module/`
- Creation Guide: `Docs/Module_Creation_Checklist.md`
- Enhancements: `Docs/Enhancements_Summary.md`
