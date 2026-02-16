using UnityEngine;
using MapMaker.Shared.Data;

namespace MapMaker.Modules.Hydrology4.Config
{
    [CreateAssetMenu(fileName = "HB_Hydrology_Config", menuName = "MapMaker/Module Configs/4 - Hydrology Config", order = 4)]
    public class HB_HydrologyConfig : ScriptableObject
    {
        [Header("Lakes")]
        [Tooltip("If enabled, closed basins can form lakes. Lakes can overflow and carve an outlet to continue drainage.")]
        public bool EnableLakes = true;

        [Tooltip("Elevation band at or above which lake formation uses 'alpine' limits (rarer, smaller).")]
        public ElevationBandFinal AlpineBandStart = ElevationBandFinal.LowMountains;

        [Range(10, 20000)]
        [Tooltip("Minimum drainage area (flow accumulation) required for a lake to form in Lowland/Highland.")]
        public int MinCatchmentForLakeLow = 800;

        [Range(10, 50000)]
        [Tooltip("Minimum drainage area (flow accumulation) required for a lake to form in alpine bands (LowMountains/HighMountains).")]
        public int MinCatchmentForLakeHigh = 3000;

        [Range(5, 5000)]
        [Tooltip("Maximum lake surface area in tiles for Lowland/Highland lakes. Beyond this, the basin will carve an outlet.")]
        public int MaxLakeAreaTilesLow = 300;

        [Range(1, 1000)]
        [Tooltip("Maximum lake surface area in tiles for alpine lakes. Beyond this, the basin will carve an outlet.")]
        public int MaxLakeAreaTilesHigh = 40;

        [Range(0.001f, 0.25f)]
        [Tooltip("Maximum fill depth above sink elevation for Lowland/Highland lakes (normalized elevation units).")]
        public float MaxLakeFillDepthLow = 0.08f;

        [Range(0.001f, 0.25f)]
        [Tooltip("Maximum fill depth above sink elevation for alpine lakes (normalized elevation units).")]
        public float MaxLakeFillDepthHigh = 0.03f;

        [Header("Outlet Carving")]
        [Tooltip("If true, when a lake would exceed its size/depth limits it will carve an outlet and continue drainage rather than flooding further.")]
        public bool CarveOutletOnLimits = true;

        [Range(0.0005f, 0.1f)]
        [Tooltip("How much to lower the spill rim when carving an outlet (normalized elevation units).")]
        public float CarveDepth = 0.01f;

        [Range(8, 4096)]
        [Tooltip("Maximum tiles to search while routing an outlet path. Keeps runtime bounded.")]
        public int MaxOutletPathSteps = 1024;

        [Header("Flow Accumulation")]
        [Range(10, 1000)]
        [Tooltip("Minimum drainage area for a stream to appear. Default: 100")]
        public int StreamThreshold = 100;

        [Range(500, 5000)]
        [Tooltip("Minimum drainage area for a river. Default: 1000")]
        public int RiverThreshold = 1000;

        [Range(2000, 20000)]
        [Tooltip("Minimum drainage area for a large river. Default: 5000")]
        public int LargeRiverThreshold = 5000;

        [Range(10000, 100000)]
        [Tooltip("Minimum drainage area for a major river. Default: 20000")]
        public int MajorRiverThreshold = 20000;

        [Header("Lake Search")]
        [Range(1, 64)]
        [Tooltip("Maximum radius (hex steps) to consider for local sink basins when forming lakes. Prevents lakes from spanning huge regions.")]
        public int MaxLakeSearchRadius = 24;

        [Header("Feature Detection")]
        [Range(0.01f, 0.1f)]
        [Tooltip("Elevation drop threshold for waterfall detection. Default: 0.05")]
        public float WaterfallThreshold = 0.05f;

        [Range(0.005f, 0.05f)]
        [Tooltip("Elevation drop threshold for rapids detection. Default: 0.02")]
        public float RapidsThreshold = 0.02f;

        [Header("Advanced")]
        [Tooltip("Enable detailed logging for debugging. Default: false")]
        public bool VerboseLogging = false;

        [Range(0.0001f, 0.01f)]
        [Tooltip("Minimum elevation difference to determine flow direction. Default: 0.001")]
        public float MinSlopeDifference = 0.001f;
    }
}
