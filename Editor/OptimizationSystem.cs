using AuraLiteWorldGenerator.Runtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Builds HLOD proxies and distance streaming chunks for the generated world.
    /// </summary>
    public static class OptimizationSystem
    {
        public static void CreateWorldOptimizationSystem(BuildContext ctx, WorldLayout layout, GenerationSettings settings, GameObject worldRoot, GameObject roadsRoot, GameObject villageRoot, GameObject fieldsRoot, GameObject forestRoot)
        {
            GameObject optimizationRoot = new GameObject("Optimization");
            optimizationRoot.transform.SetParent(worldRoot.transform);

            GameObject hlodRoot = new GameObject("HLOD");
            hlodRoot.transform.SetParent(optimizationRoot.transform);

            GameObject villageProxy = CreateVillageHLODProxy(ctx, layout, settings, hlodRoot.transform);
            GameObject forestProxy = CreateForestHLODProxy(ctx, layout, settings, hlodRoot.transform);
            GameObject roadsProxy = CreateRoadHLODProxy(ctx, layout, hlodRoot.transform);
            GameObject fieldProxy = CreateFieldHLODProxy(ctx, layout, settings, hlodRoot.transform);

            List<GameObject> roadChunks = ChunkizeChildren(roadsRoot, 700f, "RoadChunk");
            List<GameObject> fieldChunks = ChunkizeChildren(fieldsRoot, 900f, "FieldChunk");
            List<GameObject> forestChunks = ChunkizeForestChildren(forestRoot, 1200f, "ForestChunk");

            GameObject streamingGO = new GameObject("Distance Streaming");
            streamingGO.transform.SetParent(optimizationRoot.transform);
            DistanceChunkActivator activator = streamingGO.AddComponent<DistanceChunkActivator>();
            activator.updateInterval = 0.25f;
            activator.drawGizmos = false;

            float villageSwitch = 1800f + settings.qualityBoost * 180f;
            float roadsSwitch = 2400f + settings.qualityBoost * 220f;
            float fieldSwitch = 2700f + settings.qualityBoost * 240f;
            float forestSwitch = 3400f + settings.qualityBoost * 320f;

            AddChunkRule(activator, "Village_HLOD", villageRoot, villageProxy, villageSwitch, 220f);
            AddChunkRule(activator, "Forest_HLOD", forestRoot, forestProxy, forestSwitch, 260f);
            AddChunkRule(activator, "Road_HLOD", roadsRoot, roadsProxy, roadsSwitch, 220f);
            AddChunkRule(activator, "Field_HLOD", fieldsRoot, fieldProxy, fieldSwitch, 220f);

            for (int i = 0; i < roadChunks.Count; i++)
                AddChunkRule(activator, "RoadChunk_" + i, roadChunks[i], null, roadsSwitch + 180f, 140f);
            for (int i = 0; i < fieldChunks.Count; i++)
                AddChunkRule(activator, "FieldChunk_" + i, fieldChunks[i], null, fieldSwitch + 220f, 160f);
            for (int i = 0; i < forestChunks.Count; i++)
                AddChunkRule(activator, "ForestChunk_" + i, forestChunks[i], null, forestSwitch + 320f, 220f);

            activator.RebuildBounds();
            activator.ApplyImmediate();
        }

        private static void AddChunkRule(DistanceChunkActivator activator, string name, GameObject nearRoot, GameObject farRoot, float distance, float hysteresis)
        {
            DistanceChunkActivator.ChunkRule rule = new DistanceChunkActivator.ChunkRule
            {
                name = name,
                nearRoot = nearRoot,
                farRoot = farRoot,
                switchDistance = distance,
                hysteresis = hysteresis
            };
            activator.chunks.Add(rule);
            if (farRoot != null)
                farRoot.SetActive(false);
        }

        private static GameObject CreateVillageHLODProxy(BuildContext ctx, WorldLayout layout, GenerationSettings settings, Transform parent)
        {
            GameObject root = new GameObject("VillageProxy");
            root.transform.SetParent(parent);
            for (int i = 0; i < layout.houses.Count; i++)
            {
                HouseSpec spec = layout.houses[i];
                GameObject proxy = new GameObject("B_" + i);
                proxy.transform.SetParent(root.transform);
                proxy.transform.position = spec.position;
                proxy.transform.rotation = Quaternion.Euler(0f, spec.yaw, 0f);
                float w = spec.footprint.x;
                float d = spec.footprint.y;
                float h = spec.height;
                Material wall = settings.villageStyle == VillageStyle.Russian ? ctx.logWallMat : ctx.wallCreamMat;
                Material roof = (spec.kind == BuildingKind.Chapel && settings.villageStyle == VillageStyle.Russian) ? ctx.copperRoofMat : ctx.roofDarkMat;
                GameObjectBuilder.CreateCubeChild(proxy.transform, "Body", new Vector3(0f, h * 0.46f, 0f), new Vector3(w * 0.94f, h * 0.88f, d * 0.94f), wall);
                GameObjectBuilder.CreateMeshChild(proxy.transform, "Roof", ctx.roofMesh, new Vector3(0f, h * 0.88f, 0f), new Vector3(w, Mathf.Lerp(1.2f, 2.6f, h / 10f), d), roof);
                if (spec.kind == BuildingKind.Chapel || spec.kind == BuildingKind.Mill)
                    GameObjectBuilder.CreateCubeChild(proxy.transform, "Tower", new Vector3(0f, h * 0.95f, 0f), new Vector3(w * 0.18f, h * 0.9f, d * 0.18f), wall);
            }
            GameObjectBuilder.SetShadowsRecursive(root, ShadowCastingMode.Off, false);
            return root;
        }

        private static GameObject CreateForestHLODProxy(BuildContext ctx, WorldLayout layout, GenerationSettings settings, Transform parent)
        {
            GameObject root = new GameObject("ForestProxy");
            root.transform.SetParent(parent);
            float bandStart = layout.forestStartZ - 120f;
            int rows = Mathf.RoundToInt(Mathf.Lerp(8f, 14f, Mathf.InverseLerp(1f, 3f, settings.qualityBoost)));
            int cols = Mathf.RoundToInt(Mathf.Lerp(24f, 42f, Mathf.InverseLerp(1f, 3f, settings.qualityBoost)));
            for (int z = 0; z < rows; z++)
            {
                for (int x = 0; x < cols; x++)
                {
                    float px = Mathf.Lerp(120f, layout.worldSizeMeters - 120f, x / Mathf.Max(1f, cols - 1f));
                    float pz = bandStart + z * (layout.worldSizeMeters - bandStart - 120f) / Mathf.Max(1f, rows - 1f);
                    GameObject card = new GameObject("F_" + x + "_" + z, typeof(MeshFilter), typeof(MeshRenderer));
                    card.transform.SetParent(root.transform);
                    card.transform.position = new Vector3(px, 34f + z * 1.8f, pz);
                    card.transform.localScale = new Vector3(90f, 70f, 1f);
                    card.GetComponent<MeshFilter>().sharedMesh = ctx.quadMesh;
                    bool pine = (x + z) % 3 != 0;
                    MeshRenderer mr = card.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = pine ? ctx.pineMat : ctx.leafMat;
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                    CameraFacingBillboard bb = card.AddComponent<CameraFacingBillboard>();
                    bb.yOnly = true;
                    bb.yawOffset = 180f;
                }
            }
            return root;
        }

        private static GameObject CreateRoadHLODProxy(BuildContext ctx, WorldLayout layout, Transform parent)
        {
            GameObject root = new GameObject("RoadProxy");
            root.transform.SetParent(parent);
            for (int r = 0; r < layout.roads.Count; r++)
            {
                RoadPath road = layout.roads[r];
                for (int i = 0; i < road.points.Count - 1; i++)
                {
                    Vector3 a = road.points[i];
                    Vector3 b = road.points[i + 1];
                    Vector3 mid = (a + b) * 0.5f;
                    float len = Vector3.Distance(a, b);
                    GameObjectBuilder.CreateCubeChild(root.transform, "RoadProxy_" + r + "_" + i, mid + Vector3.up * 0.05f, Quaternion.LookRotation((b - a).normalized), new Vector3(road.width * 0.8f, 0.06f, len), ctx.dirtMat);
                }
            }
            GameObjectBuilder.SetShadowsRecursive(root, ShadowCastingMode.Off, false);
            return root;
        }

        private static GameObject CreateFieldHLODProxy(BuildContext ctx, WorldLayout layout, GenerationSettings settings, Transform parent)
        {
            GameObject root = new GameObject("FieldProxy");
            root.transform.SetParent(parent);
            float cell = layout.farmlandCellSize;
            int radius = Mathf.RoundToInt(Mathf.Lerp(6f, 10f, Mathf.InverseLerp(1f, 3f, settings.qualityBoost)));
            int centerX = Mathf.FloorToInt(layout.villageCenter.x / cell);
            int centerZ = Mathf.FloorToInt(layout.villageCenter.z / cell);
            for (int z = centerZ - radius; z <= centerZ + radius; z++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (x < 0 || z < 0) continue;
                    float px = (x + 0.5f) * cell;
                    float pz = (z + 0.5f) * cell;
                    if (px > layout.worldSizeMeters || pz > layout.worldSizeMeters) continue;
                    if (WorldLayoutGenerator.ComputeVillageMask(layout, px, pz) > 0.16f || WorldLayoutGenerator.ComputeForestMask(layout, px, pz) > 0.16f) continue;

                    float border;
                    float wheat = WorldLayoutGenerator.ComputeWheatFieldMask(layout, px, pz, out border);
                    GameObject card = new GameObject("FieldCard_" + x + "_" + z, typeof(MeshFilter), typeof(MeshRenderer));
                    card.transform.SetParent(root.transform);
                    card.transform.position = new Vector3(px, 14f, pz);
                    card.transform.localScale = new Vector3(cell * 0.88f, cell * 0.24f, 1f);
                    card.GetComponent<MeshFilter>().sharedMesh = ctx.quadMesh;
                    MeshRenderer mr = card.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = wheat > 0.45f ? ctx.wheatBladeMat : ctx.grassBladeMat;
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                    CameraFacingBillboard bb = card.AddComponent<CameraFacingBillboard>();
                    bb.yOnly = true;
                    bb.yawOffset = 180f;
                }
            }
            GameObjectBuilder.SetShadowsRecursive(root, ShadowCastingMode.Off, false);
            return root;
        }

        private static List<GameObject> ChunkizeForestChildren(GameObject sourceRoot, float chunkSize, string prefix)
        {
            List<GameObject> result = new List<GameObject>();
            Dictionary<string, GameObject> buckets = new Dictionary<string, GameObject>();
            LODGroup[] lodGroups = sourceRoot.GetComponentsInChildren<LODGroup>(true);

            for (int i = 0; i < lodGroups.Length; i++)
            {
                Transform child = lodGroups[i].transform;
                if (child == sourceRoot.transform)
                    continue;

                Bounds b = GeometryHelpers.CalculateHierarchyBounds(child.gameObject);
                Vector3 c = b.center;
                int cx = Mathf.FloorToInt(c.x / chunkSize);
                int cz = Mathf.FloorToInt(c.z / chunkSize);
                string key = cx + "_" + cz;
                if (!buckets.TryGetValue(key, out GameObject bucket))
                {
                    bucket = new GameObject(prefix + "_" + key);
                    bucket.transform.SetParent(sourceRoot.transform);
                    buckets[key] = bucket;
                    result.Add(bucket);
                }
                child.SetParent(bucket.transform, true);
            }
            return result;
        }

        private static List<GameObject> ChunkizeChildren(GameObject sourceRoot, float chunkSize, string prefix)
        {
            List<GameObject> result = new List<GameObject>();
            Dictionary<string, GameObject> buckets = new Dictionary<string, GameObject>();
            List<Transform> children = new List<Transform>();
            for (int i = 0; i < sourceRoot.transform.childCount; i++)
                children.Add(sourceRoot.transform.GetChild(i));

            for (int i = 0; i < children.Count; i++)
            {
                Transform child = children[i];
                Bounds b = GeometryHelpers.CalculateHierarchyBounds(child.gameObject);
                Vector3 c = b.center;
                int cx = Mathf.FloorToInt(c.x / chunkSize);
                int cz = Mathf.FloorToInt(c.z / chunkSize);
                string key = cx + "_" + cz;
                if (!buckets.TryGetValue(key, out GameObject bucket))
                {
                    bucket = new GameObject(prefix + "_" + key);
                    bucket.transform.SetParent(sourceRoot.transform);
                    buckets[key] = bucket;
                    result.Add(bucket);
                }
                child.SetParent(bucket.transform, true);
            }
            return result;
        }
    }
}
