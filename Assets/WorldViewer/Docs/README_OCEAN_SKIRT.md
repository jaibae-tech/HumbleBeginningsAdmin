# Ocean Skirt Patch

Creates a simple ocean fill beyond world bounds to hide the edge "void" when viewing at low pitch.

- Implemented as a large quad at sea level (slightly below to avoid z-fighting).
- Created and configured automatically on world load via `WorldViewerCameraBinder`.

## Install
Copy the `WorldViewer/` folder into your Unity project (overwrite).

## Tuning
In Play Mode, select the runtime `WVOceanSkirt` object and adjust:
- `MarginTiles` (default 256)
- `OceanColor`
- `SeaYOffset`
