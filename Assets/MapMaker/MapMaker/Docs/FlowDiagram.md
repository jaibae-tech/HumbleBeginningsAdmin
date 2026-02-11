# Execution Flow Diagram (High Level)

```
MapMakerTrigger.Run()
  -> MapMakerDriver.Run()

MapMakerDriver.Run()
  1) Bind logging via MapMakerLogBinder (frozen)
  2) Read HB_PipelineConfig
     - MapConfig (width/height/root seed)
     - ExportConfig (export folder, pixel size, flip)
     - Module toggles + module configs
  3) Allocate WorldArrays (once per run)
     - elevationRaw (float[])
     - elevationBands (ElevationBandFinal[])
     - (future arrays reserved)
  4) Build SeedContext(rootSeed)
     - ElevationRng, LatitudeRng, ...
  5) For each enabled module in order:
     a) moduleConfig.Validate(emit)  [WARN + continue]
     b) module.Execute(arrays, ...)  [writes arrays]
     c) module.Validate(arrays, ...) [WARN + continue]
     d) Export PNG(s) via WorldExportPass (centralized)
  6) Export Stacked PNG (excluding latitude)
  7) Log shutdown + exit
```
