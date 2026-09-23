#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using LastSignal.Inventory;
using LastSignal.Persistence;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace LastSignal.Tests
{
    public class PersistenceIntegrationTests
    {
        SaveSession saves; SessionFlow flow; string directory,path;
        [UnitySetUp] public IEnumerator Setup()
        {
            directory=Path.Combine(Path.GetTempPath(),"LastSignal-Integration-"+Guid.NewGuid().ToString("N"));path=Path.Combine(directory,"current.json");
            Time.timeScale=1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/PersistenceAcceptance.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; saves=Object.FindAnyObjectByType<SaveSession>();flow=saves.GetComponent<SessionFlow>();flow.Resume();yield return new WaitForSeconds(.9f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(flow)flow.ReturnToMenu();Time.timeScale=1;yield return null;
            if(Directory.Exists(directory))Directory.Delete(directory,true);
        }
        [UnityTest] public IEnumerator RealSavedLootRouteThreeCleanSessionsConserveAllOwners()
        {yield return PersistenceAcceptanceRoute.Run(saves,path,s=>TestContext.Out.WriteLine(s));}
        [UnityTest] public IEnumerator CorruptFileNeverCreatesSessionOrChangesOriginal()
        {
            Assert.IsTrue(saves.Save(path).Success);flow.ReturnToMenu();yield return null;
            File.WriteAllText(path,"corrupt");yield return saves.Load(path);
            Assert.IsFalse(saves.LastResult.Success);Assert.IsTrue(flow.InMenu);Assert.AreEqual("corrupt",File.ReadAllText(path));
        }
        [UnityTest] public IEnumerator StaleHydrationCannotMutateReplacementSession()
        {
            Assert.IsTrue(saves.Save(path).Success);flow.ReturnToMenu();yield return null;
            var load=saves.Load(path);Assert.IsTrue(load.MoveNext());Assert.IsTrue(flow.Restoring);
            Assert.IsFalse(flow.Player.GetComponent<PlayerInputReader>().GameplayActive);
            flow.Resume();Assert.IsTrue(flow.Restoring);Assert.IsFalse(flow.Player.GetComponent<PlayerInputReader>().GameplayActive);
            flow.ReturnToMenu();flow.BeginSession();var next=flow.Player;
            Assert.IsFalse(load.MoveNext());Assert.AreEqual(SaveError.StaleSession,saves.LastResult.Error);Assert.AreSame(next,flow.Player);
            yield return null;
        }
        [UnityTest] public IEnumerator HydrationObserverFailureReturnsToSafeMenuWithoutOverwritingSave()
        {
            Assert.IsTrue(saves.Save(path).Success);var original=File.ReadAllBytes(path);flow.ReturnToMenu();yield return null;
            var load=saves.Load(path);Assert.IsTrue(load.MoveNext());
            flow.Player.GetComponent<PlayerInventory>().InventoryChanged+=()=>throw new InvalidOperationException("injected hydrate observer");
            yield return null;
            Assert.IsFalse(load.MoveNext());Assert.IsFalse(saves.LastResult.Success);Assert.IsTrue(flow.InMenu);
            CollectionAssert.AreEqual(original,File.ReadAllBytes(path));Assert.IsFalse(flow.Restoring);
        }
        [UnityTest] public IEnumerator RepeatedLoadWhilePlayingIsRejectedWithoutMutation()
        {
            Assert.IsTrue(saves.Save(path).Success);var player=flow.Player;
            yield return saves.Load(path);Assert.AreEqual(SaveError.Busy,saves.LastResult.Error);Assert.AreSame(player,flow.Player);
        }
        [UnityTest] public IEnumerator MissingGeneratedObjectIsNotMistakenForConsumption()
        {
            WorldItem item=null;foreach(var w in Object.FindObjectsByType<WorldItem>())if(w.Origin==WorldItemOrigin.Loot){item=w;break;}
            Assert.IsNotNull(item);item.gameObject.SetActive(false);
            Assert.AreEqual(SaveError.InvalidData,saves.Capture(out _).Error);
            item.gameObject.SetActive(true);Assert.IsTrue(saves.Capture(out _).Success);yield return null;
        }
        [UnityTest] public IEnumerator DisposedLoadCannotStrandRestoringSession()
        {
            Assert.IsTrue(saves.Save(path).Success);flow.ReturnToMenu();yield return null;
            var load=saves.Load(path);Assert.IsTrue(load.MoveNext());Assert.IsTrue(flow.Restoring);
            ((IDisposable)load).Dispose();Assert.IsTrue(flow.InMenu);Assert.IsFalse(flow.Restoring);
            yield return null;yield return saves.Load(path);Assert.IsTrue(saves.LastResult.Success,saves.LastResult.Message);
        }
    }
}
#endif
