using MapMaker.Core.Logging;
using MapMaker.Modules.Sample_Module.Config;
using MapMaker.Shared.Data;
using MapMaker.Shared.Utils;

namespace MapMaker.Modules.Sample_Module.Scripts
{
    public static class SampleModulePass
    {
        /// <summary>
        /// Template module execution.
        /// - Do not hardcode tweakable values.
        /// - Do not touch logging internals.
        /// - Do not do exports here; keep exports centralized.
        /// </summary>
        public static void Execute(HB_SampleModuleConfig cfg, WorldArrays arrays, SeedContext seed, LogEmitter emit)
        {
            if (cfg == null)
            {
                emit(LogLevel.ERROR, LogContext.Module, LogPhase.Generation, "SAMPLE_CFG_NULL", "Config is null; cannot run sample module");
                return;
            }

            if (!cfg.Enabled)
            {
                emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "SAMPLE_SKIP", "Sample module is disabled; skipping");
                return;
            }

            // Validate preconditions
            if (!SampleModuleValidate.Validate(cfg, arrays, emit))
            {
                emit(LogLevel.ERROR, LogContext.Module, LogPhase.Generation, "SAMPLE_ABORT", "Validation failed; aborting sample module");
                return;
            }

            emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "SAMPLE_BEGIN", "Sample module begin");

            // Template: implement module logic here.
            // Use seed.ElevationRng (System.Random) for deterministic random.
            // Example usage:
            int exampleRoll = seed != null ? seed.ElevationRng.Next(0, 100) : -1;
            emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "SAMPLE_PROGRESS",
                $"ExampleStrength={cfg.ExampleStrength:0.###}, ExampleRoll={exampleRoll}");

            emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "SAMPLE_END", "Sample module end");
        }
    }
}
