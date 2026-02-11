using UnityEngine;

namespace HumbleBeginnings.WorldViewer
{
    public sealed class ChunkMeshBuilder
    {
        static readonly Color DeepOcean = new Color(0.03f, 0.10f, 0.35f, 1f);
        static readonly Color Ocean     = new Color(0.06f, 0.26f, 0.60f, 1f);
        static readonly Color Lowland   = new Color(0.24f, 0.62f, 0.22f, 1f);
        static readonly Color Highland  = new Color(0.55f, 0.70f, 0.20f, 1f);
        static readonly Color LowMtn    = new Color(0.55f, 0.55f, 0.55f, 1f);
        static readonly Color HighMtn   = new Color(0.95f, 0.95f, 0.95f, 1f);

        public Mesh BuildElevationMesh(
            WorldViewerController controller,
            int startX, int startY,
            int sizeX, int sizeY,
            float tileSize,
            float heightScale)
        {
            int tilesX = Mathf.Max(0, sizeX);
            int tilesY = Mathf.Max(0, sizeY);

            int vertsX = tilesX + 1;
            int vertsY = tilesY + 1;

            var verts = new Vector3[vertsX * vertsY];
            var cols  = new Color[verts.Length];
            var uvs   = new Vector2[verts.Length];

            float denomX = Mathf.Max(1f, tilesX);
            float denomY = Mathf.Max(1f, tilesY);

            int i = 0;
            for (int y = 0; y < vertsY; y++)
            for (int x = 0; x < vertsX; x++)
            {
                int tx = startX + x;
                int ty = startY + y;

                float e01 = controller.GetElevation01(tx, ty);
                float h = e01 * heightScale;

                verts[i] = new Vector3(x * tileSize, h, y * tileSize);
                cols[i]  = ColorForElevation01(e01, controller.Meta != null ? controller.Meta.seaLevel01 : 0.33f);
                uvs[i]   = new Vector2(x / denomX, y / denomY);
                i++;
            }

            var tris = new int[tilesX * tilesY * 6];
            int t = 0;
            for (int y = 0; y < tilesY; y++)
            for (int x = 0; x < tilesX; x++)
            {
                int v00 = (y * vertsX) + x;
                int v10 = v00 + 1;
                int v01 = v00 + vertsX;
                int v11 = v01 + 1;

                tris[t++] = v00; tris[t++] = v01; tris[t++] = v10;
                tris[t++] = v10; tris[t++] = v01; tris[t++] = v11;
            }

            var mesh = new Mesh
            {
                name = $"ChunkMesh_{startX}_{startY}",
                indexFormat = (verts.Length > 65000)
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16
            };

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.colors = cols;
            mesh.uv = uvs;

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        Color ColorForElevation01(float e01, float seaLevel01)
        {
            float deep = Mathf.Max(0f, seaLevel01 - 0.10f);

            if (e01 < deep) return DeepOcean;
            if (e01 < seaLevel01) return Ocean;
            if (e01 < seaLevel01 + 0.25f) return Lowland;
            if (e01 < seaLevel01 + 0.45f) return Highland;
            if (e01 < seaLevel01 + 0.65f) return LowMtn;
            return HighMtn;
        }
    }
}
