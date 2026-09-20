using NUnit.Framework;
using UnityEngine;
using LastSignal.Inventory.Data;

namespace LastSignal.Tests.Inventory
{
    public class ItemArchitectureTests
    {
        [Test]
        public void StableItemId_Value_IsCorrect()
        {
            var id = new StableItemId("test.item");
            Assert.AreEqual("test.item", id.Value);
            Assert.IsTrue(id.IsValid);
        }

        [Test]
        public void StableItemId_Equality_Works()
        {
            var id1 = new StableItemId("item.a");
            var id2 = new StableItemId("item.a");
            var id3 = new StableItemId("item.b");

            Assert.AreEqual(id1, id2);
            Assert.IsTrue(id1 == id2);
            Assert.AreNotEqual(id1, id3);
            Assert.IsTrue(id1 != id3);
        }

        [Test]
        public void ItemCatalog_Initialization_DetectsDuplicates()
        {
            var catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            var item1 = ScriptableObject.CreateInstance<ItemDefinition>();
            var item2 = ScriptableObject.CreateInstance<ItemDefinition>();

            // Access private fields for testing
            var idField = typeof(ItemDefinition).GetField("stableId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            idField.SetValue(item1, new StableItemId("medical.bandage"));
            idField.SetValue(item2, new StableItemId("medical.bandage"));

            var itemsField = typeof(ItemCatalog).GetField("items", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            itemsField.SetValue(catalog, new System.Collections.Generic.List<ItemDefinition> { item1, item2 });

            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, "ItemCatalog: Duplicate StableId 'medical.bandage' found on ''.");
            catalog.Initialize();

            // First item should be registered, second one ignored
            var retrieved = catalog.GetItem(new StableItemId("medical.bandage"));
            Assert.AreEqual(item1, retrieved);
        }
    }
}
