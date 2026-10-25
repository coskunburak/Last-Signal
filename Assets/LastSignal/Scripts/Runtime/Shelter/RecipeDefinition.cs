using System;
using LastSignal.Inventory.Data;
using UnityEngine;

namespace LastSignal.Shelter
{
    public enum CraftTimeBasis { WorldSeconds, SimulationSeconds }
    public enum CraftStation { Handcraft, Workbench }
    [Flags] public enum CraftCapability { None = 0, WoodProcessing = 1 }
    public enum IngredientSource { SelectedContainer, PlayerInventory, ShelterStorage }
    // Only existing, authoritative shelter progression is supported; unknown unlocks fail closed.
    public enum CraftUnlock { None, ClaimedShelter, UpgradedWorkbench }
    [Serializable] public sealed class RecipeAmount
    {
        public ItemDefinition item;
        public int quantity;
        public IngredientSource sourcePolicy;
    }
    [CreateAssetMenu(menuName = "Last Signal/Shelter/Multi Ingredient Recipe")]
    public sealed class RecipeDefinition : ScriptableObject
    {
        public string recipeId;
        public int definitionRevision = 1, catalogVersion = 1;
        public RecipeAmount[] inputIngredients = Array.Empty<RecipeAmount>();
        public RecipeAmount[] outputDefinitions = Array.Empty<RecipeAmount>();
        public ItemDefinition[] requiredTools = Array.Empty<ItemDefinition>();
        public CraftStation requiredStation = CraftStation.Workbench;
        public CraftCapability requiredCapabilities;
        public CraftUnlock unlockRequirement = CraftUnlock.ClaimedShelter;
        public CraftTimeBasis timeBasis;
        public double duration = 60;
        public bool requiresPower;
        public bool prototypeBalance;
    }
}
