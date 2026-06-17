#pragma warning disable CS0618 // 'BuildContext' is obsolete: 'Use GenerationContext and AssetRegistry instead.'
using UnityEngine;
using AuraLiteWorldGenerator.Editor.Core;
using AuraLiteWorldGenerator.Runtime;

namespace AuraLiteWorldGenerator.Editor
{
    public class DefaultBuildingProvider : IBuildingProvider
    {
        public bool CanBuild(BuildingKind kind) => true;

        public GameObject Build(GenerationContext ctx, HouseSpec data)
        {
            var oldCtx = ctx.Assets.Get<BuildContext>("LegacyContext");
            var root = new GameObject($"House_{data.kind}");
            
            // Need a valid TerrainGrid reference or pass null if not needed?
            // Actually BuildingBuilder.Build doesn't always need grid if it's placed already, 
            // but let's pass a dummy or get it from Assets.
            var grid = ctx.Assets.Get<TerrainGrid>("TerrainGrid");
            
            BuildingBuilder.Build(oldCtx, grid, data, root.transform, 0, ctx.Settings.villageStyle, ctx.Random);
            return root;
        }
    }
}
