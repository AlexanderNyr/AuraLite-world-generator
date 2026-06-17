using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AuraLiteWorldGenerator.Runtime
{
    /// <summary>
    /// Runtime-safe API for triggering world generation.
    /// In a full runtime scenario, this would load pre-generated layout data (JSON)
    /// and instantiate from Addressables/Resources instead of using AssetDatabase.
    /// </summary>
    [AddComponentMenu("AuraLite/World Generator/API")]
    public class WorldGeneratorAPI : MonoBehaviour
    {
        [Header("Generation Settings")]
        public int seed = 240615;
        [Tooltip("If true, the system will attempt runtime generation using Resources.")]
        public bool generateOnStart;
        [Tooltip("JSON layout file in a Resources folder (e.g. \"GeneratedVillageScene/layout\").")]
        public string layoutResourcePath = "";

        public bool IsGenerating { get; private set; }
        public float Progress { get; private set; }

        private CancellationTokenSource _cts;

        private void Start()
        {
            if (generateOnStart)
                GenerateAsync(seed).ContinueWith(_ => { });
        }

        private void OnDestroy()
        {
            Cancel();
        }

        public void Cancel()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        /// <summary>
        /// Generates or loads a world at runtime.
        /// For a real runtime pipeline, implement IRuntimeWorldBuilder and register it.
        /// </summary>
        public async Task GenerateAsync(int worldSeed, CancellationToken externalCt = default)
        {
            if (IsGenerating)
            {
                Debug.LogWarning("[AuraLite] Generation already in progress.");
                return;
            }

            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
            var ct = _cts.Token;
            IsGenerating = true;
            Progress = 0f;

            try
            {
                // Phase 1: Load or compute layout
                Progress = 0.1f;
                await Task.Delay(50, ct); // Yield to main thread

                WorldLayoutData layout = null;
                if (!string.IsNullOrEmpty(layoutResourcePath))
                {
                    var textAsset = Resources.Load<TextAsset>(layoutResourcePath);
                    if (textAsset != null)
                    {
                        layout = JsonUtility.FromJson<WorldLayoutData>(textAsset.text);
                        Debug.Log($"[AuraLite] Layout loaded from Resources/{layoutResourcePath}");
                    }
                    else
                    {
                        Debug.LogWarning($"[AuraLite] Layout not found at Resources/{layoutResourcePath}, using defaults.");
                    }
                }

                if (layout == null)
                {
                    layout = CreateDefaultLayout(worldSeed);
                }

                // Phase 2: Instantiate from Resources/Addressables
                Progress = 0.3f;
                await InstantiateWorldAsync(layout, ct);

                // Phase 3: Post-processing (lighting, weather, day/night)
                Progress = 0.8f;
                SetupRuntimeComponents();

                Progress = 1.0f;
                Debug.Log($"[AuraLite] Runtime world generation complete with seed {worldSeed}.");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[AuraLite] Generation cancelled.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AuraLite] Generation failed: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                IsGenerating = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private WorldLayoutData CreateDefaultLayout(int worldSeed)
        {
            var layout = new WorldLayoutData
            {
                seed = worldSeed,
                worldSizeMeters = 6400f,
                villageCenterX = 2432f,
                villageCenterZ = 2688f,
                waterLevel = 12.4f
            };
            return layout;
        }

        private async Task InstantiateWorldAsync(WorldLayoutData layout, CancellationToken ct)
        {
            // In a full implementation, this would:
            // 1. Load terrain tile prefabs from Addressables
            // 2. Instantiate building prefabs from Addressables  
            // 3. Place road/river meshes
            // 4. Apply biome-specific materials
            await Task.Delay(100, ct);
        }

        private void SetupRuntimeComponents()
        {
            if (FindAnyObjectByType<DayNightCycle>() == null)
            {
                var dnGO = new GameObject("DayNightCycle");
                dnGO.AddComponent<DayNightCycle>();
            }

            if (FindAnyObjectByType<WeatherSystem>() == null)
            {
                var wsGO = new GameObject("WeatherSystem");
                wsGO.AddComponent<WeatherSystem>();
            }
        }

        /// <summary>
        /// Lightweight serializable layout data for runtime use.
        /// Mirrors the Editor WorldLayout but without Unity type dependencies.
        /// </summary>
        [System.Serializable]
        public class WorldLayoutData
        {
            public int seed;
            public float worldSizeMeters;
            public float villageCenterX;
            public float villageCenterZ;
            public float waterLevel;
            public float terrainHeightMeters;
        }
    }
}
