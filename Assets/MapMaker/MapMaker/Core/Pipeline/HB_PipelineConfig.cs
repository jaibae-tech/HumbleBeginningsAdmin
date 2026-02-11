using UnityEngine;
using MapMaker.Core.Logging;
using MapMaker.Core.Export;
using MapMaker.Modules.Elevation1.Config;
using MapMaker.Modules.Latitude2.Config;
using MapMaker.Modules.Coast3.Config;
using MapMaker.Modules.Hydrology4.Config;  // ADD THIS LINE

namespace MapMaker.Core.Pipeline
{
    [CreateAssetMenu(menuName = "MapMaker/Pipeline Config")] 
    public sealed class HB_PipelineConfig : ScriptableObject
    {
        // =========================================================
        // MAP
        // =========================================================
        [Header("Map")]
        public HB_MapConfig MapConfig;

        // =========================================================
        // EXPORT
        // =========================================================
        [Header("Export")]
        public HB_ExportConfig ExportConfig;

        // =========================================================
        // LOGGING
        // =========================================================
        [Header("Logging")]
        [Tooltip("If enabled, writes MapMaker logs to disk.")]
        public bool EnableFileLogging = true;

        [Tooltip("Directory for log output. If relative, it is resolved relative to the Unity project root.")]
        public string LogDirectory = "Logs";

        [Tooltip("Filename for the run log.")]
        public string LogFileName = "mapmaker.log";

        [Tooltip("Minimum log level written to the file.")]
        public LogLevel MinLogLevel = LogLevel.INFO;

        [Tooltip("Also mirror logs to the Unity Console.")]
        public bool MirrorToConsole = true;

        [Header("Modules")]
        public bool EnableElevation = false;
        public bool EnableLatitude = false;
        public bool EnableCoast = false;
        public bool EnableHydrology = false;  // ADD THIS LINE

        public HB_ElevationConfig ElevationConfig;
        public HB_LatitudeConfig LatitudeConfig;
        public HB_CoastConfig CoastConfig;
        public HB_HydrologyConfig HydrologyConfig;  // ADD THIS LINE

        public HB_ElevationConfig Elevation => ElevationConfig;
        public HB_LatitudeConfig Latitude => LatitudeConfig;
        public HB_CoastConfig Coast => CoastConfig;
        public HB_HydrologyConfig Hydrology => HydrologyConfig;  // ADD THIS LINE
        public HB_ExportConfig Export => ExportConfig;
        public object LogSource => this;

        private void OnValidate()
        {
            if (MapConfig == null)
            {
                Debug.LogWarning("[HB_PipelineConfig] MapConfig is not assigned.");
            }

            if (ExportConfig == null)
            {
                Debug.LogWarning("[HB_PipelineConfig] ExportConfig is not assigned.");
            }

            if (EnableElevation && ElevationConfig == null)
            {
                Debug.LogWarning("[HB_PipelineConfig] Elevation is enabled but ElevationConfig is not assigned.");
            }

            if (EnableLatitude && LatitudeConfig == null)
            {
                Debug.LogWarning("[HB_PipelineConfig] Latitude is enabled but LatitudeConfig is not assigned.");
            }

            if (EnableCoast && CoastConfig == null)
            {
                Debug.LogWarning("[HB_PipelineConfig] Coast is enabled but CoastConfig is not assigned.");
            }

            // ADD THIS BLOCK
            if (EnableHydrology && HydrologyConfig == null)
            {
                Debug.LogWarning("[HB_PipelineConfig] Hydrology is enabled but HydrologyConfig is not assigned.");
            }

            if (EnableFileLogging && string.IsNullOrWhiteSpace(LogDirectory))
            {
                Debug.LogWarning("[HB_PipelineConfig] File logging is enabled but LogDirectory is empty.");
            }

            if (EnableFileLogging && string.IsNullOrWhiteSpace(LogFileName))
            {
                Debug.LogWarning("[HB_PipelineConfig] File logging is enabled but LogFileName is empty.");
            }
        }
    }
}
