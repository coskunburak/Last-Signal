#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using LastSignal.Inventory;
using LastSignal.Loot;
using UnityEngine;

namespace LastSignal.Shelter
{
    /// <summary>Opt-in Development build acceptance driver. Absent from release builds;
    /// never activates in normal gameplay. Uses production scene/interaction/UI/combat APIs.
    /// Fixture positioning is explicit and is not a replacement for human traversal QA.</summary>
    public sealed class ShelterStandaloneAcceptance : MonoBehaviour
    {
        SessionFlow session;
        ShelterLoop loop;
        PlayerInventory inventory;
        WeaponController weapon;
        Camera view;
        string evidence;
        int errors;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-s008Acceptance")
            {var go=new GameObject("S008 opt-in Development acceptance");var driver=go.AddComponent<ShelterStandaloneAcceptance>();driver.evidence=args[i+1];return;}
        }
        IEnumerator Start()
        {
            Application.logMessageReceived+=Observe;
            Directory.CreateDirectory(evidence);
            var run=Run();
            while(true)
            {
                bool next;
                try {next=run.MoveNext();}
                catch(Exception exception){File.WriteAllText(Path.Combine(evidence,"standalone-result.txt"),"FAIL\n"+exception);Debug.LogException(exception);Application.Quit(1);yield break;}
                if(!next)break;
                yield return run.Current;
            }
            Application.logMessageReceived-=Observe;
            File.WriteAllText(Path.Combine(evidence,"standalone-result.txt"),"PASS Development standalone automated acceptance. Both expeditions, actual production interaction rays/UI buttons/rifle hit/reload, conserved resources, unchanged world identities/consumed point, menu and fresh Session B. Project errors observed="+errors+". Fixture positioning used; human continuous traversal is separate.\n");
            Debug.Log("S008_STANDALONE_PASS");Application.Quit(0);
        }
        void Observe(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
        static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException("S008 acceptance: "+message);}
        void Bind(){inventory=loop.Inventory;weapon=session.Player.GetComponent<PlayerCombatController>().ActiveWeapon;view=session.Player.GetComponent<FirstPersonLook>().View;}
        void Place(Vector3 position){var capsule=session.Player.GetComponent<CharacterController>();capsule.enabled=false;session.Player.transform.position=position;capsule.enabled=true;Physics.SyncTransforms();}
        void Use(ShelterPoint point)
        {
            Vector3 side=point.Action==ShelterAction.Leave?Vector3.left:Vector3.right;
            Place(new Vector3(point.transform.position.x,.05f,point.transform.position.z)+side*1.15f);view.transform.LookAt(point.transform.position);Physics.SyncTransforms();
            Check(session.Player.GetComponent<InteractionController>().TryInteract(),"actual doorway/storage interaction "+point.Action);
        }
        WorldItem FindLoot(bool ammunition)
        {
            foreach(var w in FindObjectsByType<WorldItem>())if(w.Available&&w.transform.root.name=="Session Loot"&&(w.Definition==weapon.Definition.Ammunition)==ammunition)return w;
            throw new InvalidOperationException("Missing actual deterministic world loot");
        }
        void Pickup(WorldItem w){Place(new Vector3(w.transform.position.x+1.3f,.05f,w.transform.position.z));view.transform.LookAt(w.transform.position);Physics.SyncTransforms();Check(session.Player.GetComponent<InteractionController>().TryInteract(),"actual world pickup");}
        int Slot(bool carried,Inventory.Data.ItemDefinition item)
        {int n=carried?inventory.Capacity:loop.Storage.Capacity;for(int i=0;i<n;i++){var s=carried?inventory.GetSlot(i):loop.Storage.GetSlot(i);if(!s.IsEmpty&&s.Item==item)return i;}return -1;}
        void Capture(string name){ScreenCapture.CaptureScreenshot(Path.Combine(evidence,name+".png"));}
        IEnumerator Run()
        {
            yield return null;session=FindAnyObjectByType<SessionFlow>();loop=FindAnyObjectByType<ShelterLoop>();Check(session&&loop,"production scene loaded");session.Resume();yield return new WaitForSeconds(.9f);Bind();
            var ammoDef=weapon.Definition.Ammunition;var player=session.Player;var stash=loop.Storage;var world=GameObject.Find("Session Loot");var loot=session.GetComponent<LootPopulationService>();int seed=loot.Seed,count=loot.GeneratedCount;
            Check(stash.GetTotalQuantity(ammoDef)==0&&weapon.RuntimeState.CurrentMagazine==30&&weapon.RuntimeState.ReserveAmmo==0,"clean initial economy");
            Use(loop.StoragePoint);Capture("standalone-preparation-empty");yield return null;loop.ClosePreparation();Use(loop.ExitPoint);Check(loop.ExpeditionIndex==1&&!loop.TryUse(loop.ExitPoint),"one first leave");
            var first=FindLoot(true);int firstAmount=first.Quantity;Vector3 consumed=first.transform.position;Pickup(first);var other=FindLoot(false);var otherDef=other.Definition;int otherAmount=other.Quantity;Pickup(other);yield return null;
            var zombie=FindAnyObjectByType<ZombieController>();Check(zombie,"live production Shambler");Place(zombie.transform.position+zombie.transform.forward*4);ZombieHitRegion chest=null;foreach(var r in zombie.GetComponentsInChildren<ZombieHitRegion>())if(r.name=="Damage_Chest")chest=r;
            Check(chest,"body hit region");float hp=zombie.GetComponent<ZombieHealth>().CurrentHealth;view.transform.LookAt(chest.HitCollider.bounds.center);Physics.SyncTransforms();weapon.OnFirePressed();weapon.OnFireReleased();Check(weapon.RuntimeState.CurrentMagazine==29&&zombie.GetComponent<ZombieHealth>().CurrentHealth<hp,"real rifle damages Shambler and consumes round");Capture("standalone-expedition-1");yield return null;
            Use(loop.ReturnPoint);Check(!loop.TryUse(loop.ReturnPoint),"return exactly once");Use(loop.StoragePoint);
            var ui=loop.PreparationUI;ui.Select(true,Slot(true,ammoDef));ui.DepositStackButton.onClick.Invoke();ui.Select(true,Slot(true,otherDef));ui.DepositStackButton.onClick.Invoke();Check(stash.GetTotalQuantity(ammoDef)==firstAmount&&stash.GetTotalQuantity(otherDef)==otherAmount,"selected deposit quantities");
            ui.Select(false,Slot(false,ammoDef));ui.WithdrawOneButton.onClick.Invoke();Check(weapon.RuntimeState.CurrentMagazine==29&&weapon.RuntimeState.ReserveAmmo==1,"chosen reserve separate from magazine");Capture("standalone-preparation-loaded");yield return null;
            loop.ClosePreparation();Use(loop.ExitPoint);Check(loop.ExpeditionIndex==2&&session.Player==player&&loop.Storage==stash&&GameObject.Find("Session Loot")==world&&loot.Seed==seed&&loot.GeneratedCount==count,"same-session second expedition");
            foreach(var w in FindObjectsByType<WorldItem>())Check(Vector3.Distance(w.transform.position,consumed)>.01f,"consumed point remains empty");
            weapon.OnReloadRequested();yield return new WaitForSeconds(2.7f);Check(weapon.RuntimeState.CurrentMagazine==30&&weapon.RuntimeState.ReserveAmmo==0,"reload uses withdrawn round");
            var second=FindLoot(true);int secondAmount=second.Quantity;Pickup(second);yield return null;view.transform.rotation=Quaternion.LookRotation(Vector3.up);weapon.OnFirePressed();weapon.OnFireReleased();Check(weapon.RuntimeState.CurrentMagazine==29,"second expedition shot");
            Use(loop.ReturnPoint);Use(loop.StoragePoint);ui.Select(true,Slot(true,ammoDef));ui.DepositStackButton.onClick.Invoke();Check(stash.GetTotalQuantity(ammoDef)==firstAmount-1+secondAmount,"accumulated stash");Check(30+firstAmount+secondAmount==stash.GetTotalQuantity(ammoDef)+weapon.RuntimeState.TotalAmmo+2,"total rounds conserved");
            Capture("standalone-second-return");yield return null;
            File.WriteAllText(Path.Combine(evidence,"standalone-conservation.txt"),"Initial magazine30 + acquired ammo"+firstAmount+"+"+secondAmount+" = final stash"+stash.GetTotalQuantity(ammoDef)+" + magazine29 + reserve0 + shots2. Other stored="+otherAmount+" "+otherDef.Id+". Same Player/stash/world preserved.\n");
            loop.ClosePreparation();session.Pause();Check(session.Paused,"pause");session.Resume();Check(!session.Paused,"resume");session.ReturnToMenu();yield return null;Check(session.InMenu&&loop.Storage==null&&!ui.IsOpen,"menu teardown");Capture("standalone-menu");yield return null;
            session.BeginSession();session.Resume();yield return new WaitForSeconds(.8f);Bind();Check(loop.ExpeditionIndex==0&&loop.Storage!=stash&&loop.Storage.GetTotalQuantity(ammoDef)==0&&weapon.RuntimeState.CurrentMagazine==30&&weapon.RuntimeState.ReserveAmmo==0,"fresh Session B");session.ReturnToMenu();yield return null;Check(errors==0,"clean project error log");
        }
    }
}
#endif
