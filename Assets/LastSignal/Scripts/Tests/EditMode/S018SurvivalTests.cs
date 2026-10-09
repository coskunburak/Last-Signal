using System;
using System.Collections.Generic;
using LastSignal.Persistence;
using NUnit.Framework;

namespace LastSignal.Tests
{
    public sealed class S018SurvivalTests
    {
        static SaveCodec Codec() => new SaveCodec(new SaveValidation("world.fixture", "content.1",
            new Dictionary<string, int> { { "ammo.rifle", 60 }, { "medical.bandage", 10 }, { PlayerSurvival.BackpackId, 1 } }, "rifle.1", 30));
        [Test] public void TwelveWorldHoursDrainWaterWithoutWallClock()
        { var state = new SurvivalState(); Assert.That(state.Advance(43200), Is.EqualTo(0).Within(1e-8)); Assert.That(state.Hydration, Is.EqualTo(0).Within(1e-8)); Assert.That(state.Nutrition, Is.EqualTo(50).Within(1e-8)); }
        [Test] public void LargeAndSmallIntervalsCrossThresholdsEqually()
        {
            var a = new SurvivalState(); var b = new SurvivalState(); a.AddWound(); b.AddWound();
            double one = a.Advance(90000), many = 0;
            for (int i = 0; i < 1500; i++) many += b.Advance(60);
            Assert.That(many, Is.EqualTo(one).Within(.00001));
            Assert.That(b.Hydration, Is.EqualTo(a.Hydration).Within(.00001));
            Assert.That(b.Nutrition, Is.EqualTo(a.Nutrition).Within(.00001));
        }
        [Test] public void ZeroElapsedDoesNotDepleteResources()
        { var s = new SurvivalState(); s.AddWound(); Assert.AreEqual(0, s.Advance(0)); Assert.AreEqual(100, s.Hydration); }
        [Test] public void ConsumptionCapsAndRejectsInvalidAmounts()
        {
            var s = new SurvivalState(); s.Advance(10000); s.Drink(100); s.Eat(100);
            Assert.AreEqual(100, s.Hydration); Assert.AreEqual(100, s.Nutrition);
            Assert.Throws<ArgumentOutOfRangeException>(() => s.Drink(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => s.Eat(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => s.Advance(double.PositiveInfinity));
        }
        [Test] public void NewWoundInvalidatesOldTreatmentAndBleedingIsCapped()
        {
            var s = new SurvivalState(); s.AddWound(); long revision = s.WoundRevision;
            Assert.IsTrue(s.CanTreat(revision)); s.AddWound(); Assert.IsFalse(s.CanTreat(revision));
            for (int i = 0; i < 5; i++) s.AddWound(); Assert.AreEqual(3, s.Bleeding);
            revision = s.WoundRevision; s.Treat(); Assert.AreEqual(0, s.Bleeding); Assert.IsFalse(s.CanTreat(revision));
        }
        [Test] public void TreatmentDoesNotRestoreNutritionOrHydration()
        { var s = new SurvivalState(); s.Advance(1000); var water = s.Hydration; s.AddWound(); s.Treat(); Assert.AreEqual(water, s.Hydration); }
        [Test] public void LegacySaveHasNoInventedEquippedPack()
        { var c = Codec(); Assert.IsTrue(c.Encode(SaveFoundationTests.Fixture(), out var json).Success); Assert.IsTrue(c.Decode(json, out var s).Success); Assert.IsNull(s.survival); }
        [Test] public void SurvivalExtensionRoundtripsWithoutApplyingOfflineTime()
        {
            var s = SaveFoundationTests.Fixture(); var state = new SurvivalState(); state.Advance(7200); state.AddWound();
            s.header.survivalVersion = 1; s.survival = state.Capture(s.inventory.capacity, null);
            var c = Codec(); Assert.IsTrue(c.Encode(s, out var json).Success); Assert.IsTrue(c.Decode(json, out var copy).Success);
            var restored = new SurvivalState(); restored.Restore(copy.survival);
            Assert.AreEqual(state.Hydration, restored.Hydration); Assert.AreEqual(1, restored.Bleeding); Assert.AreEqual(state.WoundRevision, restored.WoundRevision);
        }
        [Test] public void MissingRequiredSurvivalSectionFails()
        { var s = SaveFoundationTests.Fixture(); s.header.survivalVersion = 1; Assert.IsFalse(Codec().Encode(s, out _).Success); }
        [Test] public void NonfiniteAndOutOfRangeSaveFails()
        {
            var s = SaveFoundationTests.Fixture(); s.header.survivalVersion = 1; s.survival = new SurvivalState().Capture(3, null);
            s.survival.hydration = double.NaN; Assert.IsFalse(Codec().Encode(s, out _).Success);
            s.survival.hydration = 100; s.survival.bleeding = 4; Assert.IsFalse(Codec().Encode(s, out _).Success);
        }
        [Test] public void BackpackCannotClaimCapacityWithoutMatchingInventory()
        {
            var s = SaveFoundationTests.Fixture(); s.header.survivalVersion = 1;
            s.survival = new SurvivalState().Capture(3, PlayerSurvival.BackpackId);
            Assert.IsFalse(Codec().Encode(s, out _).Success);
        }
        [Test] public void UnknownEquipmentIsNotSilentlyDropped()
        {
            var s = SaveFoundationTests.Fixture(); s.header.survivalVersion = 1;
            s.survival = new SurvivalState().Capture(3, "unknown.pack"); Assert.IsFalse(Codec().Encode(s, out _).Success);
        }
        [Test] public void UnversionedNonemptyExtensionIsRejected()
        { var s = SaveFoundationTests.Fixture(); s.survival = new SurvivalState().Capture(3, null); Assert.IsFalse(Codec().Encode(s, out _).Success); }
    }
}
