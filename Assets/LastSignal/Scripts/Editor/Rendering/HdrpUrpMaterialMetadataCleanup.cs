using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace LastSignal.Editor.Rendering
{
    // Bounded cleanup of obsolete URP editor version subassets, not material conversion.
    public static class HdrpUrpMaterialMetadataCleanup
    {
        const string PlanPath = "Docs/Implementation/HDRPProductionOptimization/Evidence/urp-material-metadata-plan.json";
        const string PlanHash = "22076fffc67a647bb980e60ea974bbde5971efc74a7b6340969f59ad66e983e6";
        const string OldType = "UnityEditor.Rendering.Universal.AssetVersion";
        [Serializable] sealed class Item { public string path, sha256, metaSha256; }
        [Serializable] sealed class Guard { public string path, sha256; }
        [Serializable] sealed class Plan { public Item[] items; public Guard[] protectedInputs; }
        [Serializable] sealed class Entry
        {
            public string path, shader, status, reason, beforeSha256, afterSha256, metaSha256;
            public int removedSubassets;
        }
        [Serializable] sealed class Receipt
        {
            public string status = "PREFLIGHT", error, rollback, planSha256 = PlanHash;
            public string scope = "Project-owned HDRP materials only; no scene/prefab/component/package/vendor edits";
            public List<Entry> entries = new List<Entry>();
        }
        sealed class Work
        {
            public Item item;
            public Entry entry;
            public string backup;
            public Dictionary<long, string> preserved;
        }
        static bool IsOld(Object value) => value && value.GetType().FullName == OldType;
        static bool HasHdrpContract(Shader shader)
        {
            if (!shader || !shader.isSupported || ShaderUtil.ShaderHasError(shader)) return false;
            // Match production coverage; Material.GetTag can reflect stale override tags.
            if (shader.name.StartsWith("HDRP/", StringComparison.Ordinal) || shader.name.StartsWith("LastSignal/HDRP/", StringComparison.Ordinal)) return true;
            for (int i = 0; i < shader.subshaderCount; i++)
                if (shader.FindSubshaderTagValue(i, new ShaderTagId("RenderPipeline")).name == "HDRenderPipeline") return true;
            return false;
        }
        static void ValidateVersionOwnership(Material material, Object version, string path)
        {
            // Hidden native-file version objects need not report IsSubAsset. Validate actual
            // persistent ownership and distinct local IDs instead of that importer classification.
            if (!IsOld(version) || !EditorUtility.IsPersistent(version) || AssetDatabase.GetAssetPath(version) != path ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material, out string materialGuid, out long materialId) ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(version, out string versionGuid, out long versionId) ||
                materialGuid != versionGuid || materialGuid != AssetDatabase.AssetPathToGUID(path) || materialId == versionId)
                throw new InvalidOperationException("Unverified version-object ownership: " + path);
        }
        static string Hash(string path)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
        static Dictionary<long, string> Capture(string path)
        {
            var data = new Dictionary<long, string>();
            foreach (var value in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (!value || IsOld(value)) continue;
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long id))
                    throw new InvalidOperationException("Cannot capture persistent identity: " + path);
                data.Add(id, guid + "\n" + value.GetType().FullName + "\n" + EditorJsonUtility.ToJson(value));
            }
            return data;
        }
        public static void Run()
            => RunPlan(PlanPath, PlanHash, 241, false);

        public static void RunVendorHdrp()
            => RunPlan("Docs/Implementation/HDRPProductionOptimization/Evidence/vendor-hdrp-metadata-plan.json",
                "16c2b73c0d838a3548ef19b49a71c7a85b24416ef70408d8213c1e4c8c8dd07c", 36, true);

        static void VerifyGuards(Plan plan)
        {
            if (plan.protectedInputs == null) return;
            foreach (var guard in plan.protectedInputs)
                if (Hash(guard.path) != guard.sha256) throw new InvalidOperationException("Protected input changed: " + guard.path);
        }

        static void RunPlan(string planPath, string planHash, int expectedCount, bool vendor)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run outside Play mode.");
            if (!LastSignal.Rendering.RenderPipelineAdapter.IsHDRP) throw new InvalidOperationException("HDRP must be active.");
            if (Hash(planPath) != planHash) throw new InvalidOperationException("Reviewed metadata plan changed.");
            var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(planPath));
            if (plan.items == null || plan.items.Length != expectedCount || plan.items.Select(x => x.path).Distinct().Count() != expectedCount)
                throw new InvalidOperationException("Unexpected plan scope.");
            VerifyGuards(plan);
            string output = "Docs/Implementation/HDRPProductionOptimization/Evidence/" + (vendor ? "vendor-hdrp-metadata-" : "urp-material-metadata-") +
                DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ") + "-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(output);
            File.Copy(planPath, output + "/plan.json");
            var receipt = new Receipt { planSha256 = planHash };
            if (vendor) receipt.scope = "36 reviewed, already-HDRP vendor materials: remove only obsolete URP version objects; preserve native values/identities/metas and all original sources in backups.";
            var work = new List<Work>();
            var touched = new List<Work>();
            try
            {
                foreach (var item in plan.items)
                {
                    if (!item.path.StartsWith(vendor ? "Assets/ThirdParty/" : "Assets/LastSignal/", StringComparison.Ordinal) || !item.path.EndsWith(".mat", StringComparison.Ordinal) ||
                        item.path.Contains("..") || Hash(item.path) != item.sha256 || Hash(item.path + ".meta") != item.metaSha256)
                        throw new InvalidOperationException("Material input changed or invalid path: " + item.path);
                    var material = AssetDatabase.LoadAssetAtPath<Material>(item.path);
                    var entry = new Entry { path = item.path, beforeSha256 = item.sha256, metaSha256 = item.metaSha256, shader = material && material.shader ? material.shader.name : "" };
                    receipt.entries.Add(entry);
                    if (!material || !HasHdrpContract(material.shader))
                    {
                        if (vendor) throw new InvalidOperationException("Reviewed vendor HDRP contract changed: " + item.path);
                        entry.status = "SKIPPED_REQUIRES_REVIEW"; entry.reason = "No verified HDRP shader contract; retain URP version data.";
                        continue;
                    }
                    var old = AssetDatabase.LoadAllAssetsAtPath(item.path).Where(IsOld).ToArray();
                    if (vendor && old.Length != 1) throw new InvalidOperationException("Reviewed vendor version-object count changed: " + item.path);
                    if (old.Length == 0) { entry.status = "NO_URP_METADATA"; continue; }
                    foreach (var value in old) ValidateVersionOwnership(material, value, item.path);
                    var current = new Work { item = item, entry = entry, preserved = Capture(item.path), backup = output + "/before/" + item.path + ".backup" };
                    // Back up every eligible file before the first mutation in the entire batch.
                    Directory.CreateDirectory(Path.GetDirectoryName(current.backup));
                    File.Copy(item.path, current.backup);
                    File.Copy(item.path + ".meta", current.backup + ".meta");
                    work.Add(current); entry.status = "READY";
                }
                VerifyGuards(plan);
                foreach (var current in work)
                {
                    string path = current.item.path;
                    if (Hash(path) != current.item.sha256 || Hash(path + ".meta") != current.item.metaSha256)
                        throw new InvalidOperationException("Input changed after preflight: " + path);
                    touched.Add(current);
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    var old = AssetDatabase.LoadAllAssetsAtPath(path).Where(IsOld).ToArray();
                    foreach (var value in old) ValidateVersionOwnership(material, value, path);
                    if (AssetDatabase.LoadMainAssetAtPath(path) != material) AssetDatabase.SetMainObject(material, path);
                    foreach (var value in old)
                    {
                        AssetDatabase.RemoveObjectFromAsset(value);
                        if (AssetDatabase.Contains(value)) throw new InvalidOperationException("Version object was not detached: " + path);
                        Object.DestroyImmediate(value); // Detached transient object; never destroy the material file.
                    }
                    EditorUtility.SetDirty(material);
                    AssetDatabase.SaveAssetIfDirty(material);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                    var after = Capture(path);
                    if (after.Count != current.preserved.Count || current.preserved.Any(p => !after.TryGetValue(p.Key, out var value) || value != p.Value) ||
                        AssetDatabase.LoadAllAssetsAtPath(path).Any(IsOld) || Hash(path + ".meta") != current.item.metaSha256)
                        throw new InvalidOperationException("Material content, preserved subasset identity or GUID changed: " + path);
                    current.entry.removedSubassets = old.Length;
                    current.entry.afterSha256 = Hash(path);
                    current.entry.status = "REMOVED_ONLY_URP_VERSION_METADATA";
                }
                VerifyGuards(plan);
                // A later material import must not invalidate an earlier verified material.
                foreach (var current in work)
                    if (Hash(current.item.path) != current.entry.afterSha256 || Hash(current.item.path + ".meta") != current.item.metaSha256)
                        throw new InvalidOperationException("Material changed during later import: " + current.item.path);
                receipt.status = receipt.entries.Any(x => x.status == "SKIPPED_REQUIRES_REVIEW") ? "APPLIED_WITH_REMAINDERS_REVIEW_REQUIRED" : "APPLIED_MATERIAL_CONTENT_VERIFIED";
            }
            catch (Exception e)
            {
                receipt.status = "FAILED"; receipt.error = e.ToString();
                var failures = new List<string>();
                foreach (var current in touched.AsEnumerable().Reverse())
                {
                    try
                    {
                        File.Copy(current.backup, current.item.path, true);
                        File.Copy(current.backup + ".meta", current.item.path + ".meta", true);
                        AssetDatabase.ImportAsset(current.item.path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                        if (Hash(current.item.path) != current.item.sha256 || Hash(current.item.path + ".meta") != current.item.metaSha256)
                            throw new IOException("Restore hash mismatch");
                        current.entry.status = "ROLLED_BACK";
                    }
                    catch (Exception restore) { failures.Add(current.item.path + ": " + restore.Message); }
                }
                receipt.rollback = failures.Count == 0 ? "All touched material/meta files restored (or no mutation started)." : "REVIEW_REQUIRED: " + string.Join("; ", failures);
                throw;
            }
            finally
            {
                File.WriteAllText(output + "/cleanup.json", JsonUtility.ToJson(receipt, true) + "\n");
                Debug.Log("URP material metadata cleanup: " + output + " — " + receipt.status);
            }
        }
    }
}
