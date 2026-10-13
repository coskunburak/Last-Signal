using System.Collections.Generic;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Persistence;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class S021CombatTests
    {
        const string Data = "Assets/LastSignal/Data/Combat/S021/";
        readonly List<GameObject> objects = new List<GameObject>();

        [TearDown] public void TearDown()
        {
            foreach (var item in objects) if (item) Object.DestroyImmediate(item);
            objects.Clear();
        }

        T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }
        GameObject New(string name)
        {
            var value = new GameObject(name);
            objects.Add(value);
            return value;
        }
        static void Ready(WeaponRuntimeState state)
        {
            Assert.That(state.TryEquip(), Is.True);
            state.Tick(state.Definition.EquipSeconds + .01f);
            Assert.That(state.TryCompleteEquip(), Is.True);
        }

        [Test] public void AuthoredDefinitionsHaveDistinctStableIdsAmmoAndRoles()
        {
            var rifle = Load<WeaponDefinition>("Assets/LastSignal/Data/Combat/WeaponDefinition_AssaultRifle.asset");
            var pistol = Load<WeaponDefinition>(Data + "Weapon_TacticalPistol.asset");
            var crowbar = Load<MeleeWeaponDefinition>("Assets/LastSignal/Data/Combat/Crowbar/Crowbar.asset");
            var blade = Load<MeleeWeaponDefinition>(Data + "Machete.asset");
            var axe = Load<MeleeWeaponDefinition>(Data + "WoodenAxe.asset");
            Assert.That(rifle.HasValidAmmunitionConfiguration && pistol.HasValidAmmunitionConfiguration, Is.True);
            Assert.That(rifle.WeaponId, Is.EqualTo("weapon.rifle"));
            Assert.That(pistol.WeaponId, Is.EqualTo("weapon.pistol"));
            Assert.That(pistol.Category, Is.EqualTo(WeaponCategory.Pistol));
            Assert.That(pistol.FireMode, Is.EqualTo(FireMode.SemiAutomatic));
            Assert.That(pistol.Ammunition.Id.Value, Is.EqualTo("ammo.9mm"));
            Assert.That(rifle.Ammunition.Id, Is.Not.EqualTo(pistol.Ammunition.Id));
            Assert.That(new HashSet<string> { rifle.WeaponId, pistol.WeaponId, crowbar.WeaponId, blade.WeaponId, axe.WeaponId }.Count, Is.EqualTo(5));
            Assert.That(crowbar.Valid && blade.Valid && axe.Valid, Is.True);
            Assert.That(blade.WindupSeconds, Is.LessThan(crowbar.WindupSeconds));
            Assert.That(blade.StaminaCost, Is.LessThan(crowbar.StaminaCost));
            Assert.That(axe.Heavy, Is.True);
            Assert.That(axe.RecoverySeconds, Is.GreaterThan(crowbar.RecoverySeconds));
            Assert.That(axe.StaminaCost, Is.GreaterThan(crowbar.StaminaCost));
        }

        [Test] public void InvalidWeaponConfigurationIsRejectedBeforeInstantiation()
        {
            var source = Load<WeaponDefinition>(Data + "Weapon_TacticalPistol.asset");
            var invalid = Object.Instantiate(source);
            try
            {
                var serialized = new SerializedObject(invalid);
                serialized.FindProperty("roundsPerMinute").floatValue = 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(invalid.HasValidCombatConfiguration, Is.False);
                serialized.FindProperty("roundsPerMinute").floatValue = 300;
                serialized.FindProperty("ammunition").objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(invalid.HasValidCombatConfiguration, Is.False);
            }
            finally { Object.DestroyImmediate(invalid); }
        }

        [Test] public void SeparateFirearmsKeepIndependentMagazinesAndConserveEachAmmoPool()
        {
            var rifle = Load<WeaponDefinition>("Assets/LastSignal/Data/Combat/WeaponDefinition_AssaultRifle.asset");
            var pistol = Load<WeaponDefinition>(Data + "Weapon_TacticalPistol.asset");
            var inventory = New("reserve").AddComponent<PlayerInventory>();
            inventory.Initialize(24);
            Assert.That(inventory.TryAdd(rifle.Ammunition, 6), Is.EqualTo(6));
            Assert.That(inventory.TryAdd(pistol.Ammunition, 4), Is.EqualTo(4));
            var r = new WeaponRuntimeState(rifle, rifle.MagazineCapacity - 2, inventory);
            var p = new WeaponRuntimeState(pistol, pistol.MagazineCapacity - 3, inventory);
            Ready(r); Ready(p);
            int rTotal = r.TotalAmmo, pTotal = p.TotalAmmo;
            Assert.That(p.TryBeginReload(), Is.True);
            p.Tick(pistol.TacticalReloadSeconds * pistol.ReloadCommitNormalized + .01f);
            Assert.That(p.TryCommitReload(), Is.True);
            Assert.That(p.TryCommitReload(), Is.False);
            Assert.That(p.TotalAmmo, Is.EqualTo(pTotal));
            Assert.That(r.TotalAmmo, Is.EqualTo(rTotal));
            Assert.That(r.TryBeginReload(), Is.True);
            r.Tick(rifle.TacticalReloadSeconds * rifle.ReloadCommitNormalized + .01f);
            Assert.That(r.TryCommitReload(), Is.True);
            Assert.That(r.TotalAmmo, Is.EqualTo(rTotal));
            Assert.That(p.TotalAmmo, Is.EqualTo(pTotal));
        }

        [Test] public void ReloadCancelAndConditionBoundsCannotCreateRounds()
        {
            var pistol = Load<WeaponDefinition>(Data + "Weapon_TacticalPistol.asset");
            var inventory = New("reserve").AddComponent<PlayerInventory>();
            inventory.Initialize(24);
            inventory.TryAdd(pistol.Ammunition, 2);
            var weapon = new WeaponRuntimeState(pistol, 0, inventory);
            Ready(weapon);
            Assert.That(weapon.TryBeginReload(), Is.True);
            Assert.That(weapon.TryCancelReload(), Is.True);
            Assert.That(weapon.TotalAmmo, Is.EqualTo(2));
            Assert.That(weapon.TryBeginReload(), Is.True);
            weapon.Tick(pistol.EmptyReloadSeconds + .01f);
            Assert.That(weapon.TryCompleteReload(), Is.True);
            Assert.That(weapon.CurrentMagazine, Is.EqualTo(2));
            Assert.That(weapon.TotalAmmo, Is.EqualTo(2));
            weapon.RestoreCondition(-20);
            Assert.That(weapon.Condition, Is.Zero);
            Assert.That(weapon.TryConsumeShot(), Is.False);
            weapon.RestoreCondition(2000);
            Assert.That(weapon.Condition, Is.EqualTo(1000));
            Assert.That(weapon.ConditionDamageFactor, Is.EqualTo(1).Within(.0001f));
            Assert.That(weapon.TryConsumeShot(), Is.True);
            Assert.That(weapon.Condition, Is.EqualTo(999));
            weapon.RestoreCondition(1);
            Assert.That(weapon.ConditionDamageFactor, Is.InRange(.8f, 1f));
        }

        [Test] public void ReloadObserverFailurePreservesCommittedAmmoAndCannotRepeatTransfer()
        {
            var pistol = Load<WeaponDefinition>(Data + "Weapon_TacticalPistol.asset");
            var inventory = New("reserve").AddComponent<PlayerInventory>();
            inventory.Initialize(24);
            inventory.TryAdd(pistol.Ammunition, 4);
            var weapon = new WeaponRuntimeState(pistol, 5, inventory);
            Ready(weapon);
            Assert.That(weapon.TryBeginReload(), Is.True);
            weapon.Tick(pistol.TacticalReloadSeconds + .01f);
            System.Action fail = () => throw new System.InvalidOperationException("observer fixture");
            inventory.InventoryChanged += fail;
            try { Assert.Throws<System.InvalidOperationException>(() => weapon.TryCommitReload()); }
            finally { inventory.InventoryChanged -= fail; }
            Assert.That(weapon.ReloadCommitted, Is.True);
            Assert.That(weapon.TryCommitReload(), Is.False);
            Assert.That(weapon.CurrentMagazine, Is.EqualTo(9));
            Assert.That(inventory.GetTotalQuantity(pistol.Ammunition), Is.Zero);
        }

        [Test] public void MeleeTimelinesAreFramePartitionIndependentAndRejectBusyAttacks()
        {
            var blade = Load<MeleeWeaponDefinition>(Data + "Machete.asset");
            var stamina = New("stamina").AddComponent<PlayerStamina>();
            var attack = new MeleeAttackState(blade);
            int active = 0;
            attack.ActiveWindow += () => active++;
            float before = stamina.CurrentStamina;
            Assert.That(attack.TryBegin(stamina), Is.True);
            Assert.That(attack.TryBegin(stamina), Is.False);
            attack.Tick(blade.WindupSeconds + blade.ActiveSeconds + blade.RecoverySeconds + .01f);
            Assert.That(active, Is.EqualTo(1));
            Assert.That(attack.State, Is.EqualTo(MeleeState.Ready));
            Assert.That(before - stamina.CurrentStamina, Is.EqualTo(blade.StaminaCost).Within(.01f));
            attack.Cancel();
            Assert.That(active, Is.EqualTo(1));
        }

        [Test] public void ZoneProtectionRunsAfterZoneMultiplierAndOnlyAcceptedHitGetsReceipt()
        {
            var root = New("zombie");
            var health = root.AddComponent<ZombieHealth>();
            // EditMode does not invoke runtime Awake; initialize the same way as ZombieDamageTests.
            typeof(ZombieHealth).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance).Invoke(health, null);
            Assert.That(health.CurrentHealth, Is.EqualTo(100));
            var zone = new GameObject("protected head");
            zone.transform.SetParent(root.transform);
            var collider = zone.AddComponent<BoxCollider>();
            var region = zone.AddComponent<ZombieHitRegion>();
            region.Configure(health, DamageRegion.Head, 2, collider, ZombieBodyPart.Head, .5f);
            var hit = new DamageInfo { Amount = 20, ShotId = 101, Category = DamageCategory.Melee,
                HitCollider = collider, HitPoint = zone.transform.position };
            var receipt = region.ReceiveDamage(hit);
            Assert.That(receipt.Committed, Is.True);
            Assert.That(receipt.Hit.BaseAmount, Is.EqualTo(20));
            Assert.That(receipt.Hit.Multiplier, Is.EqualTo(1));
            Assert.That(health.CurrentHealth, Is.EqualTo(80));
            Assert.That(region.ReceiveDamage(hit).Committed, Is.False);
            Assert.That(health.DamageTransactions, Is.EqualTo(1));
            health.SetDamageEnabled(false);
            hit.ShotId = 102;
            Assert.That(region.ReceiveDamage(hit).Committed, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(80));
        }

        static SaveCodec Codec() => new SaveCodec(new SaveValidation("world.fixture", "content.1",
            new Dictionary<string, int> { ["ammo.rifle"] = 60, ["medical.bandage"] = 10 },
            "rifle.1", 30, additionalWeapons: new Dictionary<string, int> { ["pistol.1"] = 15 }));
        static SaveGame Modern()
        {
            var save = SaveFoundationTests.Fixture();
            save.weapon.condition = 900;
            save.combat = new CombatEquipmentSnapshot { version = 2, selectedSlot = 0,
                meleeDefinitionId = MeleeWeaponDefinition.BladeId, selectedFirearmId = "pistol.1",
                stamina = PlayerStamina.Maximum, firearms = new[] {
                    new WeaponSnapshot { definitionId = "rifle.1", magazine = 17, condition = 900 },
                    new WeaponSnapshot { definitionId = "pistol.1", magazine = 7, condition = 750 } } };
            return save;
        }
        [Test] public void VersionTwoRoundTripsAndLegacySaveRemainsReadable()
        {
            var codec = Codec();
            Assert.That(codec.Encode(Modern(), out var json).Success, Is.True);
            Assert.That(codec.Decode(json, out var restored).Success, Is.True);
            Assert.That(restored.combat.selectedFirearmId, Is.EqualTo("pistol.1"));
            Assert.That(restored.combat.firearms[1].condition, Is.EqualTo(750));
            Assert.That(restored.inventory.slots[0].quantity, Is.EqualTo(60));
            Assert.That(codec.Encode(SaveFoundationTests.Fixture(), out var old).Success, Is.True);
            Assert.That(codec.Decode(old, out var legacy).Success, Is.True);
            Assert.That(legacy.weapon.magazine, Is.EqualTo(17));
            var versionOne = SaveFoundationTests.Fixture();
            versionOne.combat = new CombatEquipmentSnapshot { version = 1, selectedSlot = 1,
                meleeDefinitionId = MeleeWeaponDefinition.CrowbarId, stamina = PlayerStamina.Maximum };
            Assert.That(codec.Encode(versionOne, out var oldCombat).Success, Is.True);
            Assert.That(codec.Decode(oldCombat, out var restoredOldCombat).Success, Is.True);
            Assert.That(restoredOldCombat.combat.version, Is.EqualTo(1));
            Assert.That(restoredOldCombat.weapon.magazine, Is.EqualTo(17));
            Assert.That(codec.Decode(oldCombat.Replace("rifle.1", "unknown"), out _).Success, Is.False);
        }
        [Test] public void DuplicateMissingOrCorruptFirearmsAreRejected()
        {
            var codec = Codec();
            var save = Modern();
            save.combat.firearms[1].definitionId = "rifle.1";
            Assert.That(codec.Encode(save, out _).Success, Is.False);
            save = Modern(); save.combat.firearms[1].condition = 1001;
            Assert.That(codec.Encode(save, out _).Success, Is.False);
            save = Modern(); save.combat.firearms = new[] { save.combat.firearms[0] };
            Assert.That(codec.Encode(save, out _).Success, Is.False);
            save = Modern(); save.combat.selectedFirearmId = "unknown";
            Assert.That(codec.Encode(save, out _).Success, Is.False);
        }
    }
}
