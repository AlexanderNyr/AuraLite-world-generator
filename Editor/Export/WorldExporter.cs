using UnityEditor;
using UnityEngine;
using System.IO;

namespace AuraLiteWorldGenerator.Editor.Export
{
    public static class WorldExporter
    {
        public static void ExportToFBX(GameObject root, string path)
        {
            // Integration with Unity FBX Exporter
            Debug.Log($"Exporting {root.name} to {path}...");
        }

        public static void ExportHeightmap(Terrain terrain, string path)
        {
            int res = terrain.terrainData.heightmapResolution;
            float[,] heights = terrain.terrainData.GetHeights(0, 0, res, res);
            // Save as RAW or PNG
        }
    }
}
