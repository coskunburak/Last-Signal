#if UNITY_EDITOR
using System.Collections;
using LastSignal.Noise;
using LastSignal.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public sealed class VehicleZombieHearingTests
    {
        SessionFlow flow;
        [UnityTest] public IEnumerator EngineHornAndImpactUseExistingListenerAndDeathReleasesIt()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/LastSignal/Scenes/Production/S013Cabin.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            flow = Object.FindAnyObjectByType<SessionFlow>(); flow.Resume();
            var actor = Object.FindAnyObjectByType<ZombieEncounter>().Actor;
            Assert.IsNotNull(actor); Assert.IsFalse(actor.IsDead);
            var listener = actor.GetComponent<ZombieNoiseListener>(); Assert.IsTrue(listener.Registered);
            var producer = new VehicleNoiseProducer(flow.Noise, 9001, new VehicleTuning());
            var position = actor.GetComponent<ZombiePerception>().Origin;
            int heard = listener.Heard;
            producer.Advance(1, true, 1, position);
            Assert.AreEqual(heard + 1, listener.Heard); Assert.AreEqual(GameplayNoiseCategory.VehicleEngine, listener.LastHeard.Event.Category);
            Assert.IsTrue(producer.Horn(position));
            Assert.AreEqual(heard + 2, listener.Heard); Assert.AreEqual(GameplayNoiseCategory.VehicleHorn, listener.LastHeard.Event.Category);
            Assert.IsTrue(producer.Impact(position, 1));
            Assert.AreEqual(heard + 3, listener.Heard); Assert.AreEqual(GameplayNoiseCategory.VehicleImpact, listener.LastHeard.Event.Category);
            Assert.IsFalse(producer.Impact(position, 1)); Assert.AreEqual(heard + 3, listener.Heard);
            actor.GetComponent<ZombieHealth>().TakeDamage(new DamageInfo { Amount = 1000 });
            Assert.IsTrue(actor.IsDead); Assert.IsFalse(listener.Registered);
            Assert.IsTrue(producer.Impact(position, 2)); Assert.AreEqual(heard + 3, listener.Heard);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { if (flow) flow.ReturnToMenu(); Time.timeScale = 1; yield return null; }
    }
}
#endif
