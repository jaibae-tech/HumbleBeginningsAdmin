using UnityEngine;

namespace HumbleBeginnings.Admin.Logging
{
    [CreateAssetMenu(
        fileName = "LogSource",
        menuName = "Humble Beginnings/Admin/Log Source",
        order = 0)]
    public sealed class LogSourceDefinition : ScriptableObject
    {
        [Tooltip("Display name shown in the Log Viewer dropdown")]
        public string DisplayName;

        [Tooltip("Stable identifier used in log entries and filenames")]
        public string SourceId;

        [Tooltip("Absolute or resolved path to the log file")]
        public string LogFilePath;
    }
}
