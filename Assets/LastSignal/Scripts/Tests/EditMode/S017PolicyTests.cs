using System;
using LastSignal.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastSignal.Tests
{
    public sealed class S017PolicyTests
    {
        InputActionAsset source, clone;
        [SetUp] public void Setup()
        {
            source = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/LastSignal/Settings/Input/InputSystem_Actions.inputactions");
            clone = UnityEngine.Object.Instantiate(source);
        }
        [TearDown] public void Cleanup() { if (clone) UnityEngine.Object.DestroyImmediate(clone); }
        static int KeyBinding(InputAction action)
        { for (int i = 0; i < action.bindings.Count; i++) if (action.bindings[i].path.StartsWith("<Keyboard>/")) return i; throw new Exception("Missing keyboard binding"); }
        [Test] public void DuplicateWithinGameplayIsRejectedButSeparateContextReuseIsAllowed()
        {
            var action = clone.FindAction("Player/Interact", true); int index = KeyBinding(action);
            action.ApplyBindingOverride(index, "<Keyboard>/r");
            Assert.That(InputBindingPolicy.Valid(clone, out _), Is.False);
            action.ApplyBindingOverride(index, "<Keyboard>/h"); // Vehicle horn is another map.
            Assert.That(InputBindingPolicy.Valid(clone, out _), Is.True);
            string json = clone.SaveBindingOverridesAsJson();
            var second = UnityEngine.Object.Instantiate(source);
            try
            {
                Assert.That(InputBindingPolicy.LoadSafely(second, json, out _), Is.True);
                Assert.That(second.FindAction("Player/Interact").bindings[index].effectivePath, Is.EqualTo("<Keyboard>/h"));
                Assert.That(source.FindAction("Player/Interact").bindings[index].effectivePath, Is.EqualTo("<Keyboard>/e"));
            }
            finally { UnityEngine.Object.DestroyImmediate(second); }
        }
        [Test] public void NavigationOverrideOrInvalidPathRestoresAllDefaults()
        {
            clone.FindAction("UI/Submit").ApplyBindingOverride(0, "");
            string json = clone.SaveBindingOverridesAsJson();
            Assert.That(InputBindingPolicy.LoadSafely(clone, json, out var reason), Is.False);
            Assert.That(reason, Is.Not.Empty);
            Assert.That(clone.FindAction("UI/Submit").bindings[0].hasOverrides, Is.False);
            Assert.That(InputBindingPolicy.AllowedPath("<Keyboard>/e", "<Keyboard>/missingKey"), Is.False);
            Assert.That(InputBindingPolicy.AllowedPath("<Gamepad>/buttonNorth", "<Gamepad>/start"), Is.False);
            Assert.That(InputBindingPolicy.AllowedPath("<Keyboard>/e", "<Keyboard>/escape"), Is.False);
            Assert.That(InputBindingPolicy.LoadSafely(clone, "invalid json", out _), Is.False);
        }
        [Test] public void DisplayUsesEffectiveBindingAndResetRecoversDefault()
        {
            var action = clone.FindAction("Player/Interact"); int index = KeyBinding(action);
            string before = action.GetBindingDisplayString(index);
            action.ApplyBindingOverride(index, "<Keyboard>/h");
            Assert.That(action.GetBindingDisplayString(index), Is.Not.EqualTo(before));
            action.RemoveBindingOverride(index);
            Assert.That(action.GetBindingDisplayString(index), Is.EqualTo(before));
        }
        [Test] public void TutorialSkippedAndCompletedAreIndependentAndResettable()
        {
            var profile = new AudioPreferences("LastSignal.Tests.Unsaved.S017.");
            Assert.That(FirstUseGuide.Pending(profile, FirstUseGuide.Movement), Is.True);
            profile.TutorialSkipped = FirstUseGuide.Movement;
            Assert.That(FirstUseGuide.Pending(profile, FirstUseGuide.Movement), Is.False);
            Assert.That(profile.TutorialCompleted, Is.Zero);
            profile.TutorialCompleted = FirstUseGuide.Loot;
            Assert.That(FirstUseGuide.Pending(profile, FirstUseGuide.Loot), Is.False);
            Assert.That(FirstUseGuide.Pending(profile, FirstUseGuide.Recovery), Is.True);
            profile.TutorialCompleted = profile.TutorialSkipped = 0;
            Assert.That(FirstUseGuide.Pending(profile, FirstUseGuide.Movement | FirstUseGuide.Loot | FirstUseGuide.Recovery), Is.True);
        }
        [Test] public void UnknownControllerNeverReceivesAnXboxGlyph()
        {
            var catalog = Resources.Load<InputGlyphCatalog>("InputGlyphCatalog");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Resolve(InputDeviceFamily.KeyboardMouse, "<Keyboard>/e"), Is.Not.Null);
            Assert.That(catalog.Resolve(InputDeviceFamily.XboxStyle, "<Gamepad>/buttonNorth"), Is.Not.Null);
            Assert.That(catalog.Resolve(InputDeviceFamily.GenericGamepad, "<Gamepad>/buttonNorth"), Is.Null);
            Assert.That(catalog.Resolve(InputDeviceFamily.KeyboardMouse, "<Keyboard>/h"), Is.Null);
        }
    }
}
