#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using LastSignal.Persistence;
using LastSignal.Shelter;
using LastSignal.Inventory;
using UnityEngine;
namespace LastSignal.WorldTime
{
    /// <summary>Opt-in automated production route. Positions the player at authored interaction points; not human traversal.</summary>
    public static class WorldTimeAcceptanceRoute
    {
        public static IEnumerator Run(WorldClock clock,string path,Action<string> log)
        {
            var flow=clock.Flow;var saves=clock.GetComponent<SaveSession>();var loop=clock.GetComponent<ShelterLoop>();
            var bed=UnityEngine.Object.FindAnyObjectByType<RestPoint>();flow.Resume();yield return null;
            double start=clock.Simulation.Seconds;yield return new WaitForSeconds(.2f);Check(clock.Simulation.Seconds>start,"Normal world clock advances");
            Place(flow.Player,loop.StoragePoint.transform.position+Vector3.right);Check(loop.TryUse(loop.StoragePoint),"Preparation opens");
            start=clock.Simulation.Seconds;yield return null;yield return null;Check(clock.Simulation.Seconds==start,"Preparation pauses clock");loop.ClosePreparation();
            Place(flow.Player,loop.InsideAnchor.position);Check(loop.TryUse(loop.ExitPoint),"First expedition leave");yield return null;
            WorldItem consumed=null;foreach(var item in UnityEngine.Object.FindObjectsByType<WorldItem>())if(item.Available&&item.Origin==WorldItemOrigin.Loot){consumed=item;break;}
            Check(consumed,"Generated loot exists");string id=consumed.PersistentId;Check(consumed.TryInteract(),"Production pickup during expedition");yield return null;
            clock.Simulation.AdvanceUntil(clock.Simulation.NextWeather+600,1);Check(clock.Simulation.Raining&&clock.Simulation.Wetness>0,"Rain and exposed wetness");
            Place(flow.Player,loop.OutsideAnchor.position);clock.PollExposure(1);clock.PollExposure(1);Check(!clock.ProtectedFromRain,"Outdoors exposed");
            Check(loop.TryUse(loop.ReturnPoint),"Return while raining");clock.PollExposure(1);clock.PollExposure(1);Check(clock.ProtectedFromRain,"Shelter roof protects");
            log?.Invoke("PASS expedition 1 / pickup / rain return / actual roof protection");
            // The authored encounter is inside the 20m safety radius: clear it using production damage authority.
            var encounter=UnityEngine.Object.FindAnyObjectByType<ZombieEncounter>();
            encounter.Actor.GetComponent<ZombieHealth>().TakeDamage(new DamageInfo{Amount=1000});
            Check(!encounter.Actor.GetComponent<ZombieHealth>().IsAlive,"Existing threat cleared before rest");
            Place(flow.Player,new Vector3(-14.8f,.05f,10));yield return null;
            var hp=flow.Player.GetComponent<PlayerHealth>();hp.TakeDamage(new DamageInfo{Amount=20});
            Check(saves.Save(path).Success,"Save before sleep failed: " + saves.LastResult.Error);double before=clock.Simulation.Seconds;
            Check(clock.RequestSleep(bed,10800).Accepted,"Bed sleep accepted: "+clock.LastRejection);
            int guard=0;while(clock.Sleeping&&guard++<200)yield return null;Check(!clock.Sleeping,"Sleep completes bounded frames");
            Check(clock.LastSleep.Reason==AdvanceReason.Completed&&Math.Abs(clock.LastSleep.ElapsedSeconds-10800)<.001,"Sleep elapsed exactly 3h");
            Check(clock.Simulation.Seconds>=before+10800&&hp.CurrentHealth>80,"Sleep advances time and heals");
            flow.Pause();Check(saves.Save(path).Success,"Save after sleep");Check(saves.Capture(out var saved).Success,"Capture coherent boundary");
            log?.Invoke($"PASS sleep through weather boundaries / time={saved.worldTime.seconds:F3} wetness={saved.worldTime.wetness:F5} health={saved.player.health:F3}");
            flow.Resume();Place(flow.Player,loop.InsideAnchor.position);Check(loop.TryUse(loop.ExitPoint),"Second expedition leave");yield return null;
            Place(flow.Player,loop.OutsideAnchor.position);Check(loop.TryUse(loop.ReturnPoint),"Second return");
            Check(loop.ExpeditionIndex==2,"Two expeditions preserve lifecycle");
            flow.ReturnToMenu();yield return null;yield return saves.Load(path);Check(saves.LastResult.Success,"Load after sleep: "+saves.LastResult.Message);flow.Pause();
            Check(saves.Capture(out var restored).Success,"Restored capture");
            Check(JsonUtility.ToJson(saved.worldTime)==JsonUtility.ToJson(restored.worldTime),"Exact time/weather/wetness/schedule roundtrip");
            Check(JsonUtility.ToJson(saved.inventory)==JsonUtility.ToJson(restored.inventory),"Inventory continuity");
            Check(JsonUtility.ToJson(saved.shelter)==JsonUtility.ToJson(restored.shelter),"Stash and saved expedition continuity");
            Check(saved.weapon.magazine==restored.weapon.magazine,"Magazine continuity");
            foreach(var item in restored.world.items)if(item.id==id)Check(item.disposition==EntityDisposition.Consumed,"Consumed loot remains consumed");
            log?.Invoke("PASS second expedition / return / menu / load / inventory-stash-ammo-time-weather continuity / no loot respawn");
            flow.Resume();
        }
        public static void Place(GameObject player,Vector3 position)
        {var c=player.GetComponent<CharacterController>();c.enabled=false;player.transform.position=position;c.enabled=true;Physics.SyncTransforms();}
        static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    }
}
#endif
