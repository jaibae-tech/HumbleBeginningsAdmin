using UnityEngine;

namespace MapMaker.Modules.Coast3.Config
{
    [CreateAssetMenu(
        fileName = "HB_Coast_Default",
        menuName = "Humble Beginnings / MapMaker / Module 3 - Coast / Config")]
    public sealed class HB_CoastConfig : ScriptableObject
    {
        [Header("Island Removal")]
        [Tooltip("Safety net: Remove any landmasses that touch forbidden edges (EdgeBias should prevent this in Elevation).")]
        public bool RemoveEdgeTouchingIslands = true;

        [Header("Coastal Shelf Classification")]
        [Tooltip("Distance in tiles from land to classify as coastal shelf (vs deep ocean).")]
        [Range(1, 10)]
        public int CoastalShelfDepth = 2;

        [Header("Inland Lake Detection")]
        [Tooltip("Detect and flag landlocked ocean components as inland lakes.")]
        public bool DetectInlandLakes = true;

        [Tooltip("Minimum size (in tiles) for an ocean component to be considered a lake (smaller are converted to land).")]
        [Range(1, 100)]
        public int MinLakeSize = 4;

        [Tooltip("Threshold for classifying inland lakes as 'deep' (large) lakes. Default: 200 tiles.")]
        [Range(100, 1000)]
        public int DeepLakeThreshold = 200;

        private void OnValidate()
        {
            if (CoastalShelfDepth < 1)
            {
                Debug.LogWarning($"[HB_CoastConfig] CoastalShelfDepth must be at least 1, got {CoastalShelfDepth}");
            }

            if (MinLakeSize < 1)
            {
                Debug.LogWarning($"[HB_CoastConfig] MinLakeSize must be at least 1, got {MinLakeSize}");
            }
        }
    }
}
