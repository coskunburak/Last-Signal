using NUnit.Framework;
using UnityEngine;

namespace LastSignal.Tests
{
    public class ZombieImpactReactionTests
    {
        static DamageInfo Hit(Vector3 travel, ZombieBodyPart part = ZombieBodyPart.Torso,
            float damage = 10) => new DamageInfo
        {
            Direction = travel, Amount = damage, BodyPart = part,
            HitPoint = Vector3.zero, SourcePosition = -travel
        };

        [TestCase(0, 0, -1, ZombieImpactSide.Front)]
        [TestCase(0, 0, 1, ZombieImpactSide.Back)]
        [TestCase(1, 0, 0, ZombieImpactSide.Left)]
        [TestCase(-1, 0, 0, ZombieImpactSide.Right)]
        public void TravelDirectionIsClassifiedAtImpact(float x, float y, float z, ZombieImpactSide side)
        {
            var impact = ZombieImpactReaction.Select(Hit(new Vector3(x, y, z)), Quaternion.identity, 100, false);
            Assert.That(impact.Side, Is.EqualTo(side));
            Assert.That(impact.UseLegacyClip, Is.True);
        }

        [Test]
        public void TurningAfterTheHitDoesNotChangeCapturedSide()
        {
            Quaternion facingAtImpact = Quaternion.Euler(0, 90, 0);
            var impact = ZombieImpactReaction.Select(Hit(Vector3.right), facingAtImpact, 100, false);
            Assert.That(impact.Side, Is.EqualTo(ZombieImpactSide.Back));
            Assert.That(impact.Side, Is.Not.EqualTo(
                ZombieImpactReaction.Select(Hit(Vector3.right), Quaternion.identity, 100, false).Side));
        }

        [Test]
        public void MissingOrVerticalTravelUsesSourceThenDeterministicFallback()
        {
            var hit = Hit(Vector3.up);
            hit.SourcePosition = Vector3.back;
            Assert.That(ZombieImpactReaction.Select(hit, Quaternion.identity, 100, false).Side,
                Is.EqualTo(ZombieImpactSide.Back));
            hit.SourcePosition = Vector3.zero;
            Assert.That(ZombieImpactReaction.Select(hit, Quaternion.identity, 100, false).Side,
                Is.EqualTo(ZombieImpactSide.Front));
        }

        [Test]
        public void BodyImpactsUseExistingClipWhileHeadAndDeathRoutesStayDistinct()
        {
            Assert.That(ZombieImpactReaction.Select(Hit(Vector3.back, ZombieBodyPart.Head),
                Quaternion.identity, 100, false).UseLegacyClip, Is.False);
            var heavyTorso = ZombieImpactReaction.Select(Hit(Vector3.back, damage: 30),
                Quaternion.identity, 100, false);
            Assert.That(heavyTorso.Severity, Is.EqualTo(ZombieImpactSeverity.Heavy));
            Assert.That(heavyTorso.UseLegacyClip, Is.True);
            Assert.That(heavyTorso.UseFlyingBackDeath, Is.True);
            var sever = ZombieImpactReaction.Select(Hit(Vector3.back, ZombieBodyPart.RightArm),
                Quaternion.identity, 100, true);
            Assert.That(sever.Severity, Is.EqualTo(ZombieImpactSeverity.Sever));
            Assert.That(sever.UseLegacyClip, Is.True);
            Assert.That(sever.UseFlyingBackDeath, Is.False);
            var headSever = ZombieImpactReaction.Select(Hit(Vector3.forward, ZombieBodyPart.Head),
                Quaternion.identity, 100, true);
            Assert.That(headSever.UseLegacyClip, Is.False);
            Assert.That(headSever.UseFlyingBackDeath, Is.True);
            Assert.That(ZombieImpactReaction.Select(Hit(Vector3.forward, ZombieBodyPart.Head),
                Quaternion.identity, 100, false).UseFlyingBackDeath, Is.False);
        }
    }
}
