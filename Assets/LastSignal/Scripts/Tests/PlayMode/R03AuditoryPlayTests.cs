#if UNITY_EDITOR
using System.Collections;
using System.IO;
using LastSignal.Noise;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LastSignal.Tests
{
    public sealed class R03AuditoryPlayTests
    {
        const string Evidence="Docs/Implementation/PreS010-Recovery/Evidence/R03/20260927-foundation/";
        SessionFlow flow;
        IEnumerator Setup()
        { yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/Validation/ZombieAcceptance.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;flow=Object.FindAnyObjectByType<SessionFlow>();flow.Resume(); }
        [UnityTest] public IEnumerator RealGunshotSnapshotPauseReactionSearchVisionAndSessionSoak()
        {yield return Setup();File.WriteAllText(Evidence+"anti-omniscience-trace.txt","");yield return R03AcceptanceRoute.Run(flow,Evidence+"play-route",line=>File.AppendAllText(Evidence+"anti-omniscience-trace.txt",line+"\n"));}
        [UnityTest] public IEnumerator ThirtyListenersWarmedPhysicsAllocationAndNoEventCost()
        {
            yield return Setup();var player=flow.Player;player.GetComponent<FirstPersonMotor>().enabled=false;R03AcceptanceRoute.Place(player,new Vector3(100,0,100));
            var prefab=Resources.Load<GameObject>("LS_Zombie_Runtime");var actors=new ZombieController[30];var listeners=new ZombieNoiseListener[30];
            for(int i=0;i<30;i++)
            {var go=Object.Instantiate(prefab,new Vector3(-8+(i%6)*.1f,0,-8+(i/6)*.1f),Quaternion.identity);actors[i]=go.GetComponent<ZombieController>();Assert.IsTrue(actors[i].Initialize());Assert.IsTrue(actors[i].Bind(player));listeners[i]=go.GetComponent<ZombieNoiseListener>();}
            yield return null;
            var request=new GameplayNoiseRequest(1,new Vector3(-6,1,-6),GameplayNoiseCategory.Gunshot,flow.NoiseTuning.Gunshot);
            for(int i=0;i<30;i++)flow.Noise.TryEmit(request,out _);
            int queries=0,accepted=0,transitions=0;foreach(var l in listeners){queries+=l.PhysicsQueries;accepted+=l.Accepted;}foreach(var a in actors)transitions+=a.Runtime.Transitions;
            var watch=new System.Diagnostics.Stopwatch();long before=System.GC.GetAllocatedBytesForCurrentThread();watch.Start();
            for(int i=0;i<1000;i++)flow.Noise.TryEmit(request,out _);
            watch.Stop();long bytes=System.GC.GetAllocatedBytesForCurrentThread()-before;
            int queryEnd=0,acceptEnd=0,transitionEnd=0;foreach(var l in listeners){queryEnd+=l.PhysicsQueries;acceptEnd+=l.Accepted;}foreach(var a in actors)transitionEnd+=a.Runtime.Transitions;
            Assert.AreEqual(30000,queryEnd-queries);Assert.AreEqual(0,transitionEnd-transitions);Assert.AreEqual(0,bytes);
            R03AcceptanceRoute.Place(player,new Vector3(-8,0,-3));
            foreach(var a in actors)a.MeasureManagedAllocations=true;
            int frames=0;long aiNs=0;float until=Time.realtimeSinceStartup+2;
            using(var recorder=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Scripts,"LastSignal.Zombie.AI",1))
            {while(Time.realtimeSinceStartup<until){yield return null;frames++;aiNs+=recorder.LastValue;}}
            int quiet=0,rays=0,paths=0,ticks=0;long tickBytes=0;
            foreach(var l in listeners)quiet+=l.PhysicsQueries;
            foreach(var a in actors){rays+=a.Perception.Raycasts;paths+=a.Navigation.PathRequests;ticks+=a.MeasuredTicks;tickBytes+=a.ManagedTickBytes;}
            Assert.AreEqual(queryEnd,quiet);Assert.Greater(rays,0);Assert.Greater(paths,0);
            File.WriteAllText(Evidence+"integrated-performance.txt",$"30 added active agents, 2 real seconds, frames={frames}, AI marker mean={aiNs/(frames*1e6):F6} ms/frame; measured ticks={ticks}; managed tick bytes={tickBytes}; cumulative vision rays={rays}; cumulative path requests={paths}; no-event hearing queries=0\n");
            File.WriteAllText(Evidence+"performance.txt",$"30 measured physical listeners; total registered={flow.Noise.ListenerCount}\n1000 warmed canonical gunshots: {watch.Elapsed.TotalMilliseconds:F3} ms\nManaged bytes={bytes}\nPhysics queries={queryEnd-queries}\nAccepted replacements={acceptEnd-accepted}\nExtra state transitions={transitionEnd-transitions}\nNo-event 2 seconds: 0 hearing queries\n");
            foreach(var actor in actors)Object.Destroy(actor.gameObject);yield return null;
        }
        [UnityTest] public IEnumerator CloseOccludedSoundCannotAttackAndDisableReenableIsUnique()
        {
            yield return Setup();var actor=flow.GetComponent<ZombieEncounter>().Actor;var listener=actor.GetComponent<ZombieNoiseListener>();var player=flow.Player;
            player.GetComponent<FirstPersonMotor>().enabled=false;actor.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(new Vector3(-.8f,0,2));actor.transform.rotation=Quaternion.Euler(0,90,0);R03AcceptanceRoute.Place(player,new Vector3(.8f,0,2));
            yield return new WaitForSeconds(.3f);float hp=player.GetComponent<PlayerHealth>().CurrentHealth;
            Assert.IsFalse(actor.Runtime.Visible);flow.Noise.TryEmit(new GameplayNoiseRequest(1,player.transform.position,GameplayNoiseCategory.Gunshot,flow.NoiseTuning.Gunshot),out var noise);
            for(int i=0;i<30;i++){actor.Simulate(.05f);Assert.IsFalse(actor.Attacking);}Assert.AreEqual(hp,player.GetComponent<PlayerHealth>().CurrentHealth);
            listener.enabled=false;Assert.AreEqual(0,flow.Noise.ListenerCount);listener.enabled=true;Assert.AreEqual(1,flow.Noise.ListenerCount);
            actor.Bind(player);Assert.IsFalse(actor.Auditory.HasStimulus);Assert.AreEqual(1,flow.Noise.ListenerCount);
        }
        [UnityTest] public IEnumerator RealMovementAndCrowbarProducersReachPhysicalListener()
        {
            yield return Setup();var actor=flow.GetComponent<ZombieEncounter>().Actor;var player=flow.Player;var motor=player.GetComponent<FirstPersonMotor>();motor.enabled=false;
            var listener=actor.GetComponent<ZombieNoiseListener>();var combat=player.GetComponent<PlayerCombatController>();
            actor.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(new Vector3(-5,0,-4));actor.transform.rotation=Quaternion.identity;
            R03AcceptanceRoute.Place(player,new Vector3(-5,0,-7.4f));player.transform.rotation=Quaternion.identity;
            for(int i=0;i<50;i++)motor.Simulate(Vector2.up,false,1f/60);
            Assert.AreEqual(GameplayNoiseCategory.Footstep,flow.Noise.LastTrace.Event.Category);Assert.IsTrue(actor.Auditory.HasStimulus);Assert.AreEqual(GameplayNoiseCategory.Footstep,actor.Auditory.Stimulus.Event.Category);
            actor.Bind(player);R03AcceptanceRoute.Place(player,new Vector3(-5,0,-10));motor.ResetNoiseCadence();player.GetComponent<PlayerStamina>().ResetSession();
            for(int i=0;i<40;i++)motor.Simulate(Vector2.up,true,1f/60);
            Assert.AreEqual(GameplayNoiseCategory.SprintFootstep,actor.Auditory.Stimulus.Event.Category);
            actor.Bind(player);R03AcceptanceRoute.Place(player,new Vector3(-5,0,-8));combat.SelectSlot(PlayerCombatController.CombatSlot.Melee);player.GetComponent<PlayerStamina>().ResetSession();
            ulong before=flow.Noise.AcceptedCount;Assert.IsTrue(combat.Melee.TryAttack());combat.Melee.Simulation.Tick(1);Assert.AreEqual(before,flow.Noise.AcceptedCount);Assert.IsFalse(actor.Auditory.HasStimulus);
            var eye=player.GetComponentInChildren<Camera>().transform;var target=new GameObject("R03 real crowbar impact fixture");target.layer=8;target.transform.position=eye.position+eye.forward*1.1f;
            var hp=target.AddComponent<ZombieHealth>();var collider=target.AddComponent<BoxCollider>();collider.size=Vector3.one*.4f;target.AddComponent<ZombieHitRegion>().Configure(hp,DamageRegion.Body,1,collider);Physics.SyncTransforms();
            player.GetComponent<PlayerStamina>().ResetSession();Assert.IsTrue(combat.Melee.TryAttack());combat.Melee.Simulation.Tick(1);
            Assert.AreEqual(before+1,flow.Noise.AcceptedCount);Assert.AreEqual(GameplayNoiseCategory.MeleeImpact,actor.Auditory.Stimulus.Event.Category);Assert.AreEqual(flow.Noise.LastTrace.Event.Position,actor.InvestigateDestination);
            File.WriteAllText(Evidence+"real-producers.txt","Real motor walk and sprint → physical listener → auditory memory PASS\nReal crowbar miss: no event/no memory; committed collider impact → Investigating at impact snapshot PASS\n");Object.Destroy(target);yield return null;
        }
        [UnityTest] public IEnumerator UnreachableAndRepeatedFootstepsCannotExtendTimeoutForever()
        {
            yield return Setup();var actor=flow.GetComponent<ZombieEncounter>().Actor;var player=flow.Player;player.GetComponent<FirstPersonMotor>().enabled=false;R03AcceptanceRoute.Place(player,new Vector3(100,0,100));
            var location=actor.transform.position+Vector3.up*20;flow.Noise.TryEmit(new GameplayNoiseRequest(1,location,GameplayNoiseCategory.Gunshot,flow.NoiseTuning.Gunshot),out _);
            Assert.AreEqual(ZombieState.Investigating,actor.Runtime.State);
            for(int i=0;i<300&&actor.Runtime.State==ZombieState.Investigating;i++)actor.Simulate(.05f);
            Assert.AreEqual(ZombieState.Searching,actor.Runtime.State);Assert.LessOrEqual(actor.Navigation.PathRequests,actor.Definition.RecoveryAttempts+1);
            for(int i=0;i<260;i++)actor.Simulate(.05f);Assert.AreEqual(ZombieState.Idle,actor.Runtime.State);
            var destination=actor.transform.position+Vector3.back*2;flow.Noise.TryEmit(new GameplayNoiseRequest(1,destination,GameplayNoiseCategory.SprintFootstep,flow.NoiseTuning.Sprint),out _);
            int transitions=actor.Runtime.Transitions,requests=actor.Navigation.PathRequests;
            for(int i=0;i<100;i++){actor.Simulate(.05f);flow.Noise.TryEmit(new GameplayNoiseRequest(1,destination,GameplayNoiseCategory.SprintFootstep,flow.NoiseTuning.Sprint),out _);}
            Assert.AreEqual(transitions,actor.Runtime.Transitions);Assert.Greater(actor.InvestigateAge,4.9f);Assert.LessOrEqual(actor.Navigation.PathRequests-requests,actor.Definition.RecoveryAttempts+1);
        }
        [UnityTest] public IEnumerator ReachableHeardPointArrivesThenSearches()
        {
            yield return Setup();var actor=flow.GetComponent<ZombieEncounter>().Actor;var player=flow.Player;player.GetComponent<FirstPersonMotor>().enabled=false;R03AcceptanceRoute.Place(player,new Vector3(100,0,100));
            var point=actor.transform.position+Vector3.forward*2;
            flow.Noise.TryEmit(new GameplayNoiseRequest(1,point,GameplayNoiseCategory.Gunshot,flow.NoiseTuning.Gunshot),out _);
            yield return R03AcceptanceRoute.State(actor,ZombieState.Searching,8);
            Assert.IsFalse(actor.Navigation.Exhausted);Assert.Less(Vector3.Distance(actor.transform.position,point),.8f);Assert.AreEqual(point,actor.Search.Anchor);Assert.IsTrue(actor.SearchFromHearing);
            File.WriteAllText(Evidence+"arrival.txt",$"Actual NavMesh movement arrived at {actor.transform.position} near snapshot {point}; Idle → Investigating → Searching; search anchor={actor.Search.Anchor}; elapsed={actor.InvestigateAge:F3}s PASS\n");
        }
        [UnityTest] public IEnumerator VisionWinsThenLossGraceUsesRememberedSound()
        {
            yield return Setup();var actor=flow.GetComponent<ZombieEncounter>().Actor;var player=flow.Player;player.GetComponent<FirstPersonMotor>().enabled=false;
            R03AcceptanceRoute.Place(player,new Vector3(-5,0,1));yield return R03AcceptanceRoute.State(actor,ZombieState.Chasing);
            var heard=new Vector3(-8,0,-6);flow.Noise.TryEmit(new GameplayNoiseRequest(1,heard,GameplayNoiseCategory.Gunshot,flow.NoiseTuning.Gunshot),out _);
            Assert.AreEqual(ZombieState.Chasing,actor.Runtime.State);Assert.AreNotEqual(heard,actor.Runtime.LastKnownPosition);Assert.IsTrue(actor.Auditory.HasStimulus);
            R03AcceptanceRoute.Place(player,new Vector3(100,0,100));yield return R03AcceptanceRoute.State(actor,ZombieState.Investigating);
            Assert.AreEqual(heard,actor.InvestigateDestination);Assert.AreNotEqual(player.transform.position,actor.InvestigateDestination);
        }
        [UnityTest] public IEnumerator SoundDuringHitReactionIsUsedAfterReactionCompletes()
        {
            yield return Setup();var actor=flow.GetComponent<ZombieEncounter>().Actor;flow.Player.GetComponent<FirstPersonMotor>().enabled=false;R03AcceptanceRoute.Place(flow.Player,new Vector3(100,0,100));
            actor.GetComponent<ZombieHealth>().TakeDamage(new DamageInfo{Amount=1});Assert.AreEqual(ZombieState.HitReact,actor.Runtime.State);
            var heard=actor.transform.position+Vector3.back*3;
            flow.Noise.TryEmit(new GameplayNoiseRequest(1,heard,GameplayNoiseCategory.Gunshot,flow.NoiseTuning.Gunshot),out _);
            Assert.AreEqual(ZombieState.HitReact,actor.Runtime.State);yield return R03AcceptanceRoute.State(actor,ZombieState.Investigating);
            Assert.AreEqual(heard,actor.InvestigateDestination);
        }
        [UnityTearDown] public IEnumerator Cleanup(){if(flow)flow.ReturnToMenu();Time.timeScale=1;yield return null;}
    }
}
#endif
