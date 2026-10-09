using System;
using UnityEngine;

namespace LastSignal
{
    [CreateAssetMenu(menuName = "Last Signal/Input Glyph Catalog")]
    public sealed class InputGlyphCatalog : ScriptableObject
    {
        [Serializable] public struct Entry
        {
            public InputDeviceFamily family;
            public string path;
            public Texture2D texture;
        }
        public Entry[] entries = Array.Empty<Entry>();
        public Texture2D Resolve(InputDeviceFamily family, string effectivePath)
        {
            foreach (var entry in entries)
                if (entry.family == family && string.Equals(entry.path, effectivePath, StringComparison.OrdinalIgnoreCase)) return entry.texture;
            return null;
        }
    }
}
