using System.Collections.Generic;
using UnityEngine;

namespace LastSignal.Inventory.Data
{
    [CreateAssetMenu(menuName = "Last Signal/Inventory/Item Catalog", fileName = "ItemCatalog")]
    public class ItemCatalog : ScriptableObject
    {
        [SerializeField] List<ItemDefinition> items = new List<ItemDefinition>();

        Dictionary<StableItemId, ItemDefinition> runtimeIndex;

        public void Initialize()
        {
            if (runtimeIndex != null) return;
            runtimeIndex = new Dictionary<StableItemId, ItemDefinition>();
            foreach (var item in items)
            {
                if (item == null) continue;
                if (!item.Id.IsValid)
                {
                    Debug.LogWarning($"ItemCatalog: Item '{item.name}' has no valid StableId.", item);
                    continue;
                }
                if (runtimeIndex.ContainsKey(item.Id))
                {
                    Debug.LogError($"ItemCatalog: Duplicate StableId '{item.Id}' found on '{item.name}'.", item);
                    continue;
                }
                runtimeIndex[item.Id] = item;
            }
        }

        public ItemDefinition GetItem(StableItemId id)
        {
            if (runtimeIndex == null) Initialize();
            return runtimeIndex != null && runtimeIndex.TryGetValue(id, out var item) ? item : null;
        }

        public IReadOnlyList<ItemDefinition> EditorItems => items;
    }
}
