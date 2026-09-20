#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Text;
using LastSignal.Inventory;
using LastSignal.Loot;
using LastSignal.Shelter;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace LastSignal.Tests
{
    public class ShelterIntegrationTests
    {
        const string Evidence="Docs/Implementation/S008/Evidence/20260920-212200-entry";
        SessionFlow session;
        ShelterLoop loop;
        PlayerInventory inv;
        WeaponController weapon;
        Camera camera;
        IEnumerator Load()
        {
            Time.timeScale=1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/ShelterAcceptance.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            session=Object.FindAnyObjectByType<SessionFlow>();loop=session.GetComponent<ShelterLoop>();
            session.Resume();yield return new WaitForSeconds(.8f);
            inv=loop.Inventory;weapon=session.Player.GetComponent<PlayerCombatController>().ActiveWeapon;camera=session.Player.GetComponent<FirstPersonLook>().View;
            Assert.AreEqual(ExpeditionState.Shelter,loop.State);Assert.AreEqual(0,loop.ExpeditionIndex);
            Assert.AreEqual(0,loop.Storage.GetTotalQuantity(weapon.Definition.Ammunition));Assert.AreEqual(30,weapon.RuntimeState.CurrentMagazine);
        }
        void Place(Vector3 position)
        {var c=session.Player.GetComponent<CharacterController>();c.enabled=false;session.Player.transform.position=position;c.enabled=true;Physics.SyncTransforms();}
        bool Use(ShelterPoint point)
        {
            Vector3 side=point.Action==ShelterAction.Return?Vector3.right:point.Action==ShelterAction.Leave?Vector3.left:Vector3.right;
            Place(new Vector3(point.transform.position.x, .05f,point.transform.position.z)+side*1.15f);
            camera.transform.LookAt(point.transform.position);Physics.SyncTransforms();
            return session.Player.GetComponent<InteractionController>().TryInteract();
        }
        WorldItem FindLoot(bool ammo)
        {
            foreach(var item in Object.FindObjectsByType<WorldItem>())
                if(item.Available&&item.transform.root.name=="Session Loot"&&(item.Definition==weapon.Definition.Ammunition)==ammo)return item;
            Assert.Fail("Expected remaining actual S006 loot");return null;
        }
        void Pick(WorldItem item)
        {
            Place(new Vector3(item.transform.position.x+1.3f,.05f,item.transform.position.z));camera.transform.LookAt(item.transform.position);Physics.SyncTransforms();
            Assert.IsTrue(session.Player.GetComponent<InteractionController>().TryInteract(),"Production ray must pick actual world item");
        }
        int Slot(bool carried, LastSignal.Inventory.Data.ItemDefinition item)
        {
            int n=carried?inv.Capacity:loop.Storage.Capacity;
            for(int i=0;i<n;i++){var slot=carried?inv.GetSlot(i):loop.Storage.GetSlot(i);if(!slot.IsEmpty&&slot.Item==item)return i;}
            return -1;
        }
        [UnityTest] public IEnumerator ProductionTwoExpeditionsPreserveWorldStorageAndAmmo()
        {
            yield return Load();var ammoDefinition=weapon.Definition.Ammunition;var player=session.Player;var stash=loop.Storage;var loot=session.GetComponent<LootPopulationService>();int seed=loot.Seed,count=loot.GeneratedCount;var runtimeRoot=GameObject.Find("Session Loot");
            Assert.IsTrue(Use(loop.StoragePoint));Assert.IsTrue(loop.PreparationUI.IsOpen);loop.ClosePreparation();
            Assert.IsTrue(Use(loop.ExitPoint));Assert.IsFalse(loop.TryUse(loop.ExitPoint));Assert.AreEqual(1,loop.ExpeditionIndex);
            var ammo=FindLoot(true);int firstAmmo=ammo.Quantity;var ammoId=ammo.GetEntityId();var consumedPosition=ammo.transform.position;Pick(ammo);
            var resource=FindLoot(false);var definition=resource.Definition;int firstResource=resource.Quantity;Pick(resource);yield return null;
            Assert.IsFalse(ammo);Assert.IsFalse(resource);
            // Real rifle shot against a live production Shambler. Positioning is fixture setup,
            // ray resolution, damage, magazine cost, AI and presentation remain production code.
            var zombie=Object.FindAnyObjectByType<ZombieController>();Assert.IsNotNull(zombie);
            Place(zombie.transform.position+zombie.transform.forward*4);
            ZombieHitRegion region=null;foreach(var r in zombie.GetComponentsInChildren<ZombieHitRegion>())if(r.name=="Damage_Chest")region=r;
            Assert.IsNotNull(region);var zh=zombie.GetComponent<ZombieHealth>();float hp=zh.CurrentHealth;
            camera.transform.LookAt(region.HitCollider.bounds.center);Physics.SyncTransforms();weapon.OnFirePressed();weapon.OnFireReleased();
            Assert.AreEqual(29,weapon.RuntimeState.CurrentMagazine);Assert.That(zh.CurrentHealth,Is.LessThan(hp));
            Assert.IsTrue(Use(loop.ReturnPoint));Assert.IsFalse(loop.TryUse(loop.ReturnPoint));Assert.AreSame(stash,loop.Storage);Assert.AreSame(player,session.Player);
            Assert.IsTrue(Use(loop.StoragePoint));var ui=loop.PreparationUI;
            ui.Select(true,Slot(true,weapon.Definition.Ammunition));ui.DepositStackButton.onClick.Invoke();
            ui.Select(true,Slot(true,definition));ui.DepositStackButton.onClick.Invoke();
            Assert.AreEqual(firstAmmo,stash.GetTotalQuantity(weapon.Definition.Ammunition));Assert.AreEqual(firstResource,stash.GetTotalQuantity(definition));Assert.AreEqual(29,weapon.RuntimeState.CurrentMagazine);
            ui.Select(false,Slot(false,weapon.Definition.Ammunition));ui.WithdrawOneButton.onClick.Invoke();Assert.AreEqual(1,weapon.RuntimeState.ReserveAmmo);
            Assert.That(ui.Readiness.text,Does.Contain("Carried reserve 1"));loop.ClosePreparation();
            Assert.IsTrue(Use(loop.ExitPoint));Assert.AreEqual(2,loop.ExpeditionIndex);Assert.AreSame(player,session.Player);Assert.AreSame(runtimeRoot,GameObject.Find("Session Loot"));Assert.AreEqual(seed,loot.Seed);Assert.AreEqual(count,loot.GeneratedCount);
            foreach(var item in Object.FindObjectsByType<WorldItem>()){Assert.AreNotEqual(ammoId,item.GetEntityId());Assert.That(Vector3.Distance(consumedPosition,item.transform.position),Is.GreaterThan(.01f),"Consumed loot point must stay empty, not respawn with a new identity");}
            weapon.OnReloadRequested();yield return new WaitForSeconds(2.7f);Assert.AreEqual(30,weapon.RuntimeState.CurrentMagazine);Assert.AreEqual(0,weapon.RuntimeState.ReserveAmmo);
            var second=FindLoot(true);int secondAmmo=second.Quantity;Pick(second);yield return null;
            camera.transform.rotation=Quaternion.LookRotation(Vector3.up);weapon.OnFirePressed();weapon.OnFireReleased();Assert.AreEqual(29,weapon.RuntimeState.CurrentMagazine);
            Assert.IsTrue(Use(loop.ReturnPoint));Assert.IsTrue(Use(loop.StoragePoint));ui.Select(true,Slot(true,weapon.Definition.Ammunition));ui.DepositStackButton.onClick.Invoke();
            Assert.AreEqual(firstAmmo-1+secondAmmo,stash.GetTotalQuantity(weapon.Definition.Ammunition));
            Assert.AreEqual(30+firstAmmo+secondAmmo,stash.GetTotalQuantity(weapon.Definition.Ammunition)+weapon.RuntimeState.TotalAmmo+2);
            File.WriteAllText(Evidence+"/two-expedition-acceptance.txt","PASS actual authored Shelter scene, production InteractionController rays, initial empty stash and 30/0 weapon, both expeditions without menu. First pickup ammo="+firstAmmo+" other="+firstResource+" "+definition.Id+"; live Shambler damaged by actual rifle. Deposit/withdraw using actual UI buttons; second expedition reload uses 1 withdrawn round, second pickup="+secondAmmo+"; shots=2 final magazine=29 reserve=0 stash="+stash.GetTotalQuantity(weapon.Definition.Ammunition)+". Player, stash and Session Loot identities preserved, seed="+seed+", generated="+count+". Test positions Player to interaction points; manual traversal/standalone acceptance separate.\n");
            loop.ClosePreparation();session.ReturnToMenu();yield return null;Assert.IsNull(loop.Storage);session.BeginSession();session.Resume();yield return null;Assert.AreEqual(0,loop.Storage.GetTotalQuantity(ammoDefinition));
        }
        [UnityTest] public IEnumerator TenOpenCloseCyclesOneClickOneTransferAndInputOwnership()
        {
            yield return Load();inv.TryAdd(weapon.Definition.Ammunition,20);
            for(int i=0;i<10;i++)
            {
                Assert.IsTrue(Use(loop.StoragePoint));Assert.IsTrue(session.Paused);Assert.IsFalse(session.Player.GetComponent<PlayerInputReader>().GameplayActive);Assert.AreEqual(CursorLockMode.None,Cursor.lockState);
                var ui=loop.PreparationUI;ui.Select(true,Slot(true,weapon.Definition.Ammunition));ui.DepositOneButton.onClick.Invoke();Assert.AreEqual(i+1,loop.Storage.GetTotalQuantity(weapon.Definition.Ammunition));
                int mag=weapon.RuntimeState.CurrentMagazine;weapon.OnFirePressed();Assert.AreEqual(mag,weapon.RuntimeState.CurrentMagazine);
                Assert.IsFalse(loop.TryUse(loop.ExitPoint));session.TogglePause();Assert.IsFalse(ui.IsOpen);Assert.IsFalse(session.Paused);Assert.IsTrue(session.Player.GetComponent<PlayerInputReader>().GameplayActive);
            }
            Assert.AreEqual(10,inv.GetTotalQuantity(weapon.Definition.Ammunition));
        }
        [UnityTest] public IEnumerator DeathAndMenuFromPreparationReleasePresentationAndSessionBIsFresh()
        {
            yield return Load();inv.TryAdd(weapon.Definition.Ammunition,8);Assert.IsTrue(Use(loop.StoragePoint));loop.Transfer(true,weapon.Definition.Ammunition,8);var stash=loop.Storage;
            session.Player.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo{Amount=1000});Assert.AreEqual(ExpeditionState.Dead,loop.State);Assert.IsFalse(loop.PreparationUI.IsOpen);Assert.AreEqual(8,stash.GetTotalQuantity(weapon.Definition.Ammunition));Assert.AreEqual(0,loop.Transfer(false,weapon.Definition.Ammunition,8).Moved);
            session.ReturnToMenu();session.ReturnToMenu();yield return null;Assert.IsNull(loop.Storage);Assert.IsFalse(loop.PreparationUI.IsOpen);
            session.BeginSession();session.Resume();yield return new WaitForSeconds(.8f);Assert.AreNotSame(stash,loop.Storage);Assert.AreEqual(0,loop.Storage.GetTotalQuantity(weapon.Definition.Ammunition));Assert.AreEqual(0,loop.ExpeditionIndex);
        }
        [UnityTest] public IEnumerator PartialWorldAndDroppedItemsRemainAcrossReturn()
        {
            yield return Load();Assert.IsTrue(Use(loop.ExitPoint));var item=FindLoot(true);int original=item.Quantity;inv.Initialize(1);inv.TryAdd(item.Definition,59);Pick(item);Assert.AreEqual(original-1,item.Quantity);
            Assert.IsTrue(Use(loop.ReturnPoint));Assert.IsTrue(Use(loop.StoragePoint));loop.Transfer(true,item.Definition,60);loop.ClosePreparation();Assert.IsTrue(Use(loop.ExitPoint));Assert.AreEqual(original-1,item.Quantity);
            inv.TryAdd(item.Definition,5);camera.transform.rotation=Quaternion.LookRotation(Vector3.up);Assert.IsTrue(inv.TryDrop(0,5));WorldItem dropped=null;
            foreach(var w in Object.FindObjectsByType<WorldItem>())if(w.transform.root.name!="Session Loot")dropped=w;
            Assert.IsNotNull(dropped);Assert.IsTrue(Use(loop.ReturnPoint));Assert.IsTrue(Use(loop.ExitPoint));Assert.IsTrue(dropped);Assert.AreEqual(5,dropped.Quantity);Assert.AreEqual(original-1,item.Quantity);
        }
        [UnityTest] public IEnumerator ReturnDuringReloadBeforeAndAfterCommitPreservesExistingTiming()
        {
            yield return Load();inv.TryAdd(weapon.Definition.Ammunition,10);
            Assert.IsTrue(Use(loop.ExitPoint));camera.transform.rotation=Quaternion.LookRotation(Vector3.up);weapon.OnFirePressed();weapon.OnFireReleased();yield return new WaitForSeconds(.2f);
            weapon.OnReloadRequested();Assert.IsTrue(Use(loop.ReturnPoint));Assert.AreEqual(29,weapon.RuntimeState.CurrentMagazine);Assert.AreEqual(10,weapon.RuntimeState.ReserveAmmo);yield return new WaitForSeconds(3);Assert.AreEqual(30,weapon.RuntimeState.CurrentMagazine);Assert.AreEqual(9,weapon.RuntimeState.ReserveAmmo);
            Assert.IsTrue(Use(loop.ExitPoint));camera.transform.rotation=Quaternion.LookRotation(Vector3.up);weapon.OnFirePressed();weapon.OnFireReleased();yield return new WaitForSeconds(.2f);weapon.OnReloadRequested();yield return new WaitForSeconds(1.9f);Assert.AreEqual(30,weapon.RuntimeState.CurrentMagazine);Assert.IsTrue(Use(loop.ReturnPoint));yield return new WaitForSeconds(1);Assert.AreEqual(8,weapon.RuntimeState.ReserveAmmo);Assert.AreEqual(30,weapon.RuntimeState.CurrentMagazine);
        }
        [UnityTest] public IEnumerator TenExpeditionSoakConservesIdentityAndMeasuresTransitions()
        {
            yield return Load();var player=session.Player;var stash=loop.Storage;var loot=session.GetComponent<LootPopulationService>();var root=GameObject.Find("Session Loot");int initialWorld=Object.FindObjectsByType<WorldItem>().Length;inv.TryAdd(weapon.Definition.Ammunition,60);var report=new StringBuilder();
            long elapsed=0,allocations=0;
            for(int i=1;i<=10;i++)
            {
                Assert.IsTrue(Use(loop.StoragePoint));Assert.AreEqual(30,loop.Transfer(true,weapon.Definition.Ammunition,30).Moved);Assert.AreEqual(30,loop.Transfer(false,weapon.Definition.Ammunition,30).Moved);loop.ClosePreparation();
                long bytes=GC.GetAllocatedBytesForCurrentThread(),start=System.Diagnostics.Stopwatch.GetTimestamp();Assert.IsTrue(Use(loop.ExitPoint));Assert.IsTrue(Use(loop.ReturnPoint));elapsed+=System.Diagnostics.Stopwatch.GetTimestamp()-start;allocations+=GC.GetAllocatedBytesForCurrentThread()-bytes;
                Assert.AreEqual(i,loop.ExpeditionIndex);Assert.AreSame(stash,loop.Storage);Assert.AreSame(player,session.Player);Assert.AreSame(root,GameObject.Find("Session Loot"));Assert.AreEqual(initialWorld,Object.FindObjectsByType<WorldItem>().Length);Assert.AreEqual(90,inv.GetTotalQuantity(weapon.Definition.Ammunition)+stash.GetTotalQuantity(weapon.Definition.Ammunition)+weapon.RuntimeState.CurrentMagazine);
                report.AppendLine("Expedition "+i+": same player/stash/world, 90 total rounds, no new loot; pass");yield return null;
            }
            report.AppendLine("10 leave+return pairs incl fixture placement, actual ray interactions, assertions: "+elapsed*1000.0/System.Diagnostics.Stopwatch.Frequency+" ms total, "+allocations+" B; this includes test overhead, not isolated transition cost.");File.WriteAllText(Evidence+"/10-expedition-soak.txt",report.ToString());
            Assert.IsTrue(Use(loop.StoragePoint));yield return null;
            // Closed-over idle presentation has no Update/LateUpdate and no changes are published.
            // Record actual refresh count over frames; engine/UI allocations measured separately in profile.
            int refreshes=loop.PreparationUI.RefreshCount;long uiBytes=loop.PreparationUI.PresentationAllocatedBytes;
            for(int i=0;i<60;i++)yield return null;
            Assert.AreEqual(refreshes,loop.PreparationUI.RefreshCount);Assert.AreEqual(uiBytes,loop.PreparationUI.PresentationAllocatedBytes);
            File.WriteAllText(Evidence+"/ui-idle.txt","Open preparation observed for 60 rendered frames: 0 refresh invocations; instrumented cumulative GC.GetAllocatedBytesForCurrentThread around project-owned Refresh changed by 0 B. No project UI Update/LateUpdate; this excludes Unity Canvas/layout/render, test runner and whole-process allocations.\n");
        }
        [UnityTest] public IEnumerator DisabledPreparationClosesAndBlockedDoorwayRejectsWithoutMutation()
        {
            yield return Load();Assert.IsTrue(Use(loop.StoragePoint));loop.PreparationUI.enabled=false;Assert.IsFalse(session.Paused);Assert.IsFalse(loop.PreparationUI.IsOpen);Assert.AreEqual(ExpeditionState.Shelter,loop.State);loop.PreparationUI.enabled=true;
            Assert.IsTrue(Use(loop.StoragePoint));loop.ClosePreparation();
            var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.position=loop.OutsideAnchor.position+Vector3.up;blocker.transform.localScale=Vector3.one*2;Physics.SyncTransforms();
            Assert.IsFalse(Use(loop.ExitPoint));Assert.AreEqual(0,loop.ExpeditionIndex);Assert.AreEqual(ExpeditionState.Shelter,loop.State);Object.Destroy(blocker);yield return null;
            Assert.IsTrue(Use(loop.ExitPoint));session.Pause();Assert.IsFalse(loop.TryUse(loop.ReturnPoint));session.Resume();
            session.Player.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo{Amount=1000});Assert.IsFalse(loop.TryUse(loop.ReturnPoint));Assert.AreEqual(ExpeditionState.Dead,loop.State);
        }
        [UnityTest] public IEnumerator WarmedDomainPresentationAndTransitionMeasurements()
        {
            yield return Load();var ammo=weapon.Definition.Ammunition;
            var carrierObject=new GameObject("Profile-only carrier");var carrier=carrierObject.AddComponent<PlayerInventory>();carrier.Initialize(24);var stash=new ShelterStorage(48);carrier.TryAdd(ammo,60);
            for(int i=0;i<100;i++){ItemTransferService.Deposit(carrier,stash,ammo,1);ItemTransferService.Withdraw(stash,carrier,ammo,1);}
            long before=GC.GetAllocatedBytesForCurrentThread(),start=System.Diagnostics.Stopwatch.GetTimestamp();
            for(int i=0;i<1000;i++){ItemTransferService.Deposit(carrier,stash,ammo,1);ItemTransferService.Withdraw(stash,carrier,ammo,1);}
            long domainBytes=GC.GetAllocatedBytesForCurrentThread()-before;double domainMs=(System.Diagnostics.Stopwatch.GetTimestamp()-start)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            Object.Destroy(carrierObject);yield return null;
            inv.TryAdd(ammo,60);Assert.IsTrue(Use(loop.StoragePoint));loop.Transfer(true,ammo,1);loop.Transfer(false,ammo,1);
            long presentationBefore=loop.PreparationUI.PresentationAllocatedBytes;before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<100;i++){loop.Transfer(true,ammo,1);loop.Transfer(false,ammo,1);}
            long uiTransferBytes=GC.GetAllocatedBytesForCurrentThread()-before,refreshBytes=loop.PreparationUI.PresentationAllocatedBytes-presentationBefore;
            loop.ClosePreparation();Assert.IsTrue(Use(loop.ExitPoint));Assert.IsTrue(Use(loop.ReturnPoint));
            before=GC.GetAllocatedBytesForCurrentThread();start=System.Diagnostics.Stopwatch.GetTimestamp();
            for(int i=0;i<100;i++){loop.TryUse(loop.ExitPoint);loop.TryUse(loop.ReturnPoint);}
            long transitionBytes=GC.GetAllocatedBytesForCurrentThread()-before;double transitionMs=(System.Diagnostics.Stopwatch.GetTimestamp()-start)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            Assert.AreEqual(101,loop.ExpeditionIndex);Assert.AreEqual(0,domainBytes);
            File.WriteAllText(Evidence+"/performance.txt","Unity "+Application.unityVersion+" Editor, synchronous warmed current-thread measurements; no assertion inside measured loops.\n2000 domain transfers no UI observers: "+domainBytes+" B, "+domainMs+" ms.\n200 gameplay transfers with live UI/HUD observers: "+uiTransferBytes+" B total; instrumented UI Refresh portion "+refreshBytes+" B.\n200 actual doorway transitions via ShelterLoop.TryUse: "+transitionBytes+" B, "+transitionMs+" ms total ("+(transitionMs/200)+" ms/transition). No fixture placement inside measured transition loop.\nIdle UI project Refresh allocations measured over 60 frames separately in ui-idle.txt; excludes engine Canvas rendering.\n");
        }
        [UnityTearDown] public IEnumerator Cleanup(){if(session)session.ReturnToMenu();Time.timeScale=1;yield return null;}
    }
}
#endif
