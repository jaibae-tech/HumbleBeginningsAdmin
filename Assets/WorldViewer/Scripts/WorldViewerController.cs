using System;
using UnityEngine;

namespace HumbleBeginnings.WorldViewer
{
    /// <summary>
    /// Loads world data from WorldData/<WorldId>/ and exposes sampling + coord conversion.
    /// </summary>
    public sealed class WorldViewerController : MonoBehaviour
    {
        [Header("World Source")]
        [Tooltip("Root folder that contains WorldData/<WorldId>/... . Relative paths are resolved against Unity project root.")]
        public string WorldDataRoot = "WorldData";

        [Tooltip("Folder name under WorldDataRoot (e.g., World_8584509).")]
        public string WorldId = "";

        [Header("Scale")]
        [Tooltip("Unity units per tile (tile is 1 mile in design, but viewer uses units).")]
        public float TileSize = 1f;

        [Tooltip("Height multiplier applied when building chunk meshes.")]
        public float HeightScale = 200f;

        [Header("Bake (optional, later)")]
        public string BakeFolderName = "Bake";
        public int ChunkSize = 64;

        [Header("Debug Logging")]
        [Tooltip("Logs elevation file stats (min/max/mean + corner samples) during load.")]
        public bool DebugLogDataLoad = true;

        [Tooltip("Logs per-chunk elevation min/max + height min/max. Can be noisy.")]
        public bool DebugLogChunkBuild = false;

        [Tooltip("Logs a compact elevation sample grid around the current camera pivot tile when UpdateChunks runs.")]
        public bool DebugLogPivotSamples = false;

        [Range(3, 21)]
        public int DebugPivotSampleGridSize = 7;

        public bool IsLoaded => _loader != null && _loader.Meta != null && _loader.Elevation01 != null;

        WorldDataLoader _loader;
        public WorldMeta Meta => _loader?.Meta;

        [ContextMenu("Load World")]
        public void LoadWorld()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(WorldId))
                {
                    Debug.LogError("[WorldViewer] WorldId is empty. Set WorldId to a folder under WorldDataRoot (e.g., World_8584509).");
                    return;
                }

                string worldRoot = WorldPaths.WorldRoot(WorldDataRoot, WorldId);

                _loader = new WorldDataLoader { DebugLogs = DebugLogDataLoad };
                _loader.LoadFromWorldRoot(worldRoot);

                // Provide global heightmap to the relief shader (seamless across chunks).
                WV_GlobalHeightmap.SetHeightmap(
                    _loader.Elevation01,
                    _loader.Width,
                    _loader.Height,
                    _loader.Meta.seaLevel01,
                    HeightScale,
                    TileSize);

                // Auto-apply relief material settings (optional JSON per world, otherwise defaults).
                WV_ReliefSettings.ApplyToScene(worldRoot);

                Debug.Log($"[WorldViewer] Loaded world. Root={worldRoot}  Size={_loader.Width}x{_loader.Height}  Seed={_loader.Meta.rootSeed}  SeaLevel01={_loader.Meta.seaLevel01:0.###}");

                var binder = GetComponent<WorldViewerCameraBinder>();
                if (binder != null) binder.OnWorldLoaded();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldViewer] Load failed: {ex}");
            }
        }

        [ContextMenu("Unload World")]
        public void UnloadWorld()
        {
            WV_GlobalHeightmap.Clear();
            _loader = null;
            Debug.Log("[WorldViewer] Unloaded world.");

            var binder = GetComponent<WorldViewerCameraBinder>();
            if (binder != null) binder.OnWorldUnloaded();
        }

        /// <summary>Bounds-safe elevation sample (0..1).</summary>
        public float GetElevation01(int tileX, int tileY)
            => _loader?.GetElevation01Clamped(tileX, tileY) ?? 0f;

        public Vector3 TileToWorld(int tileX, int tileY)
            => WorldCoord.TileToWorld(tileX, tileY, TileSize);

        public Vector2Int WorldToTile(Vector3 worldPos)
            => WorldCoord.WorldToTile(worldPos, TileSize);

        public Vector3 WorldCenter()
        {
            if (!IsLoaded) return Vector3.zero;
            return WorldCoord.WorldCenter(_loader.Width, _loader.Height, TileSize);
        }

        /// <summary>
        /// Optional: log a compact elevation grid around a pivot tile (helps detect constant values / axis issues).
        /// </summary>
        public void LogPivotElevationSamples(int pivotTileX, int pivotTileY)
        {
            if (!DebugLogPivotSamples || !IsLoaded) return;

            int n = Mathf.Clamp(DebugPivotSampleGridSize, 3, 21);
            if (n % 2 == 0) n += 1;
            int r = n / 2;

            float min = float.PositiveInfinity;
            float max = float.NegativeInfinity;

            var sb = new System.Text.StringBuilder(256);
            sb.Append($"[WorldViewer][PivotSamples] Pivot=({pivotTileX},{pivotTileY}) n={n} Sea={Meta.seaLevel01:0.###}: ");

            for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                float e = GetElevation01(pivotTileX + dx, pivotTileY + dy);
                if (!float.IsNaN(e) && !float.IsInfinity(e))
                {
                    min = Mathf.Min(min, e);
                    max = Mathf.Max(max, e);
                }

                sb.Append(e.ToString("0.00"));
                sb.Append(',');
            }

            if (float.IsPositiveInfinity(min)) min = 0f;
            if (float.IsNegativeInfinity(max)) max = 0f;

            sb.Append($"| min={min:0.###} max={max:0.###} range={(max - min):0.###}");
            Debug.Log(sb.ToString());
        }
    }
}
