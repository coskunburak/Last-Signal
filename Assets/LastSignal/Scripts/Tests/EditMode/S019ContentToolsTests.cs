using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LastSignal.Authoring;
using LastSignal.Editor.S019;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Loot;
using LastSignal.Persistence;
using LastSignal.Shelter;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Presets;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public sealed class S019ContentToolsTests
    {
        readonly List<Object> owned = new List<Object>();
        ItemDefinition item, output; ItemCatalog catalog; LootProfile profile; ShelterRecipe recipe;
        T Own<T>(T value) where T : Object { owned.Add(value); return value; }
        static void Set(object obj, string field, object value) => obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(obj, value);
        [SetUp] public void Setup()
        {
            var prefab = Own(new GameObject("World prefab")); prefab.AddComponent<WorldItem>();
            item = Own(ScriptableObject.CreateInstance<ItemDefinition>()); Set(item, "stableId", new StableItemId("test.input")); Set(item, "worldPrefab", prefab);
            output = Own(ScriptableObject.CreateInstance<ItemDefinition>()); Set(output, "stableId", new StableItemId("test.output")); Set(output, "worldPrefab", prefab);
            catalog = Own(ScriptableObject.CreateInstance<ItemCatalog>()); Set(catalog, "items", new List<ItemDefinition> { item, output });
            profile = Own(ScriptableObject.CreateInstance<LootProfile>()); Set(profile, "entries", new[] { new LootEntry(item, 1, 1, 3) });
            recipe = Own(ScriptableObject.CreateInstance<ShelterRecipe>()); recipe.recipeId = "test.recipe"; recipe.input = item; recipe.output = output;
        }
        [TearDown] public void Teardown() { for (int i = owned.Count - 1; i >= 0; i--) if (owned[i]) Object.DestroyImmediate(owned[i]); owned.Clear(); }
        List<ContentIssue> Report() => ContentValidation.Definitions(new[] { item, output }, new[] { catalog }, new[] { profile }, new[] { recipe });
        void Has(string code, string field = null)
        {
            var issues = Report(); Assert.That(issues.Any(i => i.errorCode == code && (field == null || i.fieldName == field)), Is.True, string.Join("\n", issues.Select(x => x.ToString())));
            Assert.That(issues.All(i => !string.IsNullOrEmpty(i.recommendedFix) && !string.IsNullOrEmpty(i.fieldName)));
        }
        [Test] public void ValidContentHasNoErrors() => Assert.That(Report(), Is.Empty);
        [Test] public void DuplicateDefinitionId() { Set(output, "stableId", item.Id); Has("ITEM_DUPLICATE", "stableId.id"); }
        [Test] public void MissingDefinitionId() { Set(item, "stableId", new StableItemId("")); Has("ITEM_ID"); }
        [Test] public void MissingCatalogMembership() { Set(catalog, "items", new List<ItemDefinition> { output }); Has("ITEM_UNREGISTERED"); Has("ITEM_REFERENCE", "input"); }
        [Test] public void CatalogNullAndDuplicateEntries() { Set(catalog, "items", new List<ItemDefinition> { item, item, null, output }); Has("CATALOG_NULL", "items[2]"); Has("CATALOG_DUPLICATE"); }
        [Test] public void StackRejectedWithoutOnValidateClamping() { Set(item, "maxStack", 0); Has("ITEM_STACK"); }
        [Test] public void MissingWorldPrefab() { Set(item, "worldPrefab", null); Has("ITEM_PREFAB"); Has("LOOT_PREFAB"); }
        [Test] public void MissingRecipeInput() { recipe.input = null; Has("ITEM_REFERENCE", "input"); }
        [Test] public void RecipeQuantityRevisionDuration() { recipe.inputQuantity = 0; recipe.revision = 0; recipe.durationWorldSeconds = double.NaN; Has("RECIPE_INPUT_QUANTITY"); Has("RECIPE_REVISION"); Has("RECIPE_DURATION"); }
        [Test] public void RecipeCycleUsesExistingContract()
        {
            var reverse = Own(ScriptableObject.CreateInstance<ShelterRecipe>()); reverse.recipeId = "reverse"; reverse.input = output; reverse.output = item;
            var issues = ContentValidation.Definitions(new[] { item, output }, new[] { catalog }, Array.Empty<LootProfile>(), new[] { recipe, reverse });
            Assert.That(issues.Any(i => i.errorCode == "RECIPE_CATALOG"));
        }
        [TestCase(0, 1, 2, "LOOT_WEIGHT")]
        [TestCase(-1, 1, 2, "LOOT_WEIGHT")]
        [TestCase(1, 0, 2, "LOOT_QUANTITY")]
        [TestCase(1, 4, 2, "LOOT_QUANTITY")]
        public void InvalidLootEntry(int weight, int min, int max, string code) { Set(profile, "entries", new[] { new LootEntry(item, weight, min, max) }); Has(code); }
        [Test] public void CopiedInactiveIdentityBlocksBuild()
        {
            var root = Own(new GameObject("Root"));
            for (int i = 0; i < 2; i++) { var child = new GameObject("Copy"); child.transform.SetParent(root.transform); child.SetActive(false); Set(child.AddComponent<PersistentEntityId>(), "id", "same"); }
            var issues = ContentValidation.Roots(new[] { root }, "Assets/Test.unity");
            Assert.That(issues.Any(i => i.errorCode == "ENTITY_DUPLICATE" && i.assetPath == "Assets/Test.unity" && i.objectPath.Contains("Copy")));
            Assert.Throws<BuildFailedException>(() => ContentBuildValidation.RequireValid(issues));
        }
        [Test] public void CopiedLootIdAndGeneratedIdentityCollisionAreRejected()
        {
            var root = Own(new GameObject("Root"));
            var point = root.AddComponent<LootSpawnPoint>(); Set(point, "stableId", "anchor"); Set(point, "profile", profile);
            Set(root.AddComponent<PersistentEntityId>(), "id", "loot:anchor");
            Assert.That(ContentValidation.Roots(new[] { root }, "test").Any(i => i.errorCode == "ENTITY_DUPLICATE"));
        }
        [Test] public void DiagnosticsHaveStableOrder()
        {
            Set(output, "stableId", item.Id);
            var report = Report(); var reversed = ContentValidation.Sort(report.AsEnumerable().Reverse());
            Assert.That(reversed.Select(x => x.ToString()), Is.EqualTo(report.Select(x => x.ToString())));
        }
        [Test] public void PreviewMatchesRealSelectorAndDoesNotMutateInputsOrGlobalRandom()
        {
            var random = UnityEngine.Random.state; string before = EditorJsonUtility.ToJson(profile);
            var first = LootPreview.Sample(profile, int.MaxValue - 4, 1000, "real.anchor");
            var second = LootPreview.Sample(profile, int.MaxValue - 4, 1000, "real.anchor");
            int empty = 0, count = 0; long quantity = 0;
            for (int i = 0; i < 1000; i++) { var selection = profile.Select(unchecked(int.MaxValue - 4 + i), "real.anchor"); if (selection.Outcome == LootOutcome.Empty) empty++; else { count++; quantity += selection.Quantity; } }
            Assert.That(first.empty, Is.EqualTo(empty)); Assert.That(first.items[item.Id.Value].occurrences, Is.EqualTo(count)); Assert.That(first.items[item.Id.Value].quantity, Is.EqualTo(quantity));
            Assert.That(second.empty, Is.EqualTo(first.empty)); Assert.That(second.items[item.Id.Value].quantity, Is.EqualTo(quantity));
            Assert.That(EditorJsonUtility.ToJson(profile), Is.EqualTo(before)); Assert.That(UnityEngine.Random.state, Is.EqualTo(random));
        }
        [Test] public void PreviewRejectsInvalidInputAndCountsEmpty()
        {
            Set(profile, "emptyBasisPoints", 10000); var result = LootPreview.Sample(profile, 0, 12, "point"); Assert.That(result.empty, Is.EqualTo(12)); Assert.That(result.items, Is.Empty);
            Assert.Throws<ArgumentOutOfRangeException>(() => LootPreview.Sample(profile, 0, 0, "point"));
            Set(profile, "entries", new[] { new LootEntry(item, -1, 1, 1) }); Assert.Throws<ArgumentException>(() => LootPreview.Sample(profile, 0, 1, "point"));
        }
        [Test] public void FingerprintOrderCosmeticsAndSchema()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Items/ammo.rifle.prefab"); Assert.That(prefab, Is.Not.Null);
            Set(item, "worldPrefab", prefab); Set(output, "worldPrefab", prefab);
            string Hash(ItemDefinition[] items) => ContentFingerprint.Compute(items, Array.Empty<ItemCatalog>(), Array.Empty<LootProfile>(), new[] { recipe });
            string initial = Hash(new[] { item, output }); int schema = SaveValidation.SchemaVersion;
            Set(item, "displayName", "New cosmetic name"); Set(item, "description", "New text");
            var texture = Own(new Texture2D(2, 2)); var icon = Own(Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero)); Set(item, "icon", icon);
            Assert.That(Hash(new[] { output, item }), Is.EqualTo(initial)); Assert.That(SaveValidation.SchemaVersion, Is.EqualTo(schema));
            Set(item, "maxStack", 5); Assert.That(Hash(new[] { item, output }), Is.Not.EqualTo(initial));
        }
        [TestCase("Assets/ThirdParty/EnvironmentColor/a.png", null)]
        [TestCase("Assets/LastSignal/Art/S019Import/EnvironmentColor/a.png", "EnvironmentColor")]
        [TestCase("Assets/LastSignal/Art/S019Import/StaticMesh/a.fbx", "StaticMesh")]
        [TestCase("Assets/LastSignal/Art/S019Import/Audio/a.wav", "Audio")]
        [TestCase("Assets/LastSignal/Art/S019Import/AudioExtra/a.wav", null)]
        [TestCase("Assets/LastSignal/Art/S019Import/Audio/../a.wav", null)]
        public void ImportScoping(string path, string expected) => Assert.That(ContentImportPolicy.Profile(path), Is.EqualTo(expected));
        [Test] public void NativeTexturePresetIsIdempotentAndPreservesInterpretation()
        {
            string folder = ContentImportPolicy.Root + "EnvironmentColor/Test-" + Guid.NewGuid().ToString("N"); string path = folder + "/sample.png";
            try
            {
                Directory.CreateDirectory(folder); var texture = Own(new Texture2D(2, 2)); File.WriteAllBytes(path, texture.EncodeToPNG()); AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.mipmapEnabled = true; importer.anisoLevel = 4;
                var preset = Own(new Preset(importer)); importer.mipmapEnabled = false; importer.anisoLevel = 0; importer.sRGBTexture = false; importer.maxTextureSize = 128;
                string scoped = path;
                Assert.That(ContentImportPolicy.Apply(scoped, importer, preset), Is.True);
                Assert.That(importer.mipmapEnabled, Is.True); Assert.That(importer.anisoLevel, Is.EqualTo(4));
                Assert.That(importer.sRGBTexture, Is.False); Assert.That(importer.maxTextureSize, Is.EqualTo(128));
                string once = EditorJsonUtility.ToJson(importer); Assert.That(ContentImportPolicy.Apply(scoped, importer, preset), Is.True); Assert.That(EditorJsonUtility.ToJson(importer), Is.EqualTo(once));
                Assert.That(ContentImportPolicy.Apply("Assets/ThirdParty/sample.png", importer, preset), Is.False);
            }
            finally { AssetDatabase.DeleteAsset(folder); if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }
        [Test] public void PilotTemplateCreatesUniqueInstancesAndUndoRemovesOnlyNewInstance()
        {
            var template = AssetDatabase.LoadAssetAtPath<GameObject>(PoiAuthoring.TemplatePath); Assert.That(template, Is.Not.Null);
            var layout = template.GetComponent<PoiAuthoringLayout>(); Assert.That(layout.entry && layout.retreat && layout.navigationGeometry && layout.encounter);
            Assert.That(layout.lootAnchors.Length, Is.EqualTo(2));
            var issues = ContentValidation.Roots(new[] { template }, PoiAuthoring.TemplatePath);
            Assert.That(issues.Where(i => i.severity == ContentSeverity.Error), Is.Empty, string.Join("\n", issues.Select(x => x.ToString())));
            // Batch EditMode can start with an unsaved untitled scene. A preview scene
            // isolates this fixture without saving/replacing the user's scene setup.
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                string original = template.GetComponentInChildren<LootSpawnPoint>().StableId;
                var first = PoiAuthoring.CreateNew(template, scene); var second = PoiAuthoring.CreateNew(template, scene);
                Assert.That(first.GetComponentInChildren<LootSpawnPoint>().StableId, Is.Not.EqualTo(second.GetComponentInChildren<LootSpawnPoint>().StableId));
                Assert.That(template.GetComponentInChildren<LootSpawnPoint>().StableId, Is.EqualTo(original));
                Assert.That(ContentValidation.Roots(new[] { first, second }, "test").Any(i => i.errorCode == "ENTITY_DUPLICATE"), Is.False);
                Undo.PerformUndo(); Assert.That(second == null); Assert.That(first != null);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
