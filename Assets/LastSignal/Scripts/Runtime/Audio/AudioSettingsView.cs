using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace LastSignal.Audio
{
    [DisallowMultipleComponent] public sealed class AudioSettingsView : MonoBehaviour
    {
        public ProductionAudio audioOwner;
        public SessionFlow flow;
        public GameObject settingsPanel;
        public Slider[] sliders;
        public Toggle captions;
        public Text captionText;
        GameObject controlsPanel;
        public GameObject ControlsPanel => controlsPanel;
        readonly Dictionary<CanvasScaler, Vector2> baseResolutions = new Dictionary<CanvasScaler, Vector2>();
        bool dirty;
        float saveAt;
        void Start()
        {
            for(int i=0;i<sliders.Length;i++)
            {
                int bus=i; sliders[i].SetValueWithoutNotify(audioOwner.Preferences[(AudioBus)i]);
                sliders[i].onValueChanged.AddListener(v=>SetLevel(bus,v));
            }
            captions.SetIsOnWithoutNotify(audioOwner.Preferences.Captions);
            captions.onValueChanged.AddListener(SetCaptions);
            CreateControlPanel();
            ApplyUiScale();
        }
        void SetLevel(int bus,float value)
        { audioOwner.Preferences[(AudioBus)bus]=value; audioOwner.ApplyPreferences(); Changed(); }
        void SetCaptions(bool value) { audioOwner.Preferences.Captions=value; Changed(); }
        void SetFov(float value)
        {
            audioOwner.Preferences.FovDegrees = value;
            ApplyLook(); Changed();
        }
        void SetMouseSensitivity(float value)
        {
            audioOwner.Preferences.MouseSensitivity = value;
            ApplyLook(); Changed();
        }
        void SetUiScale(float value)
        {
            audioOwner.Preferences.UiScale = value;
            ApplyUiScale(); Changed();
        }
        void ApplyLook()
        {
            var player = flow ? flow.Player : null;
            var look = player ? player.GetComponent<FirstPersonLook>() : null;
            if (!look) return;
            look.ApplyPreferences(audioOwner.Preferences);
        }
        public void RefreshUiScale() => ApplyUiScale();
        void ApplyUiScale()
        {
            foreach (var scaler in Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include))
            {
                if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) continue;
                if (!baseResolutions.TryGetValue(scaler, out var resolution))
                {
                    resolution = scaler.referenceResolution;
                    baseResolutions.Add(scaler, resolution);
                }
                scaler.referenceResolution = resolution / audioOwner.Preferences.UiScale;
            }
        }
        static RectTransform Rect(GameObject go, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }
        Text Label(string name, Transform parent, Font font, Vector2 position, Vector2 size, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Rect(go, parent, position, size);
            var label = go.AddComponent<Text>();
            label.font = font;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            return label;
        }
        Slider Control(string name, Transform parent, Font font, int row, float min, float max, float value,
            System.Func<float, string> format, UnityEngine.Events.UnityAction<float> changed)
        {
            int top = -48 - row * 68;
            var label = Label(name + " değeri", parent, font, new Vector2(28, top), new Vector2(364, 30), 19);
            label.text = format(value);
            var go = new GameObject(name, typeof(RectTransform));
            var rect = Rect(go, parent, new Vector2(30, top - 36), new Vector2(360, 26));
            var slider = go.AddComponent<Slider>();
            var track = new GameObject("İz", typeof(RectTransform));
            var trackRect = Rect(track, rect, new Vector2(0, -10), new Vector2(360, 6));
            track.AddComponent<Image>().color = new Color(.28f, .36f, .4f);
            var fill = new GameObject("Dolgu", typeof(RectTransform));
            var fillRect = Rect(fill, trackRect, Vector2.zero, new Vector2(360, 6));
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            fill.AddComponent<Image>().color = new Color(.36f, .78f, .88f);
            var handle = new GameObject("Tutacak", typeof(RectTransform));
            var handleRect = Rect(handle, rect, new Vector2(0, -2), new Vector2(18, 26));
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.minValue = min; slider.maxValue = max;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v => { label.text = format(v); changed(v); });
            return slider;
        }
        void CreateControlPanel()
        {
            if (!settingsPanel || !audioOwner) return;
            var go = new GameObject("Kontrol ve erişilebilirlik ayarları", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(settingsPanel.transform.parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(250, 0);
            rect.sizeDelta = new Vector2(420, 640);
            go.AddComponent<Image>().color = new Color(.025f, .035f, .045f, .96f);
            controlsPanel = go;
            var sample = settingsPanel.GetComponentInChildren<Text>(true);
            var font = sample && sample.font ? sample.font : ProductionUiFont.Resolve();
            Label("Başlık", rect, font, new Vector2(28, -13), new Vector2(364, 40), 23).text = "Kontroller ve görünüm";
            var preferences = audioOwner.Preferences;
            Control("Görüş açısı", rect, font, 0, 60, 100, preferences.FovDegrees,
                v => "Görüş açısı: " + Mathf.RoundToInt(v) + "°", SetFov);
            Control("Fare duyarlılığı", rect, font, 1, .02f, .5f, preferences.MouseSensitivity,
                v => "Fare duyarlılığı: " + v.ToString("0.00") + "°/piksel", SetMouseSensitivity);
            Control("Arayüz ölçeği", rect, font, 2, .8f, 1.3f, preferences.UiScale,
                v => "Arayüz ölçeği: %" + Mathf.RoundToInt(v * 100), SetUiScale);
            Control("Gamepad yatay hız", rect, font, 3, 30, 300, preferences.GamepadYaw,
                v => "Gamepad yatay: " + Mathf.RoundToInt(v) + "°/sn",
                v => { preferences.GamepadYaw = v; ApplyLook(); Changed(); });
            Control("Gamepad dikey hız", rect, font, 4, 30, 300, preferences.GamepadPitch,
                v => "Gamepad dikey: " + Mathf.RoundToInt(v) + "°/sn",
                v => { preferences.GamepadPitch = v; ApplyLook(); Changed(); });
            Control("Gamepad bakış ölü bölgesi", rect, font, 5, .15f, .4f, preferences.GamepadDeadzone,
                v => "Bakış ölü bölgesi: %" + Mathf.RoundToInt(v * 100),
                v => { preferences.GamepadDeadzone = v; ApplyLook(); Changed(); });
            Control("Gamepad yatay ters", rect, font, 6, 0, 1, preferences.GamepadInvertX ? 1 : 0,
                v => "Yatay ters: " + (v >= .5f ? "Açık" : "Kapalı"),
                v => { preferences.GamepadInvertX = v >= .5f; ApplyLook(); Changed(); }).wholeNumbers = true;
            Control("Gamepad dikey ters", rect, font, 7, 0, 1, preferences.GamepadInvertY ? 1 : 0,
                v => "Dikey ters: " + (v >= .5f ? "Açık" : "Kapalı"),
                v => { preferences.GamepadInvertY = v >= .5f; ApplyLook(); Changed(); }).wholeNumbers = true;
            Label("Kayıt bilgi", rect, font, new Vector2(28, -600), new Vector2(364, 34), 16).text =
                "Tercihler otomatik kaydedilir.";
        }
        void Changed() { dirty=true; saveAt=Time.unscaledTime+.5f; }
        void Flush() { if(dirty && audioOwner) { audioOwner.Preferences.Save(); dirty=false; } }
        void OnDisable() => Flush();
        void OnApplicationPause(bool pause) { if(pause) Flush(); }
        void OnApplicationQuit() => Flush();
        void Update()
        {
            bool managed = flow && flow.UserInterface && flow.UserInterface.Ready;
            settingsPanel.SetActive(managed ? flow.SettingsOpen && flow.UserInterface.SettingsPage == 0 :
                flow && (flow.Screen == SessionScreen.MainMenu || flow.Screen == SessionScreen.Pause));
            if (controlsPanel) controlsPanel.SetActive(managed ? flow.SettingsOpen && flow.UserInterface.SettingsPage == 1 : settingsPanel.activeSelf);
            captionText.text=audioOwner.Preferences.Captions && audioOwner.Active?audioOwner.Caption:"";
            if(dirty && Time.unscaledTime>=saveAt) Flush();
        }
    }
}
