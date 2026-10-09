using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;

namespace LastSignal
{
    public enum InputDeviceFamily { KeyboardMouse, XboxStyle, GenericGamepad }

    /// <summary>Scene-owned device, prompt, UI action and rebind coordination. World saves never own this state.</summary>
    [DefaultExecutionOrder(-250), DisallowMultipleComponent]
    public sealed class SessionInput : MonoBehaviour
    {
        public InputActionAsset Actions { get; private set; }
        public InputDeviceFamily Family { get; private set; }
        public int PresentationRevision { get; private set; }
        public bool Listening => operation != null;
        public string Message { get; private set; } = "";
        public event Action PresentationChanged;
        SessionFlow flow;
        Audio.AudioPreferences preferences;
        public Audio.AudioPreferences Preferences => preferences;
        PlayerInputReader reader;
        GameObject observedPlayer;
        InputActionMap uiMap, playerMap, vehicleMap;
        InputSystemUIInputModule module;
        InputActionAsset originalUiAsset, sourceAsset;
        bool originalModuleEnabled;
        readonly List<InputActionReference> references = new List<InputActionReference>();
        InputActionRebindingExtensions.RebindingOperation operation;
        string previousOverrides;
        InputDevice activeDevice;
        float lastSwitch;
        readonly Dictionary<string, string> labels = new Dictionary<string, string>();
        bool initialized, applicationFocused = true;
        SessionScreen uiScreen = (SessionScreen)(-1);
        bool uiNeutralRequired = true;

        public void Initialize(SessionFlow owner, InputActionAsset source)
        {
            if (initialized || !source) return;
            flow = owner; sourceAsset = source;
            preferences = flow.GetComponent<Audio.ProductionAudio>()?.Preferences;
            if (preferences == null) { preferences = new Audio.AudioPreferences(); preferences.Load(); }
            Actions = Instantiate(source);
            uiMap = Actions.FindActionMap("UI", true); playerMap = Actions.FindActionMap("Player", true); vehicleMap = Actions.FindActionMap("Vehicle", false);
            if (!InputBindingPolicy.LoadSafely(Actions, preferences.BindingOverrides, out var message))
            { preferences.BindingOverrides = ""; preferences.Save(); Message = message; }
            var system = FindAnyObjectByType<EventSystem>();
            if (system)
            {
                module = system.GetComponent<InputSystemUIInputModule>();
                if (module)
                {
                    originalUiAsset = module.actionsAsset;
                    originalModuleEnabled = module.enabled;
                    module.actionsAsset = Actions;
                    module.point = Reference("UI/Point"); module.leftClick = Reference("UI/Click");
                    module.rightClick = Reference("UI/RightClick"); module.middleClick = Reference("UI/MiddleClick");
                    module.scrollWheel = Reference("UI/ScrollWheel"); module.move = Reference("UI/Navigate");
                    module.submit = Reference("UI/Submit"); module.cancel = Reference("UI/Cancel");
                }
            }
            Actions.FindAction("UI/Cancel", true).performed += OnBack;
            InputSystem.onAfterUpdate += ObserveDevices;
            InputSystem.onDeviceChange += DeviceChanged;
            initialized = true;
            Invalidate();
        }

        InputActionReference Reference(string path)
        { var reference = InputActionReference.Create(Actions.FindAction(path, true)); references.Add(reference); return reference; }

        void OnBack(InputAction.CallbackContext context)
        {
            if (Listening) { CancelRebind(); return; }
            if (flow.SettingsOpen) flow.CloseSettings();
            else flow.TogglePause();
        }

        void Update()
        {
            if (!initialized) return;
            if (observedPlayer != flow.Player)
            {
                observedPlayer = flow.Player;
                if (reader) reader.ExternalUiOwner = false;
                reader = observedPlayer ? observedPlayer.GetComponent<PlayerInputReader>() : null;
                if (reader) { reader.ExternalUiOwner = true; reader.LoadBindingOverrides(preferences.BindingOverrides); }
            }
            bool ui = flow.Screen != SessionScreen.Gameplay &&
                (applicationFocused || InputSystem.settings.backgroundBehavior == InputSettings.BackgroundBehavior.IgnoreFocus);
            if (uiScreen != flow.Screen) { uiScreen = flow.Screen; uiNeutralRequired = true; }
            if (uiNeutralRequired)
            {
                bool held = Keyboard.current != null && (Keyboard.current.enterKey.isPressed || Keyboard.current.escapeKey.isPressed);
                held |= Mouse.current != null && Mouse.current.leftButton.isPressed;
                foreach (var pad in Gamepad.all) held |= pad.buttonSouth.isPressed || pad.buttonEast.isPressed || pad.startButton.isPressed || pad.dpad.ReadValue().sqrMagnitude > .1f || pad.leftStick.ReadValue().sqrMagnitude > .0625f;
                uiNeutralRequired = held;
                ui = false; // The neutral frame itself cannot submit a button.
            }
            if (module && module.enabled != (ui && !Listening)) module.enabled = ui && !Listening;
            var map = uiMap;
            if (ui && !Listening) { if (!map.enabled) map.Enable(); }
            else if (map.enabled) map.Disable();
            // These maps supply binding data only. PlayerInputReader owns gameplay execution.
            playerMap.Disable();
            vehicleMap?.Disable();
        }

        public static InputDeviceFamily Classify(InputDevice device) => device is Gamepad
            ? (device.layout.IndexOf("XInput", StringComparison.OrdinalIgnoreCase) >= 0 ||
               device.layout.IndexOf("Xbox", StringComparison.OrdinalIgnoreCase) >= 0
                ? InputDeviceFamily.XboxStyle : InputDeviceFamily.GenericGamepad)
            : InputDeviceFamily.KeyboardMouse;

        public static bool Meaningful(Gamepad pad) => pad != null && pad.enabled &&
            (pad.leftStick.ReadValue().sqrMagnitude > .0625f || pad.rightStick.ReadValue().sqrMagnitude > .0625f ||
             pad.dpad.ReadValue().sqrMagnitude > .5f || pad.leftTrigger.ReadValue() > .55f || pad.rightTrigger.ReadValue() > .55f ||
             pad.buttonSouth.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame ||
             pad.buttonEast.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame ||
             pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame ||
             pad.startButton.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame ||
             pad.leftStickButton.wasPressedThisFrame || pad.rightStickButton.wasPressedThisFrame);

        void ObserveDevices()
        {
            if (!initialized || (!Application.isFocused && InputSystem.settings.backgroundBehavior != InputSettings.BackgroundBehavior.IgnoreFocus)) return;
            // Keyboard/mouse edge wins a simultaneous frame; sustained stick cannot flicker prompts back.
            var keyboard = Keyboard.current; var mouse = Mouse.current;
            if (Listening && mouse != null && mouse.leftButton.wasPressedThisFrame && flow.UserInterface &&
                flow.UserInterface.PointerOverRebindCancel(mouse.position.ReadValue())) { CancelRebind(); return; }
            if (keyboard != null && keyboard.enabled && keyboard.anyKey.wasPressedThisFrame) { SetDevice(keyboard); return; }
            if (mouse != null && mouse.enabled && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame ||
                mouse.delta.ReadValue().sqrMagnitude > 9 || mouse.scroll.ReadValue().sqrMagnitude > 1)) { SetDevice(mouse); return; }
            foreach (var pad in Gamepad.all)
                if (Meaningful(pad) && pad.wasUpdatedThisFrame) { SetDevice(pad); break; }
        }
        void SetDevice(InputDevice device)
        {
            if (activeDevice == device) return;
            if (activeDevice != null && Time.unscaledTime - lastSwitch < .35f) return;
            activeDevice = device; lastSwitch = Time.unscaledTime;
            Family = Classify(device); Invalidate();
        }
        void DeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (change != InputDeviceChange.Disconnected && change != InputDeviceChange.Removed && change != InputDeviceChange.Disabled) return;
            if (device is Gamepad)
            {
                uiNeutralRequired = true;
                CancelRebind();
                // Pause neutralizes both gameplay and vehicle intent through existing SessionFlow.
                if (flow && flow.Player) flow.Pause();
                if (reader) reader.NotifyFocusLost();
            }
            if (device == activeDevice)
            { activeDevice = null; Family = InputDeviceFamily.KeyboardMouse; Invalidate(); }
        }
        void OnApplicationFocus(bool focused)
        {
            applicationFocused = focused; uiNeutralRequired = true;
            if (!focused)
            {
                CancelRebind(); if (module) module.enabled = false; uiMap?.Disable();
                if (flow && flow.Player) flow.Pause();
            }
        }

        public int BindingIndex(InputAction action, bool gamepad)
        {
            if (action == null) return -1;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var b = action.bindings[i];
                if (b.isPartOfComposite) continue;
                string group = gamepad ? "Gamepad" : "Keyboard&Mouse";
                if (!string.IsNullOrEmpty(b.groups) && b.groups.Contains(group)) return i;
                if (b.isComposite)
                    for (int part = i + 1; part < action.bindings.Count && action.bindings[part].isPartOfComposite; part++)
                        if (!string.IsNullOrEmpty(action.bindings[part].groups) && action.bindings[part].groups.Contains(group)) return i;
            }
            return -1;
        }
        public string BindingText(string path)
        {
            if (labels.TryGetValue(path, out var cached)) return cached;
            var action = Actions ? Actions.FindAction(path, false) : null;
            int index = BindingIndex(action, Family != InputDeviceFamily.KeyboardMouse);
            string value = index < 0 ? "—" : action.GetBindingDisplayString(index);
            labels[path] = value; return value;
        }
        public void Invalidate()
        { labels.Clear(); PresentationRevision++; PresentationChanged?.Invoke(); }

        public bool BeginRebind(string path, int index)
        {
            if (!flow.SettingsOpen || Listening || !Actions) return false;
            var action = Actions.FindAction(path, false);
            if (action == null || index < 0 || index >= action.bindings.Count || !InputBindingPolicy.Editable(action, action.bindings[index])) return false;
            previousOverrides = Actions.SaveBindingOverridesAsJson();
            action.Disable();
            if (reader) reader.SetGameplay(false);
            if (module) module.enabled = false;
            uiMap.Disable();
            Message = "Yeni tuşa bas. Esc veya gamepad Start: iptal. 10 saniye sonra iptal edilir.";
            bool gamepad = action.bindings[index].path.StartsWith("<Gamepad>/", StringComparison.Ordinal);
            operation = action.PerformInteractiveRebinding(index).WithExpectedControlType("Button")
                .WithControlsExcluding("<Pointer>/position").WithControlsExcluding("<Pointer>/delta")
                .WithControlsExcluding("<Gamepad>/start").WithControlsExcluding("<Keyboard>/escape")
                .WithTimeout(10).WithCancelingThrough("<Keyboard>/escape")
                .OnCancel(_ => FinishRebind(false)).OnComplete(_ => FinishRebind(true))
                .OnPotentialMatch(candidate =>
                {
                    var mouse = Mouse.current;
                    if (mouse != null && candidate.candidates.Count > 0 && candidate.candidates[0] == mouse.leftButton &&
                        flow.UserInterface && flow.UserInterface.PointerOverRebindCancel(mouse.position.ReadValue())) candidate.Cancel();
                    else candidate.Complete();
                });
            if (gamepad) operation.WithControlsHavingToMatchPath("<Gamepad>");
            else { operation.WithControlsHavingToMatchPath("<Keyboard>"); operation.WithControlsHavingToMatchPath("<Mouse>"); }
            operation.Start(); return true;
        }
        public void CancelRebind() { if (operation != null) operation.Cancel(); }
        void FinishRebind(bool complete)
        {
            var current = operation; operation = null;
            current?.Dispose();
            uiNeutralRequired = true;
            bool valid = complete && InputBindingPolicy.Valid(Actions, out _);
            if (!valid)
            {
                string reason = null;
                if (complete) InputBindingPolicy.Valid(Actions, out reason);
                InputBindingPolicy.LoadSafely(Actions, previousOverrides, out _);
                Message = complete ? reason : "Tuş değiştirme iptal edildi.";
            }
            else { Persist(); Message = "Tuş kaydedildi."; }
            Invalidate();
        }
        public void ResetBinding(string path, int index)
        {
            if (Listening) CancelRebind();
            var action = Actions.FindAction(path, true);
            string old = Actions.SaveBindingOverridesAsJson();
            action.RemoveBindingOverride(index);
            if (!InputBindingPolicy.Valid(Actions, out var reason))
            { InputBindingPolicy.LoadSafely(Actions, old, out _); Message = reason + " Tüm tuşları sıfırlayabilirsin."; }
            else { Persist(); Message = "Seçili tuş varsayılana döndü."; }
            Invalidate();
        }
        public void ResetBindings()
        { CancelRebind(); Actions.RemoveAllBindingOverrides(); Persist(); Message = "Tüm tuşlar varsayılana döndü."; Invalidate(); }
        void Persist()
        {
            preferences.BindingOverrides = Actions.SaveBindingOverridesAsJson(); preferences.Save();
            if (reader) reader.LoadBindingOverrides(preferences.BindingOverrides);
        }
        void LateUpdate()
        { if (Listening && Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame) CancelRebind(); }
        void OnEnable() { if (flow && sourceAsset && !initialized) Initialize(flow, sourceAsset); }
        void OnDisable()
        {
            CancelRebind();
            InputSystem.onAfterUpdate -= ObserveDevices; InputSystem.onDeviceChange -= DeviceChanged;
            if (Actions) Actions.FindAction("UI/Cancel", true).performed -= OnBack;
            if (reader) reader.ExternalUiOwner = false;
            if (module && originalUiAsset) { module.actionsAsset = originalUiAsset; module.enabled = originalModuleEnabled; }
            foreach (var reference in references) if (reference) Destroy(reference);
            references.Clear();
            if (Actions) { Actions.Disable(); Destroy(Actions); }
            Actions = null; observedPlayer = null; reader = null; uiMap = playerMap = vehicleMap = null; initialized = false; uiNeutralRequired = true;
        }
    }
}
