using UnityEngine;

namespace HumbleBeginnings.WorldViewer
{
    public static class WVDebug
    {
        public static bool Enabled = true;

        public static void Log(string msg)
        {
            if (!Enabled) return;
            Debug.Log($"[WV] {msg}");
        }

        public static void Warn(string msg)
        {
            if (!Enabled) return;
            Debug.LogWarning($"[WV] {msg}");
        }

        public static void Error(string msg)
        {
            if (!Enabled) return;
            Debug.LogError($"[WV] {msg}");
        }
    }
}
