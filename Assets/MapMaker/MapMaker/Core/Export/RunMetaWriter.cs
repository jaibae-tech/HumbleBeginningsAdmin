using System.IO;
using UnityEngine;
using MapMaker.Core.Driver;

namespace MapMaker.Core.Export
{
    public static class RunMetaWriter
    {
        [System.Serializable]
        private class RunMeta
        {
            public string runId;
            public int rootSeed;
            public string timestampUtc;
            public int width;
            public int height;
            public string pipelineConfigName;
        }

        public static void Write(PipelineRunContext run, int width, int height, string pipelineConfigName)
        {
            var meta = new RunMeta
            {
                runId = run.RunId,
                rootSeed = run.RootSeed,
                timestampUtc = run.TimestampUtc,
                width = width,
                height = height,
                pipelineConfigName = pipelineConfigName
            };

            var json = JsonUtility.ToJson(meta, true);
            File.WriteAllText(Path.Combine(run.RunExportRoot, "RunMeta.json"), json);
        }
    }
}
