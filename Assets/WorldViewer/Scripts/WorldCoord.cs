using UnityEngine;

namespace HumbleBeginnings.WorldViewer
{
    /// <summary>
    /// Conversions between tile-space (int) and world-space (float).
    /// World axes:
    ///  - X = tile X
    ///  - Z = tile Z
    ///  - Y = elevation/height
    /// </summary>
    public static class WorldCoord
    {
        public static Vector3 TileToWorld(int tileX, int tileZ, float tileSize)
            => new Vector3(tileX * tileSize, 0f, tileZ * tileSize);

        public static Vector2Int WorldToTile(Vector3 worldPos, float tileSize)
        {
            int x = Mathf.FloorToInt(worldPos.x / tileSize);
            int z = Mathf.FloorToInt(worldPos.z / tileSize);
            return new Vector2Int(x, z);
        }

        public static Vector3 WorldCenter(int widthTiles, int heightTiles, float tileSize)
        {
            // Center of the tile domain [0..width-1] x [0..height-1].
            // Using width*0.5 introduces a half-tile offset and can drift camera/chunk alignment.
            float cx = Mathf.Max(0, widthTiles - 1) * tileSize * 0.5f;
            float cz = Mathf.Max(0, heightTiles - 1) * tileSize * 0.5f;
            return new Vector3(cx, 0f, cz);
        }

        // --- Helpers expected by chunk/camera code ---

        public static Vector2Int TileToChunk(int tileX, int tileZ, int chunkSize)
            => new Vector2Int(Mathf.FloorToInt(tileX / (float)chunkSize), Mathf.FloorToInt(tileZ / (float)chunkSize));

        public static Vector2Int ChunkToTileOrigin(int chunkX, int chunkZ, int chunkSize)
            => new Vector2Int(chunkX * chunkSize, chunkZ * chunkSize);
    }
}
