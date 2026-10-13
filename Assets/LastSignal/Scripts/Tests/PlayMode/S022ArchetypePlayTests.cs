#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using LastSignal.AI;
using LastSignal.WorldCells;
using LastSignal.WorldTime;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace LastSignal.Tests
{
    public class S022ArchetypePlayTests
    {
        SessionFlow flow; ZombieController actor;
        IEnumerator Load(string name,float distance=6)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/Validation/ZombieAcceptance.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            flow=Object.FindAnyObjectByType<SessionFlow>();flow.Resume();
            flow.Player.GetComponent<PlayerInputReader>().enabled=false;flow.Player.GetComponent<FirstPersonMotor>().enabled=false;
            var original=flow.GetComponent<ZombieEncounter>().Actor;original.Shutdown();original.gameObject.SetActive(false);
            var path=name=="Shambler" ? "Assets/LastSignal/Prefabs/Resources/LS_Zombie_Runtime.prefab" : "Assets/LastSignal/Prefabs/Enemies/Zombie/LS_Zombie_"+name+".prefab";
            actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),new Vector3(-8,0,0),Quaternion.identity).GetComponent<ZombieController>();
            Assert.That(actor.Initialize(),Is.True);Assert.That(actor.Bind(flow.Player),Is.True);Place(distance);
        }
        void Place(float distance)
        { var capsule=flow.Player.GetComponent<CharacterController>();capsule.enabled=false;
            flow.Player.transform.position=actor.transform.position+Vector3.forward*distance;capsule.enabled=true;Physics.SyncTransforms(); }
        IEnumerator Until(Func<bool> predicate,float limit=8)
        { float end=Time.realtimeSinceStartup+limit;while(!predicate() && Time.realtimeSinceStartup<end)yield return null;
            Assert.That(predicate(),Is.True,"Timeout: "+actor.Runtime.State+" / "+actor.Archetype.Phase); }
        [UnityTearDown] public IEnumerator Cleanup()
        { if(actor){actor.Shutdown();Object.Destroy(actor.gameObject);}if(flow)flow.ReturnToMenu();Time.timeScale=1;yield return null; }
        [UnityTest] public IEnumerator RunnerUsesNavMeshAndPausePreservesPreparation()
        {
            yield return Load("Runner",8);yield return Until(()=>actor.Archetype.Phase==ZombieArchetypePhase.Preparation);
            actor.SetPaused(true);float remaining=actor.Archetype.Remaining;actor.Simulate(100);Assert.That(actor.Archetype.Remaining,Is.EqualTo(remaining));
            actor.SetPaused(false);yield return Until(()=>actor.Archetype.Phase==ZombieArchetypePhase.Burst);
            Assert.That(actor.GetComponent<NavMeshAgent>().isOnNavMesh,Is.True);Assert.That(actor.GetComponent<ZombieHealth>().MaxHealth,Is.EqualTo(60));
            actor.GetComponent<ZombieHealth>().TakeDamage(new DamageInfo {Amount=20,Category=DamageCategory.Melee});
            Assert.That(actor.Archetype.Phase,Is.EqualTo(ZombieArchetypePhase.Recovery));Assert.That(actor.Attacking,Is.False);
        }
        [UnityTest] public IEnumerator LurkerWarnsBeforeAnyContactAndShutdownStopsIt()
        {
            yield return Load("Lurker",4);yield return Until(()=>actor.Archetype.Phase==ZombieArchetypePhase.Preparation);
            Assert.That(actor.ContactAttempts,Is.Zero);Assert.That(actor.Archetype.Remaining,Is.GreaterThan(.5f));
            actor.Shutdown();actor.Simulate(30);Assert.That(actor.ContactAttempts,Is.Zero);Assert.That(actor.Target,Is.Null);
        }
        [UnityTest] public IEnumerator ShamblerBaselineAndDeathDuringWindupAreAuthoritative()
        {
            yield return Load("Shambler",1.25f);Assert.That(actor.GetComponent<ZombieHealth>().MaxHealth,Is.EqualTo(100));
            yield return Until(()=>actor.Runtime.State==ZombieState.AttackWindup);
            int transactions=flow.Player.GetComponent<PlayerHealth>().DamageTransactions;
            actor.GetComponent<ZombieHealth>().TakeDamage(new DamageInfo {Amount=1000,Category=DamageCategory.Melee});
            actor.Simulate(10);Assert.That(actor.IsDead,Is.True);Assert.That(flow.Player.GetComponent<PlayerHealth>().DamageTransactions,Is.EqualTo(transactions));
        }
        [UnityTest] public IEnumerator RunnerDeathDuringWindupCancelsDamage()
        {
            yield return Load("Runner",1.25f);yield return Until(()=>actor.Runtime.State==ZombieState.AttackWindup);
            int before=flow.Player.GetComponent<PlayerHealth>().DamageTransactions;
            actor.GetComponent<ZombieHealth>().TakeDamage(new DamageInfo {Amount=1000,Category=DamageCategory.Melee});
            actor.Simulate(20);Assert.That(actor.IsDead,Is.True);Assert.That(flow.Player.GetComponent<PlayerHealth>().DamageTransactions,Is.EqualTo(before));
        }
        [UnityTest] public IEnumerator RunnerCommittedStrikeMissesSuccessfulRetreat()
        {
            yield return Load("Runner",1.25f);yield return Until(()=>actor.Runtime.State==ZombieState.AttackCommit);
            int before=flow.Player.GetComponent<PlayerHealth>().DamageTransactions;Place(4);
            yield return Until(()=>actor.ContactConsumed);
            Assert.That(actor.LastMeleeResult,Is.EqualTo(MeleeResult.OutOfRange));Assert.That(flow.Player.GetComponent<PlayerHealth>().DamageTransactions,Is.EqualTo(before));
        }
        [UnityTest] public IEnumerator SharedTargetCannotReceiveSameFrameCrowdDamage()
        {
            var go=new GameObject("S022 spacing target");var a=new GameObject("a");var b=new GameObject("b");
            try { var spacing=go.AddComponent<ZombieMeleeSpacing>();Assert.That(spacing.TryContact(10,a),Is.True);
                Assert.That(spacing.TryContact(10,b),Is.False);Assert.That(spacing.TryContact(10.29f,b),Is.True);
                go.SetActive(false);go.SetActive(true);Assert.That(spacing.TryContact(0,a),Is.True); }
            finally {Object.Destroy(go);Object.Destroy(a);Object.Destroy(b);}yield return null;
        }
        [UnityTest] public IEnumerator TypedPopulationSurvivesMigrationSnapshotAndRejectsUnknownWithoutMutation()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/Validation/WorldPopulationAcceptance.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            flow=Object.FindAnyObjectByType<SessionFlow>();flow.Resume();var pop=flow.GetComponent<WorldPopulationManager>();pop.MaterializationEnabled=false;
            int total=pop.TotalAccounted;pop.ReportNoise("noise:1","cell:1:0",5);
            var snapshot=pop.GetSaveSnapshot();Assert.That(snapshot.migrations[0].actors.Length,Is.EqualTo(snapshot.migrations[0].size));
            string id=snapshot.migrations[0].actors[0].id,type=snapshot.migrations[0].actors[0].archetypeId;
            pop.SyncFromSave(snapshot);var clock=flow.GetComponent<WorldClock>();clock.Simulation.AdvanceUntil(pop.Migrations[0].ArrivalTime,clock.Exposure);
            var restored=pop.GetSaveSnapshot();bool found=false;foreach(var unit in restored.actors)if(unit.id==id){found=true;Assert.That(unit.archetypeId,Is.EqualTo(type));}
            Assert.That(found,Is.True);Assert.That(pop.TotalAccounted,Is.EqualTo(total));
            Assert.That(pop.ArchetypeCount(ZombieArchetypes.Shambler)+pop.ArchetypeCount(ZombieArchetypes.Lurker)+pop.ArchetypeCount(ZombieArchetypes.Runner),Is.EqualTo(total));
            restored.actors[0].archetypeId="future";Assert.Throws<InvalidOperationException>(()=>pop.SyncFromSave(restored));Assert.That(pop.TotalAccounted,Is.EqualTo(total));
        }
        [UnityTest] public IEnumerator StreamedActorsKeepTypedIdentityHealthAndTombstones()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/Validation/WorldPopulationAcceptance.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            flow=Object.FindAnyObjectByType<SessionFlow>();flow.Resume();flow.Player.GetComponent<PlayerInputReader>().enabled=false;
            var pop=flow.GetComponent<WorldPopulationManager>();pop.MaterializationEnabled=false;
            var cells=flow.GetComponent<WorldCellManager>();yield return WorldCellAcceptanceRoute.Ready(cells,"cell:1:0");Assert.That(cells.TryEnter("cell:1:0"),Is.True);
            flow.Player.transform.rotation=Quaternion.Euler(0,225,0);
            var before=pop.GetSaveSnapshot();var identity=new Dictionary<string,string>();foreach(var unit in before.actors)identity.Add(unit.id,unit.archetypeId);
            int total=pop.TotalAccounted;pop.MaterializationEnabled=true;
            for(int i=0;i<120 && pop.PhysicalCount==0;i++){pop.TryMaterialize();yield return null;}
            Assert.That(pop.PhysicalCount,Is.GreaterThan(0),pop.MaterializationState);pop.MaterializationEnabled=false;
            ZombieController victim=null;
            foreach(var zombie in Object.FindObjectsByType<ZombieController>(FindObjectsSortMode.None))
                if(zombie!=flow.GetComponent<ZombieEncounter>().Actor && !zombie.transform.IsChildOf(cells.Content("cell:1:0").transform)) { victim=zombie;break; }
            Assert.That(victim,Is.Not.Null);victim.GetComponent<ZombieHealth>().TakeDamage(new DamageInfo {Amount=5,Category=DamageCategory.Melee});
            string kind=victim.Definition.ArchetypeId;
            var damaged=pop.GetSaveSnapshot();bool wounded=false;foreach(var unit in damaged.actors)
                if(unit.archetypeId==kind && unit.health==victim.GetComponent<ZombieHealth>().CurrentHealth)wounded=true;
            Assert.That(wounded,Is.True);pop.DematerializeCell("cell:1:0");yield return null;
            var unloaded=pop.GetSaveSnapshot();foreach(var unit in unloaded.actors)Assert.That(unit.archetypeId,Is.EqualTo(identity[unit.id]));
            Assert.That(pop.PhysicalCount,Is.Zero);Assert.That(pop.TotalAccounted,Is.EqualTo(total));pop.SyncFromSave(unloaded);
            pop.MaterializationEnabled=true;
            for(int i=0;i<120 && pop.PhysicalCount==0;i++){pop.TryMaterialize();yield return null;}
            Assert.That(pop.PhysicalCount,Is.GreaterThan(0));pop.MaterializationEnabled=false;
            foreach(var zombie in Object.FindObjectsByType<ZombieController>(FindObjectsSortMode.None))
                if(zombie!=flow.GetComponent<ZombieEncounter>().Actor && !zombie.transform.IsChildOf(cells.Content("cell:1:0").transform))
                { zombie.GetComponent<ZombieHealth>().TakeDamage(new DamageInfo {Amount=1000,Category=DamageCategory.Melee});break; }
            var terminal=pop.GetSaveSnapshot();Assert.That(terminal.tombstones.Length,Is.GreaterThan(0));string deadId=terminal.tombstones[0].id;
            pop.SyncFromSave(terminal);var restored=pop.GetSaveSnapshot();foreach(var unit in restored.actors)Assert.That(unit.id,Is.Not.EqualTo(deadId));
            Assert.That(pop.TotalAccounted,Is.EqualTo(total));
        }
        [UnityTest] public IEnumerator DedicatedRouteLoadsVariantBindingsAndReleasesOnRestart()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/Validation/S022ArchetypeAcceptance.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            flow=Object.FindAnyObjectByType<SessionFlow>();flow.Resume();var route=Object.FindAnyObjectByType<S022AcceptanceRoute>();Assert.That(route,Is.Not.Null);
            route.Select(5);yield return new WaitForSeconds(1);Assert.That(route.ActiveActors,Is.EqualTo(3),route.Status);
            flow.ReturnToMenu();flow.BeginSession();yield return null;yield return null;Assert.That(route.ActiveActors,Is.Zero);
        }
    }
}
#endif
