using System;
using System.Collections.Generic;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;

namespace LastSignal.Shelter
{
    public enum RecipeError
    {
        None, InvalidCatalog, InvalidId, DuplicateId, InvalidRevision, UnknownItem, InvalidQuantity,
        DuplicateIngredient, InvalidStation, InvalidTime, InvalidTool, InvalidUnlock, UnsupportedSource,
        IncompatibleCatalog, SelfConversion, CycleRequiresApproval, ProtectedItem
    }
    public readonly struct RecipeValidation
    {
        public readonly RecipeError Code;
        public readonly string Detail;
        public bool Valid => Code == RecipeError.None;
        public RecipeValidation(RecipeError code, string detail) { Code = code; Detail = detail; }
    }
    // Compiled once per session. Runtime jobs never read mutable recipe arrays/quantities.
    internal sealed class CompiledRecipe
    {
        internal string Id, Fingerprint;
        internal int Revision, CatalogVersion;
        internal ItemAmount[] Inputs, Outputs;
        internal IngredientSource[] Sources;
        internal ItemDefinition[] Tools;
        internal CraftStation Station;
        internal CraftCapability Capabilities;
        internal CraftUnlock Unlock;
        internal CraftTimeBasis TimeBasis;
        internal double Duration;
        internal bool Power;
    }
    public sealed class RecipeCatalog
    {
        readonly Dictionary<string, CompiledRecipe> recipes = new Dictionary<string, CompiledRecipe>(StringComparer.Ordinal);
        internal bool Find(string id, out CompiledRecipe recipe) => recipes.TryGetValue(id ?? "", out recipe);
        public static bool ValidId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 128 || value != value.Trim()) return false;
            foreach (char c in value) if (char.IsControl(c)) return false;
            return true;
        }
        public static RecipeValidation Validate(RecipeDefinition[] definitions, ShelterRecipe[] legacy = null, ItemCatalog items = null)
            => TryCreate(definitions, legacy, items, out _);
        internal static RecipeValidation TryCreate(RecipeDefinition[] definitions, ShelterRecipe[] legacy, ItemCatalog items, out RecipeCatalog catalog)
        {
            catalog = null;
            if (definitions == null || definitions.Length > 64) return Error(RecipeError.InvalidCatalog, "Expected at most 64 recipes.");
            var result = new RecipeCatalog();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var canonical = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
            var graph = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            if (items)
            {
                foreach (var item in items.EditorItems)
                    if (!item || !ValidId(item.Id.Value) || !canonical.TryAdd(item.Id.Value, item))
                        return Error(RecipeError.UnknownItem, "Item catalog contains null/invalid/duplicate definitions.");
            }
            if (legacy != null)
            {
                if (!ShelterRecipe.ValidateCatalog(legacy)) return Error(RecipeError.InvalidCatalog, "Invalid legacy catalog.");
                foreach (var r in legacy)
                {
                    ids.Add(r.recipeId);
                    var e = CheckItem(r.input, canonical, items); if (!e.Valid) return e;
                    e = CheckItem(r.output, canonical, items); if (!e.Valid) return e;
                    Edge(graph, r.input.Id.Value, r.output.Id.Value);
                }
            }
            foreach (var d in definitions)
            {
                if (!d || !ValidId(d.recipeId)) return Error(RecipeError.InvalidId, "Missing/invalid recipe ID.");
                if (!ids.Add(d.recipeId)) return Error(RecipeError.DuplicateId, d.recipeId);
                if (d.definitionRevision < 1) return Error(RecipeError.InvalidRevision, d.recipeId);
                if (d.catalogVersion != 1) return Error(RecipeError.IncompatibleCatalog, d.recipeId);
                if (!Enum.IsDefined(typeof(CraftStation), d.requiredStation) ||
                    (d.requiredCapabilities & ~CraftCapability.WoodProcessing) != 0 ||
                    (d.requiredStation == CraftStation.Handcraft && (d.requiresPower || d.requiredCapabilities != CraftCapability.None)))
                    return Error(RecipeError.InvalidStation, d.recipeId);
                if (!Enum.IsDefined(typeof(CraftTimeBasis), d.timeBasis) ||
                    (d.requiredStation == CraftStation.Handcraft) != (d.timeBasis == CraftTimeBasis.SimulationSeconds) ||
                    !double.IsFinite(d.duration) || d.duration < 1 || d.duration > 86400)
                    return Error(RecipeError.InvalidTime, d.recipeId);
                if (!Enum.IsDefined(typeof(CraftUnlock), d.unlockRequirement)) return Error(RecipeError.InvalidUnlock, d.recipeId);
                var error = Amounts(d.inputIngredients, true, canonical, items, out var inputs, out var sources);
                if (!error.Valid) return error;
                error = Amounts(d.outputDefinitions, false, canonical, items, out var outputs, out _);
                if (!error.Valid) return error;
                if (d.requiredTools == null || d.requiredTools.Length > 16) return Error(RecipeError.InvalidTool, d.recipeId);
                var toolIds = new HashSet<string>(StringComparer.Ordinal);
                var toolCopy = (ItemDefinition[])d.requiredTools.Clone();
                foreach (var tool in toolCopy)
                {
                    error = CheckItem(tool, canonical, items); if (!error.Valid) return error;
                    if (!toolIds.Add(tool.Id.Value)) return Error(RecipeError.InvalidTool, "Duplicate tool: " + d.recipeId);
                    foreach (var input in inputs) if (input.Item.Id == tool.Id)
                        return Error(RecipeError.InvalidTool, "Tools must not be consumed: " + d.recipeId);
                }
                Array.Sort(toolCopy, (a,b) => string.CompareOrdinal(a.Id.Value, b.Id.Value));
                foreach (var input in inputs) foreach (var output in outputs)
                {
                    if (input.Item.Id == output.Item.Id) return Error(RecipeError.SelfConversion, d.recipeId);
                    Edge(graph, input.Item.Id.Value, output.Item.Id.Value);
                }
                var compiled = new CompiledRecipe { Id = d.recipeId, Revision = d.definitionRevision, CatalogVersion = d.catalogVersion,
                    Inputs = inputs, Outputs = outputs, Sources = sources, Tools = toolCopy, Station = d.requiredStation,
                    Capabilities = d.requiredCapabilities, Unlock = d.unlockRequirement, TimeBasis = d.timeBasis,
                    Duration = d.duration, Power = d.requiresPower };
                compiled.Fingerprint = CraftIdentity.Definition(compiled);
                result.recipes.Add(compiled.Id, compiled);
            }
            // No approved cyclic economy policy exists. Even lossy/ambiguous cycles need explicit review.
            var visiting = new HashSet<string>(); var visited = new HashSet<string>();
            foreach (var id in graph.Keys) if (Cycle(id, graph, visiting, visited))
                return Error(RecipeError.CycleRequiresApproval, "Conversion cycle at " + id);
            catalog = result;
            return default;
        }
        static RecipeValidation CheckItem(ItemDefinition item, Dictionary<string, ItemDefinition> canonical, bool requireCatalog)
        {
            if (!item || !ValidId(item.Id.Value) || item.MaxStack < 1 || item.MaxStack > int.MaxValue / 256 || !item.ValidGameplayData)
                return Error(RecipeError.UnknownItem, "Missing item, invalid identity, stack or gameplay data.");
            if (canonical.TryGetValue(item.Id.Value, out var existing))
                return existing == item ? default : Error(RecipeError.UnknownItem, "Aliased item ID: " + item.Id.Value);
            if (requireCatalog) return Error(RecipeError.UnknownItem, "Item absent from save catalog: " + item.Id.Value);
            canonical.Add(item.Id.Value, item); return default;
        }
        static RecipeValidation Amounts(RecipeAmount[] values, bool input, Dictionary<string, ItemDefinition> canonical, bool requireCatalog,
            out ItemAmount[] amounts, out IngredientSource[] sources)
        {
            amounts = null; sources = null;
            if (values == null || values.Length == 0 || values.Length > 32) return Error(RecipeError.InvalidQuantity, "Expected 1..32 entries.");
            var sorted = (RecipeAmount[])values.Clone();
            var ids = new HashSet<string>();
            foreach (var v in sorted)
            {
                if (v == null) return Error(RecipeError.UnknownItem, "Null ingredient.");
                var e = CheckItem(v.item, canonical, requireCatalog); if (!e.Valid) return e;
                if (!ids.Add(v.item.Id.Value)) return Error(RecipeError.DuplicateIngredient, v.item.Id.Value);
                if (v.quantity <= 0 || v.quantity > 256L * v.item.MaxStack) return Error(RecipeError.InvalidQuantity, v.item.Id.Value);
                if (!Enum.IsDefined(typeof(IngredientSource), v.sourcePolicy) || (!input && v.sourcePolicy != IngredientSource.SelectedContainer))
                    return Error(RecipeError.UnsupportedSource, v.item.Id.Value);
                // Existing inventory has no favorite flag; quest identity is the existing project contract.
                if (input && (v.item.Id.Value.StartsWith("quest.", StringComparison.Ordinal) || v.item.Id.Value.StartsWith("item.quest.", StringComparison.Ordinal)))
                    return Error(RecipeError.ProtectedItem, v.item.Id.Value);
            }
            Array.Sort(sorted, (a,b) => string.CompareOrdinal(a.item.Id.Value, b.item.Id.Value));
            amounts = new ItemAmount[sorted.Length]; sources = new IngredientSource[sorted.Length];
            for (int i = 0; i < sorted.Length; i++) { amounts[i] = new ItemAmount(sorted[i].item, sorted[i].quantity); sources[i] = sorted[i].sourcePolicy; }
            return default;
        }
        static RecipeValidation Error(RecipeError code, string detail) => new RecipeValidation(code, detail);
        static void Edge(Dictionary<string, List<string>> graph, string from, string to)
        { if (!graph.TryGetValue(from, out var next)) graph.Add(from, next = new List<string>()); next.Add(to); }
        static bool Cycle(string id, Dictionary<string, List<string>> graph, HashSet<string> visiting, HashSet<string> visited)
        {
            if (visiting.Contains(id)) return true;
            if (!visited.Add(id)) return false;
            visiting.Add(id);
            if (graph.TryGetValue(id, out var next)) foreach (var to in next) if (Cycle(to, graph, visiting, visited)) return true;
            visiting.Remove(id); return false;
        }
    }
}
