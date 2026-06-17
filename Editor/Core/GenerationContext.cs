using System.Threading;
using AuraLiteWorldGenerator.Runtime;
using UnityEngine;
using System.Collections.Generic;

namespace AuraLiteWorldGenerator.Editor.Core
{
    public class GenerationContext
    {
        public int Seed { get; private set; }
        public SeededRandom Random { get; private set; }
        public GenerationSettings Settings { get; private set; }
        public WorldLayout Layout { get; set; }
        public CancellationToken CancellationToken { get; private set; }
        public IServiceContainer Services { get; private set; }
        
        public AssetRegistry Assets { get; private set; }
        public SceneHierarchy Hierarchy { get; private set; }

        public GenerationContext(int seed, GenerationSettings settings, IServiceContainer services, CancellationToken ct)
        {
            Seed = seed;
            Settings = settings;
            Random = new SeededRandom(seed);
            Services = services;
            CancellationToken = ct;
            Assets = new AssetRegistry();
            Hierarchy = new SceneHierarchy();
        }
    }

    public class AssetRegistryAssetProvider : IAssetProvider
    {
        private AssetRegistry _registry;

        public AssetRegistryAssetProvider(AssetRegistry registry)
        {
            _registry = registry;
        }

        public Material GetMaterial(string key) => _registry.Get<Material>(key);
        public Mesh GetMesh(string key) => _registry.Get<Mesh>(key);
        public Texture2D GetTexture(string key) => _registry.Get<Texture2D>(key);
        public T GetAsset<T>(string key) where T : Object => _registry.Get<T>(key);
    }

    /// <summary>
    /// Type-erased key/value store. Used to carry legacy data (BuildContext,
    /// TerrainGrid, raw strings, etc.) alongside UnityEngine.Object assets.
    /// Generic Get&lt;T&gt; uses pattern-match casting instead of a constraint so
    /// non-Unity types can be retrieved as well.
    /// </summary>
    public class AssetRegistry
    {
        private Dictionary<string, object> _assets = new Dictionary<string, object>();

        public void Register(string key, object asset) => _assets[key] = asset;
        public T Get<T>(string key)
        {
            if (_assets.TryGetValue(key, out var asset) && asset is T typed)
                return typed;
            return default(T);
        }
        public bool TryGet<T>(string key, out T value)
        {
            if (_assets.TryGetValue(key, out var asset) && asset is T typed)
            {
                value = typed;
                return true;
            }
            value = default(T);
            return false;
        }
    }

    public class SceneHierarchy
    {
        public GameObject Root { get; set; }
        public GameObject TerrainRoot { get; set; }
        public GameObject VillageRoot { get; set; }
        public GameObject RoadsRoot { get; set; }
        public GameObject ForestRoot { get; set; }
        public GameObject WaterRoot { get; set; }
        public GameObject FieldsRoot { get; set; }
    }
}
