using UnityEngine;

namespace AuraLiteWorldGenerator.Editor.Core
{
    public struct BiomeData
    {
        public string BiomeId;
        public float Weight;
    }

    public interface IBiomeProvider
    {
        BiomeData GetBiome(Vector2 worldPos);
    }
}
