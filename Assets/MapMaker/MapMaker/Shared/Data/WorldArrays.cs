using System;

namespace MapMaker.Shared.Data
{
    /// <summary>
    /// Centralized runtime buffers for map generation.
    /// Allocate once per run, reuse across modules.
    /// </summary>
    [Serializable]
    public sealed class WorldArrays
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Count => Width * Height;
        public int Length => Count;

        // --- Core layers ---
        public float[] ElevationRaw;
        // Snapshot of elevation used for all exports (captured once after Module 1).
        public float[] ElevationExport01;
        public ElevationBandFinal[] ElevationBands;

        // --- Climate drivers (Module 2: Latitude) ---
        // 0..1 where 1 = warmest (south edge), 0 = coldest (north edge)
        public float[] LatitudeEnergy01;
        // 0..1 seasonal swing amplitude proxy (higher toward the north)
        public float[] SeasonalAmplitude01;

        // --- Elevation macro scaffolding (Step 1) ---
        // 0..1 mask indicating probability/strength of land.
        public float[] LandMask01;
        // Plate partition (Voronoi).
        public ushort[] PlateId;
        // 0..1 uplift driver from plate boundaries.
        public float[] Uplift01;
        // 0..1 ruggedness proxy (optional use by renderer/gameplay)
        public float[] Ruggedness01;

        // --- Terrain derivatives (Module 1 Step 5) ---
        // 0..1 local gradient magnitude (computed after band assignment)
        public float[] Slope01;
        // 0..1 aspect of steepest descent (0 = east, 0.25 = north, 0.5 = west, 0.75 = south)
        public float[] Aspect01;
        // 0..1 signed curvature proxy (0.5 ~= flat, <0.5 concave/valley, >0.5 convex/ridge)
        public float[] Curvature01;
        // 0..1 distance to ocean (0 = coastline, 1 = far inland)
        public float[] CoastDistance01;

        // --- Coast / derived ---
        public bool[] IsDeepOcean;
        public bool[] IsOcean;
        public bool[] IsCoastalShelf;
        public bool[] IsInlandLake;

        // --- Hydrology (Module 4) ---
        public bool[] IsLake;                  // Final lake determination
        public bool[] IsDeepLake;              // Large inland lakes (200+ tiles)
        public RiverType[] RiverTypes;         // River classification
        public float[] FlowAccumulation;       // Water flow through tile
        public byte[] FlowDirection;           // 0-7 for 8 directions (optional)
        public int[] DrainageBasinId;          // Which watershed (optional)
        public bool[] IsWaterfall;
        public bool[] IsRapids;
        public bool[] IsRiver;
        public bool[] IsMainRiver;
        public bool[] IsMinorRiver;
        public bool[] IsStream;
        public bool[] IsWater;
        public float[] WaterDepth01 ;
        public float[] RiverFlow01;
        public int[]  RiverId;
        public int[]  LakeId;
        public bool[] IsRoad;
        public int[]  RoadId;
        public bool[] IsForest;
        public float[] ForestDensity01;
        public float[] Moisture01;
        public float[] Temperature01;
        public bool[] IsSwamp;
        public bool[] IsDesert;
        public bool[] IsJungle;
        public int[] BiomeId ;

        public void Allocate(int width, int height)
        {
            Width = width;
            Height = height;
            var n = width * height;

            ElevationRaw = new float[n];
            ElevationExport01 = new float[n];
            ElevationBands = new ElevationBandFinal[n];

            LatitudeEnergy01 = new float[n];
            SeasonalAmplitude01 = new float[n];

            // Step 1 scaffolding
            LandMask01 = new float[n];
            PlateId = new ushort[n];
            Uplift01 = new float[n];
            Ruggedness01 = new float[n];

            // Terrain derivatives (computed after elevation band assignment)
            Slope01 = new float[n];
            CoastDistance01 = new float[n];

            // Coast flags
            IsDeepOcean = new bool[n];
            IsOcean = new bool[n];
            IsCoastalShelf = new bool[n];
            IsInlandLake = new bool[n];

            // Hydrology arrays
            IsLake = new bool[n];
            IsDeepLake = new bool[n];
            RiverTypes = new RiverType[n];
            FlowAccumulation = new float[n];
            FlowDirection = new byte[n];
            DrainageBasinId = new int[n];

            // Additional hydrology fields
            IsRiver = new bool[n];
            IsMainRiver = new bool[n];
            IsMinorRiver = new bool[n];
            IsStream = new bool[n];
            IsWater = new bool[n];
            IsWaterfall = new bool[n];
            IsRapids = new bool[n];
            WaterDepth01 = new float[n];
            RiverFlow01 = new float[n];
            RiverId = new int[n];
            LakeId = new int[n];

            // Roads
            IsRoad = new bool[n];
            RoadId = new int[n];

            // Vegetation
            IsForest = new bool[n];
            ForestDensity01 = new float[n];

            // Climate
            Moisture01 = new float[n];
            Temperature01 = new float[n];

            // Biomes / features
            IsSwamp = new bool[n];
            IsDesert = new bool[n];
            IsJungle = new bool[n];
            BiomeId = new int[n];
        }

        public int Index(int x, int y) => y * Width + x;
    }
}
