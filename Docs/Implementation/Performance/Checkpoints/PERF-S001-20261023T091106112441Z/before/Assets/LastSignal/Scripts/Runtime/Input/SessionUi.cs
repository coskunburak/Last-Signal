using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace LastSignal
{
    /// <summary>Extends SessionFlow screens and the existing uGUI views. No inventory/world authority.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class SessionUi : MonoBehaviour
    {
        SessionFlow flow;
        SessionInput input;
        Audio.AudioSettingsView audioView;
        AcceptanceHud hud;
        Inventory.UI.InventoryUI inventory;
        Shelter.ShelterStorageUI storage;
        GameObject canvas, settings, bindingsPanel, journal;
        Text bindingMessage, journalText;
        RectTransform cancelRebindRect;
        public bool PointerOverRebindCancel(Vector2 position) => cancelRebindRect && cancelRebindRect.gameObject.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint(cancelRebindRect, position);
        readonly List<Selectable> choices = new List<Selectable>();
        readonly List<Selectable> buffer = new List<Selectable>();
        readonly Dictionary<SessionScreen, GameObject> previous = new Dictionary<SessionScreen, GameObject>();
        readonly List<Text> bindingLabels = new List<Text>();
        readonly List<string> bindingPaths = new List<string>();
        readonly List<int> bindingIndices = new List<int>();
        SessionScreen shown = (SessionScreen)(-1);
        int page, revision = -1;
        float nextRefresh, nextJournal;
        GameObject lastSelected;
        WorldTime.WorldTimeSaveControls saves;
        Shelter.ShelterSite site;
        VehicleCargoView cargo;
        public bool Ready { get; private set; }
        public int SettingsPage => page;

        public void Initialize(SessionFlow owner)
        {
            flow = owner; input = flow.Controls;
            hud = FindAnyObjectByType<AcceptanceHud>();
            audioView = FindAnyObjectByType<Audio.AudioSettingsView>();
            inventory = FindAnyObjectByType<Inventory.UI.InventoryUI>();
            storage = FindAnyObjectByType<Shelter.ShelterStorageUI>();
            saves = FindAnyObjectByType<WorldTime.WorldTimeSaveControls>();
            site = flow.GetComponent<Shelter.ShelterSite>();
            canvas = new GameObject("S017 Session UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().sortingOrder = 50;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            settings = Panel("Ayarlar", canvas.transform, new Vector2(0, 0), new Vector2(1720, 900));
            Button("Ses", settings.transform, new Vector2(-600, 380), () => SetPage(0));
            Button("Bakış ve görünüm", settings.transform, new Vector2(-300, 380), () => SetPage(1));
            Button("Tuş atamaları", settings.transform, new Vector2(0, 380), () => SetPage(2));
            Button("Öğretimi sıfırla", settings.transform, new Vector2(300, 380), () => flow.GetComponent<FirstUseGuide>()?.ResetGuide());
            Button("Geri", settings.transform, new Vector2(600, 380), flow.CloseSettings);
            bindingsPanel = Panel("Tuş atamaları", settings.transform, new Vector2(0, -20), new Vector2(1450, 680));
            CreateBindings();
            settings.SetActive(false);
            journal = Panel("Günlük", canvas.transform, Vector2.zero, new Vector2(1000, 650));
            journalText = Label(journal.transform, "", new Vector2(0, 30), new Vector2(900, 450), 24);
            Button("Düşen sigortayı geri çağır", journal.transform, new Vector2(-200, -270), () => flow.GetComponent<Objectives.RelayMission>()?.Recover());
            Button("Geri", journal.transform, new Vector2(200, -270), () => flow.GetComponent<Objectives.RelayMission>()?.CloseJournal());
            journal.SetActive(false);
            if (hud && hud.MenuRoot)
            {
                Button("Ayarlar", hud.MenuRoot.transform, new Vector2(0, -175), flow.OpenSettings);
                Button("Oyundan çık", hud.MenuRoot.transform, new Vector2(0, -235), Quit);
            }
            cargo = gameObject.AddComponent<VehicleCargoView>(); cargo.Initialize(flow, canvas.transform);
            Ready = true;
        }
        void Quit() { flow.ReturnToMenu(); Application.Quit(); }
        public void SetPage(int value) { page = Mathf.Clamp(value, 0, 2); nextRefresh = 0; lastSelected = null; }

        void CreateBindings()
        {
            var scrollRoot = new GameObject("Binding list", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            var rect = scrollRoot.GetComponent<RectTransform>(); rect.SetParent(bindingsPanel.transform, false);
            rect.sizeDelta = new Vector2(1360, 530); rect.anchoredPosition = new Vector2(0, 45);
            scrollRoot.GetComponent<Image>().color = new Color(.05f, .06f, .08f);
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask)).GetComponent<RectTransform>();
            viewport.SetParent(rect, false); Stretch(viewport); viewport.GetComponent<Mask>().showMaskGraphic = false;
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport, false); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
            var scroll = scrollRoot.GetComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 35;
            int row = 0;
            foreach (var map in input.Actions.actionMaps)
            foreach (var action in map.actions)
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (!InputBindingPolicy.Editable(action, action.bindings[i])) continue;
                string path = map.name + "/" + action.name; int index = i;
                float y = -35 - row++ * 62;
                var label = Label(content, "", new Vector2(-230, y), new Vector2(780, 52), 21);
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, 1);
                bindingLabels.Add(label); bindingPaths.Add(path); bindingIndices.Add(index);
                var change = Button("Değiştir", content, new Vector2(290, y), () => input.BeginRebind(path, index));
                ((RectTransform)change.transform).anchorMin = ((RectTransform)change.transform).anchorMax = new Vector2(.5f, 1);
                var reset = Button("Varsayılan", content, new Vector2(570, y), () => input.ResetBinding(path, index));
                ((RectTransform)reset.transform).anchorMin = ((RectTransform)reset.transform).anchorMax = new Vector2(.5f, 1);
            }
            content.sizeDelta = new Vector2(0, row * 62 + 12);
            Button("Tüm tuşları sıfırla", bindingsPanel.transform, new Vector2(-450, -275), input.ResetBindings);
            cancelRebindRect = (RectTransform)Button("Dinlemeyi iptal et", bindingsPanel.transform, new Vector2(-130, -275), input.CancelRebind).transform;
            bindingMessage = Label(bindingsPanel.transform, "", new Vector2(350, -275), new Vector2(620, 72), 18);
        }

        void LateUpdate()
        {
            if (!Ready || !flow || !input.Actions) return;
            if (audioView)
            {
                AdoptSettingsPanel(audioView.settingsPanel);
                AdoptSettingsPanel(audioView.ControlsPanel);
            }
            settings.SetActive(flow.SettingsOpen); bindingsPanel.SetActive(flow.SettingsOpen && page == 2);
            journal.SetActive(flow.Screen == SessionScreen.Journal);
            if (journal.activeSelf && Time.unscaledTime >= nextJournal)
            { journalText.text = flow.GetComponent<Objectives.RelayMission>()?.Journal; nextJournal = Time.unscaledTime + .2f; }
            if (bindingMessage) bindingMessage.text = input.Message;
            if (revision != input.PresentationRevision)
            {
                revision = input.PresentationRevision;
                for (int i = 0; i < bindingLabels.Count; i++)
                {
                    var action = input.Actions.FindAction(bindingPaths[i], true);
                    bool gamepad = action.bindings[bindingIndices[i]].path.StartsWith("<Gamepad>/");
                    bindingLabels[i].text = ActionLabel(action.name, action.actionMap.name == "Vehicle") + " · " +
                        (gamepad ? "Gamepad" : "Klavye / fare") + " · " + action.GetBindingDisplayString(bindingIndices[i]);
                }
                nextRefresh = 0;
            }
            var system = EventSystem.current;
            if (!system) return;
            if (shown != flow.Screen)
            {
                if (lastSelected) previous[shown] = lastSelected;
                shown = flow.Screen; nextRefresh = 0;
                system.SetSelectedGameObject(null);
            }
            if (Time.unscaledTime >= nextRefresh) { RebuildChoices(); nextRefresh = Time.unscaledTime + .25f; }
            if (input.Listening) return;
            var selected = system.currentSelectedGameObject;
            if (!selected || !selected.activeInHierarchy || !choices.Contains(selected.GetComponent<Selectable>()))
            {
                GameObject target = null;
                if (previous.TryGetValue(shown, out var old) && old && old.activeInHierarchy && choices.Contains(old.GetComponent<Selectable>())) target = old;
                if (!target && choices.Count > 0) target = choices[0].gameObject;
                system.SetSelectedGameObject(target);
            }
            lastSelected = system.currentSelectedGameObject;
        }
        void AdoptSettingsPanel(GameObject root)
        {
            if (!root || root.transform.parent == settings.transform) return;
            var rect = (RectTransform)root.transform;
            rect.SetParent(settings.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(0, -30);
        }
        void AddRoot(GameObject root)
        {
            if (!root || !root.activeInHierarchy) return;
            buffer.Clear(); root.GetComponentsInChildren(false, buffer);
            foreach (var choice in buffer)
                if (choice.IsInteractable() && choice.gameObject.activeInHierarchy && !choices.Contains(choice)) choices.Add(choice);
        }
        void RebuildChoices()
        {
            choices.Clear();
            switch (flow.Screen)
            {
                case SessionScreen.MainMenu: case SessionScreen.Pause: case SessionScreen.Death:
                    if (hud) AddRoot(hud.MenuRoot);
                    // Save/load buttons can be siblings of the authored menu.
                    if (saves) { AddRoot(saves.SaveButtonRoot); AddRoot(saves.LoadButtonRoot); }
                    break;
                case SessionScreen.Inventory: if (flow.InventoryView) AddRoot(flow.InventoryView.Root); break;
                case SessionScreen.Storage: if (storage) AddRoot(storage.Root); break;
                case SessionScreen.ShelterProduction: if (site) AddRoot(site.Root); break;
                case SessionScreen.VehicleCargo: if (cargo) AddRoot(cargo.Root); break;
                case SessionScreen.Journal: AddRoot(journal); break;
                case SessionScreen.Settings:
                    AddRoot(settings);
                    if (audioView) { AddRoot(audioView.settingsPanel); AddRoot(audioView.ControlsPanel); }
                    break;
            }
            // Explicit directional geometry is stable across authored layouts, grids and two panes.
            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i]; if (!choice.GetComponent<UiFocus>()) choice.gameObject.AddComponent<UiFocus>();
                choice.navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnUp = Nearest(i, Vector2.up), selectOnDown = Nearest(i, Vector2.down),
                    selectOnLeft = choice is Slider ? null : Nearest(i, Vector2.left), selectOnRight = choice is Slider ? null : Nearest(i, Vector2.right) };
            }
        }
        Selectable Nearest(int index, Vector2 direction)
        {
            Vector2 origin = choices[index].transform.position;
            float score = float.PositiveInfinity; Selectable best = null;
            for (int i = 0; i < choices.Count; i++)
            {
                if (i == index) continue;
                Vector2 delta = (Vector2)choices[i].transform.position - origin;
                float forward = Vector2.Dot(delta, direction); if (forward < 1) continue;
                float perpendicular = Mathf.Abs(delta.x * direction.y - delta.y * direction.x);
                float candidate = forward + perpendicular * 3;
                if (candidate < score) { score = candidate; best = choices[i]; }
            }
            return best;
        }
        static string ActionLabel(string name, bool vehicle)
        {
            string label;
            switch (name)
            {
                case "Attack": label = "Saldırı"; break; case "Aim": label = "Nişan"; break;
                case "Reload": label = "Şarjör değiştir"; break; case "Interact": label = "Etkileşim"; break;
                case "Sprint": label = "Koş"; break; case "Crouch": label = "Çömel"; break;
                case "Jump": label = "Zıpla"; break; case "Inventory": label = "Envanter"; break;
                case "MeleeSlot": label = "Levye"; break; case "FirearmSlot": label = "Tüfek"; break;
                case "Journal": label = "Günlük"; break; case "SkipTutorial": label = "İpucunu atla"; break;
                case "Handbrake": label = "El freni"; break; case "Exit": label = "Araçtan çık"; break;
                case "Horn": label = "Korna"; break; case "Lights": label = "Farlar"; break;
                case "Ignition": label = "Kontak"; break; default: label = name; break;
            }
            return vehicle ? "Araç · " + label : label;
        }
        public static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        public static GameObject Panel(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false); rect.sizeDelta = size; rect.anchoredPosition = position;
            go.GetComponent<Image>().color = new Color(.025f, .035f, .045f, .98f); return go;
        }
        public static Text Label(Transform parent, string text, Vector2 position, Vector2 size, int fontSize = 20)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false); rect.sizeDelta = size; rect.anchoredPosition = position;
            var label = go.GetComponent<Text>(); label.text = text; label.font = ProductionUiFont.Resolve(); label.fontSize = fontSize;
            label.color = Color.white; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false; return label;
        }
        public static Button Button(string name, Transform parent, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            var go = Panel(name, parent, position, new Vector2(260, 46));
            go.GetComponent<Image>().color = new Color(.13f, .22f, .27f);
            var button = go.AddComponent<Button>(); button.onClick.AddListener(action);
            var colors = button.colors; colors.selectedColor = new Color(.45f, .8f, 1); colors.highlightedColor = colors.selectedColor; button.colors = colors;
            Stretch(Label(go.transform, name, Vector2.zero, Vector2.zero).rectTransform); return button;
        }
        void OnDestroy() { if (canvas) Destroy(canvas); }
    }
}
