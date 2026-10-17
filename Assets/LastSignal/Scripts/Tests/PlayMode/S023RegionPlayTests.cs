#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using LastSignal.AI;
using LastSignal.Inventory;
using LastSignal.Persistence;
using LastSignal.WorldCells;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public sealed class S023RegionPlayTests
    {
        const string A = "cell:3:0", B = "cell:4:0";
        const string ScenePath = "Assets/LastSignal/Scenes/S023/S023RegionPilot.unity";
        WorldCellManager cells; SessionFlow flow; SaveSession saves; string directory;
        [UnitySetUp] public IEnumerator Setup()
        {
            Assert.That(File.Exists(ScenePath), Is.True, "User must run Tools/s023-author.py first.");
            directory = Path.Combine(Path.GetTempPath(), "LastSignal-S023-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory); Time.timeScale = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            cells = Object.FindAnyObjectByType<WorldCellManager>(); Assert.That(cells, Is.Not.Null);
            flow = cells.GetComponent<SessionFlow>(); saves = cells.GetComponent<SaveSession>(); flow.Resume();
            Assert.That(saves.SaveSlot, Is.EqualTo("s023-pilot"));
            // Isolate authored encounter persistence from the separately tested S022 materializer.
            cells.GetComponent<WorldPopulationManager>().MaterializationEnabled = false;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (flow) flow.ReturnToMenu(); Time.timeScale = 1; yield return null;
            if (directory != null && Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        IEnumerator Enter(string id)
        {
            yield return WorldCellAcceptanceRoute.Ready(cells, id);
            Assert.That(cells.Content(id).CollisionReady(), Is.True, id);
            Assert.That(cells.TryEnter(id), Is.True, cells.Failure);
            // Persistence fixture is not a combat timing test.
            if (cells.Content(id).encounter.Actor) cells.Content(id).encounter.Actor.SetPaused(true);
        }
        [UnityTest] public IEnumerator DepotYardResidentRoundTripPreservesDoorLootDropAndEnemy()
        {
            var home = cells.GetComponent<LastSignal.Shelter.ShelterLoop>().OutsideAnchor.position;
            Assert.That(cells.Request(A), Is.True); Assert.That(cells.TryEnter(A), Is.False);
            Assert.That(saves.Capture(out _).Error, Is.EqualTo(SaveError.Busy));
            yield return Enter(A);
            var content = cells.Content(A);
            var item = content.GetComponentsInChildren<WorldItem>().Single(i => i.PersistentId == "loot:s023.depot.scrap");
            Assert.That(item.TryInteract(), Is.True, "Guaranteed depot scrap must be reachable/populated.");
            yield return null;
            var inventory = flow.Player.GetComponent<PlayerInventory>();
            int slot = Enumerable.Range(0, inventory.Capacity).First(i => !inventory.GetSlot(i).IsEmpty && inventory.GetSlot(i).Item.Id.Value == "material.scrap");
            Assert.That(inventory.TryDrop(slot, 1), Is.True);
            yield return null;
            var drop = content.GetComponentsInChildren<WorldItem>().Single(i => i.Origin == WorldItemOrigin.Drop);
            string dropId = drop.PersistentId; Vector3 dropPosition = drop.transform.position;
            var door = content.GetComponentInChildren<DoorInteractable>();
            Assert.That(door.TryInteract(), Is.True);
            double deadline = Time.realtimeSinceStartupAsDouble + 5;
            while (door.Busy && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(door.IsOpen && !door.Busy, Is.True, "Depot door blocked by authored geometry.");
            // SetPaused also disables incoming damage. Resume synchronously before the
            // lethal transaction; no frame elapses in which the actor can attack.
            var actor = content.encounter.Actor;
            var health = actor.GetComponent<ZombieHealth>();
            actor.SetPaused(false);
            var death = health.ReceiveDamage(new DamageInfo { Amount = 1000, Category = DamageCategory.Melee });
            Assert.That(death.Committed, Is.True, "Fixture lethal damage was rejected before traversal.");
            Assert.That(death.Terminal, Is.True, "Fixture did not commit a terminal damage receipt.");
            Assert.That(health.IsAlive, Is.False, "Fixture must kill the encounter before testing death persistence.");
            Assert.That(content.encounter.CaptureEnemy().health, Is.Zero, "Encounter did not capture the committed death.");
            yield return Enter(B); Assert.That(cells.State(A), Is.EqualTo(CellState.Unloaded));
            Assert.That(cells.Capture().cells.Single(c => c.id == A).world.enemies.Single().health, Is.Zero,
                "Unloaded depot snapshot lost the committed death.");
            Assert.That(cells.ReturnToResident(home), Is.True); Assert.That(cells.State(B), Is.EqualTo(CellState.Unloaded));
            string path = Path.Combine(directory, "roundtrip.json");
            flow.Pause(); Assert.That(saves.Save(path).Success, Is.True, saves.LastResult.Message);
            flow.ReturnToMenu(); yield return null; yield return saves.Load(path);
            Assert.That(saves.LastResult.Success, Is.True, saves.LastResult.Message); flow.Resume();
            yield return Enter(A);
            AssertDepot(dropId, dropPosition);
            // Save while inside the new cell, then restore into that cell as well.
            flow.Pause(); Assert.That(saves.Save(path).Success, Is.True, saves.LastResult.Message);
            flow.ReturnToMenu(); yield return null; yield return saves.Load(path);
            Assert.That(saves.LastResult.Success, Is.True, saves.LastResult.Message); flow.Resume();
            Assert.That(cells.CurrentCell, Is.EqualTo(A)); AssertDepot(dropId, dropPosition);
            for (int cycle = 0; cycle < 3; cycle++)
            {
                yield return Enter(B); yield return Enter(A); AssertDepot(dropId, dropPosition);
            }
        }
        void AssertDepot(string dropId, Vector3 position)
        {
            var content = cells.Content(A);
            Assert.That(content.GetComponentInChildren<DoorInteractable>().IsOpen, Is.True, "Door reset.");
            Assert.That(content.encounter.Actor.GetComponent<ZombieHealth>().IsAlive, Is.False, "Enemy revived.");
            var items = content.GetComponentsInChildren<WorldItem>();
            Assert.That(items.Any(i => i.PersistentId == "loot:s023.depot.scrap"), Is.False, "Consumed opportunity respawned.");
            var drops = items.Where(i => i.PersistentId == dropId).ToArray(); Assert.That(drops.Length, Is.EqualTo(1));
            Assert.That(Vector3.Distance(drops[0].transform.position, position), Is.LessThan(.01f));
        }
        [UnityTest] public IEnumerator PilotCancellationAndMissingNavigationFailClosed()
        {
            Assert.That(cells.Request(A), Is.True); Assert.That(cells.Cancel(A), Is.True);
            yield return null; yield return null; Assert.That(cells.State(A), Is.EqualTo(CellState.Unloaded));
            var serialized = new UnityEditor.SerializedObject(cells).FindProperty("definitions");
            var definitions = Enumerable.Range(0, serialized.arraySize)
                .Select(i => serialized.GetArrayElementAtIndex(i).objectReferenceValue as WorldCellContent).ToArray();
            var broken = Object.Instantiate(definitions.Single(d => d.coordinate.Id == B));
            broken.navigation = null;
            try
            {
                flow.ReturnToMenu(); yield return null;
                cells.Configure(definitions.Select(d => d.coordinate.Id == B ? broken : d).ToArray(), saves.Catalog);
                flow.BeginSession(); flow.Resume();
                Assert.That(cells.Request(B), Is.True);
                for (int i = 0; i < 30 && cells.State(B) != CellState.Failed; i++) yield return null;
                Assert.That(cells.State(B), Is.EqualTo(CellState.Failed));
                Assert.That(cells.TryEnter(B), Is.False);
                Assert.That(cells.Failure, Does.Contain("navigation"));
                Assert.That(cells.Content(B), Is.Null);
            }
            finally
            {
                flow.ReturnToMenu(); cells.Configure(definitions, saves.Catalog);
                Object.Destroy(broken.gameObject);
            }
        }
    }
}
#endif
