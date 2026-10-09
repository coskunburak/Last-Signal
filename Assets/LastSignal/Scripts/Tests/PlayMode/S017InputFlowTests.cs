#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using LastSignal.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LastSignal.Tests
{
    public sealed class S017InputFlowTests
    {
        SessionFlow flow;
        Gamepad pad;
        Keyboard keyboard;
        readonly List<InputDevice> suspended = new List<InputDevice>();
        InputSettings settings;
        InputSettings.BackgroundBehavior background;
        InputSettings.EditorInputBehaviorInPlayMode editorInput;
        bool runInBackground;
        readonly ProfileSnapshot profile = new ProfileSnapshot();

        void SetupInput()
        {
            profile.Capture();
            // Native clock, matching the S016 fixture isolation contract. No mock runtime/Update mix.
            InputFixtureIsolation.DisableLiveActions();
            settings = InputSystem.settings; background = settings.backgroundBehavior;
            editorInput = settings.editorInputBehaviorInPlayMode; runInBackground = Application.runInBackground;
            foreach (var device in InputSystem.devices) if (device.enabled) suspended.Add(device);
            foreach (var device in suspended) InputSystem.DisableDevice(device);
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Application.runInBackground = true;
            pad = InputSystem.AddDevice<Gamepad>(); keyboard = InputSystem.AddDevice<Keyboard>();
            // Exercise defaults without adopting the user's existing rebindings/tutorial state.
            PlayerPrefs.DeleteKey(AudioPreferences.Prefix + "BindingOverrides");
        }
        [UnitySetUp] public IEnumerator OpenScene()
        {
            // UnitySetUp wraps NUnit SetUp in the installed runner. Keep device
            // isolation before scene creation, otherwise it disables the new player.
            SetupInput();
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/LastSignal/Scenes/Production/S013Cabin.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            flow = Object.FindAnyObjectByType<SessionFlow>();
            Assert.That(flow, Is.Not.Null); Assert.That(flow.Controls.Actions, Is.Not.Null);
            Assert.That(flow.Controls.BindingText("Player/Move"), Is.Not.EqualTo("—"));
            yield return new WaitForSecondsRealtime(.4f);
        }
        [UnityTearDown] public IEnumerator CloseScene()
        {
            try
            {
                if (flow) flow.ReturnToMenu();
                var scene = SceneManager.GetSceneByPath("Assets/LastSignal/Scenes/Production/S013Cabin.unity");
                if (scene.IsValid() && scene.isLoaded)
                { var empty = SceneManager.CreateScene("S017 cleanup"); SceneManager.SetActiveScene(empty); yield return SceneManager.UnloadSceneAsync(scene); }
                flow = null; yield return null;
            }
            finally { CleanupInput(); }
        }
        void CleanupInput()
        {
            InputFixtureIsolation.DisableLiveActions();
            if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            settings.backgroundBehavior = background; settings.editorInputBehaviorInPlayMode = editorInput;
            Application.runInBackground = runInBackground;
            foreach (var device in suspended) if (device.added) InputSystem.EnableDevice(device);
            suspended.Clear(); profile.Restore(); Time.timeScale = 1;
        }
        static void Queue<T>(InputControl<T> control, T value) where T : struct
        {
            using (StateEvent.From(control.device, out var pointer))
            { control.WriteValueIntoEvent(value, pointer); InputSystem.QueueEvent(pointer); }
        }
        void QueueSticks(Vector2 left, Vector2 right)
        {
            // Two full snapshots queued before an update would overwrite each other.
            using (StateEvent.From(pad, out var pointer))
            {
                pad.leftStick.WriteValueIntoEvent(left, pointer);
                pad.rightStick.WriteValueIntoEvent(right, pointer);
                InputSystem.QueueEvent(pointer);
            }
        }
        IEnumerator Tap(InputControl<float> button)
        { Queue(button, 1); yield return null; yield return null; Queue(button, 0); yield return null; yield return null; }

        [UnityTest] public IEnumerator MainMenuSubmitStartsSessionAndSettingsBackRestoresFocus()
        {
            flow.ReturnToMenu(); yield return new WaitForSecondsRealtime(.4f);
            var system = EventSystem.current;
            Assert.That(system.currentSelectedGameObject, Is.Not.Null);
            // Choose the authored start button through selection, then real gamepad Submit.
            var start = Object.FindAnyObjectByType<AcceptanceHud>().StartButton;
            Assert.That(start, Is.Not.Null, "Authored new-session button");
            system.SetSelectedGameObject(start.gameObject);
            yield return Tap(pad.buttonSouth);
            Assert.That(flow.Player, Is.Not.Null);
            flow.Pause(); yield return new WaitForSecondsRealtime(.4f);
            var settingsButton = FindButton("Ayarlar"); Assert.That(settingsButton, Is.Not.Null);
            system.SetSelectedGameObject(settingsButton.gameObject);
            yield return Tap(pad.buttonSouth);
            Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Settings));
            yield return new WaitForSecondsRealtime(.4f);
            yield return Tap(pad.buttonEast);
            Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Pause));
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(system.currentSelectedGameObject, Is.EqualTo(settingsButton.gameObject));
            Assert.That(flow.Player.GetComponent<PlayerInputReader>().GameplayActive, Is.False);
        }
        Button FindButton(string fragment)
        {
            foreach (var button in Object.FindObjectsByType<Button>())
            { var label = button.GetComponentInChildren<Text>(); if (button.name.Contains(fragment) || (label && label.text.Contains(fragment))) return button; }
            return null;
        }
        [UnityTest] public IEnumerator AnalogStickAndDeadzoneReachTheRealPlayerWithoutMouseScaling()
        {
            flow.BeginSession(); yield return null; yield return null;
            var reader = flow.Player.GetComponent<PlayerInputReader>();
            QueueSticks(new Vector2(0, .5f), new Vector2(.1f, .05f));
            yield return null; yield return null;
            Assert.That(reader.Move.y, Is.GreaterThan(0).And.LessThan(1));
            Assert.That(reader.Look, Is.EqualTo(Vector2.zero));
            Queue(pad.rightStick, new Vector2(.7f, 0)); yield return null; yield return null;
            Assert.That(reader.LookUsesGamepad, Is.True); Assert.That(reader.Look.x, Is.GreaterThan(0));
            QueueSticks(Vector2.zero, Vector2.zero); yield return null;
            flow.Pause(); flow.Resume();
            Queue(pad.rightStick, Vector2.right); yield return null; yield return null;
            Assert.That(reader.Look, Is.EqualTo(Vector2.zero), "Held input cannot cross a menu neutral gate.");
            Queue(pad.rightStick, Vector2.zero); yield return null; yield return null;
            Queue(pad.rightStick, Vector2.right); yield return null; yield return null;
            Assert.That(reader.Look.x, Is.GreaterThan(0));
        }
        [UnityTest] public IEnumerator DisconnectNeutralizesGameplayAndLeavesMenuReachableAfterReconnect()
        {
            flow.BeginSession(); yield return null; yield return null;
            using (StateEvent.From(pad, out var pointer))
            {
                pad.leftStick.WriteValueIntoEvent(Vector2.up, pointer);
                pad.rightTrigger.WriteValueIntoEvent(1f, pointer);
                InputSystem.QueueEvent(pointer);
            }
            yield return null; yield return null;
            var reader = flow.Player.GetComponent<PlayerInputReader>();
            Assert.That(reader.Move.y, Is.GreaterThan(0), "Disconnect starts with real movement intent.");
            Assert.That(reader.FireHeld, Is.True, "Disconnect starts with held fire intent.");
            InputSystem.RemoveDevice(pad); yield return null; yield return null;
            Assert.That(flow.Paused, Is.True); Assert.That(reader.Move, Is.EqualTo(Vector2.zero));
            Assert.That(reader.FireHeld, Is.False); Assert.That(reader.Look, Is.EqualTo(Vector2.zero));
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Not.Null);
            pad = InputSystem.AddDevice<Gamepad>(); yield return null;
            yield return Tap(pad.dpad.down);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Not.Null);
            Assert.That(reader.GameplayActive, Is.False, "Reconnect never auto-resumes gameplay.");
        }
        [UnityTest] public IEnumerator RebindPersistsRefreshesPromptAndCannotLeakIntoGameplay()
        {
            flow.Pause(); flow.OpenSettings(); yield return null;
            var action = flow.Controls.Actions.FindAction("Player/Interact");
            int index = flow.Controls.BindingIndex(action, false);
            string original = flow.Controls.BindingText("Player/Interact");
            Assert.That(flow.Controls.BeginRebind("Player/Interact", index), Is.True);
            yield return null;
            Assert.That(flow.Player.GetComponent<PlayerInputReader>().GameplayActive, Is.False);
            yield return Tap(keyboard.hKey);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(flow.Controls.Listening, Is.False);
            Assert.That(action.bindings[index].effectivePath, Is.EqualTo("<Keyboard>/h"));
            Assert.That(flow.Controls.BindingText("Player/Interact"), Is.Not.EqualTo(original));
            var loaded = new AudioPreferences(); loaded.Load();
            Assert.That(loaded.BindingOverrides, Does.Contain("<Keyboard>/h"));
            Assert.That(flow.Controls.BeginRebind("Player/Interact", index), Is.True);
            yield return null; yield return Tap(keyboard.rKey); yield return new WaitForSecondsRealtime(.3f);
            Assert.That(action.bindings[index].effectivePath, Is.EqualTo("<Keyboard>/h"), "Reload conflict rolls back.");
            flow.Controls.ResetBindings();
            Assert.That(action.bindings[index].effectivePath, Is.EqualTo("<Keyboard>/e"));
        }
        [UnityTest] public IEnumerator CancelListeningAndFocusLossKeepRecoveryControlsAvailable()
        {
            flow.Pause(); flow.OpenSettings(); yield return null;
            int index = flow.Controls.BindingIndex(flow.Controls.Actions.FindAction("Player/Interact"), false);
            Assert.That(flow.Controls.BeginRebind("Player/Interact", index), Is.True);
            yield return null; yield return Tap(keyboard.escapeKey);
            Assert.That(flow.Controls.Listening, Is.False); Assert.That(flow.SettingsOpen, Is.True);
            yield return new WaitForSecondsRealtime(.4f);
            yield return Tap(pad.buttonEast);
            Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Pause));
            flow.Resume(); yield return null;
            flow.Player.GetComponent<PlayerInputReader>().NotifyFocusLost(); yield return null;
            Assert.That(flow.Paused, Is.True);
        }
        [UnityTest] public IEnumerator InventorySplitAmountAndRevisionProtectTheSelectedStack()
        {
            var inventory = flow.Player.GetComponent<LastSignal.Inventory.PlayerInventory>();
            var item = UnityEditor.AssetDatabase.LoadAssetAtPath<LastSignal.Inventory.Data.ItemDefinition>("Assets/LastSignal/Data/Items/Definitions/medical.bandage.asset");
            Assert.That(item, Is.Not.Null);
            inventory.Clear(); Assert.That(inventory.TryAdd(item, 5), Is.EqualTo(5));
            Assert.That(flow.InventoryView.Open(), Is.True); yield return null;
            var view = flow.InventoryView;
            view.OnSlotClicked(0); view.BeginSplitHalf();
            var slider = view.Root.GetComponentInChildren<Slider>(); Assert.That(slider, Is.Not.Null);
            slider.value = 2; view.OnSlotClicked(1);
            Assert.That(inventory.GetSlot(0).Quantity, Is.EqualTo(3));
            Assert.That(inventory.GetSlot(1).Quantity, Is.EqualTo(2));
            view.OnSlotClicked(0); Assert.That(view.SelectedSlot, Is.EqualTo(0));
            Assert.That(inventory.TryRemove(0, 1), Is.True);
            Assert.That(view.SelectedSlot, Is.EqualTo(-1), "A changed source requires a fresh explicit selection.");
            int before = inventory.GetTotalQuantity(item);
            view.BeginSplitHalf();
            Assert.That(view.SplitPending, Is.False);
            Assert.That(inventory.GetTotalQuantity(item), Is.EqualTo(before));
        }
        [UnityTest] public IEnumerator ShelterProductionAndCargoUseTheSameSafeControllerBackFlow()
        {
            var site = flow.GetComponent<LastSignal.Shelter.ShelterSite>();
            Assert.That(site, Is.Not.Null);
            var capsule = flow.Player.GetComponent<CharacterController>();
            capsule.enabled = false; flow.Player.transform.position = site.sockets[0].transform.position; capsule.enabled = true;
            Physics.SyncTransforms();
            Assert.That(site.Open(site.sockets[0]), Is.True);
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(flow.Screen, Is.EqualTo(SessionScreen.ShelterProduction));
            Assert.That(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(site.Root.transform), Is.True);
            yield return Tap(pad.buttonEast);
            Assert.That(site.IsOpen, Is.False); Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Gameplay));
            var actor = flow.GetComponent<LastSignal.Vehicles.VehicleWorld>().Actor;
            Assert.That(actor, Is.Not.Null);
            LastSignal.Vehicles.VehicleServicePoint cargo = null;
            foreach (var point in actor.GetComponentsInChildren<LastSignal.Vehicles.VehicleServicePoint>())
                if (point.Kind == LastSignal.Vehicles.VehicleServicePoint.Service.Cargo) cargo = point;
            Assert.That(cargo, Is.Not.Null);
            capsule.enabled = false; flow.Player.transform.position = cargo.transform.position; capsule.enabled = true;
            Physics.SyncTransforms();
            yield return null;
            Assert.That(cargo.TryInteract(), Is.True);
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(flow.Screen, Is.EqualTo(SessionScreen.VehicleCargo));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Not.Null);
            yield return Tap(pad.buttonEast);
            Assert.That(cargo.IsOpen, Is.False); Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Gameplay));
        }
        [UnityTest] public IEnumerator MeaningfulDeviceChangeIgnoresDriftAndKeepsGenericFamilyHonest()
        {
            yield return new WaitForSecondsRealtime(.4f);
            Queue(pad.rightStick, new Vector2(.05f, .02f)); yield return null; yield return null;
            Assert.That(flow.Controls.Family, Is.EqualTo(InputDeviceFamily.KeyboardMouse));
            yield return Tap(pad.leftShoulder);
            Assert.That(flow.Controls.Family, Is.EqualTo(InputDeviceFamily.GenericGamepad));
            yield return new WaitForSecondsRealtime(.4f);
            yield return Tap(keyboard.hKey);
            Assert.That(flow.Controls.Family, Is.EqualTo(InputDeviceFamily.KeyboardMouse));
        }
        [UnityTest] public IEnumerator JournalAndInventoryAreReachableByControllerWithSafeBack()
        {
            flow.BeginSession(); yield return null; yield return null;
            var reader = flow.Player.GetComponent<PlayerInputReader>();
            var mission = flow.GetComponent<LastSignal.Objectives.RelayMission>();
            Assert.That(mission, Is.Not.Null);
            Assert.That(mission.Progress, Is.Not.Null, "Journal domain initialized in the production scene.");
            Assert.That(reader.GameplayActive, Is.True, "Journal input starts in gameplay.");
            int requests = 0;
            SessionScreen screenAtRequest = flow.Screen;
            System.Action observe = () => { requests++; screenAtRequest = flow.Screen; };
            reader.JournalRequested += observe;
            try { yield return Tap(pad.rightShoulder); }
            finally { reader.JournalRequested -= observe; }
            Assert.That(requests, Is.EqualTo(1), "One shoulder press must emit one Journal request.");
            Assert.That(screenAtRequest, Is.EqualTo(SessionScreen.Journal), "SessionFlow handles the Journal request before observers.");
            Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Journal), "Journal remains open after button release.");
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Not.Null);
            yield return Tap(pad.buttonEast); Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Gameplay));
            yield return null; yield return null;
            yield return Tap(pad.selectButton); Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Inventory));
            yield return new WaitForSecondsRealtime(.4f);
            var selected = EventSystem.current.currentSelectedGameObject;
            Assert.That(selected, Is.Not.Null);
            Assert.That(selected.transform.IsChildOf(flow.InventoryView.Root.transform), Is.True);
            yield return Tap(pad.buttonEast); Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Gameplay));
        }
    }

    // Reused by focused fixtures; snapshot exact key existence/type without rewriting unrelated preferences.
    internal sealed class ProfileSnapshot
    {
        readonly Dictionary<string, float> floats = new Dictionary<string, float>();
        readonly Dictionary<string, int> ints = new Dictionary<string, int>();
        readonly Dictionary<string, string> strings = new Dictionary<string, string>();
        static readonly string[] FloatKeys = { "MasterVolume", "EffectsVolume", "AmbienceVolume", "VoiceVolume", "MusicVolume", "FovDegrees", "MouseSensitivity", "UiScale", "GamepadYaw", "GamepadPitch", "GamepadDeadzone" };
        static readonly string[] IntKeys = { "Captions", "ProfileVersion", "GamepadInvertX", "GamepadInvertY", "TutorialCompleted", "TutorialSkipped" };
        public void Capture()
        {
            floats.Clear(); ints.Clear(); strings.Clear();
            foreach (var key in FloatKeys) if (PlayerPrefs.HasKey(AudioPreferences.Prefix + key)) floats[key] = PlayerPrefs.GetFloat(AudioPreferences.Prefix + key);
            foreach (var key in IntKeys) if (PlayerPrefs.HasKey(AudioPreferences.Prefix + key)) ints[key] = PlayerPrefs.GetInt(AudioPreferences.Prefix + key);
            if (PlayerPrefs.HasKey(AudioPreferences.Prefix + "BindingOverrides")) strings["BindingOverrides"] = PlayerPrefs.GetString(AudioPreferences.Prefix + "BindingOverrides");
        }
        public void Restore()
        {
            foreach (var key in FloatKeys) { if (floats.TryGetValue(key, out var value)) PlayerPrefs.SetFloat(AudioPreferences.Prefix + key, value); else PlayerPrefs.DeleteKey(AudioPreferences.Prefix + key); }
            foreach (var key in IntKeys) { if (ints.TryGetValue(key, out var value)) PlayerPrefs.SetInt(AudioPreferences.Prefix + key, value); else PlayerPrefs.DeleteKey(AudioPreferences.Prefix + key); }
            if (strings.TryGetValue("BindingOverrides", out var json)) PlayerPrefs.SetString(AudioPreferences.Prefix + "BindingOverrides", json); else PlayerPrefs.DeleteKey(AudioPreferences.Prefix + "BindingOverrides");
            PlayerPrefs.Save();
        }
    }
}
#endif
