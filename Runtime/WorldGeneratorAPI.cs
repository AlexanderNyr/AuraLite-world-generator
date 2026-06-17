using System.Threading;
using System.Threading.Tasks;
using AuraLiteWorldGenerator.Editor.Core;
using AuraLiteWorldGenerator.Editor.Modules;
using UnityEngine;

namespace AuraLiteWorldGenerator.Runtime
{
    public class WorldGeneratorAPI : MonoBehaviour
    {
        // This would need to handle runtime-specific modules 
        // because some editor modules use AssetDatabase.
        
        public async Task GenerateAsync(int seed, GenerationSettings settings)
        {
            // Implementation of runtime generation pipeline
        }
    }
}
