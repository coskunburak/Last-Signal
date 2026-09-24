#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using LastSignal.WorldCells;
using LastSignal.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
namespace LastSignal.Tests
{
    public class WorldCellIntegrationTests
    {
        WorldCellManager cells; SessionFlow flow; string directory;
        [UnitySetUp] public IEnumerator Setup()
        {
            directory=Path.Combine(Path.GetTempPath(),"LastSignal-Cells-"+Guid.NewGuid().ToString("N"));
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/WorldCellAcceptance.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;cells=UnityEngine.Object.FindAnyObjectByType<WorldCellManager>();flow=cells.GetComponent<SessionFlow>();flow.Resume();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { if(flow)flow.ReturnToMenu();Time.timeScale=1;yield return null;if(Directory.Exists(directory))Directory.Delete(directory,true); }
        [UnityTest] public IEnumerator FullTwoCellRouteAndTwelveCycleSoak()
        {
            var report=new System.Text.StringBuilder();yield return WorldCellAcceptanceRoute.Run(cells,directory,s=>report.AppendLine(s));
            File.WriteAllText("Docs/Implementation/P02-GAP-S007/Evidence/20260924-entry/playmode-route.txt",report.ToString());
        }
        [UnityTest] public IEnumerator LoadingGateAndRestoreStagesRejectEntryAndSave()
        {
            Assert.IsTrue(cells.Request(WorldCellAcceptanceRoute.A));Assert.IsFalse(cells.TryEnter(WorldCellAcceptanceRoute.A));
            Assert.AreEqual(SaveError.Busy,cells.GetComponent<SaveSession>().Capture(out _).Error);
            yield return null;Assert.IsFalse(cells.TryEnter(WorldCellAcceptanceRoute.A));
            yield return WorldCellAcceptanceRoute.Ready(cells,WorldCellAcceptanceRoute.A);
            Assert.IsTrue(cells.Content(WorldCellAcceptanceRoute.A).CollisionReady());Assert.IsTrue(cells.TryEnter(WorldCellAcceptanceRoute.A));
        }
        [UnityTest] public IEnumerator CancelLoadAndSessionRestartCannotPublishOldContent()
        {
            cells.Request(WorldCellAcceptanceRoute.B);yield return null;cells.Cancel(WorldCellAcceptanceRoute.B);
            yield return null;yield return null;Assert.AreEqual(CellState.Unloaded,cells.State(WorldCellAcceptanceRoute.B));Assert.IsNull(cells.Content(WorldCellAcceptanceRoute.B));
            cells.Request(WorldCellAcceptanceRoute.A);yield return null;flow.ReturnToMenu();flow.BeginSession();
            yield return null;yield return null;Assert.AreEqual(CellState.Unloaded,cells.State(WorldCellAcceptanceRoute.A));Assert.IsNull(cells.Content(WorldCellAcceptanceRoute.A));
        }
        [UnityTest] public IEnumerator MissingCollisionFailsClosed()
        {
            cells.Request(WorldCellAcceptanceRoute.A);
            for(int i=0;i<10&&!cells.Content(WorldCellAcceptanceRoute.A);i++)yield return null;
            Assert.IsNotNull(cells.Content(WorldCellAcceptanceRoute.A));cells.Content(WorldCellAcceptanceRoute.A).ground.enabled=false;
            yield return null;yield return null;yield return null;
            Assert.AreEqual(CellState.Failed,cells.State(WorldCellAcceptanceRoute.A));Assert.IsFalse(cells.TryEnter(WorldCellAcceptanceRoute.A));
        }
    }
}
#endif
