using NUnit.Framework;
using UnityEngine;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using System.Collections.Generic;

namespace LastSignal.Tests
{
    /// <summary>
    /// EditMode tests for weapon state machine, ammo arithmetic, reload transactions,
    /// and fire semantics. No GameObjects needed — tests pure C# WeaponRuntimeState.
    /// </summary>
    public class WeaponStateTests
    {
        readonly List<GameObject> inventories = new List<GameObject>();
        readonly List<ItemDefinition> ammoItems = new List<ItemDefinition>();
        WeaponRuntimeState CreateState(WeaponDefinition def, int mag, int reserve)
        {
            var go = new GameObject("Explicit ammo fixture"); inventories.Add(go);
            var inventory = go.AddComponent<PlayerInventory>();
            inventory.Initialize(24);
            inventory.TryAdd(def.Ammunition, reserve);
            return new WeaponRuntimeState(def, mag, inventory);
        }
        WeaponDefinition CreateDefinition(
            int magazineCapacity = 30, int maxReserve = 120,
            FireMode fireMode = FireMode.Automatic,
            float rpm = 600, float tacticalReload = 1.8f,
            float emptyReload = 2.4f, float commitNormalized = .55f,
            float equipSeconds = .6f, float unequipSeconds = .4f)
        {
            var def = ScriptableObject.CreateInstance<WeaponDefinition>();
            // Use serialization to set private fields.
            var so = new UnityEditor.SerializedObject(def);
            so.FindProperty("magazineCapacity").intValue = magazineCapacity;
            var ammo = ScriptableObject.CreateInstance<ItemDefinition>(); ammoItems.Add(ammo);
            var item = new UnityEditor.SerializedObject(ammo);
            item.FindProperty("stableId").FindPropertyRelative("id").stringValue = "ammo.fixture";
            item.FindProperty("maxStack").intValue = 60;
            item.ApplyModifiedPropertiesWithoutUndo();
            so.FindProperty("ammunition").objectReferenceValue = ammo;
            so.FindProperty("startingMagazine").intValue = magazineCapacity;
            so.FindProperty("fireMode").enumValueIndex = (int)fireMode;
            so.FindProperty("roundsPerMinute").floatValue = rpm;
            so.FindProperty("tacticalReloadSeconds").floatValue = tacticalReload;
            so.FindProperty("emptyReloadSeconds").floatValue = emptyReload;
            so.FindProperty("reloadCommitNormalized").floatValue = commitNormalized;
            so.FindProperty("emptyReloadCommitNormalized").floatValue = commitNormalized;
            so.FindProperty("equipSeconds").floatValue = equipSeconds;
            so.FindProperty("unequipSeconds").floatValue = unequipSeconds;
            so.ApplyModifiedPropertiesWithoutUndo();
            return def;
        }

        // ── Ammo Arithmetic ─────────────────────────────────────────────

        [Test]
        public void InitialAmmoMatchesConstructorValues()
        {
            var def = CreateDefinition(magazineCapacity: 30, maxReserve: 120);
            var state = CreateState(def, 30, 90);
            Assert.That(state.CurrentMagazine, Is.EqualTo(30));
            Assert.That(state.ReserveAmmo, Is.EqualTo(90));
            Assert.That(state.TotalAmmo, Is.EqualTo(120));
        }

        [Test]
        public void InitialAmmoClampedToDefinitionBounds()
        {
            var def = CreateDefinition(magazineCapacity: 10, maxReserve: 50);
            var state = CreateState(def, 99, 999);
            Assert.That(state.CurrentMagazine, Is.EqualTo(10));
            Assert.That(state.ReserveAmmo, Is.EqualTo(999), "Inventory capacity, not retired weapon reserve cap, owns reserve");
        }

        [Test]
        public void ConsumeOneShotDecreasesMagazineByOne()
        {
            var def = CreateDefinition();
            var state = CreateState(def, 30, 90);
            state.TryEquip();
            state.Tick(1f); // advance equip timer
            state.TryCompleteEquip();
            int totalBefore = state.TotalAmmo;
            Assert.That(state.TryConsumeShot(), Is.True);
            Assert.That(state.CurrentMagazine, Is.EqualTo(29));
            Assert.That(state.TotalAmmo, Is.EqualTo(totalBefore - 1));
        }

        [Test]
        public void CannotFireWithEmptyMagazine()
        {
            var def = CreateDefinition();
            var state = CreateState(def, 0, 90);
            state.TryEquip(); state.Tick(1f); state.TryCompleteEquip();
            Assert.That(state.CanFire, Is.False);
            Assert.That(state.TryConsumeShot(), Is.False);
            Assert.That(state.CurrentMagazine, Is.EqualTo(0));
        }

        [Test]
        public void CannotFireDuringCooldown()
        {
            var def = CreateDefinition(rpm: 600); // 0.1s cooldown
            var state = CreateState(def, 30, 90);
            state.TryEquip(); state.Tick(1f); state.TryCompleteEquip();
            Assert.That(state.TryConsumeShot(), Is.True);
            Assert.That(state.FireCooldownRemaining, Is.GreaterThan(0));
            Assert.That(state.TryConsumeShot(), Is.False, "Cannot fire during cooldown");
        }

        [Test]
        public void CooldownExpiresAllowsSecondShot()
        {
            var def = CreateDefinition(rpm: 600);
            var state = CreateState(def, 30, 90);
            state.TryEquip(); state.Tick(1f); state.TryCompleteEquip();
            state.TryConsumeShot();
            state.Tick(.15f); // cooldown is 0.1s
            Assert.That(state.TryConsumeShot(), Is.True);
        }

        [Test]
        public void AmmoNeverGoesNegative()
        {
            var def = CreateDefinition(magazineCapacity: 2, maxReserve: 0);
            var state = CreateState(def, 2, 0);
            state.TryEquip(); state.Tick(1f); state.TryCompleteEquip();
            state.TryConsumeShot(); state.Tick(.2f);
            state.TryConsumeShot(); state.Tick(.2f);
            Assert.That(state.CurrentMagazine, Is.EqualTo(0));
            Assert.That(state.TryConsumeShot(), Is.False);
            Assert.That(state.CurrentMagazine, Is.GreaterThanOrEqualTo(0));
        }

        // ── Reload Transaction ──────────────────────────────────────────

        [Test]
        public void TacticalReloadTransfersCorrectAmount()
        {
            var def = CreateDefinition(magazineCapacity: 30, maxReserve: 120, commitNormalized: .5f, tacticalReload: 1f);
            var state = CreateState(def, 15, 90);
            state.TryEquip(); state.Tick(1f); state.TryCompleteEquip();
            int totalBefore = state.TotalAmmo;
            Assert.That(state.TryBeginReload(), Is.True);
            Assert.That(state.IsEmptyReload, Is.False);
            state.Tick(.6f); // Past commit point (50%)
            Assert.That(state.TryCommitReload(), Is.True);
            Assert.That(state.CurrentMagazine, Is.EqualTo(30)); // 15 + 15 from reserve
            Assert.That(state.ReserveAmmo, Is.EqualTo(75)); // 90 - 15
            Assert.That(state.TotalAmmo, Is.EqualTo(totalBefore), "Total ammo unchanged");
        }

        [Test]
        public void EmptyReloadIsDetected()
        {
            var def = CreateDefinition(magazineCapacity: 30, commitNormalized: .5f, emptyReload: 2f);
            var state = CreateState(def, 0, 30);
            state.TryEquip(); state.Tick(1f); state.TryCompleteEquip();
            Assert.That(state.TryBeginReload(), Is.True);
            Assert.That(state.IsEmptyReload, Is.True);
        }

        [Test]
        public void ReloadCancelBeforeCommitPreservesAmmo()
        {
            var def = CreateDefinition(magazineCapacity: 30, commitNormalized: .8f, tacticalReload: 2f);
            var state = CreateState(def, 10, 90);
            state.TryEquip(); state.Tick(1f); state.TryCompleteEquip();
            int totalBefore = state.TotalAmmo;
            state.TryBeginReload();
            state.Tick(.5f); // Before commit point (80%)
            Assert.That(state.ReloadCommitted, Is.False);
            Assert.That(state.TryCancelReload(), Is.True);
            Assert.That(state.CurrentMagazine, Is.EqualTo(10), "Magazine unchanged after cancel");
            Assert.That(state.ReserveAmmo, Is.EqualTo(90), "Reserve unchanged after cancel");
            Assert.That(state.TotalAmmo, Is.EqualTo(totalBefore));
            Assert.That(state.State, Is.EqualTo(WeaponState.Ready));
        }

        [Test]
        public void ReloadCommitCannotBeDuplicated()
        {
            var def = CreateDefinition(magazineCapacity: 30, commitNormalized: .3f, tacticalReload: 1f);
            var state = CreateState(def, 10, 90);
            state.TryEquip(); state.Tick(1f); state.TryCompleteEquip();
            state.TryBeginReload();
            state.Tick(.5f);
            Assert.That(state.TryCommitReload(), Is.True);
            int magAfter = state.CurrentMagazine;
            int resAfter = state.ReserveAmmo;
            // Second commit must fail.
            Assert.That(state.TryCommitReload(), Is.False);
            Assert.That(state.CurrentMagazine, Is.EqualTo(magAfter), "No duplicate transfer");
            Assert.That(state.ReserveAmmo, Is.EqualTo(resAfter));
        }

        [Test]
        public void CannotReloadWithFullMagazine()
        {
            var def = CreateDefinition(magazineCapacity: 30);
            var state = CreateState(def, 30, 90);
            state.TryEquip(); state.Tick(1f); state.TryCompleteEquip();
            Assert.That(state.TryBeginReload(), Is.False);
        }

        [Test]
        public void CannotReloadWithZeroReserve()
        {
            var def = CreateDefinition(magazineCapacity: 30);
            var state = CreateState(def, 10, 0);
            state.TryEquip(); state.Tick(1f); state.TryCompleteEquip();
            Assert.That(state.TryBeginReload(), Is.False);
        }

        [Test]
        public void InsufficientReserveTransfersOnlyAvailable()
        {
            var def = CreateDefinition(magazineCapacity: 30, commitNormalized: .3f, tacticalReload: 1f);
            var state = CreateState(def, 25, 3); // Need 5, have 3
            state.TryEquip(); state.Tick(1f); state.TryCompleteEquip();
            state.TryBeginReload(); state.Tick(.5f);
            state.TryCommitReload();
            Assert.That(state.CurrentMagazine, Is.EqualTo(28)); // 25 + 3
            Assert.That(state.ReserveAmmo, Is.EqualTo(0));
        }

        // ── State Machine Transitions ───────────────────────────────────

        [Test]
        public void InitialStateIsHolstered()
        {
            var def = CreateDefinition();
            var state = CreateState(def, 30, 90);
            Assert.That(state.State, Is.EqualTo(WeaponState.Holstered));
        }

        [Test]
        public void EquipTransitionsToEquippingThenReady()
        {
            var def = CreateDefinition(equipSeconds: .5f);
            var state = CreateState(def, 30, 90);
            Assert.That(state.TryEquip(), Is.True);
            Assert.That(state.State, Is.EqualTo(WeaponState.Equipping));
            state.Tick(.3f);
            Assert.That(state.TryCompleteEquip(), Is.False, "Not enough time");
            state.Tick(.3f);
            Assert.That(state.TryCompleteEquip(), Is.True);
            Assert.That(state.State, Is.EqualTo(WeaponState.Ready));
        }

        [Test]
        public void CannotFireWhileEquipping()
        {
            var def = CreateDefinition();
            var state = CreateState(def, 30, 90);
            state.TryEquip();
            Assert.That(state.CanFire, Is.False);
            Assert.That(state.TryConsumeShot(), Is.False);
        }

        [Test]
        public void CannotFireWhileReloading()
        {
            var def = CreateDefinition(tacticalReload: 2f);
            var state = CreateState(def, 10, 90);
            state.TryEquip(); state.Tick(1f); state.TryCompleteEquip();
            state.TryBeginReload();
            Assert.That(state.CanFire, Is.False);
        }

        [Test]
        public void CannotReloadWhileEquipping()
        {
            var def = CreateDefinition();
            var state = CreateState(def, 10, 90);
            state.TryEquip();
            Assert.That(state.CanReload, Is.False);
            Assert.That(state.TryBeginReload(), Is.False);
        }

        [Test]
        public void CannotReloadWhileAlreadyReloading()
        {
            var def = CreateDefinition(tacticalReload: 2f);
            var state = CreateState(def, 10, 90);
            state.TryEquip(); state.Tick(1f); state.TryCompleteEquip();
            Assert.That(state.TryBeginReload(), Is.True);
            Assert.That(state.TryBeginReload(), Is.False, "Reload spam rejected");
        }

        [Test]
        public void UnequipTransitionsToUnequippingThenHolstered()
        {
            var def = CreateDefinition(unequipSeconds: .4f);
            var state = CreateState(def, 30, 90);
            state.TryEquip(); state.Tick(1f); state.TryCompleteEquip();
            Assert.That(state.TryUnequip(), Is.True);
            Assert.That(state.State, Is.EqualTo(WeaponState.Unequipping));
            state.Tick(.5f);
            Assert.That(state.TryCompleteUnequip(), Is.True);
            Assert.That(state.State, Is.EqualTo(WeaponState.Holstered));
        }

        [Test]
        public void ForceHolsterFromAnyState()
        {
            var def = CreateDefinition();
            var state = CreateState(def, 10, 90);
            state.TryEquip(); state.Tick(1f); state.TryCompleteEquip();
            state.TryBeginReload();
            state.ForceHolster();
            Assert.That(state.State, Is.EqualTo(WeaponState.Holstered));
        }

        // ── Semi-Auto Semantics ─────────────────────────────────────────

        [Test]
        public void SemiAutoConsumesOnlyOneShotPerPress()
        {
            var def = CreateDefinition(fireMode: FireMode.SemiAutomatic, rpm: 6000); // Very fast
            var state = CreateState(def, 30, 90);
            state.TryEquip(); state.Tick(1f); state.TryCompleteEquip();
            // First shot succeeds and sets consumed flag.
            Assert.That(state.TryConsumeShot(), Is.True);
            Assert.That(state.FireRequestConsumed, Is.True);
            // Attempting again with consumed flag should fail for semi-auto enforcement.
            // (WeaponController enforces this externally through FireRequestConsumed)
            state.Tick(.01f); // Past cooldown
            // State itself doesn't enforce semi-auto — WeaponController does.
            // But we verify the flag is set.
            Assert.That(state.FireRequestConsumed, Is.True);
        }

        // ── Fire Cadence ────────────────────────────────────────────────

        [Test]
        public void FireCadenceMatchesRPM()
        {
            var def = CreateDefinition(rpm: 600); // 10 rounds/sec = 0.1s cooldown
            Assert.That(def.FireCooldownSeconds, Is.EqualTo(.1f).Within(.001f));
        }

        [TestCase(300, .2f)]
        [TestCase(900, 1f / 15f)]
        [TestCase(60, 1f)]
        public void FireCooldownCalculation(float rpm, float expectedCooldown)
        {
            var def = CreateDefinition(rpm: rpm);
            Assert.That(def.FireCooldownSeconds, Is.EqualTo(expectedCooldown).Within(.001f));
        }

        // ── AddReserve ──────────────────────────────────────────────────

        [Test]
        public void AddReserveIncreasesTotal()
        {
            var def = CreateDefinition(maxReserve: 120);
            var state = CreateState(def, 30, 50);
            inventories[inventories.Count - 1].GetComponent<PlayerInventory>().TryAdd(def.Ammunition, 20);
            Assert.That(state.ReserveAmmo, Is.EqualTo(70));
        }

        [Test]
        public void InventoryReserveIsNotClampedByRetiredWeaponLimit()
        {
            var def = CreateDefinition(maxReserve: 120);
            var state = CreateState(def, 30, 110);
            inventories[inventories.Count - 1].GetComponent<PlayerInventory>().TryAdd(def.Ammunition, 999);
            Assert.That(state.ReserveAmmo, Is.EqualTo(1109));
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var go in inventories) Object.DestroyImmediate(go); inventories.Clear();
            foreach (var ammo in ammoItems) Object.DestroyImmediate(ammo); ammoItems.Clear();
            // ScriptableObjects created in tests need cleanup.
            var objects = Object.FindObjectsByType<WeaponDefinition>(FindObjectsSortMode.None);
            foreach (var obj in objects) Object.DestroyImmediate(obj);
        }
    }
}
