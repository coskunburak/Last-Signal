using System;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Shelter;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    // Baseline after module setup: source A=6/B=4/C=2/tool=1/output=0; inputs=2 each, output=3.
    // Setup costs come from a separate container, never from test ownership totals.
    public sealed class P02CraftFixture : IDisposable
    {
        public readonly ItemDefinition A, B, C, Output, Tool, Fuel;
        public readonly RecipeDefinition Recipe;
        public readonly ShelterRecipe Legacy;
        public InventoryContainer Source;
        public ShelterProduction Production;
        public P02CraftFixture(bool powered = false, bool handcraft = false)
        {
            A = Item("material.scrap"); B = Item("material.cloth"); C = Item("drink.water");
            Output = Item("ammo.rifle"); Tool = Item("weapon.axe"); Fuel = Item("fuel.generator");
            Recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
            Recipe.recipeId = "recipe.s005.fixture";
            Recipe.inputIngredients = new[] { Amount(C, 2), Amount(A, 2), Amount(B, 2) };
            Recipe.outputDefinitions = new[] { Amount(Output, 3) };
            Recipe.requiredTools = new[] { Tool }; Recipe.duration = 10; Recipe.requiresPower = powered;
            if (handcraft) { Recipe.requiredStation = CraftStation.Handcraft; Recipe.timeBasis = CraftTimeBasis.SimulationSeconds; }
            Legacy = ScriptableObject.CreateInstance<ShelterRecipe>();
            Legacy.recipeId = "recipe.legacy.fixture"; Legacy.input = A; Legacy.output = Output;
            Legacy.inputQuantity = 2; Legacy.outputQuantity = 3; Legacy.durationWorldSeconds = 10; Legacy.requiresPower = false;
            Source = new InventoryContainer(12); Source.TryAdd(A, 6); Source.TryAdd(B, 4); Source.TryAdd(C, 2); Source.TryAdd(Tool, 1);
            Production = Create();
        }
        public static ItemDefinition Item(string name) => AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/LastSignal/Data/Items/Definitions/" + name + ".asset");
        public static RecipeAmount Amount(ItemDefinition item, int count, IngredientSource source = IngredientSource.SelectedContainer)
            => new RecipeAmount { item = item, quantity = count, sourcePolicy = source };
        public ShelterProduction Create(double now = 0, bool modules = true)
        {
            var p = new ShelterProduction("shelter.s005", Legacy, A, Fuel, 1, 2, 5, now, multiRecipes: new[] { Recipe });
            if (modules)
            {
                var donor = new InventoryContainer(2); donor.TryAdd(A, 10);
                p.Claim(); p.Install(ShelterModule.Storage, donor); p.Install(ShelterModule.Workbench, donor);
            }
            return p;
        }
        public CraftRequest Request(long nonce = 1, long? revision = null, string source = "shelter.storage") =>
            new CraftRequest("shelter.s005", Production.CraftEpoch, nonce, Recipe.recipeId, Recipe.definitionRevision, source, revision ?? Source.Revision);
        public void Dispose() { UnityEngine.Object.DestroyImmediate(Recipe); UnityEngine.Object.DestroyImmediate(Legacy); }
        public static int Count(CraftItemSnapshot[] payload, ItemDefinition item)
        {
            if (payload != null) foreach (var a in payload) if (a.itemId == item.Id.Value) return a.quantity;
            return 0;
        }
    }
}
