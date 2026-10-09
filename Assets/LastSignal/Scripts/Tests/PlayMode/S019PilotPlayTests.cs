#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using LastSignal.Authoring;
using LastSignal.Inventory;
using LastSignal.Loot;
using LastSignal.Persistence;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public sealed class S019PilotPlayTests
    {
        const string ScenePath = "Assets/LastSignal/Scenes/S019/S019Pilot.unity";
        string directory, path; SessionFlow flow; SaveSession saves;
        [UnitySetUp] public IEnumerator Setup()
        {
            Assert.That(File.Exists(ScenePath), Is.True, "Run user author-production gate first.");
            directory = Path.Combine(Path.GetTempPath(), "LastSignal-S019-" + Guid.NewGuid().ToString("N")); path = Path.Combine(directory, "pilot.json");
            Time.timeScale = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            saves = Object.FindAnyObjectByType<SaveSession>(); Assert.That(saves, Is.Not.Null);
            flow = saves.GetComponent<SessionFlow>(); flow.Resume();
            yield return new WaitForSeconds(.9f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (flow) flow.ReturnToMenu(); Time.timeScale = 1; yield return null;
            if (directory != null && Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        [UnityTest] public IEnumerator PilotLootConsumptionAndInventorySurviveSaveLoad()
        {
            Assert.That(saves.SaveSlot, Is.EqualTo("s019-pilot"));
            Assert.That(saves.DefaultPath, Is.Not.EqualTo(Path.Combine(Application.persistentDataPath, "saves", "current.json")));
            var layout = Object.FindAnyObjectByType<PoiAuthoringLayout>(); Assert.That(layout, Is.Not.Null);
            var loot = flow.GetComponent<LootPopulationService>(); Assert.That(loot.Populated, Is.True);
            foreach (var point in layout.lootAnchors)
            {
                var result = loot.Results.Single(r => r.PointId == point.StableId);
                Assert.That(result.Selection.Outcome, Is.EqualTo(LootOutcome.Spawned), point.name + ": " + result.Diagnostic);
            }
            string consumedId = "loot:" + layout.lootAnchors[0].StableId;
            var worldItem = Object.FindObjectsByType<WorldItem>(FindObjectsSortMode.None).Single(i => i.PersistentId == consumedId);
            Assert.That(worldItem.TryInteract(), Is.True);
            yield return null;
            Assert.That(InventorySnapshots.Capture(flow.Player.GetComponent<PlayerInventory>(), "player.inventory", out var before).Success, Is.True);
            Assert.That(saves.Save(path).Success, Is.True, saves.LastResult.Message);
            byte[] written = File.ReadAllBytes(path);
            flow.ReturnToMenu(); yield return null;
            yield return saves.Load(path);
            Assert.That(saves.LastResult.Success, Is.True, saves.LastResult.Message);
            Assert.That(InventorySnapshots.Capture(flow.Player.GetComponent<PlayerInventory>(), "player.inventory", out var after).Success, Is.True);
            Assert.That(JsonUtility.ToJson(after), Is.EqualTo(JsonUtility.ToJson(before)));
            Assert.That(Object.FindObjectsByType<WorldItem>(FindObjectsSortMode.None).Any(i => i.PersistentId == consumedId), Is.False, "Consumed pilot loot regenerated after load.");
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(written), "Loading must not rewrite the save.");
            Assert.That(Object.FindAnyObjectByType<ZombieEncounter>().Actor, Is.Not.Null);
        }
    }
}
#endif
