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
    public sealed partial class WeaponGripCorrectionPlayTests
    {
        GameObject player, floor;
        PlayerCombatController combat;
        PlayerInputReader input;
        WeaponGripProbe probe;
        InputFixtureIsolation.SceneScope scene;
        InputFixtureIsolation.NativeInputScope native;
        [SetUp] public void Setup()
        {
            scene = new InputFixtureIsolation.SceneScope(); InputFixtureIsolation.DisableLiveActions();
            native = new InputFixtureIsolation.NativeInputScope(); Time.timeScale = 1;
            floor = new GameObject("Grip fixture floor"); floor.transform.position = new Vector3(500, 99.75f, 500);
            floor.AddComponent<BoxCollider>().size = new Vector3(50, .5f, 50);
            player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Player/Player.prefab"), new Vector3(500, 100.04f, 500), Quaternion.identity);
            player.AddComponent<PlayerInventory>().Initialize(24);
            input = player.GetComponent<PlayerInputReader>(); input.SetGameplay(true);
            combat = player.GetComponent<PlayerCombatController>();
            probe = player.AddComponent<WeaponGripProbe>(); probe.World = player.GetComponentInChildren<CharacterWeaponRig>();
            Physics.SyncTransforms();
        }
        [TearDown] public void TearDown()
        {
            if (player) Object.DestroyImmediate(player); if (floor) Object.DestroyImmediate(floor);
            Time.timeScale = 1; InputFixtureIsolation.DisableLiveActions();
            try { native?.Dispose(); } finally { scene?.Dispose(); }
        }
        IEnumerator Sample()
        {
            int before = probe.Samples;
            for (int i = 0; i < 30 && before == probe.Samples; i++) yield return null;
            Assert.That(probe.Samples, Is.GreaterThan(before), "Must observe the final production LateUpdate.");
        }
        IEnumerator Pistol()
        {
            yield return null;
            Assert.That(combat.SelectFirearm("weapon.pistol"), Is.True);
            probe.Fps = combat.ActiveWeapon.GetComponent<FirstPersonWeaponGrip>();
            float until = Time.realtimeSinceStartup + 5;
            while (combat.ActiveWeapon.RuntimeState.State != WeaponState.Ready)
            { Assert.That(Time.realtimeSinceStartup, Is.LessThan(until)); yield return null; }
            yield return new WaitForSeconds(.4f); yield return Sample();
        }
        void Contact(bool neutral = true)
        {
            Assert.That(probe.Active, Is.True);
            Assert.That(probe.FpsError, Is.LessThan(.02f)); Assert.That(probe.WorldError, Is.LessThan(.02f));
            Assert.That(float.IsFinite(probe.Residual), Is.True);
            if (neutral) Assert.That(probe.Residual, Is.LessThan(.02f), "A clamped contact alone is not a reachable authored pose.");
        }
        void PistolSupportContact()
        {
            var profile = probe.World.ActiveProfile;
            Assert.That(profile, Is.Not.Null);
            Assert.That(probe.Support, Is.EqualTo(profile.WorldTwoHandedPistol ? profile.HandWeight : 0).Within(.01f));
            if (profile.WorldTwoHandedPistol)
            {
                Assert.That(profile.ValidWorldPistolSupport, Is.True);
                Assert.That(probe.WorldSecondaryError, Is.LessThan(.02f));
                Assert.That(probe.WorldSecondaryResidual, Is.LessThan(.02f));
            }
        }
        [UnityTest] public IEnumerator PistolHipAdsAndShotKeepAuthoredHandContacts()
        {
            yield return Pistol(); Contact(); PistolSupportContact(); Assert.That(probe.Reload, Is.Zero);
            InputFixtureIsolation.QueueButton(Mouse.current.rightButton, true);
            yield return new WaitForSeconds(.5f); yield return Sample(); Contact();
            Assert.That(combat.ActiveWeapon.RuntimeState.AimAmount, Is.GreaterThan(.99f));
            PistolSupportContact(); Assert.That(probe.Reload, Is.Zero);
            var view = player.GetComponent<FirstPersonLook>().View.transform;
            var sight = view.InverseTransformPoint(probe.Sight);
            Assert.That(new Vector2(sight.x, sight.y).magnitude, Is.LessThan(.01f));
            int rounds = combat.ActiveWeapon.RuntimeState.CurrentMagazine;
            combat.ActiveWeapon.OnFirePressed(); combat.ActiveWeapon.OnFireReleased();
            yield return Sample(); Contact(false);
            Assert.That(combat.ActiveWeapon.RuntimeState.CurrentMagazine, Is.EqualTo(rounds - 1));
            PistolSupportContact(); Assert.That(probe.Reload, Is.Zero);
        }
        [UnityTest] public IEnumerator ReloadInterruptionSwitchPauseAndEnableRestoreContacts()
        {
            yield return Pistol(); var pistol = combat.ActiveWeapon;
            var inventory = player.GetComponent<PlayerInventory>(); inventory.TryAdd(pistol.Definition.Ammunition, 5);
            pistol.OnReloadRequested(); Assert.That(pistol.RuntimeState.State, Is.EqualTo(WeaponState.Reloading));
            yield return new WaitForSeconds(.7f); yield return Sample();
            Assert.That(probe.Reload, Is.GreaterThan(.1f)); Contact(false);
            Assert.That(combat.SelectMeleeWeapon(MeleeWeaponDefinition.BladeId), Is.True);
            yield return null; Assert.That(probe.Fps.ReloadAssist, Is.Zero);
            Assert.That(combat.SelectFirearm("weapon.pistol"), Is.True);
            yield return new WaitForSeconds(.7f); yield return Sample(); Contact(); PistolSupportContact();
            input.SetGameplay(false); Time.timeScale = 0;
            yield return Sample(); var palm = probe.Palm;
            for (int i = 0; i < 5; i++) yield return Sample();
            Assert.That(Vector3.Distance(palm, probe.Palm), Is.LessThan(.001f));
            Time.timeScale = 1; input.SetGameplay(true);
            probe.Fps.enabled = false; yield return null; Assert.That(probe.Fps.HasPose, Is.False);
            probe.Fps.enabled = true; yield return new WaitForSeconds(.3f); yield return Sample(); Contact();
        }
        [UnityTest] public IEnumerator BothMeleeWeaponsFollowCombatPhasesWithSingleDamageWindow()
        {
            yield return null;
            foreach (var id in new[] { MeleeWeaponDefinition.BladeId, MeleeWeaponDefinition.AxeId })
            {
                Assert.That(combat.SelectMeleeWeapon(id), Is.True); var melee = combat.Melee;
                probe.Fps = melee.GetComponent<FirstPersonWeaponGrip>();
                yield return new WaitForSeconds(.4f); yield return Sample(); Contact();
                int windows = 0; melee.Simulation.ActiveWindow += () => windows++;
                Assert.That(melee.TryAttack(), Is.True); Assert.That(melee.TryAttack(), Is.False);
                float end = Time.realtimeSinceStartup + 4;
                while (melee.Simulation.State != MeleeState.Ready)
                { Assert.That(Time.realtimeSinceStartup, Is.LessThan(end)); yield return Sample(); Contact(false); }
                Assert.That(windows, Is.EqualTo(1));
                yield return new WaitForSeconds(.3f); yield return Sample(); Contact();
                Assert.That(probe.World.ActiveProfile.Melee, Is.SameAs(melee.Definition));
            }
        }
        [UnityTest] public IEnumerator NewPlayerSessionDoesNotInheritReloadOrGripState()
        {
            yield return Pistol();
            var old = probe.Fps;
            player.GetComponent<PlayerInventory>().TryAdd(combat.ActiveWeapon.Definition.Ammunition, 5);
            combat.ActiveWeapon.OnReloadRequested(); yield return new WaitForSeconds(.5f);
            Object.Destroy(player); yield return null; Assert.That(old == null, Is.True);
            player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Player/Player.prefab"), new Vector3(500, 100.04f, 500), Quaternion.identity);
            player.AddComponent<PlayerInventory>().Initialize(24);
            input = player.GetComponent<PlayerInputReader>(); input.SetGameplay(true);
            combat = player.GetComponent<PlayerCombatController>();
            probe = player.AddComponent<WeaponGripProbe>(); probe.World = player.GetComponentInChildren<CharacterWeaponRig>();
            yield return Pistol(); Contact(); Assert.That(probe.Reload, Is.Zero); PistolSupportContact();
        }
        [UnityTest] public IEnumerator CrouchSprintSlidePitchAndRapidSwitchReleaseFinalPoseOnDeath()
        {
            yield return Pistol();
            var stance = player.GetComponent<PlayerStance>(); Assert.That(stance.TrySetCrouching(true), Is.True);
            yield return new WaitForSeconds(.3f); yield return Sample(); Contact(false);
            Assert.That(stance.TrySetCrouching(false), Is.True, "Fixture must stand before sprinting.");
            // Two full StateEvents queued before an input update overwrite each other.
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(Keyboard.current,
                new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.W, Key.LeftShift));
            yield return new WaitForSeconds(.4f); yield return Sample(); Contact(false);
            var motor = player.GetComponent<FirstPersonMotor>();
            Assert.That(motor.TryStartSlide(), Is.True,
                $"grounded={motor.Grounded}, sprint={motor.IsSprinting}, speed={motor.HorizontalMetersPerSecond}, move={input.Move}");
            yield return new WaitForSeconds(.15f); yield return Sample(); Contact(false);
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(Keyboard.current,
                new UnityEngine.InputSystem.LowLevel.KeyboardState());
            player.GetComponent<FirstPersonLook>().enabled = false;
            foreach (float pitch in new[] { -80f, 80f })
            {
                player.GetComponent<FirstPersonLook>().View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
                yield return Sample(); Contact(false);
            }
            for (int i = 0; i < 5; i++)
            { combat.SelectMeleeWeapon(MeleeWeaponDefinition.AxeId); combat.SelectFirearm("weapon.pistol"); yield return null; }
            yield return new WaitForSeconds(.5f); yield return Sample(); Contact(false);
            player.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo { Amount = 10000, Category = DamageCategory.Melee });
            yield return Sample(); Assert.That(probe.Fps.HasPose, Is.False);
            Assert.That(probe.World.WeaponVisual.activeSelf, Is.False);
        }
    }
}
#endif
