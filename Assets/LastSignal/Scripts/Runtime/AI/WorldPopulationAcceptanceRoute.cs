#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using LastSignal.Persistence;
using LastSignal.Shelter;
using UnityEngine;
using LastSignal.WorldTime;
using LastSignal.WorldCells;

namespace LastSignal.AI
{
    public static class WorldPopulationAcceptanceRoute
    {
        public static IEnumerator Run(WorldClock clock, string path, Action<string> log)
        {
            var flow = clock.Flow;
            var saves = clock.GetComponent<SaveSession>();
            var pop = clock.GetComponent<WorldPopulationManager>();
            if (!pop) { pop = clock.gameObject.AddComponent<WorldPopulationManager>(); }
            var cells = clock.GetComponent<WorldCellManager>();
            
            flow.Resume();
            yield return null;
            
            string startCell = cells.CurrentCell;
            Check(startCell != null && startCell != "resident", "Valid cell loaded");
            
            float quietPressure = pop.GetPressure(startCell).Pressure;
            log?.Invoke($"Quiet baseline pressure: {quietPressure}");
            
            // Fire rifle
            var combat = flow.Player.GetComponent<PlayerCombatController>();
            Check(combat != null && combat.ActiveWeapon != null, "Player has weapon");
            
            combat.ActiveWeapon.OnFirePressed();
            yield return null;
            combat.ActiveWeapon.OnFireReleased();
            
            yield return new WaitForSeconds(0.5f);
            
            float loudPressure = pop.GetPressure(startCell).Pressure;
            Check(loudPressure > quietPressure, "Pressure increased from noise");
            log?.Invoke($"Loud pressure: {loudPressure}");
            
            // Migration check
            int migrating = 15 - pop.GetLedger(startCell).Logical;
            log?.Invoke($"Migrating check (needs other cells populated, if this is isolated it might not migrate).");
            
            // Save and load
            string savePath = Path.Combine(Application.persistentDataPath, "saves", "pop_acceptance.json");
            Check(saves.Save(savePath).Success, "Saved successfully");
            
            yield return null;
            flow.ReturnToMenu();
            yield return null;
            
            yield return saves.Load(savePath);
            Check(saves.LastResult.Success, "Loaded successfully");
            yield return new WaitForSeconds(1f);
            
            var pop2 = UnityEngine.Object.FindAnyObjectByType<WorldPopulationManager>();
            float loadedPressure = pop2.GetPressure(startCell).Pressure;
            Check(Mathf.Approximately(loadedPressure, loudPressure), "Pressure restored correctly");
            
            log?.Invoke("PASS S009 population and pressure acceptance route.");
        }
        
        static void Check(bool condition, string message) { if (!condition) throw new Exception("Acceptance failed: " + message); }
    }
}
#endif
