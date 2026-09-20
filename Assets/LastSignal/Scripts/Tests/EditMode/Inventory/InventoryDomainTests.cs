using NUnit.Framework;
using UnityEngine;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;

namespace LastSignal.Tests.Inventory
{
    public class InventoryDomainTests
    {
        PlayerInventory inventory;
        ItemDefinition itemA;
        ItemDefinition itemB;

        [SetUp]
        public void Setup()
        {
            var go = new GameObject();
            inventory = go.AddComponent<PlayerInventory>();
            inventory.Initialize(5); // 5 slots

            itemA = ScriptableObject.CreateInstance<ItemDefinition>();
            itemB = ScriptableObject.CreateInstance<ItemDefinition>();

            var idField = typeof(ItemDefinition).GetField("stableId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxStackField = typeof(ItemDefinition).GetField("maxStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            idField.SetValue(itemA, new StableItemId("item.a"));
            maxStackField.SetValue(itemA, 10);

            idField.SetValue(itemB, new StableItemId("item.b"));
            maxStackField.SetValue(itemB, 1);
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(inventory.gameObject);
            Object.DestroyImmediate(itemA);
            Object.DestroyImmediate(itemB);
        }

        [Test]
        public void AddToEmptyInventory_Works()
        {
            int accepted = inventory.TryAdd(itemA, 5);
            Assert.AreEqual(5, accepted);
            Assert.AreEqual(5, inventory.GetTotalQuantity(itemA));
            Assert.AreEqual(5, inventory.GetSlot(0).Quantity);
        }

        [Test]
        public void Add_RespectsMaxStack()
        {
            int accepted = inventory.TryAdd(itemA, 15);
            Assert.AreEqual(15, accepted);
            Assert.AreEqual(10, inventory.GetSlot(0).Quantity); // Max stack
            Assert.AreEqual(5, inventory.GetSlot(1).Quantity);  // Remainder
        }

        [Test]
        public void Add_WhenFull_ReturnsRemainder()
        {
            inventory.Initialize(2); // 2 slots, max capacity 20 for itemA
            int accepted = inventory.TryAdd(itemA, 25);
            
            Assert.AreEqual(20, accepted);
            Assert.AreEqual(10, inventory.GetSlot(0).Quantity);
            Assert.AreEqual(10, inventory.GetSlot(1).Quantity);
        }

        [Test]
        public void Remove_ExactQuantity_Works()
        {
            inventory.TryAdd(itemA, 5);
            bool removed = inventory.TryRemove(0, 5);
            
            Assert.IsTrue(removed);
            Assert.IsTrue(inventory.GetSlot(0).IsEmpty);
        }

        [Test]
        public void Remove_TooMuch_Fails()
        {
            inventory.TryAdd(itemA, 5);
            bool removed = inventory.TryRemove(0, 6);
            
            Assert.IsFalse(removed);
            Assert.AreEqual(5, inventory.GetSlot(0).Quantity);
        }

        [Test]
        public void Move_ToEmptySlot_Works()
        {
            inventory.TryAdd(itemA, 5);
            bool moved = inventory.TryMove(0, 1);
            
            Assert.IsTrue(moved);
            Assert.IsTrue(inventory.GetSlot(0).IsEmpty);
            Assert.AreEqual(5, inventory.GetSlot(1).Quantity);
        }

        [Test]
        public void Merge_CompatibleStacks_Works()
        {
            inventory.TryAdd(itemA, 5); // slot 0
            inventory.TryAdd(itemA, 5); // stacks into slot 0 -> 10
            inventory.TrySplit(0, 1, 2); // Slot 0 has 8, Slot 1 has 2
            
            bool merged = inventory.TryMerge(1, 0);
            
            Assert.IsTrue(merged);
            Assert.AreEqual(10, inventory.GetSlot(0).Quantity);
            Assert.IsTrue(inventory.GetSlot(1).IsEmpty);
        }

        [Test]
        public void Split_Works()
        {
            inventory.TryAdd(itemA, 5);
            bool split = inventory.TrySplit(0, 1, 2);
            
            Assert.IsTrue(split);
            Assert.AreEqual(3, inventory.GetSlot(0).Quantity);
            Assert.AreEqual(2, inventory.GetSlot(1).Quantity);
        }

        [Test]
        public void Add_ZeroOrNegative_IsRejected()
        {
            Assert.AreEqual(0, inventory.TryAdd(itemA, 0));
            Assert.AreEqual(0, inventory.TryAdd(itemA, -5));
        }

        [Test]
        public void ChangeEvent_FiresOnSuccess()
        {
            int eventCount = 0;
            inventory.InventoryChanged += () => eventCount++;

            inventory.TryAdd(itemA, 5);
            Assert.AreEqual(1, eventCount);

            inventory.TryRemove(0, 2);
            Assert.AreEqual(2, eventCount);

            inventory.TryRemove(0, 10); // Should fail, no event
            Assert.AreEqual(2, eventCount);
        }
    }
}
