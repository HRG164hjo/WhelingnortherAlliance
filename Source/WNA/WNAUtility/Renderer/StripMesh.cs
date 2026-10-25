using System.Collections.Generic;
using UnityEngine;

namespace WNA.WNAUtility.Renderer
{
    internal class StripMesh
    {
        public const int MaxPoints = 128;
        private readonly Mesh mesh;
        private readonly List<Vector3> vertices = new List<Vector3>(MaxPoints * 2);
        private readonly List<Vector2> uvs = new List<Vector2>(MaxPoints * 2);
        private readonly List<int> triangles = new List<int>((MaxPoints - 1) * 6);
        private int pointCount;
        public Mesh Mesh => mesh;
        public bool HasGeometry => pointCount >= 2;
        public StripMesh()
        {
            mesh = new Mesh { name = "WNA_StripMesh" };
            mesh.MarkDynamic();
        }
        public void Begin()
        {
            vertices.Clear();
            uvs.Clear();
            triangles.Clear();
            pointCount = 0;
        }
        public void AddPoint(Vector3 center, Vector3 halfWidth, float v)
        {
            if (pointCount >= MaxPoints)
                return;
            int i = pointCount * 2;
            vertices.Add(center - halfWidth);
            uvs.Add(new Vector2(0f, v));
            vertices.Add(center + halfWidth);
            uvs.Add(new Vector2(1f, v));
            if (pointCount > 0)
            {
                int p = i - 2;
                triangles.Add(p);
                triangles.Add(p + 3);
                triangles.Add(p + 2);
                triangles.Add(p);
                triangles.Add(p + 1);
                triangles.Add(p + 3);
            }
            pointCount++;
        }
        public void Finish()
        {
            mesh.Clear();
            if (pointCount < 2)
                return;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}
