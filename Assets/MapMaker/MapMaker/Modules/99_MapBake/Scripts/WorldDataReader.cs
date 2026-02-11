using System;
using System.IO;
using UnityEngine;

namespace MapMaker.Modules.MapBake5.Scripts
{
    public static class WorldDataReader
    {
        public static WorldMeta LoadMeta(string metaJsonPath)
        {
            if (!File.Exists(metaJsonPath))
                throw new FileNotFoundException($"Meta.json not found: {metaJsonPath}");

            var json = File.ReadAllText(metaJsonPath);
            var meta = JsonUtility.FromJson<WorldMeta>(json);
            if (meta == null)
                throw new Exception($"Failed to parse Meta.json: {metaJsonPath}");

            if (meta.width <= 0 || meta.height <= 0)
                throw new Exception($"Invalid Meta.json dimensions: width={meta.width}, height={meta.height}");

            return meta;
        }

        /// <summary>
        /// Reads ElevationRaw.f32 as little-endian float32 array of length width*height.
        /// Values should be normalized 0..1.
        /// </summary>
        public static float[] LoadElevationF32(string elevationF32Path, int width, int height)
        {
            if (!File.Exists(elevationF32Path))
                throw new FileNotFoundException($"ElevationRaw.f32 not found: {elevationF32Path}");

            var expected = width * height;
            var byteLenExpected = expected * 4;

            var bytes = File.ReadAllBytes(elevationF32Path);
            if (bytes.Length != byteLenExpected)
                throw new Exception($"ElevationRaw.f32 length mismatch. Expected {byteLenExpected} bytes but got {bytes.Length} bytes.");

            var floats = new float[expected];
            Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);

            return floats;
        }
    }
}
