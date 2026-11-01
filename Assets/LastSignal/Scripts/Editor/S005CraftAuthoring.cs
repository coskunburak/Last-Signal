using System;
using System.Collections.Generic;
using System.IO;
using LastSignal.Inventory.Data;
using LastSignal.Persistence;
using LastSignal.Shelter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal.Editor
{
    /// <summary>Owner-invoked opt-in integration. Never edits/promotes the source scene or its catalog.</summary>
    public static class S005CraftAuthoring
    {
        const string RecipePath = "Assets/LastSignal/Data/Crafting/PrototypeLogToPlank.asset";
        // Only an isolated prototype copy is rebound; live/runtime alias validation stays strict.
        public static RecipeDefinition BindPrototypeToCatalog(RecipeDefinition template, IReadOnlyList<ItemDefinition> items)
        {
            if (!template || !template.prototypeBalance || items == null)
                throw new InvalidOperationException("S005_PROTOTYPE_REQUIRED");
            var canonical = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
            foreach (var item in items)
            {
                if (!item || !item.Id.IsValid || canonical.ContainsKey(item.Id.Value))
                    throw new InvalidOperationException("S005_INVALID_SOURCE_CATALOG");
                canonical.Add(item.Id.Value, item);
            }
            ItemDefinition Resolve(ItemDefinition item)
            {
                if (!item) throw new InvalidOperationException("S005_MISSING_ITEM");
                if (!canonical.TryGetValue(item.Id.Value, out var existing) || existing == item) return item;
                if (existing.Category != item.Category || existing.MaxStack != item.MaxStack ||
                    existing.MassGrams != item.MassGrams || existing.Use != item.Use ||
                    existing.HydrationPoints != item.HydrationPoints || existing.NutritionPoints != item.NutritionPoints ||
                    existing.TreatmentSeconds != item.TreatmentSeconds || existing.BleedingReduction != item.BleedingReduction ||
                    existing.BackpackSlotBonus != item.BackpackSlotBonus || existing.EquippedRecoveryMultiplier != item.EquippedRecoveryMultiplier)
                    throw new InvalidOperationException("S005_ITEM_GAMEPLAY_MISMATCH: " + item.Id);
                return existing;
            }
            RecipeAmount[] Amounts(RecipeAmount[] values) => Array.ConvertAll(values, a =>
                new RecipeAmount { item = Resolve(a.item), quantity = a.quantity, sourcePolicy = a.sourcePolicy });
            var copy = UnityEngine.Object.Instantiate(template);
            try
            {
                copy.inputIngredients = Amounts(template.inputIngredients);
                copy.outputDefinitions = Amounts(template.outputDefinitions);
                copy.requiredTools = Array.ConvertAll(template.requiredTools, Resolve);
                return copy;
            }
            catch { UnityEngine.Object.DestroyImmediate(copy); throw; }
        }
        [MenuItem("Last Signal/BLD/P02/S005/Create Isolated Craft Stage")]
        public static void CreateStage()
        {
            var source = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.sceneCount != 1 || !source.IsValid() || !source.isLoaded ||
                source.isDirty || string.IsNullOrEmpty(source.path))
                throw new InvalidOperationException("S005_SOURCE: open exactly one saved, clean source scene outside Play mode.");
            ShelterSite site = null; SaveSession save = null;
            foreach (var root in source.GetRootGameObjects())
            {
                foreach (var found in root.GetComponentsInChildren<ShelterSite>(true))
                { if (site) throw new InvalidOperationException("S005_SOURCE: multiple shelter owners."); site = found; }
                foreach (var found in root.GetComponentsInChildren<SaveSession>(true))
                { if (save) throw new InvalidOperationException("S005_SOURCE: multiple save owners."); save = found; }
            }
            var recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(RecipePath);
            if (!site || !site.Validate() || !save || !save.Catalog || !recipe)
                throw new InvalidOperationException("S005_SOURCE: valid ShelterSite, SaveSession/catalog and prototype recipe required.");
            var template = recipe;
            recipe = BindPrototypeToCatalog(template, save.Catalog.EditorItems);
            var definitions = new List<RecipeDefinition>(site.multiRecipes ?? Array.Empty<RecipeDefinition>());
            definitions.Remove(template);
            definitions.Add(recipe);
            var legacy = new List<ShelterRecipe> { site.recipe }; legacy.AddRange(site.additionalRecipes ?? Array.Empty<ShelterRecipe>());
            var check = RecipeCatalog.Validate(definitions.ToArray(), legacy.ToArray());
            if (!check.Valid) throw new InvalidOperationException(check.Code + ": " + check.Detail);
            var items = new List<ItemDefinition>(save.Catalog.EditorItems);
            void Include(ItemDefinition item)
            {
                foreach (var existing in items)
                    if (existing.Id == item.Id) { if (existing != item) throw new InvalidOperationException("S005_ITEM_ALIAS: " + item.Id); return; }
                items.Add(item);
            }
            foreach (var definition in definitions)
            {
                foreach (var a in definition.inputIngredients) Include(a.item);
                foreach (var a in definition.outputDefinitions) Include(a.item);
                foreach (var tool in definition.requiredTools) Include(tool);
            }
            var sourceCatalog = save.Catalog;
            string stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string folder = "Assets/LastSignal/Data/Crafting/Stage-" + stamp;
            string target = "Assets/LastSignal/Scenes/Validation/S005Craft_" + stamp + ".unity";
            if (File.Exists(target)) throw new InvalidOperationException("S005_TARGET_EXISTS");
            // SaveAsCopy leaves source assets untouched. Everything after this point targets the new copy.
            if (!EditorSceneManager.SaveScene(source, target, true)) throw new IOException("Could not create isolated scene copy.");
            // Opening a scene can destroy unpersisted ScriptableObject clones.
            // Persist both isolated assets while the source scene is still loaded.
            AssetDatabase.CreateFolder("Assets/LastSignal/Data/Crafting", "Stage-" + stamp);
            AssetDatabase.CreateAsset(recipe, folder + "/LogToPlank.asset");
            var catalog = UnityEngine.Object.Instantiate(sourceCatalog);
            catalog.name = "S005Catalog";
            var serialized = new SerializedObject(catalog);
            var entries = serialized.FindProperty("items"); entries.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++) entries.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(catalog, folder + "/S005Catalog.asset");
            AssetDatabase.SaveAssets();
            if (!EditorUtility.IsPersistent(recipe) || !EditorUtility.IsPersistent(catalog))
                throw new InvalidOperationException("S005_STAGE_ASSETS_NOT_PERSISTED");
            EditorSceneManager.OpenScene(target, OpenSceneMode.Single);
            site = UnityEngine.Object.FindAnyObjectByType<ShelterSite>(); save = UnityEngine.Object.FindAnyObjectByType<SaveSession>();
            site.multiRecipes = definitions.ToArray();
            var saveObject = new SerializedObject(save);
            saveObject.FindProperty("catalog").objectReferenceValue = catalog;
            saveObject.FindProperty("saveSlot").stringValue = "s005-" + stamp;
            saveObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(site);
            EditorSceneManager.MarkSceneDirty(site.gameObject.scene);
            if (!EditorSceneManager.SaveScene(site.gameObject.scene)) throw new IOException("Could not save isolated S005 stage.");
            AssetDatabase.SaveAssets();
            Debug.Log("S005_STAGE_CREATED: " + target + ". Owner must validate gathering clearance and workbench access. No G1 approval implied.");
        }
    }
}
