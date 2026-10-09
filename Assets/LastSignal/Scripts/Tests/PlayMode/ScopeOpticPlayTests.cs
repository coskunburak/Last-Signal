using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    // Mevcut fixture sahne açma, oturum başlatma, input ve temizleme yolunu kullanır.
    public sealed class ScopeOpticPlayTests
    {
        CombatAcceptanceTests fixture;
        [SetUp] public void Setup() { fixture = new CombatAcceptanceTests(); fixture.Setup(); }
        [TearDown] public void TearDown() { fixture.TearDown(); }
        void SetAim(bool held)
        {
            // Fare düğmeleri bit alanıdır; fixture kontrolün tam durumunu güvenle yazar.
            if (held) fixture.Press(fixture.TestMouse.rightButton);
            else fixture.Release(fixture.TestMouse.rightButton);
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator AdsAllocatesOnceAndDoesNotChangeAmmo()
        {
            yield return fixture.LoadCombatScene();
            var combat = Object.FindAnyObjectByType<PlayerCombatController>();
            var weapon = combat.ActiveWeapon;
            var optic = weapon.GetComponent<ScopeOpticPresenter>();
            Assert.That(optic, Is.Not.Null);
            int ammo = weapon.RuntimeState.TotalAmmo;
            SetAim(true);
            yield return CombatAcceptanceTests.WaitForGameplaySeconds(.6f);
            Assert.That(combat.GetComponent<PlayerInputReader>().AimHeld, Is.True, "Native mouse aim must reach the reader before optic assertions.");
            Assert.That(optic.HasAllocatedResources, Is.True);
            Assert.That(optic.ScopeCamera.enabled, Is.True);
            Assert.That(optic.Target.IsCreated(), Is.True);
            Assert.That(optic.ScopeCamera.cullingMask & (1 << 29), Is.Zero);
            Assert.That(optic.Visibility, Is.GreaterThan(.9f));
            var rt = optic.Target;
            SetAim(false);
            yield return CombatAcceptanceTests.WaitForGameplaySeconds(.6f);
            Assert.That(optic.ScopeCamera.enabled, Is.False);
            SetAim(true);
            yield return CombatAcceptanceTests.WaitForGameplaySeconds(.6f);
            Assert.That(optic.Target, Is.SameAs(rt));
            Assert.That(weapon.RuntimeState.TotalAmmo, Is.EqualTo(ammo));
            SetAim(false);
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator HeldAimResumesWhenCombatControllerReenables()
        {
            yield return fixture.LoadCombatScene();
            var combat = Object.FindAnyObjectByType<PlayerCombatController>();
            var optic = combat.ActiveWeapon.GetComponent<ScopeOpticPresenter>();
            combat.enabled = false;
            SetAim(true);
            yield return null;
            combat.enabled = true;
            yield return CombatAcceptanceTests.WaitForGameplaySeconds(.6f);
            Assert.That(combat.GetComponent<PlayerInputReader>().AimHeld, Is.True, "Native mouse aim must reach the reader before optic assertions.");
            Assert.That(optic.Visibility, Is.GreaterThan(.9f));
            Assert.That(optic.ScopeCamera.enabled, Is.True);
            SetAim(false);
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator UnequipReleasesScopeCameraAndRenderTexture()
        {
            yield return fixture.LoadCombatScene();
            var combat = Object.FindAnyObjectByType<PlayerCombatController>();
            var optic = combat.ActiveWeapon.GetComponent<ScopeOpticPresenter>();
            SetAim(true);
            yield return CombatAcceptanceTests.WaitForGameplaySeconds(.6f);
            Assert.That(combat.GetComponent<PlayerInputReader>().AimHeld, Is.True, "Native mouse aim must reach the reader before optic assertions.");
            Assert.That(optic.HasAllocatedResources, Is.True);
            var camera = optic.ScopeCamera;
            var rt = optic.Target;
            combat.UnequipWeapon();
            yield return null;
            Assert.That(camera == null, Is.True);
            Assert.That(rt == null, Is.True);
            SetAim(false);
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator AcceptedShotConsumesOneRoundAndReloadHidesScope()
        {
            yield return fixture.LoadCombatScene();
            var combat = Object.FindAnyObjectByType<PlayerCombatController>();
            var weapon = combat.ActiveWeapon;
            var optic = weapon.GetComponent<ScopeOpticPresenter>();
            // Oturum başlangıç envanteri yedek mermi garantilemez; reload önkoşulunu açıkça kur.
            var inventory = combat.GetComponent<LastSignal.Inventory.PlayerInventory>();
            Assert.That(inventory, Is.Not.Null);
            inventory.TryAdd(weapon.Definition.Ammunition, 1);
            Assert.That(weapon.RuntimeState.ReserveAmmo, Is.GreaterThan(0));
            SetAim(true);
            yield return CombatAcceptanceTests.WaitForGameplaySeconds(.6f);
            Assert.That(combat.GetComponent<PlayerInputReader>().AimHeld, Is.True, "Native mouse aim must reach the reader before optic assertions.");
            int magazine = weapon.RuntimeState.CurrentMagazine;
            int total = weapon.RuntimeState.TotalAmmo;
            weapon.OnFirePressed();
            weapon.OnFireReleased();
            Assert.That(weapon.RuntimeState.CurrentMagazine, Is.EqualTo(magazine - 1));
            yield return CombatAcceptanceTests.WaitForGameplaySeconds(.2f);
            weapon.OnReloadRequested();
            Assert.That(weapon.RuntimeState.State, Is.EqualTo(WeaponState.Reloading));
            yield return CombatAcceptanceTests.WaitForGameplaySeconds(.25f);
            Assert.That(optic.ScopeCamera.enabled, Is.False);
            Assert.That(optic.Visibility, Is.Zero.Within(.001f));
            yield return CombatAcceptanceTests.WaitForGameplaySeconds(weapon.Definition.TacticalReloadSeconds + .6f);
            Assert.That(weapon.RuntimeState.State, Is.EqualTo(WeaponState.Ready));
            Assert.That(optic.ScopeCamera.enabled, Is.True);
            Assert.That(weapon.RuntimeState.TotalAmmo, Is.EqualTo(total - 1));
            SetAim(false);
        }
    }
}
