using System;
using System.IO;
using UnityEngine;

namespace HumbleBeginnings.WorldViewer
{
    public sealed class WorldDataLoader
    {
        public WorldMeta Meta { get; private set; }
        public float[] Elevation01 { get; private set; }

        public int Width => Meta?.width ?? 0;
        public int Height => Meta?.height ?? 0;

        /// <summary>Enable to log min/max/mean + corner samples when loading.</summary>
        public bool DebugLogs { get; set; }

        public void LoadFromWorldRoot(string worldRoot)
        {
            if (string.IsNullOrWhiteSpace(worldRoot))
                throw new ArgumentException("worldRoot is null/empty.");

            string metaPath = WorldPaths.MetaJsonPath(worldRoot);
            if (!File.Exists(metaPath))
                throw new FileNotFoundException($"Meta.json not found: {metaPath}");

            var json = File.ReadAllText(metaPath);
            Meta = JsonUtility.FromJson<WorldMeta>(json);
            if (Meta == null)
                throw new Exception($"Failed to parse Meta.json: {metaPath}");

            if (Meta.width <= 0 || Meta.height <= 0)
                throw new Exception($"Invalid Meta.json dimensions: width={Meta.width}, height={Meta.height}");

            string elevPath = WorldPaths.ElevationF32Path(worldRoot);
            Elevation01 = ReadF32Array(elevPath, Meta.width * Meta.height);

            if (DebugLogs)
            {
                var s = ComputeStats(Elevation01);
                Debug.Log($"[WorldViewer][WorldDataLoader] ElevationRaw.f32 loaded. Dims={Meta.width}x{Meta.height} floats={Elevation01.Length} " +
                          $"min={s.min:0.###} max={s.max:0.###} mean={s.mean:0.###} nan={s.nanCount} inf={s.infCount} zero={s.zeroCount} one={s.oneCount}");

                Debug.Log($"[WorldViewer][WorldDataLoader] Corner samples: " +
                          $"(0,0)={GetElevation01Clamped(0,0):0.###} (W-1,0)={GetElevation01Clamped(Meta.width-1,0):0.###} " +
                          $"(0,H-1)={GetElevation01Clamped(0,Meta.height-1):0.###} (W-1,H-1)={GetElevation01Clamped(Meta.width-1,Meta.height-1):0.###}");
            }
        }

        public float GetElevation01Clamped(int x, int y)
        {
            if (Elevation01 == null || Meta == null) return 0f;

            x = Mathf.Clamp(x, 0, Meta.width - 1);
            y = Mathf.Clamp(y, 0, Meta.height - 1);
            return Elevation01[y * Meta.width + x];
        }

        static float[] ReadF32Array(string path, int expectedFloats)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"ElevationRaw.f32 not found: {path}");

            int expectedBytes = expectedFloats * sizeof(float);
            byte[] bytes = File.ReadAllBytes(path);

            if (bytes.Length != expectedBytes)
                throw new Exception($"ElevationRaw.f32 size mismatch. Expected {expectedBytes} bytes but got {bytes.Length} bytes.");

            var arr = new float[expectedFloats];
            Buffer.BlockCopy(bytes, 0, arr, 0, bytes.Length);
            return arr;
        }

        struct Stats
        {
            public float min, max, mean;
            public int nanCount, infCount, zeroCount, oneCount;
        }

        static Stats ComputeStats(float[] arr)
        {
            var s = new Stats
            {
                min = float.PositiveInfinity,
                max = float.NegativeInfinity,
                mean = 0f
            };

            if (arr == null || arr.Length == 0)
            {
                s.min = s.max = s.mean = 0f;
                return s;
            }

            double sum = 0.0;
            int finite = 0;

            for (int i = 0; i < arr.Length; i++)
            {
                float v = arr[i];
                if (float.IsNaN(v)) { s.nanCount++; continue; }
                if (float.IsInfinity(v)) { s.infCount++; continue; }

                finite++;
                sum += v;

                if (v < s.min) s.min = v;
                if (v > s.max) s.max = v;

                if (Mathf.Approximately(v, 0f)) s.zeroCount++;
                if (Mathf.Approximately(v, 1f)) s.oneCount++;
            }

            s.mean = finite > 0 ? (float)(sum / finite) : 0f;
            if (float.IsPositiveInfinity(s.min)) s.min = 0f;
            if (float.IsNegativeInfinity(s.max)) s.max = 0f;

            return s;
        }
    }
}
