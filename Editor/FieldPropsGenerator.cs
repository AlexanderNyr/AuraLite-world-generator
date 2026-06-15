using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Generates field boundaries, crop rows, stone piles, and hay bales.
    /// </summary>
    public static class FieldPropsGenerator
    {
        public static void CreateFieldBoundaryProps(BuildContext ctx, TerrainGrid grid, WorldLayout layout, GenerationSettings settings, Transform parent)
        {
            GameObject hedges = new GameObject("Hedgerows");
            hedges.transform.SetParent(parent);
            GameObject crops = new GameObject("NearFieldRows");
            crops.transform.SetParent(parent);

            float cell = layout.farmlandCellSize;
            int radiusCells = Mathf.CeilToInt((1400f + settings.qualityBoost * 320f) / cell);
            int centerX = Mathf.FloorToInt(layout.villageCenter.x / cell);
            int centerZ = Mathf.FloorToInt(layout.villageCenter.z / cell);

            for (int cz = centerZ - radiusCells; cz <= centerZ + radiusCells; cz++)
            {
                for (int cx = centerX - radiusCells; cx <= centerX + radiusCells; cx++)
                {
                    if (cx < 0 || cz < 0)
                        continue;

                    float parcelCenterX = (cx + 0.5f) * cell;
                    float parcelCenterZ = (cz + 0.5f) * cell;
                    if (parcelCenterX > layout.worldSizeMeters || parcelCenterZ > layout.worldSizeMeters)
                        continue;
                    if (WorldLayoutGenerator.ComputeForestMask(layout, parcelCenterX, parcelCenterZ) > 0.18f)
                        continue;
                    if (WorldLayoutGenerator.ComputeVillageMask(layout, parcelCenterX, parcelCenterZ) > 0.26f || WorldLayoutGenerator.ComputeRoadMask(layout, parcelCenterX, parcelCenterZ) > 0.08f)
                        continue;

                    float hedgeThreshold = Mathf.Lerp(0.58f, 0.42f, Mathf.InverseLerp(1f, 3f, settings.qualityBoost));
                    bool hedgeX = GeometryHelpers.Hash01(cx, cz, layout.seed + 601) > hedgeThreshold;
                    bool hedgeZ = GeometryHelpers.Hash01(cx, cz, layout.seed + 611) > hedgeThreshold;

                    if (hedgeX)
                        CreateHedgeSegment(ctx, grid, hedges.transform, new Vector3(cx * cell, 0f, cz * cell), new Vector3(cx * cell, 0f, (cz + 1) * cell));
                    if (hedgeZ)
                        CreateHedgeSegment(ctx, grid, hedges.transform, new Vector3(cx * cell, 0f, cz * cell), new Vector3((cx + 1) * cell, 0f, cz * cell));

                    float border;
                    float wheat = WorldLayoutGenerator.ComputeWheatFieldMask(layout, parcelCenterX, parcelCenterZ, out border);
                    if (wheat > 0.45f && Vector2.Distance(new Vector2(parcelCenterX, parcelCenterZ), new Vector2(layout.villageCenter.x, layout.villageCenter.z)) < 900f && GeometryHelpers.Hash01(cx, cz, layout.seed + 621) > 0.52f)
                    {
                        CreateCropRows(ctx, grid, crops.transform, parcelCenterX, parcelCenterZ, cell * 0.78f, cell * 0.56f, GeometryHelpers.Hash01(cx, cz, layout.seed + 631) > 0.5f, settings.qualityBoost);
                    }
                }
            }

            MarkStaticRecursive(hedges);
            MarkStaticRecursive(crops);
        }

        private static void CreateHedgeSegment(BuildContext ctx, TerrainGrid grid, Transform parent, Vector3 a, Vector3 b)
        {
            Vector3 dir = (b - a).normalized;
            float len = Vector3.Distance(a, b);
            int segments = Mathf.Max(2, Mathf.CeilToInt(len / 12f));
            for (int i = 0; i < segments; i++)
            {
                float t0 = i / (float)segments;
                float t1 = (i + 1) / (float)segments;
                Vector3 p0 = Vector3.Lerp(a, b, t0);
                Vector3 p1 = Vector3.Lerp(a, b, t1);
                Vector3 mid = (p0 + p1) * 0.5f;
                float segLen = Vector3.Distance(p0, p1);
                mid.y = GeometryHelpers.SampleTerrainHeight(grid, mid) + 0.45f;
                Quaternion rot = Quaternion.LookRotation(dir);
                GameObjectBuilder.CreateCubeChild(parent, "Hedge", mid, rot, new Vector3(1.8f, 0.9f, segLen + 0.2f), ctx.leafMat);
                if (i % 2 == 0)
                {
                    Vector3 treePos = mid + Vector3.up * 0.1f;
                    GameObjectBuilder.CreateCylinderChild(parent, "HedgeTrunk", new Vector3(treePos.x, GeometryHelpers.SampleTerrainHeight(grid, treePos) + 0.65f, treePos.z), new Vector3(0.10f, 0.65f, 0.10f), ctx.barkMat);
                }
            }
        }

        private static void CreateCropRows(BuildContext ctx, TerrainGrid grid, Transform parent, float centerX, float centerZ, float width, float depth, bool alongX, float qualityBoost)
        {
            GameObject rows = new GameObject($"CropRows_{Mathf.RoundToInt(centerX)}_{Mathf.RoundToInt(centerZ)}");
            rows.transform.SetParent(parent);
            rows.transform.position = new Vector3(centerX, 0f, centerZ);
            rows.transform.rotation = Quaternion.Euler(0f, alongX ? 0f : 90f, 0f);

            int rowCount = Mathf.Clamp(Mathf.RoundToInt(width / Mathf.Lerp(4.6f, 2.7f, Mathf.InverseLerp(1f, 3f, qualityBoost))), 10, 30);
            int strips = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(3f, 6f, Mathf.InverseLerp(1f, 3f, qualityBoost))), 3, 6);
            float rowSpacing = width / rowCount;
            for (int i = 0; i < rowCount; i++)
            {
                float offset = -width * 0.5f + rowSpacing * (i + 0.5f);
                for (int s = 0; s < strips; s++)
                {
                    float z = -depth * 0.5f + depth * (s + 0.5f) / strips;
                    Vector3 local = new Vector3(offset, 0f, z);
                    Vector3 world = rows.transform.TransformPoint(local);
                    world.y = GeometryHelpers.SampleTerrainHeight(grid, world) + 0.30f;
                    local = rows.transform.InverseTransformPoint(world);
                    GameObjectBuilder.CreateCubeChild(rows.transform, "Row", local, new Vector3(rowSpacing * 0.50f, 0.65f + 0.03f * s, depth / strips * 0.70f), ctx.wheatBaleMat);
                }
            }
        }

        public static void CreateFieldStonePiles(BuildContext ctx, TerrainGrid grid, WorldLayout layout, GenerationSettings settings, Transform parent)
        {
            GameObject root = new GameObject("FieldStonePiles");
            root.transform.SetParent(parent);
            float cell = layout.farmlandCellSize;
            int count = Mathf.RoundToInt(14f * settings.qualityBoost);
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = new Vector3(Random.Range(layout.villageCenter.x - 1200f, layout.villageCenter.x + 1200f), 0f, Random.Range(layout.villageCenter.z - 1200f, layout.villageCenter.z + 1200f));
                if (WorldLayoutGenerator.ComputeVillageMask(layout, pos.x, pos.z) > 0.18f || WorldLayoutGenerator.ComputeRoadMask(layout, pos.x, pos.z) > 0.10f || WorldLayoutGenerator.ComputeLakeMask(layout, pos.x, pos.z) > 0.06f || WorldLayoutGenerator.ComputeRiverMask(layout, pos.x, pos.z) > 0.08f)
                    continue;
                pos.y = GeometryHelpers.SampleTerrainHeight(grid, pos);
                GameObject pile = new GameObject("Pile_" + i);
                pile.transform.SetParent(root.transform);
                pile.transform.position = pos;
                int stones = Random.Range(4, 8);
                for (int s = 0; s < stones; s++)
                {
                    GameObjectBuilder.CreateSphereChild(pile.transform, "Stone_" + s, new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(0.1f, 0.5f), Random.Range(-1.0f, 1.0f)), Vector3.one * Random.Range(0.35f, 0.9f), ctx.stoneMat);
                }
            }
            MarkStaticRecursive(root);
        }

        public static void CreateFieldProps(BuildContext ctx, TerrainGrid grid, WorldLayout layout, GenerationSettings settings, Transform parent)
        {
            GameObject hayRoot = new GameObject("HayBales");
            hayRoot.transform.SetParent(parent);

            float cell = layout.farmlandCellSize;
            float fieldRadius = 900f + settings.qualityBoost * 300f;
            int minX = Mathf.Max(0, Mathf.FloorToInt((layout.villageCenter.x - fieldRadius) / cell) - 1);
            int maxX = Mathf.Min(Mathf.CeilToInt(layout.worldSizeMeters / cell), Mathf.CeilToInt((layout.villageCenter.x + fieldRadius) / cell) + 1);
            int minZ = Mathf.Max(0, Mathf.FloorToInt((layout.villageCenter.z - fieldRadius) / cell) - 1);
            int maxZ = Mathf.Min(Mathf.CeilToInt(layout.worldSizeMeters / cell), Mathf.CeilToInt((layout.villageCenter.z + fieldRadius) / cell) + 1);

            for (int cz = minZ; cz <= maxZ; cz++)
            {
                for (int cx = minX; cx <= maxX; cx++)
                {
                    float centerX = (cx + 0.5f) * cell;
                    float centerZ = (cz + 0.5f) * cell;
                    if (WorldLayoutGenerator.ComputeForestMask(layout, centerX, centerZ) > 0.2f)
                        continue;
                    if (WorldLayoutGenerator.ComputeVillageMask(layout, centerX, centerZ) > 0.18f)
                        continue;

                    float border;
                    float wheat = WorldLayoutGenerator.ComputeWheatFieldMask(layout, centerX, centerZ, out border);
                    if (wheat < 0.5f)
                        continue;
                    if (GeometryHelpers.Hash01(cx, cz, layout.seed + 99) > Mathf.Lerp(0.42f, 0.22f, Mathf.InverseLerp(1f, 3f, settings.qualityBoost)))
                        continue;

                    Vector3 pos = new Vector3(centerX + (GeometryHelpers.Hash01(cx, cz, layout.seed + 11) - 0.5f) * cell * 0.32f, 0f, centerZ + (GeometryHelpers.Hash01(cx, cz, layout.seed + 21) - 0.5f) * cell * 0.28f);
                    pos.y = GeometryHelpers.SampleTerrainHeight(grid, pos);

                    GameObject cluster = new GameObject($"HayCluster_{cx}_{cz}");
                    cluster.transform.SetParent(hayRoot.transform);
                    cluster.transform.position = pos;
                    cluster.transform.rotation = Quaternion.Euler(0f, GeometryHelpers.Hash01(cx, cz, layout.seed + 31) * 360f, 0f);

                    GameObjectBuilder.CreateCylinderChild(cluster.transform, "BaleA", new Vector3(0f, 0.42f, 0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.62f, 0.34f, 0.62f), ctx.wheatBaleMat);
                    if (GeometryHelpers.Hash01(cx, cz, layout.seed + 41) > 0.4f)
                        GameObjectBuilder.CreateCylinderChild(cluster.transform, "BaleB", new Vector3(0.76f, 0.42f, 0.18f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.58f, 0.32f, 0.58f), ctx.wheatBaleMat);
                    if (GeometryHelpers.Hash01(cx, cz, layout.seed + 51) > 0.65f)
                        GameObjectBuilder.CreateCylinderChild(cluster.transform, "BaleC", new Vector3(-0.55f, 0.42f, -0.22f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.52f, 0.30f, 0.52f), ctx.wheatBaleMat);
                    if (settings.qualityBoost > 1.4f && GeometryHelpers.Hash01(cx, cz, layout.seed + 61) > 0.58f)
                        GameObjectBuilder.CreateCylinderChild(cluster.transform, "BaleD", new Vector3(0.22f, 0.78f, -0.12f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.50f, 0.28f, 0.50f), ctx.wheatBaleMat);
                }
            }

            MarkStaticRecursive(hayRoot);
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
