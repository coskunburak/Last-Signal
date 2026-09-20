using System;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public class AmmunitionTransactionTests
    {
        GameObject owner;
        PlayerInventory inventory;
        WeaponDefinition definition;
        ItemDefinition ammo;
        [SetUp] public void Setup()
        {
            owner = new GameObject("Ammo test inventory");
            inventory = owner.AddComponent<PlayerInventory>();
            inventory.Initialize(24);
            definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/LastSignal/Scripts/Runtime/Combat/WeaponDefinition_AssaultRifle.asset");
            ammo = definition.Ammunition;
        }
        [TearDown] public void Cleanup() => Object.DestroyImmediate(owner);
        WeaponRuntimeState Ready(int magazine, int reserve)
        {
            inventory.TryAdd(ammo, reserve);
            var state = new WeaponRuntimeState(definition, magazine, inventory);
            state.TryEquip(); state.Tick(1); state.TryCompleteEquip();
            return state;
        }
        [TestCase(0, 60, 30, 30)] [TestCase(8, 50, 30, 28)]
        [TestCase(8, 6, 14, 0)] [TestCase(24, 20, 30, 14)]
        public void ReloadConservesExactQuantity(int mag, int reserve, int expectedMag, int expectedReserve)
        {
            var state = Ready(mag, reserve);
            Assert.IsTrue(state.TryBeginReload()); state.Tick(10);
            Assert.IsTrue(state.TryCommitReload()); Assert.IsFalse(state.TryCommitReload());
            Assert.AreEqual(expectedMag, state.CurrentMagazine); Assert.AreEqual(expectedReserve, inventory.GetTotalQuantity(ammo));
            Assert.AreEqual(mag + reserve, state.TotalAmmo);
        }
        [TestCase(30, 10)] [TestCase(0, 0)]
        public void UnavailableReloadIsNoOp(int mag, int reserve)
        { var state = Ready(mag, reserve); Assert.IsFalse(state.TryBeginReload()); state.Tick(10); Assert.IsFalse(state.TryCommitReload()); Assert.AreEqual(mag + reserve, state.TotalAmmo); }
        [TestCase(30)] [TestCase(60)] [TestCase(120)] [TestCase(1)]
        public void BoundaryCrossingAtDifferentRatesCommitsOnce(int hz)
        {
            var state = Ready(5, 7); Assert.IsTrue(state.TryBeginReload()); int commits = 0;
            for (int i = 0; i < hz * 5; i++)
            { state.Tick(1f / hz); if (state.TryCommitReload()) commits++; state.TryCompleteReload(); }
            Assert.AreEqual(1, commits); Assert.AreEqual(12, state.CurrentMagazine); Assert.AreEqual(0, state.ReserveAmmo);
            Assert.AreEqual(WeaponState.Ready, state.State);
        }
        [TestCase(false)] [TestCase(true)]
        public void CancellationKeepsOnlyCommittedTransfer(bool committed)
        {
            var state = Ready(8, 6); state.TryBeginReload();
            if (committed) { state.Tick(2); Assert.IsTrue(state.TryCommitReload()); }
            state.TryCancelReload(); state.Tick(10); Assert.IsFalse(state.TryCommitReload());
            Assert.AreEqual(committed ? 14 : 8, state.CurrentMagazine); Assert.AreEqual(14, state.TotalAmmo);
        }
        [Test] public void InventoryChangeBeforeCommitUsesCurrentReserve()
        {
            var state = Ready(8, 60); state.TryBeginReload(); Assert.IsTrue(inventory.TryRemove(ammo, 57));
            state.Tick(2); Assert.IsTrue(state.TryCommitReload()); Assert.AreEqual(11, state.CurrentMagazine); Assert.AreEqual(0, state.ReserveAmmo);
        }
        [Test] public void ExhaustedReserveBeforeCommitCannotManufactureAmmo()
        { var state = Ready(8, 60); state.TryBeginReload(); inventory.Clear(); state.Tick(10); state.TryCompleteReload(); Assert.AreEqual(8, state.TotalAmmo); }
        [Test] public void MultiStackExactRemovalFailsAtomicallyAndNotifiesOnce()
        {
            inventory.TryAdd(ammo, 130); int notifications = 0; inventory.InventoryChanged += () => notifications++;
            Assert.IsFalse(inventory.TryRemove(ammo, 131)); Assert.AreEqual(130, inventory.GetTotalQuantity(ammo)); Assert.AreEqual(0, notifications);
            Assert.IsTrue(inventory.TryRemove(ammo, 125)); Assert.AreEqual(5, inventory.GetTotalQuantity(ammo)); Assert.AreEqual(1, notifications);
        }
        [Test] public void ReentrantObserverSeesConservationAndCannotRepeatCommit()
        {
            var state = Ready(8, 60); state.TryBeginReload(); state.Tick(2); int notifications = 0;
            inventory.InventoryChanged += () => { notifications++; Assert.AreEqual(68, state.TotalAmmo); Assert.IsFalse(state.TryCommitReload()); Assert.IsFalse(state.TryBeginReload()); state.ForceHolster(); };
            Assert.IsTrue(state.TryCommitReload()); Assert.AreEqual(1, notifications); Assert.AreEqual(30, state.CurrentMagazine); Assert.AreEqual(38, state.ReserveAmmo);
        }
        [Test] public void ThrowingObserverCannotLoseOrDuplicateCommittedAmmo()
        {
            var state = Ready(8, 60); state.TryBeginReload(); state.Tick(2);
            inventory.InventoryChanged += () => throw new InvalidOperationException("fixture observer");
            Assert.Throws<InvalidOperationException>(() => state.TryCommitReload());
            Assert.AreEqual(68, state.TotalAmmo); Assert.AreEqual(30, state.CurrentMagazine); Assert.IsFalse(state.TryCommitReload());
        }
        [Test] public void ProductionConfigurationUsesExistingAmmoAndMeasuredTiming()
        {
            Assert.IsTrue(definition.HasValidAmmunitionConfiguration);
            Assert.AreEqual("ammo.rifle", ammo.Id.Value); Assert.AreEqual(60, ammo.MaxStack);
            Assert.AreEqual(1.666667f, definition.TacticalReloadSeconds * definition.ReloadCommitNormalized, .001f);
            Assert.AreEqual(1.833333f, definition.EmptyReloadSeconds * definition.EmptyReloadCommitNormalized, .001f);
        }
        [Test] public void MissingInventoryNeverCreatesReserve()
        { var state = new WeaponRuntimeState(definition, 0, null); state.TryEquip(); state.Tick(1); state.TryCompleteEquip(); Assert.IsFalse(state.TryBeginReload()); Assert.AreEqual(0, state.ReserveAmmo); }
    }
}
