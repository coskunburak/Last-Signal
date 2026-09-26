using System;
using System.Collections.Generic;
using LastSignal.AI;
using LastSignal.Persistence;
using NUnit.Framework;

namespace LastSignal.Tests
{
    public class WorldPopulationManagerTests
    {
        static readonly HashSet<string> Cells = new HashSet<string> { "cell:1:0", "cell:2:0" };
        static PopulationSnapshot Valid() => new PopulationSnapshot {
            lastNoiseSequence = 1,
            pressures = new[] {
                new CellPressureSnapshot { cellId = "cell:1:0", pressure = .8f, lastUpdateTime = 100, receipts = new[] { new NoiseReceiptSnapshot { id = "noise:1" } } },
                new CellPressureSnapshot { cellId = "cell:2:0", receipts = Array.Empty<NoiseReceiptSnapshot>() } },
            ledgers = new[] { new PopulationLedgerSnapshot { cellId = "cell:1:0", logical = 15 }, new PopulationLedgerSnapshot { cellId = "cell:2:0", logical = 12 } },
            migrations = new[] { new MigrationGroupSnapshot { groupId = "group", sourceCellId = "cell:2:0", targetCellId = "cell:1:0", departureTime = 100, arrivalTime = 220, size = 3 } },
            actors = Array.Empty<PopulationActorSnapshot>() };
        [Test] public void NormalAndBulkDecayAgree()
        {
            var normal = new CellPressureState { Pressure = 1, LastUpdateTime = 100 };
            var bulk = new CellPressureState { Pressure = 1, LastUpdateTime = 100 };
            for (int i = 1; i <= 100; i++) normal.ApplyDecay(100 + i, .001f);
            bulk.ApplyDecay(200, .001f);
            Assert.That(normal.Pressure, Is.EqualTo(bulk.Pressure).Within(.00001));
        }
        [Test] public void LargeElapsedClampsAtZeroWithoutOverflow()
        { var p = new CellPressureState { Pressure = 1 }; p.ApplyDecay(1e12, .001f); Assert.AreEqual(0, p.Pressure); Assert.AreEqual(1e12, p.LastUpdateTime); }
        [Test] public void BackwardTimestampCannotUndoDecay()
        { var p = new CellPressureState { Pressure = .5f, LastUpdateTime = 100 }; p.ApplyDecay(99, .001f); Assert.AreEqual(.5f, p.Pressure); Assert.AreEqual(100, p.LastUpdateTime); }
        [Test] public void MidTravelSnapshotIsValid() => Assert.IsTrue(SaveValidation.ValidPopulation(Valid(), Cells, 150));
        [TestCase(-1f)] [TestCase(1.01f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidPressureRejected(float pressure)
        { var p = Valid(); p.pressures[0].pressure = pressure; Assert.IsFalse(SaveValidation.ValidPopulation(p, Cells, 150)); }
        [Test] public void DuplicateLedgerRejected()
        { var p = Valid(); p.ledgers[1].cellId = p.ledgers[0].cellId; Assert.IsFalse(SaveValidation.ValidPopulation(p, Cells, 150)); }
        [Test] public void UnknownDestinationRejected()
        { var p = Valid(); p.migrations[0].targetCellId = "cell:99:0"; Assert.IsFalse(SaveValidation.ValidPopulation(p, Cells, 150)); }
        [Test] public void DuplicateMigrationRejected()
        { var p = Valid(); p.migrations = new[] { p.migrations[0], p.migrations[0] }; Assert.IsFalse(SaveValidation.ValidPopulation(p, Cells, 150)); }
        [Test] public void InstantTravelRejected()
        { var p = Valid(); p.migrations[0].arrivalTime = 100; Assert.IsFalse(SaveValidation.ValidPopulation(p, Cells, 150)); }
        [Test] public void FutureDecayTimestampRejected()
        { var p = Valid(); p.pressures[0].lastUpdateTime = 151; Assert.IsFalse(SaveValidation.ValidPopulation(p, Cells, 150)); }
        [Test] public void NegativeLedgerRejected()
        { var p = Valid(); p.ledgers[0].logical = -1; Assert.IsFalse(SaveValidation.ValidPopulation(p, Cells, 150)); }
        [Test] public void ExcessiveReceiptsRejected()
        { var p = Valid(); p.pressures[0].receipts = new NoiseReceiptSnapshot[65]; Assert.IsFalse(SaveValidation.ValidPopulation(p, Cells, 150)); }
        [Test] public void ReceiptAboveWatermarkRejected()
        { var p = Valid(); p.lastNoiseSequence = 0; Assert.IsFalse(SaveValidation.ValidPopulation(p, Cells, 150)); }
        [Test] public void DuplicateAndDeadDormantIdentityRejected()
        {
            var p = Valid(); var actor = new PopulationActorSnapshot { id = "unit", cellId = "cell:1:0", health = 50 };
            p.actors = new[] { actor }; Assert.IsTrue(SaveValidation.ValidPopulation(p, Cells, 150));
            p.actors = new[] { actor, actor }; Assert.IsFalse(SaveValidation.ValidPopulation(p, Cells, 150));
            p.actors = new[] { actor }; actor.health = 0; Assert.IsFalse(SaveValidation.ValidPopulation(p, Cells, 150));
        }
    }
}
