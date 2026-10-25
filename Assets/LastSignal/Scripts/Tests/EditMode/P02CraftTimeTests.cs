using System;
using LastSignal.Inventory;
using LastSignal.Shelter;
using LastSignal.WorldTime;
using NUnit.Framework;

namespace LastSignal.Tests
{
    public sealed class P02CraftTimeTests
    {
        [Test] public void PowerLossAtHalfPreservesProgressAndEscrowUntilRefuel()
        {
            using (var f = new P02CraftFixture(powered: true))
            {
                var fuel = new InventoryContainer(1); fuel.TryAdd(f.Fuel, 2); f.Production.Refuel(fuel); f.Production.ToggleGenerator();
                f.Production.StartCraft(f.Request(), f.Source); f.Production.ApplyElapsed(0, 5);
                Assert.AreEqual(.5, f.Production.Progress); Assert.AreEqual(0, f.Production.FuelSeconds);
                f.Production.ApplyElapsed(5, 5000); Assert.AreEqual(.5, f.Production.Progress);
                Assert.AreEqual(2, P02CraftFixture.Count(f.Production.Capture().crafting.job.escrow, f.A));
                f.Production.Refuel(fuel); f.Production.ApplyElapsed(5000, 5005);
                Assert.AreEqual(CraftStatus.CompletedWaitingOutput, f.Production.Status); Assert.IsEmpty(f.Production.Capture().crafting.job.escrow);
                Assert.AreEqual(3, P02CraftFixture.Count(f.Production.Capture().crafting.job.pendingOutput, f.Output));
            }
        }
        [Test] public void SimulationJobDoesNotAdvanceDuringWorldSleepOrCatchup()
        {
            using (var f = new P02CraftFixture(handcraft: true))
            {
                f.Production.StartCraft(f.Request(), f.Source); f.Production.ApplyElapsed(0, 7200); Assert.AreEqual(0, f.Production.Progress);
                f.Production.ApplySimulationElapsed(0, 4); Assert.AreEqual(.4, f.Production.Progress);
                f.Production.ApplyElapsed(7200, 10000); Assert.AreEqual(.4, f.Production.Progress);
                var snapshot = f.Production.Capture(); var restored = f.Create(10000); restored.Restore(snapshot, 10000);
                restored.ApplySimulationElapsed(4, 10); Assert.AreEqual(CraftStatus.CompletedWaitingOutput, restored.Status);
                Assert.Throws<InvalidOperationException>(() => restored.ApplySimulationElapsed(4, 10));
            }
        }
        [Test] public void CanonicalWorldSimulationPauseSleepAndLargeSkipCompleteOnce()
        {
            using (var f = new P02CraftFixture())
            {
                var clock = new WorldSimulation(new WorldTimeSettings());
                f.Production = f.Create(clock.Seconds); clock.Register(f.Production); f.Production.StartCraft(f.Request(), f.Source);
                clock.TickRealSeconds(100, true, 0); Assert.AreEqual(0, f.Production.Progress);
                clock.AdvanceUntil(clock.Seconds + 5, 0); Assert.AreEqual(.5, f.Production.Progress);
                clock.TickRealSeconds(100, true, 0); Assert.AreEqual(.5, f.Production.Progress);
                clock.AdvanceUntil(clock.Seconds + 7200, 0); Assert.AreEqual(1, f.Production.Progress);
                var snapshot = f.Production.Capture(); Assert.AreEqual(3, P02CraftFixture.Count(snapshot.crafting.job.pendingOutput, f.Output));
                Assert.Throws<InvalidOperationException>(() => f.Production.ApplyElapsed(snapshot.lastProcessed - 1, snapshot.lastProcessed));
            }
        }
        [Test] public void CancelledJobNeverProgressesOrConvertsItsRefund()
        {
            using (var f = new P02CraftFixture())
            {
                f.Production.StartCraft(f.Request(), f.Source); f.Production.ApplyElapsed(0, 5); f.Production.Cancel(); f.Production.ApplyElapsed(5, 10000);
                Assert.AreEqual(.5, f.Production.Progress); var job = f.Production.Capture().crafting.job;
                Assert.AreEqual(CraftStatus.CancelledWaitingRefund, job.status); Assert.AreEqual(2, P02CraftFixture.Count(job.escrow, f.A)); Assert.IsEmpty(job.pendingOutput);
            }
        }
        [TestCase(30)] [TestCase(60)] [TestCase(120)]
        public void SimulationDurationIsIndependentOfFramePartition(int frames)
        {
            using (var f = new P02CraftFixture(handcraft: true))
            {
                f.Production.StartCraft(f.Request(), f.Source);
                for (int i = 1; i <= frames; i++) f.Production.ApplySimulationElapsed((i - 1) * 10.0 / frames, i * 10.0 / frames);
                Assert.That(f.Production.Progress, Is.EqualTo(1).Within(1e-12));
                Assert.AreEqual(CraftStatus.CompletedWaitingOutput, f.Production.Status);
            }
        }
    }
}
