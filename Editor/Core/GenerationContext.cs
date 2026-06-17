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

    public class AssetRegistry
    {
        private Dictionary<string, Object> _assets = new Dictionary<string, Object>();

        public void Register(string key, Object asset) => _assets[key] = asset;
        public T Get<T>(string key) where T : Object
        {
            if (_assets.TryGetValue(key, out var asset)) return asset as T;
            return null;
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
    }
}
