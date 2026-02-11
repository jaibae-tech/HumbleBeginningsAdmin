using UnityEngine;

namespace MapMaker.Modules.MapBake5.Config
{
    [CreateAssetMenu(
        fileName = "HB_MapBakeConfig_Default",
        menuName = "Humble Beginnings / MapMaker / MapBake Config")]
    public sealed class HB_MapBakeConfig : ScriptableObject
    {
        [Header("Input")]
        [Tooltip("Root folder that contains WorldData/<WorldId>/... . If relative, it is resolved relative to the Unity project root.")]
        public string WorldDataRoot = "WorldData";

        [Tooltip("World Id folder name under WorldDataRoot (e.g., 'World_8584509').")] 
        public string WorldId = "World_8584509";

        [Header("Chunking")]
        [Tooltip("Chunk size in tiles (recommended: 64 or 128).")]
        public int ChunkSize = 64;

        [Header("Hillshade")]
        [Tooltip("Height scale multiplier applied to elevation when computing shading. Use to exaggerate relief in shading only.")]
        public float HillshadeHeightScale = 1.0f;

        [Tooltip("If enabled, bake bathymetry (ocean depth) map from elevation vs sea level.")]
        public bool BakeBathymetry = true;

        [Header("Output")]
        [Tooltip("Bake output folder name under WorldData/<WorldId>/Bake.")]
        public string BakeFolderName = "Bake";
    }
}
