using System.Collections.Generic;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Procedural mesh creation utilities for roofs, grass, billboards, etc.
    /// </summary>
    public static class MeshFactory
    {
        public static Mesh CreateRoofPrismMesh()
        {
            Mesh mesh = new Mesh();
            Vector3[] vertices =
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0f, 1f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0f, 1f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f)
            };
            int[] triangles = { 0, 1, 2, 5, 4, 3, 0, 3, 4, 0, 4, 1, 1, 4, 5, 1, 5, 2 };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateConeMesh(int segments)
        {
            Mesh mesh = new Mesh();
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            verts.Add(new Vector3(0f, 1f, 0f));
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f));
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                tris.Add(0); tris.Add(i + 1); tris.Add(next + 1);
            }

            int center = verts.Count;
            verts.Add(Vector3.zero);
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                tris.Add(center); tris.Add(next + 1); tris.Add(i + 1);
            }

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateDiscMesh(int segments, float radius)
        {
            Mesh mesh = new Mesh();
            List<Vector3> verts = new List<Vector3> { Vector3.zero };
            List<int> tris = new List<int>();
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                tris.Add(0); tris.Add(next + 1); tris.Add(i + 1);
            }
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f),
                new Vector3(-0.5f, 1f, 0f),
                new Vector3(0.5f, 1f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateGrassBladeMesh()
        {
            Mesh mesh = new Mesh();
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            AddCrossQuad(verts, tris, 0.05f, 0.75f, 0f, 0f, 0f);
            AddCrossQuad(verts, tris, 0.04f, 0.58f, 0.12f, 18f, 0.08f);
            AddCrossQuad(verts, tris, 0.035f, 0.52f, -0.10f, -16f, -0.06f);
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateWheatBladeMesh()
        {
            Mesh mesh = new Mesh();
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            AddCrossQuad(verts, tris, 0.04f, 1.1f, 0f, 0f, 0f);
            AddCrossQuad(verts, tris, 0.035f, 0.95f, 0.08f, 14f, 0.02f);
            AddCrossQuad(verts, tris, 0.035f, 0.92f, -0.07f, -12f, -0.02f);
            AddEarQuad(verts, tris, new Vector3(0f, 1.08f, 0.02f), 0.10f, 0.28f, 0f);
            AddEarQuad(verts, tris, new Vector3(0f, 1.00f, -0.02f), 0.08f, 0.22f, 18f);
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddCrossQuad(List<Vector3> verts, List<int> tris, float halfWidth, float height, float xOffset, float yawDeg, float zOffset)
        {
            AddQuad(verts, tris, new Vector3(xOffset, 0f, zOffset), halfWidth, height, yawDeg);
            AddQuad(verts, tris, new Vector3(xOffset, 0f, zOffset), halfWidth, height, yawDeg + 90f);
        }

        private static void AddEarQuad(List<Vector3> verts, List<int> tris, Vector3 center, float halfWidth, float height, float yawDeg)
        {
            AddQuad(verts, tris, center, halfWidth, height, yawDeg);
        }

        private static void AddQuad(List<Vector3> verts, List<int> tris, Vector3 center, float halfWidth, float height, float yawDeg)
        {
            Quaternion rot = Quaternion.Euler(0f, yawDeg, 0f);
            Vector3 right = rot * Vector3.right * halfWidth;
            int start = verts.Count;
            verts.Add(center - right);
            verts.Add(center + right);
            verts.Add(center + Vector3.up * height + right * 0.15f);
            verts.Add(center + Vector3.up * height - right * 0.15f);
            tris.Add(start + 0); tris.Add(start + 2); tris.Add(start + 1);
            tris.Add(start + 0); tris.Add(start + 3); tris.Add(start + 2);
            tris.Add(start + 1); tris.Add(start + 2); tris.Add(start + 0);
            tris.Add(start + 2); tris.Add(start + 3); tris.Add(start + 0);
        }

        public static Mesh CreateCubeMesh()
        {
            Mesh mesh = new Mesh();
            Vector3[] vertices = {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f),
            };
            int[] triangles = {
                0, 2, 1, 0, 3, 2, 2, 3, 7, 2, 7, 6, 1, 2, 6, 1, 6, 5, 0, 1, 5, 0, 5, 4, 3, 0, 4, 3, 4, 7, 4, 5, 6, 4, 6, 7
            };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
