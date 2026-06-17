using System;
using System.Collections.Generic;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor.Core
{
    [CreateAssetMenu(fileName = "MaterialPalette", menuName = "AuraLite/Material Palette")]
    public class MaterialPalette : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string Key;
            public Color Color;
        }

        public List<Entry> Entries = new List<Entry>();

        public Color GetColor(string key, Color defaultColor = default)
        {
            var entry = Entries.Find(e => e.Key == key);
            return entry != null ? entry.Color : defaultColor;
        }
    }
}
