namespace MapMaker.Shared.Data
{
    /// <summary>
    /// River classification based on flow accumulation.
    /// Used by Module 4 (Hydrology) and later modules.
    /// </summary>
    public enum RiverType : byte
    {
        None = 0,         // No river
        Stream = 1,       // Small stream (flow 1-10)
        Creek = 2,        // Creek (flow 11-50)
        River = 3,        // River (flow 51-200)
        MajorRiver = 4    // Major river (flow 201+)
    }
}
