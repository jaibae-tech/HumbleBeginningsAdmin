using System;
using System.Collections.Generic;

namespace HumbleBeginnings.Admin.Logging
{
    public sealed class StubLogProvider : ILogProvider
    {
        private readonly List<LogEntry> _entries;

        public IReadOnlyList<LogEntry> Entries => _entries;

        public StubLogProvider()
        {
            _entries = new List<LogEntry>
            {
                new LogEntry
                {
                    Timestamp = new DateTime(1000, 1, 1, 0, 0, 0),
                    Source = "AdminTool",
                    Message = "Stub log initialized",
                    Severity = LogSeverity.Info
                },
                new LogEntry
                {
                    Timestamp = new DateTime(1000, 1, 1, 0, 1, 0),
                    Source = "WorldGen",
                    Message = "Deterministic seed applied",
                    Severity = LogSeverity.Info
                },
                new LogEntry
                {
                    Timestamp = new DateTime(1000, 1, 1, 0, 2, 0),
                    Source = "WorldUpdate",
                    Message = "Anchor registration failed",
                    Severity = LogSeverity.Error
                }
            };
        }
    }
}
