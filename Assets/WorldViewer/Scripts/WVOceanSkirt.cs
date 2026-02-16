using UnityEngine;

namespace HumbleBeginnings.WorldViewer
{
    [DefaultExecutionOrder(55)]
    public sealed class WVOceanSkirt : MonoBehaviour
    {
        [Header("Appearance")]
        public Color OceanColor = new Color(0.18f, 0.33f, 0.55f, 1f);

[Tooltip("Optional material to use for the ocean. If null, a runtime URP/Lit material is created.")]
public Material OceanMaterial;

[Header("Water Scroll")]
public bool AnimateWater = true;
[Tooltip("UV scroll speed for the BaseMap (_BaseMap) if present.")]
public Vector2 BaseMapScroll = new Vector2(0.0025f, 0.0015f);
[Tooltip("UV scroll speed for the NormalMap (_BumpMap) if present.")]
public Vector2 NormalMapScroll = new Vector2(-0.004f, 0.002f);

        [Tooltip("How far (in tiles) to extend beyond the world bounds.")]
        public int MarginTiles = 256;

        [Tooltip("Offset below sea level (world units) to avoid z-fighting with coast tiles at sea level.")]
        public float SeaYOffset = 1.0f;

        [Header("Debug")]
        public bool LogOnConfigure = false;

        MeshFilter _mf;
        MeshRenderer _mr;

        const string GO_NAME = "WVOceanSkirt";

        public static WVOceanSkirt EnsureAndConfigure(WorldViewerController controller)
        {
            if (!controller || !controller.IsLoaded) return null;

            var existing = FindFirstObjectByType<WVOceanSkirt>();
            if (!existing)
            {
                var go = new GameObject(GO_NAME);
                existing = go.AddComponent<WVOceanSkirt>();
            }

            existing.Configure(controller);
            return existing;
        }

        void Awake() => EnsureComponents();

        void EnsureComponents()
        {
            if (!_mf) _mf = GetComponent<MeshFilter>();
            if (!_mf) _mf = gameObject.AddComponent<MeshFilter>();

            if (!_mr) _mr = GetComponent<MeshRenderer>();
            if (!_mr) _mr = gameObject.AddComponent<MeshRenderer>();
        }

        public void Configure(WorldViewerController controller)
        {
            EnsureComponents();

            var meta = controller.Meta;
            int w = Mathf.Max(2, meta.width);
            int h = Mathf.Max(2, meta.height);

            float tileSize = Mathf.Max(0.001f, controller.TileSize);

            float worldW = (w - 1) * tileSize;
            float worldH = (h - 1) * tileSize;

            float margin = Mathf.Max(0, MarginTiles) * tileSize;

            float seaY = meta.seaLevel01 * controller.HeightScale - Mathf.Abs(SeaYOffset);

            Vector3 center = controller.WorldCenter();
            transform.position = new Vector3(center.x, seaY, center.z);
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            float halfX = (worldW * 0.5f) + margin;
            float halfZ = (worldH * 0.5f) + margin;

            var mesh = new Mesh { name = "WVOceanSkirtMesh" };

            var verts = new Vector3[4]
            {
                new Vector3(-halfX, 0f, -halfZ),
                new Vector3( halfX, 0f, -halfZ),
                new Vector3( halfX, 0f,  halfZ),
                new Vector3(-halfX, 0f,  halfZ),
            };

            var uvs = new Vector2[4]
            {
                new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1)
            };

            var tris = new int[6] { 0, 2, 1, 0, 3, 2 };

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            _mf.sharedMesh = mesh;

            
if (OceanMaterial != null)
{
    _mr.sharedMaterial = OceanMaterial;
}
else if (_mr.sharedMaterial == null || _mr.sharedMaterial.shader == null)
{
    var sh = Shader.Find("Universal Render Pipeline/Lit");
    if (sh == null) sh = Shader.Find("Universal Render Pipeline/Simple Lit");
    if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
    if (sh == null) sh = Shader.Find("Unlit/Color");

    var mat = new Material(sh) { name = "WVOceanSkirt_Runtime" };
    _mr.sharedMaterial = mat;
}

ApplyMaterialParams(_mr.sharedMaterial);

            _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _mr.receiveShadows = false;

            if (LogOnConfigure)
            {
                Debug.Log($"[WorldViewer][WVOceanSkirt] Configured: world={w}x{h} tile={tileSize} seaY={seaY:0.###} marginTiles={MarginTiles}");
            }
        }



Vector2 _baseUv;
Vector2 _normUv;

void Update()
{
    if (!AnimateWater || !_mr || !_mr.sharedMaterial) return;

    var mat = _mr.sharedMaterial;
    float dt = Time.deltaTime;

    if (mat.HasProperty("_BaseMap"))
    {
        _baseUv += BaseMapScroll * dt;
        mat.SetTextureOffset("_BaseMap", _baseUv);
    }
    else if (mat.HasProperty("_MainTex"))
    {
        _baseUv += BaseMapScroll * dt;
        mat.SetTextureOffset("_MainTex", _baseUv);
    }

    if (mat.HasProperty("_BumpMap"))
    {
        _normUv += NormalMapScroll * dt;
        mat.SetTextureOffset("_BumpMap", _normUv);
    }
}

        void ApplyMaterialParams(Material mat)
        {
            if (!mat) return;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", OceanColor);

            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.9f);

            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", OceanColor);
        }
    }
}
