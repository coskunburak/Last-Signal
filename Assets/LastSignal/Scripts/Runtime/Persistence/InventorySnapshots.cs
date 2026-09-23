using System;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Shelter;

namespace LastSignal.Persistence
{
    /// <summary>Domain adapter shared by carried inventory and stash. UI cannot own snapshot state.</summary>
    public static class InventorySnapshots
    {
        public static SaveResult Capture(PlayerInventory inventory, string id, out ContainerSnapshot snapshot)
            => Capture(inventory ? inventory.Container : null, id, out snapshot);
        public static SaveResult Capture(ShelterStorage storage, string id, out ContainerSnapshot snapshot)
            => Capture(storage?.Container, id, out snapshot);
        static SaveResult Capture(InventoryContainer container, string id, out ContainerSnapshot snapshot)
        {
            snapshot = null;
            if (container == null || string.IsNullOrWhiteSpace(id) || id.Length > 128)
                return new SaveResult(SaveError.InvalidData, "Missing inventory or stable container identity.");
            if (OwnershipTransaction.Active || container.IsBusy) return new SaveResult(SaveError.Busy, "Ownership transaction has not completed.");
            var slots = new SlotSnapshot[container.Capacity];
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = container.GetSlot(i);
                slots[i] = slot.IsEmpty ? new SlotSnapshot() : new SlotSnapshot { definitionId = slot.Item.Id.Value, quantity = slot.Quantity };
            }
            snapshot = new ContainerSnapshot { id = id, capacity = slots.Length, slots = slots };
            return SaveResult.Ok;
        }
        public static SaveResult Restore(PlayerInventory inventory, ContainerSnapshot snapshot, string expectedId, ItemCatalog catalog)
            => Restore(inventory ? inventory.Container : null, snapshot, expectedId, catalog);
        public static SaveResult Restore(ShelterStorage storage, ContainerSnapshot snapshot, string expectedId, ItemCatalog catalog)
            => Restore(storage?.Container, snapshot, expectedId, catalog);
        static SaveResult Restore(InventoryContainer container, ContainerSnapshot snapshot, string expectedId, ItemCatalog catalog)
        {
            if (container == null || !catalog || snapshot == null || string.IsNullOrWhiteSpace(expectedId) || snapshot.id != expectedId ||
                snapshot.capacity < 1 || snapshot.capacity > 256 || snapshot.slots == null || snapshot.slots.Length != snapshot.capacity)
                return new SaveResult(SaveError.InvalidData, "Invalid inventory restore request.");
            if (OwnershipTransaction.Active || container.IsBusy) return new SaveResult(SaveError.Busy, "Ownership transaction has not completed.");
            // Resolve and validate EVERY slot before mutating the existing container.
            var slots = new InventorySlot[snapshot.capacity];
            for (int i = 0; i < slots.Length; i++)
            {
                var s = snapshot.slots[i];
                if (s == null) return new SaveResult(SaveError.InvalidData, "Missing slot.");
                if (string.IsNullOrEmpty(s.definitionId))
                {
                    if (s.quantity != 0) return new SaveResult(SaveError.InvalidData, "Empty slot contains quantity.");
                    continue;
                }
                var item = catalog.GetItem(new StableItemId(s.definitionId));
                if (!item) return new SaveResult(SaveError.UnknownDefinition, "Required item is not in the current catalog.");
                if (s.quantity < 1 || s.quantity > item.MaxStack) return new SaveResult(SaveError.InvalidData, "Invalid stack size.");
                slots[i] = new InventorySlot(item, s.quantity);
            }
            container.ReplaceValidatedSlots(slots);
            return SaveResult.Ok;
        }
    }
}
