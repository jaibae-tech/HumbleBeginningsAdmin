using System;
using System.Diagnostics;
using MapMaker.Core.Logging;

namespace MapMaker.Shared.Utils
{
    public sealed class PerformanceTimer : IDisposable
    {
        private readonly Stopwatch _stopwatch;
        private readonly LogEmitter _emitter;
        private readonly LogLevel _logLevel;
        private readonly LogContext _logContext;
        private readonly LogPhase _logPhase;
        private readonly string _key;
        private readonly string _description;
        private bool _disposed;

        public PerformanceTimer(
            LogEmitter emitter,
            LogLevel logLevel,
            LogContext logContext,
            LogPhase logPhase,
            string key,
            string description)
        {
            _emitter = emitter;
            _logLevel = logLevel;
            _logContext = logContext;
            _logPhase = logPhase;
            _key = key;
            _description = description;
            _stopwatch = Stopwatch.StartNew();
        }

        public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

        public void Dispose()
        {
            if (_disposed) return;
            
            _stopwatch.Stop();
            _emitter?.Invoke(_logLevel, _logContext, _logPhase, _key,
                $"{_description} completed in {_stopwatch.ElapsedMilliseconds}ms");
            
            _disposed = true;
        }
    }

    public static class PerformanceTimerExtensions
    {
        public static PerformanceTimer BeginTimer(
            this LogEmitter emitter,
            LogContext context,
            string key,
            string description)
        {
            return new PerformanceTimer(emitter, LogLevel.INFO, context, LogPhase.Progress, key, description);
        }
    }
}
