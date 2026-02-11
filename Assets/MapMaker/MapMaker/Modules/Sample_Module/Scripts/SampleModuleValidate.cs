using MapMaker.Core.Logging;
using MapMaker.Modules.Sample_Module.Config;
using MapMaker.Shared.Data;

namespace MapMaker.Modules.Sample_Module.Scripts
{
    public static class SampleModuleValidate
    {
        /// <summary>
        /// Module-level validation. Keep it self-contained: validate only what this module needs.
        /// </summary>
        public static bool Validate(HB_SampleModuleConfig cfg, WorldArrays arrays, LogEmitter emit)
        {
            emit(LogLevel.INFO, LogContext.Module, LogPhase.Validation, "SAMPLE_VALIDATE_BEGIN", "Sample module validation begin");

            bool ok = true;

            if (cfg == null)
            {
                emit(LogLevel.ERROR, LogContext.Module, LogPhase.Validation, "SAMPLE_CFG_NULL", "Config is null");
                return false;
            }

            ok &= cfg.Validate(emit);

            if (arrays == null)
            {
                emit(LogLevel.ERROR, LogContext.Module, LogPhase.Validation, "SAMPLE_ARRAYS_NULL", "WorldArrays is null");
                ok = false;
            }

            // Template: add module-specific array checks here (lengths, required buffers, etc.)

            emit(LogLevel.INFO, LogContext.Module, LogPhase.Validation, "SAMPLE_VALIDATE_END",
                ok ? "Sample module validation OK" : "Sample module validation FAILED");

            return ok;
        }
    }
}
