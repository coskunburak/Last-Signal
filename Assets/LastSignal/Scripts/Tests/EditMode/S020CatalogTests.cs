using System;
using System.Collections.Generic;
using System.Linq;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Loot;
using LastSignal.Persistence;
using LastSignal.Shelter;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public sealed class S020CatalogTests
    {
        const string Root = "Assets/LastSignal/Data/";
        ItemDefinition Item(string id) => AssetDatabase.LoadAssetAtPath<ItemDefinition>(Root + "Items/Definitions/" + id + ".asset");
        ShelterRecipe Recipe(string name) => AssetDatabase.LoadAssetAtPath<ShelterRecipe>(Root + "Shelter/" + name + ".asset");
        ItemCatalog Catalog => AssetDatabase.LoadAssetAtPath<ItemCatalog>(Root + "Items/Definitions/ItemCatalog.asset");
        ShelterProduction Production() => new ShelterProduction("s020.fixture", Recipe("RifleAmmunition"), Item("material.scrap"), Item("fuel.generator"), 2, 6, 1800, 0,
            new[] { Recipe("Rag"), Recipe("ExpeditionPack"), Recipe("SewingKit") });
        InventoryContainer Supplies(ShelterProduction production)
        {
            var source = new InventoryContainer(24);
            source.TryAdd(Item("material.scrap"), 2);
            Assert.IsTrue(production.Claim()); Assert.IsTrue(production.Install(ShelterModule.Workbench, source));
            source.TryAdd(Item("material.cloth"), 40); source.TryAdd(Item("tool.sewing-kit"), 1);
            return source;
        }
        [Test] public void ReleaseCatalogHasSeventeenDistinctResolvableDefinitionsAndMetadata()
        {
            Assert.AreEqual(17, Catalog.EditorItems.Count);
            var ids = new HashSet<string>();
            foreach (var item in Catalog.EditorItems)
            {
                Assert.IsNotNull(item); Assert.IsTrue(item.Id.IsValid); Assert.IsTrue(ids.Add(item.Id.Value), item.name);
                Assert.AreSame(item, Catalog.GetItem(item.Id)); Assert.IsTrue(item.HasValidUse, item.name);
                Assert.Greater(item.MaxStack, 0); Assert.Greater(item.MassGrams, 0);
                Assert.IsFalse(string.IsNullOrWhiteSpace(item.DisplayName)); Assert.IsFalse(string.IsNullOrWhiteSpace(item.Description));
                Assert.IsNotNull(item.Icon, item.name);
                if (item.WorldPrefab) Assert.IsNotNull(item.WorldPrefab.GetComponent<WorldItem>());
            }
        }
        [TestCase("food.crackers", 4)] [TestCase("drink.soda", 3)] [TestCase("medical.pressure-dressing", 3)]
        [TestCase("medical.rag", 5)] [TestCase("material.cloth", 10)] [TestCase("tool.sewing-kit", 1)] [TestCase("equipment.expedition-pack", 1)]
        public void AccumulationNeverExceedsStackOrCapacity(string id, int stack)
        {
            var item = Item(id); var container = new InventoryContainer(256);
            Assert.AreEqual(256 * stack, container.TryAdd(item, int.MaxValue)); Assert.AreEqual(0, container.TryAdd(item, 1));
            for (int i = 0; i < 256; i++) Assert.AreEqual(stack, container.GetSlot(i).Quantity);
            Assert.IsFalse(container.TryRemove(item, 256 * stack + 1)); Assert.AreEqual(256 * stack, container.GetTotalQuantity(item));
        }
        [Test] public void MassUsesWideIntegerArithmeticAndTracksActualQuantities()
        {
            var item = Object.Instantiate(Item("material.cloth"));
            try
            {
                var data = new SerializedObject(item); data.FindProperty("massGrams").intValue = int.MaxValue;
                data.ApplyModifiedPropertiesWithoutUndo(); var inventory = new InventoryContainer(256);
                Assert.AreEqual(2560, inventory.TryAdd(item, 2560));
                Assert.AreEqual((long)int.MaxValue * 2560, inventory.TotalMassGrams);
                Assert.IsTrue(inventory.TryRemove(item, 1)); Assert.AreEqual((long)int.MaxValue * 2559, inventory.TotalMassGrams);
            }
            finally { Object.DestroyImmediate(item); }
        }
        [Test] public void NewPrefabsOwnTheirCorrectDefinitions()
        {
            foreach (var id in new[] { "food.crackers", "drink.soda", "medical.pressure-dressing", "medical.rag", "material.cloth", "tool.sewing-kit", "equipment.expedition-pack" })
            {
                var item = Item(id); Assert.IsNotNull(item.WorldPrefab); Assert.IsTrue(item.WorldPrefab.activeSelf);
                Assert.AreSame(item, item.WorldPrefab.GetComponent<WorldItem>().Definition);
                Assert.IsNotNull(item.WorldPrefab.GetComponent<Collider>());
            }
        }
        [Test] public void PartialTreatmentDoesNotHealResourcesAndInvalidReductionCannotMutate()
        {
            var state = new SurvivalState(); state.Advance(21600); state.AddWound(); state.AddWound(); state.AddWound();
            double water = state.Hydration, food = state.Nutrition; long revision = state.WoundRevision;
            state.Treat(Item("medical.rag").BleedingReduction);
            Assert.AreEqual(2, state.Bleeding); Assert.IsFalse(state.CanTreat(revision));
            Assert.AreEqual(water, state.Hydration); Assert.AreEqual(food, state.Nutrition);
            Assert.Throws<ArgumentOutOfRangeException>(() => state.Treat(0)); Assert.AreEqual(2, state.Bleeding);
        }
        [Test] public void InvalidNewEffectDataFailsExistingUseContract()
        {
            var copy = Object.Instantiate(Item("food.crackers"));
            try
            {
                var data = new SerializedObject(copy); data.FindProperty("nutritionPoints").floatValue = float.NaN; data.ApplyModifiedPropertiesWithoutUndo();
                Assert.IsFalse(copy.HasValidUse);
            }
            finally { Object.DestroyImmediate(copy); }
        }
        [Test] public void CatalogRejectsDuplicateRecipeIdsAndConversionCycles()
        {
            var rag = Recipe("Rag"); var copy = Object.Instantiate(rag);
            try
            {
                Assert.IsFalse(ShelterRecipe.ValidateCatalog(new[] { rag, copy }));
                copy.recipeId = "recipe.test.reverse"; copy.input = rag.output; copy.output = rag.input;
                Assert.IsFalse(ShelterRecipe.ValidateCatalog(new[] { rag, copy }));
                copy.inputQuantity = 0; Assert.IsFalse(copy.Valid);
            }
            finally { Object.DestroyImmediate(copy); }
        }
        [Test] public void RepeatedUtilityCraftingConservesClothAndCollectsExactlyOnce()
        {
            var production = Production(); var source = Supplies(production); var recipe = Recipe("Rag");
            double now = 0;
            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(production.Start(source, recipe.recipeId)); Assert.IsFalse(production.Start(source, recipe.recipeId));
                production.ApplyElapsed(now, now + recipe.durationWorldSeconds); now += recipe.durationWorldSeconds;
                Assert.IsTrue(production.Collect(source)); Assert.IsFalse(production.Collect(source));
                Assert.AreEqual(40 - (i + 1) * 2, source.GetTotalQuantity(Item("material.cloth")));
                Assert.AreEqual(i + 1, source.GetTotalQuantity(Item("medical.rag")));
            }
            Assert.AreEqual(1, source.GetTotalQuantity(Item("tool.sewing-kit")));
        }
        [TestCase(false)] [TestCase(true)]
        public void PendingJobRestoresItsOwnRecipeAndPreservesBlockedOutputOrRefund(bool cancel)
        {
            var production = Production(); var source = Supplies(production); var recipe = Recipe("ExpeditionPack");
            Assert.IsTrue(production.Start(source, recipe.recipeId));
            if (cancel) Assert.IsTrue(production.Cancel()); else production.ApplyElapsed(0, recipe.durationWorldSeconds);
            double now = cancel ? 0 : recipe.durationWorldSeconds; var snapshot = production.Capture();
            var restored = Production(); Assert.IsTrue(restored.CanRestore(snapshot, now)); restored.Restore(snapshot, now);
            var full = new InventoryContainer(1); full.TryAdd(Item("tool.wrench"), 1);
            Assert.IsFalse(restored.Collect(full)); Assert.AreEqual(snapshot.job.id, restored.Capture().job.id);
            Assert.IsTrue(restored.Collect(source)); Assert.IsFalse(restored.Collect(source));
            Assert.AreEqual(cancel ? 40 : 10, source.GetTotalQuantity(Item("material.cloth")));
            Assert.AreEqual(cancel ? 0 : 1, source.GetTotalQuantity(Item("equipment.expedition-pack")));
            Assert.AreEqual(1, source.GetTotalQuantity(Item("tool.sewing-kit")));
        }
        [Test] public void GuaranteedWorkshopResourcesCanProduceKitAndPackWithoutLuckyLoot()
        {
            var p = Production(); var source = Supplies(p);
            source.TryRemove(Item("tool.sewing-kit"), 1); source.TryAdd(Item("tool.wrench"), 1); source.TryAdd(Item("material.scrap"), 2);
            Assert.IsTrue(p.Start(source, Recipe("SewingKit").recipeId)); p.ApplyElapsed(0, 300);
            Assert.IsTrue(p.Collect(source)); Assert.AreEqual(0, source.GetTotalQuantity(Item("material.scrap")));
            Assert.IsTrue(p.Start(source, Recipe("ExpeditionPack").recipeId)); p.ApplyElapsed(300, 1200);
            Assert.IsTrue(p.Collect(source)); Assert.AreEqual(1, source.GetTotalQuantity(Item("equipment.expedition-pack")));
            Assert.AreEqual(1, source.GetTotalQuantity(Item("tool.sewing-kit"))); Assert.AreEqual(1, source.GetTotalQuantity(Item("tool.wrench")));
        }
        [Test] public void MissingToolAndUnknownRecipeDoNotConsumeInputs()
        {
            var p = Production(); var source = Supplies(p); source.TryRemove(Item("tool.sewing-kit"), 1);
            Assert.IsFalse(p.Start(source, Recipe("ExpeditionPack").recipeId)); Assert.IsFalse(p.Start(source, "unknown"));
            Assert.AreEqual(40, source.GetTotalQuantity(Item("material.cloth"))); Assert.IsNull(p.Capture().job);
        }
        [Test] public void IncompatiblePendingRecipeIsRejectedWithoutChangingEscrow()
        {
            var p = Production(); var source = Supplies(p); Assert.IsTrue(p.Start(source, Recipe("Rag").recipeId));
            var snapshot = p.Capture(); snapshot.job.recipeId = "removed.recipe";
            Assert.IsFalse(p.CanRestore(snapshot, 0)); Assert.Throws<ArgumentException>(() => p.Restore(snapshot, 0));
            Assert.AreEqual(Recipe("Rag").recipeId, p.Capture().job.recipeId); Assert.AreEqual(38, source.GetTotalQuantity(Item("material.cloth")));
        }
        static double Probability(LootProfile profile, string id)
        {
            int weight = 0, total = 0;
            for (int i = 0; i < profile.EntryCount; i++) { var e = profile.GetEntry(i); total += e.Weight; if (e.Item.Id.Value == id) weight += e.Weight; }
            return (1 - profile.EmptyBasisPoints / 10000d) * weight / total;
        }
        [TestCase("Kitchen", "drink.water", .26)] [TestCase("Kitchen", "medical.bandage", .065)]
        [TestCase("Clinic", "drink.water", .075)] [TestCase("Clinic", "medical.bandage", .675)]
        [TestCase("Workshop", "material.scrap", .52)]
        public void EssentialAbsoluteProbabilitiesArePreserved(string name, string id, double expected)
        { Assert.AreEqual(expected, Probability(AssetDatabase.LoadAssetAtPath<LootProfile>(Root + "Loot/Profiles/" + name + ".asset"), id), 1e-12); }
        [TestCase("Kitchen")] [TestCase("Clinic")] [TestCase("Workshop")]
        public void WorstAndBoundarySeedsAreDeterministicAndValid(string name)
        {
            var p = AssetDatabase.LoadAssetAtPath<LootProfile>(Root + "Loot/Profiles/" + name + ".asset"); Assert.IsTrue(p.Validate(out var error), error);
            foreach (int seed in Enumerable.Range(-512, 1024).Concat(new[] { int.MinValue, int.MaxValue }))
            {
                var a = p.Select(seed, "s020.fixed-point"); var b = p.Select(seed, "s020.fixed-point");
                Assert.AreEqual(a.Outcome, b.Outcome); Assert.AreEqual(a.Item, b.Item); Assert.AreEqual(a.Quantity, b.Quantity);
                Assert.That(a.Outcome, Is.EqualTo(LootOutcome.Empty).Or.EqualTo(LootOutcome.Spawned));
                if (a.Outcome == LootOutcome.Spawned) { Assert.AreSame(a.Item, Catalog.GetItem(a.Item.Id)); Assert.That(a.Quantity, Is.InRange(1, a.Item.MaxStack)); }
            }
        }
        [Test] public void DuplicateStableIdsAreReportedByS019Validator()
        {
            var item = Item("food.crackers"); var duplicate = Object.Instantiate(item);
            try
            {
                var issues = LastSignal.Editor.S019.ContentValidation.Definitions(new[] { item, duplicate }, new[] { Catalog }, Array.Empty<LootProfile>(), Array.Empty<ShelterRecipe>());
                Assert.IsTrue(issues.Any(issue => issue.errorCode == "ITEM_DUPLICATE"));
            }
            finally { Object.DestroyImmediate(duplicate); }
        }
        [TestCase("StartingWater", "drink.water", 1)] [TestCase("StartingBandage", "medical.bandage", 1)]
        [TestCase("WorkshopCloth", "material.cloth", 32)]
        public void GuaranteedSourcesCannotRollEmptyEvenOnWorstSeeds(string profile, string id, int quantity)
        {
            var p = AssetDatabase.LoadAssetAtPath<LootProfile>(Root + "Loot/Profiles/S020-" + profile + ".asset");
            foreach (int seed in Enumerable.Range(-1024, 2048).Concat(new[] { int.MinValue, int.MaxValue }))
            {
                var result = p.Select(seed, "s020.essential"); Assert.AreEqual(LootOutcome.Spawned, result.Outcome);
                Assert.AreEqual(id, result.Item.Id.Value); Assert.AreEqual(quantity, result.Quantity);
            }
        }
        [Test] public void LegacySaveRepeatedCodecRoundtripPreservesQuantityAndUnknownIdsFailExplicitly()
        {
            var limits = Catalog.EditorItems.ToDictionary(i => i.Id.Value, i => i.MaxStack);
            var codec = new SaveCodec(new SaveValidation("world.fixture", "content.1", limits, "rifle.1", 30));
            var original = SaveFoundationTests.Fixture(); var current = original;
            for (int i = 0; i < 4; i++)
            {
                Assert.IsTrue(codec.Encode(current, out var json).Success); Assert.IsTrue(codec.Decode(json, out current).Success);
                Assert.AreEqual(67, current.inventory.slots.Sum(s => s.quantity)); Assert.AreEqual(73, current.player.health);
            }
            current.inventory.slots[0].definitionId = "unsupported.legacy.item";
            Assert.AreEqual(SaveError.UnknownDefinition, codec.Encode(current, out _).Error);
            Assert.AreEqual(60, current.inventory.slots[0].quantity);
        }
    }
}
