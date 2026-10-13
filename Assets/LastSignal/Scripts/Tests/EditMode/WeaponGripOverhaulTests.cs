#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace LastSignal.Tests
{
    public sealed class WeaponGripOverhaulTests
    {
        const string Prefabs = "Assets/LastSignal/Prefabs/Combat/S021/";
        [TestCase("Weapon_TacticalPistol")][TestCase("Machete_Viewmodel")][TestCase("WoodenAxe_Viewmodel")]
        public void FingersUseRealSkinBindPoseAndRejectCrossHandAssignment(string name)
        {
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + name + ".prefab"));
            try
            {
                var grip = root.GetComponent<FirstPersonWeaponGrip>();
                Assert.That(grip.ValidAuthoring, Is.True); Assert.That(grip.AuthoredFingerCurl, Is.False);
                bool checkedSkin = false;
                foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (!skin.enabled || !skin.sharedMesh) continue;
                    int bone = Array.IndexOf(skin.bones, grip.RightFingers[7]);
                    int parent = Array.IndexOf(skin.bones, grip.RightFingers[7].parent);
                    if (bone < 0 || parent < 0) continue;
                    var binds = skin.sharedMesh.bindposes;
                    var expected = (binds[parent] * binds[bone].inverse).rotation;
                    Assert.That(Quaternion.Angle(expected, grip.RightFingerRest[7]), Is.LessThan(.05f));
                    checkedSkin = true; break;
                }
                Assert.That(checkedSkin, Is.True, "Actual mesh binding must establish the neutral finger pose.");
                grip.RightFingers[0] = grip.LeftFingers[0];
                Assert.That(grip.ValidAuthoring, Is.False, "Left finger must not calibrate the right hand.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [TestCase("Weapon_TacticalPistol")][TestCase("Machete_Viewmodel")][TestCase("WoodenAxe_Viewmodel")]
        public void OwnedClipsHaveValidBindingsAndCannotOverridePistolFingers(string name)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + name + ".prefab");
            var animator = root.GetComponentInChildren<Animator>(true);
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                Assert.That(AnimationUtility.GetAnimationEvents(clip), Is.Empty);
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    Assert.That(binding.path.Length == 0 || animator.transform.Find(binding.path), Is.True, binding.path);
                    if (name == "Weapon_TacticalPistol")
                    {
                        Assert.That(binding.path.Contains("/hand."), Is.False);
                        Assert.That(binding.path.Contains("/wpn_body"), Is.False);
                    }
                }
            }
        }
        [Test] public void RepeatedFingerSamplingRestoresOriginalPose()
        {
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Machete_Viewmodel.prefab"));
            try
            {
                var grip = root.GetComponent<FirstPersonWeaponGrip>();
                var pose = new CharacterHandPose(grip.RightHand, grip.RightFingers, false, .012f, false);
                var before = new Quaternion[15]; for (int i = 0; i < 15; i++) before[i] = grip.RightFingers[i].localRotation;
                pose.Apply(1, false, 0, 1, grip.Profile); pose.Apply(.5f, false, 0, 1, grip.Profile); pose.Restore();
                for (int i = 0; i < 15; i++) Assert.That(Quaternion.Angle(before[i], grip.RightFingers[i].localRotation), Is.LessThan(.01f));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [Test] public void ReadyBlendIsBoundedAndFreezesWithoutSimulationTime()
        {
            var p = AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>("Assets/LastSignal/Data/Combat/S021/PistolPose.asset");
            Assert.That(p.StepReady(.4f, 1, 0), Is.EqualTo(.4f));
            Assert.That(p.StepReady(0, 1, 100), Is.EqualTo(1));
            Assert.That(p.StepReady(1, 0, 100), Is.Zero);
            p.ReadyPose(0, out var low, out _); p.ReadyPose(1, out var raised, out _);
            Assert.That(low.y, Is.LessThan(raised.y)); Assert.That(low.x, Is.GreaterThan(0));
        }
        [TestCase("Machete")][TestCase("WoodenAxe")]
        public void ImpactDoesNotStopMotionAndRecoveryReturnsExactly(string name)
        {
            var p = AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>("Assets/LastSignal/Data/Combat/S021/" + name + "Pose.asset");
            float total = p.Melee.WindupSeconds + p.Melee.ActiveSeconds + p.Melee.RecoverySeconds;
            float impact = (p.Melee.WindupSeconds + p.Melee.ActiveSeconds) / total;
            const float epsilon = .0001f;
            p.AttackPose(impact - epsilon, out var before, out _);
            p.AttackPose(impact, out var contact, out _);
            p.AttackPose(impact + epsilon, out var after, out _);
            Vector3 incoming = (contact - before) / epsilon, outgoing = (after - contact) / epsilon;
            Assert.That(incoming.magnitude, Is.GreaterThan(.1f));
            Assert.That(Vector3.Distance(incoming, outgoing) / incoming.magnitude, Is.LessThan(.03f));
            p.AttackPose(1, out var rest, out var rotation);
            Assert.That(rest.magnitude, Is.LessThan(.0001f)); Assert.That(Quaternion.Angle(rotation, Quaternion.identity), Is.LessThan(.01f));
        }
    }
}
#endif
