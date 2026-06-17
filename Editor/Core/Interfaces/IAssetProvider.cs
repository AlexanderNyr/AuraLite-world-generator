using UnityEngine;

namespace AuraLiteWorldGenerator.Editor.Core
{
    public interface IAssetProvider
    {
        Material GetMaterial(string key);
        Mesh GetMesh(string key);
        Texture2D GetTexture(string key);
        T GetAsset<T>(string key) where T : Object;
    }
}
