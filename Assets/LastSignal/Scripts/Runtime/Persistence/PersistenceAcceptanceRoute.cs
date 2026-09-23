#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Shelter;
using UnityEngine;

namespace LastSignal.Persistence
{
    /// <summary>Opt-in automated acceptance, not a human traversal. Uses production domain/interaction APIs.</summary>
    public static class PersistenceAcceptanceRoute
    {
        public static IEnumerator Run(SaveSession saves, string path, Action<string> evidence)
        {
            var flow=saves.GetComponent<SessionFlow>(); flow.Resume(); yield return new WaitForSeconds(.9f);
            var player=flow.Player; var inv=player.GetComponent<PlayerInventory>(); var loop=saves.GetComponent<ShelterLoop>();
            var weapon=player.GetComponent<PlayerCombatController>().ActiveWeapon; var ammo=weapon.Definition.Ammunition;
            // Explicit fixture starting resources create a full bag for a real partial pickup.
            inv.Initialize(2); inv.TryAdd(ammo,59);
            ItemDefinition blocker=null;
            foreach(var item in UnityEngine.Object.FindObjectsByType<WorldItem>()) if(item.Definition.Id.Value=="tool.wrench") blocker=item.Definition;
            Check(blocker,"Expected Workshop wrench definition"); inv.TryAdd(blocker,1);
            WorldItem partial=null,consumed=null;
            foreach(var item in UnityEngine.Object.FindObjectsByType<WorldItem>())
                if(item.Origin==WorldItemOrigin.Loot && item.Definition==ammo && item.Quantity>1) {partial=item;break;}
            Check(partial,"Expected actual S006 ammo point"); string partialId=partial.PersistentId; int original=partial.Quantity;
            Pick(flow,partial); Check(partial.Quantity==original-1,"Partial pickup must leave exact remainder");
            Check(ItemTransferService.Deposit(inv,loop.Storage,blocker,1).Moved==1,"Stash blocker");
            Check(inv.TrySplit(0,1,5),"Production split");
            player.GetComponent<FirstPersonLook>().View.transform.rotation=Quaternion.LookRotation(Vector3.right);
            Check(inv.TryDrop(1,2),"Production drop");
            string dropId=null; foreach(var item in UnityEngine.Object.FindObjectsByType<WorldItem>()) if(item.Origin==WorldItemOrigin.Drop) dropId=item.PersistentId;
            Check(dropId!=null,"Dropped entity identity");
            Check(ItemTransferService.Deposit(inv,loop.Storage,ammo,55).Moved==55,"Stash ammo");
            foreach(var item in UnityEngine.Object.FindObjectsByType<WorldItem>())
                if(item.Origin==WorldItemOrigin.Loot && item.Definition!=ammo && item.Quantity<=item.Definition.MaxStack) {consumed=item;break;}
            Check(consumed,"Expected other S006 resource"); string consumedId=consumed.PersistentId;
            Pick(flow,consumed); yield return null; Check(!consumed,"Full pickup consumed world entity");
            Place(player,loop.InsideAnchor.position,Quaternion.Euler(0,37,0));
            player.GetComponent<FirstPersonLook>().ApplyRecoil(5,0);
            player.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo {Amount=17});
            var actor=UnityEngine.Object.FindAnyObjectByType<ZombieEncounter>().Actor;
            actor.GetComponent<ZombieHealth>().TakeDamage(new DamageInfo {Amount=1000});
            // Commit a real shot into clear sky and a reload using carried reserve.
            var camera=player.GetComponent<FirstPersonLook>().View; camera.transform.rotation=Quaternion.LookRotation(Vector3.up);
            weapon.OnFirePressed(); weapon.OnFireReleased(); Check(weapon.RuntimeState.CurrentMagazine==29,"Accepted shot costs one round");
            weapon.OnReloadRequested(); yield return new WaitForSeconds(2.7f);
            Check(weapon.RuntimeState.CurrentMagazine==30 && inv.GetTotalQuantity(ammo)==2,"Reload conserves carried reserve");
            weapon.OnFirePressed();weapon.OnFireReleased(); Check(weapon.RuntimeState.CurrentMagazine==29,"Persist non-full magazine");
            var door=UnityEngine.Object.FindAnyObjectByType<DoorInteractable>(); Check(door.TryInteract(),"Open production door");
            float end=Time.realtimeSinceStartup+4;
            while(door.Busy && Time.realtimeSinceStartup<end) yield return null;
            Check(door.IsOpen&&!door.Busy,"Door opened normally");
            // Ensure orientation reflects authoritative look, not the fixture's temporary aiming rotation.
            player.GetComponent<FirstPersonLook>().ApplyRecoil(0,0); flow.Pause();
            Check(saves.Capture(out var before).Success,"Capture real game state");
            var total=Totals(before); Check(saves.Save(path).Success,"Save actual route");
            var savedPosition=player.transform.position;
            for(int cycle=0;cycle<3;cycle++)
            {
                flow.ReturnToMenu(); yield return null;
                yield return saves.Load(path); Check(saves.LastResult.Success,"Load: "+saves.LastResult.Message);
                flow.Pause(); Check(saves.Capture(out var after).Success,"Capture restored state");
                SameTotals(total,Totals(after));
                Check(Vector3.Distance(flow.Player.transform.position,savedPosition)<.06f,"Player transform restored");
                Check(Mathf.Abs(flow.Player.GetComponent<PlayerHealth>().CurrentHealth-83)<.01f,"Player health restored");
                Check(after.weapon.magazine==29,"Magazine restored separately from reserve");
                Check(after.world.doors.Length==1&&after.world.doors[0].open,"Door open restored");
                int drops=0; bool remainder=false,tombstone=false;
                foreach(var item in after.world.items)
                {
                    if(item.id==dropId) {drops++;Check(item.quantity==2&&item.origin==WorldItemOrigin.Drop,"Drop quantity/source");}
                    if(item.id==partialId) remainder=item.quantity==original-1&&item.disposition==EntityDisposition.Present;
                    if(item.id==consumedId) tombstone=item.quantity==0&&item.disposition==EntityDisposition.Consumed;
                }
                Check(drops==1&&remainder&&tombstone,"Drop once, partial remainder, consumed tombstone");
                Check(after.world.enemies[0].health==0,"Dead enemy stays dead");
                Check(JsonUtility.ToJson(before.inventory)==JsonUtility.ToJson(after.inventory),"Ordered inventory unchanged");
                Check(JsonUtility.ToJson(before.shelter)==JsonUtility.ToJson(after.shelter),"Stash/lifecycle unchanged");
                evidence?.Invoke("cycle="+cycle+" PASS totals/slots/stash/magazine/door/player/partial/drop/tombstone/dead enemy");
            }
            flow.Resume(); yield return new WaitForSeconds(.9f);
            var loadedWeapon=flow.Player.GetComponent<PlayerCombatController>().ActiveWeapon;
            flow.Player.GetComponent<FirstPersonLook>().View.transform.rotation=Quaternion.LookRotation(Vector3.up);
            loadedWeapon.OnFirePressed();loadedWeapon.OnFireReleased();Check(loadedWeapon.RuntimeState.CurrentMagazine==28,"Combat functional after restore");
            WorldItem dropped=null; foreach(var item in UnityEngine.Object.FindObjectsByType<WorldItem>()) if(item.PersistentId==dropId)dropped=item;
            Check(dropped&&dropped.TryInteract(),"Re-pickup works after restore");
            evidence?.Invoke($"captureMs={saves.CaptureMilliseconds:F4} validatedWriteMs={saves.WriteMilliseconds:F4} validatedReadMs={saves.ReadMilliseconds:F4} hydrateMs={saves.HydrateMilliseconds:F4}; scene-level automated fixture, not human traversal");
            flow.ReturnToMenu(); yield return null; Check(flow.InMenu,"ReturnToMenu works");
        }
        static void Pick(SessionFlow flow,WorldItem item)
        {
            Place(flow.Player,new Vector3(item.transform.position.x+1.3f,.05f,item.transform.position.z),Quaternion.identity);
            flow.Player.GetComponent<FirstPersonLook>().View.transform.LookAt(item.transform.position);Physics.SyncTransforms();
            Check(flow.Player.GetComponent<InteractionController>().TryInteract(),"Production interaction ray pickup");
        }
        static void Place(GameObject player,Vector3 position,Quaternion rotation)
        {var c=player.GetComponent<CharacterController>();c.enabled=false;player.transform.SetPositionAndRotation(position,rotation);c.enabled=true;Physics.SyncTransforms();}
        static Dictionary<string,long> Totals(SaveGame state)
        {
            var totals=new Dictionary<string,long>(StringComparer.Ordinal);
            foreach(var s in state.inventory.slots) if(s.quantity>0) Add(totals,s.definitionId,s.quantity);
            foreach(var s in state.shelter.storage.slots) if(s.quantity>0) Add(totals,s.definitionId,s.quantity);
            foreach(var s in state.world.items) if(s.disposition==EntityDisposition.Present) Add(totals,s.definitionId,s.quantity);
            Add(totals,"ammo.rifle",state.weapon.magazine);return totals;
        }
        static void Add(Dictionary<string,long> totals,string id,int n){totals.TryGetValue(id,out long old);totals[id]=old+n;}
        static void SameTotals(Dictionary<string,long> expected,Dictionary<string,long> actual)
        {Check(expected.Count==actual.Count,"Definition count conserved");foreach(var pair in expected)Check(actual.TryGetValue(pair.Key,out long n)&&n==pair.Value,"Quantity conserved: "+pair.Key);}
        static void Check(bool value,string message){if(!value)throw new InvalidOperationException("Persistence acceptance: "+message);}
    }
}
#endif
