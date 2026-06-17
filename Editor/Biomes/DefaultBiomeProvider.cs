using UnityEngine;
using AuraLiteWorldGenerator.Editor.Core;
using AuraLiteWorldGenerator.Runtime;

namespace AuraLiteWorldGenerator.Editor.Biomes
{
    public class DefaultBiomeProvider : IBiomeProvider
    {
        private SeededRandom _random;

        public DefaultBiomeProvider()
        {
            _random = new SeededRandom(0);
        }

        public void Initialize(int seed)
        {
            _random = new SeededRandom(seed);
        }

        public BiomeData GetBiome(Vector2 worldPos)
        {
            float noise = Mathf.PerlinNoise(worldPos.x * 0.001f, worldPos.y * 0.001f);
            string id = noise < 0.3f ? "Marshland" : noise < 0.6f ? "Temperate" : "Boreal";
            return new BiomeData { BiomeId = id, Weight = 1.0f };
        }
    }
}
