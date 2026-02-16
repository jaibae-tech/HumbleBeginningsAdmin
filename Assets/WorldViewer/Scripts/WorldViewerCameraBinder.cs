using UnityEngine;

namespace HumbleBeginnings.WorldViewer
{
    public sealed class WorldViewerCameraBinder : MonoBehaviour
    {
        public WorldViewerController Controller;
        public WorldCameraRig CameraRig;
        public WorldChunkManager ChunkManager;

        void Awake()
        {
            if (!Controller) Controller = GetComponent<WorldViewerController>();
            if (!CameraRig) CameraRig = FindFirstObjectByType<WorldCameraRig>();
            if (!ChunkManager) ChunkManager = FindFirstObjectByType<WorldChunkManager>();
        }

        // Called by WorldViewerController after LoadWorld()
        public void OnWorldLoaded()
        {
            if (!Controller || !Controller.IsLoaded) return;

            if (CameraRig)
            {
                CameraRig.ConfigureWorld(
                    Controller.Meta.width,
                    Controller.Meta.height,
                    Controller.TileSize,
                    Controller.WorldCenter());
            }

            if (ChunkManager)
            {
                ChunkManager.Initialize(Controller, CameraRig);
            }

            // Ensure an ocean skirt plane exists to fill beyond world bounds (prevents edge "void" at low pitch).
            WVOceanSkirt.EnsureAndConfigure(Controller);
        }

        // Called by WorldViewerController on unload
        public void OnWorldUnloaded()
        {
            if (ChunkManager) ChunkManager.Teardown();
        }
    }
}
