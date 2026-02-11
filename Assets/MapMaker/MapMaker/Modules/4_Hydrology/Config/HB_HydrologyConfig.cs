using UnityEngine;

namespace MapMaker.Modules.Hydrology4.Config
{
    [CreateAssetMenu(fileName = "HB_Hydrology_Config", menuName = "MapMaker/Module Configs/4 - Hydrology Config", order = 4)]
    public class HB_HydrologyConfig : ScriptableObject
    {
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

        [Header("Basin Detection")]
        [Range(5, 100)]
        [Tooltip("Minimum basin size to keep as lake. Smaller filled in. Default: 20")]
        public int MinBasinSize = 20;

        [Range(50, 500)]
        [Tooltip("Maximum basin size. Larger basins split or capped. Default: 200")]
        public int MaxBasinSize = 200;

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
