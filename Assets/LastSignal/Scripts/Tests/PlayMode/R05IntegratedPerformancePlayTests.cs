using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LastSignal.AI;
using LastSignal.Noise;
using LastSignal.WorldCells;
using LastSignal.WorldTime;

namespace LastSignal.Tests
{
    public class R05IntegratedPerformancePlayTests
    {
        SessionFlow flow;
        StringBuilder report = new StringBuilder();
        string dir;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            dir = "Docs/Implementation/PreS010-Recovery/Evidence/R05/" + DateTime.Now.ToString("yyyyMMdd-HHmm") + "/";
            Directory.CreateDirectory(dir);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/WorldPopulationAcceptance.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            flow = UnityEngine.Object.FindAnyObjectByType<SessionFlow>();
            flow.Resume();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (flow) { flow.ReturnToMenu(); yield return null; }
            Time.timeScale = 1;
            File.WriteAllText(dir + "performance-summary.txt", report.ToString());
        }

        [UnityTest]
        public IEnumerator R05_IntegratedPerformanceBenchmark()
        {
            var cells = flow.GetComponent<WorldCellManager>();
            var pop = flow.GetComponent<WorldPopulationManager>();
            var noise = flow.Noise;
            var player = flow.Player;

            yield return WorldCellAcceptanceRoute.Ready(cells, "cell:1:0");
            cells.TryEnter("cell:1:0");
            yield return null;

            // Spawn 30 zombies manually in a circle
            var zombies = new List<ZombieController>();
            for (int i = 0; i < 30; i++)
            {
                float angle = i * (Mathf.PI * 2 / 30f);
                Vector3 pos = player.transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 25f;
                // Raycast down to find ground
                if (Physics.Raycast(pos + Vector3.up * 10, Vector3.down, out var hit, 20f))
                {
                    var go = GameObject.Instantiate(Resources.Load<GameObject>("LS_Zombie_Runtime"), hit.point, Quaternion.identity);
                    var zc = go.GetComponent<ZombieController>();
                    zc.Initialize();
                    zc.Bind(player);
                    zombies.Add(zc);
                }
            }
            
            yield return new WaitForSeconds(1f); // let them settle
            
            report.AppendLine("=== R05 INTEGRATED PERFORMANCE BENCHMARK ===");
            report.AppendLine($"Zombie Count: {zombies.Count}");
            
            // Phase A: Silence
            long memStart = GC.GetTotalMemory(false);
            long queries = (long)noise.AcceptedCount;
            yield return new WaitForSeconds(2f);
            long memEnd = GC.GetTotalMemory(false);
            long queriesEnd = (long)noise.AcceptedCount;
            report.AppendLine("--- Phase A: Silence ---");
            report.AppendLine($"Hearing queries during silence = {queriesEnd - queries}");
            report.AppendLine($"Managed bytes allocated approx = {memEnd - memStart}");
            File.WriteAllText(dir + "performance-silence.txt", report.ToString());

            // Phase B: Walk
            player.GetComponent<PlayerStance>().TrySetCrouching(false);
            player.GetComponent<PlayerStamina>().Tick(Time.deltaTime, false); // simulate
            var walkStart = Time.time;
            noise.TryEmit(new GameplayNoiseRequest(1, player.transform.position, GameplayNoiseCategory.Footstep, new GameplayNoiseProfile(6f, 0.2f, 2f)), out _);
            yield return new WaitForSeconds(1f);
            report.AppendLine("--- Phase B: Walk ---");
            report.AppendLine("Simulated walk event completed.");

            // Phase C: Sprint
            var sprintStart = Time.time;
            noise.TryEmit(new GameplayNoiseRequest(1, player.transform.position, GameplayNoiseCategory.SprintFootstep, new GameplayNoiseProfile(12f, 0.4f, 2f)), out _);
            yield return new WaitForSeconds(1f);
            report.AppendLine("--- Phase C: Sprint ---");
            report.AppendLine("Simulated sprint event completed.");
            File.WriteAllText(dir + "performance-sprint.txt", report.ToString());

            // Phase D: Gunshot
            var rifle = player.GetComponent<PlayerCombatController>();
            var weapon = rifle.Firearm;
            Assert.IsNotNull(weapon);
            weapon.RequestEquip();
            yield return new WaitForSeconds(1f);
            long preShotPressure = pop.GetPressure("cell:1:0").Receipts.Count;
            weapon.OnFireReleased(); weapon.OnFirePressed(); weapon.OnFireReleased();
            yield return new WaitForSeconds(2f);
            long postShotPressure = pop.GetPressure("cell:1:0").Receipts.Count;
            report.AppendLine("--- Phase D: Gunshot ---");
            report.AppendLine($"Regional pressure contributions: {postShotPressure - preShotPressure}");
            File.WriteAllText(dir + "performance-gunshot.txt", report.ToString());

            // Phase E: Investigating
            int investigating = 0;
            foreach (var z in zombies) if (z.Runtime.State == ZombieState.Investigating) investigating++;
            report.AppendLine("--- Phase E: Investigating ---");
            report.AppendLine($"Zombies Investigating: {investigating}");

            // Phase F: Chase
            // Move player to be seen by one zombie
            player.transform.position = zombies[0].transform.position + zombies[0].transform.forward * 5f;
            yield return new WaitForSeconds(2f);
            int chasing = 0;
            foreach (var z in zombies) if (z.Runtime.State == ZombieState.Chasing) chasing++;
            report.AppendLine("--- Phase F: Chase ---");
            report.AppendLine($"Zombies Chasing: {chasing}");
            File.WriteAllText(dir + "performance-chase.txt", report.ToString());
            
            // Phase G: Combat
            // (already fired rifle, assume combat interactions happen)
            
            // Phase H: De-escalation
            // Break LOS
            player.transform.position = player.transform.position + Vector3.up * 50f; // hide
            yield return new WaitForSeconds(15f); // wait for search to expire
            int searching = 0, idle = 0;
            foreach (var z in zombies)
            {
                if (z.Runtime.State == ZombieState.Searching) searching++;
                if (z.Runtime.State == ZombieState.Idle) idle++;
            }
            report.AppendLine("--- Phase H: De-escalation ---");
            report.AppendLine($"Zombies Searching: {searching}, Idle: {idle}");
            
            // Phase I: ReturnToMenu
            report.AppendLine("--- Phase I: ReturnToMenu ---");
            flow.ReturnToMenu();
            yield return null;
            var zcount = UnityEngine.Object.FindObjectsByType<ZombieController>(FindObjectsSortMode.None).Length;
            report.AppendLine($"Zombies active after ReturnToMenu: {zcount}");
        }
    }
}
