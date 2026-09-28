using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LastSignal.AI;
using LastSignal.Noise;
using LastSignal.WorldCells;
using LastSignal.WorldTime;
using LastSignal.Persistence;

namespace LastSignal.Tests
{
    public class R05SessionLifecyclePlayTests
    {
        SessionFlow flow;
        
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/WorldPopulationAcceptance.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            flow = UnityEngine.Object.FindAnyObjectByType<SessionFlow>();
            flow.Resume();
        }
        
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (flow) { flow.ReturnToMenu(); yield return null; }
            Time.timeScale = 1;
        }

        [UnityTest]
        public IEnumerator R05_Lifecycle_10_Session_Loops()
        {
            int maxZombies = 0;
            
            for (int i = 0; i < 10; i++)
            {
                Debug.Log($"Starting lifecycle loop {i + 1}/10");
                if (i > 0)
                {
                    flow.BeginSession();
                    yield return null;
                }
                
                var pop = flow.GetComponent<WorldPopulationManager>();
                var cells = flow.GetComponent<WorldCellManager>();
                var noise = flow.Noise;
                
                yield return WorldCellAcceptanceRoute.Ready(cells, "cell:1:0");
                cells.TryEnter("cell:1:0");
                pop.MaterializationEnabled = true;
                
                yield return new WaitForSeconds(0.2f);
                
                // Simulate gameplay
                var player = flow.Player;
                var weapon = player.GetComponent<PlayerCombatController>();
                Assert.That(weapon, Is.Not.Null);
                
                Assert.IsTrue(weapon.SelectSlot(PlayerCombatController.CombatSlot.Firearm));
                var rifle = weapon.Firearm;
                Assert.IsNotNull(rifle);
                rifle.RequestEquip();
                yield return new WaitForSeconds(0.5f);
                rifle.OnFireReleased(); rifle.OnFirePressed(); rifle.OnFireReleased(); // Real committed shot.
                yield return new WaitForSeconds(0.5f);
                
                var actors = UnityEngine.Object.FindObjectsByType<ZombieController>(FindObjectsSortMode.None);
                maxZombies = Mathf.Max(maxZombies, actors.Length);
                
                // Assert no duplicate registrations
                var noiseListeners = UnityEngine.Object.FindObjectsByType<ZombieNoiseListener>(FindObjectsSortMode.None);
                Assert.That(noiseListeners.Length, Is.EqualTo(actors.Length), "Each zombie must have exactly one noise listener");
                
                // Save and Load
                flow.GetComponent<SaveSession>().Configure(null, null, "r05-test-world");
                yield return flow.GetComponent<SaveSession>().Save();
                yield return flow.GetComponent<SaveSession>().Load();
                
                yield return new WaitForSeconds(0.2f);
                
                // Return to menu
                flow.ReturnToMenu();
                yield return null;
                
                // Verify teardown
                actors = UnityEngine.Object.FindObjectsByType<ZombieController>(FindObjectsSortMode.None);
                Assert.That(actors.Length, Is.EqualTo(0), "ReturnToMenu must destroy all zombies");
            }
        }
    }
}
