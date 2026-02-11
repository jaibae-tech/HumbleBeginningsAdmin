using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HumbleBeginnings.WorldViewer
{
    public sealed class WorldViewerDebugHUD : MonoBehaviour
    {
        public bool Visible = true;

        public WorldViewerController Controller;
        public WorldCameraRig CameraRig;
        public WorldChunkManager ChunkManager;

        GUIStyle _style;

        void OnGUI()
        {
            if (!Visible) return;
            if (Controller == null || !Controller.IsLoaded) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18
                };
                _style.normal.textColor = Color.white;
            }

            int x = 10, y = 10, lh = 22;

            if (CameraRig != null)
            {
                var pivotTile = Controller.WorldToTile(CameraRig.Pivot.position);
                GUI.Label(new Rect(x, y, 900, lh), $"PivotTile: {pivotTile.x},{pivotTile.y}", _style); y += lh;

                GUI.Label(new Rect(x, y, 900, lh),
                    $"TilesVisibleAcross: {CameraRig.TilesVisibleAcross:0.0}  Pitch: {CameraRig.PitchDegrees:0.0}  Yaw: {CameraRig.YawDegrees:0.0}",
                    _style);
                y += lh;
            }

            if (ChunkManager != null)
            {
                GUI.Label(new Rect(x, y, 900, lh),
                    $"LoadedChunks: {ChunkManager.LoadedChunkCount}  RenderedRadius: {ChunkManager.RenderedRadius}  LoadedRadius: {ChunkManager.LoadedRadius}",
                    _style);
                y += lh;
            }

            // Hover tile / elevation (Input System)
#if ENABLE_INPUT_SYSTEM
            if (CameraRig != null && CameraRig.Cam != null && Mouse.current != null)
            {
                Vector2 mp = Mouse.current.position.ReadValue();
                Ray r = CameraRig.Cam.ScreenPointToRay(new Vector3(mp.x, mp.y, 0f));
                Plane ground = new Plane(Vector3.up, Vector3.zero);

                if (ground.Raycast(r, out float t))
                {
                    Vector3 hit = r.GetPoint(t);
                    Vector2Int tile = Controller.WorldToTile(hit);

                    tile.x = Mathf.Clamp(tile.x, 0, Controller.Meta.width - 1);
                    tile.y = Mathf.Clamp(tile.y, 0, Controller.Meta.height - 1);

                    float e01 = Controller.GetElevation01(tile.x, tile.y);

                    GUI.Label(new Rect(x, y, 900, lh), $"HoverTile: {tile.x},{tile.y}  Elev01: {e01:0.000}", _style);
                    y += lh;
                }
            }
#endif
        }
    }
}
