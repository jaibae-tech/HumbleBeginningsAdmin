using System;
using System.Collections.Generic;

namespace MapMaker.Shared.Utils
{
    public static class GridHelpers
    {
        public static int ToIndex(int x, int y, int width) => y * width + x;

        public static (int x, int y) ToCoords(int index, int width) => (index % width, index / width);

        public static bool IsInBounds(int x, int y, int width, int height) 
            => x >= 0 && x < width && y >= 0 && y < height;

        public static IEnumerable<(int x, int y)> GetNeighbors4(int x, int y, int width, int height)
        {
            if (y > 0) yield return (x, y - 1);
            if (y < height - 1) yield return (x, y + 1);
            if (x > 0) yield return (x - 1, y);
            if (x < width - 1) yield return (x + 1, y);
        }

        public static IEnumerable<(int x, int y)> GetNeighbors8(int x, int y, int width, int height)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if (IsInBounds(nx, ny, width, height))
                        yield return (nx, ny);
                }
            }
        }

        public static void FloodFill(bool[] mask, int width, int height, int startX, int startY, bool fillValue = true)
        {
            if (!IsInBounds(startX, startY, width, height)) return;
            
            int startIdx = ToIndex(startX, startY, width);
            bool searchValue = mask[startIdx];
            
            if (searchValue == fillValue) return;

            Queue<int> queue = new Queue<int>();
            queue.Enqueue(startIdx);
            mask[startIdx] = fillValue;

            while (queue.Count > 0)
            {
                int idx = queue.Dequeue();
                var (x, y) = ToCoords(idx, width);

                foreach (var (nx, ny) in GetNeighbors4(x, y, width, height))
                {
                    int nIdx = ToIndex(nx, ny, width);
                    if (mask[nIdx] == searchValue)
                    {
                        mask[nIdx] = fillValue;
                        queue.Enqueue(nIdx);
                    }
                }
            }
        }

        public static void FloodFillEdges(bool[] mask, int width, int height, bool edgeValue = false, bool fillValue = true)
        {
            for (int x = 0; x < width; x++)
            {
                if (mask[ToIndex(x, 0, width)] == edgeValue)
                    FloodFill(mask, width, height, x, 0, fillValue);
                if (mask[ToIndex(x, height - 1, width)] == edgeValue)
                    FloodFill(mask, width, height, x, height - 1, fillValue);
            }

            for (int y = 0; y < height; y++)
            {
                if (mask[ToIndex(0, y, width)] == edgeValue)
                    FloodFill(mask, width, height, 0, y, fillValue);
                if (mask[ToIndex(width - 1, y, width)] == edgeValue)
                    FloodFill(mask, width, height, width - 1, y, fillValue);
            }
        }

        public static float[] ComputeDistanceField(bool[] sources, int width, int height, float maxDistance = float.MaxValue)
        {
            int count = width * height;
            float[] distances = new float[count];

            for (int i = 0; i < count; i++)
            {
                distances[i] = sources[i] ? 0f : maxDistance;
            }

            Queue<int> queue = new Queue<int>();
            for (int i = 0; i < count; i++)
            {
                if (sources[i])
                    queue.Enqueue(i);
            }

            while (queue.Count > 0)
            {
                int idx = queue.Dequeue();
                var (x, y) = ToCoords(idx, width);
                float currentDist = distances[idx];

                foreach (var (nx, ny) in GetNeighbors4(x, y, width, height))
                {
                    int nIdx = ToIndex(nx, ny, width);
                    float newDist = currentDist + 1f;

                    if (newDist < distances[nIdx])
                    {
                        distances[nIdx] = newDist;
                        if (newDist < maxDistance)
                            queue.Enqueue(nIdx);
                    }
                }
            }

            return distances;
        }

        public static float[] ComputeEuclideanDistanceField(bool[] sources, int width, int height, float maxDistance = float.MaxValue)
        {
            int count = width * height;
            float[] distances = new float[count];

            for (int i = 0; i < count; i++)
            {
                distances[i] = maxDistance;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = ToIndex(x, y, width);
                    if (sources[idx])
                    {
                        distances[idx] = 0f;
                        continue;
                    }

                    for (int sy = 0; sy < height; sy++)
                    {
                        for (int sx = 0; sx < width; sx++)
                        {
                            int sIdx = ToIndex(sx, sy, width);
                            if (sources[sIdx])
                            {
                                float dx = x - sx;
                                float dy = y - sy;
                                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                                if (dist < distances[idx])
                                    distances[idx] = dist;
                            }
                        }
                    }
                }
            }

            return distances;
        }

        public static int CountNeighbors4Matching<T>(T[] array, int x, int y, int width, int height, T value) where T : IEquatable<T>
        {
            int count = 0;
            foreach (var (nx, ny) in GetNeighbors4(x, y, width, height))
            {
                if (array[ToIndex(nx, ny, width)].Equals(value))
                    count++;
            }
            return count;
        }

        public static int CountNeighbors8Matching<T>(T[] array, int x, int y, int width, int height, T value) where T : IEquatable<T>
        {
            int count = 0;
            foreach (var (nx, ny) in GetNeighbors8(x, y, width, height))
            {
                if (array[ToIndex(nx, ny, width)].Equals(value))
                    count++;
            }
            return count;
        }
    }
}
