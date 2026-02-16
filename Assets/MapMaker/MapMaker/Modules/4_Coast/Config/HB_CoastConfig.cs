using UnityEngine;

namespace MapMaker.Modules.Coast3.Config
{
    [CreateAssetMenu(
        fileName = "HB_Coast_Default",
        menuName = "Humble Beginnings / MapMaker / Module 3 - Coast / Config")]
    public sealed class HB_CoastConfig : ScriptableObject
    {
        [Header("Legacy (kept for asset compatibility)")]
        [Tooltip("Legacy safety net. No longer used by Coast (Elevation is authoritative).")]
        public bool RemoveEdgeTouchingIslands = false;

        [Header("Coastal Shelf")]
        [Tooltip("Shelf width in tiles measured from the ocean shoreline (hex steps).")]
        [Range(1, 16)]
        public int CoastalShelfDepth = 4;

        [Tooltip("If true, shelf will not be marked on DeepOcean tiles.")]
        public bool RequireNotDeepOcean = true;

        [Header("Inland Seas (edge-disconnected ocean components)")]
        [Tooltip("If true, marks ocean components not connected to the map edge as inland seas (stored in IsInlandLake[] for legacy naming).")]
        public bool DetectInlandLakes = true;

        [Tooltip("Minimum size (in tiles) for an edge-disconnected ocean component to be flagged as inland sea.")]
        [Range(1, 500)]
        public int MinLakeSize = 8;

        [Header("Coast Distance Field")]
        [Tooltip("If true, computes CoastDistance01 for land tiles (0=coastline, 1=far inland).")]
        public bool ComputeCoastDistance = true;

        [Tooltip("Distance in tiles that maps to CoastDistance01=1. Distances beyond this clamp at 1.")]
        [Range(8, 512)]
        public int MaxCoastDistanceTiles = 128;

        [Header("Shelf / Coast Behavior")]
        [Tooltip("If true, inland seas will not receive coastal shelf classification.")]
        public bool ExcludeInlandSeasFromShelf = true;

        private void OnValidate()
        {
            CoastalShelfDepth = Mathf.Max(1, CoastalShelfDepth);
            MinLakeSize = Mathf.Max(1, MinLakeSize);
            MaxCoastDistanceTiles = Mathf.Max(8, MaxCoastDistanceTiles);
        }
    }
}
