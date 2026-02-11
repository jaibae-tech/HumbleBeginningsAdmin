using System;
using System.Collections.Generic;
using UnityEngine;

namespace HumbleBeginnings.Debugging
{
    public static class DebugLog
    {
        private const int DEFAULT_CAPACITY = 1000;

        private static readonly Dictionary<DebugLogRealm, RingBuffer<string>> _buffers
            = new();

        static DebugLog()
        {
            foreach (DebugLogRealm realm in Enum.GetValues(typeof(DebugLogRealm)))
            {
                _buffers[realm] = new RingBuffer<string>(DEFAULT_CAPACITY);
            }
        }

        public static void Emit(DebugLogRealm realm, string message)
        {
            string entry = $"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}";
            _buffers[realm].Add(entry);

            // Secondary sink ONLY — not relied upon
            Debug.Log($"[{realm}] {message}");
        }

        public static IReadOnlyList<string> GetEntries(DebugLogRealm realm)
        {
            return _buffers[realm].Snapshot();
        }
    }
}
