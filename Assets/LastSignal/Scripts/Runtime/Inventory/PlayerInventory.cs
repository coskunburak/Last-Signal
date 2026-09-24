using LastSignal.Inventory.Data;
using System;
using UnityEngine;

namespace LastSignal.Inventory
{
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField, Min(1)] int capacity = 24;
        InventoryContainer container;
        internal InventoryContainer Container => container ?? (container = new InventoryContainer(capacity));
        public event Action InventoryChanged { add => Container.InventoryChanged += value; remove => Container.InventoryChanged -= value; }
        public int Capacity => Container.Capacity;
        void Awake() { var initialized = Container; }
        public void Initialize(int newCapacity) { Container.Initialize(newCapacity); capacity = Container.Capacity; }
        public InventorySlot GetSlot(int index) => Container.GetSlot(index);
        public int TryAdd(ItemDefinition item, int quantity) => Container.TryAdd(item, quantity);
        public bool TryRemove(int index, int quantity) => Container.TryRemove(index, quantity);
        public bool TryRemove(ItemDefinition item, int quantity) => Container.TryRemove(item, quantity);
        public bool TryMove(int sourceIndex, int destIndex) => Container.TryMove(sourceIndex, destIndex);
        public bool TryMerge(int sourceIndex, int destIndex) => Container.TryMerge(sourceIndex, destIndex);
        public bool TrySplit(int sourceIndex, int destIndex, int amount) => Container.TrySplit(sourceIndex, destIndex, amount);
        public int GetTotalQuantity(ItemDefinition item) => Container.GetTotalQuantity(item);
        public void Clear() => Container.Clear();

        [SerializeField] Transform dropOrigin;
        [SerializeField] LayerMask dropObstructionMask = 1; // Default layer

        public void ConfigureDrop(Transform origin) => dropOrigin = origin;

        public bool TryDrop(int slotIndex, int amount)
        {
            if (LastSignal.Persistence.OwnershipTransaction.Active || Container.IsBusy || slotIndex < 0 || slotIndex >= Capacity || amount <= 0) return false;
            var slot = GetSlot(slotIndex);
            if (slot.IsEmpty || slot.Quantity < amount) return false;
            
            var def = slot.Item;
            if (!def.WorldPrefab || !dropOrigin) return false;

            // Placement logic
            Vector3 spawnPos = dropOrigin.position + dropOrigin.forward * 1.5f;
            if (Physics.Raycast(dropOrigin.position, dropOrigin.forward, out RaycastHit hit, 1.5f, dropObstructionMask))
            {
                spawnPos = hit.point - dropOrigin.forward * 0.2f;
            }

            LastSignal.Persistence.OwnershipTransaction.Enter();
            try
            {
                var go = Instantiate(def.WorldPrefab, spawnPos, Quaternion.identity);
                var worldItem = go.GetComponent<WorldItem>();
                if (!worldItem) worldItem = go.AddComponent<WorldItem>();
                worldItem.Configure(def, amount);
                var cells = FindAnyObjectByType<LastSignal.WorldCells.WorldCellManager>();
                if (cells) cells.AdoptDrop(worldItem);
                if (Container.BeginWorldRemove(slotIndex, amount))
                {
                    Container.CompleteWorldMutation();
                    return true;
                }
                // A prefab callback must not leave a new owner if the removal was rejected.
                go.SetActive(false); Destroy(go);
                return false;
            }
            finally { LastSignal.Persistence.OwnershipTransaction.Exit(); }
        }
    }
}
