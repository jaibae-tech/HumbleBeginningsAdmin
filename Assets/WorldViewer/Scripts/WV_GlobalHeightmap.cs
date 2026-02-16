using UnityEngine;

namespace HumbleBeginnings.WorldViewer
{
    /// <summary>
    /// Uploads the full-world elevation01 array to a global RFloat texture so shaders can
    /// compute slope/curvature seamlessly across chunk boundaries.
    ///
    /// Shader globals:
    ///   _HB_HeightTex    : RFloat Texture2D (elevation01)
    ///   _HB_HeightParams : (W, H, 1/W, 1/H)
    ///   _HB_WorldParams  : (SeaLevel01, HeightScale, TileSize, unused)
    /// </summary>
    public static class WV_GlobalHeightmap
    {
        static readonly int HeightTexID    = Shader.PropertyToID("_HB_HeightTex");
        static readonly int HeightParamsID = Shader.PropertyToID("_HB_HeightParams");
        static readonly int WorldParamsID  = Shader.PropertyToID("_HB_WorldParams");

        static Texture2D _heightTex;

        public static void SetHeightmap(float[] elevation01, int width, int height, float seaLevel01, float heightScale, float tileSize)
        {
            if (elevation01 == null || elevation01.Length == 0 || width <= 1 || height <= 1)
            {
                Debug.LogWarning("[WorldViewer][WV_GlobalHeightmap] Invalid heightmap input.");
                return;
            }

            // Ensure texture exists and matches dimensions.
            if (_heightTex == null || _heightTex.width != width || _heightTex.height != height)
            {
                if (_heightTex != null)
                    Object.Destroy(_heightTex);

                _heightTex = new Texture2D(width, height, TextureFormat.RFloat, mipChain: false, linear: true)
                {
                    name = "HB_WorldHeight_RFloat",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }

            // RFloat expects one float per pixel (R channel).
            _heightTex.SetPixelData(elevation01, 0);
            _heightTex.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            Shader.SetGlobalTexture(HeightTexID, _heightTex);
            Shader.SetGlobalVector(HeightParamsID, new Vector4(width, height, 1f / width, 1f / height));
            Shader.SetGlobalVector(WorldParamsID, new Vector4(seaLevel01, heightScale, tileSize, 0f));
        }

        public static void Clear()
        {
            Shader.SetGlobalTexture(HeightTexID, null);
            Shader.SetGlobalVector(HeightParamsID, Vector4.zero);
            Shader.SetGlobalVector(WorldParamsID, Vector4.zero);

            if (_heightTex != null)
            {
                Object.Destroy(_heightTex);
                _heightTex = null;
            }
        }
    }
}
