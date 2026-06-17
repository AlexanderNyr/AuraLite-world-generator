using System.Threading;
using System.Threading.Tasks;
using AuraLiteWorldGenerator.Editor.Core;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor.Modules
{
    public class OptimizationModule : IWorldGeneratorModule
    {
        public string Id => "Optimization";
        public int Order => 100;

        public Task ExecuteAsync(GenerationContext ctx, IProgressReporter progress, CancellationToken ct)
        {
            progress.Report("Optimizing world", 0.5f);
            var oldCtx = ctx.Assets.Get<BuildContext>("LegacyContext");
            OptimizationSystem.CreateWorldOptimizationSystem(oldCtx, ctx.Layout, ctx.Settings, ctx.Hierarchy.Root, 
                ctx.Hierarchy.RoadsRoot, ctx.Hierarchy.VillageRoot, null, ctx.Hierarchy.ForestRoot);
            progress.Report("Optimization complete", 1.0f);
            return Task.CompletedTask;
        }
    }

    public class LightingModule : IWorldGeneratorModule
    {
        public string Id => "Lighting";
        public int Order => 110;

        public Task ExecuteAsync(GenerationContext ctx, IProgressReporter progress, CancellationToken ct)
        {
            progress.Report("Configuring lighting", 0.5f);
            var oldCtx = ctx.Assets.Get<BuildContext>("LegacyContext");
            LightingAndEnvironment.CreateLightingRig(ctx.Hierarchy.Root.transform, ctx.Layout.villageCenter);
            LightingAndEnvironment.CreateCloudSystem(oldCtx, ctx.Layout, ctx.Settings, ctx.Hierarchy.Root.transform);
            LightingAndEnvironment.CreateMainCamera(ctx.Layout, ctx.Settings);
            LightingAndEnvironment.CreateReflectionProbe(ctx.Layout, ctx.Hierarchy.Root.transform);
            LightingAndEnvironment.CreateGlobalVolume(oldCtx, ctx.Hierarchy.Root.transform);
            progress.Report("Lighting complete", 1.0f);
            return Task.CompletedTask;
        }
    }
}
