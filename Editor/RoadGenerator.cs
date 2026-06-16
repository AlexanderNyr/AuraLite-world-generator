using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Generates road meshes, bridges, and street fences.
    /// </summary>
    public static class RoadGenerator
    {
        public static void CreateRoadMeshes(BuildContext ctx, TerrainGrid grid, WorldLayout layout, GenerationSettings settings, Transform parent)
        {
            for (int i = 0; i < layout.roads.Count; i++)
            {
                CreateRoadPathMeshes(ctx, grid, parent, layout.roads[i], settings.qualityBoost);
            }
        }

        private static void CreateRoadPathMeshes(BuildContext ctx, TerrainGrid grid, Transform parent, RoadPath road, float qualityBoost)
        {
            GameObject roadRoot = new GameObject(road.name);
            roadRoot.transform.SetParent(parent);

            for (int seg = 0; seg < road.points.Count - 1; seg++)
            {
                Vector3 a = road.points[seg];
                Vector3 b = road.points[seg + 1];
                float length = Vector3.Distance(a, b);
                int pieces = Mathf.Max(3, Mathf.CeilToInt(length / Mathf.Lerp(8f, 4.5f, Mathf.InverseLerp(1f, 3f, qualityBoost))));

                for (int i = 0; i < pieces; i++)
                {
                    float t0 = i / (float)pieces;
                    float t1 = (i + 1) / (float)pieces;
                    Vector3 p0 = Vector3.Lerp(a, b, t0);
                    Vector3 p1 = Vector3.Lerp(a, b, t1);
                    Vector3 mid = (p0 + p1) * 0.5f;
                    float segLen = Vector3.Distance(p0, p1);
                    mid.y = GeometryHelpers.SampleTerrainHeight(grid, mid) + 0.04f;

                    Quaternion rot = Quaternion.LookRotation((p1 - p0).normalized, Vector3.up);
                    Vector3 side = Vector3.Cross(Vector3.up, (p1 - p0).normalized).normalized;

                    GameObjectBuilder.CreateCubeChild(roadRoot.transform, "Road", mid, rot, new Vector3(road.width, 0.12f, segLen + 0.18f), ctx.roadMat);
                    GameObjectBuilder.CreateCubeChild(roadRoot.transform, "RutL", mid + side * (road.width * 0.16f), rot, new Vector3(road.width * 0.14f, 0.04f, segLen + 0.06f), ctx.dirtMat);
                    GameObjectBuilder.CreateCubeChild(roadRoot.transform, "RutR", mid - side * (road.width * 0.16f), rot, new Vector3(road.width * 0.14f, 0.04f, segLen + 0.06f), ctx.dirtMat);

                    if (road.hasStoneShoulders)
                    {
                        Vector3 left = mid + side * (road.width * 0.52f);
                        Vector3 right = mid - side * (road.width * 0.52f);
                        left.y = GeometryHelpers.SampleTerrainHeight(grid, left) + 0.06f;
                        right.y = GeometryHelpers.SampleTerrainHeight(grid, right) + 0.06f;
                        GameObjectBuilder.CreateCubeChild(roadRoot.transform, "ShoulderL", left, rot, new Vector3(0.24f, 0.16f, segLen + 0.2f), ctx.shoulderMat);
                        GameObjectBuilder.CreateCubeChild(roadRoot.transform, "ShoulderR", right, rot, new Vector3(0.24f, 0.16f, segLen + 0.2f), ctx.shoulderMat);
                    }
                }
            }

            MeshCombiner.CombineChildrenByMaterial(roadRoot.transform);
        }

        public static void CreateBridge(BuildContext ctx, TerrainGrid grid, WorldLayout layout, Transform parent)
        {
            if (layout.riverPoints.Count < 3)
                return;

            Vector3 a = layout.riverPoints[1];
            Vector3 b = layout.riverPoints[2];
            Vector3 center = Vector3.Lerp(a, b, 0.52f);
            Vector3 tangent = (b - a).normalized;
            Vector3 cross = Vector3.Cross(Vector3.up, tangent).normalized;
            center.y = layout.waterLevel + 1.45f;

            GameObject bridge = new GameObject("VillageBridge");
            bridge.transform.SetParent(parent);
            bridge.transform.position = center;
            bridge.transform.rotation = Quaternion.LookRotation(cross, Vector3.up);

            float deckWidth = 5.2f;
            float deckLength = 18f;
            GameObjectBuilder.CreateCubeChild(bridge.transform, "Deck", new Vector3(0f, 0f, 0f), new Vector3(deckWidth, 0.32f, deckLength), ctx.timberMat);
            GameObjectBuilder.CreateCubeChild(bridge.transform, "RailL", new Vector3(-deckWidth * 0.43f, 0.75f, 0f), new Vector3(0.18f, 0.22f, deckLength), ctx.timberMat);
            GameObjectBuilder.CreateCubeChild(bridge.transform, "RailR", new Vector3(deckWidth * 0.43f, 0.75f, 0f), new Vector3(0.18f, 0.22f, deckLength), ctx.timberMat);
            for (int i = 0; i < 4; i++)
            {
                float z = -deckLength * 0.34f + i * (deckLength * 0.23f);
                GameObjectBuilder.CreateCubeChild(bridge.transform, "PostL_" + i, new Vector3(-deckWidth * 0.43f, 0.42f, z), new Vector3(0.16f, 0.84f, 0.16f), ctx.timberMat);
                GameObjectBuilder.CreateCubeChild(bridge.transform, "PostR_" + i, new Vector3(deckWidth * 0.43f, 0.42f, z), new Vector3(0.16f, 0.84f, 0.16f), ctx.timberMat);
            }
            GameObjectBuilder.CreateCubeChild(bridge.transform, "SupportA", new Vector3(0f, -1.0f, -deckLength * 0.26f), new Vector3(0.6f, 2.0f, 0.6f), ctx.stoneMat);
            GameObjectBuilder.CreateCubeChild(bridge.transform, "SupportB", new Vector3(0f, -1.0f, deckLength * 0.26f), new Vector3(0.6f, 2.0f, 0.6f), ctx.stoneMat);

            MeshCombiner.CombineChildrenByMaterial(bridge.transform);
        }

        public static void CreateStreetFences(BuildContext ctx, TerrainGrid grid, WorldLayout layout, Transform parent)
        {
            GameObject root = new GameObject("StreetFences");
            root.transform.SetParent(parent);
            HashSet<string> targetRoads = new HashSet<string> { "MainStreet", "CrossRoad", "NorthLane", "SouthLane" };

            for (int r = 0; r < layout.roads.Count; r++)
            {
                RoadPath road = layout.roads[r];
                if (!targetRoads.Contains(road.name))
                    continue;

                float length = GeometryHelpers.GetPathLength(road);
                float spacing = 9f;
                float offset = road.width * 0.82f + 8.5f;
                for (float d = 12f; d < length - 12f; d += spacing)
                {
                    Vector3 p = GeometryHelpers.SamplePath(road, d);
                    Vector3 dir = GeometryHelpers.DirectionOnPath(road, d);
                    Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Vector3 postPos = p + side * offset * s;
                        if (WorldLayoutGenerator.ComputeVillageMask(layout, postPos.x, postPos.z) < 0.12f || WorldLayoutGenerator.ComputeRiverMask(layout, postPos.x, postPos.z) > 0.05f)
                            continue;
                        postPos.y = GeometryHelpers.SampleTerrainHeight(grid, postPos);
                        GameObject post = new GameObject("StreetFencePost");
                        post.transform.SetParent(root.transform);
                        post.transform.position = postPos;
                        post.transform.rotation = Quaternion.LookRotation(dir);
                        GameObjectBuilder.CreateCubeChild(post.transform, "Post", new Vector3(0f, 0.58f, 0f), new Vector3(0.12f, 1.16f, 0.12f), ctx.timberMat);
                        GameObjectBuilder.CreateCubeChild(post.transform, "RailA", new Vector3(0f, 0.74f, 0f), new Vector3(0.10f, 0.08f, spacing * 1.02f), ctx.timberMat);
                        GameObjectBuilder.CreateCubeChild(post.transform, "RailB", new Vector3(0f, 0.38f, 0f), new Vector3(0.10f, 0.08f, spacing * 1.02f), ctx.timberMat);
                    }
                }
            }
            MeshCombiner.CombineChildrenByMaterial(root.transform);
        }

    }
}
