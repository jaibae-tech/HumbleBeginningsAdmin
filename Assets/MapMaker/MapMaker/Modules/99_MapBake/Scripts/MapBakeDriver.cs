using System;
using System.IO;
using UnityEngine;
using MapMaker.Modules.MapBake5.Config;
using MapMaker.Core.Logging;

namespace MapMaker.Modules.MapBake5.Scripts
{
    /// <summary>
    /// MapBake reads WorldData/<WorldId>/Meta.json and WorldData/<WorldId>/Tiles/ElevationRaw.f32,
    /// then writes baked viewer artifacts under WorldData/<WorldId>/<BakeFolderName>/...
    /// </summary>
    public sealed class MapBakeDriver
    {
        private readonly LogEmitter _emit;
        readonly HB_MapBakeConfig cfg;

        public MapBakeDriver(HB_MapBakeConfig cfg)
        {
            this.cfg = cfg;
        }

        public void Run()
        {
            
            if (cfg == null) throw new Exception("HB_MapBakeConfig is null.");

            var worldRoot = MapBakeIO.WorldRoot(cfg.WorldDataRoot, cfg.WorldId);
            var metaPath = MapBakeIO.MetaJsonPath(worldRoot);
            var elevPath = MapBakeIO.ElevationF32Path(worldRoot);

            var meta = WorldDataReader.LoadMeta(metaPath);
            var elev = WorldDataReader.LoadElevationF32(elevPath, meta.width, meta.height);

            var bakeRoot = MapBakeIO.BakeRoot(worldRoot, cfg.BakeFolderName);
            Directory.CreateDirectory(bakeRoot);

            int chunkSize = Mathf.Max(8, cfg.ChunkSize);
            int chunksX = Mathf.CeilToInt((float)meta.width / chunkSize);
            int chunksY = Mathf.CeilToInt((float)meta.height / chunkSize);

            Debug.Log($"[MapBake] Started. WorldRoot={worldRoot}  BakeRoot={bakeRoot}  Chunks={chunksX}x{chunksY}");

            for (int cy = 0; cy < chunksY; cy++)
            for (int cx = 0; cx < chunksX; cx++)
            {
                // Hillshade
                var hill = HillshadeBaker.BakeHillshadeChunk(
                    elev, meta.width, meta.height, cx, cy, chunkSize, cfg.HillshadeHeightScale);

                var hillPath = MapBakeIO.BakeHillshadePath(bakeRoot, cx, cy);
                MapBakeIO.WriteGrayPng(hillPath, hill, chunkSize, chunkSize);

                // Bathymetry (optional)
                if (cfg.BakeBathymetry)
                {
                    var bath = HillshadeBaker.BakeBathymetryChunk(
                        elev, meta.width, meta.height, cx, cy, chunkSize, meta.seaLevel01);

                    var bathPath = MapBakeIO.BakeBathymetryPath(bakeRoot, cx, cy);
                    MapBakeIO.WriteGrayPng(bathPath, bath, chunkSize, chunkSize);
                }
            }

            Debug.Log($"[MapBake] Completed. WorldRoot={worldRoot}  BakeRoot={bakeRoot}  Chunks={chunksX}x{chunksY}");
        }
    }
}
