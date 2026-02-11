using System;
using UnityEngine;

namespace HumbleBeginnings.WorldViewer
{
    /// <summary>
    /// Step 1: Loads world data from WorldData/<WorldId>/ and exposes a stable API.
    /// Rendering and camera are added in later steps.
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

        [Tooltip("Height multiplier applied when rendering (not used yet).")]
        public float HeightScale = 200f;

        [Header("Bake (optional, later)")]
        public string BakeFolderName = "Bake";
        public int ChunkSize = 64;

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
                _loader = new WorldDataLoader();
                _loader.LoadFromWorldRoot(worldRoot);

                Debug.Log($"[WorldViewer] Loaded world. Root={worldRoot}  Size={_loader.Width}x{_loader.Height}  Seed={_loader.Meta.rootSeed}  SeaLevel01={_loader.Meta.seaLevel01}");
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
            _loader = null;
            Debug.Log("[WorldViewer] Unloaded world.");
            var binder = GetComponent<WorldViewerCameraBinder>();
            if (binder != null) binder.OnWorldUnloaded();
        }

        /// <summary>
        /// Bounds-safe elevation sample.
        /// </summary>
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
    }
}
