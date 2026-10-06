using LastSignal.Noise;
using LastSignal.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class VehicleNoiseTests
    {
        sealed class Listener : IGameplayNoiseListener
        { public ulong ListenerId => 9; public int Count; public void ReceiveNoise(in GameplayNoiseEvent noise) => Count++; }
        sealed class Sink : IGameplayNoisePressureSink
        { public int Count; public bool Forward(in GameplayNoiseEvent noise) { Count++; return true; } }
        [TestCase(20)] [TestCase(60)] [TestCase(120)]
        public void VEH_NOISE_001_EquivalentEngineCadence(int fps)
        {
            double time = 0; var sink = new Sink(); var bus = new GameplayNoiseSystem(() => time, () => true, sink);
            var producer = new VehicleNoiseProducer(bus, 2, new VehicleTuning());
            for (int i = 0; i < fps * 60; i++) { time += 1.0 / fps; producer.Advance(1.0 / fps, true, .5f, Vector3.zero); }
            Assert.AreEqual(60, bus.AcceptedCount); Assert.AreEqual(60, sink.Count);
            bus.End();
        }
        [Test] public void VEH_NOISE_002_004_HornUsesExistingHearingIndependentOfAudioVolume()
        {
            double time = 0; var sink = new Sink(); var bus = new GameplayNoiseSystem(() => time, () => true, sink);
            var listener = new Listener(); bus.Register(listener, Vector3.right);
            var producer = new VehicleNoiseProducer(bus, 2, new VehicleTuning()); float old = AudioListener.volume;
            try
            {
                AudioListener.volume = 1; Assert.IsTrue(producer.Horn(Vector3.zero)); var first = bus.LastTrace.Event;
                Assert.IsFalse(producer.Horn(Vector3.zero)); time = .5; producer.Advance(.5, false, 0, Vector3.zero);
                AudioListener.volume = 0; Assert.IsTrue(producer.Horn(Vector3.zero));
                Assert.AreEqual(first.BaseRadiusMeters, bus.LastTrace.Event.BaseRadiusMeters); Assert.AreEqual(2, listener.Count); Assert.AreEqual(2, sink.Count);
            }
            finally { AudioListener.volume = old; bus.End(); }
        }
        [Test] public void ImpactReceiptCannotReplayAfterResetOrLaterImpacts()
        {
            var bus = new GameplayNoiseSystem(() => 0, () => true);
            var producer = new VehicleNoiseProducer(bus, 2, new VehicleTuning());
            Assert.IsTrue(producer.Impact(Vector3.zero, 1));
            Assert.IsFalse(producer.Impact(Vector3.zero, 1));
            Assert.IsTrue(producer.Impact(Vector3.zero, 2)); producer.Reset();
            Assert.IsFalse(producer.Impact(Vector3.zero, 1));
            Assert.IsFalse(producer.Impact(Vector3.zero, 2));
            Assert.AreEqual(2, bus.AcceptedCount); bus.End();
        }
        [Test] public void PausedAndHitchCadenceCannotBurst()
        {
            double time = 0; bool allowed = true; var bus = new GameplayNoiseSystem(() => time, () => allowed);
            var producer = new VehicleNoiseProducer(bus, 2, new VehicleTuning());
            time = 60; producer.Advance(60, true, 1, Vector3.zero); Assert.AreEqual(1, bus.AcceptedCount);
            allowed = false; producer.Advance(100, true, 1, Vector3.zero); Assert.IsFalse(producer.Horn(Vector3.zero));
            allowed = true; producer.Advance(.5, true, 1, Vector3.zero); Assert.AreEqual(1, bus.AcceptedCount);
            producer.Advance(.5, true, 1, Vector3.zero); Assert.AreEqual(2, bus.AcceptedCount); bus.End();
        }
    }
}
