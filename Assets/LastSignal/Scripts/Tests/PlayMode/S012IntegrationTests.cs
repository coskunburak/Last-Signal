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
    public sealed class S012IntegrationTests
    {
        S012Acceptance driver;string directory;
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale=1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/IntegratedGraybox.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;driver=new GameObject("S012 test driver").AddComponent<S012Acceptance>();driver.Bind();driver.flow.Resume();
            directory=Path.Combine(Application.temporaryCachePath,"s012-"+System.Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        }
        [UnityTearDown] public IEnumerator Teardown(){if(driver){driver.flow.ReturnToMenu();Object.Destroy(driver.gameObject);}yield return null;Time.timeScale=1;}
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
