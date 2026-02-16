using System;
using System.IO;
using UnityEngine;
using MapMaker.Core.Pipeline;

namespace MapMaker.Core.Logging
{
    // NOTE:
    // - Do not create new logging systems in modules.
    // - The driver sets the run-scoped log folder during Stage 0 via SetRuntimeLogFolder(...).
    public sealed class MapMakerLogBinder : MonoBehaviour
    {
        // Absolute folder path for this run's logs (set by driver Stage 0).   
        private static string _runtimeLogFolder;

        /// <summary>
        /// Stage 0 hook: driver must call once per run, before BindOrCreateEmitter.
        /// </summary>
        public static void SetRuntimeLogFolder(string absoluteFolderPath)
        {
            if (string.IsNullOrWhiteSpace(absoluteFolderPath))
                throw new ArgumentException("absoluteFolderPath is null/empty.");

            _runtimeLogFolder = Path.GetFullPath(absoluteFolderPath);
            Directory.CreateDirectory(_runtimeLogFolder);
        }

        /// <summary>
        /// Binds an emitter using the pipeline settings. File logging is written under the Stage 0 run folder.
        /// logSource is expected to be HB_PipelineConfig (passed from driver), but kept as object to match existing API.
        /// </summary>
        public static LogEmitter BindOrCreateEmitter(object logSource, HB_MapConfig driverConfig)
        {
            var pipeline = logSource as HB_PipelineConfig;
            if (pipeline == null)
            {
                // Fallback: console-only if a pipeline config wasn't provided.
                return (level, context, phase, key, message) =>
                {
                    Debug.Log($"[{level}] [{context}] [{phase}] {key}: {message}");
                };
            }

            string logPath = null;

            if (pipeline.EnableFileLogging)
            {
                // One-way rule: file logs must go to the run-scoped folder.
                if (string.IsNullOrWhiteSpace(_runtimeLogFolder))
                    throw new Exception("Runtime log folder not set. Driver must call MapMakerLogBinder.SetRuntimeLogFolder() during Stage 0.");

                logPath = Path.Combine(_runtimeLogFolder, pipeline.LogFileName);
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
