using UnityEngine;

namespace MapMaker.Modules.Latitude2.Config
{
    [CreateAssetMenu(
        fileName = "HB_Latitude_Default",
        menuName = "Humble Beginnings / MapMaker / Module 2 - Latitude / Config")]
    public sealed class HB_LatitudeConfig : ScriptableObject
    {
        [Header("Latitude Energy (Driver Field)")]
        [Tooltip("Lower clamp for latitude energy to avoid true polar extremes. 0..1.")]
        [Range(0f, 1f)]
        public float LatitudeMin01 = 0.15f;

        [Tooltip("Upper clamp for latitude energy to avoid true equatorial extremes. 0..1.")]
        [Range(0f, 1f)]
        public float LatitudeMax01 = 0.90f;

        [Tooltip("Optional shaping curve. 1 = linear. >1 biases mid-lats cooler; <1 biases mid-lats warmer.")]
        [Range(0.25f, 4f)]
        public float CurvePower = 1.0f;

        [Header("Optional Global Warp (One-Lobe)")]
        [Tooltip("If enabled, applies a single broad sinusoidal warp along X to simulate a tilted/curved planet slice. Not noise.")]
        public bool EnableGlobalWarp = false;

        [Tooltip("Warp amplitude added to latitude energy. Keep small (<= 0.05) to avoid fragmentation.")]
        [Range(0f, 0.05f)]
        public float WarpAmplitude = 0.03f;

        [Header("Seasonal Variance (Stored Amplitude)")]
        [Tooltip("Minimum seasonal amplitude (0..1). Applies toward the warm south.")]
        [Range(0f, 1f)]
        public float SeasonAmpMin01 = 0.05f;

        [Tooltip("Maximum seasonal amplitude (0..1). Applies toward the cold north.")]
        [Range(0f, 1f)]
        public float SeasonAmpMax01 = 0.25f;

        [Tooltip("Exponent controlling how quickly seasonal swing increases toward the north.")]
        [Range(0.25f, 4f)]
        public float SeasonLatitudePower = 1.5f;

        private void OnValidate()
        {
            LatitudeMin01 = Mathf.Clamp01(LatitudeMin01);
            LatitudeMax01 = Mathf.Clamp01(LatitudeMax01);

            if (LatitudeMax01 <= LatitudeMin01)
            {
                // keep the asset valid
                LatitudeMax01 = Mathf.Min(1f, LatitudeMin01 + 0.01f);
            }

            WarpAmplitude = Mathf.Clamp(WarpAmplitude, 0f, 0.05f);
            SeasonAmpMin01 = Mathf.Clamp01(SeasonAmpMin01);
            SeasonAmpMax01 = Mathf.Clamp01(SeasonAmpMax01);
            if (SeasonAmpMax01 < SeasonAmpMin01)
                SeasonAmpMax01 = SeasonAmpMin01;
        }
    }
}
