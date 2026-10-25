using System;
using LastSignal.Shelter;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class P02RecipeValidationTests
    {
        P02CraftFixture f;
        [SetUp] public void Setup() => f = new P02CraftFixture();
        [TearDown] public void Cleanup() => f.Dispose();
        RecipeValidation Validate(params RecipeDefinition[] recipes) => RecipeCatalog.Validate(recipes, new[] { f.Legacy });
        [Test] public void ValidMultiInputCatalogAndExistingLegacyCoexist() => Assert.IsTrue(Validate(f.Recipe).Valid);
        [TestCase("null", RecipeError.UnknownItem)] [TestCase("zero", RecipeError.InvalidQuantity)]
        [TestCase("negative", RecipeError.InvalidQuantity)] [TestCase("overflow", RecipeError.InvalidQuantity)]
        [TestCase("duplicate", RecipeError.DuplicateIngredient)] [TestCase("empty", RecipeError.InvalidQuantity)]
        [TestCase("id", RecipeError.InvalidId)] [TestCase("revision", RecipeError.InvalidRevision)]
        [TestCase("catalog", RecipeError.IncompatibleCatalog)] [TestCase("self", RecipeError.SelfConversion)]
        [TestCase("toolConsumed", RecipeError.InvalidTool)] [TestCase("station", RecipeError.InvalidStation)]
        [TestCase("capability", RecipeError.InvalidStation)] [TestCase("time", RecipeError.InvalidTime)]
        [TestCase("source", RecipeError.UnsupportedSource)] [TestCase("unlock", RecipeError.InvalidUnlock)]
        [TestCase("quest", RecipeError.ProtectedItem)]
        public void UnsafeDefinitionsReturnActionableCodes(string invalid, RecipeError expected)
        {
            switch (invalid)
            {
                case "null": f.Recipe.inputIngredients[0].item = null; break;
                case "zero": f.Recipe.inputIngredients[0].quantity = 0; break;
                case "negative": f.Recipe.inputIngredients[0].quantity = -1; break;
                case "overflow": f.Recipe.inputIngredients[0].quantity = int.MaxValue; break;
                case "duplicate": f.Recipe.inputIngredients[1] = f.Recipe.inputIngredients[0]; break;
                case "empty": f.Recipe.inputIngredients = Array.Empty<RecipeAmount>(); break;
                case "id": f.Recipe.recipeId = " "; break;
                case "revision": f.Recipe.definitionRevision = 0; break;
                case "catalog": f.Recipe.catalogVersion = 2; break;
                case "self": f.Recipe.outputDefinitions[0].item = f.A; break;
                case "toolConsumed": f.Recipe.requiredTools = new[] { f.A }; break;
                case "station": f.Recipe.requiredStation = (CraftStation)99; break;
                case "capability": f.Recipe.requiredCapabilities = (CraftCapability)128; break;
                case "time": f.Recipe.duration = double.NaN; break;
                case "source": f.Recipe.inputIngredients[0].sourcePolicy = (IngredientSource)99; break;
                case "unlock": f.Recipe.unlockRequirement = (CraftUnlock)99; break;
                case "quest": f.Recipe.inputIngredients[0].item = P02CraftFixture.Item("quest.relay-fuse"); break;
            }
            long rev = f.Source.Revision;
            Assert.AreEqual(expected, Validate(f.Recipe).Code);
            Assert.Throws<ArgumentException>(() => f.Create());
            Assert.AreEqual(rev, f.Source.Revision, "Invalid catalog cannot authorize mutation.");
            Assert.AreEqual(6, f.Source.GetTotalQuantity(f.A)); Assert.AreEqual(4, f.Source.GetTotalQuantity(f.B));
        }
        [Test] public void DuplicateRecipeIdIncludingLegacyIsRejected()
        {
            Assert.AreEqual(RecipeError.DuplicateId, Validate(f.Recipe, f.Recipe).Code);
            f.Recipe.recipeId = f.Legacy.recipeId; Assert.AreEqual(RecipeError.DuplicateId, Validate(f.Recipe).Code);
        }
        [Test] public void CycleAcrossNewAndLegacyRecipesFailsClosedEvenIfApparentlyLossy()
        {
            f.Recipe.inputIngredients = new[] { P02CraftFixture.Amount(f.Output, 20) };
            f.Recipe.outputDefinitions = new[] { P02CraftFixture.Amount(f.A, 1) };
            Assert.AreEqual(RecipeError.CycleRequiresApproval, Validate(f.Recipe).Code);
        }
        [Test] public void MultiRecipeFreeResourceCycleIsRejected()
        {
            var reverse = UnityEngine.Object.Instantiate(f.Recipe);
            try
            {
                reverse.recipeId = "recipe.reverse"; reverse.inputIngredients = new[] { P02CraftFixture.Amount(f.Output, 1) };
                reverse.outputDefinitions = new[] { P02CraftFixture.Amount(f.C, 20) };
                Assert.AreEqual(RecipeError.CycleRequiresApproval, Validate(f.Recipe, reverse).Code);
            }
            finally { UnityEngine.Object.DestroyImmediate(reverse); }
        }
        [TestCase(-1)] [TestCase(0)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
        public void InvalidDurationsAreRejected(double duration)
        { f.Recipe.duration = duration; Assert.AreEqual(RecipeError.InvalidTime, Validate(f.Recipe).Code); }
        [Test] public void ExistingJobUsesCompiledDefinitionEvenIfAuthoringObjectChanges()
        {
            f.Recipe.inputIngredients[0].quantity = 999; f.Recipe.outputDefinitions[0].quantity = 999;
            Assert.AreEqual(CraftReason.Accepted, f.Production.StartCraft(f.Request(), f.Source).reason);
            f.Production.ApplyElapsed(0, 10);
            Assert.AreEqual(3, P02CraftFixture.Count(f.Production.Capture().crafting.job.pendingOutput, f.Output));
        }
        [Test] public void PrototypeAssetUsesActualHarvestLogAndApprovedPlankIdentity()
        {
            var recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>("Assets/LastSignal/Data/Crafting/PrototypeLogToPlank.asset");
            Assert.IsNotNull(recipe); Assert.IsTrue(recipe.prototypeBalance);
            Assert.AreEqual("material.log", recipe.inputIngredients[0].item.Id.Value);
            Assert.AreEqual("item.resource.plank", recipe.outputDefinitions[0].item.Id.Value);
            Assert.AreEqual(4, recipe.outputDefinitions[0].item.MaxStack); Assert.AreEqual(1500, recipe.outputDefinitions[0].item.MassGrams);
            Assert.IsTrue(recipe.outputDefinitions[0].item.WorldPrefab);
            Assert.IsTrue(RecipeCatalog.Validate(new[] { recipe }).Valid);
        }
        [Test] public void AliasedItemDefinitionAndMissingCatalogItemAreRejected()
        {
            var alias = UnityEngine.Object.Instantiate(f.A);
            try { f.Recipe.inputIngredients[1].item = alias; Assert.AreEqual(RecipeError.UnknownItem, Validate(f.Recipe).Code); }
            finally { UnityEngine.Object.DestroyImmediate(alias); }
            f.Recipe.inputIngredients[1].item = f.A;
            var catalog = ScriptableObject.CreateInstance<LastSignal.Inventory.Data.ItemCatalog>();
            try { Assert.AreEqual(RecipeError.UnknownItem, RecipeCatalog.Validate(new[] { f.Recipe }, items: catalog).Code); }
            finally { UnityEngine.Object.DestroyImmediate(catalog); }
        }
    }
}
