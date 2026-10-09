using System;
using System.Collections.Generic;
using System.Linq;
using LastSignal.Authoring;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Loot;
using LastSignal.Persistence;
using LastSignal.Shelter;
using LastSignal.WorldCells;
using UnityEditor;
using Unity.AI.Navigation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSignal.Editor.S019
{
    public enum ContentSeverity { Error, Warning }
    public sealed class ContentIssue
    {
        public ContentSeverity severity;
        public string errorCode, assetPath, objectPath, fieldName, message, recommendedFix;
        public override string ToString() => $"{severity} {errorCode} | {assetPath} | {objectPath} | {fieldName}: {message} Fix: {recommendedFix}";
    }

    public static class ContentValidation
    {
        public const string Scope = "Assets/LastSignal";
        public static List<ContentIssue> Sort(IEnumerable<ContentIssue> issues) => issues
            .OrderBy(i => i.assetPath, StringComparer.Ordinal).ThenBy(i => i.objectPath, StringComparer.Ordinal)
            .ThenBy(i => i.fieldName, StringComparer.Ordinal).ThenBy(i => i.errorCode, StringComparer.Ordinal)
            .ThenBy(i => i.message, StringComparer.Ordinal).ToList();
        public static T[] Assets<T>() where T : Object => AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { Scope })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.Ordinal)
            .Select(AssetDatabase.LoadAssetAtPath<T>).Where(a => a).ToArray();
        static bool Id(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 128;
        static string Location(Object obj) => obj ? AssetDatabase.GetAssetPath(obj) : "";
        public static void Add(List<ContentIssue> issues, Object obj, string code, string field, string message,
            string fix, string asset = null, string path = "", ContentSeverity severity = ContentSeverity.Error)
        {
            issues.Add(new ContentIssue { severity = severity, errorCode = code, assetPath = asset ?? Location(obj),
                objectPath = path, fieldName = field, message = message, recommendedFix = fix });
        }
        public static List<ContentIssue> Definitions(IEnumerable<ItemDefinition> definitions, IEnumerable<ItemCatalog> catalogs,
            IEnumerable<LootProfile> profiles, IEnumerable<ShelterRecipe> recipes)
        {
            var issues = new List<ContentIssue>();
            var items = definitions.Where(x => x).OrderBy(Location, StringComparer.Ordinal).ToArray();
            var books = catalogs.Where(x => x).OrderBy(Location, StringComparer.Ordinal).ToArray();
            var members = new HashSet<ItemDefinition>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var book in books)
            {
                var local = new HashSet<string>(StringComparer.Ordinal);
                if (book.EditorItems == null || book.EditorItems.Count == 0)
                    Add(issues, book, "CATALOG_EMPTY", "items", "Catalog is empty.", "Register production item definitions.");
                if (book.EditorItems == null) continue;
                for (int i = 0; i < book.EditorItems.Count; i++)
                {
                    var item = book.EditorItems[i]; string field = $"items[{i}]";
                    if (!item) Add(issues, book, "CATALOG_NULL", field, "Missing item reference.", "Assign the intended definition.");
                    else
                    {
                        members.Add(item);
                        if (!Id(item.Id.Value)) Add(issues, book, "CATALOG_ID", field, "Invalid item identity.", "Repair the definition identity explicitly.");
                        if (!local.Add(item.Id.Value ?? "")) Add(issues, book, "CATALOG_DUPLICATE", field, "Duplicate item identity.", "Remove duplicate registration or resolve identity collision.");
                    }
                }
            }
            foreach (var item in items)
            {
                if (!Id(item.Id.Value)) Add(issues, item, "ITEM_ID", "stableId.id", "Missing or oversized identity.", "Assign a stable ID of 1..128 characters; review save compatibility first.");
                else if (!ids.Add(item.Id.Value)) Add(issues, item, "ITEM_DUPLICATE", "stableId.id", "Duplicate definition ID: " + item.Id.Value, "Resolve collision without renaming released identities.");
                if (!members.Contains(item)) Add(issues, item, "ITEM_UNREGISTERED", "stableId.id", "Definition is absent from all project catalogs.", "Register in the owning ItemCatalog.");
                if (item.MaxStack < 1) Add(issues, item, "ITEM_STACK", "maxStack", "Stack must be positive.", "Set maxStack >= 1.");
                if (!item.HasValidUse) Add(issues, item, "ITEM_USE", "use", "Use/category/stack contract is invalid.", "Match the existing ItemDefinition.HasValidUse contract.");
                if (!item.WorldPrefab && item.Category != ItemCategory.Tool)
                    Add(issues, item, "ITEM_PREFAB", "worldPrefab", "Required world prefab is missing.", "Assign an active WorldItem prefab.");
                else if (item.WorldPrefab && (!item.WorldPrefab.activeSelf || !item.WorldPrefab.GetComponent<WorldItem>()))
                    Add(issues, item, "ITEM_PREFAB", "worldPrefab", "World prefab must be active and contain WorldItem.", "Use the project WorldItem prefab convention.");
            }
            foreach (var profile in profiles.Where(x => x).OrderBy(Location, StringComparer.Ordinal))
            {
                if (profile.EmptyBasisPoints < 0 || profile.EmptyBasisPoints > 10000)
                    Add(issues, profile, "LOOT_EMPTY_CHANCE", "emptyBasisPoints", "Chance is outside 0..10000.", "Use basis points in range.");
                var seen = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < profile.EntryCount; i++)
                {
                    var e = profile.GetEntry(i); string f = $"entries[{i}]";
                    Reference(issues, profile, e.Item, f + ".item", members);
                    if (e.Item && !seen.Add(e.Item.Id.Value ?? "")) Add(issues, profile, "LOOT_DUPLICATE", f + ".item", "Repeated item identity.", "Combine the weighted entry.");
                    if (e.Weight <= 0) Add(issues, profile, "LOOT_WEIGHT", f + ".weight", "Weight must be positive.", "Set weight >= 1.");
                    if (e.MinQuantity < 1 || e.MaxQuantity < e.MinQuantity)
                        Add(issues, profile, "LOOT_QUANTITY", f + ".minQuantity/maxQuantity", "Invalid quantity range.", "Set 1 <= minQuantity <= maxQuantity.");
                    if (e.Item && (!e.Item.WorldPrefab || !e.Item.WorldPrefab.activeSelf || !e.Item.WorldPrefab.GetComponent<WorldItem>()))
                        Add(issues, profile, "LOOT_PREFAB", f + ".item.worldPrefab", "Loot requires an active WorldItem prefab, including tools.", "Assign a valid world prefab on this definition.");
                }
                if (!profile.Validate(out var error)) Add(issues, profile, "LOOT_CONTRACT", "entries", error, "Resolve the reported LootProfile contract.");
            }
            var recipeArray = recipes.Where(x => x).OrderBy(Location, StringComparer.Ordinal).ToArray(); ids.Clear();
            foreach (var recipe in recipeArray)
            {
                if (!Id(recipe.recipeId)) Add(issues, recipe, "RECIPE_ID", "recipeId", "Invalid recipe ID.", "Assign a stable ID of 1..128 characters.");
                else if (!ids.Add(recipe.recipeId)) Add(issues, recipe, "RECIPE_DUPLICATE", "recipeId", "Duplicate recipe ID.", "Resolve collision explicitly.");
                Reference(issues, recipe, recipe.input, "input", members);
                Reference(issues, recipe, recipe.output, "output", members);
                if (recipe.tool) Reference(issues, recipe, recipe.tool, "tool", members);
                if (recipe.revision < 1) Add(issues, recipe, "RECIPE_REVISION", "revision", "Revision must be positive.", "Set an explicit positive revision.");
                if (recipe.inputQuantity < 1 || (recipe.input && recipe.inputQuantity > 256L * recipe.input.MaxStack))
                    Add(issues, recipe, "RECIPE_INPUT_QUANTITY", "inputQuantity", "Quantity exceeds the production contract.", "Use 1..256 * input.MaxStack.");
                if (recipe.outputQuantity < 1 || (recipe.output && recipe.outputQuantity > 256L * recipe.output.MaxStack))
                    Add(issues, recipe, "RECIPE_OUTPUT_QUANTITY", "outputQuantity", "Quantity exceeds the production contract.", "Use 1..256 * output.MaxStack.");
                if (double.IsNaN(recipe.durationWorldSeconds) || double.IsInfinity(recipe.durationWorldSeconds) || recipe.durationWorldSeconds < 1 || recipe.durationWorldSeconds > 86400)
                    Add(issues, recipe, "RECIPE_DURATION", "durationWorldSeconds", "Duration must be finite and 1..86400.", "Set duration in world seconds.");
                if (!recipe.Valid) Add(issues, recipe, "RECIPE_CONTRACT", "input/output/station", "ShelterRecipe.Valid is false.", "Use distinct input/output, valid quantities and the existing Workbench station.");
            }
            if (recipeArray.Length > 0 && !ShelterRecipe.ValidateCatalog(recipeArray))
                foreach (var recipe in recipeArray) Add(issues, recipe, "RECIPE_CATALOG", "input/output/recipeId", "Catalog fails the existing recipe count, uniqueness, validity or conversion-cycle contract.", "Resolve invalid recipes or cyclic conversions; maximum 64 recipes.");
            return Sort(issues);
        }
        static void Reference(List<ContentIssue> issues, Object owner, ItemDefinition item, string field, HashSet<ItemDefinition> members)
        {
            if (!item || !Id(item.Id.Value) || !members.Contains(item)) Add(issues, owner, "ITEM_REFERENCE", field,
                "Missing, invalid or unregistered item reference.", "Assign a valid item registered in the owning catalog.");
        }
        public static string Hierarchy(Transform t)
        {
            var parts = new Stack<string>();
            while (t) { parts.Push(t.name + "[" + t.GetSiblingIndex() + "]"); t = t.parent; }
            return string.Join("/", parts);
        }
        public static List<ContentIssue> Roots(IEnumerable<GameObject> roots, string assetPath, bool requireAuthoredIdentities = true)
        {
            var issues = new List<ContentIssue>(); var identities = new HashSet<string>(StringComparer.Ordinal);
            var scopedIdentities = new HashSet<string>(StringComparer.Ordinal);
            var all = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).OrderBy(Hierarchy, StringComparer.Ordinal).ToArray();
            foreach (var t in all)
            {
                string path = Hierarchy(t);
                void Issue(string code, string field, string message, string fix) => Add(issues, t, code, field, message, fix, assetPath, path);
                void Identity(string id, string field)
                {
                    if (!Id(id)) Issue("ENTITY_ID", field, "Missing or oversized persistent identity.", "Assign identity explicitly before release.");
                    else if (!identities.Add(id)) Issue("ENTITY_DUPLICATE", field, "Duplicate persistence identity: " + id, "For a NEW object use an explicit new identity; never silently rekey released objects.");
                }
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)
                    Issue("MISSING_SCRIPT", "m_Component", "Missing script.", "Restore the script/reference.");
                void ScopedIdentity(string id, string field, string domain)
                {
                    if (!Id(id)) Issue("AUTHORED_ID", field, "Missing or oversized authored identity.", "Assign a stable identity explicitly.");
                    else if (!scopedIdentities.Add(domain + ":" + id)) Issue("AUTHORED_DUPLICATE", field, "Duplicate authored identity: " + id, "Resolve the duplicate within its ownership domain.");
                }
                var socket = t.GetComponent<ShelterSocket>();
                if (socket) ScopedIdentity(socket.socketId, "ShelterSocket.socketId", "socket:" + (socket.site ? socket.site.shelterId : "missing"));
                var relay = t.GetComponent<LastSignal.Objectives.RelayPoint>();
                if (relay) ScopedIdentity(relay.stableId, "RelayPoint.stableId", "relay");
                var entity = t.GetComponent<PersistentEntityId>();
                if (entity) Identity(entity.Id, "PersistentEntityId.id");
                if (requireAuthoredIdentities && (t.GetComponent<DoorInteractable>() || t.GetComponent<ZombieEncounter>()) && !entity)
                    Issue("ENTITY_REQUIRED", "PersistentEntityId", "Door/encounter has no persistence identity.", "Add and explicitly assign PersistentEntityId.");
                var point = t.GetComponent<LootSpawnPoint>();
                if (point)
                {
                    Identity(point.StableId, "LootSpawnPoint.stableId");
                    // Runtime produces this identity for a populated opportunity.
                    if (Id(point.StableId)) Identity("loot:" + point.StableId, "LootSpawnPoint.generatedEntityId");
                    if (!point.Validate(out var error)) Issue("LOOT_POINT", "LootSpawnPoint.profile/clearance/transform", error, "Assign valid profile, upright unit transform and positive clearance.");
                }
                var encounter = t.GetComponent<ZombieEncounter>();
                if (encounter)
                {
                    var so = new SerializedObject(encounter);
                    foreach (string f in new[] { "prefab", "spawn" })
                        if (!so.FindProperty(f).objectReferenceValue) Issue("ENCOUNTER_REFERENCE", "ZombieEncounter." + f, "Missing encounter reference.", "Assign existing production zombie and local spawn marker.");
                }
                var cell = t.GetComponent<WorldCellContent>();
                if (cell && (!cell.entry || !cell.ground || !cell.navigation || !cell.loot || cell.requiredSolids == null))
                    Issue("CELL_REFERENCE", "WorldCellContent", "Cell navigation/collision/loot references incomplete.", "Complete the existing cell contract; do not add another streaming owner.");
                var layout = t.GetComponent<PoiAuthoringLayout>();
                if (layout)
                {
                    var surface = layout.navigationGeometry ? layout.navigationGeometry.GetComponent<NavMeshSurface>() : null;
                    if (!surface) Issue("POI_NAVIGATION", "navigationGeometry.NavMeshSurface", "Missing native navigation surface.", "Assign the existing NavMeshSurface to the geometry root.");
                    else if (!surface.navMeshData)
                        Add(issues, t, "POI_NAVIGATION_UNBAKED", "navigationGeometry.NavMeshSurface.navMeshData", "Pilot navigation has not been baked.", "Bake the native surface and verify entry/retreat/encounter reachability before integration.", assetPath, path,
                            assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ? ContentSeverity.Error : ContentSeverity.Warning);
                    foreach (var pair in new[] { ("entry", layout.entry), ("retreat", layout.retreat), ("navigationGeometry", layout.navigationGeometry) })
                        if (!pair.Item2 || !pair.Item2.IsChildOf(t)) Issue("POI_REFERENCE", pair.Item1, "Missing or external POI marker.", "Assign a marker inside this POI.");
                    if (layout.lootAnchors == null || layout.lootAnchors.Length == 0) Issue("POI_LOOT", "lootAnchors", "No loot anchors.", "Assign local LootSpawnPoints.");
                    else foreach (var anchor in layout.lootAnchors)
                        if (!anchor || !anchor.transform.IsChildOf(t)) Issue("POI_LOOT", "lootAnchors", "Missing or external loot anchor.", "Assign local LootSpawnPoints.");
                    if (!layout.encounter || !layout.encounter.transform.IsChildOf(t)) Issue("POI_ENCOUNTER", "encounter", "Missing or external encounter.", "Assign the local existing ZombieEncounter.");
                }
                var site = t.GetComponent<ShelterSite>();
                if (site) ScopedIdentity(site.shelterId, "ShelterSite.shelterId", "shelter");
                if (site && !site.Validate()) Issue("SHELTER_CONTRACT", "ShelterSite.recipe/sockets/material/fuel", "Existing shelter authoring contract failed.", "Resolve ShelterSite.Validate requirements.");
                var manager = t.GetComponent<WorldCellManager>();
                if (manager)
                {
                    var so = new SerializedObject(manager); var defs = so.FindProperty("definitions");
                    var coordinates = new HashSet<string>(StringComparer.Ordinal);
                    var catalog = so.FindProperty("catalog").objectReferenceValue as ItemCatalog;
                    if (!catalog) Issue("CELL_CATALOG", "WorldCellManager.catalog", "Missing streaming catalog.", "Assign the runtime ItemCatalog.");
                    for (int index = 0; index < defs.arraySize; index++)
                    {
                        var definition = defs.GetArrayElementAtIndex(index).objectReferenceValue as WorldCellContent;
                        if (!definition || !coordinates.Add(definition.coordinate.Id)) Issue("CELL_DEFINITION", "WorldCellManager.definitions[" + index + "]", "Missing or duplicate streamed cell definition.", "Assign one definition per coordinate.");
                        if (definition && catalog)
                            foreach (var anchor in definition.GetComponentsInChildren<LootSpawnPoint>(true))
                                if (anchor.Profile) for (int j = 0; j < anchor.Profile.EntryCount; j++)
                                    if (!(catalog.EditorItems?.Contains(anchor.Profile.GetEntry(j).Item) ?? false)) Issue("CELL_ITEM_REFERENCE", "WorldCellManager.catalog", "Streamed loot item is not in the runtime catalog.", "Register this cell's loot items.");
                    }
                }
                var session = t.GetComponent<SaveSession>();
                if (session)
                {
                    try { SaveSession.PathForSlot("validation", session.SaveSlot); }
                    catch (ArgumentException) { Issue("SAVE_SLOT", "saveSlot", "Invalid save slot filename.", "Use 1..64 ASCII letters, digits, hyphens or underscores; production default is current."); }
                    if (!session.Catalog) Issue("SESSION_CATALOG", "catalog", "Missing save catalog.", "Assign the actual runtime catalog.");
                    else
                    {
                        var memberSet = new HashSet<ItemDefinition>(session.Catalog.EditorItems ?? Array.Empty<ItemDefinition>());
                        foreach (var shelter in all.Select(x => x.GetComponent<ShelterSite>()).Where(x => x))
                        {
                            var references = new List<ItemDefinition> { shelter.material, shelter.fuel };
                            if (shelter.recipe) { references.Add(shelter.recipe.input); references.Add(shelter.recipe.output); if (shelter.recipe.tool) references.Add(shelter.recipe.tool); }
                            foreach (var reference in references)
                                if (!reference || !memberSet.Contains(reference)) Issue("SESSION_RECIPE_REFERENCE", "catalog", "Shelter resource/recipe item absent from this save catalog.", "Register shelter items in this session catalog.");
                        }
                        foreach (var marker in all.Select(x => x.GetComponent<LootSpawnPoint>()).Where(x => x && x.Profile))
                            for (int i = 0; i < marker.Profile.EntryCount; i++)
                                if (!memberSet.Contains(marker.Profile.GetEntry(i).Item))
                                    Issue("SESSION_ITEM_REFERENCE", "catalog", "Scene loot item absent from this save catalog: " + Location(marker.Profile), "Register loot items in this session catalog.");
                    }
                }
            }
            return Sort(issues);
        }
        public static List<ContentIssue> Project()
        {
            var issues = Definitions(Assets<ItemDefinition>(), Assets<ItemCatalog>(), Assets<LootProfile>(), Assets<ShelterRecipe>());
            foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { Scope }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(x => x, StringComparer.Ordinal))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab) issues.AddRange(Roots(new[] { prefab }, path, prefab.GetComponent<PoiAuthoringLayout>() || prefab.GetComponent<WorldCellContent>()));
            }
            return Sort(issues);
        }
    }
}
