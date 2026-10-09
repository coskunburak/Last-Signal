#if UNITY_EDITOR
using System.Collections;
using LastSignal.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LastSignal.Tests
{
    public sealed class S016SettingsPlayTests
    {
        static readonly string[] Keys = {
            "MasterVolume", "EffectsVolume", "AmbienceVolume", "VoiceVolume", "MusicVolume",
            "Captions", "FovDegrees", "MouseSensitivity", "UiScale", "ProfileVersion",
            "GamepadYaw", "GamepadPitch", "GamepadDeadzone", "GamepadInvertX", "GamepadInvertY", "BindingOverrides", "TutorialCompleted", "TutorialSkipped"
        };
        readonly bool[] present = new bool[Keys.Length];
        readonly float[] floats = new float[Keys.Length];
        readonly int[] ints = new int[Keys.Length];
        readonly string[] strings = new string[Keys.Length];
        SessionFlow flow;

        [SetUp] public void SnapshotPreferences()
        {
            for (int i = 0; i < Keys.Length; i++)
            {
                string key = AudioPreferences.Prefix + Keys[i];
                present[i] = PlayerPrefs.HasKey(key);
                if (Keys[i] == "BindingOverrides") strings[i] = PlayerPrefs.GetString(key);
                else if (Keys[i] == "Captions" || Keys[i] == "ProfileVersion" || Keys[i] == "GamepadInvertX" || Keys[i] == "GamepadInvertY" || Keys[i] == "TutorialCompleted" || Keys[i] == "TutorialSkipped") ints[i] = PlayerPrefs.GetInt(key);
                else floats[i] = PlayerPrefs.GetFloat(key);
            }
        }

        [UnitySetUp] public IEnumerator OpenProduction()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/LastSignal/Scenes/Production/S013Cabin.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            flow = Object.FindFirstObjectByType<SessionFlow>();
            Assert.That(flow, Is.Not.Null);
            Assert.That(flow.Player, Is.Not.Null);
            yield return null;
        }

        [UnityTearDown] public IEnumerator CloseProduction()
        {
            if (flow) flow.ReturnToMenu();
            yield return null;
            var production = SceneManager.GetSceneByPath("Assets/LastSignal/Scenes/Production/S013Cabin.unity");
            if (production.IsValid() && production.isLoaded)
            {
                var empty = SceneManager.CreateScene("S016 settings cleanup");
                SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync(production);
            }
            flow = null;
        }

        [TearDown] public void RestorePreferences()
        {
            for (int i = 0; i < Keys.Length; i++)
            {
                string key = AudioPreferences.Prefix + Keys[i];
                if (!present[i]) PlayerPrefs.DeleteKey(key);
                else if (Keys[i] == "BindingOverrides") PlayerPrefs.SetString(key, strings[i]);
                else if (Keys[i] == "Captions" || Keys[i] == "ProfileVersion" || Keys[i] == "GamepadInvertX" || Keys[i] == "GamepadInvertY" || Keys[i] == "TutorialCompleted" || Keys[i] == "TutorialSkipped") PlayerPrefs.SetInt(key, ints[i]);
                else PlayerPrefs.SetFloat(key, floats[i]);
            }
            PlayerPrefs.Save();
            Time.timeScale = 1;
        }

        [UnityTest] public IEnumerator PauseSettingsApplyLiveSaveAndSurviveNewSession()
        {
            var audio = Object.FindFirstObjectByType<ProductionAudio>();
            var view = Object.FindFirstObjectByType<AudioSettingsView>();
            Assert.That(audio, Is.Not.Null);
            Assert.That(view, Is.Not.Null);
            flow.Pause();
            yield return null;
            Assert.That(view.settingsPanel.activeSelf, Is.False, "Settings has its own child screen.");
            flow.OpenSettings();
            flow.UserInterface.SetPage(1);
            yield return null;
            Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Settings));
            var controls = view.ControlsPanel ? view.ControlsPanel.transform : null;
            Assert.That(controls, Is.Not.Null);
            Assert.That(controls.gameObject.activeSelf, Is.True);
            var fov = controls.Find("Görüş açısı").GetComponent<Slider>();
            var sensitivity = controls.Find("Fare duyarlılığı").GetComponent<Slider>();
            var scale = controls.Find("Arayüz ölçeği").GetComponent<Slider>();
            var look = flow.Player.GetComponent<FirstPersonLook>();
            var canvas = view.GetComponent<CanvasScaler>();
            float previousScale = audio.Preferences.UiScale;
            Vector2 previousResolution = canvas.referenceResolution;
            float nextFov = audio.Preferences.FovDegrees < 80 ? 91 : 67;
            float nextSensitivity = audio.Preferences.MouseSensitivity < .2f ? .24f : .08f;
            float nextScale = previousScale < 1.1f ? 1.2f : .9f;

            fov.value = nextFov;
            sensitivity.value = nextSensitivity;
            scale.value = nextScale;
            Assert.That(look.BaseFOV, Is.EqualTo(nextFov).Within(.001f));
            Assert.That(look.MouseSensitivity, Is.EqualTo(nextSensitivity).Within(.001f));
            Assert.That(canvas.referenceResolution.x,
                Is.EqualTo(previousResolution.x * previousScale / nextScale).Within(.01f));
            yield return new WaitForSecondsRealtime(.65f);
            Assert.That(PlayerPrefs.GetFloat(AudioPreferences.Prefix + "FovDegrees"), Is.EqualTo(nextFov).Within(.001f));
            Assert.That(PlayerPrefs.GetFloat(AudioPreferences.Prefix + "MouseSensitivity"), Is.EqualTo(nextSensitivity).Within(.001f));
            Assert.That(PlayerPrefs.GetFloat(AudioPreferences.Prefix + "UiScale"), Is.EqualTo(nextScale).Within(.001f));

            flow.ReturnToMenu();
            yield return null;
            flow.BeginSession();
            yield return null;
            look = flow.Player.GetComponent<FirstPersonLook>();
            Assert.That(look.BaseFOV, Is.EqualTo(nextFov).Within(.001f));
            Assert.That(look.MouseSensitivity, Is.EqualTo(nextSensitivity).Within(.001f));
        }
    }
}
#endif
