using System.Collections.Generic;
using AuraLiteWorldGenerator.Editor.Core;
using AuraLiteWorldGenerator.Runtime;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor.Roads
{
    public class OrganicRoadStrategy : IRoadNetworkStrategy
    {
        public RoadNetwork Generate(RoadNetworkRequest request)
        {
            var network = new RoadNetwork();
            if (request.CostMap == null) return network;
            
            int width = request.GridSize.x;
            int height = request.GridSize.y;
            
            Vector2Int start = request.Start;
            Vector2Int end = request.End;

            start.x = Mathf.Clamp(start.x, 0, width - 1);
            start.y = Mathf.Clamp(start.y, 0, height - 1);
            end.x = Mathf.Clamp(end.x, 0, width - 1);
            end.y = Mathf.Clamp(end.y, 0, height - 1);

            SeededRandom random = new SeededRandom(request.Seed);
            
            var openSet = new List<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float>();
            var fScore = new Dictionary<Vector2Int, float>();

            openSet.Add(start);
            gScore[start] = 0;
            fScore[start] = Heuristic(start, end);

            Vector2Int[] neighbors = {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1),
                new Vector2Int(1, 1), new Vector2Int(-1, 1),
                new Vector2Int(1, -1), new Vector2Int(-1, -1)
            };

            Vector2Int current = start;
            bool found = false;

            while (openSet.Count > 0)
            {
                current = openSet[0];
                int currentIndex = 0;
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (fScore.GetValueOrDefault(openSet[i], float.MaxValue) < fScore.GetValueOrDefault(current, float.MaxValue))
                    {
                        current = openSet[i];
                        currentIndex = i;
                    }
                }

                if (current == end)
                {
                    found = true;
                    break;
                }

                openSet.RemoveAt(currentIndex);

                foreach (var dir in neighbors)
                {
                    Vector2Int neighbor = current + dir;
                    if (neighbor.x < 0 || neighbor.x >= width || neighbor.y < 0 || neighbor.y >= height)
                        continue;

                    float stepCost = (dir.x != 0 && dir.y != 0) ? 1.414f : 1.0f;
                    float baseCost = request.CostMap[neighbor.y, neighbor.x];
                    
                    if (baseCost >= 1000f) continue;
                    
                    float cost = stepCost * (1f + baseCost) + random.Range(0f, 0.1f);
                    float tentativeG = gScore.GetValueOrDefault(current, float.MaxValue) + cost;

                    if (tentativeG < gScore.GetValueOrDefault(neighbor, float.MaxValue))
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        fScore[neighbor] = tentativeG + Heuristic(neighbor, end);
                        if (!openSet.Contains(neighbor))
                            openSet.Add(neighbor);
                    }
                }
            }

            if (found)
            {
                RoadPath path = new RoadPath();
                path.mainRoad = true;
                path.width = 6f;
                
                var node = current;
                var rawPoints = new List<Vector3>();
                while (cameFrom.ContainsKey(node))
                {
                    rawPoints.Add(new Vector3(node.x, 0, node.y));
                    node = cameFrom[node];
                }
                rawPoints.Add(new Vector3(start.x, 0, start.y));
                rawPoints.Reverse();

                path.points.AddRange(SmoothPath(rawPoints, 2));
                network.Roads.Add(path);
            }

            return network;
        }

        private float Heuristic(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return 1.0f * (dx + dy) + (1.414f - 2.0f) * Mathf.Min(dx, dy);
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
