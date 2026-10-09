#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using LastSignal.Inventory.UI;
using LastSignal.Inventory;
using LastSignal.Shelter;
using LastSignal.WorldTime;
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
    public sealed class S016ModalTests
    {
        SessionFlow flow;
        InventoryUI inventory;
        Mouse mouse;
        Keyboard keyboard;
        readonly List<InputDevice> suspendedDevices = new List<InputDevice>();
        bool previousRunInBackground;
        InputSettings inputSettings;
        InputSettings.BackgroundBehavior previousBackgroundBehavior;
        InputSettings.EditorInputBehaviorInPlayMode previousEditorBehavior;

        [SetUp] public void Setup()
        {
            // A real scene owns action assets and UI modules across asynchronous loads.
            // Keep their InputManager alive; replacing it through InputTestFixture can
            // leave resolved controls pointing at devices owned by the previous manager.
            previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            inputSettings = InputSystem.settings;
            previousBackgroundBehavior = inputSettings.backgroundBehavior;
            previousEditorBehavior = inputSettings.editorInputBehaviorInPlayMode;
            inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            suspendedDevices.Clear();
            foreach (var device in InputSystem.devices)
                if (device.enabled) suspendedDevices.Add(device);
            foreach (var device in suspendedDevices) InputSystem.DisableDevice(device);
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
        }

        [UnitySetUp] public IEnumerator OpenProduction()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/LastSignal/Scenes/Production/S013Cabin.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            flow = Object.FindFirstObjectByType<SessionFlow>();
            inventory = Object.FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
            Assert.That(flow, Is.Not.Null); Assert.That(inventory, Is.Not.Null);
            flow.Resume();
            yield return null; yield return null;
        }

        [UnityTearDown] public IEnumerator CloseProduction()
        {
            if (flow) flow.ReturnToMenu();
            // Complete deferred player destruction and unload scene-owned action/UI
            // consumers before removing their devices. Do not destroy the test runner.
            yield return null;
            var production = SceneManager.GetSceneByPath("Assets/LastSignal/Scenes/Production/S013Cabin.unity");
            if (production.IsValid() && production.isLoaded)
            {
                var empty = SceneManager.CreateScene("S016 cleanup");
                SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync(production);
            }
            flow = null; inventory = null;
        }

        [TearDown] public void RestoreInputEnvironment()
        {
            Time.timeScale = 1;
            try
            {
                if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
                if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            }
            finally
            {
                if (inputSettings)
                {
                    inputSettings.backgroundBehavior = previousBackgroundBehavior;
                    inputSettings.editorInputBehaviorInPlayMode = previousEditorBehavior;
                }
                Application.runInBackground = previousRunInBackground;
                foreach (var device in suspendedDevices)
                    if (device.added) InputSystem.EnableDevice(device);
                suspendedDevices.Clear();
                keyboard = null; mouse = null;
            }
        }

        void Key(bool pressed, Key key)
        {
            // The live Input System processes queued events in the player loop.
            // Callers yield a frame before observing the resulting gameplay state.
            InputSystem.QueueStateEvent(keyboard, pressed ? new KeyboardState(key) : new KeyboardState());
        }
        void MouseButton(bool pressed)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = (ushort)(pressed ? 1 : 0) });
        }
        Button CloseButton()
        {
            foreach (var button in inventory.GetComponentsInChildren<Button>(true))
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                    if (button.onClick.GetPersistentTarget(i) == inventory && button.onClick.GetPersistentMethodName(i) == "Close")
                        return button;
            Assert.Fail("Production inventory must have an authored Close button.");
            return null;
        }

        [UnityTest] public IEnumerator EscapeClosesInventoryBeforeOpeningPause()
        {
            Key(true, UnityEngine.InputSystem.Key.Tab); yield return null;
            Key(false, UnityEngine.InputSystem.Key.Tab); yield return null;
            Assert.That(inventory.IsOpen, Is.True);
            Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Inventory));
            Assert.That(flow.SessionMenuVisible, Is.False);
            Assert.That(flow.Player.GetComponent<PlayerInputReader>().GameplayActive, Is.False);
            Assert.That(flow.GetComponent<LastSignal.Audio.ProductionAudio>(), Is.Not.Null);
            var audioView = Object.FindFirstObjectByType<LastSignal.Audio.AudioSettingsView>();
            Assert.That(audioView.settingsPanel.activeSelf, Is.False);
            var clock = flow.GetComponent<WorldClock>();
            double before = clock.Simulation.Seconds;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(clock.Simulation.Seconds, Is.EqualTo(before), "Inventory must freeze the real world clock.");
            Key(true, UnityEngine.InputSystem.Key.Escape); yield return null;
            Assert.That(inventory.IsOpen, Is.False);
            Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Gameplay));
            Key(false, UnityEngine.InputSystem.Key.Escape); yield return null;
            Key(true, UnityEngine.InputSystem.Key.Escape); yield return null;
            Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Pause));
            Assert.That(flow.SessionMenuVisible, Is.True);
        }

        [UnityTest] public IEnumerator AuthoredMouseCloseRequiresReleaseBeforeWeaponCanFire()
        {
            var weapon = flow.Player.GetComponent<PlayerCombatController>().ActiveWeapon;
            Assert.That(weapon, Is.Not.Null);
            yield return new WaitForSeconds(weapon.Definition.EquipSeconds + .1f);
            int ammo = weapon.RuntimeState.CurrentMagazine;
            Assert.That(inventory.Open(), Is.True);
            var button = CloseButton();
            MouseButton(true); yield return null;
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current)
                { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Assert.That(inventory.IsOpen, Is.False);
            Assert.That(flow.Paused, Is.False);
            yield return null; yield return null;
            Assert.That(weapon.RuntimeState.CurrentMagazine, Is.EqualTo(ammo), "The closing click must not fire.");
            Assert.That(flow.Player.GetComponent<PlayerInputReader>().FireHeld, Is.False);
            MouseButton(false); yield return null; yield return null;
            MouseButton(true); yield return null;
            MouseButton(false); yield return null;
            Assert.That(weapon.RuntimeState.CurrentMagazine, Is.EqualTo(ammo - 1), "A fresh click after release must still work.");
        }

        [UnityTest] public IEnumerator FocusLossKeepsModalAndHeldAttackCannotReplay()
        {
            var input = flow.Player.GetComponent<PlayerInputReader>();
            int requests = 0; input.FirePressed += () => requests++;
            Assert.That(inventory.Open(), Is.True);
            MouseButton(true); input.NotifyFocusLost(); yield return null;
            Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Inventory));
            flow.Resume(); yield return null; yield return null;
            Assert.That(inventory.IsOpen, Is.False);
            Assert.That(requests, Is.Zero);
            Assert.That(input.FireHeld, Is.False);
            MouseButton(false); yield return null; yield return null;
            MouseButton(true); yield return null;
            Assert.That(requests, Is.EqualTo(1));
            MouseButton(false);
        }

        [UnityTest] public IEnumerator RepeatSessionsDoNotRetainInventoryOrStaleCloseOwnership()
        {
            for (int i = 0; i < 3; i++)
            {
                Assert.That(inventory.Open(), Is.True);
                flow.ReturnToMenu(); yield return null;
                Assert.That(inventory.IsOpen, Is.False);
                Assert.That(flow.Screen, Is.EqualTo(SessionScreen.MainMenu));
                inventory.Close();
                Assert.That(Time.timeScale, Is.Zero);
                flow.BeginSession(); yield return null; yield return null;
                Key(true, UnityEngine.InputSystem.Key.Tab); yield return null;
                Key(false, UnityEngine.InputSystem.Key.Tab); yield return null;
                Assert.That(inventory.IsOpen, Is.True, "One input must open once after every rebind.");
                flow.Resume();
                flow.Pause(); inventory.Close();
                Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Pause), "A stale close must not resume a different modal.");
                flow.Resume(); yield return null; yield return null;
            }
        }

        [UnityTest] public IEnumerator DisabledInventoryReleasesModalAndDeathCannotResumeGameplay()
        {
            Assert.That(inventory.Open(), Is.True);
            inventory.enabled = false;
            Assert.That(inventory.IsOpen, Is.False);
            Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Gameplay));
            inventory.enabled = true;
            Assert.That(inventory.Open(), Is.True);
            flow.Player.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo { Amount = 10000 });
            flow.Resume(); flow.TogglePause(); inventory.Close();
            Assert.That(inventory.IsOpen, Is.False);
            Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Death));
            Assert.That(flow.Player.GetComponent<PlayerInputReader>().GameplayActive, Is.False);
            Assert.That(flow.SessionMenuVisible, Is.True);
            yield return null;
        }

        [UnityTest] public IEnumerator FailedFullStackMoveKeepsSelectionAndQuantity()
        {
            var carried=flow.Player.GetComponent<PlayerInventory>();
            var ammo=flow.Player.GetComponent<PlayerCombatController>().ActiveWeapon.Definition.Ammunition;
            carried.Clear();
            Assert.That(carried.TryAdd(ammo, ammo.MaxStack * 2), Is.EqualTo(ammo.MaxStack * 2));
            Assert.That(inventory.Open(), Is.True);
            inventory.OnSlotClicked(0); inventory.OnSlotClicked(1);
            Assert.That(inventory.SelectedSlot, Is.EqualTo(0));
            Assert.That(carried.GetSlot(0).Quantity, Is.EqualTo(ammo.MaxStack));
            Assert.That(carried.GetSlot(1).Quantity, Is.EqualTo(ammo.MaxStack));
            Assert.That(inventory.FeedbackMessage, Does.Contain("reddedildi"));
            yield return null;
        }

        [UnityTest] public IEnumerator SplitHalfUsesDomainAndPreservesTotal()
        {
            var carried=flow.Player.GetComponent<PlayerInventory>();
            var ammo=flow.Player.GetComponent<PlayerCombatController>().ActiveWeapon.Definition.Ammunition;
            carried.Clear(); Assert.That(carried.TryAdd(ammo, 5), Is.EqualTo(5));
            Assert.That(inventory.Open(), Is.True);
            inventory.OnSlotClicked(0); inventory.BeginSplitHalf();
            Assert.That(inventory.SplitPending, Is.True);
            inventory.OnSlotClicked(1);
            Assert.That(carried.GetSlot(0).Quantity, Is.EqualTo(3));
            Assert.That(carried.GetSlot(1).Quantity, Is.EqualTo(2));
            Assert.That(carried.GetTotalQuantity(ammo), Is.EqualTo(5));
            Assert.That(inventory.SelectedSlot, Is.EqualTo(-1));
            Assert.That(inventory.FeedbackMessage, Does.Contain("bölündü"));
            yield return null;
        }

        [UnityTest] public IEnumerator ExternalRemovalClearsSelectionBeforeSlotReuse()
        {
            var carried=flow.Player.GetComponent<PlayerInventory>();
            var ammo=flow.Player.GetComponent<PlayerCombatController>().ActiveWeapon.Definition.Ammunition;
            carried.Clear(); Assert.That(carried.TryAdd(ammo, 3), Is.EqualTo(3));
            Assert.That(inventory.Open(), Is.True);
            inventory.OnSlotClicked(0); Assert.That(inventory.SelectedSlot, Is.Zero);
            Assert.That(carried.TryRemove(0, 3), Is.True);
            Assert.That(inventory.SelectedSlot, Is.EqualTo(-1));
            Assert.That(carried.TryAdd(ammo, 2), Is.EqualTo(2));
            Assert.That(inventory.SelectedSlot, Is.EqualTo(-1), "A new stack must not inherit the old selection.");
            yield return null;
        }

        [Test] public void StorageResultDistinguishesPartialAndRejectedTransfer()
        {
            string partial=ShelterStorageUI.DescribeTransfer(true,"Bandaj",new TransferResult(5,3,TransferReason.Partial));
            Assert.That(partial, Does.Contain("3 / 5"));
            Assert.That(partial, Does.Contain("2 sığmadı"));
            string full=ShelterStorageUI.DescribeTransfer(false,"Bandaj",new TransferResult(5,0,TransferReason.DestinationFull));
            Assert.That(full, Does.Contain("kaynak değişmedi"));
        }
    }
}
#endif
