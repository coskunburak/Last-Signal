using LastSignal.Inventory.Data;
using UnityEngine;

namespace LastSignal.Inventory
{
    public sealed class WorldItem : MonoBehaviour, IInteractable
    {
        [SerializeField] ItemDefinition definition;
        [SerializeField, Min(1)] int quantity = 1;

        string persistentId;
        System.Action<string,int> persistenceChanged;
        internal void BindPersistence(System.Action<string,int> listener) => persistenceChanged = listener;
        LastSignal.Persistence.WorldItemOrigin origin = LastSignal.Persistence.WorldItemOrigin.Drop;
        // New runtime drops receive an ID once. Restore replaces it with the SAVED ID before activation.
        public string PersistentId => persistentId;
        public LastSignal.Persistence.WorldItemOrigin Origin => origin;
        void Awake() { if (string.IsNullOrEmpty(persistentId)) persistentId = System.Guid.NewGuid().ToString("N"); }
        internal void AssignPersistentIdentity(string id, LastSignal.Persistence.WorldItemOrigin source)
        { persistentId = id; origin = source; }

        public ItemDefinition Definition => definition;
        public int Quantity => quantity;
        public bool Available => isActiveAndEnabled && definition != null && quantity > 0;
        public string Prompt => definition != null ? $"Pick up {definition.DisplayName} x{quantity}" : "Invalid Item";

        public void Configure(ItemDefinition item, int initialQuantity)
        {
            definition = item;
            quantity = initialQuantity;
            persistenceChanged?.Invoke(persistentId, quantity);
            if (quantity <= 0) Destroy(gameObject);
        }

        void OnValidate()
        {
            if (quantity < 1) quantity = 1;
        }

        public bool TryInteract()
        {
            if (!Available || LastSignal.Persistence.OwnershipTransaction.Active) return false;
            
            var playerInventory = FindObjectOfType<PlayerInventory>();
            if (playerInventory == null) return false;

            LastSignal.Persistence.OwnershipTransaction.Enter();
            try
            {
                int accepted = playerInventory.Container.BeginWorldAdd(definition, quantity);
                if (accepted <= 0) return false;
                try
                {
                    quantity -= accepted;
                    persistenceChanged?.Invoke(persistentId, quantity);
                    if (quantity <= 0) Destroy(gameObject);
                }
                finally { playerInventory.Container.CompleteWorldMutation(); }
                return true;
            }
            finally { LastSignal.Persistence.OwnershipTransaction.Exit(); }
        }
    }
}
