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

            ApplyCameraSize();
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

            ApplyCameraSize();
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

        void ApplyCameraSize()
        {
            Cam.orthographic = UseOrthographic;

            if (!Cam.orthographic)
                return; // perspective zoom not implemented here

            float unitsPerTile = Mathf.Max(0.0001f, TileSize);

            float targetWidthUnits = _tilesVisible * unitsPerTile;
            float targetHeightUnits = targetWidthUnits / Mathf.Max(0.0001f, Cam.aspect);

            Cam.orthographicSize = targetHeightUnits * 0.5f;
        }

        void ClampPivotToWorld()
        {
            float maxX = WorldWidthTiles * TileSize;
            float maxZ = WorldHeightTiles * TileSize;

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
        }

        public Vector2Int GetPivotTile()
        {
            return WorldCoord.WorldToTile(Pivot.position, TileSize);
        }
    }
}
