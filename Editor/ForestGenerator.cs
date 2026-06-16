using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Generates the far forest with pines and broadleaf trees.
    /// </summary>
    public static class ForestGenerator
    {
        public static IEnumerator CreateFarForest(BuildContext ctx, TerrainGrid grid, WorldLayout layout, GenerationSettings settings, Transform parent, CancellationToken cancellationToken = default)
        {
            GameObject edgeRoot = new GameObject("ForestEdge");
            edgeRoot.transform.SetParent(parent);
            GameObject pines = new GameObject("Pines");
            pines.transform.SetParent(parent);
            GameObject broadleaf = new GameObject("Broadleaf");
            broadleaf.transform.SetParent(parent);

            float startZ = layout.forestStartZ - 260f;
            float endZ = layout.worldSizeMeters - 80f;
            float step = layout.worldSizeMeters > 9000f ? Mathf.Lerp(38f, 26f, Mathf.InverseLerp(1f, 3f, settings.qualityBoost)) : Mathf.Lerp(30f, 18f, Mathf.InverseLerp(1f, 3f, settings.qualityBoost));
            int created = 0;
            const int yieldBatch = 50;

            for (float z = startZ; z <= endZ; z += step)
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (float x = 80f; x <= layout.worldSizeMeters - 80f; x += step)
                {
                    float forest = WorldLayoutGenerator.ComputeForestMask(layout, x, z);
                    if (forest < 0.48f)
                        continue;
                    float hash = GeometryHelpers.Hash01(Mathf.FloorToInt(x / step), Mathf.FloorToInt(z / step), layout.seed + 301);
                    if (hash > Mathf.Lerp(0.28f, 0.64f, forest))
                        continue;

                    Vector3 pos = new Vector3(x + (GeometryHelpers.Hash01((int)x, (int)z, layout.seed + 17) - 0.5f) * step * 0.6f, 0f, z + (GeometryHelpers.Hash01((int)x, (int)z, layout.seed + 27) - 0.5f) * step * 0.6f);
                    pos.y = GeometryHelpers.SampleTerrainHeight(grid, pos);
                    bool edgeBand = forest < 0.72f;
                    bool pine = edgeBand ? (GeometryHelpers.Hash01((int)x, (int)z, layout.seed + 37) > 0.52f) : (GeometryHelpers.Hash01((int)x, (int)z, layout.seed + 37) > 0.24f);

                    GameObject tree = new GameObject((pine ? "Pine_" : "Tree_") + created++);
                    tree.transform.SetParent(edgeBand ? edgeRoot.transform : (pine ? pines.transform : broadleaf.transform));
                    tree.transform.position = pos;
                    tree.transform.rotation = Quaternion.Euler(layout.random.Range(-2f, 2f), GeometryHelpers.Hash01((int)x, (int)z, layout.seed + 47) * 360f, layout.random.Range(-2f, 2f));
                    tree.transform.localScale = Vector3.one * (pine ? Mathf.Lerp(0.95f, 1.70f, forest) : Mathf.Lerp(0.88f, 1.55f, forest));

                    if (pine)
                        VegetationBuilder.BuildPineTree(ctx, tree.transform);
                    else
                        VegetationBuilder.BuildBroadleafTree(ctx, tree.transform);

                    if (!pine && settings.qualityBoost > 1.25f && GeometryHelpers.Hash01((int)x, (int)z, layout.seed + 333) > 0.65f)
                    {
                        VegetationBuilder.CreateShrubCluster(ctx, tree.transform, new Vector3(0.8f, 0f, -0.4f), 0.7f);
                        VegetationBuilder.CreateShrubCluster(ctx, tree.transform, new Vector3(-0.6f, 0f, 0.5f), 0.6f);
                    }

                    GameObjectBuilder.MarkStaticRecursive(tree);

                    if (created % yieldBatch == 0)
                        yield return null;
                }
            }
        }

    }
}
