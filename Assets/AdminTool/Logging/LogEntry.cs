using System;

namespace HumbleBeginnings.Admin.Logging
{
    public sealed class LogEntry
    {
        public DateTime Timestamp;
        public string Source;
        public string Message;
        public LogSeverity Severity;
    }
}
