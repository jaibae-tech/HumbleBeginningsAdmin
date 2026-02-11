using UnityEngine;

namespace MapMaker.Shared.Data
{
    /// <summary>
    /// Core hexagonal grid system utilities.
    /// ALL modules must use this for neighbor calculations.
    /// </summary>
    public static class GridSystem
    {
        // Grid configuration
        public const int NEIGHBOR_COUNT = 6;
        
        // Direction offsets for EVEN rows (y % 2 == 0)
        private static readonly int[] DX_EVEN = {  1,  0, -1, -1, -1,  0 };
        private static readonly int[] DY_EVEN = {  0,  1,  1,  0, -1, -1 };
        
        // Direction offsets for ODD rows (y % 2 == 1)
        private static readonly int[] DX_ODD = {  1,  1,  0, -1,  0,  1 };
        private static readonly int[] DY_ODD = {  0,  1,  1,  0, -1, -1 };
        
        /// <summary>
        /// Get neighbor coordinates in given hex direction.
        /// </summary>
        public static (int nx, int ny) GetNeighborCoords(int x, int y, HexDirection dir)
        {
            bool isOddRow = (y % 2 == 1);
            int[] dx = isOddRow ? DX_ODD : DX_EVEN;
            int[] dy = isOddRow ? DY_ODD : DY_EVEN;
            
            int d = (int)dir;
            return (x + dx[d], y + dy[d]);
        }
        
        /// <summary>
        /// Get neighbor index in linear array.
        /// Returns -1 if neighbor is out of bounds.
        /// </summary>
        public static int GetNeighborIndex(int x, int y, int width, int height, HexDirection dir)
        {
            var (nx, ny) = GetNeighborCoords(x, y, dir);
            
            if (!IsValidCoord(nx, ny, width, height))
                return -1;
            
            return ny * width + nx;
        }
        
        /// <summary>
        /// Get all valid neighbor indices for a tile.
        /// Returns 3-6 neighbors (fewer at edges).
        /// </summary>
        public static int[] GetAllNeighbors(int x, int y, int width, int height)
        {
            var neighbors = new System.Collections.Generic.List<int>();
            
            for (int d = 0; d < NEIGHBOR_COUNT; d++)
            {
                int nidx = GetNeighborIndex(x, y, width, height, (HexDirection)d);
                if (nidx >= 0)
                    neighbors.Add(nidx);
            }
            
            return neighbors.ToArray();
        }
        
        /// <summary>
        /// Check if coordinates are within bounds.
        /// </summary>
        public static bool IsValidCoord(int x, int y, int width, int height)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
        
        /// <summary>
        /// Get opposite hex direction (for edge continuity validation).
        /// E ↔ W, SE ↔ NW, SW ↔ NE
        /// </summary>
        public static HexDirection GetOpposite(HexDirection dir)
        {
            return (HexDirection)(((int)dir + 3) % 6);
        }
        
        /// <summary>
        /// Calculate hex distance between two tiles.
        /// Uses axial coordinate conversion for accurate hex distance.
        /// </summary>
        public static int HexDistance(int x1, int y1, int x2, int y2)
        {
            var (q1, r1) = OffsetToAxial(x1, y1);
            var (q2, r2) = OffsetToAxial(x2, y2);
            
            int dq = Mathf.Abs(q1 - q2);
            int dr = Mathf.Abs(r1 - r2);
            int ds = Mathf.Abs((q1 + r1) - (q2 + r2));
            
            return Mathf.Max(Mathf.Max(dq, dr), ds);
        }
        
        /// <summary>
        /// Convert offset coordinates to axial coordinates.
        /// Needed for distance and some algorithms.
        /// </summary>
        public static (int q, int r) OffsetToAxial(int x, int y)
        {
            int q = x - (y - (y & 1)) / 2;  // (y & 1) == (y % 2)
            int r = y;
            return (q, r);
        }
        
        /// <summary>
        /// Convert axial coordinates back to offset.
        /// </summary>
        public static (int x, int y) AxialToOffset(int q, int r)
        {
            int x = q + (r - (r & 1)) / 2;
            int y = r;
            return (x, y);
        }
        
        /// <summary>
        /// Convert tile coordinates to world space position for rendering.
        /// </summary>
        public static Vector3 TileToWorldPosition(int x, int y, float hexSize = 1.0f)
        {
            float width = hexSize * 2.0f;
            float height = hexSize * Mathf.Sqrt(3);
            
            float worldX = x * width * 0.75f;
            float worldZ = y * height;
            
            // Offset odd rows
            if ((y & 1) == 1)
                worldX += width * 0.375f;
            
            return new Vector3(worldX, 0, worldZ);
        }
        
        /// <summary>
        /// Get all tiles within a given radius (inclusive).
        /// </summary>
        public static System.Collections.Generic.HashSet<int> GetTilesInRadius(
            int centerX, int centerY, int radius, int width, int height)
        {
            var result = new System.Collections.Generic.HashSet<int>();
            
            // Convert to axial for easier range calculation
            var (cq, cr) = OffsetToAxial(centerX, centerY);
            
            for (int dq = -radius; dq <= radius; dq++)
            {
                int r1 = Mathf.Max(-radius, -dq - radius);
                int r2 = Mathf.Min(radius, -dq + radius);
                
                for (int dr = r1; dr <= r2; dr++)
                {
                    var (x, y) = AxialToOffset(cq + dq, cr + dr);
                    
                    if (IsValidCoord(x, y, width, height))
                    {
                        result.Add(y * width + x);
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Flood fill from starting tile using validation function.
        /// </summary>
        public static System.Collections.Generic.HashSet<int> FloodFill(
            int startIndex, int width, int height, System.Func<int, bool> isValid)
        {
            var result = new System.Collections.Generic.HashSet<int>();
            var queue = new System.Collections.Generic.Queue<int>();
            
            if (!isValid(startIndex))
                return result;
            
            queue.Enqueue(startIndex);
            result.Add(startIndex);
            
            while (queue.Count > 0)
            {
                int idx = queue.Dequeue();
                int x = idx % width;
                int y = idx / width;
                
                // Check all 6 hex neighbors
                for (int d = 0; d < NEIGHBOR_COUNT; d++)
                {
                    int nidx = GetNeighborIndex(x, y, width, height, (HexDirection)d);
                    
                    if (nidx < 0 || result.Contains(nidx))
                        continue;
                    
                    if (isValid(nidx))
                    {
                        result.Add(nidx);
                        queue.Enqueue(nidx);
                    }
                }
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// Hexagonal directions (clockwise from East).
    /// </summary>
    public enum HexDirection : byte
    {
        E  = 0,  // East (0°)
        SE = 1,  // Southeast (60°)
        SW = 2,  // Southwest (120°)
        W  = 3,  // West (180°)
        NW = 4,  // Northwest (240°)
        NE = 5   // Northeast (300°)
    }
}
