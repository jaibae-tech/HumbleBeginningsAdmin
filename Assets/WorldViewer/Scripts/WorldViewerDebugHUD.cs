using UnityEngine;

namespace HumbleBeginnings.WorldViewer
{
    public sealed class WorldViewerDebugHUD : MonoBehaviour
    {
        public WorldViewerController Controller;
        public WorldCameraRig CameraRig;
        public WorldChunkManager ChunkManager;

        public bool Visible = true;
        public int FontSize = 14;

        GUIStyle _style;

        void Awake()
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = FontSize,
                normal = { textColor = Color.black }
            };
        }

        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            // If you want a toggle, wire it later via an InputAction. Leaving always-on for now.
#endif
        }

        void OnGUI()
        {
            if (!Visible) return;
            if (!_style) _style = new GUIStyle(GUI.skin.label);

            var tile = CameraRig ? CameraRig.GetPivotTile() : Vector2Int.zero;

            float tilesAcross = CameraRig ? CameraRig.TilesVisibleAcross : 0f;
            float pitch = CameraRig ? CameraRig.PitchDegrees : 0f;
            float yaw = CameraRig ? CameraRig.YawDegrees : 0f;

            int loaded = ChunkManager ? ChunkManager.LoadedChunkCount : 0;
            int renderedRadius = ChunkManager ? ChunkManager.RenderedRadius : 0;
            int loadedRadius = ChunkManager ? ChunkManager.LoadedRadius : 0;

            float elev01 = 0f;
            if (Controller && Controller.Grid != null)
            {
                int x = Mathf.Clamp(tile.x, 0, Controller.Meta.WidthTiles - 1);
                int z = Mathf.Clamp(tile.y, 0, Controller.Meta.HeightTiles - 1);
                elev01 = Controller.Grid[x, z];
            }

            string text =
                $"PivotTile: {tile.x}, {tile.y}\n" +
                $"TilesVisibleAcross: {tilesAcross:0.0}\n" +
                $"Pitch: {pitch:0.0}  Yaw: {yaw:0.0}\n" +
                $"LoadedChunks: {loaded}  RenderedRadius: {renderedRadius}  LoadedRadius: {loadedRadius}\n" +
                $"Elev01: {elev01:0.000}";

            GUI.Label(new Rect(10, 10, 600, 200), text, _style);
        }
    }
}
