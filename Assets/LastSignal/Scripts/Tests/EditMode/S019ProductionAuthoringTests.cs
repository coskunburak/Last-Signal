using System;
using System.IO;
using System.Linq;
using LastSignal.Authoring;
using LastSignal.Editor.S019;
using LastSignal.Persistence;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public sealed class S019ProductionAuthoringTests
    {
        [Test] public void LegacyDefaultSaveSlotRemainsCurrent()
        {
            var go = new GameObject("Slot fixture"); go.SetActive(false);
            try
            {
                var save = go.AddComponent<SaveSession>();
                Assert.That(save.SaveSlot, Is.EqualTo("current"));
                Assert.That(save.DefaultPath, Is.EqualTo(Path.Combine(Application.persistentDataPath, "saves", "current.json")));
                Assert.That(SaveSession.PathForSlot(Application.persistentDataPath, S019ProductionAuthoring.PilotSlot), Is.Not.EqualTo(save.DefaultPath));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [TestCase(null)] [TestCase("")] [TestCase("../current")] [TestCase("/tmp/current")]
        [TestCase("..\\current")] [TestCase("current.json")] [TestCase("bad slot")]
        public void UnsafeSaveSlotRejected(string slot) => Assert.Throws<ArgumentException>(() => SaveSession.PathForSlot("/tmp", slot));
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void NativePresetsAreScopedAndReadable(int index)
        {
            var preset = AssetDatabase.LoadAssetAtPath<Preset>(S019ProductionAuthoring.PresetPath(index));
            Assert.That(preset, Is.Not.Null, "Run user author-production gate first.");
            var importer = AssetImporter.GetAtPath(S019ProductionAuthoring.CopyPath(index));
            Assert.That(importer, Is.Not.Null); Assert.That(preset.CanBeAppliedTo(importer), Is.True);
            var leaves = ContentImportPolicy.Fields(S019ProductionAuthoring.Families[index]);
            var applied = preset.PropertyModifications.Where(p => !preset.excludedProperties.Any(e => p.propertyPath == e || p.propertyPath.StartsWith(e + ".", StringComparison.Ordinal))).ToArray();
            Assert.That(applied, Is.Not.Empty);
            Assert.That(applied.All(p => leaves.Any(l => p.propertyPath == l || p.propertyPath.EndsWith("." + l, StringComparison.Ordinal))), Is.True, "Preset includes unapproved settings.");
            Assert.That(ContentImportPolicy.Apply(S019ProductionAuthoring.Exemplars[index], AssetImporter.GetAtPath(S019ProductionAuthoring.Exemplars[index]), preset), Is.False, "Original exemplar must remain outside auto-import scope.");
        }
        [Test] public void PilotSceneHasOneExistingSessionAndIsolatedSaveSlot()
        {
            Assert.That(File.Exists(S019ProductionAuthoring.ScenePath), Is.True, "Run user author-production gate first.");
            var scene = EditorSceneManager.OpenPreviewScene(S019ProductionAuthoring.ScenePath);
            try
            {
                var roots = scene.GetRootGameObjects();
                var layouts = roots.SelectMany(r => r.GetComponentsInChildren<PoiAuthoringLayout>(true)).ToArray();
                var saves = roots.SelectMany(r => r.GetComponentsInChildren<SaveSession>(true)).ToArray();
                Assert.That(layouts.Length, Is.EqualTo(1)); Assert.That(saves.Length, Is.EqualTo(1));
                Assert.That(saves[0].SaveSlot, Is.EqualTo(S019ProductionAuthoring.PilotSlot));
                Assert.That(saves[0].ValidateAuthoring().Success, Is.True);
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<ZombieEncounter>(true)).Count(), Is.EqualTo(1));
                var issues = ContentValidation.Roots(roots, scene.path);
                Assert.That(issues.Where(i => i.severity == ContentSeverity.Error), Is.Empty, string.Join("\n", issues.Select(i => i.ToString())));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
