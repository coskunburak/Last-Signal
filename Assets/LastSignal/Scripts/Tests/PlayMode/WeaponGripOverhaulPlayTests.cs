#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace LastSignal.Tests
{
    public sealed partial class WeaponGripCorrectionPlayTests
    {
        [UnityTest] public IEnumerator PistolLowRaisedReadyAndReturnKeepAuthoredHandContacts()
        {
            yield return Pistol();
            var view = combat.ActiveWeapon.GetComponent<WeaponViewPresenter>();
            Assert.That(view.ReadyWeight, Is.Zero); Assert.That(probe.World.ReadyWeight, Is.Zero); Contact();
            Assert.That(probe.WristBend, Is.LessThan(85), "Neutral wrist must not hyperextend.");
            combat.ActiveWeapon.OnFirePressed(); combat.ActiveWeapon.OnFireReleased();
            yield return new WaitForSeconds(.3f); yield return Sample();
            Assert.That(view.ReadyWeight, Is.GreaterThan(.95f));
            Assert.That(probe.World.ReadyWeight, Is.GreaterThan(.95f));
            PistolSupportContact(); Contact();
            input.SetGameplay(false); Time.timeScale = 0;
            float ready = view.ReadyWeight; yield return Sample(); yield return Sample();
            Assert.That(view.ReadyWeight, Is.EqualTo(ready));
            Time.timeScale = 1; input.SetGameplay(true);
            yield return new WaitForSeconds(probe.Fps.Profile.ReadyHoldSeconds + .4f); yield return Sample();
            Assert.That(view.ReadyWeight, Is.Zero); Assert.That(probe.World.ReadyWeight, Is.Zero); Contact();
        }
        [UnityTest] public IEnumerator AnatomicalContactsAndSilhouettesSurviveFovAndAspectChanges()
        {
            yield return Pistol();
            var look = player.GetComponent<FirstPersonLook>(); var camera = look.View;
            float oldAspect = camera.aspect, oldFov = look.BaseFOV;
            try
            {
                foreach (var id in new[] { "weapon.pistol", MeleeWeaponDefinition.BladeId, MeleeWeaponDefinition.AxeId })
                {
                    if (id == "weapon.pistol") combat.SelectFirearm(id); else combat.SelectMeleeWeapon(id);
                    probe.Fps = id == "weapon.pistol" ? combat.ActiveWeapon.GetComponent<FirstPersonWeaponGrip>() : combat.Melee.GetComponent<FirstPersonWeaponGrip>();
                    yield return new WaitForSeconds(.5f);
                    foreach (float aspect in new[] { 16f / 9, 16f / 10, 21f / 9 })
                    foreach (float fov in new[] { 60f, 75f, 100f })
                    {
                        camera.aspect = aspect; look.SetBaseFOV(fov);
                        yield return Sample(); yield return Sample(); Contact();
                        // Coroutines resume before LateUpdate, after the grip's Update restoration.
                        // Read the same final presentation sample as the contact measurements.
                        string context = $"{id}, FOV={probe.SampledFov}, aspect={probe.SampledAspect}";
                        Assert.That(probe.SampledFov, Is.EqualTo(fov).Within(.01f), context);
                        Assert.That(probe.SampledAspect, Is.EqualTo(aspect).Within(.001f), context);
                        Assert.That(probe.WristBend, Is.LessThan(85), context);
                        var point = probe.SilhouetteViewport;
                        Assert.That(point.z, Is.GreaterThan(camera.nearClipPlane), context);
                        Assert.That(point.x, Is.InRange(.50f, .98f), id + " silhouette should remain on the right.");
                        Assert.That(point.y, Is.InRange(.02f, .98f), id + " silhouette should remain visible.");
                        if (id == MeleeWeaponDefinition.AxeId) Assert.That(point.y, Is.GreaterThan(.5f), "Axe head should be above view centre.");
                        if (id == "weapon.pistol") Assert.That(point.y, Is.LessThan(.5f), "Exploration pistol should be low-ready.");
                    }
                }
            }
            finally { camera.aspect = oldAspect; look.SetBaseFOV(oldFov); }
        }
        [UnityTest] public IEnumerator HeavyFollowThroughKeepsReachWithinAuthoredBudget()
        {
            yield return null; combat.SelectMeleeWeapon(MeleeWeaponDefinition.AxeId);
            var melee = combat.Melee; probe.Fps = melee.GetComponent<FirstPersonWeaponGrip>();
            yield return new WaitForSeconds(.5f); Assert.That(melee.TryAttack(), Is.True);
            float deadline = Time.realtimeSinceStartup + 4;
            while (melee.Simulation.State != MeleeState.Ready)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
                yield return Sample(); Contact(false);
                Assert.That(probe.Residual, Is.LessThan(.035f),
                    $"Contact clamping must not conceal an unreachable heavy swing: state={melee.Simulation.State}, progress={melee.Simulation.PresentationProgress}.");
            }
        }
    }
}
#endif
