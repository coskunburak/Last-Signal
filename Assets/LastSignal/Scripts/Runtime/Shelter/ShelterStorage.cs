using System;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;

namespace LastSignal.Shelter
{
    /// <summary>Session-owned stash. UI lifetime never owns or resets this object.</summary>
    public sealed class ShelterStorage
    {
        internal InventoryContainer Container { get; }
        public ShelterStorage(int capacity) { Container = new InventoryContainer(capacity); }
        public int Capacity => Container.Capacity;
        public event Action Changed { add => Container.InventoryChanged += value; remove => Container.InventoryChanged -= value; }
        public InventorySlot GetSlot(int index) => Container.GetSlot(index);
        public int GetTotalQuantity(ItemDefinition item) => Container.GetTotalQuantity(item);
        public int TryAdd(ItemDefinition item, int quantity) => Container.TryAdd(item, quantity);
        public bool TryRemove(ItemDefinition item, int quantity) => Container.TryRemove(item, quantity);
    }
}
