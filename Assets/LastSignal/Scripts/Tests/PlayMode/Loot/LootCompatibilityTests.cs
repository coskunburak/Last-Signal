#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Inventory.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace LastSignal.Tests
{
    public class LootCompatibilityTests
    {
        [Test] public void SeedItemsHaveVisibleWorldRepresentation()
        {
            foreach(string id in new[]{"medical.bandage","food.canned","drink.water","ammo.rifle","material.scrap","tool.wrench"})
            {var item=AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Game/Items/Definitions/"+id+".asset");Assert.IsNotEmpty(item.WorldPrefab.GetComponentsInChildren<Renderer>(true),id+" has no visible world representation");}
        }
        [UnityTest] public IEnumerator RebindingSlotDoesNotDoubleInvokeClick()
        {
            var go=new GameObject("UI",typeof(InventoryUI));var slot=new GameObject("Slot",typeof(RectTransform),typeof(Button),typeof(InventorySlotUI));
            try
            {
                var ui=go.GetComponent<InventoryUI>();var s=slot.GetComponent<InventorySlotUI>();
                var player=new GameObject("Inventory",typeof(PlayerInventory));
                try
                {ui.Bind(player.GetComponent<PlayerInventory>(),null,null);s.Configure(0,ui);s.Configure(0,ui);slot.GetComponent<Button>().onClick.Invoke();Assert.AreEqual(0,typeof(InventoryUI).GetField("selectedSlot",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(ui));}
                finally{Object.DestroyImmediate(player);}
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(slot);}
            yield return null;
        }
    }
}
#endif
