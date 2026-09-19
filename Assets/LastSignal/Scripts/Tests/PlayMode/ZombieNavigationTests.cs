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
    public class ZombieNavigationTests
    {
        GameObject actor;
        [UnityTest] public IEnumerator BakedArenaSupportsCompleteAndPartialPathsWithBoundedRequests()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/ZombieAcceptance.unity", new LoadSceneParameters(LoadSceneMode.Single));
            var session = Object.FindAnyObjectByType<SessionFlow>(); session.Resume();
            actor = new GameObject("NavigationProbe"); actor.SetActive(false);
            actor.transform.position = new Vector3(-5, 0, -4);
            var agent = actor.AddComponent<NavMeshAgent>(); agent.enabled = false;
            for (int i=0;i<NavMesh.GetSettingsCount();i++)
            { var s=NavMesh.GetSettingsByIndex(i); if(NavMesh.GetSettingsNameFromID(s.agentTypeID)=="LastSignal Shambler")agent.agentTypeID=s.agentTypeID; }
            var controller = actor.AddComponent<ZombieController>();
            controller.Configure(AssetDatabase.LoadAssetAtPath<ZombieDefinition>("Assets/LastSignal/Enemies/Zombie/Shambler.asset"));
            actor.SetActive(true); Assert.That(controller.Initialize(), Is.True);
            var nav = controller.Navigation;
            Assert.That(nav.MoveTo(new Vector3(-5, 0, 7),0,.35f), Is.True);
            Assert.That(nav.PathStatus, Is.EqualTo(NavMeshPathStatus.PathComplete));
            for(int i=0;i<10;i++)nav.MoveTo(new Vector3(-5,0,7),i*.01f,.35f);
            Assert.That(nav.PathRequests, Is.EqualTo(1));
            nav.ResetPolicy(); nav.MoveTo(new Vector3(17,0,4),1,.35f);
            Assert.That(nav.PathStatus, Is.EqualTo(NavMeshPathStatus.PathPartial));
            Assert.That(nav.Destination, Is.EqualTo(new Vector3(17,0,4)));
            Object.DestroyImmediate(actor); actor=null; session.ReturnToMenu();
        }
        [TearDown] public void TearDown() { if(actor)Object.DestroyImmediate(actor); Time.timeScale=1; }
    }
}
#endif
