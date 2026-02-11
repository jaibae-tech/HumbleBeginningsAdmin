using System;

namespace MapMaker.Core.Logging
{
    public enum LogLevel { DEBUG, INFO, WARN, ERROR }
    public enum LogContext { Driver, Pipeline, Module, Export }
    public enum LogPhase { Init, Validation, Generation, Export, Shutdown, Begin, End, Skip, Progress, Evaluated, Assigned }

    public delegate void LogEmitter(LogLevel level, LogContext context, LogPhase phase, string key, string message);

    public static class MapMakerLogging
    {
        public static LogEmitter Emitter;

        public static void Emit(LogLevel level, LogContext context, LogPhase phase, string key, string message)
        {
            Emitter?.Invoke(level, context, phase, key, message);
        }
    }
}
