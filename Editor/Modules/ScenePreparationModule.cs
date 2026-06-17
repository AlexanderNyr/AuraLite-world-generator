using System.Threading;
using System.Threading.Tasks;
using AuraLiteWorldGenerator.Editor.Core;
using UnityEngine;
using UnityEditor;

namespace AuraLiteWorldGenerator.Editor.Modules
{
    public class ScenePreparationModule : IWorldGeneratorModule
    {
        public string Id => "ScenePreparation";
        public int Order => 5;

        public Task ExecuteAsync(GenerationContext ctx, IProgressReporter progress, CancellationToken ct)
        {
            progress.Report("Preparing scene hierarchy", 0.5f);
            
            var root = new GameObject("GeneratedRuralWorld_Root");
            Undo.RegisterCreatedObjectUndo(root, "AuraLite Generate");
            ctx.Hierarchy.Root = root;
            
            ctx.Hierarchy.TerrainRoot = CreateChild(root, "Environment");
            ctx.Hierarchy.RoadsRoot = CreateChild(root, "Roads");
            ctx.Hierarchy.VillageRoot = CreateChild(root, "Village");
            ctx.Hierarchy.ForestRoot = CreateChild(root, "ForestFar");
            ctx.Hierarchy.WaterRoot = CreateChild(root, "Water");
            ctx.Hierarchy.FieldsRoot = CreateChild(root, "Fields"); 
            
            progress.Report("Scene ready", 1.0f);
            return Task.CompletedTask;
        }

        private GameObject CreateChild(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            return go;
        }
    }
}
