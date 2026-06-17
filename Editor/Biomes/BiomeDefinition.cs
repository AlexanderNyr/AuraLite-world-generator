using System;
using System.Collections.Generic;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor.Biomes
{
    [CreateAssetMenu(fileName = "BiomeDefinition", menuName = "AuraLite/Biome Definition")]
    public class BiomeDefinition : ScriptableObject
    {
        public string BiomeId;
        public AnimationCurve HeightCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public float MinTemperature;
        public float MaxTemperature;
        public float MinHumidity;
        public float MaxHumidity;

        public List<TerrainLayerWeight> TerrainLayers = new List<TerrainLayerWeight>();
        
        [Serializable]
        public class TerrainLayerWeight
        {
            public TerrainLayer Layer;
            public float BaseWeight;
        }

        public Color WaterColor = new Color(0.2f, 0.4f, 0.8f);
        public float FogDensity = 0.01f;
    }
}
