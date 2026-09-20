using LastSignal.Inventory.Data;
using System;
using UnityEngine;

namespace LastSignal.Inventory
{
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField, Min(1)] int capacity = 24;
        InventorySlot[] slots;

        public event Action InventoryChanged;

        public int Capacity => capacity;

        void Awake()
        {
            Initialize(capacity);
        }

        public void Initialize(int newCapacity)
        {
            capacity = newCapacity;
            slots = new InventorySlot[capacity];
            for (int i = 0; i < capacity; i++)
            {
                slots[i] = new InventorySlot(null, 0);
            }
            InventoryChanged?.Invoke();
        }

        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= slots.Length) return new InventorySlot(null, 0);
            return slots[index];
        }

        public int TryAdd(ItemDefinition item, int quantity)
        {
            if (item == null || quantity <= 0) return 0;

            int remaining = quantity;

            // 1. Fill existing compatible stacks
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                ref InventorySlot slot = ref slots[i];
                if (!slot.IsEmpty && slot.Item.Id == item.Id && slot.Quantity < item.MaxStack)
                {
                    int space = item.MaxStack - slot.Quantity;
                    int add = Mathf.Min(space, remaining);
                    slot = slot.WithAddedQuantity(add);
                    remaining -= add;
                }
            }

            // 2. Fill empty slots
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                ref InventorySlot slot = ref slots[i];
                if (slot.IsEmpty)
                {
                    int add = Mathf.Min(item.MaxStack, remaining);
                    slot = new InventorySlot(item, add);
                    remaining -= add;
                }
            }

            int accepted = quantity - remaining;
            if (accepted > 0)
            {
                InventoryChanged?.Invoke();
            }
            return accepted;
        }

        public bool TryRemove(int index, int quantity)
        {
            if (index < 0 || index >= slots.Length || quantity <= 0) return false;
            ref InventorySlot slot = ref slots[index];
            if (slot.IsEmpty || slot.Quantity < quantity) return false;
            
            slot = slot.WithRemovedQuantity(quantity);
            InventoryChanged?.Invoke();
            return true;
        }

        public bool TryMove(int sourceIndex, int destIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= slots.Length) return false;
            if (destIndex < 0 || destIndex >= slots.Length) return false;
            if (sourceIndex == destIndex) return false;

            ref InventorySlot source = ref slots[sourceIndex];
            ref InventorySlot dest = ref slots[destIndex];

            if (source.IsEmpty) return false;

            if (dest.IsEmpty)
            {
                dest = source;
                source.Clear();
                InventoryChanged?.Invoke();
                return true;
            }

            if (source.Item.Id == dest.Item.Id)
            {
                return TryMerge(sourceIndex, destIndex);
            }

            // Swap
            InventorySlot temp = dest;
            dest = source;
            source = temp;
            InventoryChanged?.Invoke();
            return true;
        }

        public bool TryMerge(int sourceIndex, int destIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= slots.Length) return false;
            if (destIndex < 0 || destIndex >= slots.Length) return false;
            if (sourceIndex == destIndex) return false;

            ref InventorySlot source = ref slots[sourceIndex];
            ref InventorySlot dest = ref slots[destIndex];

            if (source.IsEmpty || dest.IsEmpty || source.Item.Id != dest.Item.Id) return false;

            int space = dest.Item.MaxStack - dest.Quantity;
            if (space <= 0) return false;

            int moveAmount = Mathf.Min(space, source.Quantity);
            dest = dest.WithAddedQuantity(moveAmount);
            source = source.WithRemovedQuantity(moveAmount);

            InventoryChanged?.Invoke();
            return true;
        }

        public bool TrySplit(int sourceIndex, int destIndex, int amount)
        {
            if (sourceIndex < 0 || sourceIndex >= slots.Length) return false;
            if (destIndex < 0 || destIndex >= slots.Length) return false;
            if (sourceIndex == destIndex) return false;
            if (amount <= 0) return false;

            ref InventorySlot source = ref slots[sourceIndex];
            ref InventorySlot dest = ref slots[destIndex];

            if (source.IsEmpty || source.Quantity <= amount) return false;
            if (!dest.IsEmpty) return false;

            dest = new InventorySlot(source.Item, amount);
            source = source.WithRemovedQuantity(amount);

            InventoryChanged?.Invoke();
            return true;
        }

        public int GetTotalQuantity(ItemDefinition item)
        {
            if (item == null) return 0;
            int total = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                ref InventorySlot slot = ref slots[i];
                if (!slot.IsEmpty && slot.Item.Id == item.Id)
                {
                    total += slot.Quantity;
                }
            }
            return total;
        }

        public void Clear()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].Clear();
            }
            InventoryChanged?.Invoke();
        }

        [SerializeField] Transform dropOrigin;
        [SerializeField] LayerMask dropObstructionMask = 1; // Default layer

        public void ConfigureDrop(Transform origin) => dropOrigin = origin;

        public bool TryDrop(int slotIndex, int amount)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length || amount <= 0) return false;
            ref InventorySlot slot = ref slots[slotIndex];
            if (slot.IsEmpty || slot.Quantity < amount) return false;
            
            var def = slot.Item;
            if (!def.WorldPrefab || !dropOrigin) return false;

            // Placement logic
            Vector3 spawnPos = dropOrigin.position + dropOrigin.forward * 1.5f;
            if (Physics.Raycast(dropOrigin.position, dropOrigin.forward, out RaycastHit hit, 1.5f, dropObstructionMask))
            {
                spawnPos = hit.point - dropOrigin.forward * 0.2f;
            }

            var go = Instantiate(def.WorldPrefab, spawnPos, Quaternion.identity);
            var worldItem = go.GetComponent<WorldItem>();
            if (!worldItem) worldItem = go.AddComponent<WorldItem>();
            
            worldItem.Configure(def, amount);

            slot = slot.WithRemovedQuantity(amount);
            InventoryChanged?.Invoke();
            return true;
        }
    }
}
