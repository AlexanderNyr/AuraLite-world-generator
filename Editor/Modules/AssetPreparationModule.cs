using System;
using System.Threading;
using System.Threading.Tasks;
using AuraLiteWorldGenerator.Editor.Core;
using UnityEditor;

namespace AuraLiteWorldGenerator.Editor.Modules
{
    public class AssetPreparationModule : IWorldGeneratorModule
    {
        public string Id => "AssetPreparation";
        public int Order => 0;

        public Task ExecuteAsync(GenerationContext ctx, IProgressReporter progress, CancellationToken ct)
        {
            progress.Report("Preparing assets", 0.1f);
            
            // For backward compatibility, we use the old factory but store assets in the new registry
            var oldCtx = BuildContextFactory.Prepare(ctx.Settings);
            
            // Map old context to new registry
            ctx.Assets.Register("rootFolder", oldCtx.rootFolder);
            ctx.Assets.Register("litShader", oldCtx.litShader);
            ctx.Assets.Register("terrainShader", oldCtx.terrainShader);
            // ... more registrations can be added as needed
            
            // Store the whole old context for components that still need it
            ctx.Assets.Register("LegacyContext", oldCtx);

            progress.Report("Assets prepared", 1.0f);
            return Task.CompletedTask;
        }
    }
}
