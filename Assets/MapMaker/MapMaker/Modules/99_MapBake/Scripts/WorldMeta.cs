using System;

namespace MapMaker.Modules.MapBake5.Scripts
{
    [Serializable]
    public sealed class WorldMeta
    {
        // Increment when you change the on-disk format.
        public int formatVersion = 1;

        public int width;
        public int height;

        // Root seed used to generate the world (optional for bake).
        public int rootSeed;

        // Normalized sea level threshold (0..1) in the same scale as ElevationRaw01 values.
        public float seaLevel01 = 0.5f;

        // Optional freeform notes
        public string notes;
    }
}
