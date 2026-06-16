using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Combines MeshFilter children into batched meshes per spatial chunk and shared material.
    /// Keeps culling efficiency because each chunk is a separate GameObject with its own bounds.
    /// </summary>
    public static class ChunkedMeshCombiner
    {
        public const int MaxVerticesPerMesh = 60000;

        public static void CombineChildrenByChunkAndMaterial(Transform root, float chunkSize)
        {
            if (root == null)
                throw new System.ArgumentNullException(nameof(root));
            if (chunkSize <= 0f)
                throw new System.ArgumentException("Chunk size must be positive.", nameof(chunkSize));

            Dictionary<string, List<MeshFilter>> groups = new Dictionary<string, List<MeshFilter>>();
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

                Bounds b = CalculateBounds(filters[i].gameObject);
                Vector3 center = b.center;
                int cx = Mathf.FloorToInt(center.x / chunkSize);
                int cz = Mathf.FloorToInt(center.z / chunkSize);
                string key = renderer.sharedMaterial.name + "_" + cx + "_" + cz;
                if (!groups.TryGetValue(key, out List<MeshFilter> list))
                {
                    list = new List<MeshFilter>();
                    groups[key] = list;
                }
                list.Add(filters[i]);
            }

            if (groups.Count == 0)
                return;

            Matrix4x4 rootToLocal = root.worldToLocalMatrix;

            foreach (var kvp in groups)
            {
                Material material = kvp.Value[0].GetComponent<MeshRenderer>().sharedMaterial;
                CombineBatched(root, rootToLocal, material, kvp.Value, kvp.Key);
            }

            // Destroy original children that contributed to the combined meshes.
            for (int i = filters.Length - 1; i >= 0; i--)
            {
                if (filters[i] != null && filters[i].transform != root)
                    Object.DestroyImmediate(filters[i].gameObject);
            }
        }

        private static void CombineBatched(Transform root, Matrix4x4 rootToLocal, Material material, List<MeshFilter> filters, string key)
        {
            List<CombineInstance> batch = new List<CombineInstance>(filters.Count);
            int batchVertexCount = 0;
            int batchIndex = 0;

            for (int i = 0; i < filters.Count; i++)
            {
                Mesh mesh = filters[i].sharedMesh;
                int vertexCount = mesh.vertexCount;

                if (batchVertexCount + vertexCount > MaxVerticesPerMesh && batch.Count > 0)
                {
                    FlushBatch(root, rootToLocal, material, batch, key + "_" + batchIndex);
                    batchVertexCount = 0;
                    batch.Clear();
                    batchIndex++;
                }

                batch.Add(new CombineInstance
                {
                    mesh = mesh,
                    transform = rootToLocal * filters[i].transform.localToWorldMatrix
                });
                batchVertexCount += vertexCount;
            }

            if (batch.Count > 0)
                FlushBatch(root, rootToLocal, material, batch, key + "_" + batchIndex);
        }

        private static void FlushBatch(Transform root, Matrix4x4 rootToLocal, Material material, List<CombineInstance> batch, string name)
        {
            Mesh combinedMesh = new Mesh { name = "Chunked_" + name };
            combinedMesh.CombineMeshes(batch.ToArray(), true, true);

            GameObject go = new GameObject("Chunked_" + name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(root, false);

            MeshFilter filter = go.GetComponent<MeshFilter>();
            filter.sharedMesh = combinedMesh;

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;

            GameObjectBuilder.MarkStaticRecursive(go);
        }

        private static Bounds CalculateBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(go.transform.position, Vector3.one);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
        }
    }
}
