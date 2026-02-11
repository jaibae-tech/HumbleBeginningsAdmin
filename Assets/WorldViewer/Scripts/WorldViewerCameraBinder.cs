using UnityEngine;

namespace HumbleBeginnings.WorldViewer
{
    public sealed class WorldViewerCameraBinder : MonoBehaviour
    {
        public WorldViewerController Controller;
        public WorldCameraRig CameraRig;
        public WorldChunkManager ChunkManager;

        [Header("Chunk Streaming")]
        public int ChunkSize = 64;
        public int LoadedRadius = 2;
        public int RenderedRadius = 1;

        // Called by WorldViewerController after it loads a world.
        public void OnWorldLoaded()
        {
            if (!Controller || !CameraRig || !ChunkManager)
                return;

            CameraRig.ConfigureWorld(
                Controller.Meta.WidthTiles,
                Controller.Meta.HeightTiles,
                Controller.TileSize,
                WorldCoord.WorldCenter(Controller.Meta.WidthTiles, Controller.Meta.HeightTiles, Controller.TileSize)
            );

            ChunkManager.Initialize(
                Controller,
                CameraRig,
                ChunkSize,
                LoadedRadius,
                RenderedRadius
            );

            Debug.Log("[WorldViewerCameraBinder] Camera rig configured.");
        }

        // Called by WorldViewerController when world is unloaded (if you add that flow later).
        public void OnWorldUnloaded()
        {
            if (ChunkManager)
                ChunkManager.Teardown();
        }
    }
}
