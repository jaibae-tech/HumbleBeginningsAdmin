using System;
using UnityEngine;

namespace HumbleBeginnings.WorldViewer
{
    /// <summary>
    /// On-disk metadata for a generated world (WorldData/<WorldId>/Meta.json).
    /// Must remain compatible with MapBake's WorldMeta schema.
    /// </summary>
    [Serializable]
    public sealed class WorldMeta
    {
        public int formatVersion = 1;
        public int width;
        public int height;
        public int rootSeed;
        public float seaLevel01 = 0.5f;
        public string notes;
    }
}
