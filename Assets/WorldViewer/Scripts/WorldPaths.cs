using System.IO;
using UnityEngine;

namespace HumbleBeginnings.WorldViewer
{
    public static class WorldPaths
    {
        public static string ProjectRootPath
        {
            get
            {
                // Application.dataPath -> <ProjectRoot>/Assets
                var assets = Application.dataPath;
                return Directory.GetParent(assets)?.FullName ?? assets;
            }
        }

        public static string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return ProjectRootPath;
            if (Path.IsPathRooted(path)) return path;
            return Path.Combine(ProjectRootPath, path);
        }

        public static string WorldRoot(string worldDataRoot, string worldId)
            => Path.Combine(ResolvePath(worldDataRoot), worldId);

        public static string MetaJsonPath(string worldRoot)
            => Path.Combine(worldRoot, "Meta.json");

        public static string ElevationF32Path(string worldRoot)
            => Path.Combine(worldRoot, "Tiles", "ElevationRaw.f32");

        public static string BakeHillshadePath(string worldRoot, string bakeFolder, int chunkX, int chunkY)
            => Path.Combine(worldRoot, bakeFolder, "Hillshade", $"Chunk_{chunkX}_{chunkY}.png");

        public static string BakeBathymetryPath(string worldRoot, string bakeFolder, int chunkX, int chunkY)
            => Path.Combine(worldRoot, bakeFolder, "Bathymetry", $"Chunk_{chunkX}_{chunkY}.png");
    }
}
