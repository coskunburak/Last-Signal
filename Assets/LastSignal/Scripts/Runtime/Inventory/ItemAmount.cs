using LastSignal.Inventory.Data;

namespace LastSignal.Inventory
{
    public readonly struct ItemAmount
    {
        public readonly ItemDefinition Item;
        public readonly int Quantity;
        public ItemAmount(ItemDefinition item, int quantity) { Item = item; Quantity = quantity; }
    }
}
