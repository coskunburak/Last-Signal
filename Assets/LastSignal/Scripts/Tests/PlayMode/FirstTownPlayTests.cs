#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using LastSignal.Persistence;
using LastSignal.Shelter;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace LastSignal.Tests
{
    // User-run only after native authoring and navigation bake. These tests are not visual acceptance.
    public sealed class FirstTownPlayTests
    {
        const string Scene="Assets/LastSignal/Scenes/Production/FirstTownCore.unity";
        SessionFlow flow; SaveSession save; string directory,path;
        [UnitySetUp] public IEnumerator Setup()
        {
            Assert.That(File.Exists(Scene),Is.True,"Run First Town/Create and Bake navigation first.");
            directory=Path.Combine(Path.GetTempPath(),"LastSignal-FirstTown-"+Guid.NewGuid().ToString("N")); path=Path.Combine(directory,"town.json");
            Time.timeScale=1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            flow=Object.FindAnyObjectByType<SessionFlow>(); save=flow.GetComponent<SaveSession>(); flow.Resume();
            yield return new WaitForSeconds(.5f);
            Assert.That(save.ValidateAuthoring().Success,Is.True,save.ValidateAuthoring().Message);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(flow)flow.ReturnToMenu(); Time.timeScale=1; yield return null;
            if(directory!=null && Directory.Exists(directory))Directory.Delete(directory,true);
        }
        [UnityTest] public IEnumerator MultipleEncounterDeathsRestoreByIdentityAfterOwnerReordering()
        {
            var all=flow.ResidentEncounters.ToArray(); Assert.That(all.Length,Is.GreaterThan(2));
            all[0].Actor.GetComponent<ZombieHealth>().TakeDamage(new DamageInfo { Amount=10000 });
            string dead=all[0].GetComponent<PersistentEntityId>().Id;
            Assert.That(save.Save(path).Success,Is.True,save.LastResult.Message);
            flow.ReturnToMenu(); yield return null;
            Array.Reverse(all); flow.ConfigureResidentEncounters(all[0],all.Skip(1).ToArray());
            yield return save.Load(path);
            Assert.That(save.LastResult.Success,Is.True,save.LastResult.Message);
            Assert.That(flow.ResidentEncounters.Single(e=>e.GetComponent<PersistentEntityId>().Id==dead).CaptureEnemy().health,Is.Zero);
            Assert.That(flow.ResidentEncounters.Count(e=>e.CaptureEnemy().health>0),Is.EqualTo(all.Length-1));
            Assert.That(save.Save(path).Success,Is.True,save.LastResult.Message);
        }
        [UnityTest] public IEnumerator PhysicalBoundaryChangesExpeditionWithoutMovingPlayerOrDuplicatingEntries()
        {
            var shelter=flow.GetComponent<ShelterLoop>(); Assert.That(shelter.PhysicalTraversal);
            Assert.That(shelter.TryUse(shelter.ExitPoint),Is.False,"Legacy shortcut must be unavailable in town.");
            Place(new Vector3(-180,.1f,-135)); yield return null; yield return null;
            Assert.That(shelter.State,Is.EqualTo(ExpeditionState.Expedition)); Assert.That(shelter.ExpeditionIndex,Is.EqualTo(1));
            Assert.That(flow.Player.transform.position.x,Is.EqualTo(-180).Within(.05f)); Assert.That(flow.Player.transform.position.z,Is.EqualTo(-135).Within(.05f));
            yield return null; Assert.That(shelter.ExpeditionIndex,Is.EqualTo(1));
            Place(new Vector3(-180,.1f,-145)); yield return null; yield return null;
            Assert.That(shelter.State,Is.EqualTo(ExpeditionState.Shelter)); Assert.That(shelter.ExpeditionIndex,Is.EqualTo(1));
            Assert.That(flow.Player.transform.position.z,Is.EqualTo(-145).Within(.05f));
        }
        void Place(Vector3 position)
        {
            // Fixture movement only; the production crossing code never writes player position.
            var capsule=flow.Player.GetComponent<CharacterController>(); capsule.enabled=false;
            flow.Player.transform.position=position; capsule.enabled=true; Physics.SyncTransforms();
        }
    }
}
#endif
