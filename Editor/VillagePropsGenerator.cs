using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Generates village greenery, street props, and lake shore props.
    /// </summary>
    public static class VillagePropsGenerator
    {
        public static void CreateVillageGreenery(BuildContext ctx, TerrainGrid grid, WorldLayout layout, GenerationSettings settings, Transform parent)
        {
            GameObject root = new GameObject("VillageGreenery");
            root.transform.SetParent(parent);

            int treeCount = Mathf.RoundToInt(28f * settings.qualityBoost);
            for (int i = 0; i < treeCount; i++)
            {
                Vector3 pos = layout.villageCenter + new Vector3(Random.Range(-layout.villageLengthMeters * 0.50f, layout.villageLengthMeters * 0.50f), 0f, Random.Range(-layout.villageHalfWidthMeters * 1.10f, layout.villageHalfWidthMeters * 1.10f));
                if (WorldLayoutGenerator.ComputeHouseMask(layout, pos.x, pos.z) > 0.08f || WorldLayoutGenerator.ComputeRoadMask(layout, pos.x, pos.z) > 0.10f || WorldLayoutGenerator.ComputeRiverMask(layout, pos.x, pos.z) > 0.06f || WorldLayoutGenerator.ComputeLakeMask(layout, pos.x, pos.z) > 0.04f)
                    continue;
                pos.y = GeometryHelpers.SampleTerrainHeight(grid, pos);
                GameObject tree = new GameObject("VillageTree_" + i);
                tree.transform.SetParent(root.transform);
                tree.transform.position = pos;
                tree.transform.localScale = Vector3.one * Random.Range(0.58f, 0.92f);
                VegetationBuilder.BuildBroadleafTree(ctx, tree.transform);
            }

            int orchardCount = Mathf.RoundToInt(12f * settings.qualityBoost);
            for (int i = 0; i < orchardCount; i++)
            {
                Vector3 pos = layout.villageCenter + new Vector3(Random.Range(-layout.villageLengthMeters * 0.42f, layout.villageLengthMeters * 0.42f), 0f, Random.Range(-layout.villageHalfWidthMeters * 0.92f, layout.villageHalfWidthMeters * 0.92f));
                if (WorldLayoutGenerator.ComputeHouseMask(layout, pos.x, pos.z) > 0.08f || WorldLayoutGenerator.ComputeRoadMask(layout, pos.x, pos.z) > 0.10f || WorldLayoutGenerator.ComputeRiverMask(layout, pos.x, pos.z) > 0.06f)
                    continue;
                pos.y = GeometryHelpers.SampleTerrainHeight(grid, pos);
                GameObject orchard = new GameObject("OrchardCluster_" + i);
                orchard.transform.SetParent(root.transform);
                orchard.transform.position = pos;
                for (int t = 0; t < 3; t++)
                {
                    GameObject tree = new GameObject("Tree_" + t);
                    tree.transform.SetParent(orchard.transform, false);
                    tree.transform.localPosition = new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(-6f, 6f));
                    tree.transform.localScale = Vector3.one * Random.Range(0.48f, 0.68f);
                    VegetationBuilder.BuildBroadleafTree(ctx, tree.transform);
                }
            }

            MarkStaticRecursive(root);
        }

        public static void CreateRoadsideProps(BuildContext ctx, TerrainGrid grid, WorldLayout layout, GenerationSettings settings, Transform parent)
        {
            GameObject polesRoot = new GameObject("TelegraphPoles");
            polesRoot.transform.SetParent(parent);
            GameObject treesRoot = new GameObject("RoadsideTrees");
            treesRoot.transform.SetParent(parent);

            for (int r = 0; r < layout.roads.Count; r++)
            {
                RoadPath road = layout.roads[r];
                float length = GeometryHelpers.GetPathLength(road);
                if (length <= 0f)
                    continue;

                float spacing = road.mainRoad ? Mathf.Lerp(44f, 26f, Mathf.InverseLerp(1f, 3f, settings.qualityBoost)) : (road.name.StartsWith("Lane") ? 30f : 42f);
                float sideOffset = road.width * 0.72f + (road.mainRoad ? 3.4f : 2.2f);

                for (float d = spacing; d < length - 10f; d += spacing)
                {
                    Vector3 p = GeometryHelpers.SamplePath(road, d);
                    Vector3 dir = GeometryHelpers.DirectionOnPath(road, d);
                    Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
                    bool left = GeometryHelpers.Hash01(r, Mathf.RoundToInt(d), layout.seed + 501) > 0.5f;
                    Vector3 place = p + side * sideOffset * (left ? 1f : -1f);
                    place.y = GeometryHelpers.SampleTerrainHeight(grid, place);

                    GameObject pole = new GameObject($"Pole_{r}_{Mathf.RoundToInt(d)}");
                    pole.transform.SetParent(polesRoot.transform);
                    pole.transform.position = place;
                    pole.transform.rotation = Quaternion.LookRotation(dir);
                    GameObjectBuilder.CreateCylinderChild(pole.transform, "Post", new Vector3(0f, 3.2f, 0f), new Vector3(0.12f, 3.2f, 0.12f), ctx.timberMat);
                    GameObjectBuilder.CreateCubeChild(pole.transform, "Cross", new Vector3(0f, 6.0f, 0f), new Vector3(1.1f, 0.08f, 0.08f), ctx.timberMat);

                    if (road.name.StartsWith("Lane") && GeometryHelpers.Hash01(r, Mathf.RoundToInt(d), layout.seed + 551) > 0.62f)
                    {
                        Vector3 treePos = p - side * sideOffset * 1.55f;
                        treePos.y = GeometryHelpers.SampleTerrainHeight(grid, treePos);
                        GameObject tree = new GameObject($"RoadTree_{r}_{Mathf.RoundToInt(d)}");
                        tree.transform.SetParent(treesRoot.transform);
                        tree.transform.position = treePos;
                        tree.transform.localScale = Vector3.one * Random.Range(0.68f, 0.92f);
                        VegetationBuilder.BuildBroadleafTree(ctx, tree.transform);
                    }

                    if (road.mainRoad && GeometryHelpers.Hash01(r, Mathf.RoundToInt(d), layout.seed + 561) > 0.82f)
                    {
                        GameObject sign = new GameObject($"Sign_{r}_{Mathf.RoundToInt(d)}");
                        sign.transform.SetParent(polesRoot.transform);
                        sign.transform.position = place + dir * 2.2f;
                        sign.transform.rotation = Quaternion.LookRotation(dir);
                        GameObjectBuilder.CreateCubeChild(sign.transform, "Post", new Vector3(0f, 0.8f, 0f), new Vector3(0.10f, 1.6f, 0.10f), ctx.timberMat);
                        GameObjectBuilder.CreateCubeChild(sign.transform, "Board", new Vector3(0.35f, 1.55f, 0f), new Vector3(0.70f, 0.34f, 0.08f), ctx.wallCreamMat);
                    }
                }
            }

            MarkStaticRecursive(polesRoot);
            MarkStaticRecursive(treesRoot);
        }

        public static void CreateVillageStreetProps(BuildContext ctx, TerrainGrid grid, WorldLayout layout, GenerationSettings settings, Transform parent)
        {
            GameObject root = new GameObject("StreetProps");
            root.transform.SetParent(parent);

            Vector3 commons = layout.villageCenter + new Vector3(layout.villageLengthMeters * 0.04f, 0f, 0f);
            commons.y = GeometryHelpers.SampleTerrainHeight(grid, commons);
            GameObjectBuilder.CreateCylinderChild(root.transform, "WellRing", commons + new Vector3(0f, 0.42f, 0f), new Vector3(2.2f, 0.42f, 2.2f), ctx.stoneMat);
            GameObjectBuilder.CreateCubeChild(root.transform, "WellPostL", commons + new Vector3(-1.1f, 2.0f, 0f), new Vector3(0.16f, 2.8f, 0.16f), ctx.timberMat);
            GameObjectBuilder.CreateCubeChild(root.transform, "WellPostR", commons + new Vector3(1.1f, 2.0f, 0f), new Vector3(0.16f, 2.8f, 0.16f), ctx.timberMat);
            GameObjectBuilder.CreateMeshChild(root.transform, "WellRoof", ctx.roofMesh, commons + new Vector3(0f, 3.1f, 0f), new Vector3(2.8f, 1.2f, 2.2f), ctx.roofDarkMat);

            int benchCount = Mathf.RoundToInt(Mathf.Lerp(4f, 8f, Mathf.InverseLerp(1f, 3f, settings.qualityBoost)));
            for (int i = 0; i < benchCount; i++)
            {
                float ang = i / (float)benchCount * Mathf.PI * 2f;
                Vector3 p = layout.villageCenter + new Vector3(Mathf.Cos(ang) * 18f, 0f, Mathf.Sin(ang) * 14f);
                p.y = GeometryHelpers.SampleTerrainHeight(grid, p);
                Quaternion benchRot = Quaternion.Euler(0f, ang * Mathf.Rad2Deg + 90f, 0f);
                GameObjectBuilder.CreateCubeChild(root.transform, "BenchSeat_" + i, p + new Vector3(0f, 0.42f, 0f), benchRot, new Vector3(1.7f, 0.14f, 0.42f), ctx.timberMat);
                GameObjectBuilder.CreateCubeChild(root.transform, "BenchLegA_" + i, p + new Vector3(-0.60f, 0.22f, 0f), benchRot, new Vector3(0.12f, 0.44f, 0.12f), ctx.timberMat);
                GameObjectBuilder.CreateCubeChild(root.transform, "BenchLegB_" + i, p + new Vector3(0.60f, 0.22f, 0f), benchRot, new Vector3(0.12f, 0.44f, 0.12f), ctx.timberMat);
            }

            int cartCount = Mathf.RoundToInt(Mathf.Lerp(3f, 6f, Mathf.InverseLerp(1f, 3f, settings.qualityBoost)));
            for (int i = 0; i < cartCount; i++)
            {
                Vector3 p = layout.villageCenter + new Vector3(-10f + i * 9f, 0f, -18f + i * 3f);
                p.y = GeometryHelpers.SampleTerrainHeight(grid, p);
                GameObjectBuilder.CreateCubeChild(root.transform, "CartBase_" + i, p + new Vector3(0f, 0.48f, 0f), Quaternion.Euler(0f, 20f + i * 40f, 0f), new Vector3(2.0f, 0.38f, 1.2f), ctx.timberMat);
                GameObjectBuilder.CreateCylinderChild(root.transform, "WheelA_" + i, p + new Vector3(0.9f, 0.34f, 0.62f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.36f, 0.12f, 0.36f), ctx.stoneMat);
                GameObjectBuilder.CreateCylinderChild(root.transform, "WheelB_" + i, p + new Vector3(-0.9f, 0.34f, 0.62f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.36f, 0.12f, 0.36f), ctx.stoneMat);
            }

            int lampCount = Mathf.RoundToInt(Mathf.Lerp(4f, 8f, Mathf.InverseLerp(1f, 3f, settings.qualityBoost)));
            for (int i = 0; i < lampCount; i++)
            {
                float angLamp = i / Mathf.Max(1f, lampCount) * Mathf.PI * 2f;
                Vector3 p = layout.villageCenter + new Vector3(Mathf.Cos(angLamp) * 24f, 0f, Mathf.Sin(angLamp) * 20f);
                p.y = GeometryHelpers.SampleTerrainHeight(grid, p);
                GameObjectBuilder.CreateCylinderChild(root.transform, "LampPost_" + i, p + new Vector3(0f, 1.8f, 0f), new Vector3(0.11f, 1.8f, 0.11f), ctx.timberMat);
                GameObjectBuilder.CreateSphereChild(root.transform, "LampGlow_" + i, p + new Vector3(0f, 3.4f, 0f), Vector3.one * 0.28f, ctx.glassMat);
            }
            MarkStaticRecursive(root);
        }

        public static void CreateLakeShoreProps(BuildContext ctx, TerrainGrid grid, WorldLayout layout, GenerationSettings settings, Transform parent)
        {
            GameObject root = new GameObject("LakeShoreProps");
            root.transform.SetParent(parent);

            Vector3 dockPos = layout.lakeCenter + new Vector3(layout.lakeRadiusX * 0.24f, 0f, layout.lakeRadiusZ * 0.82f);
            dockPos.y = layout.waterLevel + 0.35f;
            GameObject dock = new GameObject("Dock");
            dock.transform.SetParent(root.transform);
            dock.transform.position = dockPos;
            dock.transform.rotation = Quaternion.Euler(0f, 18f, 0f);
            int dockSections = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(5f, 9f, Mathf.InverseLerp(1f, 3f, settings.qualityBoost))), 5, 9);
            for (int i = 0; i < dockSections; i++)
            {
                float z = i * 2.1f;
                GameObjectBuilder.CreateCubeChild(dock.transform, "Deck_" + i, new Vector3(0f, 0f, z), new Vector3(2.4f, 0.16f, 1.8f), ctx.timberMat);
                GameObjectBuilder.CreateCubeChild(dock.transform, "PostL_" + i, new Vector3(-0.9f, -0.65f, z), new Vector3(0.16f, 1.5f, 0.16f), ctx.timberMat);
                GameObjectBuilder.CreateCubeChild(dock.transform, "PostR_" + i, new Vector3(0.9f, -0.65f, z), new Vector3(0.16f, 1.5f, 0.16f), ctx.timberMat);
            }
            GameObjectBuilder.CreateCubeChild(dock.transform, "Boat", new Vector3(2.6f, -0.08f, 6.8f), Quaternion.Euler(0f, 28f, 0f), new Vector3(1.2f, 0.26f, 3.4f), ctx.timberMat);

            int rockCount = Mathf.RoundToInt(Mathf.Lerp(20f, 36f, Mathf.InverseLerp(1f, 3f, settings.qualityBoost)));
            for (int i = 0; i < rockCount; i++)
            {
                float a = i / (float)rockCount * Mathf.PI * 2f;
                Vector3 p = layout.lakeCenter + new Vector3(Mathf.Cos(a) * layout.lakeRadiusX * 1.04f, 0f, Mathf.Sin(a) * layout.lakeRadiusZ * 1.02f);
                p.y = GeometryHelpers.SampleTerrainHeight(grid, p) + 0.2f;
                GameObjectBuilder.CreateSphereChild(root.transform, "Rock_" + i, p, Vector3.one * Random.Range(0.8f, 1.8f), ctx.stoneMat);
            }
            MarkStaticRecursive(root);
        }

        private static void MarkStaticRecursive(GameObject root)
        {
            GameObjectUtility.SetStaticEditorFlags(root,
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.ContributeGI |
                StaticEditorFlags.OccluderStatic |
                StaticEditorFlags.OccludeeStatic |
                StaticEditorFlags.ReflectionProbeStatic);

            for (int i = 0; i < root.transform.childCount; i++)
                MarkStaticRecursive(root.transform.GetChild(i).gameObject);
        }
    }
}
