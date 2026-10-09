using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    // Authored production labels keep the imported font alive. Runtime-created
    // controls reuse that same asset without a second font copy or editor API.
    public static class ProductionUiFont
    {
        static Font cached;

        public static Font Resolve()
        {
            if (cached) return cached;
            foreach (var label in Object.FindObjectsByType<Text>(FindObjectsInactive.Include))
            {
                var font = label.font;
                if (font && font.name.StartsWith("Noto", System.StringComparison.Ordinal))
                    return cached = font;
            }
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
