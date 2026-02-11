using System;
using UnityEngine;

namespace MapMaker.Modules.MapBake5.Scripts
{
    public static class HillshadeBaker
    {
        // Four diagonal lights + normalized up; keeps relief readable from many angles.
        static readonly Vector3[] DefaultLights = new[]
        {
            new Vector3( 1f, 1f,  1f).normalized,
            new Vector3(-1f, 1f,  1f).normalized,
            new Vector3( 1f, 1f, -1f).normalized,
            new Vector3(-1f, 1f, -1f).normalized,
        };

        public static byte[] BakeHillshadeChunk(
            float[] elevation01,
            int worldWidth,
            int worldHeight,
            int chunkX,
            int chunkY,
            int chunkSize,
            float heightScale)
        {
            var outW = chunkSize;
            var outH = chunkSize;
            var outBytes = new byte[outW * outH];

            int baseX = chunkX * chunkSize;
            int baseY = chunkY * chunkSize;

            for (int y = 0; y < outH; y++)
            for (int x = 0; x < outW; x++)
            {
                int wx = baseX + x;
                int wy = baseY + y;

                // Clamp to world bounds (handles edge chunks when world size not multiple of chunkSize).
                if (wx < 0 || wy < 0 || wx >= worldWidth || wy >= worldHeight)
                {
                    outBytes[y * outW + x] = 0;
                    continue;
                }

                var n = ComputeNormal(elevation01, worldWidth, worldHeight, wx, wy, heightScale);

                float shade = 0f;
                for (int i = 0; i < DefaultLights.Length; i++)
                    shade += Mathf.Clamp01(Vector3.Dot(n, DefaultLights[i]));

                shade /= DefaultLights.Length;

                outBytes[y * outW + x] = (byte)Mathf.Clamp(Mathf.RoundToInt(shade * 255f), 0, 255);
            }

            return outBytes;
        }

        public static byte[] BakeBathymetryChunk(
            float[] elevation01,
            int worldWidth,
            int worldHeight,
            int chunkX,
            int chunkY,
            int chunkSize,
            float seaLevel01)
        {
            var outW = chunkSize;
            var outH = chunkSize;
            var outBytes = new byte[outW * outH];

            int baseX = chunkX * chunkSize;
            int baseY = chunkY * chunkSize;

            for (int y = 0; y < outH; y++)
            for (int x = 0; x < outW; x++)
            {
                int wx = baseX + x;
                int wy = baseY + y;

                if (wx < 0 || wy < 0 || wx >= worldWidth || wy >= worldHeight)
                {
                    outBytes[y * outW + x] = 0;
                    continue;
                }

                float e = elevation01[wy * worldWidth + wx];
                if (e >= seaLevel01)
                {
                    outBytes[y * outW + x] = 0;
                    continue;
                }

                // Deeper water -> brighter value (you can invert in shader)
                float depth01 = Mathf.Clamp01((seaLevel01 - e) / Mathf.Max(0.0001f, seaLevel01));
                outBytes[y * outW + x] = (byte)Mathf.Clamp(Mathf.RoundToInt(depth01 * 255f), 0, 255);
            }

            return outBytes;
        }

        static Vector3 ComputeNormal(float[] elevation01, int w, int h, int x, int y, float heightScale)
        {
            // 3x3 Sobel-ish gradient using 4-neighbor central diff (fast and stable).
            int xm1 = Mathf.Max(0, x - 1);
            int xp1 = Mathf.Min(w - 1, x + 1);
            int ym1 = Mathf.Max(0, y - 1);
            int yp1 = Mathf.Min(h - 1, y + 1);

            float l = elevation01[y * w + xm1] * heightScale;
            float r = elevation01[y * w + xp1] * heightScale;
            float d = elevation01[ym1 * w + x] * heightScale;
            float u = elevation01[yp1 * w + x] * heightScale;

            float dx = r - l;
            float dz = u - d;

            // Y scale controls steepness in shading. 2 is a decent default for normalized height.
            var n = new Vector3(-dx, 2f, -dz);
            return n.normalized;
        }
    }
}
