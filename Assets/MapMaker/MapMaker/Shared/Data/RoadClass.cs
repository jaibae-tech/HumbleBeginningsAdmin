namespace MapMaker.Shared.Data
{
    /// <summary>
    /// Road classification used by world data.
    /// This is intentionally minimal; gameplay/rendering can extend later.
    /// </summary>
    public enum RoadClass : byte
    {
        None = 0,
        Path = 1,
        Road = 2,
        Highway = 3
    }
}
