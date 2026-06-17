#pragma warning disable CS0618 // 'BuildContext' is obsolete: 'Use GenerationContext and AssetRegistry instead.'
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using AuraLiteWorldGenerator.Editor.Core;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor.Modules
{
    public class SettlementModule : IWorldGeneratorModule
    {
        public string Id => "Settlement";
        public int Order => 50;

        public async Task ExecuteAsync(GenerationContext ctx, IProgressReporter progress, CancellationToken ct)
        {
            progress.Report("Building settlement", 0.0f);
            var oldCtx = ctx.Assets.Get<BuildContext>("LegacyContext");
            var grid = ctx.Assets.Get<TerrainGrid>("TerrainGrid");
            
            GameObject housesRoot = new GameObject("Houses");
            housesRoot.transform.SetParent(ctx.Hierarchy.VillageRoot.transform);
            
            for (int i = 0; i < ctx.Layout.houses.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                BuildingBuilder.Build(oldCtx, grid, ctx.Layout.houses[i], housesRoot.transform, i, ctx.Settings.villageStyle, ctx.Layout.random);
                if (i % 10 == 0)
                {
                    progress.Report("Building houses", (float)i / ctx.Layout.houses.Count);
                    await Task.Yield();
                }
            }
            
            VillagePropsGenerator.CreateVillageStreetProps(oldCtx, grid, ctx.Layout, ctx.Settings, ctx.Hierarchy.VillageRoot.transform);
            VillagePropsGenerator.CreateVillageGreenery(oldCtx, grid, ctx.Layout, ctx.Settings, ctx.Hierarchy.VillageRoot.transform);
            VillagePropsGenerator.CreateLakeShoreProps(oldCtx, grid, ctx.Layout, ctx.Settings, ctx.Hierarchy.VillageRoot.transform);
            
            progress.Report("Settlement complete", 1.0f);
        }
    }

    public class VegetationModule : IWorldGeneratorModule
    {
        public string Id => "Vegetation";
        public int Order => 60;

        public async Task ExecuteAsync(GenerationContext ctx, IProgressReporter progress, CancellationToken ct)
        {
            progress.Report("Generating vegetation", 0.1f);
            var oldCtx = ctx.Assets.Get<BuildContext>("LegacyContext");
            var grid = ctx.Assets.Get<TerrainGrid>("TerrainGrid");
            var biomeProvider = ctx.Services.Resolve<IBiomeProvider>();
            
            // Note: ForestGenerator.CreateFarForest is a coroutine
            var tcs = new TaskCompletionSource<bool>();
            EditorCoroutineBridge.Run(ForestGenerator.CreateFarForest(oldCtx, grid, ctx.Layout, ctx.Settings, ctx.Hierarchy.ForestRoot.transform, biomeProvider, ct), tcs, ct);
            await tcs.Task;
            
            progress.Report("Vegetation complete", 1.0f);
        }
    }
}
