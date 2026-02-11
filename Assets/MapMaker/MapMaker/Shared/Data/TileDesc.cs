using System.Runtime.InteropServices;

namespace MapMaker.Shared.Data
{
    /// <summary>
    /// Authoritative gameplay data for a single tile.
    /// This is the ONLY data gameplay systems should read.
    /// Size: 24 bytes per tile.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TileDesc
    {
        // ===== Identity (4 bytes) =====
        
        /// <summary>
        /// Deterministic seed for this tile.
        /// Derived from: worldSeed + tileX + tileY
        /// Used to generate encounters, vegetation, etc.
        /// </summary>
        public uint TileSeed;
        
        // ===== Terrain (4 bytes) =====
        
        /// <summary>
        /// Quantized elevation: 0-65535 maps to 0.0-1.0
        /// Used for heightmap rendering and combat elevation.
        /// </summary>
        public ushort ElevationQ;
        
        /// <summary>
        /// Quantized slope: 0-255 maps to 0°-90°
        /// Affects movement cost, combat positioning.
        /// </summary>
        public byte SlopeQ;
        
        /// <summary>
        /// Terrain curvature: convex (ridges) vs concave (valleys).
        /// 0 = flat, 128 = neutral, <128 = concave, >128 = convex
        /// Optional for rendering quality.
        /// </summary>
        public byte Curvature;
        
        // ===== Hydrology (6 bytes) =====
        
        /// <summary>
        /// Water type flags (bitfield).
        /// See WaterFlags enum.
        /// </summary>
        public byte WaterFlags;
        
        /// <summary>
        /// River flow IN edges (6 bits, one per hex direction).
        /// Bit N set = water enters from direction N.
        /// Can have multiple bits (tributaries converge).
        /// </summary>
        public byte RiverFlowIn;
        
        /// <summary>
        /// River flow OUT direction and flags.
        /// Bits 0-2: OUT direction (0-5 = HexDirection, 6 = none, 7 = unused)
        /// Bits 3-7: Reserved for future flags
        /// </summary>
        public byte RiverFlowOut;
        
        /// <summary>
        /// River order/importance: 0 = no river, 1-8 = increasing size.
        /// Based on flow accumulation (drainage area).
        /// Affects river width in rendering and combat.
        /// </summary>
        public byte RiverOrder;
        
        /// <summary>
        /// Lake identifier for map labeling (0 = no lake).
        /// Lakes with same ID are connected.
        /// Used to center "Lake Name" labels.
        /// </summary>
        public ushort LakeId;
        
        // ===== Climate/Biome (3 bytes) =====
        
        /// <summary>
        /// Biome identifier: 0-255 biome types.
        /// Determines vegetation, color palette, encounter types.
        /// </summary>
        public byte BiomeId;
        
        /// <summary>
        /// Quantized temperature: 0 = coldest, 255 = hottest.
        /// Affects encounters (snow, heat exhaustion).
        /// </summary>
        public byte TemperatureQ;
        
        /// <summary>
        /// Quantized moisture: 0 = arid, 255 = saturated.
        /// Affects vegetation density in rendering.
        /// Used for feature placement (forests need moisture).
        /// </summary>
        public byte MoistureQ;
        
        // ===== Features (6 bytes) =====
        
        /// <summary>
        /// Feature presence flags (bitfield).
        /// See FeatureFlags enum.
        /// Indicates which types of features exist on this tile.
        /// </summary>
        public ushort FeatureMask;
        
        /// <summary>
        /// Index into feature array (only valid if FeatureMask != 0).
        /// References detailed feature data (dungeon layout, ruin type, etc.).
        /// </summary>
        public uint FeatureRef;
        
        // ===== Infrastructure (1 byte) =====
        
        /// <summary>
        /// Road connections (6 bits, one per hex direction).
        /// Bit N set = road connects to neighbor in direction N.
        /// Bidirectional (if A→B, then B→A).
        /// </summary>
        public byte RoadEdges;
        
        // ===== Padding (3 bytes for 8-byte alignment) =====
        
        private byte _padding1;
        private ushort _padding2;
    }
    
    /// <summary>
    /// Water type flags (bitfield for TileDesc.WaterFlags).
    /// </summary>
    [System.Flags]
    public enum WaterFlags : byte
    {
        None = 0,
        Ocean = 1 << 0,      // Deep or shallow ocean
        DeepOcean = 1 << 1,  // Deep ocean (far from land)
        Lake = 1 << 2,       // Inland lake
        River = 1 << 3,      // Flowing river
        Marsh = 1 << 4,      // Wetland/swamp
        Coastal = 1 << 5,    // Coastal shelf (shallow ocean near land)
        // Bits 6-7 reserved
    }
    
    /// <summary>
    /// Feature type flags (bitfield for TileDesc.FeatureMask).
    /// </summary>
    [System.Flags]
    public enum FeatureFlags : ushort
    {
        None = 0,
        
        // Natural features
        Forest = 1 << 0,
        Waterfall = 1 << 1,
        Rapids = 1 << 2,
        Canyon = 1 << 3,
        
        // Structures
        Ruins = 1 << 4,
        Dungeon = 1 << 5,
        Settlement = 1 << 6,
        Camp = 1 << 7,
        
        // Infrastructure
        Bridge = 1 << 8,
        Ford = 1 << 9,
        
        // Landmarks
        Monument = 1 << 10,
        Cave = 1 << 11,
        
        // Bits 12-15 reserved for expansion
    }
}
