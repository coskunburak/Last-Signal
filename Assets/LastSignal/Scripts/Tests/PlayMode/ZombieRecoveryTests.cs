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
    public class ZombieRecoveryTests
    {
        SessionFlow session;
        GameObject probe;
        IEnumerator Load(string path)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; session=Object.FindAnyObjectByType<SessionFlow>();session.Resume();
        }
        [UnityTest] public IEnumerator NormalRouteTenSessionsClearTargetsTimersAndPausePresentation()
        {
            yield return Load("Assets/Scenes/SampleScene.unity");
            var encounter=session.GetComponent<ZombieEncounter>(); Assert.That(encounter,Is.Not.Null);
            for(int cycle=0;cycle<10;cycle++)
            {
                var actor=encounter.Actor;var player=session.Player;
                Assert.That(actor,Is.Not.Null);Assert.That(actor.Target,Is.EqualTo(player));
                Assert.That(Object.FindObjectsByType<ZombieController>().Length,Is.EqualTo(1));
                var animator=actor.GetComponentInChildren<Animator>(); Assert.That(animator.applyRootMotion,Is.False);
                yield return new WaitForSeconds(.15f);
                session.Pause();float clock=actor.GameTime;var position=actor.transform.position;int rays=actor.Perception.Raycasts;
                yield return new WaitForSecondsRealtime(.15f);
                Assert.That(actor.GameTime,Is.EqualTo(clock));Assert.That(actor.transform.position,Is.EqualTo(position));
                Assert.That(actor.Perception.Raycasts,Is.EqualTo(rays));Assert.That(animator.speed,Is.Zero);
                session.Resume();yield return null;Assert.That(animator.speed,Is.GreaterThan(0));
                session.ReturnToMenu();
                Assert.That(actor.Target,Is.Null);Assert.That(actor.Runtime.HasMemory,Is.False);Assert.That(actor.GameTime,Is.Zero);
                Assert.That(actor.Search.PointIndex,Is.Zero);Assert.That(actor.Navigation.Velocity,Is.EqualTo(Vector3.zero));
                yield return null;Assert.That(actor==null,Is.True);Assert.That(player==null,Is.True);
                Assert.That(Object.FindObjectsByType<ZombieController>().Length,Is.Zero);
                session.BeginSession();session.Resume();yield return null;
                Assert.That(encounter.Actor.Target,Is.EqualTo(session.Player));
            }
        }
        [UnityTest] public IEnumerator NavigationInvalidOffMeshStuckAndObstacleAreSafe()
        {
            yield return Load("Assets/LastSignal/Scenes/ZombieAcceptance.unity");
            var actor=session.GetComponent<ZombieEncounter>().Actor;actor.enabled=false;
            var nav=actor.Navigation;var agent=actor.GetComponent<NavMeshAgent>();
            Assert.That(nav.MoveTo(new Vector3(-5,0,7),0,.35f),Is.True);
            Assert.That(nav.PathStatus,Is.EqualTo(NavMeshPathStatus.PathComplete));
            Assert.That(agent.path.corners.Length,Is.GreaterThan(2),"Baked crate must require a detour");
            nav.Stop();nav.ResetPolicy();
            for(int i=0;i<10;i++)nav.MoveTo(new Vector3(100,100,100),i*3,.35f);
            Assert.That(nav.Exhausted,Is.True);Assert.That(nav.PathRequests,Is.LessThanOrEqualTo(3));
            nav.ResetPolicy();agent.isStopped=false;
            // Deliberately suppress integration while preserving a complete path: simulate a wedged actor.
            nav.MoveTo(new Vector3(-5,0,7),40,.35f);
            for(int i=0;i<10;i++) { nav.Tick(3,43+i*3);nav.MoveTo(new Vector3(-5,0,7),43+i*3,.35f); }
            Assert.That(nav.Stuck,Is.True);Assert.That(nav.Exhausted,Is.True);
            nav.ResetPolicy();agent.enabled=false;int requests=nav.PathRequests;
            for(int i=0;i<50;i++){Assert.That(nav.MoveTo(Vector3.zero,i,.35f),Is.False);nav.Tick(.1f,i);}
            Assert.That(nav.PathRequests,Is.EqualTo(requests));Assert.That(nav.Exhausted,Is.True);
        }
        [UnityTest] public IEnumerator PresenterUsesVelocityAndRestoresSpeedAfterPause()
        {
            yield return Load("Assets/LastSignal/Scenes/ZombieAcceptance.unity");
            var actor=session.GetComponent<ZombieEncounter>().Actor;actor.enabled=false;
            var presenter=actor.GetComponent<ZombieAnimationPresenter>();var animator=actor.GetComponentInChildren<Animator>();
            presenter.Present(.92f,1,false);animator.Update(.2f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),Is.True);
            Assert.That(animator.speed,Is.EqualTo(.92f/actor.Definition.MeasuredWalkSpeed).Within(.01f));
            presenter.Present(0,0,true);Assert.That(animator.speed,Is.Zero);
            presenter.Present(0,.2f,false);animator.Update(.2f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"),Is.True);Assert.That(animator.speed,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator PartialEndpointRetriesAreBoundedAndOffMeshSpawnFailsOnce()
        {
            yield return Load("Assets/LastSignal/Scenes/ZombieAcceptance.unity");
            var actor=session.GetComponent<ZombieEncounter>().Actor;actor.enabled=false;
            var nav=actor.Navigation;var agent=actor.GetComponent<NavMeshAgent>();
            nav.MoveTo(new Vector3(17,0,4),0,.35f);Assert.That(nav.PathStatus,Is.EqualTo(NavMeshPathStatus.PathPartial));
            var corners=agent.path.corners;Assert.That(agent.Warp(corners[corners.Length-1]),Is.True);
            nav.Stop();nav.ResetPolicy();
            for(int i=0;i<5;i++){nav.MoveTo(new Vector3(17,0,4),3+i*3,.35f);yield return null;}
            Assert.That(nav.Exhausted,Is.True);Assert.That(nav.PathRequests,Is.LessThanOrEqualTo(4));
            probe=new GameObject("OwnedOffMeshProbe");probe.SetActive(false);probe.transform.position=new Vector3(100,0,100);
            var bad=probe.AddComponent<NavMeshAgent>();bad.enabled=false;bad.agentTypeID=agent.agentTypeID;
            var off=probe.AddComponent<ZombieNavigation>();probe.SetActive(true);
            LogAssert.Expect(LogType.Error,"Zombie spawn has no matching NavMesh within bounded correction radius. Move the encounter spawn onto the baked Shambler surface.");
            Assert.That(off.Initialize(actor.Definition),Is.False);
            for(int i=0;i<30;i++){Assert.That(off.MoveTo(Vector3.zero,i,.35f),Is.False);off.Tick(.1f,i);}
            Assert.That(off.PathRequests,Is.Zero);Assert.That(bad.enabled,Is.False);
        }
        [Test]public void OffscreenInitializationEvaluatesIdleBeforeFirstVisibleFrame()
        {
            probe=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/LS_Zombie_Runtime.prefab"));
            var animator=probe.GetComponentInChildren<Animator>();animator.cullingMode=AnimatorCullingMode.CullCompletely;
            Assert.That(probe.GetComponent<ZombieAnimationPresenter>().Initialize(AssetDatabase.LoadAssetAtPath<ZombieDefinition>("Assets/LastSignal/Enemies/Zombie/Shambler.asset")),Is.True);
            var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);var initial=hand.position;
            Assert.That(animator.cullingMode,Is.EqualTo(AnimatorCullingMode.CullCompletely));
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Update(0);
            Assert.That(Vector3.Distance(initial,hand.position),Is.LessThan(.001f),"First rendered pose must already be evaluated Idle, not bind pose");
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(probe)Object.DestroyImmediate(probe);if(session)session.ReturnToMenu();Time.timeScale=1;yield return null;}
    }
}
#endif
