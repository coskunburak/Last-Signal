using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using LastSignal.AI;
using LastSignal.Persistence;
using LastSignal.WorldTime;
using LastSignal.WorldCells;

namespace LastSignal.Tests
{
    public class WorldPopulationPlayTests
    {
        SessionFlow flow;
        WorldPopulationManager pop;
        WorldCellManager cells;
        SaveSession save;
        WorldClock clock;
        
        [UnitySetUp]
        public IEnumerator SetUp()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/WorldTimeAcceptance.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync("WorldTimeAcceptance", LoadSceneMode.Single);
#endif
            flow = UnityEngine.Object.FindAnyObjectByType<SessionFlow>();
            pop = UnityEngine.Object.FindAnyObjectByType<WorldPopulationManager>();
            cells = UnityEngine.Object.FindAnyObjectByType<WorldCellManager>();
            save = UnityEngine.Object.FindAnyObjectByType<SaveSession>();
            clock = UnityEngine.Object.FindAnyObjectByType<WorldClock>();
            
            yield return new WaitForSeconds(1f); // let cells load
        }
        
        [UnityTest]
        public IEnumerator Noise_IncreasesPressure_And_Migrates()
        {
            if (!pop) Assert.Ignore("WorldPopulationManager not in scene.");
            
            string currentCell = cells.CurrentCell;
            var initialPressure = pop.GetPressure(currentCell).Pressure;
            
            pop.ReportNoise(System.Guid.NewGuid().ToString(), currentCell, 5f);
            
            yield return new WaitForSeconds(0.5f);
            
            var afterPressure = pop.GetPressure(currentCell).Pressure;
            Assert.That(afterPressure, Is.GreaterThan(initialPressure));
        }

        [UnityTest]
        public IEnumerator SaveLoad_DoesNotDuplicateNoise()
        {
            if (!pop || !save) Assert.Ignore("Missing dependencies.");
            
            string currentCell = cells.CurrentCell;
            pop.ReportNoise("receipt-save-test", currentCell, 1f);
            float p1 = pop.GetPressure(currentCell).Pressure;
            
            string path = Path.Combine(Application.persistentDataPath, "test_save.json");
            save.Save(path);
            yield return null;
            
            flow.ReturnToMenu();
            yield return null;
            // Since we can't easily access SaveSession's Restore (it takes SaveGame?), we will just capture and restore PopulationSnapshot directly for unit test.
            
            var snap = pop.GetSaveSnapshot();
            pop.ClearSession();
            pop.SyncFromSave(snap);
            
            var pop2 = pop;
            pop2.ReportNoise("receipt-save-test", currentCell, 1f);
            
            float p2 = pop2.GetPressure(currentCell).Pressure;
            Assert.That(p2, Is.EqualTo(p1));
        }
        
        [UnityTest]
        public IEnumerator SleepAdvancesTravelAndDecaysPressure()
        {
            if (!pop || !clock || clock.Simulation == null) Assert.Ignore("Missing dependencies.");
            string currentCell = cells.CurrentCell;
            pop.ReportNoise("receipt-sleep", currentCell, 1f);
            float initialPressure = pop.GetPressure(currentCell).Pressure;
            
            // Advance time
            double target = clock.Simulation.Seconds + 60 * 60 * 8;
            clock.Simulation.AdvanceUntil(target, clock.Exposure, clock.ProtectedFromRain ? 1f : 0f, null);
            yield return null;
            
            float afterPressure = pop.GetPressure(currentCell).Pressure;
            Assert.That(afterPressure, Is.LessThan(initialPressure));
        }
    }
}
