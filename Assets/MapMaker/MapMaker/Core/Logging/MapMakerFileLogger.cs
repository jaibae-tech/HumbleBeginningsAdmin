using System;
using System.IO;
using UnityEngine;

namespace MapMaker.Core.Logging
{
    /// <summary>
    /// Simple file-backed log emitter. Intended to be bound by MapMakerDriver.
    /// </summary>
    public sealed class MapMakerFileLogger : IDisposable
    {
        private readonly string _filePath;
        private readonly LogLevel _minLevel;
        private readonly bool _alsoLogToConsole;
        private StreamWriter _writer;

        public MapMakerFileLogger(string filePath, LogLevel minLevel, bool alsoLogToConsole)
        {
            _filePath = filePath;
            _minLevel = minLevel;
            _alsoLogToConsole = alsoLogToConsole;

            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Append so repeated runs accumulate a single log.
            _writer = new StreamWriter(_filePath, append: true)
            {
                AutoFlush = true
            };

            _writer.WriteLine("============================================================");
            _writer.WriteLine($"MapMaker log start: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _writer.WriteLine($"Path: {_filePath}");
        }

        public void Emit(LogLevel level, LogContext context, LogPhase phase, string key, string message)
        {
            if (level < _minLevel) return;

            var line = $"{level} | {context} | {phase} | {key} | {message}";

            try
            {
                _writer?.WriteLine(line);
            }
            catch (Exception ex)
            {
                // Fallback to console if file write fails.
                Debug.LogError($"[MapMakerFileLogger] Failed to write log line. Exception: {ex.Message}\n{line}");
            }

            if (_alsoLogToConsole)
            {
                if (level >= LogLevel.ERROR) Debug.LogError(line);
                else Debug.Log(line);
            }
        }

        public void Dispose()
        {
            try
            {
                _writer?.WriteLine($"MapMaker log end: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _writer?.Dispose();
            }
            catch
            {
                // ignore
            }

            _writer = null;
        }
    }
}
