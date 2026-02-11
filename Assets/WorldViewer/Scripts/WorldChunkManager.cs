using System.Collections.Generic;
using UnityEngine;

namespace HumbleBeginnings.WorldViewer
{
    public sealed class WorldChunkManager : MonoBehaviour
    {
        [Header("Chunk Settings")]
        public int ChunkSize = 64;

        [Tooltip("How many chunks outward to KEEP loaded (creates a (2R+1)^2 grid).")]
        public int LoadedRadius = 2;

        [Tooltip("How many chunks outward to RENDER meshes for. <= LoadedRadius.")]
        public int RenderedRadius = 1;

        [Header("Runtime Material")]
        public Shader ChunkShader;
        Material _runtimeMat;

        WorldViewerController _controller;
        WorldCameraRig _cameraRig;

        readonly Dictionary<Vector2Int, GameObject> _loaded = new();
        Vector2Int _centerTile;
        Vector2Int _centerChunk;

        public int LoadedChunkCount => _loaded.Count;

        void Awake()
        {
            if (!ChunkShader)
                ChunkShader = Shader.Find("Universal Render Pipeline/Lit");

            _runtimeMat = new Material(ChunkShader)
            {
                name = "WV_ChunkMaterial_Runtime"
            };
        }

        void Update()
        {
            if (_controller == null || _cameraRig == null) return;
            if (_controller.Grid == null) return;

            _centerTile = _cameraRig.GetPivotTile();
            UpdateLoadedChunks();
        }

        // --- API expected by binder/HUD ---

        public void Initialize(WorldViewerController controller, WorldCameraRig rig, int chunkSize, int loadedRadius, int renderedRadius)
        {
            _controller = controller;
            _cameraRig = rig;

            ChunkSize = chunkSize;
            LoadedRadius = Mathf.Max(0, loadedRadius);
            RenderedRadius = Mathf.Clamp(renderedRadius, 0, LoadedRadius);

            _centerTile = _cameraRig.GetPivotTile();
            UpdateLoadedChunks(forceRebuild: true);
        }

        public void Teardown()
        {
            foreach (var kv in _loaded)
            {
                if (kv.Value) Destroy(kv.Value);
            }
            _loaded.Clear();

            _controller = null;
            _cameraRig = null;
        }

        public void SetController(WorldViewerController controller) => _controller = controller;

        public void SetRadii(int renderedRadius, int loadedRadius)
        {
            LoadedRadius = Mathf.Max(0, loadedRadius);
            RenderedRadius = Mathf.Clamp(renderedRadius, 0, LoadedRadius);
        }

        public void SetCenterTile(Vector2Int tile)
        {
            _centerTile = tile;
            UpdateLoadedChunks();
        }

        // --- Internal ---

        void UpdateLoadedChunks(bool forceRebuild = false)
        {
            if (_controller == null) return;

            int centerChunkX = Mathf.FloorToInt(_centerTile.x / (float)ChunkSize);
            int centerChunkZ = Mathf.FloorToInt(_centerTile.y / (float)ChunkSize);
            _centerChunk = new Vector2Int(centerChunkX, centerChunkZ);

            // determine target set
            var target = new HashSet<Vector2Int>();
            for (int dz = -LoadedRadius; dz <= LoadedRadius; dz++)
            for (int dx = -LoadedRadius; dx <= LoadedRadius; dx++)
            {
                var cc = new Vector2Int(_centerChunk.x + dx, _centerChunk.y + dz);
                target.Add(cc);

                if (!_loaded.ContainsKey(cc))
                    _loaded[cc] = CreateChunk(cc);

                // enable/disable renderer depending on RenderedRadius
                bool shouldRender = Mathf.Abs(dx) <= RenderedRadius && Mathf.Abs(dz) <= RenderedRadius;
                if (_loaded[cc] && _loaded[cc].TryGetComponent<MeshRenderer>(out var mr))
                    mr.enabled = shouldRender;
            }

            // unload anything not targeted
            var toRemove = new List<Vector2Int>();
            foreach (var kv in _loaded)
            {
                if (!target.Contains(kv.Key))
                {
                    if (kv.Value) Destroy(kv.Value);
                    toRemove.Add(kv.Key);
                }
            }
            foreach (var k in toRemove) _loaded.Remove(k);

            if (forceRebuild)
            {
                // optional future: rebuild meshes
            }
        }

        GameObject CreateChunk(Vector2Int chunkCoord)
        {
            // Tile origin for this chunk
            var originTile = WorldCoord.ChunkToTileOrigin(chunkCoord.x, chunkCoord.y, ChunkSize);

            // World position of that tile origin
            Vector3 originWS = WorldCoord.TileToWorld(originTile.x, originTile.y, _controller.TileSize);

            var go = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
            go.transform.SetParent(transform, false);
            go.transform.position = originWS;

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _runtimeMat;

            var meshBuilder = new ChunkMeshBuilder();
            mf.sharedMesh = meshBuilder.BuildElevationMesh(
                _controller.Grid,
                _controller.Meta,
                originTile.x,
                originTile.y,
                ChunkSize,
                _controller.TileSize,
                _controller.HeightScale
            );

            return go;
        }
    }
}
