using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using AuraLiteWorldGenerator.Editor.Core;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor.Modules
{
    public class TerrainGenerationModule : IWorldGeneratorModule
    {
        public string Id => "TerrainGeneration";
        public int Order => 20;

        public async Task ExecuteAsync(GenerationContext ctx, IProgressReporter progress, CancellationToken ct)
        {
            progress.Report("Generating terrain", 0.0f);
            
            var oldCtx = ctx.Assets.Get<BuildContext>("LegacyContext");
            TerrainGrid grid = null;

            // Helper to run IEnumerator as Task
            await RunCoroutineAsTask(TerrainGenerator.CreateTerrainGrid(oldCtx, ctx.Layout, ctx.Settings, ctx.Hierarchy.TerrainRoot.transform, result => grid = result, ct), ct);
            ctx.Assets.Register("TerrainGrid", grid);
            progress.Report("Terrain grid created", 0.4f);

            await RunCoroutineAsTask(TerrainGenerator.PaintTerrainGrid(oldCtx, grid, ctx.Layout, null, ct), ct);
            progress.Report("Terrain painted", 0.7f);

            await RunCoroutineAsTask(TerrainGenerator.PopulateTerrainDetails(oldCtx, grid, ctx.Layout, ctx.Settings, null, ct), ct);
            progress.Report("Terrain details populated", 1.0f);
        }

        private Task RunCoroutineAsTask(IEnumerator routine, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>();
            EditorCoroutineBridge.Run(routine, tcs, ct);
            return tcs.Task;
        }
    }
}
