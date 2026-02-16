using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HumbleBeginnings.WorldViewer
{
    /// <summary>
    /// Input System camera rig:
    ///  - Orbit: RMB drag (yaw/pitch around pivot)
    ///  - Pan:   MMB drag (moves pivot across XZ plane)
    ///  - Zoom:  Mouse wheel (orthographic size via "tiles visible")
    /// </summary>
    public sealed class WorldCameraRig : MonoBehaviour
    {
        [Header("Refs")]
        public Camera Cam;
        public Transform Pivot;

        [Header("World Constraints (set at runtime)")]
        public bool ClampToWorldBounds = true;
        public int WorldWidthTiles = 0;
        public int WorldHeightTiles = 0;
        public float TileSize = 1f;

        [Header("View Constraints")]
        public int MinTilesVisible = 20;
        public int MaxTilesVisible = 800;
        public float MinPitch = 45f;
        public float MaxPitch = 90f;

        [Header("Speeds")]
        public float OrbitDegreesPerPixel = 0.15f;
        public float PanUnitsPerPixel = 0.15f;
        public float ZoomSpeed = 1.0f;
        public float ZoomSensitivity = 1.0f;

        [Header("Camera Mode")]
        public bool UseOrthographic = true;
        public float InitialTilesVisible = 250f;

        [Tooltip("World-space camera distance from pivot in tile units (scaled by TileSize).")]
        public float CameraDistanceTiles = 400f;

        [Tooltip("Minimum world-space offset above pivot to avoid ground clipping.")]
        public float MinCameraHeight = 250f;

        [Header("Sea / Horizon Clamp")]
        [Tooltip("If true, prevents the camera from ever going below sea level + clearance (prevents under-world views).")]
        public bool ClampAboveSea = true;

        [Tooltip("Extra clearance above sea level in world units.")]
        public float SeaClearance = 20f;

        [Tooltip("Optional: add to camera near clip to reduce depth artifacts when low-angle (0.3–1.0 recommended).")]
        public float NearClip = 0.3f;

        WorldViewerController _controller;
        float _seaWorldY;

float _yaw;
        float _pitch = 65f;
        float _tilesVisible;

        public float TilesVisibleAcross => _tilesVisible;
        public float YawDegrees => _yaw;
        public float PitchDegrees => _pitch;

        void Awake()
        {
            if (!Cam) Cam = Camera.main;
            if (!Pivot) Pivot = transform;

            _yaw = Pivot.eulerAngles.y;
            _pitch = Mathf.Clamp(_pitch, MinPitch, MaxPitch);
            _tilesVisible = Mathf.Clamp(InitialTilesVisible, MinTilesVisible, MaxTilesVisible);

            _controller = FindFirstObjectByType<WorldViewerController>();
            RefreshSeaWorldY();

            Pivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

            RefreshSeaWorldY();

            ApplyCameraSize();
            ApplyCameraTransform();
            EnforceSeaClamp(false);
            EnforceSeaClamp(true);
        }


        void Update()
        {
#if !ENABLE_INPUT_SYSTEM
            return;
#else
            if (!Cam || !Pivot || Mouse.current == null) return;

            HandleOrbit();
            HandlePan();
            HandleZoom();

            Pivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

            if (ClampToWorldBounds && WorldWidthTiles > 0 && WorldHeightTiles > 0)
                ClampPivotToWorld();

            RefreshSeaWorldY();

            ApplyCameraSize();
            ApplyCameraTransform();
            EnforceSeaClamp(false);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        void HandleOrbit()
        {
            if (!Mouse.current.rightButton.isPressed) return;

            Vector2 d = Mouse.current.delta.ReadValue();
            _yaw += d.x * OrbitDegreesPerPixel;
            _pitch -= d.y * OrbitDegreesPerPixel;
            _pitch = Mathf.Clamp(_pitch, MinPitch, MaxPitch);
        }

        void HandlePan()
        {
            if (!Mouse.current.middleButton.isPressed) return;

            Vector2 d = Mouse.current.delta.ReadValue();
            float dx = -d.x;
            float dy = -d.y;

            Quaternion yawRot = Quaternion.Euler(0f, _yaw, 0f);
            Vector3 right = yawRot * Vector3.right;
            Vector3 forward = yawRot * Vector3.forward;

            float zoomScale = Mathf.Max(1f, _tilesVisible / 200f);
            Pivot.position += (right * dx + forward * dy) * (PanUnitsPerPixel * zoomScale);
        }

        void HandleZoom()
        {
            float scrollY = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scrollY) < 0.01f) return;

            // Windows commonly reports 120 per notch.
            float notches = (scrollY / 120f) * ZoomSensitivity;

            // Exponential zoom feels stable.
            float factor = Mathf.Pow(0.85f, notches * ZoomSpeed);
            _tilesVisible = Mathf.Clamp(_tilesVisible * factor, MinTilesVisible, MaxTilesVisible);
        }
#endif

        void ApplyCameraTransform()
        {
            if (!Cam || !Pivot) return;

            float unitsPerTile = Mathf.Max(0.0001f, TileSize);
            float distance = Mathf.Max(0.01f, CameraDistanceTiles * unitsPerTile);

            // In orthographic mode, keep the camera far enough from terrain and scale distance with zoom.
            if (Cam.orthographic)
            {
                float zoomDrivenDistance = _tilesVisible * unitsPerTile * 0.9f;
                distance = Mathf.Max(distance, zoomDrivenDistance);
            }

            // Keep camera authored relative to pivot when parented (scene default),
            // which avoids unstable world/local conversions and guarantees aiming through pivot.
            if (Cam.transform.parent == Pivot)
            {
                // Drive camera offset from pitch so low angles (e.g. 20°) are achievable.
                // distance is along the view ray; decompose into local Y/Z using sin/cos.
                float pitchRad = _pitch * Mathf.Deg2Rad;

                float localY = Mathf.Max(MinCameraHeight, distance * Mathf.Sin(pitchRad));
                float localZ = -Mathf.Max(0.01f, distance * Mathf.Cos(pitchRad));

                Vector3 localPos = new Vector3(0f, localY, localZ);

                Cam.transform.localPosition = localPos;
                Cam.transform.localRotation = Quaternion.LookRotation((-localPos).normalized, Vector3.up);
                return;
            }

            // Fallback for unparented camera references.
            Vector3 viewDir = Pivot.forward.sqrMagnitude > 0.0001f ? Pivot.forward.normalized : Vector3.forward;
            Vector3 camPos = Pivot.position - (viewDir * distance);
            camPos.y = Mathf.Max(camPos.y, MinCameraHeight);

            Cam.transform.position = camPos;
            Cam.transform.rotation = Quaternion.LookRotation((Pivot.position - camPos).normalized, Vector3.up);
        }

        void ApplyCameraSize()
        {
            Cam.orthographic = UseOrthographic;

            if (!Cam.orthographic)
                return; // perspective zoom not implemented here

            float unitsPerTile = Mathf.Max(0.0001f, TileSize);

            float targetWidthUnits = _tilesVisible * unitsPerTile;
            float targetHeightUnits = targetWidthUnits / Mathf.Max(0.0001f, Cam.aspect);

            Cam.orthographicSize = targetHeightUnits * 0.5f;

            float worldW = Mathf.Max(1f, WorldWidthTiles * unitsPerTile);
            float worldH = Mathf.Max(1f, WorldHeightTiles * unitsPerTile);
            float worldDiagonal = Mathf.Sqrt(worldW * worldW + worldH * worldH);

            Cam.nearClipPlane = Mathf.Max(0.01f, NearClip);
            Cam.farClipPlane = Mathf.Max(8000f, worldDiagonal + Mathf.Max(2000f, Cam.orthographicSize * 8f));
        }

        

void RefreshSeaWorldY()
{
    if (!ClampAboveSea) return;

    if (!_controller) _controller = FindFirstObjectByType<WorldViewerController>();
    if (_controller && _controller.IsLoaded)
    {
        var meta = _controller.Meta;
        _seaWorldY = meta.seaLevel01 * _controller.HeightScale;
    }
}

void EnforceSeaClamp(bool forceSnap)
{
    if (!ClampAboveSea || !Cam || !Pivot) return;
    if (!_controller || !_controller.IsLoaded) return;

    float minWorldY = _seaWorldY + Mathf.Max(0f, SeaClearance);

    // If the pivot itself is below sea, lift it (rare, but avoids orbiting from underwater).
    if (forceSnap && Pivot.position.y < minWorldY * 0.25f)
    {
        var p = Pivot.position;
        p.y = 0f;
        Pivot.position = p;
    }

    var camPos = Cam.transform.position;
    if (camPos.y < minWorldY)
    {
        float delta = minWorldY - camPos.y;

        if (Cam.transform.parent == Pivot)
        {
            // Increase local Y by the delta (approx). Re-aim through pivot.
            var lp = Cam.transform.localPosition;
            lp.y += delta;
            Cam.transform.localPosition = lp;

            var toPivot = (Pivot.position - Cam.transform.position);
            if (toPivot.sqrMagnitude > 0.0001f)
                Cam.transform.rotation = Quaternion.LookRotation(toPivot.normalized, Vector3.up);
        }
        else
        {
            camPos.y = minWorldY;
            Cam.transform.position = camPos;

            var toPivot = (Pivot.position - camPos);
            if (toPivot.sqrMagnitude > 0.0001f)
                Cam.transform.rotation = Quaternion.LookRotation(toPivot.normalized, Vector3.up);
        }
    }
}
void ClampPivotToWorld()
        {
            float maxX = Mathf.Max(0, WorldWidthTiles - 1) * TileSize;
            float maxZ = Mathf.Max(0, WorldHeightTiles - 1) * TileSize;

            var p = Pivot.position;
            p.x = Mathf.Clamp(p.x, 0f, maxX);
            p.z = Mathf.Clamp(p.z, 0f, maxZ);
            Pivot.position = p;
        }

        public void ConfigureWorld(int widthTiles, int heightTiles, float tileSize, Vector3 worldCenter)
        {
            WorldWidthTiles = widthTiles;
            WorldHeightTiles = heightTiles;
            TileSize = tileSize;
            Pivot.position = worldCenter;
            RefreshSeaWorldY();

            ApplyCameraSize();
            ApplyCameraTransform();
            EnforceSeaClamp(false);
            EnforceSeaClamp(true);
        }


        public Vector2Int GetPivotTile()
        {
            return WorldCoord.WorldToTile(Pivot.position, TileSize);
        }
    }
}
