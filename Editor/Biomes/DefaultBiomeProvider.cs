using UnityEngine;
using AuraLiteWorldGenerator.Editor.Core;
using AuraLiteWorldGenerator.Runtime;

namespace AuraLiteWorldGenerator.Editor.Biomes
{
    /// <summary>
    /// Provides biome data based on world position using layered noise.
    /// 5 built-in biome types: Temperate, Marshland, Boreal, Steppe, Alpine.
    /// Each biome defines material weights, vegetation bias, and terrain parameters.
    /// </summary>
    public class DefaultBiomeProvider : IBiomeProvider
    {
        private SeededRandom _random;
        private int _seed;

        public DefaultBiomeProvider()
        {
            _random = new SeededRandom(0);
        }

        public void Initialize(int seed)
        {
            _seed = seed;
            _random = new SeededRandom(seed);
        }

        public BiomeData GetBiome(Vector2 worldPos)
        {
            float temperatureNoise = Mathf.PerlinNoise(worldPos.x * 0.0008f + _seed * 0.013f, worldPos.y * 0.0008f + 7.3f);
            float humidityNoise = Mathf.PerlinNoise(worldPos.x * 0.0006f + _seed * 0.017f + 100f, worldPos.y * 0.0006f + 42.1f);

            float temperature = Mathf.Lerp(-0.2f, 1.2f, temperatureNoise);
            float humidity = Mathf.Lerp(-0.1f, 1.1f, humidityNoise);

            string id;
            if (temperature > 0.75f)
            {
                // Hot regions
                if (humidity > 0.45f)
                    id = "Marshland";
                else
                    id = "Steppe";
            }
            else if (temperature > 0.35f)
            {
                // Temperate
                id = "Temperate";
            }
            else if (temperature > 0.15f)
            {
                // Cold
                if (humidity > 0.4f)
                    id = "Boreal";
                else
                    id = "Steppe";
            }
            else
            {
                // Very cold - high altitude
                id = "Alpine";
            }

            return new BiomeData { BiomeId = id, Weight = 1.0f };
        }

        /// <summary>
        /// Gets the terrain layer weights for a given biome. Used by TerrainGenerator for splatmap painting.
        /// </summary>
        public static void GetBiomeTerrainWeights(string biomeId, out float grassMul, out float wheatMul, out float forestMul, out float dirtMul, out float stoneMul)
        {
            switch (biomeId)
            {
                case "Marshland":
                    grassMul = 1.4f;  // More grass
                    wheatMul = 0.3f;  // Less wheat (wet soil)
                    forestMul = 1.2f;  // More trees
                    dirtMul = 0.8f;
                    stoneMul = 0.3f;
                    break;
                case "Boreal":
                    grassMul = 0.7f;  // Less grass
                    wheatMul = 0.2f;  // Very little wheat
                    forestMul = 2.0f;  // Dense conifer forest
                    dirtMul = 1.1f;
                    stoneMul = 0.9f;
                    break;
                case "Steppe":
                    grassMul = 1.8f;  // Lots of grass
                    wheatMul = 0.9f;  // Some wheat
                    forestMul = 0.2f;  // Almost no trees
                    dirtMul = 1.3f;
                    stoneMul = 0.5f;
                    break;
                case "Alpine":
                    grassMul = 0.5f;
                    wheatMul = 0.0f;  // No crops
                    forestMul = 0.8f;
                    dirtMul = 0.7f;
                    stoneMul = 2.5f;  // Lots of stone
                    break;
                case "Temperate":
                default:
                    grassMul = 1.0f;
                    wheatMul = 1.0f;
                    forestMul = 1.0f;
                    dirtMul = 1.0f;
                    stoneMul = 1.0f;
                    break;
            }
        }

        /// <summary>
        /// Gets the water color tint for a given biome.
        /// </summary>
        public static Color GetBiomeWaterColor(string biomeId)
        {
            switch (biomeId)
            {
                case "Marshland": return new Color(0.25f, 0.38f, 0.28f, 0.85f); // Murky green
                case "Boreal": return new Color(0.12f, 0.22f, 0.38f, 0.8f);     // Dark blue
                case "Steppe": return new Color(0.28f, 0.35f, 0.42f, 0.75f);     // Dusty blue
                case "Alpine": return new Color(0.18f, 0.42f, 0.55f, 0.7f);      // Clear mountain blue
                case "Temperate":
                default: return new Color(0.22f, 0.41f, 0.58f, 0.78f);           // Standard blue
            }
        }

        /// <summary>
        /// Gets tree type preference for a biome. Returns true if pine is preferred.
        /// </summary>
        public static bool PrefersPine(string biomeId)
        {
            switch (biomeId)
            {
                case "Boreal": return true;
                case "Alpine": return true;
                case "Marshland": return false;
                case "Steppe": return false;
                case "Temperate":
                default: return false;
            }
        }

        /// <summary>
        /// Gets the fog density multiplier for a biome.
        /// </summary>
        public static float GetFogDensityMultiplier(string biomeId)
        {
            switch (biomeId)
            {
                case "Marshland": return 2.5f;
                case "Boreal": return 1.3f;
                case "Steppe": return 0.6f;
                case "Alpine": return 1.8f;
                case "Temperate":
                default: return 1.0f;
            }
        }
    }
}
