using System;
using System.IO;
using UnityEngine;

namespace MapMaker.Core.Driver
{
    [Serializable]
    public sealed class PipelineRunContext
    {
        public int RootSeed;
        public string TimestampUtc;   // yyyyMMdd_HHmmss
        public string RunId;          // <seed>_<timestamp>
        public string ExportRoot;     // HB_ExportConfig.ExportFolderName
        public string RunExportRoot;  // ExportRoot/RunId

        public static PipelineRunContext Create(int rootSeed, string exportRoot)
        {
            var ts = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var runId = $"{rootSeed}_{ts}";
            var runExportRoot = Path.Combine(exportRoot, runId);

            return new PipelineRunContext
            {
                RootSeed = rootSeed,
                TimestampUtc = ts,
                RunId = runId,
                ExportRoot = exportRoot,
                RunExportRoot = runExportRoot
            };
        }

        public void EnsureDirectories()
        {
            Directory.CreateDirectory(RunExportRoot);
        }

        public string Combine(params string[] parts)
        {
            var p = RunExportRoot;
            for (int i = 0; i < parts.Length; i++)
                p = Path.Combine(p, parts[i]);
            return p;
        }
    }
}
