#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
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
    public class PersistenceIntegrationTests
    {
        SaveSession saves; SessionFlow flow; string directory,path;
        [UnitySetUp] public IEnumerator Setup()
        {
            directory=Path.Combine(Path.GetTempPath(),"LastSignal-Integration-"+Guid.NewGuid().ToString("N"));path=Path.Combine(directory,"current.json");
            Time.timeScale=1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/Validation/PersistenceAcceptance.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; saves=Object.FindAnyObjectByType<SaveSession>();flow=saves.GetComponent<SessionFlow>();flow.Resume();yield return new WaitForSeconds(.9f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(flow)flow.ReturnToMenu();Time.timeScale=1;yield return null;
            if(Directory.Exists(directory))Directory.Delete(directory,true);
        }
        [UnityTest] public IEnumerator RealSavedLootRouteThreeCleanSessionsConserveAllOwners()
        {yield return PersistenceAcceptanceRoute.Run(saves,path,s=>TestContext.Out.WriteLine(s));}
        [UnityTest] public IEnumerator CorruptFileNeverCreatesSessionOrChangesOriginal()
        {
            Assert.IsTrue(saves.Save(path).Success);flow.ReturnToMenu();yield return null;
            File.WriteAllText(path,"corrupt");yield return saves.Load(path);
            Assert.IsFalse(saves.LastResult.Success);Assert.IsTrue(flow.InMenu);Assert.AreEqual("corrupt",File.ReadAllText(path));
        }
        [UnityTest] public IEnumerator StaleHydrationCannotMutateReplacementSession()
        {
            Assert.IsTrue(saves.Save(path).Success);flow.ReturnToMenu();yield return null;
            var load=saves.Load(path);Assert.IsTrue(load.MoveNext());Assert.IsTrue(flow.Restoring);
            Assert.IsFalse(flow.Player.GetComponent<PlayerInputReader>().GameplayActive);
            flow.Resume();Assert.IsTrue(flow.Restoring);Assert.IsFalse(flow.Player.GetComponent<PlayerInputReader>().GameplayActive);
            flow.ReturnToMenu();flow.BeginSession();var next=flow.Player;
            Assert.IsFalse(load.MoveNext());Assert.AreEqual(SaveError.StaleSession,saves.LastResult.Error);Assert.AreSame(next,flow.Player);
            yield return null;
        }
        [UnityTest] public IEnumerator HydrationObserverFailureReturnsToSafeMenuWithoutOverwritingSave()
        {
            Assert.IsTrue(saves.Save(path).Success);var original=File.ReadAllBytes(path);flow.ReturnToMenu();yield return null;
            var load=saves.Load(path);Assert.IsTrue(load.MoveNext());
            flow.Player.GetComponent<PlayerInventory>().InventoryChanged+=()=>throw new InvalidOperationException("injected hydrate observer");
            yield return null;
            Assert.IsFalse(load.MoveNext());Assert.IsFalse(saves.LastResult.Success);Assert.IsTrue(flow.InMenu);
            CollectionAssert.AreEqual(original,File.ReadAllBytes(path));Assert.IsFalse(flow.Restoring);
        }
        [UnityTest] public IEnumerator RepeatedLoadWhilePlayingIsRejectedWithoutMutation()
        {
            Assert.IsTrue(saves.Save(path).Success);var player=flow.Player;
            yield return saves.Load(path);Assert.AreEqual(SaveError.Busy,saves.LastResult.Error);Assert.AreSame(player,flow.Player);
        }
        [UnityTest] public IEnumerator MissingGeneratedObjectIsNotMistakenForConsumption()
        {
            WorldItem item=null;foreach(var w in Object.FindObjectsByType<WorldItem>())if(w.Origin==WorldItemOrigin.Loot){item=w;break;}
            Assert.IsNotNull(item);item.gameObject.SetActive(false);
            Assert.AreEqual(SaveError.InvalidData,saves.Capture(out _).Error);
            item.gameObject.SetActive(true);Assert.IsTrue(saves.Capture(out _).Success);yield return null;
        }
        [UnityTest] public IEnumerator DisposedLoadCannotStrandRestoringSession()
        {
            Assert.IsTrue(saves.Save(path).Success);flow.ReturnToMenu();yield return null;
            var load=saves.Load(path);Assert.IsTrue(load.MoveNext());Assert.IsTrue(flow.Restoring);
            ((IDisposable)load).Dispose();Assert.IsTrue(flow.InMenu);Assert.IsFalse(flow.Restoring);
            yield return null;yield return saves.Load(path);Assert.IsTrue(saves.LastResult.Success,saves.LastResult.Message);
        }

        [UnityTest] public IEnumerator S014ReloadBeforeCommitSurvivesSaveDeathAndLoad()
        { yield return ReloadSaveDeathAndLoad(false); }

        [UnityTest] public IEnumerator S014ReloadAfterCommitSurvivesSaveDeathAndLoad()
        { yield return ReloadSaveDeathAndLoad(true); }

        IEnumerator ReloadSaveDeathAndLoad(bool afterCommit)
        {
            var combat = flow.Player.GetComponent<PlayerCombatController>();
            var weapon = combat.Firearm;
            Assert.IsNotNull(weapon);
            yield return WaitForRifleReady(weapon);
            var inventory = flow.Player.GetComponent<PlayerInventory>();
            Assert.AreEqual(60, inventory.TryAdd(weapon.Definition.Ammunition, 60));
            var state = weapon.RuntimeState;
            Assert.IsTrue(state.TryConsumeShot()); // Seed a partial magazine without affecting the encounter.
            int total = state.TotalAmmo;
            int commits = 0;
            weapon.ReloadCommitted += () => commits++;
            weapon.OnReloadRequested();
            Assert.AreEqual(WeaponState.Reloading, state.State);
            if (afterCommit)
            {
                float deadline = Time.realtimeSinceStartup + 5;
                while (!state.ReloadCommitted)
                {
                    Assert.Less(Time.realtimeSinceStartup, deadline, "Reload never reached commit.");
                    yield return null;
                }
            }
            Assert.AreEqual(WeaponState.Reloading, state.State);
            Assert.AreEqual(afterCommit, state.ReloadCommitted);
            int magazine = state.CurrentMagazine;
            int reserve = state.ReserveAmmo;
            Assert.AreEqual(afterCommit ? 1 : 0, commits);
            Assert.IsTrue(saves.Save(path).Success, saves.LastResult.Message);
            byte[] savedBytes = File.ReadAllBytes(path);
            Assert.AreEqual(WeaponState.Reloading, state.State, "Saving must not commit or cancel the live action.");
            Assert.AreEqual(magazine, state.CurrentMagazine);
            Assert.AreEqual(reserve, state.ReserveAmmo);

            var health = flow.Player.GetComponent<PlayerHealth>();
            health.TakeDamage(new DamageInfo { Amount = health.MaxHealth + 1 });
            Assert.IsTrue(flow.PlayerDead);
            Assert.IsNull(combat.Firearm, "Session death must detach the weapon synchronously.");
            Assert.AreEqual(WeaponState.Holstered, state.State);
            Assert.AreEqual(magazine, state.CurrentMagazine);
            Assert.AreEqual(reserve, state.ReserveAmmo);
            Assert.IsFalse(saves.Save(path).Success, "A dead session must not replace the living checkpoint.");
            CollectionAssert.AreEqual(savedBytes, File.ReadAllBytes(path));
            yield return null; // Complete the old viewmodel's deferred destruction.
            Assert.IsTrue(weapon == null);

            flow.ReturnToMenu();
            yield return null;
            yield return saves.Load(path);
            Assert.IsTrue(saves.LastResult.Success, saves.LastResult.Message);
            Assert.IsFalse(flow.PlayerDead);
            var restored = flow.Player.GetComponent<PlayerCombatController>().Firearm;
            Assert.IsNotNull(restored);
            yield return WaitForRifleReady(restored);
            Assert.AreEqual(magazine, restored.RuntimeState.CurrentMagazine);
            Assert.AreEqual(reserve, restored.RuntimeState.ReserveAmmo);
            Assert.AreEqual(total, restored.RuntimeState.TotalAmmo);
            Assert.IsFalse(restored.RuntimeState.ReloadCommitted, "A load must not replay the old reload transaction.");
            Assert.AreEqual(afterCommit ? 1 : 0, commits, "Destroyed weapon must not publish a stale commit.");
        }

        static IEnumerator WaitForRifleReady(WeaponController weapon)
        {
            var animator = weapon.GetComponentInChildren<Animator>(true);
            Assert.IsNotNull(animator);
            float deadline = Time.realtimeSinceStartup + 5;
            while (weapon.RuntimeState.State != WeaponState.Ready ||
                !animator.GetCurrentAnimatorStateInfo(0).IsName("Ready") || animator.IsInTransition(0))
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "Restored production rifle did not settle into Ready.");
                yield return null;
            }
        }
    }
}
#endif
