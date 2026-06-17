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
            
            // Using correct roots
            var fieldsParent = ctx.Hierarchy.FieldsRoot.transform;
            
            FieldPropsGenerator.CreateFieldBoundaryProps(oldCtx, grid, ctx.Layout, ctx.Settings, fieldsParent);
            FieldPropsGenerator.CreateFieldStonePiles(oldCtx, grid, ctx.Layout, ctx.Settings, fieldsParent);
            WaterGenerator.CreateWaterVegetation(oldCtx, grid, ctx.Layout, ctx.Settings, fieldsParent);
            FieldPropsGenerator.CreateFieldProps(oldCtx, grid, ctx.Layout, ctx.Settings, fieldsParent);
            
            progress.Report("Props added", 1.0f);
            return Task.CompletedTask;
        }
    }
}
