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
    public sealed class MeleeGripPlayTests
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
            floor = new GameObject("Melee grip floor"); floor.transform.position = new Vector3(500, 99.75f, 500);
            floor.AddComponent<BoxCollider>().size = new Vector3(50, .5f, 50);
            CreatePlayer(); Physics.SyncTransforms();
        }
        void CreatePlayer()
        {
            player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Player/Player.prefab"), new Vector3(500, 100.04f, 500), Quaternion.identity);
            player.AddComponent<PlayerInventory>().Initialize(24);
            input = player.GetComponent<PlayerInputReader>(); input.SetGameplay(true);
            combat = player.GetComponent<PlayerCombatController>();
            probe = player.AddComponent<WeaponGripProbe>(); probe.World = player.GetComponentInChildren<CharacterWeaponRig>();
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
            Assert.That(probe.Samples, Is.GreaterThan(before));
        }
        IEnumerator Equip(bool axe)
        {
            Assert.That(combat.SelectMeleeWeapon(axe ? MeleeWeaponDefinition.AxeId : MeleeWeaponDefinition.BladeId), Is.True);
            probe.Fps = combat.Melee.GetComponent<FirstPersonWeaponGrip>();
            yield return new WaitForSeconds(.5f); yield return Sample(); Contact(axe);
            if (axe) VisibleHands();
        }
        void VisibleHands()
        {
            foreach (var point in new[] { probe.PrimaryPalmViewport, probe.SecondaryPalmViewport })
            {
                Assert.That(point.z, Is.GreaterThan(.05f), "Palm must be ahead of camera");
                Assert.That(point.x, Is.InRange(.04f, .96f), "Ready palm horizontal framing");
                Assert.That(point.y, Is.InRange(.04f, .96f), "Ready palm vertical framing");
            }
        }
        void BodyClearance()
        {
            Assert.That(probe.WorldRightPenetration, Is.LessThan(.003f), "Right arm intersects jacket envelope: " + probe.WorldAnatomy);
            if (probe.World.SupportHandWeight > .95f)
                Assert.That(probe.WorldLeftPenetration, Is.LessThan(.003f), "Left arm intersects jacket envelope: " + probe.WorldAnatomy);
        }
        void Contact(bool axe)
        {
            Assert.That(probe.Active, Is.True);
            BodyClearance();
            Assert.That(probe.World.ActiveProfile, Is.SameAs(probe.World.ResolvePresentation(probe.Fps.Profile)));
            Assert.That(probe.FpsError, Is.LessThan(.02f), "FPS right palm");
            Assert.That(probe.WorldError, Is.LessThan(.02f), "Active character right palm");
            Assert.That(probe.Residual, Is.LessThan(.02f), "FPS requested wrist reach");
            Assert.That(probe.WorldRightResidual, Is.LessThan(.02f), "Active character requested wrist reach");
            Assert.That(float.IsFinite(probe.WristBend), Is.True);
            if (axe)
            {
                Assert.That(probe.Support, Is.EqualTo(1));
                Assert.That(probe.FpsSecondaryError, Is.LessThan(.02f), "FPS left palm");
                Assert.That(probe.WorldSecondaryError, Is.LessThan(.02f),
                    $"Active character left palm; reach={probe.WorldSecondaryResidual:F4} m, shared-root adjustment={probe.World.MeleePlacementCorrection:F4} m");
                Assert.That(probe.FpsSecondaryResidual, Is.LessThan(.02f));
                Assert.That(probe.WorldSecondaryResidual, Is.LessThan(.02f));
                Assert.That(probe.World.MeleePlacementCorrection, Is.LessThanOrEqualTo(.1001f));
            }
            else
            {
                Assert.That(probe.Support, Is.Zero); Assert.That(probe.Reload, Is.Zero);
                Assert.That(probe.Fps.Sockets.SecondaryGrip, Is.Null);
                Assert.That(Vector3.Distance(probe.SecondaryPalm, probe.PrimaryContact), Is.GreaterThan(.08f));
            }
        }
        [UnityTest] public IEnumerator BothWeaponsKeepContactThroughAuthoritativeSwingAndRecovery()
        {
            yield return null;
            foreach (bool axe in new[] { true, false })
            {
                yield return Equip(axe);
                var start = probe.VisualLocalPosition; var rotation = probe.VisualLocalRotation;
                int windows = 0; System.Action onWindow = () => windows++;
                var simulation = combat.Melee.Simulation; simulation.ActiveWindow += onWindow;
                try
                {
                    Assert.That(combat.Melee.TryAttack(), Is.True); Assert.That(combat.Melee.TryAttack(), Is.False);
                    float deadline = Time.realtimeSinceStartup + 5;
                    while (simulation.State != MeleeState.Ready)
                    {
                        Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
                        yield return Sample(); Contact(axe);
                        Assert.That(probe.World.GetComponent<Animator>().GetBool("MeleeActive"), Is.False, "Legacy crowbar swing must not compete with procedural melee.");
                    }
                    Assert.That(windows, Is.EqualTo(1));
                    yield return new WaitForSeconds(1.5f); yield return Sample(); Contact(axe);
                    Assert.That(Vector3.Distance(start, probe.VisualLocalPosition), Is.LessThan(.002f));
                    Assert.That(Quaternion.Angle(rotation, probe.VisualLocalRotation), Is.LessThan(.5f));
                }
                finally { simulation.ActiveWindow -= onWindow; }
            }
        }
        [UnityTest] public IEnumerator StancePitchFovAndPauseKeepTwoContacts()
        {
            yield return null; yield return Equip(true);
            var stance = player.GetComponent<PlayerStance>();
            Assert.That(stance.TrySetCrouching(true), Is.True);
            yield return new WaitForSeconds(.3f); yield return Sample(); Contact(true);
            Assert.That(stance.TrySetCrouching(false), Is.True);
            InputSystem.QueueStateEvent(Keyboard.current, new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.W, Key.LeftShift));
            yield return new WaitForSeconds(.4f); yield return Sample(); Contact(true);
            Assert.That(player.GetComponent<FirstPersonMotor>().TryStartSlide(), Is.True);
            yield return new WaitForSeconds(.15f); yield return Sample(); Contact(true);
            InputSystem.QueueStateEvent(Keyboard.current, new UnityEngine.InputSystem.LowLevel.KeyboardState());
            yield return new WaitForSeconds(1);
            var look = player.GetComponent<FirstPersonLook>(); look.enabled = false;
            foreach (float fov in new[] { 60f, 75f, 100f }) foreach (float aspect in new[] { 4f / 3f, 16f / 9f, 21f / 9f })
            {
                look.View.fieldOfView = fov; look.View.aspect = aspect;
                foreach (float pitch in new[] { -80f, 0f, 80f })
                {
                    look.View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
                    yield return new WaitForSeconds(.4f); yield return Sample(); Contact(true); VisibleHands();
                }
            }
            input.SetGameplay(false); Time.timeScale = 0; yield return Sample();
            var palm = probe.Palm;
            for (int i = 0; i < 8; i++) { yield return Sample(); Contact(true); Assert.That(Vector3.Distance(palm, probe.Palm), Is.LessThan(.001f)); }
            Time.timeScale = 1; input.SetGameplay(true);
        }
        [UnityTest] public IEnumerator AssaultArmsRemainOutsideJacketThroughStanceAndLook()
        {
            yield return null;
            Assert.That(combat.SelectFirearm("weapon.rifle"), Is.True);
            yield return new WaitForSeconds(1);
            var look = player.GetComponent<FirstPersonLook>(); look.enabled = false;
            foreach (bool crouched in new[] { false, true })
            {
                Assert.That(player.GetComponent<PlayerStance>().TrySetCrouching(crouched), Is.True);
                foreach (float pitch in new[] { -55f, 0f, 55f })
                {
                    look.View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
                    yield return new WaitForSeconds(.5f); yield return Sample();
                    BodyClearance();
                    Assert.That(probe.WorldHipClearanceDegrees, Is.LessThanOrEqualTo(12.001f), probe.WorldAnatomy);
                    Assert.That(probe.WorldStockError, Is.LessThan(.001f), probe.WorldAnatomy);
                    Assert.That(probe.WorldError, Is.LessThan(.02f), probe.WorldAnatomy);
                    Assert.That(probe.WorldSecondaryError, Is.LessThan(.02f), probe.WorldAnatomy);
                }
            }
        }
        [UnityTest] public IEnumerator SwitchingRecoveryDisableDeathAndRestartReleaseConstraints()
        {
            yield return null;
            for (int i = 0; i < 3; i++)
            {
                yield return Equip(true); Assert.That(combat.Melee.TryAttack(), Is.True);
                yield return new WaitForSeconds(combat.Melee.Definition.WindupSeconds + combat.Melee.Definition.ActiveSeconds + .02f);
                var old = probe.Fps; yield return Equip(false); Assert.That(old.HasPose, Is.False);
                yield return Equip(true);
                Assert.That(combat.SelectFirearm("weapon.pistol"), Is.True);
                yield return new WaitForSeconds(.7f); yield return Sample();
                // Current character profile intentionally supports the pistol with both hands.
                Assert.That(probe.World.SupportHandWeight, probe.World.ActiveProfile.WorldTwoHandedPistol ? Is.GreaterThan(.95f) : Is.Zero);
                yield return Equip(false);
                Assert.That(combat.SelectFirearm("weapon.rifle"), Is.True);
                yield return new WaitForSeconds(.7f); yield return Sample();
                Assert.That(probe.World.ActiveProfile.TwoHandedMelee, Is.False);
            }
            yield return Equip(true);
            combat.SetVehicleHolstered(true); yield return Sample(); Assert.That(probe.Fps.HasPose, Is.False);
            Assert.That(probe.World.WeaponVisual.activeSelf, Is.False);
            combat.SetVehicleHolstered(false); yield return new WaitForSeconds(.5f); yield return Sample(); Contact(true);
            probe.Fps.enabled = false; yield return Sample(); Assert.That(probe.Fps.HasPose, Is.False);
            probe.Fps.enabled = true; yield return new WaitForSeconds(.5f); yield return Sample(); Contact(true);
            player.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo { Amount = 10000, Category = DamageCategory.Melee });
            yield return Sample(); Assert.That(probe.Active, Is.False); Assert.That(probe.World.WeaponVisual.activeSelf, Is.False);
            Object.Destroy(player); yield return null;
            CreatePlayer(); yield return null; yield return Equip(false); yield return Equip(true);
        }
    }
}
#endif
