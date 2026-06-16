using UnityEngine;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Persistent asset that stores the generator settings. Can be used as a preset.
    /// </summary>
    [CreateAssetMenu(fileName = "AAA_RuralWorldSettings", menuName = "AuraLite/Rural World Settings")]
    public class GenerationSettingsAsset : ScriptableObject
    {
        public GenerationSettings settings = new GenerationSettings();
    }
}
