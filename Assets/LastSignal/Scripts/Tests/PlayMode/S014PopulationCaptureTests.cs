#if UNITY_EDITOR
using System.Collections;
using LastSignal.Slice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    public sealed class S014PopulationCaptureTests
    {
        SessionFlow flow;
        S014PopulationCapture fixture;
        [UnityTest] public IEnumerator OneProductionActorHasCleanLifecycle() => CheckFixture(1);
        [UnityTest] public IEnumerator TenProductionActorsHaveCleanLifecycle() => CheckFixture(10);
        [UnityTest] public IEnumerator TwentyProductionActorsHaveCleanLifecycle() => CheckFixture(20);
        IEnumerator CheckFixture(int count)
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/LastSignal/Scenes/Production/S013Cabin.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            flow = Object.FindAnyObjectByType<SessionFlow>(); flow.Resume();
            fixture = new GameObject("S014 fixture lifecycle test").AddComponent<S014PopulationCapture>();
            fixture.PrepareFixture(flow, count);
            Assert.AreEqual(count, fixture.ActiveActors.Count);
            Assert.AreEqual(count, Object.FindObjectsByType<ZombieController>(FindObjectsSortMode.None).Length);
            int listeners = flow.Noise.ListenerCount;
            foreach (var actor in fixture.ActiveActors)
            {
                Assert.IsTrue(actor.CanHear); Assert.IsTrue(actor.Navigation.Ready);
                Assert.IsTrue(actor.GetComponent<ZombieNoiseListener>().Registered);
                var animator = actor.GetComponentInChildren<Animator>();
                Assert.IsNotNull(animator); Assert.IsFalse(animator.applyRootMotion);
                Assert.IsTrue(animator.avatar.isValid); Assert.AreEqual(flow.Player, actor.Target);
            }
            yield return new WaitForSeconds(8f);
            int contacts = 0;
            foreach (var actor in fixture.ActiveActors) contacts += actor.ContactAttempts;
            Assert.Greater(contacts, 0, "Real attack/contact must remain active.");
            Assert.IsTrue(flow.Player.GetComponent<PlayerHealth>().isActiveAndEnabled);
            Assert.AreEqual(100, flow.Player.GetComponent<PlayerHealth>().CurrentHealth);
            foreach (var actor in fixture.ActiveActors) Assert.Greater(actor.GameTime, 0);
            fixture.ClearFixture(); fixture.ClearFixture(); // Idempotent teardown.
            yield return null; yield return null;
            Assert.AreEqual(0, fixture.ActiveActors.Count);
            Assert.AreEqual(0, Object.FindObjectsByType<ZombieController>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(listeners - count, flow.Noise.ListenerCount, "Fixture subscriptions leaked.");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (fixture) Object.Destroy(fixture.gameObject);
            if (flow) flow.ReturnToMenu();
            Time.timeScale = 1; yield return null;
        }
    }
}
#endif
