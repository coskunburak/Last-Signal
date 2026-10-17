using System.IO;
using System.Linq;
using LastSignal.Editor.S019;
using LastSignal.Editor.S023;
using LastSignal.Loot;
using LastSignal.Persistence;
using LastSignal.WorldCells;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class S023RegionTests
    {
        [Test] public void PilotPreservesResidentTopologyAndIsolatesSaveIdentity()
        {
            Assert.That(File.Exists(S023RegionAuthoring.ScenePath), Is.True, "Run user author gate first.");
            var scene = EditorSceneManager.OpenPreviewScene(S023RegionAuthoring.ScenePath);
            try
            {
                var roots = scene.GetRootGameObjects();
                var save = roots.SelectMany(r => r.GetComponentsInChildren<SaveSession>(true)).Single();
                var manager = roots.SelectMany(r => r.GetComponentsInChildren<WorldCellManager>(true)).Single();
                Assert.That(save.SaveSlot, Is.EqualTo(S023RegionAuthoring.Slot));
                Assert.That(new SerializedObject(save).FindProperty("worldId").stringValue, Is.EqualTo(S023RegionAuthoring.World));
                Assert.That(save.DefaultPath, Is.Not.EqualTo(SaveSession.PathForSlot(Application.persistentDataPath, "current")));
                var definitions = S023RegionAuthoring.Definitions(manager);
                Assert.That(definitions.Select(d => d.coordinate.Id), Is.EquivalentTo(new[] { "cell:1:0", "cell:2:0", "cell:3:0", "cell:4:0" }));
                var issues = ContentValidation.Roots(definitions.Select(d => d.gameObject), scene.path);
                Assert.That(issues.Where(i => i.severity == ContentSeverity.Error), Is.Empty, string.Join("\n", issues));
                Assert.That(manager.ValidateTopology(new CellWorldSnapshot { playerCell = "resident", cells = new[] { new CellSnapshot { id = "cell:1:0" }, new CellSnapshot { id = "cell:2:0" } } }), Is.False,
                    "Old topology must be rejected explicitly, never silently migrated or reset.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [TestCase(3, 2)] [TestCase(4, 1)]
        public void CellReferencesOwnershipAndResourceCapsAreValid(int x, int points)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(S023RegionAuthoring.CellPath(x));
            Assert.That(prefab, Is.Not.Null, "Run user author gate first.");
            var content = prefab.GetComponent<WorldCellContent>();
            Assert.That(CellCoordinate.Size, Is.EqualTo(64));
            Assert.That(content.coordinate, Is.EqualTo(new CellCoordinate(x, 0)));
            Assert.That(content.navigation, Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<ZombieEncounter>(true).Length, Is.EqualTo(1));
            Assert.That(prefab.GetComponentsInChildren<SaveSession>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<WorldCellManager>(true), Is.Empty);
            var anchors = prefab.GetComponentsInChildren<LootSpawnPoint>(true);
            Assert.That(anchors.Length, Is.EqualTo(points));
            foreach (var point in anchors)
            {
                Assert.That(CellCoordinate.FromWorld(point.transform.position), Is.EqualTo(content.coordinate));
                Assert.That(point.Validate(out var error), Is.True, error);
                for (int i = 0; i < point.Profile.EntryCount; i++)
                {
                    var entry = point.Profile.GetEntry(i);
                    Assert.That(new[] { "material.scrap", "material.cloth", "fuel.generator" }, Does.Contain(entry.Item.Id.Value));
                    Assert.That(entry.MaxQuantity, Is.LessThanOrEqualTo(2));
                }
            }
            // S019's existing duplication check must also apply to S023 prefab instances.
            var one = Object.Instantiate(prefab); var two = Object.Instantiate(prefab);
            try
            {
                var issues = ContentValidation.Roots(new[] { one, two }, "duplicate-fixture.prefab");
                Assert.That(issues.Any(i => i.errorCode == "ENTITY_DUPLICATE"), Is.True);
            }
            finally { Object.DestroyImmediate(one); Object.DestroyImmediate(two); }
        }

        [Test] public void MaintenanceGuaranteesBoundedLootForPersistenceAcceptance()
        {
            var profile = AssetDatabase.LoadAssetAtPath<LootProfile>(S023RegionAuthoring.Data + "Maintenance.asset");
            Assert.That(profile, Is.Not.Null, "Run user author gate first.");
            var selection = profile.Select(S023RegionAuthoring.Seed, "s023.depot.scrap");
            Assert.That(selection.Outcome, Is.EqualTo(LootOutcome.Spawned));
            Assert.That(selection.Item.Id.Value, Is.EqualTo("material.scrap"));
            Assert.That(selection.Quantity, Is.InRange(1, 2));
        }
    }
}
