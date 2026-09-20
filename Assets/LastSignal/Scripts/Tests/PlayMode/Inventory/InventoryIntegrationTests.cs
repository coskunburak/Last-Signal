using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Inventory.UI;

namespace LastSignal.Tests.Inventory
{
    public class InventoryIntegrationTests
    {
        PlayerInventory inventory;
        SessionFlow sessionFlow;
        WorldItem pickup;
        ItemDefinition itemA;
        GameObject playerObj;

        [SetUp]
        public void Setup()
        {
            itemA = ScriptableObject.CreateInstance<ItemDefinition>();
            var idField = typeof(ItemDefinition).GetField("stableId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxStackField = typeof(ItemDefinition).GetField("maxStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            idField.SetValue(itemA, new StableItemId("item.a"));
            maxStackField.SetValue(itemA, 10);

            // Mock player and inventory
            playerObj = new GameObject("Player");
            inventory = playerObj.AddComponent<PlayerInventory>();
            inventory.Initialize(5);

            // Mock world item
            var pickupObj = new GameObject("Pickup");
            pickupObj.SetActive(false); // prevent auto awake issues
            pickup = pickupObj.AddComponent<WorldItem>();
            pickup.Configure(itemA, 15);
            pickupObj.SetActive(true);
        }

        [TearDown]
        public void Teardown()
        {
            if (playerObj) Object.DestroyImmediate(playerObj);
            if (pickup && pickup.gameObject) Object.DestroyImmediate(pickup.gameObject);
            if (itemA) Object.DestroyImmediate(itemA);
            if (sessionFlow) Object.DestroyImmediate(sessionFlow.gameObject);
        }

        [UnityTest]
        public IEnumerator Pickup_Partial_LeavesRemainderInWorld()
        {
            inventory.Initialize(1); // Capacity 1 slot, max 10 for itemA

            // Act
            bool interacted = pickup.TryInteract();

            // Assert
            Assert.IsTrue(interacted);
            Assert.AreEqual(10, inventory.GetTotalQuantity(itemA));
            Assert.AreEqual(5, pickup.Quantity);
            Assert.IsNotNull(pickup.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Pickup_Full_DestroysWorldItem()
        {
            // Act
            bool interacted = pickup.TryInteract();

            // Assert
            Assert.IsTrue(interacted);
            Assert.AreEqual(15, inventory.GetTotalQuantity(itemA));
            
            // Should be destroyed
            yield return null;
            Assert.IsTrue(pickup == null || pickup.gameObject == null);
        }

        [UnityTest]
        public IEnumerator DoubleInteraction_DoesNotDuplicate()
        {
            // Act
            pickup.TryInteract();
            
            // Simulated race condition before destroy takes effect
            pickup.TryInteract(); 
            
            // Assert
            Assert.AreEqual(15, inventory.GetTotalQuantity(itemA));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Drop_RecreatesWorldItem_AndDecrementsInventory()
        {
            inventory.ConfigureDrop(playerObj.transform);
            inventory.TryAdd(itemA, 5);

            // Need to mock world prefab for drop
            var prefabObj = new GameObject("Prefab");
            prefabObj.AddComponent<WorldItem>();
            prefabObj.SetActive(false);
            
            var prefabField = typeof(ItemDefinition).GetField("worldPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            prefabField.SetValue(itemA, prefabObj);

            // Act
            bool dropped = inventory.TryDrop(0, 3);
            Assert.IsTrue(dropped);
            Assert.AreEqual(2, inventory.GetSlot(0).Quantity);

            // Find dropped item
            yield return null;
            var droppedItems = Object.FindObjectsOfType<WorldItem>(true);
            WorldItem droppedItem = null;
            foreach (var w in droppedItems)
            {
                if (w != pickup && w.gameObject != prefabObj) droppedItem = w;
            }

            Assert.IsNotNull(droppedItem);
            Assert.AreEqual(3, droppedItem.Quantity);
            Assert.AreEqual("item.a", droppedItem.Definition.Id.Value);

            Object.DestroyImmediate(prefabObj);
        }
        
        [UnityTest]
        public IEnumerator SessionReset_CleansInventoryState()
        {
            // Just verifying that a new PlayerInventory has no old state
            var newPlayer = new GameObject("Player2");
            var newInventory = newPlayer.AddComponent<PlayerInventory>();
            newInventory.Initialize(5);
            
            Assert.AreEqual(0, newInventory.GetTotalQuantity(itemA));
            
            Object.DestroyImmediate(newPlayer);
            yield return null;
        }
    }
}
