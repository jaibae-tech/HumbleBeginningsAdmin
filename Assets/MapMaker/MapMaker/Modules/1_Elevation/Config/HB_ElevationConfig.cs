using UnityEngine;

namespace MapMaker.Modules.Elevation1.Config
{
    /// <summary>
    /// Direction of edge bias - determines where the coast is located.
    /// </summary>
    public enum EdgeBiasDirection
    {
        None = 0,   // No bias, archipelago
        West = 1,   // Coast on west edge, land grows eastward
        East = 2,   // Coast on east edge, land grows westward
        North = 3,  // Coast on north edge, land grows southward
        South = 4,  // Coast on south edge, land grows northward
        All = 5     // Coast on all edges, land in center (island continent)
    }

    [CreateAssetMenu(fileName = "HB_Elevation_Config", menuName = "MapMaker/Module Configs/1 - Elevation Config", order = 1)]
    public class HB_ElevationConfig : ScriptableObject
    {
        [Header("Base Noise")]
        [Tooltip("Base noise scale. Lower = larger features. Default 120")]
        public float NoiseScale = 120f;

        [Range(1, 8)]
        [Tooltip("Number of noise octaves. More = more detail. Default 4")]
        public int Octaves = 4;

        [Range(0.05f, 0.99f)]
        [Tooltip("Octave amplitude decay. Lower = smoother. Default 0.5")]
        public float Persistence = 0.5f;

        [Range(1f, 4f)]
        [Tooltip("Octave frequency multiplier. Default 2")]
        public float Lacunarity = 2f;

        [Header("Mountain Ranges")]
        [Range(0f, 1f)]
        [Tooltip("Strength of mountain ridges. 0 = no ridges, 1 = sharp ranges. Default 0.4")]
        public float MountainRidgeStrength = 0.4f;

        [Tooltip("Scale for mountain ridges. Lower = longer ranges. Default 100")]
        public float MountainRidgeScale = 100f;

        [Header("Edge Bias")]
        [Tooltip("Where should the coast be? All = coast on all edges (island continent)")]
        public EdgeBiasDirection EdgeBias = EdgeBiasDirection.All;

        [Range(0f, 3f)]
        [Tooltip("How strongly to pull land away from edges. Higher = stronger. Default 1.5")]
        public float ContinentalGradientStrength = 1.5f;

        [Range(0f, 0.15f)]
        [Tooltip("Percentage of map edge that fades to lower elevation. 0.1 = outer 10%. Default 0.1")]
        public float EdgeFalloffPercent = 0.1f;

        [Range(1f, 3f)]
        [Tooltip("Falloff curve. 2 = smooth quadratic, 3 = sharper cubic. Default 2")]
        public float EdgeFalloffPower = 2f;

        [Range(0f, 1f)]
        [Tooltip("Bay depth variation. 0 = uniform shallow bays, 1 = varied deep bays. Default 0.5")]
        public float BayDepthVariation = 0.5f;

        [Header("Coastal Features")]
        [Range(0f, 1f)]
        [Tooltip("Irregularity of coastline. 0 = smooth, 1 = very jagged. Default 0.5")]
        public float CoastalComplexity = 0.5f;

        [Tooltip("Scale for coastal variation. Lower = larger bays/peninsulas. Default 400")]
        public float CoastalComplexityScale = 400f;

        [Range(0f, 0.5f)]
        [Tooltip("Strength of offshore archipelagos/islands. 0 = no islands, 0.3 = many islands. Default 0.2")]
        public float ArchipelagoStrength = 0.2f;

        [Header("Elevation Bands (Must sum to 1.0)")]
        [Range(0f, 0.5f)]
        [Tooltip("Total ocean percentage. Default 0.15 (15%)")]
        public float OceanTotalPercent = 0.15f;

        [Range(0f, 1f)]
        [Tooltip("Share of ocean that is deep. 0.3 = 30% of ocean is deep. Default 0.3")]
        public float DeepOceanShareWithinOcean = 0.3f;

        [Range(0f, 1f)]
        [Tooltip("Lowland percentage of total map. Default 0.40")]
        public float LowlandPercent = 0.4f;

        [Range(0f, 1f)]
        [Tooltip("Highlands percentage. Default 0.23")]
        public float HighlandsPercent = 0.23f;

        [Range(0f, 1f)]
        [Tooltip("Low mountains percentage. Default 0.14")]
        public float LowMountainsPercent = 0.14f;

        [Range(0f, 1f)]
        [Tooltip("High mountains percentage. Default 0.08")]
        public float HighMountainsPercent = 0.08f;
    }
}
