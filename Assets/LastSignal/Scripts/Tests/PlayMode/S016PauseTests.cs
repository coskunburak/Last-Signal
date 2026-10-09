#if UNITY_EDITOR
using System.Collections;
using LastSignal.Audio;
using LastSignal.Vehicles;
using LastSignal.WorldTime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LastSignal.Tests
{
    public sealed class S016PauseTests
    {
        SessionFlow flow;

        [UnitySetUp] public IEnumerator OpenProduction()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/LastSignal/Scenes/Production/S013Cabin.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            flow = Object.FindAnyObjectByType<SessionFlow>();
            Assert.That(flow, Is.Not.Null);
            flow.Resume();
            yield return null; yield return null;
        }

        [UnityTearDown] public IEnumerator CloseProduction()
        {
            if (flow) flow.ReturnToMenu();
            yield return null;
            var production = SceneManager.GetSceneByPath("Assets/LastSignal/Scenes/Production/S013Cabin.unity");
            if (production.IsValid() && production.isLoaded)
            {
                var empty = SceneManager.CreateScene("S016 pause cleanup");
                SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync(production);
            }
            flow = null;
            Time.timeScale = 1;
        }

        [UnityTest] public IEnumerator SoloPauseFreezesWorldAndThreatWhileSettingsStayInteractive()
        {
            var player = flow.Player;
            var input = player.GetComponent<PlayerInputReader>();
            var motor = player.GetComponent<FirstPersonMotor>();
            var health = player.GetComponent<PlayerHealth>();
            var interaction = player.GetComponent<InteractionController>();
            var clock = flow.GetComponent<WorldClock>();
            var zombie = flow.GetComponent<ZombieEncounter>().Actor;
            var vehicle = flow.GetComponent<VehicleWorld>().Actor;
            var settings = Object.FindAnyObjectByType<AudioSettingsView>();
            Assert.That(clock.Simulation, Is.Not.Null);
            Assert.That(zombie, Is.Not.Null);
            Assert.That(vehicle, Is.Not.Null);
            Assert.That(settings, Is.Not.Null);

            double movingTime = clock.Simulation.Seconds;
            yield return new WaitForSeconds(.08f);
            Assert.That(clock.Simulation.Seconds, Is.GreaterThan(movingTime), "Clock must advance before pause.");
            flow.Pause();
            yield return null;
            Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Pause));
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(input.GameplayActive, Is.False);
            Assert.That(vehicle.Ready, Is.False);
            Assert.That(interaction.TryInteract(), Is.False);
            flow.OpenSettings();
            yield return null;
            Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Settings));
            Assert.That(settings.settingsPanel.activeSelf, Is.True);
            Assert.That(settings.sliders[0].interactable, Is.True);

            double worldTime = clock.Simulation.Seconds;
            Vector3 position = player.transform.position;
            Vector3 zombiePosition = zombie.transform.position;
            float hp = health.CurrentHealth;
            var zombieState = zombie.Runtime.State;
            float attackTime = zombie.AttackTime;
            int contacts = zombie.ContactAttempts;
            zombie.Simulate(1f); // The explicit pause gate must reject even an offered step.
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(clock.Simulation.Seconds, Is.EqualTo(worldTime));
            Assert.That(player.transform.position, Is.EqualTo(position));
            Assert.That(motor.HorizontalMetersPerSecond, Is.Zero);
            Assert.That(health.CurrentHealth, Is.EqualTo(hp));
            Assert.That(zombie.Runtime.State, Is.EqualTo(zombieState));
            Assert.That(zombie.AttackTime, Is.EqualTo(attackTime));
            Assert.That(zombie.ContactAttempts, Is.EqualTo(contacts));
            Assert.That(zombie.transform.position, Is.EqualTo(zombiePosition));
            foreach (var wheel in vehicle.GetComponentsInChildren<WheelCollider>())
                Assert.That(wheel.motorTorque, Is.Zero);

            // Back from Settings returns to Pause; only the next resume enters gameplay.
            flow.Resume();
            yield return null;
            Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Pause));
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(input.GameplayActive, Is.False);
            Assert.That(settings.settingsPanel.activeSelf, Is.False);
            flow.Resume();
            yield return null; yield return null;
            Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Gameplay));
            Assert.That(input.GameplayActive, Is.True);
            Assert.That(settings.settingsPanel.activeSelf, Is.False);
            double resumed = clock.Simulation.Seconds;
            yield return new WaitForSeconds(.08f);
            Assert.That(clock.Simulation.Seconds, Is.GreaterThan(resumed));
        }
    }
}
#endif
