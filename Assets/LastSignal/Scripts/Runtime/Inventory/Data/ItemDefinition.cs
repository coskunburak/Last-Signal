using UnityEngine;

namespace LastSignal.Inventory.Data
{
    public enum ItemUse { None, Hydrate, Nourish, Bandage, Backpack }
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
        [SerializeField] ItemUse use;
        [SerializeField, Min(0)] int massGrams;
        [SerializeField, Range(1, 100)] float hydrationPoints = 40, nutritionPoints = 35;
        [SerializeField, Range(0.5f, 30)] float treatmentSeconds = 3;
        [SerializeField, Range(1, 3)] int bleedingReduction = 3;
        [SerializeField, Range(1, 16)] int backpackSlots = 8;
        [SerializeField, Range(.25f, 1)] float equippedRecoveryMultiplier = 1;
        public int MassGrams => massGrams;
        public float HydrationPoints => hydrationPoints;
        public float NutritionPoints => nutritionPoints;
        public float TreatmentSeconds => treatmentSeconds;
        public int BleedingReduction => bleedingReduction;
        public int BackpackSlotBonus => backpackSlots;
        public float EquippedRecoveryMultiplier => equippedRecoveryMultiplier;
        public bool ValidGameplayData => massGrams >= 0 &&
            float.IsFinite(hydrationPoints) && hydrationPoints > 0 && hydrationPoints <= 100 &&
            float.IsFinite(nutritionPoints) && nutritionPoints > 0 && nutritionPoints <= 100 &&
            float.IsFinite(treatmentSeconds) && treatmentSeconds >= .5f && treatmentSeconds <= 30 &&
            bleedingReduction >= 1 && bleedingReduction <= 3 && backpackSlots >= 1 && backpackSlots <= 16 &&
            float.IsFinite(equippedRecoveryMultiplier) && equippedRecoveryMultiplier >= .25f && equippedRecoveryMultiplier <= 1;

        public StableItemId Id => stableId;
        public string DisplayName => displayName;
        public string Description => description;
        public ItemCategory Category => category;
        public int MaxStack => maxStack;
        public Sprite Icon => icon;
        public GameObject WorldPrefab => worldPrefab;
        public ItemUse Use => use;
        public bool HasValidUse => ValidGameplayData && (use == ItemUse.None ||
            (use == ItemUse.Hydrate && category == ItemCategory.Drink) ||
            (use == ItemUse.Nourish && category == ItemCategory.Food) ||
            (use == ItemUse.Bandage && category == ItemCategory.Medical) ||
            (use == ItemUse.Backpack && category == ItemCategory.Tool && maxStack == 1));

        void OnValidate()
        {
            if (maxStack < 1) maxStack = 1;
        }
    }
}
