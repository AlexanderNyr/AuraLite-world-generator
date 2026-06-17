using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using AuraLiteWorldGenerator.Editor; // Shared assembly types

namespace AuraLiteWorldGenerator.Runtime
{
    public class WorldGeneratorAPI : MonoBehaviour
    {
        public async Task GenerateAsync(int seed, GenerationSettings settings, CancellationToken cancellationToken = default)
        {
            // Initialize SeededRandom
            var random = new SeededRandom(seed);
            
            // In a real runtime scenario, this would load a simplified pipeline,
            // or use Addressables/Resources to load assets instead of AssetDatabase.
            // For now, we'll just delay to simulate work and ensure it compiles in player.
            
            await Task.Delay(100, cancellationToken);
            
            Debug.Log($"[Runtime] World generated with seed {seed}");
        }
    }
}
