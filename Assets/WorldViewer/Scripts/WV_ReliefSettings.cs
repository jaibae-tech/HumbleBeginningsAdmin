using System;
using System.IO;
using UnityEngine;

namespace HumbleBeginnings.WorldViewer
{
    /// <summary>
    /// Centralizes all "relief look" configuration and applies it automatically at runtime.
    ///
    /// Supports an optional JSON file at:
    ///   <worldRoot>/ReliefSettings.json
    ///
    /// If missing, code defaults are used (so you don't have to tweak materials by hand).
    /// </summary>
    public static class WV_ReliefSettings
    {
        [Serializable]
        public sealed class Data
        {
            public float slopeStrength = 1.2f;
            public float curvatureStrength = 0.8f;
            public float curvatureRadius = 2f;

            public bool aoEnabled = false;
            public float aoStrength = 0.7f;
            public float aoRadius = 3f;

            public float oceanDarken = 0.15f;

            // New: prevents relief math from crushing to pure black.
            public float minShade = 0.25f;

            // Physical coloring (optional; only applied if shader supports it)
            public float shoreLow = 0.02f;
            public float shoreHigh = 0.03f;

            public float rockSlopeStart = 0.35f;
            public float rockSlopeEnd = 0.70f;
            public float rockStrength = 0.65f;

            public float snowHeight01 = 0.80f;
            public float snowBlend = 0.03f;
            public float snowStrength = 0.85f;
            public float snowLatitudeStrength = 0.10f;
            public float snowLatitudePower = 1.50f;
        }

        /// <summary>Load relief settings from JSON if present; otherwise return defaults.</summary>
        public static Data LoadOrDefault(string worldRoot)
        {
            try
            {
                string path = Path.Combine(worldRoot, "ReliefSettings.json");
                if (!File.Exists(path))
                    return new Data();

                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                    return new Data();

                var data = JsonUtility.FromJson<Data>(json);
                return data ?? new Data();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WorldViewer][WV_ReliefSettings] Failed to load ReliefSettings.json. Using defaults. {ex.Message}");
                return new Data();
            }
        }

        /// <summary>
        /// Apply settings to the scene's WorldChunkManager material.
        /// Creates a runtime material instance so the project asset is not modified.
        /// </summary>
        public static void ApplyToScene(string worldRoot)
        {
            var mgr = UnityEngine.Object.FindFirstObjectByType<WorldChunkManager>();
            if (mgr == null || mgr.ChunkMaterial == null)
                return;

            var data = LoadOrDefault(worldRoot);
            ApplyToMaterialRuntime(mgr, data);
        }

        static void ApplyToMaterialRuntime(WorldChunkManager mgr, Data data)
        {
            // Ensure runtime material instance (prevents accidental asset edits).
            if (mgr.ChunkMaterial != null && (Application.isPlaying))
            {
                if (mgr.ChunkMaterial.name.Contains("(Instance)") == false)
                {
                    mgr.ChunkMaterial = new Material(mgr.ChunkMaterial)
                    {
                        name = mgr.ChunkMaterial.name + " (Instance)"
                    };
                }
            }

            var mat = mgr.ChunkMaterial;
            if (mat == null) return;

            // Only set if the property exists on the current shader.
            SetIfHas(mat, "_SlopeStrength", data.slopeStrength);
            SetIfHas(mat, "_CurvatureStrength", data.curvatureStrength);
            SetIfHas(mat, "_CurvatureRadius", data.curvatureRadius);

            SetIfHas(mat, "_AOEnabled", data.aoEnabled ? 1f : 0f);
            SetIfHas(mat, "_AOStrength", data.aoStrength);
            SetIfHas(mat, "_AORadius", data.aoRadius);

            SetIfHas(mat, "_OceanDarken", data.oceanDarken);
            SetIfHas(mat, "_MinShade", data.minShade);

            // Physical coloring controls (safe no-ops on shaders that don't have these props)
            SetIfHas(mat, "_ShoreLow", data.shoreLow);
            SetIfHas(mat, "_ShoreHigh", data.shoreHigh);

            SetIfHas(mat, "_RockSlopeStart", data.rockSlopeStart);
            SetIfHas(mat, "_RockSlopeEnd", data.rockSlopeEnd);
            SetIfHas(mat, "_RockStrength", data.rockStrength);

            SetIfHas(mat, "_SnowHeight01", data.snowHeight01);
            SetIfHas(mat, "_SnowBlend", data.snowBlend);
            SetIfHas(mat, "_SnowStrength", data.snowStrength);
            SetIfHas(mat, "_SnowLatitudeStrength", data.snowLatitudeStrength);
            SetIfHas(mat, "_SnowLatitudePower", data.snowLatitudePower);
        }

        static void SetIfHas(Material mat, string prop, float value)
        {
            if (mat != null && mat.HasProperty(prop))
                mat.SetFloat(prop, value);
        }
    }
}
