using System;
using System.Diagnostics;
using System.IO;
using LastSignal.Noise;
using NUnit.Framework;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class R02NoiseTests
    {
        sealed class Listener : IGameplayNoiseListener
        {
            public ulong ListenerId { get; set; }
            public int Count;
            public Action Callback;
            public void ReceiveNoise(in GameplayNoiseEvent e) { Count++; Callback?.Invoke(); }
        }
        sealed class Pressure : IGameplayNoisePressureSink
        { public int Count; public bool Forward(in GameplayNoiseEvent e) { if (e.Category != GameplayNoiseCategory.Gunshot) return false; Count++; return true; } }
        double now; bool allowed; GameplayNoiseSystem noise; Pressure pressure;
        readonly GameplayNoiseTuning tuning = new GameplayNoiseTuning();
        const string Evidence = "Docs/Implementation/PreS010-Recovery/Evidence/R02/20260927-foundation/";
        [SetUp] public void Setup() { now = 100; allowed = true; pressure = new Pressure(); noise = new GameplayNoiseSystem(() => now, () => allowed, pressure); }
        GameplayNoiseRequest Request(GameplayNoiseCategory category = GameplayNoiseCategory.Gunshot, float radius = 10) =>
            new GameplayNoiseRequest(7, Vector3.zero, category, new GameplayNoiseProfile(radius, .5f, 2), 32);
        [Test] public void DomainPreservesSchemaAndRejectsDuplicatePermanently()
        {
            var listener = new Listener { ListenerId = 1 }; noise.Register(listener, Vector3.zero);
            Assert.IsTrue(noise.TryEmit(Request(), out var e)); Assert.AreEqual(7, e.SourceId); Assert.AreEqual(32, e.ActionId);
            Assert.AreEqual(now, e.SimulationTime); Assert.AreEqual(now + 2, e.ExpiresAt); Assert.AreEqual(.5f, e.Intensity);
            Assert.AreEqual(GameplayNoiseCategory.Gunshot, e.Category); Assert.IsFalse(noise.TryReceive(e));
            Assert.AreEqual(1, listener.Count); Assert.AreEqual(1, pressure.Count);
            for (int i = 0; i < 200; i++) Assert.IsTrue(noise.TryEmit(Request(), out _));
            Assert.AreEqual(64, noise.RecentCount); Assert.IsFalse(noise.TryReceive(e));
            now += 3; Assert.IsFalse(noise.TryReceive(e));
        }
        [TestCase(float.NaN, 1, 2)] [TestCase(-1, 1, 2)] [TestCase(129, 1, 2)]
        [TestCase(1, float.NaN, 2)] [TestCase(1, -1, 2)] [TestCase(1, 2, 2)]
        [TestCase(1, 1, -1)] [TestCase(1, 1, 0)] [TestCase(1, 1, 31)] [TestCase(1, 1, float.PositiveInfinity)]
        public void InvalidProfileFailsClosed(float radius, float intensity, float ttl)
        { Assert.IsFalse(noise.TryEmit(new GameplayNoiseRequest(1, Vector3.zero, GameplayNoiseCategory.Gunshot, new GameplayNoiseProfile(radius, intensity, ttl)), out _)); Assert.AreEqual(0, noise.RecentCount); }
        [Test] public void InvalidPositionSourceCategoryTimeAndInactiveRejected()
        {
            Assert.IsFalse(noise.TryEmit(new GameplayNoiseRequest(1, Vector3.one * float.NaN, GameplayNoiseCategory.Gunshot, tuning.Gunshot), out _));
            Assert.IsFalse(noise.TryEmit(new GameplayNoiseRequest(0, Vector3.zero, GameplayNoiseCategory.Gunshot, tuning.Gunshot), out _));
            Assert.IsFalse(noise.TryEmit(new GameplayNoiseRequest(1, Vector3.zero, (GameplayNoiseCategory)99, tuning.Gunshot), out _));
            now = double.NaN; Assert.IsFalse(noise.TryEmit(Request(), out _)); now = 100;
            var future = new GameplayNoiseEvent(new GameplayNoiseId(noise.Epoch, 1), Request(), 101); Assert.IsFalse(noise.TryReceive(future));
            var expired = new GameplayNoiseEvent(new GameplayNoiseId(noise.Epoch, 1), Request(), 97); Assert.IsFalse(noise.TryReceive(expired));
            allowed = false; Assert.IsFalse(noise.TryEmit(Request(), out _)); allowed = true; noise.End(); Assert.IsFalse(noise.TryEmit(Request(), out _));
        }
        [Test] public void SpatialNearEdgeOutsideVerticalAndUnrelatedBuckets()
        {
            var near = new Listener { ListenerId = 1 }; var edge = new Listener { ListenerId = 2 };
            var outside = new Listener { ListenerId = 3 }; var far = new Listener { ListenerId = 4 }; var above = new Listener { ListenerId = 5 };
            noise.Register(near, new Vector3(-1, 0, 0)); noise.Register(edge, new Vector3(10, 0, 0)); noise.Register(outside, new Vector3(10.01f, 0, 0));
            noise.Register(far, new Vector3(10000, 0, 0)); noise.Register(above, new Vector3(0, 11, 0));
            noise.TryEmit(Request(), out _);
            Assert.AreEqual(1, near.Count); Assert.AreEqual(1, edge.Count); Assert.AreEqual(0, outside.Count + far.Count + above.Count);
            Assert.AreEqual(4, noise.LastTrace.CandidateCount); Assert.AreEqual(4, noise.LastTrace.CellsVisited);
            Assert.IsFalse(noise.Register(near, Vector3.zero));
            noise.UpdatePosition(far, Vector3.zero); noise.UpdatePosition(near, new Vector3(-1000, 0, 0)); noise.TryEmit(Request(), out _);
            Assert.AreEqual(1, far.Count); Assert.AreEqual(1, near.Count); noise.Unregister(far); noise.TryEmit(Request(), out _); Assert.AreEqual(1, far.Count);
        }
        [Test] public void CallbackRemovalRegistrationAndReentrancyAreSafe()
        {
            var a = new Listener { ListenerId = 1 }; var b = new Listener { ListenerId = 2 }; var c = new Listener { ListenerId = 3 };
            noise.Register(a, Vector3.zero); noise.Register(b, Vector3.zero);
            a.Callback = () => { noise.Unregister(b); noise.Register(c, Vector3.zero); Assert.IsFalse(noise.TryEmit(Request(), out _)); };
            noise.TryEmit(Request(), out _); Assert.AreEqual(0, b.Count + c.Count);
            a.Callback = () => noise.End(); noise.TryEmit(Request(), out _);
            Assert.AreEqual(0, noise.ListenerCount); Assert.AreEqual(0, noise.RecentCount);
        }
        [Test] public void NewSessionCannotReplayOldEventsAndDoesNotRetainListeners()
        {
            noise.Register(new Listener { ListenerId = 1 }, Vector3.zero); noise.TryEmit(Request(), out var old); noise.End();
            var fresh = new GameplayNoiseSystem(() => now, () => true);
            Assert.IsFalse(fresh.TryReceive(old)); Assert.IsTrue(fresh.TryEmit(Request(), out var current)); Assert.AreNotEqual(old.EventId, current.EventId);
            Assert.AreEqual(0, noise.ListenerCount); Assert.AreEqual(0, noise.BucketCount);
        }
        [TestCase(20)] [TestCase(60)] [TestCase(120)]
        public void DistanceCadenceIsFrameIndependent(int fps)
        {
            var step = new FootstepNoiseProducer(noise, tuning, 1);
            for (int i = 0; i < fps * 10; i++) step.Advance(Vector3.right * (3.2f / fps), Vector3.zero, true, false, false);
            Assert.AreEqual(20, noise.AcceptedCount); Assert.AreEqual(0, pressure.Count);
        }
        [Test] public void GroundPauseStillTeleportAndCrouchPolicies()
        {
            var step = new FootstepNoiseProducer(noise, tuning, 1);
            for (int i = 0; i < 100; i++) { step.Advance(Vector3.zero, Vector3.zero, true, true, false); step.Advance(Vector3.right, Vector3.zero, false, true, false); }
            allowed = false; step.Advance(Vector3.right, Vector3.zero, true, true, false); allowed = true;
            step.Advance(Vector3.right * 10, Vector3.zero, true, true, false); Assert.AreEqual(0, noise.AcceptedCount);
            step.Advance(Vector3.right * 1.6f, Vector3.zero, true, true, false); Assert.AreEqual(GameplayNoiseCategory.SprintFootstep, noise.LastTrace.Event.Category);
            Assert.Greater(noise.LastTrace.Event.BaseRadiusMeters, tuning.Walk.RadiusMeters);
            step.Advance(Vector3.right * 1.6f, Vector3.zero, true, false, true); Assert.AreEqual(tuning.Crouch.RadiusMeters, noise.LastTrace.Event.BaseRadiusMeters);
        }
        [Test] public void IdenticalCommittedRequestIsIndependentOfPresentationVolume()
        {
            float original = AudioListener.volume;
            try
            {
                AudioListener.volume = 1; noise.TryEmit(Request(), out var full);
                AudioListener.volume = 0; noise.TryEmit(Request(), out var muted);
                Assert.AreEqual(full.SourceId, muted.SourceId); Assert.AreEqual(full.ActionId, muted.ActionId);
                Assert.AreEqual(full.Category, muted.Category); Assert.AreEqual(full.Position, muted.Position);
                Assert.AreEqual(full.BaseRadiusMeters, muted.BaseRadiusMeters); Assert.AreEqual(full.Intensity, muted.Intensity);
                Assert.AreEqual(full.SimulationTime, muted.SimulationTime); Assert.AreEqual(full.ExpiresAt, muted.ExpiresAt);
                Assert.AreNotEqual(full.EventId, muted.EventId);
            }
            finally { AudioListener.volume = original; }
        }
        [Test] public void CapacityAndMaximumRadiusBoundSpatialWork()
        {
            for (ulong i = 1; i <= GameplayNoiseSystem.MaxListeners; i++) Assert.IsTrue(noise.Register(new Listener { ListenerId = i }, Vector3.zero));
            Assert.IsFalse(noise.Register(new Listener { ListenerId = 9000 }, Vector3.zero));
            noise.TryEmit(Request(GameplayNoiseCategory.Footstep, 128), out _);
            Assert.AreEqual(289, noise.LastTrace.CellsVisited); Assert.AreEqual(4096, noise.LastTrace.DeliveryCount);
            noise.End(); Assert.AreEqual(0, noise.BucketCount);
        }
        [Test] public void EveryLocalOnlyCategorySkipsPressureAndLateRegistrationDoesNotReplay()
        {
            foreach (var category in new[] { GameplayNoiseCategory.Footstep, GameplayNoiseCategory.SprintFootstep, GameplayNoiseCategory.MeleeImpact })
                noise.TryEmit(Request(category), out _);
            var listener = new Listener { ListenerId = 1 }; noise.Register(listener, Vector3.zero);
            Assert.AreEqual(0, listener.Count); Assert.AreEqual(0, pressure.Count);
            noise.TryEmit(Request(), out _); Assert.AreEqual(1, listener.Count); Assert.AreEqual(1, pressure.Count);
        }
        [Test] public void WarmedMovementCadenceDoesNotAllocate()
        {
            var step = new FootstepNoiseProducer(noise, tuning, 1);
            for (int i = 0; i < 100; i++) step.Advance(Vector3.right * .05f, Vector3.zero, true, false, false);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) step.Advance(Vector3.right * .05f, Vector3.zero, true, false, false);
            long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            File.WriteAllText(Evidence + "footstep-performance.txt", $"10000 warmed movement substeps: ManagedBytes={bytes}\n");
            Assert.AreEqual(0, bytes);
        }
        [Test] public void WarmedSpatialStormMeasuresAllocationsAndBounds()
        {
            for (ulong i = 1; i <= 100; i++) noise.Register(new Listener { ListenerId = i }, i <= 10 ? Vector3.zero : Vector3.one * (1000 + i));
            var request = Request(GameplayNoiseCategory.Footstep);
            for (int i = 0; i < 100; i++) noise.TryEmit(request, out _);
            var timer = new Stopwatch(); long before = GC.GetAllocatedBytesForCurrentThread(); timer.Start();
            for (int i = 0; i < 1000; i++) noise.TryEmit(request, out _);
            timer.Stop(); long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Evidence + "performance.txt", $"100 listeners, 100 warmup, 1000 measured emissions\nTimeMs={timer.Elapsed.TotalMilliseconds:F4}\nManagedBytes={allocated}\nCandidatesPerEvent={noise.LastTrace.CandidateCount}\nCellsPerEvent={noise.LastTrace.CellsVisited}\nHistory={noise.RecentCount}/{GameplayNoiseSystem.HistoryCapacity}\n");
            Assert.AreEqual(10, noise.LastTrace.CandidateCount); Assert.AreEqual(64, noise.RecentCount); Assert.AreEqual(0, allocated);
        }
    }
}
