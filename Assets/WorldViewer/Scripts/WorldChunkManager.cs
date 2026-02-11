using System.Collections.Generic;
using UnityEngine;

namespace HumbleBeginnings.WorldViewer
{
    [DefaultExecutionOrder(60)]
    public sealed class WorldChunkManager : MonoBehaviour
    {
        [Header("Refs (wired by binder)")]
        public WorldViewerController Controller;
        public WorldCameraRig CameraRig;

        [Header("Chunk Settings")]
        public int ChunkSize = 64;
        public int LoadedRadius = 2;
        public int RenderedRadius = 1;

        [Header("Rendering")]
        public Material ChunkMaterial;
        public bool AddMeshCollider = false;

        readonly Dictionary<Vector2Int, GameObject> _chunks = new Dictionary<Vector2Int, GameObject>(256);
        Vector2Int _lastCenterChunk = new Vector2Int(int.MinValue, int.MinValue);
        ChunkMeshBuilder _builder;

        public int LoadedChunkCount => _chunks.Count;

        void Awake()
        {
            _builder = new ChunkMeshBuilder();
        }

        public void Initialize(WorldViewerController controller, WorldCameraRig rig)
        {
            Controller = controller;
            CameraRig = rig;

            if (Controller != null)
                ChunkSize = Mathf.Max(1, Controller.ChunkSize);

            EnsureMaterial();
            RebuildAll();
        }

        public void Teardown()
        {
            foreach (var kv in _chunks)
                if (kv.Value) Destroy(kv.Value);

            _chunks.Clear();
            _lastCenterChunk = new Vector2Int(int.MinValue, int.MinValue);
        }

        [ContextMenu("Rebuild All Chunks")]
        public void RebuildAll()
        {
            if (!Controller || !Controller.IsLoaded || !CameraRig) return;

            Teardown();

            var centerTile = Controller.WorldToTile(CameraRig.Pivot.position);
            var centerChunk = TileToChunk(centerTile);

            _lastCenterChunk = centerChunk;
            UpdateChunkSet(centerChunk);
            UpdateRenderState(centerChunk);
        }

        void Update()
        {
            if (!Controller || !Controller.IsLoaded || !CameraRig) return;

            // Safety
            if (RenderedRadius > LoadedRadius) RenderedRadius = LoadedRadius;

            var centerTile = Controller.WorldToTile(CameraRig.Pivot.position);
            var centerChunk = TileToChunk(centerTile);

            if (centerChunk != _lastCenterChunk)
            {
                _lastCenterChunk = centerChunk;
                UpdateChunkSet(centerChunk);
            }

            UpdateRenderState(centerChunk);
        }

        Vector2Int TileToChunk(Vector2Int tile)
        {
            int cx = Mathf.FloorToInt(tile.x / (float)ChunkSize);
            int cy = Mathf.FloorToInt(tile.y / (float)ChunkSize);
            return new Vector2Int(cx, cy);
        }

        bool ChunkStartsInWorld(Vector2Int chunkCoord)
        {
            int w = Controller.Meta.width;
            int h = Controller.Meta.height;

            int startX = chunkCoord.x * ChunkSize;
            int startY = chunkCoord.y * ChunkSize;

            // IMPORTANT: do NOT clamp negatives to 0,0 (that causes stacking).
            if (startX < 0 || startY < 0) return false;
            if (startX >= w || startY >= h) return false;

            return true;
        }

        void UpdateChunkSet(Vector2Int centerChunk)
        {
            var desired = new HashSet<Vector2Int>();

            for (int dy = -LoadedRadius; dy <= LoadedRadius; dy++)
            for (int dx = -LoadedRadius; dx <= LoadedRadius; dx++)
            {
                var cc = new Vector2Int(centerChunk.x + dx, centerChunk.y + dy);
                if (!ChunkStartsInWorld(cc)) continue;
                desired.Add(cc);
            }

            // remove
            var toRemove = new List<Vector2Int>();
            foreach (var kv in _chunks)
                if (!desired.Contains(kv.Key))
                    toRemove.Add(kv.Key);

            foreach (var key in toRemove)
            {
                if (_chunks.TryGetValue(key, out var go) && go) Destroy(go);
                _chunks.Remove(key);
            }

            // add
            foreach (var cc in desired)
            {
                if (_chunks.ContainsKey(cc)) continue;
                var go = CreateChunkGO(cc);
                if (go != null) _chunks.Add(cc, go);
            }
        }

        void UpdateRenderState(Vector2Int centerChunk)
        {
            foreach (var kv in _chunks)
            {
                int d = Mathf.Max(Mathf.Abs(kv.Key.x - centerChunk.x), Mathf.Abs(kv.Key.y - centerChunk.y));
                bool shouldRender = d <= RenderedRadius;

                if (kv.Value && kv.Value.activeSelf != shouldRender)
                    kv.Value.SetActive(shouldRender);
            }
        }

        GameObject CreateChunkGO(Vector2Int chunkCoord)
        {
            int w = Controller.Meta.width;
            int h = Controller.Meta.height;

            int startX = chunkCoord.x * ChunkSize;
            int startY = chunkCoord.y * ChunkSize;

            if (startX < 0 || startY < 0) return null;
            if (startX >= w || startY >= h) return null;

            int sizeX = Mathf.Min(ChunkSize, w - startX);
            int sizeY = Mathf.Min(ChunkSize, h - startY);
            if (sizeX <= 0 || sizeY <= 0) return null;

            var go = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
            go.transform.SetParent(transform, false);
            go.transform.position = Controller.TileToWorld(startX, startY);

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = ChunkMaterial;

            if (AddMeshCollider) go.AddComponent<MeshCollider>();

            var mesh = _builder.BuildElevationMesh(
                Controller,
                startX, startY,
                sizeX, sizeY,
                Controller.TileSize,
                Controller.HeightScale);

            mf.sharedMesh = mesh;

            if (AddMeshCollider)
            {
                var mc = go.GetComponent<MeshCollider>();
                if (mc) mc.sharedMesh = mesh;
            }

            return go;
        }

        void EnsureMaterial()
        {
            if (ChunkMaterial) return;

            // Default to your VertexColorLit if present; else URP/Lit
            var shader = Shader.Find("HumbleBeginnings/WorldViewer/VertexColorLit");
            if (!shader) shader = Shader.Find("Universal Render Pipeline/Lit");

            ChunkMaterial = new Material(shader) { name = "WV_ChunkMaterial_Runtime" };
        }
    }
}
