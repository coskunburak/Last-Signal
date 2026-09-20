using UnityEngine;

namespace LastSignal.Inventory.Data
{
    [CreateAssetMenu(menuName = "Last Signal/Inventory/Item Definition", fileName = "NewItem")]
    public class ItemDefinition : ScriptableObject
    {
        [SerializeField] StableItemId stableId;
        [SerializeField] string displayName;
        [SerializeField, TextArea] string description;
        [SerializeField] ItemCategory category;
        [SerializeField, Min(1)] int maxStack = 1;
        [SerializeField] Sprite icon;
        [SerializeField] GameObject worldPrefab;

        public StableItemId Id => stableId;
        public string DisplayName => displayName;
        public string Description => description;
        public ItemCategory Category => category;
        public int MaxStack => maxStack;
        public Sprite Icon => icon;
        public GameObject WorldPrefab => worldPrefab;

        void OnValidate()
        {
            if (maxStack < 1) maxStack = 1;
        }
    }
}
