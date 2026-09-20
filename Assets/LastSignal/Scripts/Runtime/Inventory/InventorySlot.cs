using LastSignal.Inventory.Data;
using System;
using UnityEngine;

namespace LastSignal.Inventory
{
    [Serializable]
    public struct InventorySlot
    {
        [SerializeField] ItemDefinition item;
        [SerializeField] int quantity;

        public ItemDefinition Item => item;
        public int Quantity => quantity;
        public bool IsEmpty => item == null || quantity <= 0;
        public int RemainingCapacity => IsEmpty ? 0 : item.MaxStack - quantity;

        public InventorySlot(ItemDefinition item, int quantity)
        {
            this.item = item;
            this.quantity = quantity;
            if (this.quantity <= 0) this.item = null;
        }

        public void Clear()
        {
            item = null;
            quantity = 0;
        }

        public InventorySlot WithAddedQuantity(int amount)
        {
            if (IsEmpty || amount <= 0) return this;
            return new InventorySlot(item, Mathf.Min(item.MaxStack, quantity + amount));
        }

        public InventorySlot WithRemovedQuantity(int amount)
        {
            if (IsEmpty || amount <= 0) return this;
            int newQuantity = quantity - amount;
            if (newQuantity <= 0) return new InventorySlot(null, 0);
            return new InventorySlot(item, newQuantity);
        }
    }
}
