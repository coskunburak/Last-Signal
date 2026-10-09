#if UNITY_EDITOR
using System.Collections;
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
            {var item=AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/LastSignal/Data/Items/Definitions/"+id+".asset");Assert.IsNotEmpty(item.WorldPrefab.GetComponentsInChildren<Renderer>(true),id+" has no visible world representation");}
        }
        [UnityTest] public IEnumerator RebindingSlotDoesNotDoubleInvokeClick()
        {
            var go=new GameObject("UI",typeof(InventoryUI));var slot=new GameObject("Slot",typeof(RectTransform),typeof(Button),typeof(InventorySlotUI));
            var panel=new GameObject("Inventory panel",typeof(RectTransform));
            slot.transform.SetParent(panel.transform,false);
            try
            {
                var ui=go.GetComponent<InventoryUI>();var s=slot.GetComponent<InventorySlotUI>();
                var player=new GameObject("Inventory",typeof(PlayerInventory));
                try
                {
                    var inventory=player.GetComponent<PlayerInventory>();
                    inventory.Initialize(2);
                    var item=AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/LastSignal/Data/Items/Definitions/medical.bandage.asset");
                    Assert.AreEqual(1,inventory.TryAdd(item,1));
                    ui.ConfigureView(panel,new[]{s},null);
                    ui.Bind(inventory,null,null);
                    panel.SetActive(true); // Slot routing fixture: a visible, populated view.
                    s.Configure(0,ui);s.Configure(0,ui);
                    slot.GetComponent<Button>().onClick.Invoke();
                    Assert.AreEqual(0,ui.SelectedSlot,"One click selects; duplicate callbacks would toggle it off.");
                    slot.GetComponent<Button>().onClick.Invoke();
                    Assert.AreEqual(-1,ui.SelectedSlot,"The next click cancels exactly once.");
                    Assert.AreEqual(1,inventory.GetSlot(0).Quantity);
                }
                finally{Object.DestroyImmediate(player);}
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(panel);}
            yield return null;
        }
    }
}
#endif
