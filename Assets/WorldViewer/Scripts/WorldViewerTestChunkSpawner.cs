using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace HumbleBeginnings.WorldViewer
{
    /// <summary>
    /// Step 3 (Test Chunk): Minimal visual proof that URP 3D renderer + camera rig can render world-derived geometry.
    /// This does NOT depend on the WorldViewerController; it reads Meta.json + Tiles/ElevationRaw.f32 directly.
    ///
    /// Hook this method to a UI button:
    ///   WorldViewerTestChunkSpawner.LoadLatestWorldAndSpawn()
    ///
    /// Output:
    ///   Creates a GameObject named "WV_TestChunk" with a MeshFilter+MeshRenderer and a simple URP/Lit material.
    /// </summary>
    public class WorldViewerTestChunkSpawner : MonoBehaviour
    {
        [Header("World Data Location")]
        [Tooltip("If empty, defaults to '<ProjectRoot>/WorldData'.")]
        public string WorldDataRootAbsolute = "";

        [Tooltip("If empty, the newest 'World_*' directory under WorldDataRoot is used.")]
        public string WorldIdOverride = "";

        [Header("Chunk Settings")]
        [Range(8, 512)]
        public int ChunkSizeTiles = 50;

        [Tooltip("World-space origin offset to add to the sampled tile coordinates.")]
        public Vector3 WorldOrigin = Vector3.zero;

        [Tooltip("Vertical height scale in Unity units (meters) for elevation values (0..1).")]
        public float HeightScale = 200f;

        [Tooltip("If true, samples elevation from the center of the world. If false, samples from (0,0).")]
        public bool CenterOnWorld = true;

        [Tooltip("If true, moves the WorldCameraRig pivot over the spawned chunk center (prevents hunting).")]
        public bool AutoFocusRigOnSpawn = true;

        [Header("Material")]
        public Color BaseColor = new Color(0.25f, 0.55f, 0.25f, 1f);

        [Tooltip("Optional. If assigned, uses this material instead of creating a URP/Lit material at runtime.")]
        public Material OverrideMaterial;

        private GameObject _chunkGo;

        public void LoadLatestWorldAndSpawn()
        {
            try
            {
                var root = ResolveWorldDataRoot();
                var worldDir = ResolveWorldDirectory(root);
                var meta = ReadMeta(worldDir);

                var elevPath = Path.Combine(worldDir, "Tiles", "ElevationRaw.f32");
                if (!File.Exists(elevPath))
                    throw new FileNotFoundException("Elevation file not found", elevPath);

                // Read float32 elevation array
                float[] elev = ReadFloat32Array(elevPath, meta.width * meta.height);

                // Choose sampling window
                int tiles = Mathf.Clamp(ChunkSizeTiles, 8, 512);
                int startX = 0;
                int startY = 0;
                if (CenterOnWorld)
                {
                    startX = Mathf.Clamp((meta.width / 2) - (tiles / 2), 0, Mathf.Max(0, meta.width - tiles - 1));
                    startY = Mathf.Clamp((meta.height / 2) - (tiles / 2), 0, Mathf.Max(0, meta.height - tiles - 1));
                }

                SpawnMesh(meta.width, meta.height, elev, startX, startY, tiles);

                Debug.Log($"[WorldViewerTestChunk] Spawned {tiles}x{tiles} at tiles ({startX},{startY}) " +
                          $"chunkPos={_chunkGo.transform.position} from {worldDir}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldViewerTestChunk] Failed: {ex.Message}\n{ex}");
            }
        }

        private string ResolveWorldDataRoot()
        {
            if (!string.IsNullOrWhiteSpace(WorldDataRootAbsolute))
                return WorldDataRootAbsolute;

            // Project root = parent of Assets
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            return Path.Combine(projectRoot ?? "", "WorldData");
        }

        private string ResolveWorldDirectory(string worldDataRoot)
        {
            if (!Directory.Exists(worldDataRoot))
                throw new DirectoryNotFoundException($"WorldDataRoot not found: {worldDataRoot}");

            if (!string.IsNullOrWhiteSpace(WorldIdOverride))
            {
                var direct = Path.Combine(worldDataRoot, WorldIdOverride);
                if (!Directory.Exists(direct))
                    throw new DirectoryNotFoundException($"World directory not found: {direct}");
                return direct;
            }

            // Pick newest directory matching "World_*"
            var dirs = Directory.GetDirectories(worldDataRoot, "World_*", SearchOption.TopDirectoryOnly);
            if (dirs == null || dirs.Length == 0)
                throw new DirectoryNotFoundException($"No World_* directories found under: {worldDataRoot}");

            var newest = dirs
                .Select(d => new DirectoryInfo(d))
                .OrderByDescending(di => di.LastWriteTimeUtc)
                .First()
                .FullName;

            return newest;
        }

        [Serializable]
        private class WorldMeta
        {
            public int formatVersion = 1;
            public int width = 0;
            public int height = 0;
            public int rootSeed = 0;
            public float seaLevel01 = 0.0f;
            public string notes = "";
        }

        private WorldMeta ReadMeta(string worldDir)
        {
            var metaPath = Path.Combine(worldDir, "Meta.json");
            if (!File.Exists(metaPath))
                throw new FileNotFoundException("Meta.json not found", metaPath);

            var json = File.ReadAllText(metaPath);
            var meta = JsonUtility.FromJson<WorldMeta>(json);
            if (meta == null || meta.width <= 0 || meta.height <= 0)
                throw new InvalidDataException("Meta.json parsed but width/height are invalid.");

            return meta;
        }

        private float[] ReadFloat32Array(string path, int expectedCount)
        {
            var bytes = File.ReadAllBytes(path);
            int count = bytes.Length / 4;
            if (count < expectedCount)
                throw new InvalidDataException($"Elevation count too small. Expected >= {expectedCount}, got {count} (bytes={bytes.Length}).");

            float[] arr = new float[expectedCount];
            Buffer.BlockCopy(bytes, 0, arr, 0, expectedCount * 4);
            return arr;
        }

        private void SpawnMesh(int worldW, int worldH, float[] elev, int startX, int startY, int tiles)
        {
            // Destroy previous
            if (_chunkGo != null) Destroy(_chunkGo);

            _chunkGo = new GameObject("WV_TestChunk");

            // OPTION A: place the chunk GameObject at the sampled tile coordinates in world space
            // Mesh vertices are 0..tiles in local XZ, so offsetting the GameObject positions it correctly.
            _chunkGo.transform.position = WorldOrigin + new Vector3(startX, 0f, startY);
            _chunkGo.transform.rotation = Quaternion.identity;
            _chunkGo.transform.localScale = Vector3.one;
            _chunkGo.layer = 0; // Default (matches your current camera mask setup)

            var mf = _chunkGo.AddComponent<MeshFilter>();
            var mr = _chunkGo.AddComponent<MeshRenderer>();

            var mesh = BuildGridMesh(worldW, worldH, elev, startX, startY, tiles);
            mf.sharedMesh = mesh;
            mr.sharedMaterial = ResolveMaterial();

            // Helpful log + autofocus so you never "hunt" on a huge world
            var rend = _chunkGo.GetComponent<Renderer>();
            if (rend != null)
            {
                Debug.Log($"[WorldViewerTestChunk] BoundsCenter={rend.bounds.center} BoundsExtents={rend.bounds.extents}");

                if (AutoFocusRigOnSpawn)
                {
                    var rig = FindFirstObjectByType<WorldCameraRig>();
                    if (rig != null)
                    {
                        Vector3 c = rend.bounds.center;
                        rig.transform.position = new Vector3(c.x, 0f, c.z);
                        if (rig.Pivot != null) rig.Pivot.position = new Vector3(c.x, 0f, c.z);
                        if (rig.Cam != null) rig.Cam.transform.LookAt(c);

                        Debug.Log($"[WorldViewerTestChunk] AutoFocus: RigPos={rig.transform.position} CamPos={(rig.Cam ? rig.Cam.transform.position : Vector3.zero)}");
                    }
                }
            }
        }

        private Material ResolveMaterial()
        {
            if (OverrideMaterial != null) return OverrideMaterial;

            // URP Lit shader name
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                // fallback
                shader = Shader.Find("Standard");
            }

            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", BaseColor);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", BaseColor);

            // Keep it untextured for now
            return mat;
        }

        private Mesh BuildGridMesh(int worldW, int worldH, float[] elev, int startX, int startY, int tiles)
        {
            // vertices are (tiles+1)x(tiles+1)
            int vx = tiles + 1;
            int vy = tiles + 1;
            int vCount = vx * vy;

            var vertices = new Vector3[vCount];
            var uvs = new Vector2[vCount];
            var normals = new Vector3[vCount];

            // Build vertices in XZ plane (local chunk space)
            int idx = 0;
            for (int y = 0; y < vy; y++)
            {
                for (int x = 0; x < vx; x++)
                {
                    int wx = Mathf.Clamp(startX + x, 0, worldW - 1);
                    int wy = Mathf.Clamp(startY + y, 0, worldH - 1);
                    float h01 = elev[wy * worldW + wx];

                    vertices[idx] = new Vector3(x, h01 * HeightScale, y);
                    uvs[idx] = new Vector2((float)x / tiles, (float)y / tiles);
                    normals[idx] = Vector3.up;
                    idx++;
                }
            }

            // Triangles: tiles*tiles*2
            int tCount = tiles * tiles * 6;
            var tris = new int[tCount];
            int ti = 0;

            for (int y = 0; y < tiles; y++)
            {
                for (int x = 0; x < tiles; x++)
                {
                    int i0 = y * vx + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + vx;
                    int i3 = i2 + 1;

                    // (i0, i2, i1) and (i1, i2, i3)
                    tris[ti++] = i0;
                    tris[ti++] = i2;
                    tris[ti++] = i1;

                    tris[ti++] = i1;
                    tris[ti++] = i2;
                    tris[ti++] = i3;
                }
            }

            var mesh = new Mesh();
            mesh.indexFormat = (vCount > 65000) ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.name = "WV_TestChunkMesh";
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = tris;

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
