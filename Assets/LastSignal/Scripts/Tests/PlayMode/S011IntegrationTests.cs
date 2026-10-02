#if UNITY_EDITOR
using System.Collections;
using System.IO;
using LastSignal.Objectives;
using LastSignal.Inventory;
using LastSignal.Persistence;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace LastSignal.Tests
{
    public sealed class S011IntegrationTests
    {
        SessionFlow flow; RelayMission mission; RelayAcceptance driver; string directory;
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale=1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/RelayExpedition.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; flow=Object.FindAnyObjectByType<SessionFlow>(); mission=flow.GetComponent<RelayMission>();
            driver=new GameObject("S011 test driver").AddComponent<RelayAcceptance>(); driver.flow=flow; driver.mission=mission;
            directory=Path.Combine(Application.temporaryCachePath,"s011-tests-"+System.Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); flow.Resume(); yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup() { if(flow) flow.ReturnToMenu(); if(driver) Object.Destroy(driver.gameObject); yield return null; Time.timeScale=1; }
        [UnityTest] public IEnumerator ClueFirstProductionRoute() { yield return driver.Run(false,false,directory,"clue-first"); }
        [UnityTest] public IEnumerator FuseFirstProductionRoute() { yield return driver.Run(true,false,directory,"fuse-first"); }
        [UnityTest] public IEnumerator DroppedFuseRecoveryProductionRoute() { yield return driver.Run(true,true,directory,"recovery"); }
        [UnityTest] public IEnumerator SessionReplacementDoesNotRetainAcquisitionOrCallbacks()
        {
            driver.Pick(mission.fuse); yield return null; Assert.IsTrue(mission.Progress.Acquired); var old=mission.Progress;
            flow.ReturnToMenu(); yield return null; flow.BeginSession(); yield return null;
            Assert.IsFalse(mission.Progress.Acquired); old.DiscoverRadio(); Assert.IsFalse(mission.Progress.Radio); Assert.AreEqual(0,mission.Pending);
        }
        [UnityTest] public IEnumerator ThreatRejectionDoesNotMutateEnemy()
        {
            driver.Pick(mission.fuse); driver.Pick(mission.tool); driver.Use(mission.radio); mission.CloseJournal();
            driver.Place(new Vector3(mission.relay.transform.position.x+1.5f,.05f,mission.relay.transform.position.z));
            var enemy=Object.FindAnyObjectByType<ZombieHealth>(); var agent=enemy.GetComponent<UnityEngine.AI.NavMeshAgent>(); agent.enabled=false;
            enemy.transform.position=flow.Player.transform.position+Vector3.forward*2; Physics.SyncTransforms();
            float health=enemy.CurrentHealth; Assert.AreEqual(0,mission.BeginRepair()); Assert.That(mission.Feedback,Does.Contain("threat")); Assert.AreEqual(health,enemy.CurrentHealth);
            Assert.AreEqual(1,flow.Player.GetComponent<PlayerInventory>().GetTotalQuantity(mission.fuse)); yield return null;
        }
        [UnityTest] public IEnumerator CancellationBoundariesAndNewThreatRetainFuse()
        {
            driver.Pick(mission.fuse); driver.Pick(mission.tool); driver.Use(mission.radio); mission.CloseJournal();
            var enemy=Object.FindAnyObjectByType<ZombieHealth>(); var agent=enemy.GetComponent<UnityEngine.AI.NavMeshAgent>(); agent.enabled=false;
            enemy.transform.position=new Vector3(-100,0,-100); Physics.SyncTransforms();
            foreach(float delay in new[]{0f,1.5f,2.8f})
            {
                driver.Use(mission.relay); long token=mission.Pending; if(delay>0) yield return new WaitForSeconds(delay);
                mission.CancelRepair(); Assert.IsFalse(mission.FinishRepair(token)); Assert.AreEqual(1,flow.Player.GetComponent<PlayerInventory>().GetTotalQuantity(mission.fuse));
            }
            driver.Use(mission.relay); long stale=mission.Pending; enemy.transform.position=flow.Player.transform.position+Vector3.forward*2; Physics.SyncTransforms(); yield return null;
            Assert.AreEqual(0,mission.Pending); Assert.IsFalse(mission.FinishRepair(stale)); Assert.IsFalse(mission.Progress.Repaired); Assert.AreEqual(1,flow.Player.GetComponent<PlayerInventory>().GetTotalQuantity(mission.fuse));
        }
        [UnityTest] public IEnumerator InaccessibleDropRecallPreservesIdentityAndQuantity()
        {
            yield return driver.ClearThreat(); driver.Pick(mission.fuse); yield return null;
            var inventory=flow.Player.GetComponent<PlayerInventory>(); int slot=-1; for(int index=0;index<inventory.Capacity;index++) if(inventory.GetSlot(index).Item==mission.fuse) slot=index;
            Assert.IsTrue(inventory.TryDrop(slot,1)); yield return null;
            WorldItem dropped=null; foreach(var item in Object.FindObjectsByType<WorldItem>()) if(item.Available&&item.Definition==mission.fuse) dropped=item;
            Assert.IsNotNull(dropped); string identity=dropped.PersistentId;
            // Controlled inaccessible-location fixture, not a reward or acquisition shortcut.
            dropped.transform.position=new Vector3(20,-1000,20); Physics.SyncTransforms();
            driver.Use(mission.radio); Assert.IsTrue(mission.Recover()); Assert.AreEqual(identity,dropped.PersistentId); Assert.AreEqual(1,dropped.Quantity);
            mission.CloseJournal(); driver.Pick(mission.fuse); yield return null; Assert.AreEqual(1,inventory.GetTotalQuantity(mission.fuse));
        }
        [UnityTest] public IEnumerator StorageOwnerPreventsRecoveryDuplication()
        {
            yield return driver.ClearThreat(); driver.Pick(mission.fuse); yield return null;
            var inventory=flow.Player.GetComponent<PlayerInventory>(); var storage=flow.GetComponent<LastSignal.Shelter.ShelterLoop>().Storage;
            Assert.AreEqual(1,LastSignal.Shelter.ItemTransferService.Deposit(inventory,storage,mission.fuse,1).Moved);
            driver.Use(mission.radio); Assert.IsFalse(mission.Recover()); Assert.That(mission.Feedback,Does.Contain("storage")); Assert.AreEqual(1,storage.GetTotalQuantity(mission.fuse));
            Assert.AreEqual(0,inventory.GetTotalQuantity(mission.fuse)); mission.CloseJournal(); Assert.IsTrue(flow.GetComponent<SaveSession>().Capture(out _).Success);
        }
        [UnityTest] public IEnumerator StaleCellHydrationCannotRollBackContact()
        {
            var cells=flow.GetComponent<LastSignal.WorldCells.WorldCellManager>(); string id=null; foreach(var candidate in cells.DefinedCellIds) { id=candidate; break; }
            Assert.IsTrue(cells.Request(id)); float timeout=Time.realtimeSinceStartup+15;
            while(cells.State(id)!=LastSignal.WorldCells.CellState.Ready&&Time.realtimeSinceStartup<timeout) yield return null;
            Assert.IsTrue(cells.Unload(id)); var stale=cells.Capture();
            yield return driver.Run(false,false,directory,"stale-cell-route");
            yield return cells.Restore(stale,flow.GetComponent<LastSignal.WorldTime.WorldClock>().Simulation.Seconds);
            Assert.IsTrue(cells.Request(id)); timeout=Time.realtimeSinceStartup+15;
            while(cells.State(id)!=LastSignal.WorldCells.CellState.Ready&&Time.realtimeSinceStartup<timeout) yield return null;
            Assert.AreEqual(LastSignal.WorldCells.CellState.Ready,cells.State(id)); Assert.AreEqual(1,mission.Progress.Phase); Assert.AreEqual(1,mission.Progress.RewardCount); Assert.IsTrue(mission.Progress.Repaired);
        }
        [UnityTest] public IEnumerator UnloadedCellFuseRecallPreservesOneOwner()
        {
            yield return driver.ClearThreat(); driver.Pick(mission.fuse); yield return null;
            var cells=flow.GetComponent<LastSignal.WorldCells.WorldCellManager>(); string id=null; foreach(var candidate in cells.DefinedCellIds) { id=candidate; break; }
            Assert.IsNotNull(id); Assert.IsTrue(cells.Request(id));
            float timeout=Time.realtimeSinceStartup+15; while(cells.State(id)!=LastSignal.WorldCells.CellState.Ready&&Time.realtimeSinceStartup<timeout) yield return null;
            Assert.IsTrue(cells.TryEnter(id)); var inventory=flow.Player.GetComponent<PlayerInventory>();
            int slot=-1; for(int index=0;index<inventory.Capacity;index++) if(inventory.GetSlot(index).Item==mission.fuse) slot=index;
            Assert.IsTrue(inventory.TryDrop(slot,1)); yield return null;
            Assert.IsTrue(cells.ReturnToResident(new Vector3(-13.5f,.05f,9))); yield return null;
            Assert.AreEqual(LastSignal.WorldCells.CellState.Unloaded,cells.State(id));
            driver.Use(mission.radio); Assert.IsTrue(mission.Recover()); mission.Recover(); mission.CloseJournal(); driver.Pick(mission.fuse); yield return null;
            Assert.AreEqual(1,inventory.GetTotalQuantity(mission.fuse));
            Assert.IsTrue(flow.GetComponent<SaveSession>().Capture(out var snapshot).Success);
            foreach(var cell in snapshot.cells.cells) if(cell.visited) foreach(var item in cell.world.items) Assert.IsFalse(item.definitionId==RelayProgression.FuseId&&item.disposition==EntityDisposition.Present);
        }
        [UnityTest] public IEnumerator RealS010CheckpointMigratesWithoutChangingOriginalFile()
        {
            string path=Path.GetFullPath("Docs/Implementation/S010/Evidence/20260928-closure/route-save.json"); string original=File.ReadAllText(path);
            var saves=flow.GetComponent<SaveSession>(); flow.ReturnToMenu(); yield return null; yield return saves.Load(path);
            Assert.IsTrue(saves.LastResult.Success,saves.LastResult.Message); Assert.AreEqual(original,File.ReadAllText(path));
            Assert.IsFalse(mission.Progress.Radio); Assert.IsFalse(mission.Progress.Repaired);
            var actor = flow.GetComponent<ZombieEncounter>().Actor;
            var anatomy = actor.GetComponent<ZombieDismemberment>()?.CaptureState();
            Assert.IsTrue(SaveValidation.ValidZombieAnatomy(anatomy),
                $"Runtime anatomy mask={anatomy?.severedMask}, torso={anatomy?.torsoDamage}, regions={anatomy?.regionalDamage?.Length}");
            var capture = saves.Capture(out _);
            Assert.IsTrue(capture.Success, capture.Message);
        }
        [UnityTest] public IEnumerator DeathReturnsToCheckpointWithoutInventingRecoveryBag()
        {
            yield return driver.ClearThreat(); driver.Pick(mission.fuse); yield return null;
            string path=Path.Combine(directory,"death-checkpoint.json"); var saves=flow.GetComponent<SaveSession>(); Assert.IsTrue(saves.Save(path).Success,saves.LastResult.Message);
            flow.Player.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo { Amount=1000 }); yield return null;
            Assert.IsTrue(flow.PlayerDead); Assert.IsFalse(saves.Save(path).Success); flow.ReturnToMenu(); yield return null; yield return saves.Load(path);
            Assert.IsTrue(saves.LastResult.Success,saves.LastResult.Message); Assert.AreEqual(1,flow.Player.GetComponent<PlayerInventory>().GetTotalQuantity(mission.fuse)); Assert.IsTrue(mission.Progress.Acquired);
        }
    }
}
#endif
