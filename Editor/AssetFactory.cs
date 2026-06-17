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
                // Generate noise-based Normal Map with better quality
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float nx = x / (float)(width - 1);
                        float ny = y / (float)(height - 1);
                        float strength = 0.25f;
                        
                        // Multi-octave normal computation
                        float v0 = FBMForNormal(nx, ny, scale, 3);
                        float vx = FBMForNormal(nx + 0.005f, ny, scale, 3);
                        float vy = FBMForNormal(nx, ny + 0.005f, scale, 3);
                        
                        Vector3 normal = new Vector3(
                            (v0 - vx) * strength * scale,
                            (v0 - vy) * strength * scale,
                            1.0f
                        ).normalized;
                        
                        pixels[y * width + x] = new Color(
                            normal.x * 0.5f + 0.5f,
                            normal.y * 0.5f + 0.5f,
                            normal.z * 0.5f + 0.5f,
                            1f
                        );
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

        private static float FBMForNormal(float x, float y, float scale, int octaves)
        {
            float sum = 0f;
            float amp = 1f;
            float freq = scale;
            float maxVal = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Mathf.PerlinNoise(x * freq, y * freq) * amp;
                maxVal += amp;
                amp *= 0.5f;
                freq *= 2f;
            }
            return maxVal > 0f ? sum / maxVal : 0f;
        }

        /// <summary>
        /// Creates a procedural wood grain texture with visible rings and fiber lines.
        /// </summary>
        public static Texture2D CreateWoodGrainTexture(string path, int width, int height, Color baseColor, Color ringColor, float ringScale)
        {
            DeleteExistingAsset<Texture2D>(path);
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true)
            {
                name = Path.GetFileNameWithoutExtension(path),
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = x / (float)(width - 1);
                    float ny = y / (float)(height - 1);
                    
                    // Wood rings - concentric circles with distortion
                    float distX = nx - 0.5f + Mathf.PerlinNoise(ny * 8f + 1.3f, nx * 3f) * 0.12f;
                    float distY = ny + Mathf.PerlinNoise(nx * 5f + 7.1f, ny * 4f) * 0.06f;
                    float ring = Mathf.Sin(distX * ringScale * Mathf.PI * 2f) * 0.5f + 0.5f;
                    ring = Mathf.Pow(ring, 0.7f); // Sharpen rings slightly
                    
                    // Fiber lines along Y
                    float fiber = Mathf.PerlinNoise(nx * 40f, ny * 2f) * 0.15f;
                    
                    // Combine
                    Color c = Color.Lerp(baseColor, ringColor, ring * 0.6f + fiber);
                    
                    // Add subtle noise
                    float noise = Mathf.PerlinNoise(nx * 20f + 100f, ny * 20f + 200f) * 0.08f - 0.04f;
                    c += new Color(noise, noise * 0.8f, noise * 0.5f, 0f);
                    c.a = 0f;
                    pixels[y * width + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);
            AssetDatabase.CreateAsset(tex, path);
            return tex;
        }

        /// <summary>
        /// Creates a procedural brick/stone wall texture with mortar lines.
        /// </summary>
        public static Texture2D CreateBrickTexture(string path, int width, int height, Color brickColor, Color mortarColor, float brickScale)
        {
            DeleteExistingAsset<Texture2D>(path);
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true)
            {
                name = Path.GetFileNameWithoutExtension(path),
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[width * height];
            float brickW = brickScale;
            float brickH = brickScale * 0.5f;
            float mortarW = 0.02f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = x / (float)(width - 1);
                    float ny = y / (float)(height - 1);
                    
                    int row = Mathf.FloorToInt(ny / brickH);
                    float offset = (row % 2 == 0) ? 0f : brickW * 0.5f;
                    float localX = (nx + offset) % brickW;
                    float localY = ny % brickH;
                    
                    // Mortar check
                    bool isMortar = localX < mortarW || localY < mortarW;
                    
                    // Brick color variation
                    float brickNoise = Mathf.PerlinNoise(
                        Mathf.Floor(nx / brickW) * 3.7f + row * 1.3f,
                        row * 2.1f
                    ) * 0.3f;
                    
                    // Surface roughness
                    float roughness = Mathf.PerlinNoise(nx * 30f, ny * 30f) * 0.1f;
                    
                    Color c;
                    if (isMortar)
                    {
                        c = mortarColor + new Color(roughness, roughness, roughness, 0f);
                    }
                    else
                    {
                        c = brickColor * (0.85f + brickNoise) + new Color(roughness, roughness * 0.9f, roughness * 0.8f, 0f);
                    }
                    c.a = 0f;
                    pixels[y * width + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);
            AssetDatabase.CreateAsset(tex, path);
            return tex;
        }

        /// <summary>
        /// Creates a procedural roof tile texture with overlapping rows.
        /// </summary>
        public static Texture2D CreateRoofTileTexture(string path, int width, int height, Color tileColor, float tileScale)
        {
            DeleteExistingAsset<Texture2D>(path);
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true)
            {
                name = Path.GetFileNameWithoutExtension(path),
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[width * height];
            float tileW = tileScale;
            float tileH = tileScale * 0.6f;
            float overlap = 0.15f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = x / (float)(width - 1);
                    float ny = y / (float)(height - 1);
                    
                    int row = Mathf.FloorToInt(ny / tileH);
                    float offset = (row % 2 == 0) ? 0f : tileW * 0.5f;
                    float localY = (ny % tileH) / tileH;
                    
                    // Tile edge shadow
                    float shadow = smoothstep(overlap, overlap + 0.05f, localY);
                    float highlight = 1f - smoothstep(0.0f, 0.03f, localY) * 0.2f;
                    
                    // Per-tile color variation
                    float tileNoise = Mathf.PerlinNoise(
                        Mathf.Floor((nx + offset) / tileW) * 5.3f + row * 1.7f,
                        row * 3.1f
                    ) * 0.25f;
                    
                    // Weathering
                    float weather = Mathf.PerlinNoise(nx * 15f + 50f, ny * 15f + 70f) * 0.1f;
                    
                    Color c = tileColor * (0.8f + tileNoise + weather) * shadow * highlight;
                    c.a = 0f;
                    pixels[y * width + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);
            AssetDatabase.CreateAsset(tex, path);
            return tex;
        }

        private static float smoothstep(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
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
                bloom.intensity.Override(0.35f);
                bloom.threshold.Override(1.2f);
                bloom.scatter.Override(0.7f);
                bloom.tint.Override(new Color(1f, 0.95f, 0.9f));
            }

            profile.Add<Tonemapping>(true);
            if (profile.TryGet<Tonemapping>(out var tonemapping))
            {
                tonemapping.mode.Override(TonemappingMode.ACES);
            }

            profile.Add<ColorAdjustments>(true);
            if (profile.TryGet<ColorAdjustments>(out var colorAdjustments))
            {
                colorAdjustments.contrast.Override(12f);
                colorAdjustments.saturation.Override(8f);
                colorAdjustments.postExposure.Override(0.05f);
            }

            // Color Curves for cinematic look
            // URP 17 (Unity 6) changed the master/red/green/blue parameters to TextureCurveParameter.
            // AnimationCurve is no longer directly assignable, so we add the component with default
            // values rather than overriding curves here.
            profile.Add<ColorCurves>(true);

            // White Balance for warm sunlight feel
            profile.Add<WhiteBalance>(true);
            if (profile.TryGet<WhiteBalance>(out var whiteBalance))
            {
                whiteBalance.temperature.Override(15f); // Slightly warm
                whiteBalance.tint.Override(5f); // Slightly green/magenta
            }

            // Vignette for cinematic framing
            profile.Add<Vignette>(true);
            if (profile.TryGet<Vignette>(out var vignette))
            {
                vignette.intensity.Override(0.2f);
                vignette.smoothness.Override(0.5f);
                // URP 17 removed 'roundness' from Vignette - it's gone in Unity 6's URP package.
                vignette.color.Override(new Color(0f, 0f, 0f));
            }

            // Screen Space Ambient Occlusion
            AddVolumeComponentIfExists(profile, "UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion");

            // For Unity 6 Volumetric effects, use reflection to avoid compilation errors
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
