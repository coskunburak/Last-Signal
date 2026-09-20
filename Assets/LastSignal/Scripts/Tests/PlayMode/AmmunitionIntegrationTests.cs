#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using LastSignal.Inventory;
using LastSignal.Loot;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public class AmmunitionIntegrationTests
    {
        const string Evidence = "Docs/Implementation/S007/Evidence/20260920-103550-entry";
        SessionFlow session;
        WeaponController weapon;
        PlayerInventory inventory;
        Camera camera;
        Text ammoText;
        readonly System.Collections.Generic.List<GameObject> owned = new System.Collections.Generic.List<GameObject>();
        IEnumerator Load(int magazine = 30)
        {
            Time.timeScale = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/ScavengingAcceptance.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            session = Object.FindAnyObjectByType<SessionFlow>();
            session.ReturnToMenu(); yield return null;
            var loot = session.GetComponent<LootPopulationService>();
            var so = new SerializedObject(loot); so.FindProperty("overrideSeed").boolValue = true; so.FindProperty("explicitSeed").intValue = 12345; so.ApplyModifiedPropertiesWithoutUndo();
            session.BeginSession(); session.Resume(); yield return null;
            Bind();
            foreach (var zombie in Object.FindObjectsByType<ZombieController>()) zombie.SetPaused(true); // Isolate weapon assertions; existing full regression covers active combat.
            weapon.Initialize(camera.transform, session.Player, magazine); weapon.RequestEquip();
            yield return new WaitForSeconds(.65f);
            var hud = Object.FindAnyObjectByType<AcceptanceHud>();
            ammoText = (Text)typeof(AcceptanceHud).GetField("ammoDisplay", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(hud);
            Assert.AreEqual(WeaponState.Ready, weapon.RuntimeState.State);
        }
        void Bind()
        {
            weapon = session.Player.GetComponent<PlayerCombatController>().ActiveWeapon;
            inventory = session.Player.GetComponent<PlayerInventory>();
            camera = session.Player.GetComponent<FirstPersonLook>().View;
        }
        WorldItem AmmoPickup()
        {
            foreach (var item in Object.FindObjectsByType<WorldItem>())
                if (item.Available && item.Definition == weapon.Definition.Ammunition && item.transform.root.name == "Session Loot") return item;
            Assert.Fail("Deterministic actual Security profile must produce rifle ammo."); return null;
        }
        void FireUp()
        { camera.transform.rotation = Quaternion.LookRotation(Vector3.up); weapon.OnFirePressed(); weapon.OnFireReleased(); }
        void AssertHud()
        { Assert.IsNotNull(ammoText); Assert.That(ammoText.text, Does.EndWith(weapon.RuntimeState.CurrentMagazine + " / " + weapon.RuntimeState.ReserveAmmo)); }
        [UnityTest] public IEnumerator ActualShotsMissWallEmptyAndRejectedRequests()
        {
            yield return Load(2); int shots = 0, dry = 0; WeaponFireResolver.ShotResult last = default;
            weapon.ShotFired += r => { shots++; last = r; }; weapon.ShotRejected += () => dry++;
            FireUp(); Assert.AreEqual(1, shots); Assert.AreEqual(1, weapon.RuntimeState.CurrentMagazine); Assert.IsNull(last.Collider); AssertHud();
            FireUp(); Assert.AreEqual(1, shots); // cadence rejection
            yield return new WaitForSeconds(.15f);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); owned.Add(wall); wall.transform.position = camera.transform.position + Vector3.up * 2; wall.transform.localScale = Vector3.one * .5f; Physics.SyncTransforms();
            FireUp(); Assert.AreEqual(2, shots); Assert.AreEqual(wall.GetComponent<Collider>(), last.Collider); Assert.AreEqual(0, weapon.RuntimeState.CurrentMagazine);
            camera.transform.rotation = Quaternion.LookRotation(Vector3.up); weapon.OnFirePressed();
            yield return new WaitForSeconds(.5f); Assert.AreEqual(1, dry); Assert.AreEqual(2, shots); Assert.AreEqual(0, weapon.RuntimeState.CurrentMagazine); weapon.OnFireReleased();
            inventory.TryAdd(weapon.Definition.Ammunition, 7); weapon.OnReloadRequested(); weapon.OnFirePressed(); Assert.AreEqual(2, shots); Assert.AreEqual(0, weapon.RuntimeState.CurrentMagazine);
        }
        [UnityTest] public IEnumerator MissingMuzzleAndDisabledWeaponConsumeNothing()
        {
            yield return Load(2); var so = new SerializedObject(weapon); var muzzle = weapon.Muzzle; so.FindProperty("muzzle").objectReferenceValue = null; so.ApplyModifiedPropertiesWithoutUndo();
            FireUp(); Assert.AreEqual(2, weapon.RuntimeState.CurrentMagazine);
            so.FindProperty("muzzle").objectReferenceValue = muzzle; so.ApplyModifiedPropertiesWithoutUndo();
            inventory.TryAdd(weapon.Definition.Ammunition, 7); weapon.OnReloadRequested(); var state = weapon.RuntimeState;
            weapon.gameObject.SetActive(false); state.Tick(10); Assert.AreEqual(WeaponState.Holstered, state.State); Assert.IsFalse(state.TryCommitReload());
            weapon.OnFirePressed(); Assert.AreEqual(2, state.CurrentMagazine); Assert.AreEqual(7, state.ReserveAmmo);
        }
        [UnityTest] public IEnumerator SecurityPickupPartialReloadAndDropRePickupConserve()
        {
            yield return Load(0); var state = weapon.RuntimeState; var item = AmmoPickup(); int picked = item.Quantity;
            Assert.That(picked, Is.InRange(5, 20)); Assert.IsTrue(((IInteractable)item).TryInteract()); Assert.AreEqual(picked, state.ReserveAmmo); AssertHud();
            weapon.OnReloadRequested(); for (int i=0;i<20;i++) weapon.OnReloadRequested();
            yield return new WaitForSeconds(1.5f); Assert.AreEqual(0, state.CurrentMagazine); Assert.AreEqual(picked, state.ReserveAmmo);
            yield return new WaitForSeconds(.5f); Assert.IsTrue(state.ReloadCommitted); Assert.AreEqual(picked, state.CurrentMagazine); Assert.AreEqual(0, state.ReserveAmmo); AssertHud();
            yield return new WaitForSeconds(1.5f); FireUp(); Assert.AreEqual(picked-1, state.CurrentMagazine);
            var second = AmmoPickup(); int added = second.Quantity; Assert.IsTrue(second.TryInteract()); yield return null;
            int slot = -1; for(int i=0;i<inventory.Capacity;i++) if(!inventory.GetSlot(i).IsEmpty && inventory.GetSlot(i).Item == weapon.Definition.Ammunition){slot=i;break;}
            camera.transform.rotation = Quaternion.LookRotation(Vector3.up); Assert.IsTrue(inventory.TryDrop(slot, added)); Assert.AreEqual(0, state.ReserveAmmo); AssertHud();
            WorldItem dropped = null; foreach(var w in Object.FindObjectsByType<WorldItem>()) if(w.Definition == weapon.Definition.Ammunition && w.transform.root.name != "Session Loot") dropped=w;
            Assert.IsNotNull(dropped); Assert.IsTrue(dropped.TryInteract()); Assert.AreEqual(added, state.ReserveAmmo); AssertHud();
            weapon.OnReloadRequested(); yield return new WaitForSeconds(2.7f);
            Assert.AreEqual(picked+added-1,state.TotalAmmo); AssertHud();
            File.WriteAllText(Evidence+"/pickup-to-reload.txt", "Actual Security seed 12345 -> WorldItem -> Inventory -> empty partial reload -> miss -> second pickup -> drop/re-pick -> tactical reload. picked="+picked+"+"+added+" shots=1 finalTotal="+state.TotalAmmo+"; conserved.\n");
        }
        IEnumerator Death(bool afterCommit)
        {
            yield return Load(8); inventory.TryAdd(weapon.Definition.Ammunition, 6); var state=weapon.RuntimeState; weapon.OnReloadRequested();
            if(afterCommit) yield return new WaitForSeconds(1.85f);
            session.Player.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo{Amount=1000});
            yield return new WaitForSeconds(3);
            Assert.AreEqual(WeaponState.Holstered,state.State); Assert.AreEqual(afterCommit?14:8,state.CurrentMagazine); Assert.AreEqual(afterCommit?0:6,inventory.GetTotalQuantity(state.Definition.Ammunition)); Assert.IsFalse(state.TryCommitReload());
        }
        [UnityTest] public IEnumerator DeathBeforeCommitCancels() { yield return Death(false); }
        [UnityTest] public IEnumerator DeathAfterCommitKeepsTransfer() { yield return Death(true); }
        [UnityTest] public IEnumerator PauseAtBothSidesOfCommitAndInventoryMutation()
        {
            yield return Load(8); inventory.TryAdd(weapon.Definition.Ammunition, 20); var state=weapon.RuntimeState; weapon.OnReloadRequested();
            yield return new WaitForSeconds(1.4f); session.Pause(); float timer=state.StateTimer;
            yield return new WaitForSecondsRealtime(.3f); Assert.AreEqual(timer,state.StateTimer); Assert.AreEqual(8,state.CurrentMagazine);
            weapon.OnFirePressed(); Assert.AreEqual(8,state.CurrentMagazine);
            Assert.IsTrue(inventory.TryRemove(weapon.Definition.Ammunition,14)); session.Resume();
            yield return new WaitForSeconds(.5f); Assert.AreEqual(14,state.CurrentMagazine); Assert.AreEqual(0,state.ReserveAmmo);
            session.Pause(); timer=state.StateTimer; yield return new WaitForSecondsRealtime(.3f); Assert.AreEqual(timer,state.StateTimer);
            session.Resume(); yield return new WaitForSeconds(1); Assert.AreEqual(WeaponState.Ready,state.State); Assert.AreEqual(14,state.CurrentMagazine); AssertHud();
        }
        [UnityTest] public IEnumerator TenSessionsClearReloadBindingsAndHud()
        {
            yield return Load(8);
            for(int cycle=0;cycle<10;cycle++)
            {
                if(cycle>0){session.BeginSession();session.Resume();yield return new WaitForSeconds(.7f);Bind();}
                Assert.AreEqual(0,weapon.RuntimeState.ReserveAmmo); AssertHud();
                var pickup=AmmoPickup(); int amount=pickup.Quantity; Assert.IsTrue(pickup.TryInteract()); Assert.AreEqual(amount,weapon.RuntimeState.ReserveAmmo); AssertHud();
                FireUp(); yield return new WaitForSeconds(.15f); weapon.OnReloadRequested();
                if(cycle%2==0)yield return new WaitForSeconds(1.85f);
                var old=weapon.RuntimeState; int mag=old.CurrentMagazine,res=old.ReserveAmmo; var oldInventory=inventory;
                session.ReturnToMenu(); Assert.AreEqual(WeaponState.Holstered,old.State); old.Tick(10); Assert.IsFalse(old.TryCommitReload());
                Assert.AreEqual(mag,old.CurrentMagazine); Assert.AreEqual(res,oldInventory.GetTotalQuantity(old.Definition.Ammunition));
                yield return null; Assert.AreEqual("",ammoText.text); Assert.IsFalse(ammoText.enabled);
            }
            File.WriteAllText(Evidence+"/session-soak.txt","10 normal-session cycles with actual Security ammo, fire, reload, alternating pre/post-commit teardown, fresh reserve=0, HUD synchronization; all assertions passed.\n");
        }
        [UnityTest] public IEnumerator WarmedAmmoHudAndFireAllocationMeasurements()
        {
            yield return Load(30); var hud=Object.FindAnyObjectByType<AcceptanceHud>();
            var update=(Action)Delegate.CreateDelegate(typeof(Action),hud,typeof(AcceptanceHud).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance));
            for(int i=0;i<100;i++)update(); long start=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1000;i++)update();long idle=GC.GetAllocatedBytesForCurrentThread()-start;
            Assert.AreEqual(0,idle,"Existing HUD idle including event-driven ammo must be allocation free");
            for(int i=0;i<5;i++){weapon.RuntimeState.Tick(.2f);FireUp();}
            camera.transform.rotation=Quaternion.LookRotation(Vector3.up); start=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<20;i++){weapon.RuntimeState.Tick(.2f);weapon.OnFirePressed();weapon.OnFireReleased();}
            long shots=GC.GetAllocatedBytesForCurrentThread()-start;
            inventory.TryAdd(weapon.Definition.Ammunition,60); weapon.OnReloadRequested(); weapon.RuntimeState.Tick(2);
            start=GC.GetAllocatedBytesForCurrentThread();Assert.IsTrue(weapon.RuntimeState.TryCommitReload());long reload=GC.GetAllocatedBytesForCurrentThread()-start;
            File.WriteAllText(Evidence+"/allocation-profile.txt","Editor synchronous warmed samples; excludes test/engine frame overhead. HUD Update 1000 calls="+idle+" B. Actual miss fire incl presentation+changing HUD, 20 shots="+shots+" B ("+(shots/20.0)+" B/shot). Reload+HUD one transaction (includes NUnit assertion overhead)="+reload+" B. These are not whole-process GC measurements.\n");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { if(session)session.ReturnToMenu();foreach(var go in owned)if(go)Object.Destroy(go);owned.Clear();Time.timeScale=1;yield return null; }
    }
}
#endif
