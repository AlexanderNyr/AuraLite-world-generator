using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AuraLiteWorldGenerator.Editor.Assets
{
    public static class TextureAtlasPacker
    {
        public static void PackTextures(List<Texture2D> textures, string outputPath)
        {
            Texture2D atlas = new Texture2D(2048, 2048);
            Rect[] rects = atlas.PackTextures(textures.ToArray(), 2, 2048);
            // Save atlas and handle UV remaps
            byte[] bytes = atlas.EncodeToPNG();
            System.IO.File.WriteAllBytes(outputPath, bytes);
            AssetDatabase.ImportAsset(outputPath);
        }
    }
}
