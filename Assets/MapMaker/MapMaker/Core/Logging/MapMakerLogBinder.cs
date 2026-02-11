using System;
using System.IO;
using UnityEngine;
using MapMaker.Core.Pipeline;

namespace MapMaker.Core.Logging
{
    public sealed class MapMakerLogBinder : MonoBehaviour
    {
        public static LogEmitter BindOrCreateEmitter(object logSource, HB_MapConfig driverConfig)
        {
            var pipeline = logSource as HB_PipelineConfig;
            if (pipeline == null)
            {
                return (level, context, phase, key, message) =>
                {
                    Debug.Log($"[{level}] [{context}] [{phase}] {key}: {message}");
                };
            }

            string logPath = null;
            if (pipeline.EnableFileLogging)
            {
                var dir = Path.Combine(Application.dataPath, "..", pipeline.LogDirectory);
                Directory.CreateDirectory(dir);
                logPath = Path.Combine(dir, pipeline.LogFileName);
            }

            return (level, context, phase, key, message) =>
            {
                if (level < pipeline.MinLogLevel) return;

                string formatted = $"[{DateTime.Now:HH:mm:ss}] [{level}] [{context}] [{phase}] {key}: {message}";

                if (pipeline.EnableFileLogging && logPath != null)
                {
                    try
                    {
                        File.AppendAllText(logPath, formatted + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to write log: {ex.Message}");
                    }
                }

                if (pipeline.MirrorToConsole)
                {
                    switch (level)
                    {
                        case LogLevel.ERROR:
                            Debug.LogError(formatted);
                            break;
                        case LogLevel.WARN:
                            Debug.LogWarning(formatted);
                            break;
                        default:
                            Debug.Log(formatted);
                            break;
                    }
                }
            };
        }

        public void Bind(LogEmitter emitter)
        {
            MapMakerLogging.Emitter = emitter;
        }
    }
}
