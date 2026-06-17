using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor.Biomes
{
    public static class BiomeRegistry
    {
        private static List<BiomeDefinition> _biomes = new List<BiomeDefinition>();

        public static void Reload()
        {
            _biomes.Clear();
            string[] guids = AssetDatabase.FindAssets("t:BiomeDefinition");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                _biomes.Add(AssetDatabase.LoadAssetAtPath<BiomeDefinition>(path));
            }
        }

        public static List<BiomeDefinition> GetAllBiomes()
        {
            if (_biomes.Count == 0) Reload();
            return _biomes;
        }
    }
}
