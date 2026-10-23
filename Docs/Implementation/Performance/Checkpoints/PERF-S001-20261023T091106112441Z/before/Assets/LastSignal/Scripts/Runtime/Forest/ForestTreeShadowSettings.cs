using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastSignal.Forest
{
    /// <summary>Player-selected tree shadow casting for the isolated forest; leaves geometry and collision intact.</summary>
    public sealed class ForestTreeShadowSettings : MonoBehaviour
    {
        const string PreferenceKey = "LastSignal.Forest.TreeShadows";
        [System.Serializable] struct Entry { public Renderer renderer; public ShadowCastingMode original; }
        [SerializeField, HideInInspector] List<Entry> trees = new List<Entry>();
        [SerializeField, HideInInspector] ForestPrototypeExplorer explorer;
        [SerializeField, HideInInspector] ForestPrototypeCapture capture;
        [field: SerializeField] public bool TreeShadowsEnabled { get; private set; } = true;
        public static bool DiagnosticOverrideActive { get; set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => DiagnosticOverrideActive = false;

        public void Initialize(ForestPrototypeExplorer owner)
        {
            if (explorer) return;
            if (!owner || !owner.state || !owner.state.generated) return;
            explorer = owner; capture = GetComponent<ForestPrototypeCapture>();
            var state = owner.state;
            var vegetation = state.generated.transform.Find("Vegetation");
            var hero = vegetation ? vegetation.Find("HeroTrees") : null;
            var distant = state.generated.transform.Find("Non-playable distant dressing");
            if (!state.canopy || !hero || !distant)
            {
                Debug.LogWarning("Tree shadow setting unavailable: authored forest roots missing.");
                explorer = null; return;
            }
            var seen = new HashSet<Renderer>();
            void Add(Transform root)
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    if (seen.Add(renderer)) trees.Add(new Entry { renderer = renderer, original = renderer.shadowCastingMode });
            }
            Add(state.canopy); Add(hero);
            // Keep the distant ground apron untouched.
            foreach (var group in distant.GetComponentsInChildren<LODGroup>(true)) Add(group.transform);
            TreeShadowsEnabled = PlayerPrefs.GetInt(PreferenceKey, 1) != 0;
            Apply();
        }
        void Apply()
        {
            foreach (var entry in trees)
                if (entry.renderer) entry.renderer.shadowCastingMode = TreeShadowsEnabled ? entry.original : ShadowCastingMode.Off;
        }
        public void SetTreeShadows(bool enabled)
        {
            if (!explorer || DiagnosticOverrideActive || (capture && capture.enabled)) return;
            TreeShadowsEnabled = enabled; Apply();
            PlayerPrefs.SetInt(PreferenceKey, enabled ? 1 : 0); PlayerPrefs.Save();
        }
        void OnGUI()
        {
            if (!explorer || !explorer.input || explorer.input.GameplayActive) return;
            GUI.Box(new Rect(20, 20, 340, 160), "Grafik ayarları");
            bool previous = GUI.enabled;
            GUI.enabled = previous && !DiagnosticOverrideActive && !(capture && capture.enabled);
            bool choice = GUI.Toggle(new Rect(40, 58, 290, 28), TreeShadowsEnabled, "Ağaç gölgeleri");
            if (choice != TreeShadowsEnabled) SetTreeShadows(choice);
            GUI.enabled = previous;
            GUI.Label(new Rect(40, 95, 300, 25), "Tercihiniz otomatik kaydedilir.");
            GUI.Label(new Rect(40, 125, 300, 25), "Oyuna dönmek için Escape.");
        }
        void OnDestroy()
        {
            foreach (var entry in trees)
                if (entry.renderer) entry.renderer.shadowCastingMode = entry.original;
        }
    }
}
