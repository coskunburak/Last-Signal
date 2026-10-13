#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
namespace LastSignal.Tests
{
    public sealed class CharacterArmClearanceTests
    {
        static CharacterArmClearance.Torso Envelope => new CharacterArmClearance.Torso
        { Center = Vector3.zero, Radii = Vector3.one * .14f, Rotation = Quaternion.identity };
        [TestCase(-1f)] [TestCase(1f)]
        public void SwivelPreservesContactAndBothBoneLengths(float side)
        {
            Vector3 shoulder = new Vector3(.3f * side, 0, 0);
            var original = new CharacterArmIK.Solution { Valid = true,
                Elbow = new Vector3(.1f * side, 0, .1f), Wrist = new Vector3(0, 0, .4f) };
            var solved = CharacterArmClearance.AvoidTorso(original, shoulder, Envelope);
            Assert.That(Vector3.Distance(solved.Wrist, original.Wrist), Is.LessThan(.000001f));
            Assert.That(Vector3.Distance(shoulder, solved.Elbow), Is.EqualTo(Vector3.Distance(shoulder, original.Elbow)).Within(.000001f));
            Assert.That(Vector3.Distance(solved.Elbow, solved.Wrist), Is.EqualTo(Vector3.Distance(original.Elbow, original.Wrist)).Within(.000001f));
            for (int i = 0; i <= 100; i++)
                Assert.That(Vector3.Lerp(solved.Elbow, solved.Wrist, i / 100f).magnitude, Is.GreaterThan(.139f));
        }
        [Test] public void ForearmIntersectionBetweenSamplePointsIsDetected()
        {
            var torso = Envelope;
            float depth = CharacterArmClearance.Penetration(torso,
                new Vector3(-3, 0, .12f), new Vector3(-2.1f, 0, .12f), new Vector3(1.9f, 0, .12f));
            Assert.That(depth, Is.EqualTo(.02f).Within(.00001f));
        }
        [Test] public void AlreadyClearArmIsUnchanged()
        {
            var pose = new CharacterArmIK.Solution { Valid = true, Elbow = new Vector3(.4f, 0, .2f), Wrist = new Vector3(.3f, 0, .4f) };
            var solved = CharacterArmClearance.AvoidTorso(pose, new Vector3(.3f, 0, 0), Envelope);
            Assert.That(solved.Elbow, Is.EqualTo(pose.Elbow));
            Assert.That(solved.Wrist, Is.EqualTo(pose.Wrist));
        }
        [Test] public void ImpossibleContactIsNotHiddenByStretchingOrMovingWrist()
        {
            var pose = new CharacterArmIK.Solution { Valid = true, Elbow = new Vector3(.2f, 0, .2f), Wrist = Vector3.zero };
            var solved = CharacterArmClearance.AvoidTorso(pose, new Vector3(.3f, 0, 0), Envelope);
            Assert.That(solved.Wrist, Is.EqualTo(Vector3.zero));
            Assert.That(CharacterArmClearance.Penetration(Envelope, new Vector3(.3f, 0, 0), solved.Elbow, solved.Wrist), Is.GreaterThan(.1f));
        }
        [Test] public void FrontCorrectionRespectsRotatedTranslatedBody()
        {
            var torso = Envelope; torso.Center = new Vector3(4, 2, -7); torso.Rotation = Quaternion.Euler(20, 130, 0);
            Vector3 point = torso.Center;
            Vector3 corrected = point + torso.FrontCorrection(point);
            Assert.That(torso.Local(corrected).z, Is.EqualTo(.143f).Within(.00001f));
            Assert.That(torso.Penetration(corrected), Is.Zero);
        }
    }
}
#endif
