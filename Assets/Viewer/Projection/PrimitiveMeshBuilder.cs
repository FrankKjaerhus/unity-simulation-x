using UnityEngine;

namespace UnitySimulationX.Viewer.Projection
{
    public static class PrimitiveMeshBuilder
    {
        public static Mesh CreateCone(int segments = 24)
        {
            segments = Mathf.Max(3, segments);
            var mesh = new Mesh { name = "ProceduralCone" };

            var vertices = new Vector3[segments + 2];
            var triangles = new int[segments * 6];

            vertices[0] = Vector3.zero;
            vertices[1] = Vector3.up;

            for (var i = 0; i < segments; i++)
            {
                var angle = i / (float)segments * Mathf.PI * 2f;
                vertices[i + 2] = new Vector3(Mathf.Cos(angle) * 0.5f, 0f, Mathf.Sin(angle) * 0.5f);
            }

            var tri = 0;
            for (var i = 0; i < segments; i++)
            {
                var next = (i + 1) % segments;
                triangles[tri++] = 0;
                triangles[tri++] = i + 2;
                triangles[tri++] = next + 2;

                triangles[tri++] = 1;
                triangles[tri++] = next + 2;
                triangles[tri++] = i + 2;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
