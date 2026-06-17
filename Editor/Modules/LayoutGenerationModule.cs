using System.Threading;
using System.Threading.Tasks;
using AuraLiteWorldGenerator.Editor.Core;

namespace AuraLiteWorldGenerator.Editor.Modules
{
    public class LayoutGenerationModule : IWorldGeneratorModule
    {
        public string Id => "LayoutGeneration";
        public int Order => 10;

        public Task ExecuteAsync(GenerationContext ctx, IProgressReporter progress, CancellationToken ct)
        {
            progress.Report("Generating layout", 0.1f);
            ctx.Layout = WorldLayoutGenerator.Generate(ctx.Settings);
            progress.Report("Layout generated", 1.0f);
            return Task.CompletedTask;
        }
    }
}
