using System.Threading;
using System.Threading.Tasks;
using AuraLiteWorldGenerator.Editor.Core;

namespace AuraLiteWorldGenerator.Editor.Modules
{
    public class PropsModule : IWorldGeneratorModule
    {
        public string Id => "Props";
        public int Order => 70;

        public Task ExecuteAsync(GenerationContext ctx, IProgressReporter progress, CancellationToken ct)
        {
            progress.Report("Adding props", 0.1f);
            var oldCtx = ctx.Assets.Get<BuildContext>("LegacyContext");
            var grid = ctx.Assets.Get<TerrainGrid>("TerrainGrid");
            
            // Reusing existing roots or creating a new one if needed
            var parent = ctx.Hierarchy.VillageRoot.transform; // Simplified
            
            FieldPropsGenerator.CreateFieldBoundaryProps(oldCtx, grid, ctx.Layout, ctx.Settings, parent);
            FieldPropsGenerator.CreateFieldStonePiles(oldCtx, grid, ctx.Layout, ctx.Settings, parent);
            WaterGenerator.CreateWaterVegetation(oldCtx, grid, ctx.Layout, ctx.Settings, parent);
            FieldPropsGenerator.CreateFieldProps(oldCtx, grid, ctx.Layout, ctx.Settings, parent);
            
            progress.Report("Props added", 1.0f);
            return Task.CompletedTask;
        }
    }
}
