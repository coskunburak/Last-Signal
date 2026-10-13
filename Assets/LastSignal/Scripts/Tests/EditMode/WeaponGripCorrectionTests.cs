#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace LastSignal.Tests
{
    public sealed class WeaponGripCorrectionTests
    {
        const string Data = "Assets/LastSignal/Data/Combat/S021/";
        const string Prefabs = "Assets/LastSignal/Prefabs/Combat/S021/";
        [TestCase("Pistol", "Weapon_TacticalPistol", WeaponGripMode.OneHandedPistol)]
        [TestCase("Machete", "Machete_Viewmodel", WeaponGripMode.OneHandedLightMelee)]
        [TestCase("WoodenAxe", "WoodenAxe_Viewmodel", WeaponGripMode.TwoHandedHeavyMelee)]
        public void RealAssetsShareOneProfileAndHaveExplicitRightHandContacts(string name, string prefab, WeaponGripMode mode)
        {
            var profile = AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(Data + name + "Pose.asset");
            var fps = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + prefab + ".prefab");
            Assert.That(profile.GripMode, Is.EqualTo(mode));
            Assert.That(profile.PrimaryHand, Is.EqualTo(WeaponPrimaryHand.Right));
            Assert.That(profile.AttackOwnership, Is.EqualTo(WeaponPoseOwnership.ProfileArmTargets));
            var grip = fps.GetComponent<FirstPersonWeaponGrip>();
            Assert.That(grip.Profile, Is.SameAs(profile)); Assert.That(grip.Sockets.ValidFor(profile), Is.True);
            Assert.That(profile.WorldPrefab.GetComponent<CharacterWeaponSockets>().ValidFor(profile), Is.True);
            if (profile.TwoHandedMelee) Assert.That(grip.Sockets.SecondaryGrip, Is.Not.Null);
            else Assert.That(grip.Sockets.SecondaryGrip, Is.Null); Assert.That(grip.Sockets.StockContact, Is.Null);
            Assert.That(grip.RightFingers.Length, Is.EqualTo(15)); Assert.That(grip.RightFingers.All(t => t), Is.True);
            Assert.That(grip.LeftFingers.All(t => t), Is.True);
            Assert.That(grip.Visual.IsChildOf(grip.RightHand), Is.False, "Independent target must not chase its solved hand.");
            Assert.That(fps.GetComponentsInChildren<Animator>(true).Length, Is.EqualTo(1));
            Assert.That(profile.WorldScale, Is.EqualTo(1));
            var body = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Art/CharacterIntegration/Diesel_WorldBody.prefab");
            Assert.That(body.GetComponent<CharacterWeaponRig>().Profiles.Count(p => p == profile), Is.EqualTo(1));
        }
        [Test] public void PistolNeedsNeitherSupportNorStockButRejectsMissingPrimaryAndSight()
        {
            var profile = AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(Data + "PistolPose.asset");
            var go = Object.Instantiate(profile.WorldPrefab);
            try
            {
                var sockets = go.GetComponent<CharacterWeaponSockets>();
                Assert.That(sockets.HasFirearmGrips, Is.False);
                Assert.That(sockets.ValidFor(profile), Is.True);
                var primary = sockets.PrimaryGrip;
                sockets.PalmContact = sockets.RightGrip = null;
                Assert.That(sockets.ValidFor(profile), Is.False);
                sockets.RightGrip = primary; sockets.AimReference = null;
                Assert.That(sockets.ValidFor(profile), Is.False);
            }
            finally { Object.DestroyImmediate(go); }
        }
        [TestCase("Machete")][TestCase("WoodenAxe")]
        public void MeleeRejectsInvalidScaleAndHasNoFirearmSocketDependency(string name)
        {
            var profile = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(Data + name + "Pose.asset"));
            var go = Object.Instantiate(profile.WorldPrefab);
            try
            {
                var sockets = go.GetComponent<CharacterWeaponSockets>();
                Assert.That(sockets.Muzzle, Is.Null); Assert.That(sockets.Magazine, Is.Null);
                Assert.That(sockets.ValidFor(profile), Is.True);
                profile.WorldScale = float.NaN; Assert.That(sockets.ValidFor(profile), Is.False);
                profile.WorldScale = 1; sockets.PrimaryGrip.localScale = new Vector3(1, 2, 1);
                Assert.That(sockets.ValidFor(profile), Is.False);
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(profile); }
        }
        [Test] public void GenericPalmCalibrationAndRotationRestorationUseActualArms()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Machete_Viewmodel.prefab");
            // Prefab asset transforms are inspected through a detached clone without entering Play mode.
            var go = Object.Instantiate(prefab);
            try
            {
                var grip = go.GetComponent<FirstPersonWeaponGrip>();
                var palm = new CharacterHandPose(grip.RightHand, grip.RightFingers, false, .012f, false);
                Assert.That(palm.Valid, Is.True);
                palm.WristPose(palm.PalmPosition, palm.PalmRotation, out var position, out var rotation);
                Assert.That(Vector3.Distance(position, grip.RightHand.position), Is.LessThan(.0001f));
                Assert.That(Quaternion.Angle(rotation, grip.RightHand.rotation), Is.LessThan(.01f));
                var initial = grip.RightFingers.Select(t => t.localRotation).ToArray();
                palm.Apply(1, false, 0, .7f); palm.Restore(); palm.Restore();
                for (int i = 0; i < initial.Length; i++) Assert.That(Quaternion.Angle(initial[i], grip.RightFingers[i].localRotation), Is.LessThan(.01f));
                CharacterWeaponSockets.Attach(grip.Visual, grip.Sockets.PrimaryGrip, palm.PalmPosition, palm.PalmRotation);
                Assert.That(Vector3.Distance(grip.Sockets.PrimaryGrip.position, palm.PalmPosition), Is.LessThan(.0001f));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void RifleContractAndGeometryArePreserved()
        {
            var profile = AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>("Assets/LastSignal/Art/CharacterIntegration/RiflePose.asset");
            Assert.That(profile.SingleHanded, Is.False);
            Assert.That(profile.WorldPrefab.GetComponent<CharacterWeaponSockets>().HasFirearmGrips, Is.True);
            Assert.That(profile.WorldPrefab.GetComponent<CharacterWeaponSockets>().ValidFor(profile), Is.True);
        }
        [Test] public void PistolHasNoEnabledRifleLensOrTriggerAndUsesRealMagazine()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Weapon_TacticalPistol.prefab");
            Assert.That(root.GetComponent<ScopeOpticPresenter>().enabled, Is.False);
            Assert.That(root.GetComponent<WeaponAnimationPresenter>().enabled, Is.False);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                if (renderer.name == "ScopeLens" || renderer.name == "Trigger" || renderer.name == "MRPoly_RifleBody")
                    Assert.That(renderer.enabled, Is.False, renderer.name);
            var grip = root.GetComponent<FirstPersonWeaponGrip>();
            Assert.That(grip.Sockets.Magazine.name, Is.EqualTo("Magazine"));
            Assert.That(grip.Sockets.ReloadReference.IsChildOf(grip.Sockets.Magazine), Is.True);
            Assert.That(root.GetComponent<WeaponController>().AimReference, Is.SameAs(grip.Sockets.AimReference));
        }
        [Test] public void LightAndHeavyMotionHaveDistinctAnticipationAndReturnAtCombatEnd()
        {
            var light = AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(Data + "MachetePose.asset");
            var heavy = AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(Data + "WoodenAxePose.asset");
            Assert.That(heavy.WindupOffset.y, Is.GreaterThan(light.WindupOffset.y));
            foreach (var p in new[] { light, heavy })
            {
                float total = p.Melee.WindupSeconds + p.Melee.ActiveSeconds + p.Melee.RecoverySeconds;
                p.AttackPose(p.Melee.WindupSeconds / total, out var windup, out _);
                Assert.That(Vector3.Distance(windup, p.WindupOffset), Is.LessThan(.0001f));
                p.AttackPose(1, out var end, out var rotation);
                Assert.That(end.magnitude, Is.LessThan(.0001f)); Assert.That(Quaternion.Angle(rotation, Quaternion.identity), Is.LessThan(.01f));
            }
        }
    }
}
#endif
