#if UNITY_EDITOR
using System.Collections;
using System.IO;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Persistence;
using LastSignal.Shelter;
using LastSignal.WorldTime;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public class S010IntegrationTests
    {
        SessionFlow flow;
        ShelterSite site;
        ShelterLoop loop;
        WorldClock clock;
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/Validation/WorldPopulationAcceptance.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; flow = Object.FindAnyObjectByType<SessionFlow>(); site = flow.GetComponent<ShelterSite>(); loop = flow.GetComponent<ShelterLoop>(); clock = flow.GetComponent<WorldClock>();
            flow.Resume(); yield return null; Assert.IsNotNull(site); Assert.IsNotNull(site.Production);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { if (flow) flow.ReturnToMenu(); yield return null; Time.timeScale = 1; }
        void Place(Vector3 point)
        {
            var capsule = flow.Player.GetComponent<CharacterController>(); capsule.enabled = false; flow.Player.transform.position = point; capsule.enabled = true; Physics.SyncTransforms();
        }
        void Open(int socket)
        {
            Place(new Vector3(site.sockets[socket].transform.position.x + 1, .05f, site.sockets[socket].transform.position.z - (socket == 0 ? 1.1f : 0)));
            var view = flow.Player.GetComponent<FirstPersonLook>().View; view.transform.LookAt(site.sockets[socket].transform.position);
            Physics.SyncTransforms(); Assert.IsTrue(flow.Player.GetComponent<InteractionController>().TryInteract(), "Production interaction ray reaches authored socket.");
        }
        void Modules()
        {
            foreach (var enemy in Object.FindObjectsByType<ZombieHealth>())
                if (enemy.IsAlive) enemy.TakeDamage(new DamageInfo { Amount = 1000 });
            loop.Inventory.TryAdd(site.material, 20);
            Open(0); Assert.That(site.Act(0), Does.StartWith("Shelter claimed")); Assert.That(site.Act(1), Does.Contain("installed")); site.Close();
            Open(1); Assert.That(site.Act(1), Does.Contain("installed")); site.Close();
            Open(2); Assert.That(site.Act(1), Does.Contain("installed")); site.Close();
        }
        [UnityTest] public IEnumerator ClaimRejectsLiveThreatDuplicateInvalidAndRemoteWithoutDeletingActors()
        {
            Open(0); var zombie = Object.FindAnyObjectByType<ZombieHealth>(); Assert.IsNotNull(zombie); float health = zombie.CurrentHealth;
            var old = zombie.transform.position; var agent = zombie.GetComponent<UnityEngine.AI.NavMeshAgent>(); bool enabled = agent && agent.enabled; if (agent) agent.enabled = false;
            zombie.transform.position = flow.Player.transform.position + Vector3.right * 2; Physics.SyncTransforms();
            Assert.That(site.Act(0), Does.Contain("threat")); Assert.IsFalse(site.Production.Claimed); Assert.AreEqual(health, zombie.CurrentHealth);
            zombie.transform.position = new Vector3(15, 0, 0); Physics.SyncTransforms();
            Assert.That(site.Act(0), Does.StartWith("Shelter claimed")); Assert.That(site.Act(0), Does.Contain("Already"));
            Assert.AreEqual(health, zombie.CurrentHealth); Assert.IsTrue(zombie.IsAlive);
            string id = site.shelterId; site.shelterId = ""; Assert.That(site.Claim(), Does.Contain("Invalid")); site.shelterId = id;
            Place(Vector3.zero); Assert.That(site.Act(0), Does.Contain("unavailable")); yield return null;
        }
        [UnityTest] public IEnumerator InvalidSocketAndRemoteStorageConsumeNothing()
        {
            Modules(); Open(2); int before = loop.Inventory.GetTotalQuantity(site.material); Assert.That(site.Act(1), Does.Contain("occupied")); Assert.AreEqual(before, loop.Inventory.GetTotalQuantity(site.material)); site.Close();
            Place(new Vector3(loop.StoragePoint.transform.position.x + 1, .05f, loop.StoragePoint.transform.position.z)); Assert.IsTrue(loop.TryUse(loop.StoragePoint));
            Assert.AreEqual(1, loop.Transfer(true, site.material, 1).Moved);
            Place(Vector3.zero); Assert.AreEqual(0, loop.Transfer(true, site.material, 1).Moved); Assert.AreEqual(1, loop.Storage.GetTotalQuantity(site.material)); loop.ClosePreparation();
            yield return null;
        }
        [UnityTest] public IEnumerator LegacyShelterAndMalformedSnapshotAreValidatedBeforeMutation()
        {
            site.Restore(null);
            Assert.IsTrue(site.Production.Claimed); Assert.IsTrue(site.Production.Installed(ShelterModule.Storage));
            Assert.IsTrue(site.Production.Installed(ShelterModule.Bed)); Assert.IsFalse(site.Production.Installed(ShelterModule.Workbench));
            var before = site.Production.Capture(); var bad = site.Production.Capture(); bad.fuelSeconds = double.NaN;
            Assert.IsFalse(site.CanRestore(bad, clock.Simulation.Seconds));
            Assert.AreEqual(JsonUtility.ToJson(before), JsonUtility.ToJson(site.Production.Capture()));
            Assert.IsTrue(InventorySnapshots.Capture(loop.Storage, "shelter.storage", out var stash).Success);
            stash.capacity = 48; stash.slots = new SlotSnapshot[48];
            for (int i = 0; i < stash.slots.Length; i++) stash.slots[i] = new SlotSnapshot();
            stash.slots[47] = new SlotSnapshot { definitionId = site.recipe.output.Id.Value, quantity = 10 };
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/LastSignal/Data/Items/Definitions/ItemCatalog.asset");
            Assert.IsTrue(InventorySnapshots.Restore(loop.Storage, stash, "shelter.storage", catalog).Success);
            Place(new Vector3(loop.StoragePoint.transform.position.x + 1, .05f, loop.StoragePoint.transform.position.z));
            Assert.IsTrue(loop.TryUse(loop.StoragePoint));
            Assert.AreEqual(loop.Inventory.Capacity + 48, loop.PreparationUI.GetComponentsInChildren<ShelterSlotUI>().Length, "Legacy restored capacity is accessible in the actual UI.");
            Assert.AreEqual(10, loop.Transfer(false, site.recipe.output, 10).Moved); loop.ClosePreparation();
            yield return null;
        }
        [UnityTest] public IEnumerator ProductionSaveLoadFuelNoiseOutputAndNewSession()
        {
            Modules(); loop.Storage.TryAdd(site.material, 12); loop.Storage.TryAdd(site.fuel, 2); loop.Storage.TryAdd(site.recipe.tool, 1);
            Open(2); Assert.That(site.Act(2), Does.StartWith("Done")); Assert.That(site.Act(2), Does.StartWith("Rejected"));
            Assert.That(site.Act(3), Does.StartWith("Done")); Assert.That(site.Act(3), Does.StartWith("Rejected")); Assert.That(site.Act(4), Does.StartWith("Done"));
            Assert.AreEqual(12, loop.Storage.GetTotalQuantity(site.material));
            Assert.That(site.Act(7), Does.StartWith("Done")); Assert.That(site.Act(2), Does.StartWith("Done")); Assert.That(site.Act(5), Does.StartWith("Done")); Assert.That(site.Act(6), Does.StartWith("Done")); site.Close();
            double start = clock.Simulation.Seconds; clock.Simulation.AdvanceUntil(start + 120, 0); Assert.That(site.Production.Progress, Is.EqualTo(.4).Within(.001));
            Assert.AreEqual(Noise.GameplayNoiseCategory.Generator, flow.Noise.LastTrace.Event.Category);
            var saves = flow.GetComponent<SaveSession>(); string path = Path.Combine(Application.temporaryCachePath, "s010-test-save.json");
            Assert.IsTrue(saves.Save(path).Success, saves.LastResult.Message); var snap = site.Production.Capture();
            flow.ReturnToMenu(); yield return null; yield return saves.Load(path); Assert.IsTrue(saves.LastResult.Success, saves.LastResult.Message);
            Assert.AreEqual(snap.job.id, site.Production.Capture().job.id); Assert.IsTrue(site.Production.Upgraded);
            clock.Simulation.AdvanceUntil(clock.Simulation.Seconds + 180, 0); Assert.AreEqual(CraftStatus.CompletedWaitingOutput, site.Production.Status);
            Assert.IsTrue(saves.Save(path).Success); flow.ReturnToMenu(); yield return null; yield return saves.Load(path); Assert.IsTrue(saves.LastResult.Success, saves.LastResult.Message);
            Open(2); Assert.That(site.Act(4), Does.StartWith("Done")); Assert.That(site.Act(4), Does.StartWith("Rejected")); Assert.AreEqual(10, loop.Storage.GetTotalQuantity(site.recipe.output)); site.Close();
            flow.ReturnToMenu(); yield return null; flow.BeginSession(); yield return null;
            Assert.IsFalse(site.Production.Claimed); Assert.IsNull(site.Production.Status); Assert.AreEqual(0, site.Production.FuelSeconds); Assert.AreEqual(0, loop.Storage.GetTotalQuantity(site.recipe.output));
        }
    }
}
#endif
