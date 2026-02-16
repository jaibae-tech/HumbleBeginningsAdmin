WorldViewer Camera + Ocean Patch

1) Camera clamps:
- Prevents camera going below sea level + SeaClearance (default 20 world units).
- Exposes NearClip on WorldCameraRig (default 0.3).

2) Ocean skirt:
- Supports assigning a custom OceanMaterial (URP Lit recommended).
- Optional UV scrolling for BaseMap and NormalMap.

Usage:
- Drop these files into Assets/WorldViewer/... (overwrite).
- In scene, select WorldCameraRig: set MinPitch ~10-15, MaxPitch ~85, SeaClearance as desired.
- In scene, select WVOceanSkirt: assign OceanMaterial if you want textured water.
  Otherwise it will create a runtime Lit material tinted OceanColor.
