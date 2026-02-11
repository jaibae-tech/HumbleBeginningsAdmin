using UnityEngine;

namespace HumbleBeginnings.WorldViewer
{
    public sealed class WorldViewerCameraBinder : MonoBehaviour
    {
        public WorldViewerController Controller;
        public WorldCameraRig CameraRig;
        public WorldChunkManager ChunkManager;
        public WorldViewerDebugHUD DebugHUD;

        [Header("Chunk Streaming Defaults")]
        public int ChunkSize = 64;
        public int LoadedRadius = 2;
        public int RenderedRadius = 2;

        void Awake()
        {
            if (!Controller) Controller = FindFirstObjectByType<WorldViewerController>();
            if (!CameraRig) CameraRig = FindFirstObjectByType<WorldCameraRig>();
            if (!ChunkManager) ChunkManager = FindFirstObjectByType<WorldChunkManager>();
            if (!DebugHUD) DebugHUD = FindFirstObjectByType<WorldViewerDebugHUD>();
        }

        // Called by WorldViewerController after LoadWorld()
        public void OnWorldLoaded()
        {
            if (!Controller || !Controller.IsLoaded) return;

            // Configure rig bounds
            if (CameraRig)
            {
                var center = Controller.WorldCenter();
                CameraRig.ConfigureWorld(Controller.Meta.width, Controller.Meta.height, Controller.TileSize, center);
            }

            // Configure chunk manager
            if (ChunkManager)
            {
                ChunkManager.ChunkSize = ChunkSize;
                ChunkManager.LoadedRadius = LoadedRadius;
                ChunkManager.RenderedRadius = Mathf.Min(RenderedRadius, LoadedRadius);
                ChunkManager.Initialize(Controller, CameraRig);
            }

            if (DebugHUD)
            {
                DebugHUD.Controller = Controller;
                DebugHUD.CameraRig = CameraRig;
                DebugHUD.ChunkManager = ChunkManager;
            }
        }

        // Called by WorldViewerController when unloading
        public void OnWorldUnloaded()
        {
            if (ChunkManager) ChunkManager.Teardown();
        }
    }
}
