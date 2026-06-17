using System.Threading;
using System.Threading.Tasks;
using AuraLiteWorldGenerator.Editor.Core;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_AI_NAVIGATION
using UnityEditor.AI;
#endif

namespace AuraLiteWorldGenerator.Editor.Modules
{
    /// <summary>
    /// Generates NavMesh for the world so NPCs can navigate.
    /// Marks terrain as walkable, roads as preferred paths, and water as off-limits.
    /// </summary>
    public class NavMeshModule : IWorldGeneratorModule
    {
        public string Id => "NavMesh";
        public int Order => 105; // After optimization, before lighting

        public Task ExecuteAsync(GenerationContext ctx, IProgressReporter progress, CancellationToken ct)
        {
            progress.Report("Generating NavMesh", 0.1f);

            var root = ctx.Hierarchy.Root;
            if (root == null)
            {
                progress.Report("NavMesh skipped (no root)", 1.0f);
                return Task.CompletedTask;
            }

            // 1. Add NavMeshModifier to key objects
            SetupNavMeshModifiers(ctx);

            // 2. Add NavMeshLink for the bridge
            SetupBridgeLink(ctx);

            // 3. Create NavMeshSurface and bake
            SetupNavMeshSurface(ctx, root);

            progress.Report("NavMesh complete", 1.0f);
            return Task.CompletedTask;
        }

        private void SetupNavMeshModifiers(GenerationContext ctx)
        {
            // Terrain is walkable by default - mark all terrains
            var grid = ctx.Assets.Get<TerrainGrid>("TerrainGrid");
            if (grid != null && grid.terrains != null)
            {
                for (int z = 0; z < grid.tileCount; z++)
                {
                    for (int x = 0; x < grid.tileCount; x++)
                    {
                        var terrain = grid.terrains[x, z];
                        if (terrain == null) continue;

                        var modifier = terrain.gameObject.GetComponent<NavMeshModifier>();
                        if (modifier == null)
                            modifier = terrain.gameObject.AddComponent<NavMeshModifier>();
                        modifier.areaID = 0; // Walkable
                        modifier.overrideArea = true;
                    }
                }
            }

            // Roads are walkable with lower cost
            if (ctx.Hierarchy.RoadsRoot != null)
            {
                var roadModifier = ctx.Hierarchy.RoadsRoot.GetComponent<NavMeshModifier>();
                if (roadModifier == null)
                    roadModifier = ctx.Hierarchy.RoadsRoot.AddComponent<NavMeshModifier>();
                roadModifier.overrideArea = false; // Default walkable
            }

            // Water is not walkable
            if (ctx.Hierarchy.WaterRoot != null)
            {
                var waterModifier = ctx.Hierarchy.WaterRoot.GetComponent<NavMeshModifier>();
                if (waterModifier == null)
                    waterModifier = ctx.Hierarchy.WaterRoot.AddComponent<NavMeshModifier>();
                waterModifier.areaID = 1; // Not Walkable
                waterModifier.overrideArea = true;
            }
        }

        private void SetupBridgeLink(GenerationContext ctx)
        {
            // Find the bridge and add a NavMeshLink
            var layout = ctx.Layout;
            if (layout.riverPoints.Count < 3) return;

            Vector3 a = layout.riverPoints[1];
            Vector3 b = layout.riverPoints[2];
            Vector3 center = Vector3.Lerp(a, b, 0.52f);
            center.y = layout.waterLevel + 1.5f;

            var bridgeGO = FindChildByName(ctx.Hierarchy.RoadsRoot, "VillageBridge");
            if (bridgeGO != null)
            {
                var link = bridgeGO.GetComponent<NavMeshLink>();
                if (link == null)
                    link = bridgeGO.AddComponent<NavMeshLink>();

                link.startPoint = new Vector3(0f, 0f, -8f);
                link.endPoint = new Vector3(0f, 0f, 8f);
                link.width = 5f;
                link.costModifier = -1f; // Cheaper path
            }
        }

        private void SetupNavMeshSurface(GenerationContext ctx, GameObject root)
        {
            var surfaceGO = new GameObject("NavMeshSurface");
            surfaceGO.transform.SetParent(root.transform);

            var surface = surfaceGO.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.defaultArea = 0; // Walkable
            surface.layerMask = -1; // All layers

            // Set reasonable defaults for rural world
            var settings = surface.GetBuildSettings();
            settings.agentRadius = 0.5f;
            settings.agentHeight = 2f;
            settings.agentSlope = 45f;
            settings.agentClimb = 0.75f;
            settings.minRegionArea = 100;
            settings.overrideVoxelSize = false;
            settings.overrideTileSize = false;
            surface.SetBuildSettings(settings);

#if UNITY_AI_NAVIGATION
            // Bake the NavMesh
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
#elif UNITY_EDITOR
            // Fallback for older Unity versions
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
#endif
        }

        private GameObject FindChildByName(GameObject parent, string name)
        {
            if (parent == null) return null;
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                var child = parent.transform.GetChild(i);
                if (child.name == name) return child.gameObject;
            }
            return null;
        }
    }
}
