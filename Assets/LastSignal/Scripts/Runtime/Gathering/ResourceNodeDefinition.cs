using UnityEngine;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;

namespace LastSignal.Gathering
{
    [CreateAssetMenu(menuName = "Last Signal/Gathering/Resource Node")]
    public sealed class ResourceNodeDefinition : ScriptableObject
    {
        public string definitionId = "resource.pine";
        public int revision = 1;
        public bool physicalTree;
        public string requiredToolId = MeleeWeaponDefinition.AxeId;
        public int minimumToolTier = 1; // V1 supports the existing owned axe only; no general equipment migration.
        public int health = 100, strikeDamage = 25, yield = 3;
        [Tooltip("Physical cutting resistance and owned-axe efficiency; affect penetration, not resource quantity.")]
        public float cuttingResistance=1, axeEfficiency=1;
        public float PhysicalPenetration => Mathf.Clamp01((float)strikeDamage/health*axeEfficiency/cuttingResistance);
        public ItemDefinition output;
        public bool Valid => (!physicalTree || (float.IsFinite(cuttingResistance) && cuttingResistance>=.1f && cuttingResistance<=10 &&
            float.IsFinite(axeEfficiency) && axeEfficiency>=.1f && axeEfficiency<=10)) && GatheringState.Id(definitionId) && revision > 0 && requiredToolId == MeleeWeaponDefinition.AxeId && minimumToolTier == 1 && health > 0 && health <= 10000 &&
            strikeDamage > 0 && strikeDamage <= health && yield > 0 && yield <= 64 && ValidOutput(output) && yield <= output.MaxStack;
        public static bool ValidOutput(ItemDefinition item) => item && item.Id.IsValid && item.Category == ItemCategory.Material &&
            item.MassGrams > 0 && item.MassGrams <= 100000 && item.MaxStack > 0 && item.MaxStack <= 64 &&
            item.WorldPrefab && item.WorldPrefab.GetComponent<WorldItem>() &&
            item.WorldPrefab.GetComponentInChildren<Collider>(true) && item.WorldPrefab.GetComponentInChildren<Renderer>(true);
    }
}
