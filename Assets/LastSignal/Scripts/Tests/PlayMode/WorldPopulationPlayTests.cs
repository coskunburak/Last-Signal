#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Text;
using LastSignal.AI;
using LastSignal.Persistence;
using LastSignal.WorldTime;
using LastSignal.WorldCells;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

namespace LastSignal.Tests
{
    public class WorldPopulationPlayTests
    {
        SessionFlow flow; WorldPopulationManager pop; WorldCellManager cells; WorldClock clock;
        string directory;
        const string Evidence = "Docs/Implementation/P02-GAP-S009/Evidence/20260925-independent/";
        [UnitySetUp] public IEnumerator Setup()
        {
            directory = Path.Combine(Path.GetTempPath(), "LastSignal-S009-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/WorldPopulationAcceptance.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            flow = UnityEngine.Object.FindAnyObjectByType<SessionFlow>(); Assert.IsNotNull(flow);
            pop = flow.GetComponent<WorldPopulationManager>(); cells = flow.GetComponent<WorldCellManager>(); clock = flow.GetComponent<WorldClock>();
            Assert.IsNotNull(pop); flow.Resume();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { if (flow) flow.ReturnToMenu(); Time.timeScale = 1; yield return null; if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        [UnityTest] public IEnumerator ProductionRifleTravelDeathSaveLoadRoute()
        {
            var report = new StringBuilder();
            try { yield return WorldPopulationAcceptanceRoute.Run(clock, Path.Combine(directory, "route.json"), s => report.AppendLine(s)); }
            finally { File.WriteAllText(Evidence + "playmode-route.txt", report.ToString()); }
        }
        [UnityTest] public IEnumerator ReceiptCompactionBoundsIsolationAndReplayAcrossSnapshot()
        {
            pop.MaterializationEnabled = false;
            for (int i = 1; i <= 100; i++) pop.ReportNoise("noise:" + i, "cell:1:0", 100);
            Assert.AreEqual(1, pop.GetPressure("cell:1:0").Pressure); Assert.AreEqual(0, pop.GetPressure("cell:2:0").Pressure);
            Assert.AreEqual(10, pop.GetPressure("cell:1:0").Receipts.Count); Assert.AreEqual(30, pop.TotalAccounted);
            var snap = pop.GetSaveSnapshot(); pop.SyncFromSave(snap);
            clock.Simulation.AdvanceUntil(clock.Simulation.Seconds + 1000, clock.Exposure);
            pop.ReportNoise("noise:1", "cell:1:0", 100); Assert.AreEqual(0, pop.GetPressure("cell:1:0").Pressure);
            pop.ReportNoise("noise:101", "cell:999:0", 1); Assert.AreEqual(100, pop.LastNoiseSequence);
            pop.ReportNoise("noise:101", "cell:1:0", -10); Assert.AreEqual(100, pop.LastNoiseSequence);
            yield return null;
        }
        [UnityTest] public IEnumerator MalformedRestoreDoesNotMutateLiveAuthority()
        {
            var snap = pop.GetSaveSnapshot(); snap.ledgers[0].logical = -1;
            Assert.Throws<InvalidOperationException>(() => pop.SyncFromSave(snap)); Assert.AreEqual(30, pop.TotalAccounted);
            yield return null;
        }
        [UnityTest] public IEnumerator StaleShotAndCellContinuationCannotMutateRestart()
        {
            long old = flow.Generation; cells.Request("cell:1:0"); yield return null;
            flow.ReturnToMenu(); flow.BeginSession(); pop.ReportShot(old);
            yield return null; yield return null;
            Assert.AreEqual(0, pop.LastNoiseSequence); Assert.AreEqual(0, pop.PhysicalCount); Assert.AreEqual(30, pop.TotalAccounted);
            Assert.AreEqual(CellState.Unloaded, cells.State("cell:1:0"));
        }
        [UnityTest] public IEnumerator NoActorBeforeReadyAndExplicitLogicalOnlyPolicy()
        {
            Assert.IsFalse(pop.TryMaterialize());
            yield return WorldCellAcceptanceRoute.Ready(cells, "cell:1:0"); Assert.IsTrue(cells.TryEnter("cell:1:0"));
            pop.MaterializationEnabled = false; Assert.IsFalse(pop.TryMaterialize()); Assert.AreEqual(0, pop.PhysicalCount);
        }
        [UnityTest] public IEnumerator WorldTimeProcessesArrivalAtBoundaryWithoutFrame()
        {
            pop.MaterializationEnabled = false;
            pop.ReportNoise("noise:1", "cell:1:0", 5); var trip = pop.Migrations[0];
            Assert.AreEqual(12, pop.GetLedger("cell:2:0").Logical); Assert.AreEqual(15, pop.GetLedger("cell:1:0").Logical);
            clock.Simulation.AdvanceUntil(trip.ArrivalTime - .01, clock.Exposure); Assert.AreEqual(1, pop.MigrationCount);
            clock.Simulation.AdvanceUntil(trip.ArrivalTime, clock.Exposure); Assert.AreEqual(0, pop.MigrationCount);
            Assert.AreEqual(18, pop.GetLedger("cell:1:0").Logical); Assert.AreEqual(30, pop.TotalAccounted);
            yield return null;
        }
    }
}
#endif
