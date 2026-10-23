using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.RenderingQuality
{
    /// <summary>Shared staged settings view. Closing discards changes that have not been applied.</summary>
    public sealed class GraphicsSettingsPresenter : MonoBehaviour
    {
        GraphicsSettingsManager manager;
        GraphicsSettingsState draft;
        Text summary, message;
        Button global, trees, distance, lod, fog, ao, apply, reset, restore;
        readonly System.Collections.Generic.List<Button> presets = new System.Collections.Generic.List<Button>();
        public GraphicsSettingsState Draft => draft.Copy();
        public void Initialize(GraphicsSettingsManager owner)
        {
            manager = owner; draft = manager.State;
            summary = SessionUi.Label(transform, "", new Vector2(0, 265), new Vector2(1300, 90), 22);
            for (int i = 0; i < 4; i++)
            {
                var preset = (GraphicsPreset)i;
                presets.Add(SessionUi.Button(preset.ToString(), transform, new Vector2(-450 + i * 300, 180), () => SelectPreset(preset)));
            }
            global = Control(-330, 90, () => Edit(s => s.globalShadows = (ShadowQuality)(((int)s.globalShadows + 1) % 4)));
            trees = Control(330, 90, () => Edit(s => s.treeShadows = (ShadowQuality)(((int)s.treeShadows + 1) % 4)));
            distance = Control(-330, 15, () => Edit(s => s.shadowDistance = s.shadowDistance >= 160 ? 10 : s.shadowDistance + 10));
            lod = Control(330, 15, () => Edit(s => s.lodBias = s.lodBias >= 1.5f ? .5f : Mathf.Min(1.5f, Mathf.Round((s.lodBias + .1f) * 100) / 100)));
            fog = Control(-330, -60, () => Edit(s => s.volumetricFog = !s.volumetricFog));
            ao = Control(330, -60, () => Edit(s => s.ambientOcclusion = !s.ambientOcclusion));
            apply = SessionUi.Button("Uygula ve kaydet", transform, new Vector2(-300, -155), () => ApplyDraft());
            reset = SessionUi.Button("Varsayılan: Low", transform, new Vector2(0, -155), () => { if (manager.ResetDefaults()) Reload(); else Refresh(); });
            SessionUi.Button("Değişiklikleri geri al", transform, new Vector2(300, -155), Reload);
            restore = SessionUi.Button("Özgün sahne (geçici)", transform, new Vector2(0, -210), () => { manager.RestoreAuthored(); Refresh(); });
            message = SessionUi.Label(transform, "", new Vector2(0, -285), new Vector2(1300, 95), 17);
            manager.Changed += Reload; Refresh();
        }
        Button Control(float x, float y, UnityEngine.Events.UnityAction action)
        {
            var b = SessionUi.Button("", transform, new Vector2(x, y), action);
            ((RectTransform)b.transform).sizeDelta = new Vector2(590, 54); return b;
        }
        public void SelectPreset(GraphicsPreset preset) { draft = manager.Profiles.Get(preset); Refresh(); }
        public void Edit(System.Action<GraphicsSettingsState> edit) { edit(draft); draft.preset = GraphicsPreset.Custom; Refresh(); }
        public bool ApplyDraft() { bool result = manager.Apply(draft); Refresh(); return result; }
        public void Reload() { if (!manager) return; draft = manager.State; Refresh(); }
        void OnEnable() => Reload();
        bool locked;
        void Update()
        {
            if (manager && locked != (manager.ChangesLocked || manager.DiagnosticActive)) Refresh();
        }
        static void Caption(Button b, string text) => b.GetComponentInChildren<Text>().text = text;
        void Refresh()
        {
            if (!summary || !message) return;
            locked = manager.ChangesLocked || manager.DiagnosticActive;
            bool available = manager.Adapter.Supported && !locked;
            foreach (var b in presets) b.interactable = available;
            global.interactable = trees.interactable = distance.interactable = lod.interactable = apply.interactable = reset.interactable = restore.interactable = available;
            fog.interactable = available && manager.Adapter.FogSupported;
            ao.interactable = available && manager.Adapter.AoSupported;
            var current = manager.State;
            summary.text = "Grafik • Etkin: " + (manager.Suspended ? "Özgün sahne (geçici)" : current.preset.ToString()) +
                " • Genel gölge: " + current.globalShadows + " • Ağaç: " + current.treeShadows + "\nSeçim: " + draft.preset + " — değişiklikleri Uygula ile kaydedin";
            Caption(global, "Genel gölge: " + draft.globalShadows);
            Caption(trees, "Ağaç gölgesi: " + draft.treeShadows);
            Caption(distance, "Gölge mesafesi: " + draft.shadowDistance + " m (+10)");
            Caption(lod, "LOD ayrıntısı: " + draft.lodBias.ToString("0.00") + " (+0.10)");
            Caption(fog, "Hacimsel sis: " + (manager.Adapter.FogSupported ? (draft.volumetricFog ? "Açık" : "Kapalı") : "Desteklenmiyor"));
            Caption(ao, "Ortam gölgeleme (SSAO): " + (manager.Adapter.AoSupported ? (draft.ambientOcclusion ? "Açık" : "Kapalı") : "Desteklenmiyor"));
            message.text = (locked ? "Performans kaydı / tanılama sırasında ayarlar kilitli.\n" : "") +
                (draft.globalShadows == ShadowQuality.Off ? "Genel gölge kapalıyken ağaç seçimi saklanır; gölge çizilmez.\n" : "") +
                "Ağaç Low: " + manager.Profiles.lowTreeDistance + " m • Medium: " + manager.Profiles.mediumTreeDistance + " m • High: özgün gölgeler. Geçiş: +" + manager.Profiles.distanceHysteresis + " m.\n" + manager.Message + " " + manager.Adapter.Status;
        }
        void OnDestroy() { if (manager) manager.Changed -= Reload; }
    }
}
