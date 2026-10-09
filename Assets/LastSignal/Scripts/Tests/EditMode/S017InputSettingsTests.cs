using System;
using System.Collections.Generic;
using System.Linq;
using LastSignal.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastSignal.Tests
{
    public sealed class S017InputSettingsTests
    {
        string prefix;
        GameObject cameraOwner;
        FirstPersonLook look;

        [SetUp] public void Setup()
        {
            prefix = "LastSignal.Tests.S017." + Guid.NewGuid() + ".";
            cameraOwner = new GameObject("S017 look policy");
            cameraOwner.SetActive(false);
            look = cameraOwner.AddComponent<FirstPersonLook>();
        }

        [TearDown] public void TearDown()
        {
            if (cameraOwner) UnityEngine.Object.DestroyImmediate(cameraOwner);
            foreach (var parameter in AudioPreferences.Parameters) PlayerPrefs.DeleteKey(prefix + parameter);
            foreach (var key in new[] { "Captions", "ProfileVersion", "FovDegrees", "MouseSensitivity", "UiScale",
                "GamepadYaw", "GamepadPitch", "GamepadDeadzone", "GamepadInvertX", "GamepadInvertY", "BindingOverrides", "TutorialCompleted", "TutorialSkipped" })
                PlayerPrefs.DeleteKey(prefix + key);
            PlayerPrefs.Save();
        }

        [Test] public void S016ProfileMigratesWithoutLosingExistingPreferences()
        {
            PlayerPrefs.SetInt(prefix + "ProfileVersion", 1);
            PlayerPrefs.SetFloat(prefix + "FovDegrees", 93);
            PlayerPrefs.SetFloat(prefix + "MouseSensitivity", .23f);
            PlayerPrefs.SetFloat(prefix + "UiScale", 1.2f);
            PlayerPrefs.SetFloat(prefix + "MasterVolume", .34f);
            PlayerPrefs.SetInt(prefix + "Captions", 0);
            var settings = new AudioPreferences(prefix);
            settings.Load(); settings.Save();
            var loaded = new AudioPreferences(prefix); loaded.Load();
            Assert.That(loaded.FovDegrees, Is.EqualTo(93));
            Assert.That(loaded.MouseSensitivity, Is.EqualTo(.23f));
            Assert.That(loaded.UiScale, Is.EqualTo(1.2f));
            Assert.That(loaded[AudioBus.Master], Is.EqualTo(.34f));
            Assert.That(loaded.Captions, Is.False);
            Assert.That(loaded.GamepadYaw, Is.EqualTo(AudioPreferences.DefaultGamepadYaw));
            Assert.That(loaded.GamepadPitch, Is.EqualTo(AudioPreferences.DefaultGamepadPitch));
            Assert.That(loaded.GamepadDeadzone, Is.EqualTo(AudioPreferences.DefaultGamepadDeadzone));
            Assert.That(loaded.GamepadInvertX || loaded.GamepadInvertY, Is.False);
        }

        [Test] public void ControllerPreferencesSurviveProfileReload()
        {
            var settings = new AudioPreferences(prefix) { GamepadYaw = 210, GamepadPitch = 135,
                GamepadDeadzone = .31f, GamepadInvertX = true, GamepadInvertY = true };
            settings.Save();
            var loaded = new AudioPreferences(prefix); loaded.Load();
            Assert.That(loaded.GamepadYaw, Is.EqualTo(210));
            Assert.That(loaded.GamepadPitch, Is.EqualTo(135));
            Assert.That(loaded.GamepadDeadzone, Is.EqualTo(.31f));
            Assert.That(loaded.GamepadInvertX && loaded.GamepadInvertY, Is.True);
        }

        [Test] public void InvalidPersistedTuningFallsBackOrClamps()
        {
            PlayerPrefs.SetInt(prefix + "ProfileVersion", AudioPreferences.ProfileVersion);
            PlayerPrefs.SetFloat(prefix + "GamepadYaw", float.NaN);
            PlayerPrefs.SetFloat(prefix + "GamepadPitch", 900);
            PlayerPrefs.SetFloat(prefix + "GamepadDeadzone", float.PositiveInfinity);
            var settings = new AudioPreferences(prefix); settings.Load();
            Assert.That(settings.GamepadYaw, Is.EqualTo(AudioPreferences.DefaultGamepadYaw));
            Assert.That(settings.GamepadPitch, Is.EqualTo(300));
            Assert.That(settings.GamepadDeadzone, Is.EqualTo(AudioPreferences.DefaultGamepadDeadzone));
            settings.GamepadDeadzone = -1;
            Assert.That(settings.GamepadDeadzone, Is.EqualTo(.15f));
        }

        [Test] public void StickRatePreservesAnalogMagnitudeAcrossFrameRates()
        {
            look.ApplyPreferences(new AudioPreferences(prefix) { GamepadYaw = 120, GamepadPitch = 90 });
            foreach (int fps in new[] { 30, 60, 120 })
            {
                Vector2 total = Vector2.zero;
                for (int frame = 0; frame < fps; frame++)
                    total += look.CalculateLookDegrees(new Vector2(.5f, .25f), true, 1f / fps);
                Assert.That(total.x, Is.EqualTo(60).Within(.001f), "yaw at " + fps);
                Assert.That(total.y, Is.EqualTo(22.5f).Within(.001f), "pitch at " + fps);
            }
            Assert.That(look.CalculateLookDegrees(Vector2.one, true, 0), Is.EqualTo(Vector2.zero));
            Assert.That(look.CalculateLookDegrees(Vector2.zero, true, 1), Is.EqualTo(Vector2.zero));
        }

        [Test] public void InvertAndAdsApplyToStickWithoutChangingMouseDeltaSemantics()
        {
            look.ApplyPreferences(new AudioPreferences(prefix) { MouseSensitivity = .2f,
                GamepadYaw = 120, GamepadPitch = 90, GamepadInvertX = true, GamepadInvertY = true });
            look.SetSensitivityMultiplier(.5f);
            var stick = look.CalculateLookDegrees(new Vector2(.5f, .5f), true, 1);
            Assert.That(stick.x, Is.EqualTo(-30).Within(.001f));
            Assert.That(stick.y, Is.EqualTo(-22.5f).Within(.001f));
            var pixels = new Vector2(20, -10);
            foreach (float seconds in new[] { 0, 1f / 30, 1f / 120 })
            {
                var mouse = look.CalculateLookDegrees(pixels, false, seconds);
                Assert.That(mouse.x, Is.EqualTo(2).Within(.001f));
                Assert.That(mouse.y, Is.EqualTo(-1).Within(.001f));
            }
        }

        [Test] public void PlayerGamepadBindingsAreUnambiguousAndEssentialActionsReachable()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/LastSignal/Settings/Input/InputSystem_Actions.inputactions");
            Assert.That(asset, Is.Not.Null);
            var player = asset.FindActionMap("Player", true);
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var actions = new HashSet<string>();
            foreach (var binding in player.bindings)
            {
                if (!binding.path.StartsWith("<Gamepad>/", StringComparison.Ordinal)) continue;
                Assert.That(paths.Add(binding.path), Is.True, "Ambiguous Player binding: " + binding.path);
                actions.Add(binding.action);
            }
            foreach (string action in new[] { "Move", "Look", "Attack", "Aim", "Reload", "Interact", "Inventory", "Pause", "MeleeSlot", "FirearmSlot" })
                Assert.That(actions.Contains(action), Is.True, "Unreachable Player action: " + action);
            Assert.That(player.FindAction("Attack").bindings.Any(b => b.path == "<Gamepad>/rightTrigger"), Is.True);
            Assert.That(asset.FindAction("Vehicle/Throttle", true).bindings.Any(b => b.path == "<Gamepad>/rightTrigger"), Is.True,
                "Vehicle reuse belongs to a separate context.");
        }
    }
}
