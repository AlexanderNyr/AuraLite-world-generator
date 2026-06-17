using UnityEngine;
using AuraLiteWorldGenerator.Runtime;

namespace AuraLiteWorldGenerator.Editor.Core
{
    /// <summary>
    /// Interface for providing building geometry.
    /// </summary>
    public interface IBuildingProvider
    {
        bool CanBuild(BuildingKind kind);
        GameObject Build(GenerationContext ctx, HouseSpec data);
    }
}
