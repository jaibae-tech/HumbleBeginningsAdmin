using System.Collections.Generic;
using System.Text;

namespace HumbleBeginnings.Admin.Debug
{
    /// <summary>
    /// Central in-memory log store, separated by realm.
    /// Intended for debug tooling only.
    /// </summary>
    public static class LogStore
    {
        private static readonly Dictionary<LogRealm, List<string>> logs =
            new Dictionary<LogRealm, List<string>>();

        static LogStore()
        {
            foreach (LogRealm realm in System.Enum.GetValues(typeof(LogRealm)))
            {
                logs[realm] = new List<string>();
            }
        }

        public static void Add(LogRealm realm, string message)
        {
            logs[realm].Add(message);
        }

        public static string GetLogs(LogRealm realm)
        {
            var sb = new StringBuilder();
            foreach (var line in logs[realm])
            {
                sb.AppendLine(line);
            }
            return sb.ToString();
        }

        public static void Clear(LogRealm realm)
        {
            logs[realm].Clear();
        }
    }
}

