using LastSignal.Inventory.Data;
using System;
using UnityEngine;

namespace LastSignal.Inventory
{
    public sealed class InventoryContainer
    {
        int capacity;
        bool silent;
        public bool IsBusy { get; private set; }
        InventorySlot[] slots;

        public event Action InventoryChanged;

        public int Capacity => capacity;

        public InventoryContainer(int capacity) { Initialize(capacity); }

        public void Initialize(int newCapacity)
        {
            if (IsBusy) return;
            if (newCapacity < 1 || newCapacity > 256) throw new ArgumentOutOfRangeException(nameof(newCapacity));
            capacity = newCapacity;
            slots = new InventorySlot[capacity];
            for (int i = 0; i < capacity; i++)
            {
                slots[i] = new InventorySlot(null, 0);
            }
            Notify();
        }

        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= slots.Length) return new InventorySlot(null, 0);
            return slots[index];
        }

        public int TryAdd(ItemDefinition item, int quantity)
        {
            if (IsBusy || item == null || !item.Id.IsValid || item.MaxStack < 1 || quantity <= 0) return 0;

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
                Notify();
            }
            return accepted;
        }

        public bool TryRemove(int index, int quantity)
        {
            if (IsBusy) return false;
            if (index < 0 || index >= slots.Length || quantity <= 0) return false;
            ref InventorySlot slot = ref slots[index];
            if (slot.IsEmpty || slot.Quantity < quantity) return false;
            
            slot = slot.WithRemovedQuantity(quantity);
            Notify();
            return true;
        }

        /// <summary>Exact removal across stacks; failure changes nothing, success publishes once.</summary>
        public bool TryRemove(ItemDefinition item, int quantity)
        {
            if (IsBusy) return false;
            if (!item || quantity <= 0 || GetTotalQuantity(item) < quantity) return false;
            int remaining = quantity;
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                ref InventorySlot slot = ref slots[i];
                if (slot.IsEmpty || slot.Item.Id != item.Id) continue;
                int take = Mathf.Min(remaining, slot.Quantity);
                slot = slot.WithRemovedQuantity(take);
                remaining -= take;
            }
            Notify();
            return true;
        }

        public bool TryMove(int sourceIndex, int destIndex)
        {
            if (IsBusy) return false;
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
                Notify();
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
            Notify();
            return true;
        }

        public bool TryMerge(int sourceIndex, int destIndex)
        {
            if (IsBusy) return false;
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

            Notify();
            return true;
        }

        public bool TrySplit(int sourceIndex, int destIndex, int amount)
        {
            if (IsBusy) return false;
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

            Notify();
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
            if (IsBusy) return;
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].Clear();
            }
            Notify();
        }

        // Persistence adapter prevalidates detached slots. Replace rather than append so repeated
        // load cannot duplicate quantities; publish only after the whole container is coherent.
        internal void ReplaceValidatedSlots(InventorySlot[] replacement)
        {
            if (IsBusy) throw new InvalidOperationException("Container transaction already in progress.");
            IsBusy = true;
            try
            {
                slots = replacement;
                capacity = replacement.Length;
                InventoryChanged?.Invoke();
            }
            finally { IsBusy = false; }
        }

        void Notify() { if (!silent) InventoryChanged?.Invoke(); }

        // Cross-owner world operations stage this owner silently, then let the caller commit
        // the world quantity before any observer runs. Existing normal/reload notifications
        // keep their original exception contract.
        internal int BeginWorldAdd(ItemDefinition item, int quantity)
        {
            if (IsBusy) return 0;
            int accepted;
            silent = true;
            try { accepted = TryAdd(item, quantity); }
            finally { silent = false; }
            if (accepted > 0) IsBusy = true;
            return accepted;
        }
        internal bool BeginWorldRemove(int index, int quantity)
        {
            if (IsBusy) return false;
            bool removed;
            silent = true;
            try { removed = TryRemove(index, quantity); }
            finally { silent = false; }
            if (removed) IsBusy = true;
            return removed;
        }
        internal void CompleteWorldMutation()
        {
            // Quantities are already committed. Preserve normal observer exception propagation;
            // the caller's finally releases the ownership barrier without rolling back state.
            try { InventoryChanged?.Invoke(); }
            finally { IsBusy = false; }
        }

        internal TransferResult TransferTo(InventoryContainer destination, ItemDefinition item, int requested)
        {
            if (destination == null || destination == this || !item || !item.Id.IsValid || item.MaxStack < 1 || requested <= 0)
                return new TransferResult(requested, 0, TransferReason.InvalidRequest);
            if (IsBusy || destination.IsBusy) return new TransferResult(requested, 0, TransferReason.Busy);
            int available = GetTotalQuantity(item);
            if (available == 0) return new TransferResult(requested, 0, TransferReason.SourceEmpty);
            long space = 0;
            for (int i = 0; i < destination.slots.Length; i++)
            {
                var slot = destination.slots[i];
                if (slot.IsEmpty) space += item.MaxStack;
                else if (slot.Item.Id == item.Id)
                {
                    // A catalog ID must not alias incompatible definitions.
                    if (slot.Item != item) return new TransferResult(requested, 0, TransferReason.InvalidRequest);
                    space += slot.RemainingCapacity;
                }
            }
            for (int i = 0; i < slots.Length; i++)
                if (!slots[i].IsEmpty && slots[i].Item.Id == item.Id && slots[i].Item != item)
                    return new TransferResult(requested, 0, TransferReason.InvalidRequest);
            int moved = (int)Math.Min(Math.Min((long)requested, available), space);
            if (moved <= 0) return new TransferResult(requested, 0, TransferReason.DestinationFull);
            // Main-thread synchronous commit. Reuse exactly the S005 add/remove algorithms,
            // suppressing callbacks until BOTH owners have committed. No fallible external work.
            silent = destination.silent = true;
            try
            {
                int accepted = destination.TryAdd(item, moved);
                if (accepted != moved || !TryRemove(item, moved))
                {
                    if (accepted > 0) destination.TryRemove(item, accepted);
                    return new TransferResult(requested, 0, TransferReason.InvalidRequest);
                }
            }
            finally { silent = destination.silent = false; }
            IsBusy = destination.IsBusy = true;
            try
            {
                // Observer failures cannot roll back a committed transfer or suppress the other owner.
                PublishTransfer(); destination.PublishTransfer();
            }
            finally { IsBusy = destination.IsBusy = false; }
            return new TransferResult(requested, moved, moved == requested ? TransferReason.Complete : TransferReason.Partial);
        }
        void PublishTransfer()
        {
            if (InventoryChanged == null) return;
            foreach (Action observer in InventoryChanged.GetInvocationList())
                try { observer(); } catch (Exception exception) { Debug.LogException(exception); }
        }
    }
}
