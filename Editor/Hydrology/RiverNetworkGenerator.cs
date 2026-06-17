using System.Collections.Generic;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor.Hydrology
{
    public class RiverNetworkGenerator
    {
        public List<Vector3> GenerateRiver(float[,] heightmap, Vector2 start, Vector2 end)
        {
            List<Vector3> points = new List<Vector3>();
            int width = heightmap.GetLength(1);
            int height = heightmap.GetLength(0);

            Vector2 current = start;
            points.Add(new Vector3(current.x, 0, current.y));

            Vector2[] dirs = {
                new Vector2(1, 0), new Vector2(-1, 0),
                new Vector2(0, 1), new Vector2(0, -1),
                new Vector2(1, 1), new Vector2(-1, 1),
                new Vector2(1, -1), new Vector2(-1, -1)
            };

            int maxSteps = width * height;
            for (int step = 0; step < maxSteps; step++)
            {
                if (Vector2.Distance(current, end) < 5f)
                {
                    points.Add(new Vector3(end.x, 0, end.y));
                    break;
                }

                Vector2 bestDir = Vector2.zero;
                float minHeight = heightmap[(int)current.y, (int)current.x];
                float maxDrop = 0f;

                foreach (var dir in dirs)
                {
                    Vector2 neighbor = current + dir;
                    if (neighbor.x < 0 || neighbor.x >= width || neighbor.y < 0 || neighbor.y >= height)
                        continue;

                    float h = heightmap[(int)neighbor.y, (int)neighbor.x];
                    float drop = minHeight - h;
                    
                    float distToEnd = Vector2.Distance(neighbor, end);
                    float currentDistToEnd = Vector2.Distance(current, end);
                    if (distToEnd < currentDistToEnd) drop += 0.001f;

                    if (drop > maxDrop)
                    {
                        maxDrop = drop;
                        bestDir = dir;
                    }
                }

                if (bestDir == Vector2.zero)
                {
                    Vector2 dirToEnd = (end - current).normalized;
                    bestDir = new Vector2(Mathf.Round(dirToEnd.x), Mathf.Round(dirToEnd.y));
                    if (bestDir == Vector2.zero) bestDir = new Vector2(1, 0);
                }

                current += bestDir;
                current.x = Mathf.Clamp(current.x, 0, width - 1);
                current.y = Mathf.Clamp(current.y, 0, height - 1);
                
                if (Vector3.Distance(points[points.Count - 1], new Vector3(current.x, 0, current.y)) > 2f)
                {
                    points.Add(new Vector3(current.x, 0, current.y));
                }
            }

            return SmoothPath(points, 2);
        }

        private List<Vector3> SmoothPath(List<Vector3> points, int iterations)
        {
            if (points.Count < 3) return points;
            
            var result = new List<Vector3>(points);
            for (int iter = 0; iter < iterations; iter++)
            {
                var newPoints = new List<Vector3>();
                newPoints.Add(result[0]);
                for (int i = 0; i < result.Count - 1; i++)
                {
                    var p0 = result[i];
                    var p1 = result[i + 1];
                    newPoints.Add(Vector3.Lerp(p0, p1, 0.25f));
                    newPoints.Add(Vector3.Lerp(p0, p1, 0.75f));
                }
                newPoints.Add(result[result.Count - 1]);
                result = newPoints;
            }
            return result;
        }
    }
}
