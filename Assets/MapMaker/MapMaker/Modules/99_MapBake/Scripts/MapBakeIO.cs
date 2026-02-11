using System;
using System.IO;
using UnityEngine;

namespace MapMaker.Modules.MapBake5.Scripts
{
    public static class MapBakeIO
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
        {
            return Path.Combine(ResolvePath(worldDataRoot), worldId);
        }

        public static string MetaJsonPath(string worldRoot) => Path.Combine(worldRoot, "Meta.json");

        public static string ElevationF32Path(string worldRoot) => Path.Combine(worldRoot, "Tiles", "ElevationRaw.f32");

        public static string BakeRoot(string worldRoot, string bakeFolderName) => Path.Combine(worldRoot, bakeFolderName);

        public static string BakeHillshadePath(string bakeRoot, int chunkX, int chunkY)
            => Path.Combine(bakeRoot, "Hillshade", $"Chunk_{chunkX}_{chunkY}.png");

        public static string BakeBathymetryPath(string bakeRoot, int chunkX, int chunkY)
            => Path.Combine(bakeRoot, "Bathymetry", $"Chunk_{chunkX}_{chunkY}.png");

        public static void EnsureParentDir(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }

        public static void WriteGrayPng(string filePath, byte[] gray, int width, int height)
        {
            EnsureParentDir(filePath);
            var tex = new Texture2D(width, height, TextureFormat.R8, mipChain: false, linear: true);
            tex.LoadRawTextureData(gray);
            tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            var png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            File.WriteAllBytes(filePath, png);
        }
    }
}
