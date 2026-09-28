#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using LastSignal.Noise;
using UnityEngine;
using UnityEngine.AI;
namespace LastSignal
{
    public static class R03AcceptanceRoute
    {
        static void Check(bool value,string message) { if(!value)throw new InvalidOperationException("R03: "+message); }
        public static void Place(GameObject player,Vector3 point)
        { var c=player.GetComponent<CharacterController>();c.enabled=false;player.transform.position=point;c.enabled=true;Physics.SyncTransforms(); }
        public static IEnumerator State(ZombieController actor,ZombieState expected,float timeout=5)
        { float end=Time.realtimeSinceStartup+timeout;while(actor.Runtime.State!=expected&&Time.realtimeSinceStartup<end)yield return null;Check(actor.Runtime.State==expected,"Expected "+expected+", actual "+actor.Runtime.State); }
        static IEnumerator Ready(WeaponController weapon)
        { float end=Time.realtimeSinceStartup+8;while((weapon.RuntimeState.State!=WeaponState.Ready||weapon.RuntimeState.FireCooldownRemaining>0)&&Time.realtimeSinceStartup<end)yield return null;Check(weapon.RuntimeState.State==WeaponState.Ready,"Rifle not ready"); }
        static IEnumerator CaptureOverview(SessionFlow flow,string directory,string name,Vector3 heard)
        {
            flow.Pause();
            var view=new GameObject("R03 acceptance overview");var camera=view.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(0,18,1),Quaternion.Euler(90,0,0));camera.orthographic=true;camera.orthographicSize=10;camera.depth=100;
            var a=GameObject.CreatePrimitive(PrimitiveType.Sphere);a.name="Heard A";a.GetComponent<Collider>().enabled=false;a.transform.position=heard+Vector3.up*.3f;a.transform.localScale=Vector3.one*.5f;a.GetComponent<Renderer>().material.color=Color.yellow;
            var b=GameObject.CreatePrimitive(PrimitiveType.Sphere);b.name="Player current B";b.GetComponent<Collider>().enabled=false;b.transform.position=flow.Player.transform.position+Vector3.up*.5f;b.transform.localScale=Vector3.one*.5f;b.GetComponent<Renderer>().material.color=Color.cyan;
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,name));yield return null;
            UnityEngine.Object.Destroy(view);UnityEngine.Object.Destroy(a);UnityEngine.Object.Destroy(b);flow.Resume();
        }
        public static IEnumerator Run(SessionFlow flow,string directory,Action<string> log,bool screenshots=false)
        {
            Directory.CreateDirectory(directory);flow.Resume();yield return null;
            var actor=flow.GetComponent<ZombieEncounter>().Actor;Check(actor,"Production encounter missing");
            var player=flow.Player;var motor=player.GetComponent<FirstPersonMotor>();motor.enabled=false;
            var listener=actor.GetComponent<ZombieNoiseListener>();Check(listener.Registered,"Listener missing session registration");
            actor.GetComponent<NavMeshAgent>().Warp(new Vector3(-3,0,2));actor.transform.rotation=Quaternion.Euler(0,90,0);
            Place(player,new Vector3(3,0,2));player.transform.rotation=Quaternion.identity;
            yield return new WaitForSeconds(.6f);Check(actor.Runtime.State==ZombieState.Idle&&!actor.Runtime.Visible,"Wall must hide player");
            log("Idle: unseen and silent; no auditory memory; listeners="+flow.Noise.ListenerCount);
            var combat=player.GetComponent<PlayerCombatController>();var weapon=combat.Firearm;
            combat.SelectSlot(PlayerCombatController.CombatSlot.Firearm);weapon.Initialize(player.GetComponentInChildren<Camera>().transform,player,6);weapon.RequestEquip();yield return Ready(weapon);
            float volume=AudioListener.volume;AudioListener.volume=0;
            weapon.OnFireReleased();weapon.OnFirePressed();weapon.OnFireReleased();AudioListener.volume=volume;
            var e=flow.Noise.LastTrace.Event;Check(e.Category==GameplayNoiseCategory.Gunshot&&listener.LastHeard.Event.EventId.Equals(e.EventId),"Real muted shot not heard");
            yield return State(actor,ZombieState.Investigating);
            Check(!actor.Runtime.Visible&&listener.LastHeard.Occluded,"Gunshot must be occluded and unseen");
            Check(actor.InvestigateDestination==e.Position,"Investigation is not shot snapshot");
            var visual=actor.Runtime.LastKnownPosition;var direction=actor.Runtime.LastSeenDirection;
            log($"Idle → Investigating: event={e.EventId} category={e.Category} strength={listener.LastHeard.Strength:F4} occluded={listener.LastHeard.Occluded} A={e.Position}");
            Place(player,new Vector3(6,0,6));player.transform.rotation=Quaternion.Euler(0,135,0);
            yield return new WaitForSeconds(.25f);
            Check(!actor.Runtime.Visible&&actor.InvestigateDestination==e.Position,"Hidden movement changed investigation");
            Check(actor.Runtime.LastKnownPosition==visual&&actor.Runtime.LastSeenDirection==direction,"Hearing corrupted visual memory/direction");
            log($"ANTI-OMNISCIENCE PASS: Noise emitted at A={e.Position}; player moved unseen to B={player.transform.position}; investigate destination remained A={actor.InvestigateDestination}; visual position/direction unchanged.");
            float age=actor.Auditory.Age,investigateAge=actor.InvestigateAge;var position=actor.transform.position;int queries=listener.PhysicsQueries;
            flow.Pause();yield return new WaitForSecondsRealtime(.25f);
            Check(actor.Auditory.Age==age&&actor.InvestigateAge==investigateAge&&actor.transform.position==position&&listener.PhysicsQueries==queries,"Pause progressed investigation");
            flow.Resume();log("Pause: memory/timeout/motion/query count frozen PASS");
            int transitions=actor.Runtime.Transitions;listener.ReceiveNoise(e);Check(actor.Runtime.Transitions==transitions,"Duplicate transitioned");
            actor.GetComponent<ZombieHealth>().TakeDamage(new DamageInfo{Amount=1});yield return State(actor,ZombieState.HitReact);
            yield return State(actor,ZombieState.Investigating);log("Investigating → HitReact → Investigating PASS");
            if(screenshots) yield return CaptureOverview(flow, directory, "investigating.png", e.Position);
            Place(player,new Vector3(100,0,100));
            yield return State(actor,ZombieState.Searching,16);
            Check(actor.SearchFromHearing&&actor.Search.Anchor==e.Position,"Search seed not auditory snapshot");
            log($"Investigating → Searching: anchor={actor.Search.Anchor}; source=Hearing; pathRequests={actor.Navigation.PathRequests}; investigateAge={actor.InvestigateAge:F3}");
            // A fresh real gunshot at a second hidden location must interrupt search.
            Place(player,new Vector3(6,0,6));yield return Ready(weapon);weapon.OnFirePressed();weapon.OnFireReleased();
            yield return State(actor,ZombieState.Investigating);log("Searching → Investigating on fresh real shot PASS");
            actor.GetComponent<NavMeshAgent>().Warp(new Vector3(-5,0,-4));actor.transform.rotation=Quaternion.identity;Place(player,new Vector3(-5,0,0));
            yield return State(actor,ZombieState.Chasing);log("Investigating → Chasing on visual confirmation PASS");
            if(screenshots) yield return CaptureOverview(flow, directory, "vision-takeover.png", e.Position);
            actor.GetComponent<ZombieHealth>().TakeDamage(new DamageInfo{Amount=1000});Check(actor.IsDead&&!listener.Registered&&!actor.Auditory.HasStimulus,"Death retained hearing");
            var authority=flow.Noise;flow.ReturnToMenu();Check(authority.ListenerCount==0&&!authority.Active,"Menu leaked listener");yield return null;
            log("Death/menu: no memory, zero old listeners PASS");
            for(int i=0;i<3;i++)
            {
                flow.BeginSession();flow.Resume();yield return null;yield return null;
                var fresh=flow.GetComponent<ZombieEncounter>().Actor;Check(fresh&&!fresh.Auditory.HasStimulus&&flow.Noise.ListenerCount==1,"New session stale/duplicate hearing");
                Check(!flow.Noise.TryReceive(e),"Old epoch replay");
                flow.ReturnToMenu();yield return null;Check(flow.Noise==null,"Session teardown");
            }
            log("Session soak: 3 new-session/menu cycles; one listener per session; zero stale memories; old event rejected PASS");
            log("R03 ROUTE PASS");
        }
    }
}
#endif
