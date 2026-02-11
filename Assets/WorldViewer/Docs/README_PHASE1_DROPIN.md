# WorldViewer Phase 1 Drop-In (Chunk Grid + Debug HUD)

This package replaces the single-chunk prototype with a stable Phase 1 world viewer foundation.

## What you get
- `WorldChunkManager`: maintains a chunk grid around the camera pivot
  - Rendered radius: 1 (3x3)
  - Loaded radius: 2 (5x5) to support 45° pitch without revealing empty space during pan/orbit
- `ChunkMeshBuilder`: builds chunk meshes from `ElevationRaw.f32`
- `WorldViewerDebugHUD`: runtime overlay (toggle with **F1**) showing pivot tile, hover tile, zoom, and chunk counts
- Vertex-color shader: `HumbleBeginnings/WorldViewer/VertexColorUnlit` (URP)

## How to apply
1. **Import / overwrite** the `Assets/WorldViewer/` folder with this drop-in.
2. In your scene, ensure these objects exist:
   - `WorldViewerController` (already present in your setup)
   - `WorldCameraRig` (already present)
   - `WorldViewerCameraBinder` on the same GameObject as `WorldViewerController`
3. Add a new empty GameObject anywhere in the scene named `WorldChunks`:
   - Add component: `WorldChunkManager`
   - (Optional) Assign a material; if left empty, a runtime material is created automatically.
4. Add a new empty GameObject named `WorldViewerHUD`:
   - Add component: `WorldViewerDebugHUD`
5. On the `WorldViewerCameraBinder`, optionally assign:
   - `ChunkManager` -> your `WorldChunks` object
   - `DebugHUD` -> your `WorldViewerHUD` object
   (If you don't assign them, it will `FindFirstObjectByType` at runtime.)

## Notes
- The old `WorldViewerTestChunkSpawner` is still present but should be considered **deprecated / unused**.
- Camera defaults updated:
  - `MinTilesVisible = 20`
  - `MinPitch = 45`
  - `MaxPitch = 90`
- Elevation band colors are a Phase 1 placeholder. Tune in `ChunkMeshBuilder.ColorForElevation01()` as needed.

## Controls
- RMB drag: orbit (yaw/pitch)
- MMB drag: pan
- Wheel: zoom
- F1: toggle debug HUD
