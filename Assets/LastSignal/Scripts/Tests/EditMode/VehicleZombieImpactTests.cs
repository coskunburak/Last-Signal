using LastSignal.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class VehicleZombieImpactTests
    {
        [TestCase(0, 0)] [TestCase(1, 0)] [TestCase(2.5f, 0)] [TestCase(18, 1)] [TestCase(30, 1)]
        public void SpeedIsThresholdedAndBounded(float speed, float expected)
            => Assert.AreEqual(expected, new VehicleZombieImpactTuning().Severity(speed));
        [Test] public void GlanceAndReverseUseNormalClosingMotion()
        {
            Assert.AreEqual(10, VehicleZombieImpactTuning.ClosingSpeed(Vector3.forward * 10, Vector3.back));
            Assert.AreEqual(10, VehicleZombieImpactTuning.ClosingSpeed(Vector3.back * 10, Vector3.forward));
            Assert.AreEqual(0, VehicleZombieImpactTuning.ClosingSpeed(Vector3.forward * 10, Vector3.left));
            Assert.AreEqual(0, VehicleZombieImpactTuning.ClosingSpeed(Vector3.back * 10, Vector3.back));
        }
        [Test] public void RotationUsesEachContactPointAndTargetMotion()
        {
            var left = VehicleZombieImpactTuning.RelativePointMotion(Vector3.forward * 8, Vector3.up * 2,
                Vector3.zero, Vector3.left, Vector3.forward);
            var right = VehicleZombieImpactTuning.RelativePointMotion(Vector3.forward * 8, Vector3.up * 2,
                Vector3.zero, Vector3.right, Vector3.forward);
            Assert.AreEqual(9, VehicleZombieImpactTuning.ClosingSpeed(left, Vector3.back));
            Assert.AreEqual(5, VehicleZombieImpactTuning.ClosingSpeed(right, Vector3.back));
            Assert.AreEqual(0, VehicleZombieImpactTuning.ClosingSpeed(right, Vector3.right));
        }
        [TestCase(1, 0, 0)]
        [TestCase(5, 30, 50)]
        [TestCase(8, 70, 99)]
        [TestCase(11.5f, 100, 140)]
        public void NormalInfectedDamageHasMeaningfulSpeedThreshold(float speed, float minimum, float maximum)
        {
            var tuning = new VehicleZombieImpactTuning();
            Assert.That(tuning.Damage(tuning.Severity(speed)), Is.InRange(minimum, maximum));
        }
        [Test] public void InvalidSpeedAndTuningCannotCreateDamage()
        {
            var t = new VehicleZombieImpactTuning(); Assert.AreEqual(0, t.Severity(float.NaN));
            Assert.AreEqual(0, t.Severity(float.PositiveInfinity));
            t.maximumConditionCost = 1; Assert.IsFalse(t.Valid);
        }
        [Test] public void CompoundContactRequiresAllSeparationsAndCooldown()
        {
            var gate = new VehicleImpactGate(); Assert.IsTrue(gate.Begin(1, 0));
            Assert.IsFalse(gate.Begin(1, 2)); gate.End(1);
            Assert.IsFalse(gate.Begin(1, 3)); gate.End(1); gate.End(1);
            Assert.IsTrue(gate.Begin(1, 4)); Assert.IsTrue(gate.Begin(2, 4));
        }
        [Test] public void HundredEpisodesRecycleBoundedGate()
        {
            var gate = new VehicleImpactGate(4);
            for (ulong i = 1; i <= 100; i++) { Assert.IsTrue(gate.Begin(i, i * 2)); gate.End(i); }
        }
        [Test] public void InfectedCostIsLessThanWallAndTuningCopyIsIndependent()
        {
            var tuning = new VehicleTuning(); var copy = tuning.Copy(); copy.zombieImpact.maximumDamage = 5;
            Assert.AreEqual(140, tuning.zombieImpact.maximumDamage);
            Assert.Less(tuning.zombieImpact.Cost(1), (18 - tuning.impactMinimumMetersPerSecond) * tuning.impactConditionPerMeterPerSecond);
            Assert.Greater(tuning.zombieImpact.Damage(tuning.zombieImpact.Severity(8)), 0);
        }
    }
}
