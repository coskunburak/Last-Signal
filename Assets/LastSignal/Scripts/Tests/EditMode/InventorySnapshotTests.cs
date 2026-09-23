using System;
using System.Collections.Generic;
using System.Reflection;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Persistence;
using LastSignal.Shelter;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public class InventorySnapshotTests
    {
        PlayerInventory inventory;
        ItemCatalog catalog;
        ItemDefinition item;
        GameObject world;
        const BindingFlags Fields = BindingFlags.NonPublic | BindingFlags.Instance;
        [SetUp] public void Setup()
        {
            inventory = new GameObject("Snapshot test player").AddComponent<PlayerInventory>(); inventory.Initialize(3);
            item = ScriptableObject.CreateInstance<ItemDefinition>();
            typeof(ItemDefinition).GetField("stableId", Fields).SetValue(item, new StableItemId("test.ammo"));
            typeof(ItemDefinition).GetField("maxStack", Fields).SetValue(item, 60);
            catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            typeof(ItemCatalog).GetField("items", Fields).SetValue(catalog, new List<ItemDefinition> { item });
        }
        [TearDown] public void Cleanup()
        {
            // Every spawned object uses this test's unique in-memory definition; no scene-root cleanup.
            foreach (var owned in Object.FindObjectsByType<WorldItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (owned.Definition == item) Object.DestroyImmediate(owned.gameObject);
            if (world) Object.DestroyImmediate(world);
            Object.DestroyImmediate(inventory.gameObject); Object.DestroyImmediate(catalog); Object.DestroyImmediate(item);
            Assert.IsFalse(OwnershipTransaction.Active, "Barrier must never leak out of a transaction.");
        }
        [Test] public void OrderedSlotsAndCapacitySurviveFiftyRestoresWithoutDuplication()
        {
            inventory.TryAdd(item, 67); inventory.TryMove(1, 2);
            Assert.IsTrue(InventorySnapshots.Capture(inventory, "player.inventory", out var state).Success);
            inventory.Initialize(1);
            for (int i = 0; i < 50; i++)
            {
                Assert.IsTrue(InventorySnapshots.Restore(inventory, state, "player.inventory", catalog).Success);
                Assert.AreEqual(3, inventory.Capacity); Assert.AreEqual(60, inventory.GetSlot(0).Quantity);
                Assert.IsTrue(inventory.GetSlot(1).IsEmpty); Assert.AreEqual(7, inventory.GetSlot(2).Quantity);
                Assert.AreEqual(67, inventory.GetTotalQuantity(item));
            }
            state.slots[0].quantity = 1;
            Assert.AreEqual(60, inventory.GetSlot(0).Quantity, "DTO must not alias authoritative inventory.");
        }
        [Test] public void UnknownFinalSlotCannotEraseOrPartiallyRestoreContainer()
        {
            inventory.TryAdd(item, 67);
            InventorySnapshots.Capture(inventory, "player.inventory", out var state);
            state.slots[0].quantity = 3; state.slots[2].definitionId = "unknown"; state.slots[2].quantity = 1;
            Assert.AreEqual(SaveError.UnknownDefinition, InventorySnapshots.Restore(inventory, state, "player.inventory", catalog).Error);
            Assert.AreEqual(67, inventory.GetTotalQuantity(item)); Assert.AreEqual(60, inventory.GetSlot(0).Quantity);
        }
        [Test] public void ShelterUsesSameSnapshotAndReplaceContract()
        {
            var stash = new ShelterStorage(4); stash.TryAdd(item, 100);
            Assert.IsTrue(InventorySnapshots.Capture(stash, "stash", out var state).Success);
            stash.TryRemove(item, 80);
            Assert.IsTrue(InventorySnapshots.Restore(stash, state, "stash", catalog).Success);
            Assert.AreEqual(100, stash.GetTotalQuantity(item)); Assert.AreEqual(4, stash.Capacity);
        }
        [Test] public void RestorePublishesOnlyCoherentStateAndRejectsReentrantMutation()
        {
            inventory.TryAdd(item, 67); InventorySnapshots.Capture(inventory, "player.inventory", out var state);
            inventory.Clear(); int notifications = 0;
            inventory.InventoryChanged += () => {
                notifications++; Assert.AreEqual(67, inventory.GetTotalQuantity(item));
                Assert.AreEqual(0, inventory.TryAdd(item, 1));
                Assert.AreEqual(SaveError.Busy, InventorySnapshots.Capture(inventory, "player.inventory", out _).Error);
            };
            Assert.IsTrue(InventorySnapshots.Restore(inventory, state, "player.inventory", catalog).Success);
            Assert.AreEqual(1, notifications);
        }
        [Test] public void PickupObserverCannotCaptureHalfCommittedTransfer()
        {
            inventory.Initialize(1); inventory.TryAdd(item, 55);
            world = new GameObject("Snapshot test world stack"); var pickup = world.AddComponent<WorldItem>(); pickup.Configure(item, 10);
            SaveResult receipt = default;
            bool recursivePickup = true;
            inventory.InventoryChanged += () => recursivePickup = pickup.TryInteract();
            inventory.InventoryChanged += () => receipt = InventorySnapshots.Capture(inventory, "player.inventory", out _);
            Assert.IsTrue(pickup.TryInteract());
            Assert.AreEqual(SaveError.Busy, receipt.Error, "Inventory event occurs before world decrement: capture must reject until both owners commit.");
            Assert.IsFalse(recursivePickup);
            Assert.AreEqual(5, pickup.Quantity); Assert.AreEqual(60, inventory.GetTotalQuantity(item));
            Assert.IsTrue(InventorySnapshots.Capture(inventory, "player.inventory", out _).Success);
        }
        void DropSetup()
        {
            world = new GameObject("Snapshot test drop prefab");
            world.AddComponent<WorldItem>().Configure(item, 1);
            typeof(ItemDefinition).GetField("worldPrefab", Fields).SetValue(item, world);
            inventory.ConfigureDrop(inventory.transform);
            inventory.TryAdd(item, 10);
        }
        int DroppedQuantity()
        {
            int total = 0;
            foreach (var drop in Object.FindObjectsByType<WorldItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (drop.gameObject != world && drop.Definition == item) total += drop.Quantity;
            return total;
        }
        [Test] public void DropObserverSeesCommittedOwnersButCaptureAndRecursiveDropRemainBusy()
        {
            DropSetup(); int notifications = 0;
            inventory.InventoryChanged += () => {
                notifications++;
                Assert.AreEqual(7, inventory.GetTotalQuantity(item)); Assert.AreEqual(3, DroppedQuantity());
                Assert.IsFalse(inventory.TryDrop(0, 1));
                Assert.AreEqual(SaveError.Busy, InventorySnapshots.Capture(inventory, "player.inventory", out _).Error);
            };
            Assert.IsTrue(inventory.TryDrop(0, 3)); Assert.AreEqual(1, notifications);
            Assert.AreEqual(10, inventory.GetTotalQuantity(item) + DroppedQuantity());
            Assert.IsTrue(InventorySnapshots.Capture(inventory, "player.inventory", out _).Success);
        }
        [Test] public void PickupObserverExceptionPropagatesAfterExactCommitWithoutLossOrDuplication()
        {
            inventory.Initialize(1); inventory.TryAdd(item, 55);
            world = new GameObject("Snapshot test partial pickup"); var pickup = world.AddComponent<WorldItem>(); pickup.Configure(item, 10);
            inventory.InventoryChanged += () => throw new InvalidOperationException("pickup observer");
            Assert.Throws<InvalidOperationException>(() => pickup.TryInteract());
            Assert.AreEqual(5, pickup.Quantity); Assert.AreEqual(60, inventory.GetTotalQuantity(item));
            Assert.AreEqual(65, pickup.Quantity + inventory.GetTotalQuantity(item));
            Assert.IsFalse(OwnershipTransaction.Active);
            Assert.IsTrue(InventorySnapshots.Capture(inventory, "player.inventory", out _).Success);
        }
        [Test] public void DropObserverExceptionPropagatesAfterExactCommitWithoutLossOrDuplication()
        {
            DropSetup(); inventory.InventoryChanged += () => throw new InvalidOperationException("drop observer");
            Assert.Throws<InvalidOperationException>(() => inventory.TryDrop(0, 3));
            Assert.AreEqual(7, inventory.GetTotalQuantity(item)); Assert.AreEqual(3, DroppedQuantity());
            Assert.IsFalse(OwnershipTransaction.Active);
            Assert.IsTrue(InventorySnapshots.Capture(inventory, "player.inventory", out _).Success);
        }
        [TestCase(0)][TestCase(-1)][TestCase(61)]
        public void InvalidOccupiedSlotQuantityLeavesInventoryUnchanged(int quantity)
        {
            inventory.TryAdd(item, 10); InventorySnapshots.Capture(inventory, "player.inventory", out var state);
            state.slots[0].quantity = quantity;
            Assert.AreEqual(SaveError.InvalidData, InventorySnapshots.Restore(inventory, state, "player.inventory", catalog).Error);
            Assert.AreEqual(10, inventory.GetTotalQuantity(item));
        }
        [Test] public void EmptySnapshotReplacesExistingContentsAndWrongCapacityCannotMutate()
        {
            InventorySnapshots.Capture(inventory, "player.inventory", out var empty); inventory.TryAdd(item, 10);
            Assert.IsTrue(InventorySnapshots.Restore(inventory, empty, "player.inventory", catalog).Success);
            Assert.AreEqual(0, inventory.GetTotalQuantity(item));
            inventory.TryAdd(item, 7); empty.capacity = 2;
            Assert.AreEqual(SaveError.InvalidData, InventorySnapshots.Restore(inventory, empty, "player.inventory", catalog).Error);
            Assert.AreEqual(7, inventory.GetTotalQuantity(item));
        }
        [Test] public void RestoreObserverExceptionPropagatesAfterCompleteReplacement()
        {
            inventory.TryAdd(item, 10); InventorySnapshots.Capture(inventory, "player.inventory", out var state);
            inventory.Clear(); inventory.InventoryChanged += () => throw new InvalidOperationException("restore observer");
            Assert.Throws<InvalidOperationException>(() => InventorySnapshots.Restore(inventory, state, "player.inventory", catalog));
            Assert.AreEqual(10, inventory.GetTotalQuantity(item));
            Assert.IsTrue(InventorySnapshots.Capture(inventory, "player.inventory", out _).Success);
        }
        [Test] public void DifferentDefinitionsAndExactMaximumSurviveRestore()
        {
            var other=ScriptableObject.CreateInstance<ItemDefinition>();
            try
            {
                typeof(ItemDefinition).GetField("stableId",Fields).SetValue(other,new StableItemId("test.other"));
                typeof(ItemDefinition).GetField("maxStack",Fields).SetValue(other,1);
                typeof(ItemCatalog).GetField("items",Fields).SetValue(catalog,new List<ItemDefinition>{item,other});
                inventory.TryAdd(item,60);inventory.TryAdd(other,1);
                Assert.IsTrue(InventorySnapshots.Capture(inventory,"player.inventory",out var state).Success);
                inventory.Clear();Assert.IsTrue(InventorySnapshots.Restore(inventory,state,"player.inventory",catalog).Success);
                Assert.AreEqual(60,inventory.GetTotalQuantity(item));Assert.AreEqual(1,inventory.GetTotalQuantity(other));
            }
            finally {Object.DestroyImmediate(other);}
        }
    }
}
