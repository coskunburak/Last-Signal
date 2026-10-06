#if UNITY_EDITOR
using System.Collections;
using System.IO;
using LastSignal.Slice;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LastSignal.Tests
{
    public sealed class S013IntegrationTests
    {
        S012Acceptance driver;string directory;
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale=1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/Production/S013Cabin.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;driver=new GameObject("S013 test driver").AddComponent<S012Acceptance>();driver.Bind();driver.flow.Resume();
            directory=Path.Combine(Application.temporaryCachePath,"s013-"+System.Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        }
        [UnityTearDown] public IEnumerator Teardown(){if(driver){driver.flow.ReturnToMenu();Object.Destroy(driver.gameObject);}yield return null;Time.timeScale=1;}

        [UnityTest] public IEnumerator ModularDoorwayPassesProductionMotorAndZombieAgent()
        {
            yield return driver.relay.ClearThreat();
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(50,-.25f,0);floor.transform.localScale=new Vector3(6,.5f,8);
            var doorway=Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Art/S013/Production/Prefabs/Doorway_2m.prefab"));doorway.transform.position=new Vector3(49,0,0);Physics.SyncTransforms();
            UnityEngine.AI.NavMeshData data=null;UnityEngine.AI.NavMeshDataInstance instance=default;GameObject actor=null;
            try
            {
                var route=new[]{new Vector3(50,.05f,-3),new Vector3(50,.05f,3)};
                yield return driver.Traverse(route,false,false,directory,"module-standing");yield return driver.Traverse(route,false,true,directory,"module-crouched");
                driver.relay.Place(new Vector3(52,.05f,3));
                var sources=new System.Collections.Generic.List<UnityEngine.AI.NavMeshBuildSource>();
                foreach(var col in new[]{floor.GetComponent<BoxCollider>()})sources.Add(new UnityEngine.AI.NavMeshBuildSource{shape=UnityEngine.AI.NavMeshBuildSourceShape.Box,transform=col.transform.localToWorldMatrix,size=col.size,area=0});
                foreach(var col in doorway.GetComponentsInChildren<BoxCollider>())sources.Add(new UnityEngine.AI.NavMeshBuildSource{shape=UnityEngine.AI.NavMeshBuildSourceShape.Box,transform=col.transform.localToWorldMatrix*Matrix4x4.Translate(col.center),size=col.size,area=0});
                // Use the resident navigation surface's configured agent type, not an invented radius.
                var surface=Object.FindAnyObjectByType<Unity.AI.Navigation.NavMeshSurface>();var settings=UnityEngine.AI.NavMesh.GetSettingsByID(surface.agentTypeID);
                data=UnityEngine.AI.NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(new Vector3(50,1,0),new Vector3(8,6,10)),Vector3.zero,Quaternion.identity);Assert.IsNotNull(data);instance=UnityEngine.AI.NavMesh.AddNavMeshData(data);
                actor=new GameObject("Doorway live navigation agent");actor.SetActive(false);actor.transform.position=new Vector3(50,0,-3);var agent=actor.AddComponent<UnityEngine.AI.NavMeshAgent>();agent.agentTypeID=settings.agentTypeID;agent.radius=settings.agentRadius;agent.height=settings.agentHeight;agent.speed=3;agent.stoppingDistance=.1f;actor.SetActive(true);
                Assert.IsTrue(agent.isOnNavMesh);Assert.IsTrue(agent.SetDestination(new Vector3(50,0,3)));float until=Time.realtimeSinceStartup+8;while(Time.realtimeSinceStartup<until&&Vector3.Distance(actor.transform.position,new Vector3(50,0,3))>.3f)yield return null;
                Assert.That(actor.transform.position.z,Is.GreaterThan(2.7f),"Actual navigation dimensions must traverse the modular opening");
            }
            finally{if(actor)Object.DestroyImmediate(actor);instance.Remove();if(data)Object.DestroyImmediate(data);Object.DestroyImmediate(doorway);Object.DestroyImmediate(floor);}
        }
        [UnityTest] public IEnumerator ClueFirstShelterOutputAndRecovery(){yield return driver.relay.Run(false,false,directory,"clue-first");yield return driver.Shelter(directory);yield return driver.DeathCheckpoint(directory,"after-shelter");}
        [UnityTest] public IEnumerator FuseFirstAndDroppedRecovery(){yield return driver.relay.Run(true,true,directory,"early-recovery");}
        [UnityTest] public IEnumerator BothRoutesHaveContinuousCollision(){yield return driver.relay.ClearThreat();yield return driver.Traverse(S012Acceptance.Direct,false,false,directory,"direct");yield return driver.Traverse(S012Acceptance.Covered,false,false,directory,"covered");}
        [UnityTest] public IEnumerator SprintAndCrouchUseProductionMotor(){yield return driver.relay.ClearThreat();yield return driver.Traverse(S012Acceptance.Direct,true,false,directory,"sprint");yield return driver.Traverse(S012Acceptance.Covered,false,true,directory,"crouch");}
        [UnityTest] public IEnumerator RelayReturnIsTraversable(){yield return driver.relay.ClearThreat();yield return driver.Traverse(S012Acceptance.Return,false,false,directory,"relay-return");}
        [UnityTest] public IEnumerator CrowbarDamagesLiveEncounter(){yield return driver.Melee(directory);}
        [UnityTest] public IEnumerator DeathBeforeExpeditionAndWithFuse(){yield return driver.DeathCheckpoint(directory,"early");driver.relay.Pick(driver.relay.mission.fuse);yield return null;yield return driver.DeathCheckpoint(directory,"fuse");}
        [UnityTest] public IEnumerator CellSaveQuitLoadRetainsMission(){yield return driver.Cells(directory);Assert.IsFalse(driver.relay.mission.Progress.Repaired);}
    }
}
#endif
