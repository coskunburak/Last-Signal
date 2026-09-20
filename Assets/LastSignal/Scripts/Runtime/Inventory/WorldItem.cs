using LastSignal.Inventory.Data;
using UnityEngine;

namespace LastSignal.Inventory
{
    public sealed class WorldItem : MonoBehaviour, IInteractable
    {
        [SerializeField] ItemDefinition definition;
        [SerializeField, Min(1)] int quantity = 1;

        public ItemDefinition Definition => definition;
        public int Quantity => quantity;
        public bool Available => isActiveAndEnabled && definition != null && quantity > 0;
        public string Prompt => definition != null ? $"Pick up {definition.DisplayName} x{quantity}" : "Invalid Item";

        public void Configure(ItemDefinition item, int initialQuantity)
        {
            definition = item;
            quantity = initialQuantity;
            if (quantity <= 0) Destroy(gameObject);
        }

        void OnValidate()
        {
            if (quantity < 1) quantity = 1;
        }

        public bool TryInteract()
        {
            if (!Available) return false;
            
            var playerInventory = FindObjectOfType<PlayerInventory>();
            if (playerInventory == null) return false;

            int accepted = playerInventory.TryAdd(definition, quantity);
            if (accepted > 0)
            {
                quantity -= accepted;
                if (quantity <= 0)
                {
                    Destroy(gameObject);
                }
                return true;
            }
            return false;
        }
    }
}
