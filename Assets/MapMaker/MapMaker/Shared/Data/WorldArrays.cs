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
        public ElevationBandFinal[] ElevationBands;

        public LatitudeBandType[] LatitudeBands;

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

        public void Allocate(int width, int height)
        {
            Width = width;
            Height = height;
            var n = width * height;

            ElevationRaw = new float[n];
            ElevationBands = new ElevationBandFinal[n];
            LatitudeBands = new LatitudeBandType[n];
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
        }

        public int Index(int x, int y) => y * Width + x;
    }
}
