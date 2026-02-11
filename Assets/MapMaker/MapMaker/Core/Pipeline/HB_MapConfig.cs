using UnityEngine;

namespace MapMaker.Core.Pipeline
{
    [CreateAssetMenu(menuName = "MapMaker/Pipeline/Map Config", fileName = "HB_MapConfig_Default")]
    public sealed class HB_MapConfig : ScriptableObject
    {
        [Header("Map Size")]
        [Min(1)] public int MapWidth = 250;
        [Min(1)] public int MapHeight = 250;

        [Header("Seed")]
        public int RootSeed = 123456;

        [Header("Latitude Bands")]
        [Tooltip("If map height < this value, use 3 latitude bands (Arctic/Temperate/Tropical). Otherwise use 5 bands (Arctic/Temperate/Tropical/Temperate/Arctic).")]
        [Min(1)] public int ThreeToFiveBandHeightThreshold = 1500;

        public void ValidateOrThrow() 
        {
            if (MapWidth <= 0 || MapHeight <= 0)
                throw new System.ArgumentException("[HB_MapConfig] MapWidth/MapHeight must be > 0.");
        }

        private void OnValidate()
        {
            if (MapWidth <= 0)
            {
                Debug.LogWarning($"[HB_MapConfig] MapWidth must be positive, got {MapWidth}");
                MapWidth = 1;
            }

            if (MapHeight <= 0)
            {
                Debug.LogWarning($"[HB_MapConfig] MapHeight must be positive, got {MapHeight}");
                MapHeight = 1;
            }

            if (MapWidth > 2000 || MapHeight > 2000)
            {
                Debug.LogWarning($"[HB_MapConfig] Map size {MapWidth}x{MapHeight} is very large. " +
                    "Consider performance implications for procedural generation.");
            }
        }
    }
}
