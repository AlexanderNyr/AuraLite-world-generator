using UnityEngine;
using UnityEditor;
using System.IO;

namespace AuraLiteWorldGenerator.Editor.Assets
{
    public static class PbrMaterialFactory
    {
        public static Material CreateLitMaterial(string path, Color color, Texture2D normal = null, Texture2D mask = null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (mask != null)
            {
                mat.SetTexture("_MetallicGlossMap", mask);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        public static Texture2D CreateMaskMap(int width, int height, float metallic, float ao, float detail, float smoothness)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color[] pixels = new Color[width * height];
            Color mask = new Color(metallic, ao, detail, smoothness);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = mask;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
