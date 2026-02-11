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
    }
}
