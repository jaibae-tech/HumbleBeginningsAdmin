using UnityEngine;

namespace MapMaker.Modules.Latitude2.Config
{
    [CreateAssetMenu(
        fileName = "HB_Latitude_Default",
        menuName = "Humble Beginnings / MapMaker / Module 2 - Latitude / Config")]
    public sealed class HB_LatitudeConfig : ScriptableObject
    {
        [Header("3-Band Mode Percentages (Arctic + Temperate + Tropical = 1.0)")]
        [Tooltip("Percentage of map height for Arctic/Tundra bands in 3-band mode.")]
        [Range(0f, 1f)]
        public float ThreeBandArcticPercent = 0.15f;

        [Tooltip("Percentage of map height for Temperate band in 3-band mode.")]
        [Range(0f, 1f)]
        public float ThreeBandTemperatePercent = 0.60f;

        [Tooltip("Percentage of map height for Tropical band in 3-band mode.")]
        [Range(0f, 1f)]
        public float ThreeBandTropicalPercent = 0.25f;

        [Header("5-Band Mode Percentages (Arctic + Temperate + Tropical + Temperate + Arctic = 1.0)")]
        [Tooltip("Percentage for North Arctic in 5-band mode.")]
        [Range(0f, 1f)]
        public float FiveBandNorthArcticPercent = 0.12f;

        [Tooltip("Percentage for North Temperate in 5-band mode.")]
        [Range(0f, 1f)]
        public float FiveBandNorthTemperatePercent = 0.29f;

        [Tooltip("Percentage for Tropical (equator) in 5-band mode.")]
        [Range(0f, 1f)]
        public float FiveBandTropicalPercent = 0.18f;

        [Tooltip("Percentage for South Temperate in 5-band mode.")]
        [Range(0f, 1f)]
        public float FiveBandSouthTemperatePercent = 0.29f;

        [Tooltip("Percentage for South Arctic in 5-band mode.")]
        [Range(0f, 1f)]
        public float FiveBandSouthArcticPercent = 0.12f;

        [Header("Band Warping (Natural Coastline)")]
        [Tooltip("Scale for Perlin noise that warps latitude band boundaries.")]
        public float BandWarpNoiseScale = 0.02f;

        [Tooltip("Maximum offset (as fraction of map height) for band boundaries. 0.05 = ±5% variation.")]
        [Range(0f, 0.2f)]
        public float BandWarpStrength = 0.05f;

        public float ThreeBandSum() => ThreeBandArcticPercent + ThreeBandTemperatePercent + ThreeBandTropicalPercent;
        public float FiveBandSum() => FiveBandNorthArcticPercent + FiveBandNorthTemperatePercent + FiveBandTropicalPercent + FiveBandSouthTemperatePercent + FiveBandSouthArcticPercent;

        private void OnValidate()
        {
            float sum3 = ThreeBandSum();
            if (Mathf.Abs(sum3 - 1f) > 0.01f)
            {
                Debug.LogWarning($"[HB_LatitudeConfig] 3-band percentages sum to {sum3:F3}, not 1.0. Adjust manually for predictable results.");
            }

            float sum5 = FiveBandSum();
            if (Mathf.Abs(sum5 - 1f) > 0.01f)
            {
                Debug.LogWarning($"[HB_LatitudeConfig] 5-band percentages sum to {sum5:F3}, not 1.0. Adjust manually for predictable results.");
            }

            if (BandWarpNoiseScale <= 0f)
            {
                Debug.LogWarning($"[HB_LatitudeConfig] BandWarpNoiseScale must be positive, got {BandWarpNoiseScale}");
            }
        }
    }
}
