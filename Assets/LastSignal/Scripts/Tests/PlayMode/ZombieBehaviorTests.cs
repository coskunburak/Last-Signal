#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    public class ZombieBehaviorTests
    {
        SessionFlow session;
        GameObject actor;
        ZombieController zombie;
        GameObject player;
        IEnumerator Setup()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/ZombieAcceptance.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            session=Object.FindAnyObjectByType<SessionFlow>();session.Resume();player=session.Player;
            player.GetComponent<FirstPersonMotor>().enabled=false;
            zombie=session.GetComponent<ZombieEncounter>().Actor;
            Assert.That(zombie,Is.Not.Null);actor=zombie.gameObject;
            Assert.That(zombie.Target,Is.EqualTo(player));
            Place(new Vector3(-5,0,-8));
        }
        void Place(Vector3 position)
        { var capsule=player.GetComponent<CharacterController>();capsule.enabled=false;player.transform.position=position;capsule.enabled=true;Physics.SyncTransforms(); }
        IEnumerator Wait(float seconds)
        { float until=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<until)yield return null; }
        IEnumerator State(ZombieState expected,float timeout=3)
        { float until=Time.realtimeSinceStartup+timeout;while(zombie.Runtime.State!=expected&&Time.realtimeSinceStartup<until)yield return null;Assert.That(zombie.Runtime.State,Is.EqualTo(expected)); }
        [UnityTest] public IEnumerator FovAcquisitionWallMemorySearchAndReacquisition()
        {
            yield return Setup();yield return Wait(.6f);Assert.That(zombie.Runtime.State,Is.EqualTo(ZombieState.Idle));
            Place(new Vector3(-3,0,2));yield return State(ZombieState.Chasing);
            Assert.That(zombie.Runtime.Confidence,Is.EqualTo(1));
            var a=zombie.Runtime.LastKnownPosition;
            Place(new Vector3(3,0,4));yield return State(ZombieState.Searching);
            Assert.That(Vector3.Distance(zombie.Runtime.LastKnownPosition,a),Is.LessThan(.01f),"Wall must freeze memory at A");
            Place(new Vector3(6,0,6));yield return Wait(.3f);
            Assert.That(Vector3.Distance(zombie.Runtime.LastKnownPosition,a),Is.LessThan(.01f),"Hidden movement to B must not alter A");
            Place(actor.transform.position+actor.transform.forward*3);yield return State(ZombieState.Chasing);
        }
        [UnityTest] public IEnumerator InitiallyOccludedPlayerIsNotAcquiredAndSearchExpires()
        {
            yield return Setup();actor.transform.position=new Vector3(-3,0,2);actor.transform.rotation=Quaternion.Euler(0,90,0);
            actor.GetComponent<NavMeshAgent>().Warp(actor.transform.position);
            Place(new Vector3(3,0,2));yield return Wait(.8f);Assert.That(zombie.Runtime.State,Is.EqualTo(ZombieState.Idle));
            Place(new Vector3(-1,0,2));yield return State(ZombieState.Chasing);
            Place(new Vector3(100,0,100));yield return State(ZombieState.Searching);
            yield return Wait(3);Assert.That(zombie.Search.Inspecting||zombie.Search.PointIndex>0,Is.True,"Search must inspect the remembered area");
            yield return State(ZombieState.Idle,14);Assert.That(zombie.Runtime.HasMemory,Is.False);
        }
        [UnityTest] public IEnumerator PauseFreezesMotionKnowledgeAndTimersThenDestroyedTargetClears()
        {
            yield return Setup();Place(new Vector3(-5,0,1));yield return State(ZombieState.Chasing);
            zombie.SetPaused(true);session.Pause();var position=actor.transform.position;var clock=zombie.GameTime;int rays=zombie.Perception.Raycasts;
            yield return Wait(.5f);Assert.That(actor.transform.position,Is.EqualTo(position));Assert.That(zombie.GameTime,Is.EqualTo(clock));Assert.That(zombie.Perception.Raycasts,Is.EqualTo(rays));
            session.Resume();zombie.SetPaused(false);yield return Wait(.3f);Assert.That(zombie.GameTime,Is.GreaterThan(clock));
            Object.Destroy(player);yield return null;yield return null;Assert.That(zombie.Target==null,Is.True);Assert.That(zombie.Runtime.HasMemory,Is.False);
        }
        [UnityTest] public IEnumerator VisibleReacquisitionDoesNotWaitForExhaustedSearchTimeout()
        {
            yield return Setup();Place(new Vector3(-3,0,2));yield return State(ZombieState.Chasing);
            Place(new Vector3(3,0,4));yield return State(ZombieState.Searching);
            var nav=zombie.Navigation;nav.ResetPolicy();
            for(int i=0;i<3;i++)nav.MoveTo(new Vector3(100,100,100),zombie.GameTime+i*3,.35f);
            Assert.That(nav.Exhausted,Is.True);
            Place(actor.transform.position+actor.transform.forward*3);yield return State(ZombieState.Chasing);
            Assert.That(nav.Exhausted,Is.False);Assert.That(zombie.Runtime.SearchAge,Is.Zero);
        }
        [TearDown]public void Teardown(){if(actor)Object.DestroyImmediate(actor);if(session)session.ReturnToMenu();Time.timeScale=1;}
    }
}
#endif
