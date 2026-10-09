using System;
using LastSignal.Audio;
using NUnit.Framework;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class S016SettingsTests
    {
        string prefix;

        [SetUp] public void Setup() => prefix = "LastSignal.Tests.Settings." + Guid.NewGuid() + ".";

        [TearDown] public void TearDown()
        {
            foreach (var parameter in AudioPreferences.Parameters) PlayerPrefs.DeleteKey(prefix + parameter);
            foreach (var key in new[] { "Captions", "FovDegrees", "MouseSensitivity", "UiScale", "ProfileVersion", "GamepadYaw", "GamepadPitch", "GamepadDeadzone", "GamepadInvertX", "GamepadInvertY", "BindingOverrides", "TutorialCompleted", "TutorialSkipped" })
                PlayerPrefs.DeleteKey(prefix + key);
            PlayerPrefs.Save();
        }

        [Test] public void LegacyAudioValuesMigrateWithoutResetAndControlsUseDefaults()
        {
            PlayerPrefs.SetFloat(prefix + "MasterVolume", .31f);
            PlayerPrefs.SetInt(prefix + "Captions", 0);
            var settings = new AudioPreferences(prefix);
            settings.Load();
            Assert.That(settings[AudioBus.Master], Is.EqualTo(.31f).Within(.001f));
            Assert.That(settings.Captions, Is.False);
            Assert.That(settings.FovDegrees, Is.EqualTo(AudioPreferences.DefaultFovDegrees));
            Assert.That(settings.MouseSensitivity, Is.EqualTo(AudioPreferences.DefaultMouseSensitivity));
            Assert.That(settings.UiScale, Is.EqualTo(AudioPreferences.DefaultUiScale));
        }

        [Test] public void ControlsRoundtripInVersionedDeviceProfile()
        {
            var settings = new AudioPreferences(prefix);
            settings.FovDegrees = 94;
            settings.MouseSensitivity = .21f;
            settings.UiScale = 1.2f;
            settings.Save();
            var loaded = new AudioPreferences(prefix);
            loaded.Load();
            Assert.That(PlayerPrefs.GetInt(prefix + "ProfileVersion"), Is.EqualTo(AudioPreferences.ProfileVersion));
            Assert.That(loaded.FovDegrees, Is.EqualTo(94).Within(.001f));
            Assert.That(loaded.MouseSensitivity, Is.EqualTo(.21f).Within(.001f));
            Assert.That(loaded.UiScale, Is.EqualTo(1.2f).Within(.001f));
        }

        [Test] public void InvalidControlValuesFallBackOrClampWithoutAffectingAudio()
        {
            PlayerPrefs.SetInt(prefix + "ProfileVersion", AudioPreferences.ProfileVersion);
            PlayerPrefs.SetFloat(prefix + "FovDegrees", float.NaN);
            PlayerPrefs.SetFloat(prefix + "MouseSensitivity", 5);
            PlayerPrefs.SetFloat(prefix + "UiScale", float.PositiveInfinity);
            PlayerPrefs.SetFloat(prefix + "MasterVolume", .62f);
            var settings = new AudioPreferences(prefix);
            settings.Load();
            Assert.That(settings.FovDegrees, Is.EqualTo(AudioPreferences.DefaultFovDegrees));
            Assert.That(settings.MouseSensitivity, Is.EqualTo(.5f));
            Assert.That(settings.UiScale, Is.EqualTo(AudioPreferences.DefaultUiScale));
            Assert.That(settings[AudioBus.Master], Is.EqualTo(.62f).Within(.001f));
        }
    }
}
