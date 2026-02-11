using UnityEngine;
using MapMaker.Core.Logging;

namespace MapMaker.Modules.Sample_Module.Config
{
    /// <summary>
    /// Template config for a module. Copy this folder to create a new module.
    /// Keep ALL tweakable values here (ScriptableObject), not hardcoded in execution code.
    /// </summary>
    [CreateAssetMenu(fileName = "HB_SampleModuleConfig", menuName = "MapMaker/Modules/Sample Module Config")]
    public sealed class HB_SampleModuleConfig : ScriptableObject
    {
        [Header("Enable")]
        public bool Enabled = true;

        [Header("Template Fields")]
        [Tooltip("Example tweakable value. Replace with real module settings.")]
        [Range(0f, 1f)]
        public float ExampleStrength = 0.5f;

        [Tooltip("Freeform notes for this config asset.")]
        [TextArea(2, 6)]
        public string Notes;

        /// <summary>
        /// Self-contained config validation. Returns true if config is usable.
        /// Do not introduce new logging APIs; use the provided emitter.
        /// </summary>
        public bool Validate(LogEmitter emit)
        {
            bool ok = true;

            if (ExampleStrength < 0f || ExampleStrength > 1f)
            {
                emit(LogLevel.WARN, LogContext.Module, LogPhase.Validation, "SAMPLE_CFG_RANGE",
                    $"ExampleStrength out of range: {ExampleStrength} (expected 0..1)");
                ok = false;
            }

            return ok;
        }
    }
}
