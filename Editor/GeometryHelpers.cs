using System.Collections.Generic;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Common math, noise and path helpers used by the world generators.
    /// </summary>
    public static class GeometryHelpers
    {
        public static float GetPathLength(RoadPath road)
        {
            if (road == null || road.points.Count < 2)
                return 0f;

            float length = 0f;
            for (int i = 0; i < road.points.Count - 1; i++)
                length += Vector3.Distance(road.points[i], road.points[i + 1]);
            return length;
        }

        public static Vector3 SamplePath(RoadPath road, float distance)
        {
            if (road == null || road.points.Count == 0)
                return Vector3.zero;
            if (road.points.Count == 1)
                return road.points[0];

            return SamplePolyline(road.points, distance);
        }

        public static Vector3 DirectionOnPath(RoadPath road, float distance)
        {
            if (road == null || road.points.Count < 2)
                return Vector3.forward;
            return DirectionOnPolyline(road.points, distance);
        }

        public static Vector3 SamplePolyline(List<Vector3> points, float distance)
        {
            if (points == null || points.Count == 0)
                return Vector3.zero;
            if (points.Count == 1)
                return points[0];

            float remaining = Mathf.Max(0f, distance);
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 a = points[i];
                Vector3 b = points[i + 1];
                float len = Vector3.Distance(a, b);
                if (remaining <= len || len < 0.0001f)
                    return len > 0.0001f ? Vector3.Lerp(a, b, remaining / len) : a;
                remaining -= len;
            }
            return points[points.Count - 1];
        }

        public static Vector3 DirectionOnPolyline(List<Vector3> points, float distance)
        {
            if (points == null || points.Count < 2)
                return Vector3.forward;

            float remaining = Mathf.Max(0f, distance);
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 a = points[i];
                Vector3 b = points[i + 1];
                float len = Vector3.Distance(a, b);
                if (remaining <= len || len < 0.0001f)
                    return (b - a).normalized;
                remaining -= len;
            }
            return (points[points.Count - 1] - points[points.Count - 2]).normalized;
        }

        public static float DistancePointPolylineXZ(Vector3 point, List<Vector3> points)
        {
            if (points == null || points.Count < 2)
                return float.MaxValue;

            float best = float.MaxValue;
            for (int i = 0; i < points.Count - 1; i++)
            {
                float d = DistancePointSegmentXZ(point, points[i], points[i + 1]);
                if (d < best)
                    best = d;
            }
            return best;
        }

        public static float DistancePointPolylineXZ(Vector3 point, RoadPath road)
        {
            if (road == null || road.points.Count < 2)
                return float.MaxValue;

            float best = float.MaxValue;
            for (int i = 0; i < road.points.Count - 1; i++)
            {
                float d = DistancePointSegmentXZ(point, road.points[i], road.points[i + 1]);
                if (d < best)
                    best = d;
            }
            return best;
        }

        public static float DistancePointSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 pp = new Vector2(p.x, p.z);
            Vector2 aa = new Vector2(a.x, a.z);
            Vector2 bb = new Vector2(b.x, b.z);
            Vector2 ab = bb - aa;
            float len = ab.sqrMagnitude;
            if (len < 0.0001f)
                return Vector2.Distance(pp, aa);
            float t = Mathf.Clamp01(Vector2.Dot(pp - aa, ab) / len);
            return Vector2.Distance(pp, aa + ab * t);
        }

        public static float FBM(float x, float y, int octaves, float persistence, float lacunarity)
        {
            float amplitude = 1f;
            float frequency = 1f;
            float sum = 0f;
            float max = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
                max += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            return max > 0f ? sum / max : 0f;
        }

        public static float Hash01(int x, int z, int s)
        {
            uint h = (uint)(x * 374761393 + z * 668265263 + s * 224682251);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h & 0x00FFFFFF) / 16777215f;
        }

        public static float SampleTerrainHeight(TerrainGrid grid, Vector3 worldPos)
        {
            if (grid == null || grid.terrains == null)
                return 0f;

            int tx = Mathf.Clamp(Mathf.FloorToInt(worldPos.x / grid.tileSize), 0, grid.tileCount - 1);
            int tz = Mathf.Clamp(Mathf.FloorToInt(worldPos.z / grid.tileSize), 0, grid.tileCount - 1);
            UnityEngine.Terrain terrain = grid.terrains[tx, tz];
            return terrain != null ? terrain.SampleHeight(worldPos) : 0f;
        }

        public static Bounds CalculateHierarchyBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(root.transform.position, Vector3.one * 10f);

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
        }

        public static bool IntersectRects(Vector2 pos1, Vector2 size1, float angle1, Vector2 pos2, Vector2 size2, float angle2, float padding)
        {
            Vector2[] corners1 = GetRectCorners(pos1, size1 + Vector2.one * padding, angle1);
            Vector2[] corners2 = GetRectCorners(pos2, size2 + Vector2.one * padding, angle2);

            return IntersectPolygons(corners1, corners2);
        }

        private static Vector2[] GetRectCorners(Vector2 pos, Vector2 size, float angle)
        {
            Vector2[] corners = new Vector2[4];
            float rad = angle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            Vector2 half = size * 0.5f;
            Vector2[] dirs = { new Vector2(-half.x, -half.y), new Vector2(half.x, -half.y), new Vector2(half.x, half.y), new Vector2(-half.x, half.y) };

            for (int i = 0; i < 4; i++)
            {
                corners[i].x = pos.x + (dirs[i].x * cos - dirs[i].y * sin);
                corners[i].y = pos.y + (dirs[i].x * sin + dirs[i].y * cos);
            }
            return corners;
        }

        private static bool IntersectPolygons(Vector2[] poly1, Vector2[] poly2)
        {
            for (int i = 0; i < poly1.Length + poly2.Length; i++)
            {
                Vector2 normal;
                if (i < poly1.Length)
                    normal = GetNormal(poly1, i);
                else
                    normal = GetNormal(poly2, i - poly1.Length);

                if (IsSeparatingAxis(normal, poly1, poly2))
                    return false;
            }
            return true;
        }

        private static Vector2 GetNormal(Vector2[] poly, int i)
        {
            Vector2 p1 = poly[i];
            Vector2 p2 = poly[(i + 1) % poly.Length];
            Vector2 edge = p2 - p1;
            return new Vector2(-edge.y, edge.x).normalized;
        }

        private static bool IsSeparatingAxis(Vector2 axis, Vector2[] poly1, Vector2[] poly2)
        {
            float min1 = float.MaxValue, max1 = float.MinValue;
            float min2 = float.MaxValue, max2 = float.MinValue;

            foreach (var p in poly1)
            {
                float proj = Vector2.Dot(p, axis);
                min1 = Mathf.Min(min1, proj);
                max1 = Mathf.Max(max1, proj);
            }

            foreach (var p in poly2)
            {
                float proj = Vector2.Dot(p, axis);
                min2 = Mathf.Min(min2, proj);
                max2 = Mathf.Max(max2, proj);
            }

            return max1 < min2 || max2 < min1;
        }
    }
}
