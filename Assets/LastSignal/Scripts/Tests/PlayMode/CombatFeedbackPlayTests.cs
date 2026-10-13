#if UNITY_EDITOR
using System.Collections;
using LastSignal.Inventory;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    public sealed class CombatFeedbackPlayTests
    {
        InputFixtureIsolation.SceneScope sceneScope;
        GameObject actor, shooter, weaponObject, wall;
        WeaponController weapon;
        Transform view;
        ZombieHealth health;
        ZombieBloodVfxPool blood;
        WeaponFireResolver.ShotResult shot;
        int fired, rejected;
        bool originalGore, originalReduced;
        readonly Vector3 origin = new Vector3(5000, 100, 5000);
        [UnitySetUp] public IEnumerator Setup()
        {
            sceneScope = new InputFixtureIsolation.SceneScope();
            originalGore = ZombieGorePreference.GraphicGoreEnabled; originalReduced = CombatPresentationPreference.ReducedEffects;
            ZombieGorePreference.SetGraphicGoreEnabled(true); CombatPresentationPreference.SetReducedEffects(false); Time.timeScale = 1;
            actor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Resources/LS_Zombie_Runtime.prefab"), origin, Quaternion.identity);
            health = actor.GetComponent<ZombieHealth>(); blood = ZombieBloodVfxPool.Warm(actor.GetComponent<ZombieBloodVfxPresenter>().Profile);
            if (blood) blood.gameObject.SetActive(true);
            fired = 0; rejected = 0;
            shooter = new GameObject("Combat integration shooter"); shooter.AddComponent<PlayerInventory>().Initialize(24);
            view = new GameObject("Aim").transform; view.SetParent(shooter.transform); view.gameObject.AddComponent<Camera>(); view.gameObject.tag = "MainCamera";
            weaponObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Resources/Weapon_AssaultRifle.prefab"));
            weapon = weaponObject.GetComponent<WeaponController>();
            weapon.Initialize(view, shooter, 3); weapon.RequestEquip();
            weapon.ShotFired += result => { shot = result; fired++; }; weapon.ShotRejected += () => rejected++;
            float until = Time.realtimeSinceStartup + 3;
            while (weapon.RuntimeState.State != WeaponState.Ready && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(weapon.RuntimeState.State, Is.EqualTo(WeaponState.Ready));
        }
        [TearDown] public void Cleanup()
        {
            if (weaponObject) Object.DestroyImmediate(weaponObject);
            // Presenter destroys its world pool at end of frame; clean explicitly for synchronous isolation.
            foreach (var pool in Object.FindObjectsByType<BallisticFeedbackPool>(FindObjectsSortMode.None)) Object.DestroyImmediate(pool.gameObject);
            if (actor) Object.DestroyImmediate(actor); if (shooter) Object.DestroyImmediate(shooter); if (wall) Object.DestroyImmediate(wall);
            if (blood) Object.DestroyImmediate(blood.gameObject);
            var detached = Object.FindAnyObjectByType<ZombieDetachedPartPool>(); if (detached) Object.DestroyImmediate(detached.gameObject);
            ZombieGorePreference.SetGraphicGoreEnabled(originalGore); CombatPresentationPreference.SetReducedEffects(originalReduced); Time.timeScale = 1;
            sceneScope?.Dispose(); sceneScope = null;
        }
        ZombieHitRegion Region(ZombieBodyPart part)
        {
            foreach (var region in actor.GetComponentsInChildren<ZombieHitRegion>(true))
                if (region.BodyPart == part && (part != ZombieBodyPart.Torso || region.name == "Damage_Chest")) return region;
            Assert.Fail("Missing production region " + part); return null;
        }
        void Aim(ZombieBodyPart part)
        {
            var r = Region(part); Physics.SyncTransforms();
            // Shoot laterally at arms to avoid the chest occluding the chosen anatomical collider.
            Vector3 offset = part == ZombieBodyPart.LeftArm ? -Vector3.right * 3 : part == ZombieBodyPart.RightArm ? Vector3.right * 3 : Vector3.forward * 3;
            view.position = r.HitCollider.bounds.center + offset; view.LookAt(r.HitCollider.bounds.center);
            weaponObject.transform.rotation = view.rotation;
            weaponObject.transform.position += view.position + view.forward * .25f - weapon.Muzzle.position;
            shooter.transform.position = view.position; // restore child aim after moving owner
            view.position = r.HitCollider.bounds.center + offset; view.LookAt(r.HitCollider.bounds.center);
            Physics.SyncTransforms();
        }
        void Fire()
        {
            weapon.RuntimeState.Tick(weapon.Definition.FireCooldownSeconds + .01f);
            weapon.OnFirePressed(); weapon.OnFireReleased();
        }
        [UnityTest] public IEnumerator RealFirearmCommitsTorsoBloodWoundAndOneRound()
        {
            Aim(ZombieBodyPart.Torso); int before = weapon.RuntimeState.CurrentMagazine; Fire();
            Assert.That(shot.DamageCommitted); Assert.That(shot.Receipt.Hit.BodyPart, Is.EqualTo(ZombieBodyPart.Torso));
            Assert.That(health.DamageTransactions, Is.EqualTo(1)); Assert.That(weapon.RuntimeState.CurrentMagazine, Is.EqualTo(before - 1));
            Assert.That(blood.TotalHitRequests, Is.EqualTo(1)); Assert.That(blood.ActiveHits, Is.EqualTo(1));
            var wounds = actor.GetComponent<ZombieLocalizedWounds>(); Assert.That(wounds.WoundCount, Is.EqualTo(1));
            yield return null;
            bool visible = false;
            foreach (var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>()) if (skin.name == "Localized wound surface" && skin.enabled)
            { visible = true; Assert.That(skin.sharedMesh, Is.Not.Null); Assert.That(skin.bones.Length, Is.GreaterThan(0)); }
            Assert.That(visible, "A successful hit must enable a skin-following wound surface.");
        }
        [TestCase(ZombieBodyPart.LeftArm)] [TestCase(ZombieBodyPart.RightArm)]
        public void RealArmHitHasMatchingAnatomyAndLocalizedBlood(ZombieBodyPart part)
        {
            Aim(part); Fire();
            Assert.That(shot.DamageCommitted); Assert.That(shot.Receipt.Hit.BodyPart, Is.EqualTo(part));
            Assert.That(blood.TotalHitRequests, Is.EqualTo(1));
            Assert.That(actor.GetComponent<ZombieLocalizedWounds>().WoundCount, Is.EqualTo(1));
        }
        [Test] public void UnequippingRejectsFireWithoutPresentationOrAmmoChange()
        {
            Aim(ZombieBodyPart.Torso); int ammo = weapon.RuntimeState.CurrentMagazine;
            var ballistics = weapon.GetComponent<WeaponVfxPresenter>().Ballistics;
            int presented = ballistics.PresentedShots;
            weapon.RequestUnequip(); Fire();
            Assert.That(fired, Is.Zero); Assert.That(weapon.RuntimeState.CurrentMagazine, Is.EqualTo(ammo));
            Assert.That(ballistics.PresentedShots, Is.EqualTo(presented));
        }
        [UnityTest] public IEnumerator RemovingSupportReleasesBulletMark()
        {
            Aim(ZombieBodyPart.Torso);
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position = view.position + view.forward;
            wall.transform.localScale = new Vector3(2, 3, .1f); Physics.SyncTransforms(); Fire();
            var pool = weapon.GetComponent<WeaponVfxPresenter>().Ballistics; Assert.That(pool.ActiveMarks, Is.EqualTo(1));
            wall.SetActive(false); yield return null; Assert.That(pool.ActiveMarks, Is.Zero);
        }
        [UnityTest] public IEnumerator FatalHeadHitRetainsReceiptAndOrdinaryImpact()
        {
            Aim(ZombieBodyPart.Head); Fire(); yield return null; Aim(ZombieBodyPart.Head); Fire();
            Assert.That(shot.Receipt.Terminal && shot.DamageCommitted); Assert.That(health.IsAlive, Is.False);
            Assert.That(shot.Receipt.Hit.Region, Is.EqualTo(DamageRegion.Head)); Assert.That(blood.TotalHitRequests, Is.EqualTo(2));
        }
        [Test] public void MissAndEmptyTriggerNeverCreateFalseConfirmation()
        {
            view.position = origin + Vector3.up * 10; view.rotation = Quaternion.LookRotation(Vector3.up);
            weaponObject.transform.position += view.position - weapon.Muzzle.position;
            Fire(); Fire(); Fire(); int count = weapon.GetComponent<WeaponVfxPresenter>().Ballistics.PresentedShots;
            Fire(); Fire(); Assert.That(fired, Is.EqualTo(3)); Assert.That(rejected, Is.EqualTo(2));
            Assert.That(shot.Hit || shot.DamageCommitted, Is.False); Assert.That(health.DamageTransactions, Is.Zero);
            Assert.That(weapon.GetComponent<WeaponVfxPresenter>().Ballistics.PresentedShots, Is.EqualTo(count));
            Assert.That(blood.TotalHitRequests, Is.Zero); Assert.That(weapon.RuntimeState.CurrentMagazine, Is.Zero);
        }
        [Test] public void MuzzleWallImpactHasNoBloodAndCreatesSupportedEntry()
        {
            Aim(ZombieBodyPart.Torso);
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position = view.position + view.forward;
            wall.transform.localScale = new Vector3(2, 3, .1f); wall.AddComponent<ImpactSurface>().kind = ImpactSurfaceKind.Stone;
            Physics.SyncTransforms(); Fire();
            Assert.That(shot.Hit); Assert.That(shot.DamageCommitted, Is.False); Assert.That(shot.Collider, Is.EqualTo(wall.GetComponent<Collider>()));
            Assert.That(shot.Surface, Is.EqualTo(ImpactSurfaceKind.Stone)); Assert.That(blood.TotalHitRequests, Is.Zero);
            Assert.That(weapon.GetComponent<WeaponVfxPresenter>().Ballistics.ActiveMarks, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator TracerMissBudgetAndDisableCleanupAreBounded()
        {
            var pool = weapon.GetComponent<WeaponVfxPresenter>().Ballistics;
            var profile = AssetDatabase.LoadAssetAtPath<CombatFeedbackProfile>("Assets/LastSignal/Data/VFX/CombatFeedback.asset");
            for (int i = 0; i < 60; i++) pool.Present(new WeaponFireResolver.ShotResult { ShotId = (ulong)((i + 1) * profile.tracerEvery), SourcePosition = origin, EndPoint = origin + Vector3.forward * 15, Distance = 15 });
            Assert.That(pool.ActiveTracers, Is.EqualTo(profile.tracerCapacity)); Assert.That(health.DamageTransactions, Is.Zero);
            yield return new WaitForSeconds(.1f); Assert.That(pool.ActiveTracers, Is.Zero);
            weaponObject.SetActive(false); Assert.That(pool.ActiveMarks, Is.Zero); Assert.That(pool.ActiveTracers, Is.Zero);
        }
        [UnityTest] public IEnumerator GoreToggleOwnerDisableAndSessionResetClearWithoutReplay()
        {
            Aim(ZombieBodyPart.Torso); Fire(); var wounds = actor.GetComponent<ZombieLocalizedWounds>(); int transactions = health.DamageTransactions;
            ZombieGorePreference.SetGraphicGoreEnabled(false); Assert.That(blood.ActiveHits, Is.Zero); Assert.That(wounds.WoundCount, Is.Zero);
            ZombieGorePreference.SetGraphicGoreEnabled(true); Assert.That(blood.ActiveHits, Is.Zero); Assert.That(health.DamageTransactions, Is.EqualTo(transactions));
            Fire(); Assert.That(blood.ActiveHits, Is.GreaterThan(0)); actor.SetActive(false); Assert.That(blood.ActiveHits, Is.Zero);
            ZombieBloodVfxPool.ResetSession(); Assert.That(blood.ActiveMarks, Is.Zero); yield return null;
        }
        [Test] public void GoreOffPreservesDamageAndAmmunition()
        {
            ZombieGorePreference.SetGraphicGoreEnabled(false); Aim(ZombieBodyPart.Torso); Fire();
            Assert.That(shot.DamageCommitted); Assert.That(health.CurrentHealth, Is.EqualTo(100 - weapon.Definition.BaseDamage));
            Assert.That(weapon.RuntimeState.CurrentMagazine, Is.EqualTo(2)); Assert.That(blood.ActiveHits, Is.Zero);
        }
        [Test] public void HitBurstCapacityDoesNotEvictStumpBudgetOrApplyExtraDamage()
        {
            Aim(ZombieBodyPart.Torso); Fire(); int transactions = health.DamageTransactions;
            for (int i = 0; i < 60; i++) blood.PlayHit(actor.GetComponent<ZombieBloodVfxPresenter>(), shot.Receipt);
            Assert.That(blood.ActiveHits, Is.EqualTo(actor.GetComponent<ZombieBloodVfxPresenter>().Profile.hitCapacity));
            Assert.That(blood.ActiveEffects, Is.Zero); Assert.That(health.DamageTransactions, Is.EqualTo(transactions));
            ZombieBloodVfxPool.ResetSession(); Assert.That(blood.ActiveHits, Is.Zero);
        }
        [Test] public void DuplicatePresentationDoesNotConsumeRingAndRestoreKeepsWoundWithoutReplay()
        {
            Aim(ZombieBodyPart.Torso); Fire(); var wounds = actor.GetComponent<ZombieLocalizedWounds>();
            for (int i = 0; i < 30; i++) wounds.Present(shot.Receipt);
            Assert.That(wounds.WoundCount, Is.EqualTo(1));
            var sever = actor.GetComponent<ZombieDismemberment>(); sever.RestoreState(sever.CaptureState());
            Assert.That(wounds.WoundCount, Is.EqualTo(1)); Assert.That(blood.ActiveHits, Is.Zero);
        }
    }
}
#endif
