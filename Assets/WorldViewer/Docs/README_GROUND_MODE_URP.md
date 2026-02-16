# Ground Mode Upgrade (URP) – Drop-in Files

This patch provides an upgraded close-zoom terrain shader for WorldViewer:

- Proper URP lighting (directional light + ambient SH + shadows)
- Shoreline beach blend (sand near sea level)
- Wet-sand band just above sea level
- Detail normals (optional, can be left as default)
- Macro color variation (procedural) to reduce tiling
- Procedural snow (no snow texture required)

## Install
Copy the `WorldViewer/` folder into your Unity project (overwrite files if prompted).

## Create the Material (do this in Unity)
1. Right-click `Assets/WorldViewer/Materials` → Create → Material
2. Name: `WW_ChunkMaterial_Ground`
3. Shader: `WorldViewer/GroundLit_URP`
4. Assign textures from AmbientCG zips (recommended):
   - Grass:
     - _GrassAlbedo  = `Grass007_4K-JPG_Color.jpg`
     - _GrassNormal  = `Grass007_4K-JPG_NormalDX.jpg`
   - Rock:
     - _RockAlbedo   = `Rock058_4K-JPG_Color.jpg`
     - _RockNormal   = `Rock058_4K-JPG_NormalDX.jpg`
   - Sand/Dirt:
     - _SandAlbedo   = `Ground033_4K-JPG_Color.jpg`
     - _SandNormal   = `Ground033_4K-JPG_NormalDX.jpg`
   - Optional Detail Normal:
     - _DetailNormal = any small-scale normal (you can reuse one of the above normals)

## Texture Import Settings (Unity)
For *Color/Albedo* maps:
- Texture Type: Default
- sRGB (Color Texture): ON
- Wrap Mode: Repeat

For *NormalDX* maps:
- Texture Type: Normal map
- Wrap Mode: Repeat
- If normals look inverted, flip the green channel in import or switch to NormalGL.

## Sea Level / Height Scale
This shader reads:
- `_HB_HeightTex`, `_HB_HeightParams`, `_HB_WorldParams`
set globally by `WV_GlobalHeightmap` when the world loads.

So shoreline + snowline track your actual world sea level automatically.

## Optional Crossfade Controller
`WV_MapGroundBlendController.cs` is provided to drive a smooth blend value `_HB_ModeBlend`
based on camera height and pitch. Wire it to your camera and materials if you want seamless
transitions from map-mode to ground-mode.

Note: the physical-map shader may ignore `_HB_ModeBlend` (safe).
