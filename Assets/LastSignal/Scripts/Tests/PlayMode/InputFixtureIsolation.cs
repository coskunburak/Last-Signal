using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;
using System.Collections.Generic;

namespace LastSignal.Tests
{
    internal static class InputFixtureIsolation
    {
        // Asynchronous scene tests retain the real manager/clock. Shared UI assets
        // can outlive a scene and must not resolve controls against a swapped manager.
        public sealed class NativeInputScope : System.IDisposable
        {
            readonly List<InputDevice> suspended = new List<InputDevice>();
            readonly InputSettings settings;
            readonly InputSettings.BackgroundBehavior background;
            readonly InputSettings.EditorInputBehaviorInPlayMode editorInput;
            readonly bool runInBackground;
            public Keyboard Keyboard { get; }
            public Mouse Mouse { get; }

            public NativeInputScope()
            {
                DisableLiveActions();
                settings = InputSystem.settings;
                background = settings.backgroundBehavior;
                editorInput = settings.editorInputBehaviorInPlayMode;
                runInBackground = Application.runInBackground;
                foreach (var device in InputSystem.devices) if (device.enabled) suspended.Add(device);
                foreach (var device in suspended) InputSystem.DisableDevice(device);
                settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                Application.runInBackground = true;
                Keyboard = InputSystem.AddDevice<Keyboard>();
                Mouse = InputSystem.AddDevice<Mouse>();
            }

            public void Dispose()
            {
                try
                {
                    DisableLiveActions();
                    if (Keyboard != null && Keyboard.added) InputSystem.RemoveDevice(Keyboard);
                    if (Mouse != null && Mouse.added) InputSystem.RemoveDevice(Mouse);
                }
                finally
                {
                    settings.backgroundBehavior = background;
                    settings.editorInputBehaviorInPlayMode = editorInput;
                    Application.runInBackground = runInBackground;
                    foreach (var device in suspended) if (device.added) InputSystem.EnableDevice(device);
                    suspended.Clear();
                }
            }
        }

        public static void QueueButton(UnityEngine.InputSystem.Controls.ButtonControl button, bool held)
        {
            using (UnityEngine.InputSystem.LowLevel.StateEvent.From(button.device, out var pointer))
            {
                button.WriteValueIntoEvent(held ? 1f : 0f, pointer);
                InputSystem.QueueEvent(pointer);
            }
        }

        // Additive fixture scenes share default Physics and still run old behaviours.
        // Suspend authored roots, preserving the runner and restoring surviving roots.
        public sealed class SceneScope : System.IDisposable
        {
            readonly List<GameObject> roots = new List<GameObject>();
            public SceneScope()
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    foreach (var root in SceneManager.GetSceneAt(i).GetRootGameObjects())
                    {
                        if (!root.activeSelf || (root.hideFlags & HideFlags.DontSave) != 0) continue;
                        bool runner = false;
                        foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                            if (component && component.GetType().Assembly.GetName().Name.Contains("TestRunner")) { runner = true; break; }
                        if (runner) continue;
                        roots.Add(root); root.SetActive(false);
                    }
                Physics.SyncTransforms();
            }
            public void Dispose()
            {
                foreach (var root in roots) if (root) root.SetActive(true);
                roots.Clear(); Physics.SyncTransforms();
            }
        }
        // InputTestFixture cihaz yöneticisini değiştirir. Önceki sahnenin etkin
        // eylemleri eski yöneticide kontrol izleyicileri bırakmamalıdır.
        // Liste bir anlık kopyadır; Disable sırasında etkin eylem listesi değişebilir.
        public static void DisableLiveActions()
        {
            // Stop owners before swapping the InputTestFixture manager. Disabling
            // actions alone lets Update re-enable stale device monitors next frame.
            foreach (var owner in Object.FindObjectsByType<SessionInput>(FindObjectsSortMode.None)) owner.enabled = false;
            foreach (var module in Object.FindObjectsByType<InputSystemUIInputModule>(FindObjectsSortMode.None)) module.enabled = false;
            var actions = InputSystem.ListEnabledActions();
            foreach (var action in actions) action.Disable();
        }
    }
}
