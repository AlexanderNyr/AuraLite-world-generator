using UnityEngine;
using UnityEngine.Rendering;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Generates the lake, river, and riverside vegetation.
    /// </summary>
    public static class WaterGenerator
    {
        public static void CreateWaterSystem(BuildContext ctx, TerrainGrid grid, WorldLayout layout, GenerationSettings settings, Transform parent)
        {
            GameObject waterRoot = new GameObject("Water");
            waterRoot.transform.SetParent(parent);
            CreateLakeMesh(ctx, layout, waterRoot.transform);
            CreateRiverMesh(ctx, grid, layout, settings, waterRoot.transform);
        }

        private static void CreateLakeMesh(BuildContext ctx, WorldLayout layout, Transform parent)
        {
            GameObject lake = new GameObject("Lake", typeof(MeshFilter), typeof(MeshRenderer));
            lake.transform.SetParent(parent);
            lake.transform.position = new Vector3(layout.lakeCenter.x, layout.waterLevel, layout.lakeCenter.z);
            lake.GetComponent<MeshFilter>().sharedMesh = MeshFactory.CreateDiscMesh(48, 1f);
            lake.GetComponent<MeshRenderer>().sharedMaterial = ctx.waterMat;
            lake.transform.localScale = new Vector3(layout.lakeRadiusX * 2f, 1f, layout.lakeRadiusZ * 2f);
            lake.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void CreateRiverMesh(BuildContext ctx, TerrainGrid grid, WorldLayout layout, GenerationSettings settings, Transform parent)
        {
            GameObject riverRoot = new GameObject("River");
            riverRoot.transform.SetParent(parent);
            for (int i = 0; i < layout.riverPoints.Count - 1; i++)
            {
                Vector3 a = layout.riverPoints[i];
                Vector3 b = layout.riverPoints[i + 1];
                float len = Vector3.Distance(a, b);
                int pieces = Mathf.Max(3, Mathf.CeilToInt(len / Mathf.Lerp(16f, 8f, Mathf.InverseLerp(1f, 3f, settings.qualityBoost))));
                for (int p = 0; p < pieces; p++)
                {
                    float t0 = p / (float)pieces;
                    float t1 = (p + 1) / (float)pieces;
                    Vector3 p0 = Vector3.Lerp(a, b, t0);
                    Vector3 p1 = Vector3.Lerp(a, b, t1);
                    Vector3 mid = (p0 + p1) * 0.5f;
                    mid.y = layout.waterLevel + 0.03f;
                    float segLen = Vector3.Distance(p0, p1);
                    float wobble = 0.88f + Mathf.PerlinNoise(mid.x * 0.004f, mid.z * 0.004f) * 0.34f;
                    float width = layout.riverWidth * Mathf.Lerp(1.18f, 0.86f, i / Mathf.Max(1f, layout.riverPoints.Count - 2f)) * wobble;
                    GameObjectBuilder.CreateCubeChild(riverRoot.transform, "RiverSegment", mid, Quaternion.LookRotation((p1 - p0).normalized, Vector3.up), new Vector3(width, 0.05f, segLen + 0.25f), ctx.waterMat);
                }
            }
            MeshCombiner.CombineChildrenByMaterial(riverRoot.transform);
        }

        public static void CreateWaterVegetation(BuildContext ctx, TerrainGrid grid, WorldLayout layout, GenerationSettings settings, Transform parent)
        {
            GameObject root = new GameObject("WaterVegetation");
            root.transform.SetParent(parent);

            int reedCount = Mathf.RoundToInt(180f * settings.qualityBoost);
            for (int i = 0; i < reedCount; i++)
            {
                float angle = GeometryHelpers.Hash01(i, 1, layout.seed + 901) * Mathf.PI * 2f;
                float radius = Mathf.Lerp(layout.lakeRadiusX * 0.78f, layout.lakeRadiusX * 1.08f, GeometryHelpers.Hash01(i, 2, layout.seed + 911));
                Vector3 pos = layout.lakeCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius * (layout.lakeRadiusZ / layout.lakeRadiusX));
                float lakeMask = WorldLayoutGenerator.ComputeLakeMask(layout, pos.x, pos.z);
                if (lakeMask < 0.10f || lakeMask > 0.48f)
                    continue;
                pos.y = GeometryHelpers.SampleTerrainHeight(grid, pos);
                VegetationBuilder.CreateReedCluster(ctx, root.transform, pos, GeometryHelpers.Hash01(i, 3, layout.seed + 921), settings.qualityBoost);
            }

            float riverLengthApprox = 0f;
            for (int i = 0; i < layout.riverPoints.Count - 1; i++)
                riverLengthApprox += Vector3.Distance(layout.riverPoints[i], layout.riverPoints[i + 1]);

            for (float d = 12f; d < riverLengthApprox; d += Mathf.Lerp(18f, 10f, Mathf.InverseLerp(1f, 3f, settings.qualityBoost)))
            {
                Vector3 p = GeometryHelpers.SamplePolyline(layout.riverPoints, d);
                Vector3 dir = GeometryHelpers.DirectionOnPolyline(layout.riverPoints, d);
                Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 pos = p + side * (layout.riverWidth * (0.75f + GeometryHelpers.Hash01((int)d, s, layout.seed + 931) * 0.55f) * s);
                    if (WorldLayoutGenerator.ComputeRiverMask(layout, pos.x, pos.z) > 0.42f)
                        continue;
                    pos.y = GeometryHelpers.SampleTerrainHeight(grid, pos);
                    VegetationBuilder.CreateReedCluster(ctx, root.transform, pos, GeometryHelpers.Hash01((int)d, s, layout.seed + 941), settings.qualityBoost);
                }
            }

            GameObjectBuilder.MarkStaticRecursive(root);
        }

    }
}
