using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Creates and manages all generated assets (materials, textures, meshes, prefabs, terrain layers, volume profiles).
    /// </summary>
    public static class AssetFactory
    {
        public static void EnsureFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Folder path cannot be empty.", nameof(path));

            string[] parts = path.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
                throw new ArgumentException("Path must start with Assets: " + path, nameof(path));

            for (int i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(parts[i]))
                    throw new ArgumentException("Folder path contains empty segments: " + path, nameof(path));
            }

            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        public static Shader FindBestLitShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit");
        }

        public static Shader FindBestTerrainShader()
        {
            return Shader.Find("Universal Render Pipeline/Terrain/Lit");
        }

        public static Material CreateOpaqueMaterialAsset(string path, Color color, float metallic, float smoothness, Shader shader)
        {
            if (shader == null)
                throw new ArgumentNullException(nameof(shader), "Cannot create material without a shader at " + path);

            DeleteExistingAsset<Material>(path);
            Material mat = new Material(shader);
            mat.name = Path.GetFileNameWithoutExtension(path);
            SetColorSafe(mat, "_BaseColor", color);
            SetColorSafe(mat, "_Color", color);
            SetColorSafe(mat, "_SpecColor", Color.black);
            SetFloatSafe(mat, "_Metallic", metallic);
            SetFloatSafe(mat, "_Smoothness", smoothness);
            SetFloatSafe(mat, "_Glossiness", smoothness);
            if (smoothness <= 0.01f)
            {
                mat.DisableKeyword("_SPECULAR_HIGHLIGHTS");
                mat.SetFloat("_SpecularHighlights", 0f);
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        public static Material CreateTransparentMaterialAsset(string path, Color color, float metallic, float smoothness, Shader shader)
        {
            Material mat = CreateOpaqueMaterialAsset(path, color, metallic, smoothness, shader);
            ConfigureURPTransparent(mat, color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void ConfigureURPTransparent(Material mat, Color color)
        {
            if (mat == null) return;
            SetColorSafe(mat, "_BaseColor", color);
            SetColorSafe(mat, "_Color", color);
            SetFloatSafe(mat, "_Surface", 1f);
            SetFloatSafe(mat, "_Blend", 0f);
            SetFloatSafe(mat, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            SetFloatSafe(mat, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            SetFloatSafe(mat, "_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        public static Texture2D CreateOrReplaceTextureAsset(string path, int width, int height, Color a, Color b, float noiseAmount, float scale, bool highContrast, bool isNormal = false)
        {
            DeleteExistingAsset<Texture2D>(path);

            Texture2D tex = new Texture2D(width, height, isNormal ? TextureFormat.RGBA32 : TextureFormat.RGBA32, true)
            {
                name = Path.GetFileNameWithoutExtension(path),
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[width * height];
            if (!isNormal)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float nx = x / (float)(width - 1);
                        float ny = y / (float)(height - 1);
                        float n1 = Mathf.PerlinNoise(nx * scale, ny * scale);
                        float n2 = Mathf.PerlinNoise(nx * scale * 2.1f + 23.1f, ny * scale * 2.1f + 57.8f);
                        float n3 = Mathf.PerlinNoise(nx * scale * 4.5f, ny * scale * 4.5f);
                        float n = Mathf.Lerp(n1, n2, 0.45f);
                        n = Mathf.Lerp(n, n3, 0.2f);
                        if (highContrast)
                            n = Mathf.Pow(n, 1.4f);
                        Color c = Color.Lerp(a, b, n);
                        c += new Color(noiseAmount * (n2 - 0.5f), noiseAmount * (n1 - 0.5f), noiseAmount * (n - 0.5f), 0f);
                        c.a = 0f; // Smoothness 0
                        pixels[y * width + x] = c;
                    }
                }
            }
            else
            {
                // Generate simple noise-based Normal Map
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float nx = x / (float)(width - 1);
                        float ny = y / (float)(height - 1);
                        float strength = 0.15f;
                        float v0 = Mathf.PerlinNoise(nx * scale, ny * scale);
                        float vx = Mathf.PerlinNoise((nx + 0.01f) * scale, ny * scale);
                        float vy = Mathf.PerlinNoise(nx * scale, (ny + 0.01f) * scale);
                        
                        Vector3 normal = new Vector3((v0 - vx) * strength, (v0 - vy) * strength, 1.0f).normalized;
                        pixels[y * width + x] = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);
            AssetDatabase.CreateAsset(tex, path);
            
            if (isNormal)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.SaveAndReimport();
                }
            }
            
            return tex;
        }

        public static Texture2D CreateOrReplaceCloudTextureAsset(string path, int width, int height)
        {
            DeleteExistingAsset<Texture2D>(path);

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true)
            {
                name = Path.GetFileNameWithoutExtension(path),
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = x / (float)(width - 1);
                    float ny = y / (float)(height - 1);
                    float fx = nx * 2f - 1f;
                    float fy = ny * 2f - 1f;
                    float radial = 1f - Mathf.Clamp01(Mathf.Sqrt(fx * fx + fy * fy));
                    float n = GeometryHelpers.FBM(nx * 5.2f + 14f, ny * 5.2f + 27f, 4, 0.5f, 2f);
                    float alpha = Mathf.Clamp01((n - 0.42f) * 2.1f) * Mathf.Pow(radial, 1.3f);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(true, false);
            AssetDatabase.CreateAsset(tex, path);
            return tex;
        }

        public static TerrainLayer CreateOrReplaceTerrainLayer(string path, Texture2D diffuse, Vector2 tileSize, float smoothness)
        {
            DeleteExistingAsset<TerrainLayer>(path);
            TerrainLayer layer = new TerrainLayer
            {
                diffuseTexture = diffuse,
                tileSize = tileSize,
                tileOffset = Vector2.zero,
                specular = Color.black,
                metallic = 0f,
                smoothness = smoothness,
                diffuseRemapMax = Color.white,
                diffuseRemapMin = Color.black
            };
            AssetDatabase.CreateAsset(layer, path);
            return layer;
        }

        public static Mesh CreateOrReplaceMeshAsset(string path, Mesh mesh)
        {
            DeleteExistingAsset<Mesh>(path);
            mesh.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        public static VolumeProfile CreateOrReplaceVolumeProfile(string path)
        {
            DeleteExistingAsset<VolumeProfile>(path);
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }

        public static GameObject CreateOrReplaceDetailPrefab(string path, Mesh mesh, Material material)
        {
            DeleteExistingAsset<GameObject>(path);
            GameObject temp = new GameObject(Path.GetFileNameWithoutExtension(path), typeof(MeshFilter), typeof(MeshRenderer));
            temp.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer mr = temp.GetComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = true;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            UnityEngine.Object.DestroyImmediate(temp);
            return prefab;
        }

        public static void SetupURPVolumeProfile(VolumeProfile profile)
        {
            if (profile == null) return;

            // Core URP Bloom & Tonemapping
            profile.Add<Bloom>(true);
            if (profile.TryGet<Bloom>(out var bloom))
            {
                bloom.intensity.Override(0.5f);
                bloom.threshold.Override(1.0f);
            }

            profile.Add<Tonemapping>(true);
            if (profile.TryGet<Tonemapping>(out var tonemapping))
            {
                tonemapping.mode.Override(TonemappingMode.ACES);
            }

            profile.Add<ColorAdjustments>(true);
            if (profile.TryGet<ColorAdjustments>(out var colorAdjustments))
            {
                colorAdjustments.contrast.Override(15f);
                colorAdjustments.saturation.Override(10f);
            }

            // For Unity 6 Volumetric effects, we use reflection to avoid compilation errors 
            // if the project is using an older URP version or missing references.
            AddVolumeComponentIfExists(profile, "UnityEngine.Rendering.Universal.VolumetricClouds");
            AddVolumeComponentIfExists(profile, "UnityEngine.Rendering.Universal.PhysicallyBasedSky");
            AddVolumeComponentIfExists(profile, "UnityEngine.Rendering.Universal.Fog");
        }

        private static void AddVolumeComponentIfExists(VolumeProfile profile, string typeName)
        {
            Type type = Type.GetType(typeName + ", Unity.RenderPipelines.Universal.Runtime");
            if (type == null) type = Type.GetType(typeName); // Try without assembly
            
            if (type != null && typeof(VolumeComponent).IsAssignableFrom(type))
            {
                if (!profile.Has(type))
                {
                    var comp = profile.Add(type, true);
                    // Try to enable it
                    var activeField = type.GetField("active");
                    if (activeField != null) activeField.SetValue(comp, true);
                }
            }
        }

        public static void EnableEmission(Material material, Color emissionColor)
        {
            if (material == null) return;
            material.EnableKeyword("_EMISSION");
            SetColorSafe(material, "_EmissionColor", emissionColor);
        }

        public static void EnableInstancing(params Material[] materials)
        {
            foreach (Material mat in materials)
                if (mat != null)
                    mat.enableInstancing = true;
        }

        public static void SetColorSafe(Material mat, string property, Color value)
        {
            if (mat != null && mat.HasProperty(property))
                mat.SetColor(property, value);
        }

        public static void SetFloatSafe(Material mat, string property, float value)
        {
            if (mat != null && mat.HasProperty(property))
                mat.SetFloat(property, value);
        }

        public static void SetTextureSafe(Material mat, string property, Texture value)
        {
            if (mat != null && mat.HasProperty(property))
                mat.SetTexture(property, value);
        }

        public static void DeleteExistingAsset<T>(string path) where T : UnityEngine.Object
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);
        }
    }
}
