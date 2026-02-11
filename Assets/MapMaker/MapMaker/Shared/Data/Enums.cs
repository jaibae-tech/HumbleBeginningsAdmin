namespace MapMaker.Shared.Data
{
    /// <summary>
    /// Elevation classification used by Module 1 (Elevation) and later modules.
    /// </summary>
    public enum ElevationBandFinal
    {
        DeepOcean = 0,
        Ocean = 1,
        Lowland = 2,
        Highlands = 3,
        LowMountains = 4,
        HighMountains = 5
    }

    /// <summary>
    /// Latitude classification used by Module 2 (Latitude) and later modules.
    /// Arctic zones are polar/tundra regions, Temperate zones are mid-latitude, Tropical zones are equatorial.
    /// </summary>
    public enum LatitudeBandType
    {
        Arctic = 0,
        Temperate = 1,
        Tropical = 2
    }

    /// <summary>
    /// Placeholder for later modules.
    /// </summary>
    public enum CoastalTag
    {
        None = 0,
        OceanAdjacent = 1,
        CoastalShelf = 2
    }
}
