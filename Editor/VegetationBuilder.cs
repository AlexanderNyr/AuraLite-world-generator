#pragma warning disable CS0618 // 'BuildContext' is obsolete: 'Use GenerationContext and AssetRegistry instead.'
using UnityEngine;
using UnityEngine.Rendering;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Builds vegetation elements (broadleaf trees, pine trees, shrubs, reeds) and their LODs.
    /// </summary>
    public static class VegetationBuilder
    {
        public static void BuildPineTree(BuildContext ctx, Transform root)
        {
            GameObjectBuilder.CreateCylinderChild(root, "Trunk", new Vector3(0f, 2.3f, 0f), new Vector3(0.23f, 2.3f, 0.23f), ctx.barkMat);
            GameObjectBuilder.CreateMeshChild(root, "ConeA", ctx.coneMesh, new Vector3(0f, 2.7f, 0f), new Vector3(2.2f, 2.5f, 2.2f), ctx.pineMat);
            GameObjectBuilder.CreateMeshChild(root, "ConeB", ctx.coneMesh, new Vector3(0f, 3.9f, 0f), new Vector3(1.8f, 2.1f, 1.8f), ctx.pineMat);
            GameObjectBuilder.CreateMeshChild(root, "ConeC", ctx.coneMesh, new Vector3(0f, 5.0f, 0f), new Vector3(1.2f, 1.6f, 1.2f), ctx.pineMat);
            SetupTreeLOD(ctx, root.gameObject, true);
        }

        public static void BuildBroadleafTree(BuildContext ctx, Transform root)
        {
            GameObjectBuilder.CreateCylinderChild(root, "Trunk", new Vector3(0f, 2.5f, 0f), new Vector3(0.28f, 2.5f, 0.28f), ctx.barkMat);
            GameObjectBuilder.CreateSphereChild(root, "Leaf1", new Vector3(-0.42f, 4.3f, 0f), new Vector3(2.1f, 2.0f, 2.0f), ctx.leafMat);
            GameObjectBuilder.CreateSphereChild(root, "Leaf2", new Vector3(0.52f, 4.55f, 0.18f), new Vector3(2.0f, 1.9f, 2.0f), ctx.leafMat);
            GameObjectBuilder.CreateSphereChild(root, "Leaf3", new Vector3(0f, 5.1f, -0.24f), new Vector3(2.2f, 2.0f, 2.2f), ctx.leafMat);
            SetupTreeLOD(ctx, root.gameObject, false);
        }

        private static void SetupTreeLOD(BuildContext ctx, GameObject root, bool pine)
        {
            if (root.GetComponent<LODGroup>() != null)
                return;

            GameObject lod0 = new GameObject("LOD0");
            lod0.transform.SetParent(root.transform, false);
            GameObjectBuilder.MoveAllChildrenExcept(root.transform, lod0.transform, null);

            GameObject lod1 = new GameObject("LOD1");
            lod1.transform.SetParent(root.transform, false);
            if (pine)
            {
                GameObjectBuilder.CreateCylinderChild(lod1.transform, "Trunk", new Vector3(0f, 2.0f, 0f), new Vector3(0.18f, 2.0f, 0.18f), ctx.barkMat);
                GameObjectBuilder.CreateMeshChild(lod1.transform, "Cone", ctx.coneMesh, new Vector3(0f, 3.0f, 0f), new Vector3(1.6f, 2.8f, 1.6f), ctx.pineMat);
            }
            else
            {
                GameObjectBuilder.CreateCylinderChild(lod1.transform, "Trunk", new Vector3(0f, 2.0f, 0f), new Vector3(0.20f, 2.0f, 0.20f), ctx.barkMat);
                GameObjectBuilder.CreateSphereChild(lod1.transform, "Crown", new Vector3(0f, 4.0f, 0f), new Vector3(2.0f, 2.0f, 2.0f), ctx.leafMat);
            }

            GameObject lod2 = new GameObject("LOD2");
            lod2.transform.SetParent(root.transform, false);
            GameObjectBuilder.CreateCubeChild(lod2.transform, "Trunk", new Vector3(0f, 1.8f, 0f), new Vector3(0.12f, 3.6f, 0.12f), ctx.barkMat);
            if (pine)
                GameObjectBuilder.CreateMeshChild(lod2.transform, "Top", ctx.coneMesh, new Vector3(0f, 3.2f, 0f), new Vector3(1.1f, 2.1f, 1.1f), ctx.pineMat);
            else
                GameObjectBuilder.CreateSphereChild(lod2.transform, "Top", new Vector3(0f, 3.8f, 0f), new Vector3(1.6f, 1.6f, 1.6f), ctx.leafMat);

            GameObjectBuilder.SetShadowsRecursive(lod1, ShadowCastingMode.Off, false);
            GameObjectBuilder.SetShadowsRecursive(lod2, ShadowCastingMode.Off, false);
            LODHelpers.ApplyLODGroup(root, lod0, lod1, lod2, 0.58f, 0.24f, 0.06f);
        }

        public static void CreateShrubCluster(BuildContext ctx, Transform parent, Vector3 localPos, float scale)
        {
            GameObject root = new GameObject("Shrub");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            root.transform.localScale = Vector3.one * scale;
            GameObjectBuilder.CreateSphereChild(root.transform, "A", new Vector3(-0.25f, 0.45f, 0f), new Vector3(0.7f, 0.7f, 0.7f), ctx.leafMat);
            GameObjectBuilder.CreateSphereChild(root.transform, "B", new Vector3(0.24f, 0.46f, 0.07f), new Vector3(0.74f, 0.72f, 0.74f), ctx.leafMat);
            GameObjectBuilder.CreateSphereChild(root.transform, "C", new Vector3(0f, 0.60f, -0.16f), new Vector3(0.78f, 0.72f, 0.78f), ctx.leafMat);
        }

        public static void CreateReedCluster(BuildContext ctx, Transform parent, Vector3 worldPos, float variant, float qualityBoost)
        {
            GameObject root = new GameObject("Reed");
            root.transform.SetParent(parent);
            root.transform.position = worldPos;
            root.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.25f, variant);
            int sections = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(5f, 8f, Mathf.InverseLerp(1f, 3f, qualityBoost))), 5, 8);
            for (int i = 0; i < sections; i++)
            {
                float a = i * 72f + variant * 20f;
                GameObjectBuilder.CreateMeshChild(root.transform, "Blade_" + i, ctx.wheatBladeMesh, new Vector3((i - 2) * 0.05f, 0f, 0f), new Vector3(0.7f, 1.1f + i * 0.06f, 0.7f), ctx.grassBladeMat).transform.localRotation = Quaternion.Euler(0f, a, 0f);
            }
        }
    }
}
