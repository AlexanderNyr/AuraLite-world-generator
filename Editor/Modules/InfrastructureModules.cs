using System.Threading;
using System.Threading.Tasks;
using AuraLiteWorldGenerator.Editor.Core;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor.Modules
{
    public class HydrologyModule : IWorldGeneratorModule
    {
        public string Id => "Hydrology";
        public int Order => 30;

        public Task ExecuteAsync(GenerationContext ctx, IProgressReporter progress, CancellationToken ct)
        {
            progress.Report("Generating hydrology", 0.5f);
            var oldCtx = ctx.Assets.Get<BuildContext>("LegacyContext");
            var grid = ctx.Assets.Get<TerrainGrid>("TerrainGrid");
            WaterGenerator.CreateWaterSystem(oldCtx, grid, ctx.Layout, ctx.Settings, ctx.Hierarchy.WaterRoot.transform);
            progress.Report("Hydrology complete", 1.0f);
            return Task.CompletedTask;
        }
    }

    public class RoadNetworkModule : IWorldGeneratorModule
    {
        public string Id => "RoadNetwork";
        public int Order => 40;

        public Task ExecuteAsync(GenerationContext ctx, IProgressReporter progress, CancellationToken ct)
        {
            progress.Report("Generating roads", 0.5f);
            var oldCtx = ctx.Assets.Get<BuildContext>("LegacyContext");
            var grid = ctx.Assets.Get<TerrainGrid>("TerrainGrid");
            RoadGenerator.CreateRoadMeshes(oldCtx, grid, ctx.Layout, ctx.Settings, ctx.Hierarchy.RoadsRoot.transform);
            RoadGenerator.CreateBridge(oldCtx, grid, ctx.Layout, ctx.Hierarchy.RoadsRoot.transform);
            RoadGenerator.CreateStreetFences(oldCtx, grid, ctx.Layout, ctx.Hierarchy.RoadsRoot.transform);
            progress.Report("Roads complete", 1.0f);
            return Task.CompletedTask;
        }
    }
}
