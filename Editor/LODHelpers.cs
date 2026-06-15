using System.Collections.Generic;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Helper methods for creating and managing LOD groups.
    /// </summary>
    public static class LODHelpers
    {
        public static void ApplyLODGroup(GameObject root, GameObject lod0, GameObject lod1, GameObject lod2, float h0, float h1, float h2)
        {
            LODGroup group = root.GetComponent<LODGroup>();
            if (group == null)
                group = root.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.CrossFade;
            group.animateCrossFading = true;
            LOD[] lods = new LOD[3];
            lods[0] = new LOD(h0, CollectRenderersForGroup(lod0, group));
            lods[1] = new LOD(h1, CollectRenderersForGroup(lod1, group));
            lods[2] = new LOD(h2, CollectRenderersForGroup(lod2, group));
            group.SetLODs(lods);
            group.RecalculateBounds();
        }

        private static Renderer[] CollectRenderersForGroup(GameObject root, LODGroup owner)
        {
            List<Renderer> renderers = new List<Renderer>();
            MeshRenderer[] mrs = root.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < mrs.Length; i++)
            {
                LODGroup nearest = mrs[i].GetComponentInParent<LODGroup>();
                if (nearest == owner)
                    renderers.Add(mrs[i]);
            }
            return renderers.ToArray();
        }
    }
}
