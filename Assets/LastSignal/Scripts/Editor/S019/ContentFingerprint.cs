using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LastSignal.Inventory.Data;
using LastSignal.Loot;
using LastSignal.Shelter;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Editor.S019
{
    public static class ContentFingerprint
    {
        // This is an audit format version, NOT SaveValidation.SchemaVersion or SaveHeader.contentVersion.
        public const int FormatVersion = 2;
        static string Ref(UnityEngine.Object asset)
        {
            if (!asset) return "";
            return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long local)
                ? guid + ":" + local.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : throw new ArgumentException("Fingerprint requires saved asset references.");
        }
        public static string Compute(IEnumerable<ItemDefinition> items, IEnumerable<ItemCatalog> catalogs,
            IEnumerable<LootProfile> profiles, IEnumerable<ShelterRecipe> recipes)
        {
            // Sorting encoded semantic records removes enumeration order without using paths or instance IDs.
            var records = new List<byte[]>();
            void Record(Action<BinaryWriter> write)
            {
                using var stream = new MemoryStream(); using (var writer = new BinaryWriter(stream, Encoding.UTF8, true)) write(writer);
                records.Add(stream.ToArray());
            }
            foreach (var item in items) Record(w => { w.Write("item"); w.Write(item.Id.Value ?? ""); w.Write((int)item.Category); w.Write(item.MaxStack); w.Write((int)item.Use); w.Write(item.MassGrams); w.Write(item.HydrationPoints); w.Write(item.NutritionPoints); w.Write(item.TreatmentSeconds); w.Write(item.BleedingReduction); w.Write(item.BackpackSlotBonus); w.Write(item.EquippedRecoveryMultiplier); w.Write(Ref(item.WorldPrefab)); });
            foreach (var catalog in catalogs) Record(w =>
            {
                w.Write("catalog"); w.Write(Ref(catalog));
                var ids = catalog.EditorItems.Select(x => x ? x.Id.Value ?? "" : "").OrderBy(x => x, StringComparer.Ordinal).ToArray();
                w.Write(ids.Length); foreach (var id in ids) w.Write(id);
            });
            foreach (var profile in profiles) Record(w =>
            {
                w.Write("loot"); w.Write(Ref(profile)); w.Write(profile.EmptyBasisPoints); w.Write(profile.EntryCount);
                // Entry order changes seeded selection, so it is intentionally preserved.
                for (int i = 0; i < profile.EntryCount; i++) { var e = profile.GetEntry(i); w.Write(e.Item ? e.Item.Id.Value ?? "" : ""); w.Write(e.Weight); w.Write(e.MinQuantity); w.Write(e.MaxQuantity); }
            });
            foreach (var recipe in recipes) Record(w =>
            {
                w.Write("recipe"); w.Write(recipe.recipeId ?? ""); w.Write(recipe.revision);
                w.Write(recipe.input ? recipe.input.Id.Value ?? "" : ""); w.Write(recipe.output ? recipe.output.Id.Value ?? "" : ""); w.Write(recipe.tool ? recipe.tool.Id.Value ?? "" : "");
                w.Write(recipe.inputQuantity); w.Write(recipe.outputQuantity); w.Write(recipe.durationWorldSeconds); w.Write((int)recipe.station); w.Write(recipe.requiresPower);
            });
            using var canonical = new MemoryStream();
            using (var writer = new BinaryWriter(canonical, Encoding.UTF8, true))
            {
                writer.Write(FormatVersion); writer.Write(records.Count);
                foreach (var record in records.OrderBy(r => Convert.ToBase64String(r), StringComparer.Ordinal)) { writer.Write(record.Length); writer.Write(record); }
            }
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(canonical.ToArray())).Replace("-", "").ToLowerInvariant();
        }
        [MenuItem("Last Signal/S019/Record content fingerprint")]
        public static void RecordProject()
        {
            ContentBuildValidation.RequireValid(ContentValidation.Definitions(ContentValidation.Assets<ItemDefinition>(), ContentValidation.Assets<ItemCatalog>(), ContentValidation.Assets<LootProfile>(), ContentValidation.Assets<ShelterRecipe>()));
            string hash = Compute(ContentValidation.Assets<ItemDefinition>(), ContentValidation.Assets<ItemCatalog>(), ContentValidation.Assets<LootProfile>(), ContentValidation.Assets<ShelterRecipe>());
            const string folder = "Docs/Implementation/S019/Evidence/Fingerprints";
            Directory.CreateDirectory(folder); string path = folder + "/" + hash + ".txt";
            if (!File.Exists(path)) File.WriteAllText(path, "fingerprintFormat=1\ncatalogHash=" + hash + "\nSave schema, compatibility contentVersion and buildId unchanged.\n");
            Debug.Log("S019 catalog fingerprint: " + hash + "\n" + path);
        }
    }
}
