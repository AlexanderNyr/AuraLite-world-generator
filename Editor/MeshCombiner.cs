using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Combines MeshFilter children that share a material into one or more batched meshes.
    /// Safe for meshes that approach the 65k vertex limit by splitting into multiple batches.
    /// </summary>
    public static class MeshCombiner
    {
        public const int MaxVerticesPerMesh = 60000;

        /// <summary>
        /// Combines all MeshFilter children of <paramref name="root"/> by shared material.
        /// Original child GameObjects are destroyed after combining.
        /// </summary>
        public static void CombineChildrenByMaterial(Transform root)
        {
            if (root == null)
                throw new System.ArgumentNullException(nameof(root));

            var filtersToDestroy = new List<MeshFilter>();
            Dictionary<Material, List<MeshFilter>> groups = GroupFiltersByMaterial(root, filtersToDestroy);
            if (groups.Count == 0)
                return;

            Matrix4x4 rootToLocal = root.worldToLocalMatrix;

            foreach (var kvp in groups)
            {
                Material material = kvp.Key;
                List<MeshFilter> filters = kvp.Value;
                CombineBatched(root, rootToLocal, material, filters);
            }

            // Destroy original child objects that were combined.
            for (int i = filtersToDestroy.Count - 1; i >= 0; i--)
            {
                if (filtersToDestroy[i] != null)
                    Object.DestroyImmediate(filtersToDestroy[i].gameObject);
            }
        }

        private static Dictionary<Material, List<MeshFilter>> GroupFiltersByMaterial(Transform root, List<MeshFilter> outFiltersToDestroy)
        {
            Dictionary<Material, List<MeshFilter>> groups = new Dictionary<Material, List<MeshFilter>>();
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);

            for (int i = 0; i < filters.Length; i++)
            {
                if (filters[i].transform == root)
                    continue;
                if (filters[i].sharedMesh == null)
                    continue;

                MeshRenderer renderer = filters[i].GetComponent<MeshRenderer>();
                if (renderer == null || renderer.sharedMaterial == null)
                    continue;

                Material mat = renderer.sharedMaterial;
                if (!groups.TryGetValue(mat, out List<MeshFilter> list))
                {
                    list = new List<MeshFilter>();
                    groups[mat] = list;
                }
                list.Add(filters[i]);
                outFiltersToDestroy.Add(filters[i]);
            }

            return groups;
        }

        private static void CombineBatched(Transform root, Matrix4x4 rootToLocal, Material material, List<MeshFilter> filters)
        {
            List<CombineInstance> batch = new List<CombineInstance>(filters.Count);
            int batchVertexCount = 0;
            int batchIndex = 0;

            for (int i = 0; i < filters.Count; i++)
            {
                MeshFilter filter = filters[i];
                Mesh mesh = filter.sharedMesh;
                int vertexCount = mesh.vertexCount;

                if (batchVertexCount + vertexCount > MaxVerticesPerMesh && batch.Count > 0)
                {
                    FlushBatch(root, rootToLocal, material, batch, batchIndex++);
                    batchVertexCount = 0;
                    batch.Clear();
                }

                CombineInstance instance = new CombineInstance
                {
                    mesh = mesh,
                    transform = rootToLocal * filter.transform.localToWorldMatrix
                };
                batch.Add(instance);
                batchVertexCount += vertexCount;
            }

            if (batch.Count > 0)
                FlushBatch(root, rootToLocal, material, batch, batchIndex);
        }

        private static void FlushBatch(Transform root, Matrix4x4 rootToLocal, Material material, List<CombineInstance> batch, int batchIndex)
        {
            Mesh combinedMesh = new Mesh
            {
                name = $"Combined_{material.name}_{batchIndex}"
            };
            combinedMesh.CombineMeshes(batch.ToArray(), true, true);

            GameObject combinedGO = new GameObject($"Combined_{material.name}_{batchIndex}", typeof(MeshFilter), typeof(MeshRenderer));
            combinedGO.transform.SetParent(root, false);

            MeshFilter filter = combinedGO.GetComponent<MeshFilter>();
            filter.sharedMesh = combinedMesh;

            MeshRenderer renderer = combinedGO.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;

            GameObjectBuilder.MarkStaticRecursive(combinedGO);
        }
    }
}
