using LastSignal.Inventory;
using LastSignal.Inventory.Data;

namespace LastSignal.Inventory
{
    public enum TransferReason { Complete, Partial, InvalidRequest, SourceEmpty, DestinationFull, Busy, Unavailable }
    public readonly struct TransferResult
    {
        public readonly int Requested, Moved, Remaining;
        public readonly TransferReason Reason;
        public TransferResult(int requested, int moved, TransferReason reason)
        { Requested = requested; Moved = moved; Remaining = requested > 0 ? requested - moved : 0; Reason = reason; }
    }
}
namespace LastSignal.Shelter
{
    public static class ItemTransferService
    {
        public static TransferResult Deposit(PlayerInventory player, ShelterStorage storage, ItemDefinition item, int quantity)
            => player && storage != null ? player.Container.TransferTo(storage.Container, item, quantity) : new TransferResult(quantity, 0, TransferReason.InvalidRequest);
        public static TransferResult Withdraw(ShelterStorage storage, PlayerInventory player, ItemDefinition item, int quantity)
            => player && storage != null ? storage.Container.TransferTo(player.Container, item, quantity) : new TransferResult(quantity, 0, TransferReason.InvalidRequest);
    }
}
