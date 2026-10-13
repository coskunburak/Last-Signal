#if UNITY_EDITOR
using System.Collections;
using LastSignal.Inventory;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    public sealed partial class CharacterIntegrationPlayTests
    {
        CharacterPoseProbe AddPoseProbe()
        {
            var probe = player.AddComponent<CharacterPoseProbe>();
            probe.Rig = presenter.GetComponent<CharacterWeaponRig>(); probe.Animator = animator;
            return probe;
        }
        static IEnumerator NextPose(CharacterPoseProbe probe)
        {
            int before = probe.Samples;
            for (int i = 0; i < 30 && probe.Samples <= before; i++) yield return null;
            Assert.That(probe.Samples, Is.GreaterThan(before), "Production LateUpdate must actually execute.");
        }
        static void AssertContact(CharacterPoseProbe probe, float tolerance = .025f)
        {
            Assert.That(probe.Active, Is.True); Assert.That(probe.Weight, Is.InRange(0, 1));
            Assert.That(probe.StockError, Is.LessThan(.001f), "Stock must retain priority over hand reach.");
            Assert.That(Mathf.Abs(probe.Rig.TorsoReachYaw), Is.LessThanOrEqualTo(probe.Rig.ActiveProfile.TorsoReachYawDegrees + .001f));
            Assert.That(probe.RightError, Is.LessThan(tolerance), $"Right palm must contact the pistol grip. {probe.Describe()}");
            Assert.That(probe.LeftError, Is.LessThan(tolerance), $"Left palm must contact the support/reload target. {probe.Describe()}");
        }
        [UnityTest] public IEnumerator RifleIdleAdsAndHipUseActualAimAndCalibratedContacts()
        {
            var probe = AddPoseProbe();
            yield return new WaitForSeconds(1); yield return NextPose(probe);
            AssertContact(probe); Assert.That(probe.Weight, Is.EqualTo(1).Within(.001f));
            var hipDirection = probe.MuzzleDirection;
            Press(Mouse.current.rightButton);
            yield return new WaitForSeconds(.6f); yield return NextPose(probe);
            Assert.That(presenter.AimAmount, Is.GreaterThan(.99f)); AssertContact(probe);
            Assert.That(probe.Rig.HipClearanceDegrees, Is.LessThan(.01f), "Full ADS must retain exact authored aim");
            Assert.That(Vector3.Angle(probe.MuzzleDirection, player.GetComponent<FirstPersonLook>().View.transform.forward), Is.LessThan(1));
            Assert.That(Vector3.Angle(hipDirection, probe.MuzzleDirection), Is.GreaterThan(3), "Hip and ADS must be distinct.");
            Release(Mouse.current.rightButton);
            yield return new WaitForSeconds(.6f); yield return NextPose(probe);
            Assert.That(presenter.AimAmount, Is.LessThan(.01f)); AssertContact(probe);
        }
        [UnityTest] public IEnumerator RifleCrouchWalkSprintAndSlideKeepStockAndBoundedArms()
        {
            var probe = AddPoseProbe(); yield return new WaitForSeconds(1);
            Assert.That(stance.TrySetCrouching(true), Is.True);
            yield return new WaitForSeconds(.5f); yield return NextPose(probe); AssertContact(probe, .04f);
            Assert.That(stance.TrySetCrouching(false), Is.True);
            Press(Keyboard.current.wKey);
            yield return new WaitForSeconds(.4f); yield return NextPose(probe); AssertContact(probe, .04f);
            Press(Keyboard.current.leftShiftKey);
            yield return new WaitForSeconds(.35f); yield return NextPose(probe);
            Assert.That(motor.IsSprinting, Is.True); Assert.That(probe.Rig.SprintWeight, Is.GreaterThan(.9f));
            AssertContact(probe, .05f);
            Assert.That(motor.TryStartSlide(), Is.True);
            yield return new WaitForSeconds(.2f); yield return NextPose(probe);
            Assert.That(probe.Rig.SlideWeight, Is.GreaterThan(.5f)); AssertContact(probe, .05f);
            Release(Keyboard.current.wKey); Release(Keyboard.current.leftShiftKey);
            yield return new WaitForSeconds(1); yield return NextPose(probe);
            Assert.That(probe.Rig.SlideWeight, Is.LessThan(.01f)); Assert.That(probe.Rig.SprintWeight, Is.LessThan(.01f));
            Assert.That(motor.IsSliding, Is.False);
            AssertContact(probe, .04f);
            // Sample the recovered idle across time; its chest animation can have a different
            // heading from the initial idle sample. A single settled frame misses that regression.
            float idleUntil = Time.time + 2;
            while (Time.time < idleUntil)
            {
                yield return new WaitForSeconds(.05f); yield return NextPose(probe);
                AssertContact(probe, .04f);
            }
        }
        [UnityTest] public IEnumerator RifleExtremePitchClampsWithoutBoneStretchAndReportsContactError()
        {
            var probe = AddPoseProbe(); yield return new WaitForSeconds(1);
            var look = player.GetComponent<FirstPersonLook>();
            var bones = new[] { HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
                HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand };
            var t = new Transform[bones.Length]; for (int i = 0; i < t.Length; i++) t[i] = animator.GetBoneTransform(bones[i]);
            float rightA = Vector3.Distance(t[0].position, t[1].position), rightB = Vector3.Distance(t[1].position, t[2].position);
            float leftA = Vector3.Distance(t[3].position, t[4].position), leftB = Vector3.Distance(t[4].position, t[5].position);
            Press(Mouse.current.rightButton);
            foreach (float pitch in new[] { -55f, 55f, 0f })
            {
                look.ApplyRecoil(pitch - look.Pitch, 0);
                yield return new WaitForSeconds(.8f); yield return NextPose(probe);
                Assert.That(probe.StockError, Is.LessThan(.001f));
                Assert.That(Vector3.Distance(t[0].position, t[1].position), Is.EqualTo(rightA).Within(.001f));
                Assert.That(Vector3.Distance(t[1].position, t[2].position), Is.EqualTo(rightB).Within(.001f));
                Assert.That(Vector3.Distance(t[3].position, t[4].position), Is.EqualTo(leftA).Within(.001f));
                Assert.That(Vector3.Distance(t[4].position, t[5].position), Is.EqualTo(leftB).Within(.001f));
                Assert.That(probe.RightError, Is.EqualTo(probe.RightResidual).Within(.002f));
                Assert.That(probe.LeftError, Is.EqualTo(probe.LeftResidual).Within(.002f));
                Assert.That(Vector3.Angle(probe.MuzzleDirection, look.View.transform.forward), Is.LessThan(1));
            }
            Release(Mouse.current.rightButton);
        }
        [UnityTest] public IEnumerator RifleAcceptedShotMovesAllContactsAndRecoversWithoutExtraAmmoChanges()
        {
            var probe = AddPoseProbe(); yield return new WaitForSeconds(1);
            var weapon = presenter.Combat.ActiveWeapon; int before = weapon.RuntimeState.CurrentMagazine;
            weapon.OnFirePressed(); weapon.OnFireReleased();
            yield return NextPose(probe);
            Assert.That(weapon.RuntimeState.CurrentMagazine, Is.EqualTo(before - 1));
            Assert.That(probe.Recoil, Is.GreaterThan(0)); Assert.That(probe.Recoil, Is.LessThanOrEqualTo(probe.Rig.ActiveProfile.RecoilDistance * 2));
            AssertContact(probe, .04f);
            yield return new WaitForSeconds(.8f); yield return NextPose(probe);
            Assert.That(probe.Recoil, Is.LessThan(.0001f)); AssertContact(probe);
            Assert.That(weapon.RuntimeState.CurrentMagazine, Is.EqualTo(before - 1));
        }
        [UnityTest] public IEnumerator RifleReloadFollowsMovingMagazineAndInterruptionRestoresIt()
        {
            var probe = AddPoseProbe(); yield return new WaitForSeconds(1);
            var weapon = presenter.Combat.ActiveWeapon;
            var inventory = player.GetComponent<PlayerInventory>(); inventory.TryAdd(weapon.Definition.Ammunition, 30);
            weapon.OnFirePressed(); weapon.OnFireReleased();
            var rest = probe.Rig.Sockets.Magazine.localPosition;
            int total = weapon.RuntimeState.TotalAmmo;
            weapon.OnReloadRequested();
            yield return new WaitForSeconds(weapon.Definition.TacticalReloadSeconds * .5f);
            yield return NextPose(probe);
            Assert.That(weapon.RuntimeState.State, Is.EqualTo(WeaponState.Reloading));
            Assert.That(probe.Rig.Sockets.Magazine.localPosition.y, Is.LessThan(rest.y - .07f));
            AssertContact(probe, .04f);
            Assert.That(weapon.RuntimeState.TotalAmmo, Is.EqualTo(total));
            weapon.RequestUnequip();
            yield return NextPose(probe);
            Assert.That(Vector3.Distance(probe.Rig.Sockets.Magazine.localPosition, rest), Is.LessThan(.0001f));
            yield return new WaitForSeconds(weapon.Definition.UnequipSeconds + .1f); yield return NextPose(probe);
            Assert.That(probe.Active, Is.False); Assert.That(probe.Rig.WeaponVisual.activeSelf, Is.False);
            weapon.RequestEquip(); yield return new WaitForSeconds(weapon.Definition.EquipSeconds + .3f);
            yield return NextPose(probe); AssertContact(probe);
        }
        [UnityTest] public IEnumerator RifleRepeatedSwitchDisableAndEquipDiscardStaleTargets()
        {
            var probe = AddPoseProbe(); yield return new WaitForSeconds(1);
            for (int i = 0; i < 4; i++)
            {
                var old = probe.Rig.WeaponVisual;
                Assert.That(presenter.Combat.SelectSlot(PlayerCombatController.CombatSlot.Melee), Is.True);
                yield return null; yield return NextPose(probe);
                Assert.That(old == null, Is.True); Assert.That(probe.Active, Is.False);
                Assert.That(probe.Rig.ActiveProfile.Melee, Is.Not.Null);
                Assert.That(presenter.Combat.SelectSlot(PlayerCombatController.CombatSlot.Firearm), Is.True);
                yield return new WaitForSeconds(.8f); yield return NextPose(probe); AssertContact(probe);
                Assert.That(presenter.GetComponentsInChildren<CharacterWeaponSockets>(), Has.Length.EqualTo(1));
            }
            for (int i = 0; i < 3; i++)
            {
                var retired = probe.Rig.WeaponVisual;
                presenter.Combat.UnequipWeapon(); yield return null; yield return NextPose(probe);
                Assert.That(retired == null && probe.Rig.Sockets == null && probe.Rig.ActiveProfile == null, Is.True);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Resources/Weapon_AssaultRifle.prefab");
                presenter.Combat.EquipWeapon(Object.Instantiate(prefab).GetComponent<WeaponController>());
                yield return new WaitForSeconds(.8f); yield return NextPose(probe); AssertContact(probe);
            }
            probe.Rig.enabled = false; yield return null;
            Assert.That(probe.Rig.Sockets == null && probe.Rig.WeaponVisual == null && probe.Rig.ActiveProfile == null, Is.True);
            probe.Rig.enabled = true; yield return new WaitForSeconds(.5f); yield return NextPose(probe); AssertContact(probe);
        }
        [UnityTest] public IEnumerator RifleEquipWeightStaysBoundedAndAdsModalPauseKeepsContactsFrozen()
        {
            var probe = AddPoseProbe(); yield return new WaitForSeconds(1);
            float blendSeconds = probe.Rig.ActiveProfile.PoseBlendSeconds;
            probe.Rig.enabled = false; yield return null; probe.Rig.enabled = true;
            float previous = 0;
            // The blend is measured in scaled seconds. An uncapped batch player can render 120
            // frames in less than PoseBlendSeconds; frame count is not a completion deadline.
            float deadline = Time.time + Mathf.Max(1, blendSeconds * 4);
            float watchdog = Time.realtimeSinceStartup + 10;
            while (previous < .999f && Time.time < deadline && Time.realtimeSinceStartup < watchdog)
            {
                yield return NextPose(probe);
                Assert.That(probe.Weight, Is.InRange(previous, 1)); previous = probe.Weight;
            }
            Assert.That(previous, Is.GreaterThan(.999f)); AssertContact(probe);
            Press(Mouse.current.rightButton);
            yield return new WaitForSeconds(.6f); yield return NextPose(probe);
            Assert.That(presenter.AimAmount, Is.GreaterThan(.99f));
            input.SetGameplay(false); yield return NextPose(probe);
            Vector3 frozenStock = probe.Stock, frozenDirection = probe.MuzzleDirection;
            for (int i = 0; i < 20; i++)
            {
                yield return NextPose(probe);
                Assert.That(Vector3.Distance(frozenStock, probe.Stock), Is.LessThan(.0001f));
                Assert.That(Vector3.Angle(frozenDirection, probe.MuzzleDirection), Is.LessThan(.1f));
            }
            Release(Mouse.current.rightButton); input.SetGameplay(true);
            yield return new WaitForSeconds(.6f); yield return NextPose(probe); AssertContact(probe);
        }
        [UnityTest] public IEnumerator RifleCompletedReloadRestoresSupportAndPreservesAmmoTotal()
        {
            var probe = AddPoseProbe(); yield return new WaitForSeconds(1);
            var weapon = presenter.Combat.ActiveWeapon;
            player.GetComponent<PlayerInventory>().TryAdd(weapon.Definition.Ammunition, 30);
            weapon.OnFirePressed(); weapon.OnFireReleased();
            int total = weapon.RuntimeState.TotalAmmo;
            var magazine = probe.Rig.Sockets.Magazine; var rest = magazine.localPosition;
            weapon.OnReloadRequested();
            yield return new WaitForSeconds(weapon.Definition.TacticalReloadSeconds + .2f); yield return NextPose(probe);
            Assert.That(weapon.RuntimeState.State, Is.EqualTo(WeaponState.Ready));
            Assert.That(weapon.RuntimeState.TotalAmmo, Is.EqualTo(total));
            Assert.That(Vector3.Distance(magazine.localPosition, rest), Is.LessThan(.0001f)); AssertContact(probe);
        }
        [UnityTest] public IEnumerator RiflePauseAndTimeScaleFreezeAllProceduralBonesThenResume()
        {
            var probe = AddPoseProbe(); yield return new WaitForSeconds(1);
            Time.timeScale = 0; yield return NextPose(probe);
            var frozen = (Quaternion[])probe.Rotations.Clone(); var stock = probe.Stock;
            for (int frame = 0; frame < 20; frame++)
            {
                yield return NextPose(probe);
                for (int i = 0; i < frozen.Length; i++)
                    Assert.That(Quaternion.Angle(frozen[i], probe.Rotations[i]), Is.LessThan(.1f), "Paused bone " + i);
                Assert.That(Vector3.Distance(stock, probe.Stock), Is.LessThan(.0001f)); AssertContact(probe);
            }
            Time.timeScale = .25f; yield return new WaitForSeconds(.2f); yield return NextPose(probe); AssertContact(probe);
            Time.timeScale = 1; yield return new WaitForSeconds(.2f); yield return NextPose(probe); AssertContact(probe);
        }
        [UnityTest] public IEnumerator RifleVehicleAndDeathReleaseOwnershipThenRecover()
        {
            var probe = AddPoseProbe(); yield return new WaitForSeconds(1);
            Assert.That(input.SetDrivingContext(true, true), Is.True);
            yield return NextPose(probe);
            Assert.That(probe.Active, Is.False); Assert.That(probe.Rig.WeaponVisual.activeSelf, Is.False);
            input.SetDrivingContext(true, false); yield return NextPose(probe); Assert.That(probe.Active, Is.False);
            input.SetDrivingContext(false, true); input.SetGameplay(true);
            yield return new WaitForSeconds(.5f); yield return NextPose(probe); AssertContact(probe);
            var health = player.GetComponent<PlayerHealth>();
            health.TakeDamage(new DamageInfo { Amount = health.MaxHealth });
            yield return NextPose(probe);
            Assert.That(probe.Active, Is.False); Assert.That(probe.Rig.WeaponVisual == null || !probe.Rig.WeaponVisual.activeSelf, Is.True);
            health.ResetForSession(); input.SetGameplay(true);
            // Combat may holster on death; request the normal existing equip path.
            var weapon = presenter.Combat.ActiveWeapon; if (weapon) weapon.RequestEquip();
            yield return new WaitForSeconds(1); yield return NextPose(probe); AssertContact(probe);
        }
    }
}
#endif
