#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class MeleeGripTests
    {
        const string Data = "Assets/LastSignal/Data/Combat/S021/";
        const string Prefabs = "Assets/LastSignal/Prefabs/Combat/S021/";
        [TestCase("WoodenAxe", true)]
        [TestCase("Machete", false)]
        public void ProductionContactsMatchHandleAndRigOwnership(string name, bool twoHands)
        {
            var p = AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(Data + name + "Pose.asset");
            Assert.That(p.ValidGripData, Is.True);
            Assert.That(p.TwoHandedMelee, Is.EqualTo(twoHands));
            Assert.That(p.SingleHanded, Is.EqualTo(!twoHands));
            Assert.That(p.AttackOwnership, Is.EqualTo(WeaponPoseOwnership.ProfileArmTargets));
            var fps = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + name + "_Viewmodel.prefab").GetComponent<FirstPersonWeaponGrip>();
            Assert.That(fps.ValidAuthoring, Is.True);
            Assert.That(fps.Profile, Is.SameAs(p));
            Assert.That(fps.RightHand.name, Is.EqualTo("R_wrist"));
            Assert.That(fps.LeftHand.name, Is.EqualTo("L_wrist"));
            foreach (var sockets in new[] { fps.Sockets, p.WorldPrefab.GetComponent<CharacterWeaponSockets>() })
            {
                Assert.That(sockets.ValidFor(p), Is.True);
                Assert.That(sockets.PrimaryGrip.name, Is.EqualTo("PrimaryGrip_R"));
                Assert.That(sockets.PrimaryGrip.parent.localScale, Is.EqualTo(Vector3.one));
                Assert.That(sockets.PrimaryGrip.localPosition.x, Is.EqualTo(twoHands ? .011f : .017f).Within(.001f));
                Assert.That(Vector3.Dot(sockets.PrimaryGrip.localRotation * Vector3.up, Vector3.left), Is.GreaterThan(.999f));
                if (twoHands)
                {
                    Assert.That(sockets.SecondaryGrip.name, Is.EqualTo("SecondaryGrip_L"));
                    Assert.That(sockets.PrimaryGrip.localPosition.z, Is.EqualTo(.08f).Within(.001f));
                    Assert.That(sockets.SecondaryGrip.localPosition.z, Is.EqualTo(-.12f).Within(.001f));
                    Assert.That(Vector3.Dot(sockets.SecondaryGrip.localRotation * Vector3.up, Vector3.right), Is.GreaterThan(.999f));
                    Assert.That(Vector3.Distance(sockets.PrimaryGrip.position, sockets.SecondaryGrip.position), Is.InRange(.19f, .22f));
                }
                else Assert.That(sockets.SecondaryGrip, Is.Null);
            }
            var animator = fps.PoseFrame.GetComponent<Animator>();
            Assert.That(animator.runtimeAnimatorController.animationClips.Length, Is.GreaterThan(0));
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                Assert.That(AnimationUtility.GetAnimationEvents(clip), Is.Empty, "No animation-owned damage.");
                Assert.That(AnimationUtility.GetCurveBindings(clip).Any(b => b.path.Contains("wrist") || b.path.Contains("WeaponVisualRoot")), Is.False);
            }
        }
        [Test] public void TwoHandedModeRejectsMissingSharedForeignAndInvalidSockets()
        {
            var p = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(Data + "WoodenAxePose.asset"));
            var root = Object.Instantiate(p.WorldPrefab);
            var foreign = new GameObject("Foreign contact");
            try
            {
                var s = root.GetComponent<CharacterWeaponSockets>(); var left = s.LeftGrip;
                s.LeftGrip = null; Assert.That(s.ValidFor(p), Is.False);
                s.LeftGrip = s.PrimaryGrip; Assert.That(s.ValidFor(p), Is.False);
                s.LeftGrip = foreign.transform; Assert.That(s.ValidFor(p), Is.False);
                s.LeftGrip = left; left.localScale = new Vector3(1, 2, 1); Assert.That(s.ValidFor(p), Is.False);
                left.localScale = Vector3.one; p.SecondaryFingerFlexion.x = float.NaN; Assert.That(s.ValidFor(p), Is.False);
                p.SecondaryFingerFlexion.x = 58; p.WorldScale = float.PositiveInfinity; Assert.That(s.ValidFor(p), Is.False);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(p); Object.DestroyImmediate(foreign); }
        }
        [TestCase("WoodenAxe")]
        [TestCase("Machete")]
        public void PhaseSamplesStayFiniteAndReturnToSameReady(string name)
        {
            var p = AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(Data + name + "Pose.asset");
            for (int i = 0; i <= 200; i++)
            {
                p.AttackPose(i / 200f, out var position, out var rotation);
                Assert.That(float.IsFinite(position.sqrMagnitude), Is.True);
                Assert.That(float.IsFinite(rotation.x + rotation.y + rotation.z + rotation.w), Is.True);
            }
            p.AttackPose(1, out var end, out var q);
            Assert.That(end.magnitude, Is.LessThan(.0001f)); Assert.That(Quaternion.Angle(q, Quaternion.identity), Is.LessThan(.01f));
        }
        [Test] public void EnumMigrationKeepsFirearmSerializedValues()
        {
            Assert.That((int)WeaponGripMode.OneHandedPistol, Is.EqualTo(1));
            Assert.That((int)WeaponGripMode.TwoHandedRifle, Is.EqualTo(4));
            Assert.That((int)WeaponGripMode.TwoHandedHeavyMelee, Is.EqualTo(5));
        }
    }
}
#endif
