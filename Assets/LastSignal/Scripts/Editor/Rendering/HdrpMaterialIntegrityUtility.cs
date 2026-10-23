using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace LastSignal.Editor.Rendering
{
    /// <summary>Owner-operated, bounded recovery. Never edits vendor materials, importers, scenes or gameplay.</summary>
    public static class HdrpMaterialIntegrityUtility
    {
        const string Evidence = "Docs/Implementation/VisualRealismMigration/Evidence";
        const string PlanPath = "Docs/Implementation/VisualRealismMigration/MaterialIntegrityRepairPlan.json";
        const string ReceiptPath = Evidence + "/material_integrity_execution.json";
        const string GeneratedRoot = "Assets/LastSignal/Art/HdrpMaterialRecovery/";

        [Serializable] public sealed class Guard { public string path, sha256; }
        [Serializable] public sealed class Recipe
        {
            public string sourcePath, sourceGuid, targetPath, kind, metallicPath, occlusionPath,
                roughnessPath, maskPath, baseMapPath, sourceEvidence;
            public long sourceLocalId;
            public float metallic, smoothness, occlusionStrength;
            public bool emissionEnabled;
            public Vector2 scale, offset;
            public int maxSize;
        }
        [Serializable] public sealed class MaterialCheck
        {
            public string path, guid, maskGuid;
            public float metallicMin, metallicMax, smoothnessMin, smoothnessMax, aoMin, aoMax;
        }
        [Serializable] public sealed class Plan
        {
            public int schemaVersion;
            public string sourceHead;
            public Recipe[] recipes;
            public string[] prefabs;
            public Guard[] guards;
            public MaterialCheck[] validationMaterials;
        }
        [Serializable] public sealed class Change
        {
            public string path, beforeHash, afterHash, backupPath;
        }
        [Serializable] public sealed class Receipt
        {
            public string status, timestampUtc, planHash, snapshot, error;
            public string visualStatus = "VISUAL_NOT_APPROVED";
            public int materialCount, prefabCount, replacedSlots;
            public List<Change> changes = new List<Change>();
            public List<string> checks = new List<string>();
        }

        static Plan LoadPlan()
        {
            var p = JsonUtility.FromJson<Plan>(File.ReadAllText(PlanPath));
            if (p == null || p.schemaVersion != 1 || p.recipes == null || p.recipes.Length != 9 ||
                p.prefabs == null || p.prefabs.Length != 12 || p.guards == null || p.guards.Length == 0)
                throw new InvalidOperationException("Unexpected P0 plan scope/schema.");
            foreach (var r in p.recipes)
            {
                if (!r.targetPath.StartsWith("Assets/LastSignal/", StringComparison.Ordinal) || r.targetPath.Contains("..") ||
                    (!string.IsNullOrEmpty(r.maskPath) && (!r.maskPath.StartsWith(GeneratedRoot, StringComparison.Ordinal) || r.maskPath.Contains(".."))))
                    throw new InvalidOperationException("Unsafe target path: " + r.targetPath);
            }
            foreach (string prefab in p.prefabs)
                if (!prefab.StartsWith("Assets/LastSignal/", StringComparison.Ordinal) || prefab.Contains("..") || !prefab.EndsWith(".prefab"))
                    throw new InvalidOperationException("Unsafe prefab path: " + prefab);
            return p;
        }

        internal static string Hash(string path)
        {
            if (!File.Exists(path)) return "";
            using (var hash = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        static Material Source(Recipe r)
        {
            if (AssetDatabase.AssetPathToGUID(r.sourcePath) != r.sourceGuid)
                throw new InvalidOperationException("Source GUID changed: " + r.sourcePath);
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(r.sourcePath))
                if (obj is Material material && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out long id) &&
                    guid == r.sourceGuid && id == r.sourceLocalId) return material;
            throw new InvalidOperationException("Source material/local ID missing: " + r.sourcePath + ":" + r.sourceLocalId);
        }

        static void CheckGuards(Plan p, Receipt previous)
        {
            foreach (var guard in p.guards)
            {
                string expected = guard.sha256;
                var change = previous?.changes.Find(c => c.path == guard.path);
                if (change != null && !string.IsNullOrEmpty(change.afterHash)) expected = change.afterHash;
                if (Hash(guard.path) != expected)
                    throw new InvalidOperationException("Input changed since approved plan/receipt: " + guard.path);
            }
            if (previous != null)
                foreach (var c in previous.changes)
                    if (Hash(c.path) != c.afterHash)
                        throw new InvalidOperationException("Repair output changed since receipt: " + c.path);
        }

        static Receipt Previous()
        {
            if (!File.Exists(ReceiptPath)) return null;
            var receipt = JsonUtility.FromJson<Receipt>(File.ReadAllText(ReceiptPath));
            if (receipt.planHash != Hash(PlanPath)) throw new InvalidOperationException("Plan differs from last execution receipt.");
            if (receipt.status != "TECHNICAL_CHECKS_PASS")
                throw new InvalidOperationException("Previous repair did not finish. Inspect its receipt and snapshot before retrying.");
            return receipt;
        }

        static void Preflight(Plan p, Receipt previous)
        {
            if (Shader.Find("HDRP/Lit") == null) throw new InvalidOperationException("HDRP/Lit unavailable.");
            CheckGuards(p, previous);
            VerifyPreserved(p);
            foreach (var r in p.recipes)
            {
                var source = Source(r);
                if (r.kind != "medicine" && source.shader.name != "HDRP/Lit")
                    throw new InvalidOperationException("Expected already migrated HDRP/Lit source: " + r.sourcePath);
                if (previous == null && r.kind != "existing" && File.Exists(r.targetPath))
                    throw new InvalidOperationException("Unreceipted derivative already exists: " + r.targetPath);
                if (previous == null && !string.IsNullOrEmpty(r.maskPath) && File.Exists(r.maskPath))
                    throw new InvalidOperationException("Unreceipted packed map already exists: " + r.maskPath);
                foreach (string input in new[] { r.metallicPath, r.occlusionPath, r.roughnessPath })
                {
                    if (string.IsNullOrEmpty(input)) continue;
                    var texture = HdrpMaterialMaskUtility.ReadRaw(input, r.kind == "medicine");
                    Object.DestroyImmediate(texture);
                }
            }
            // Resolve nested prefabs in memory; no Save/Apply calls in dry-run.
            var seen = new HashSet<string>();
            int medicineSlots = 0;
            foreach (string path in p.prefabs)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (!material) throw new InvalidOperationException("Missing renderer material in " + path);
                        foreach (var r in p.recipes.Where(x => x.kind != "existing"))
                            if (Matches(material, r) || AssetDatabase.GetAssetPath(material) == r.targetPath)
                            {
                                seen.Add(r.targetPath);
                                if (r.kind == "medicine") medicineSlots++;
                            }
                    }
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            if (medicineSlots != 15 || p.recipes.Any(r => r.kind != "existing" && !seen.Contains(r.targetPath)))
                throw new InvalidOperationException("Expected derivative references or 15 Medicine slots not found.");
        }

        static bool Matches(Material material, Recipe r) => material &&
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material, out string guid, out long id) &&
            guid == r.sourceGuid && id == r.sourceLocalId;

        [MenuItem("Last Signal/Rendering/HDRP Integrity/1. Dry Run")]
        public static void DryRun()
        {
            var p = LoadPlan();
            Preflight(p, Previous());
            var report = new Receipt { status = "DRY_RUN_READY", timestampUtc = DateTime.UtcNow.ToString("O"),
                planHash = Hash(PlanPath), materialCount = p.recipes.Length, prefabCount = p.prefabs.Length };
            File.WriteAllText(Evidence + "/material_integrity_dry_run.json", JsonUtility.ToJson(report, true));
            Debug.Log("P0 dry-run ready: 9 material recipes, 12 project prefabs. No Unity assets modified.");
        }

        static void Capture(Receipt receipt, string path)
        {
            if (receipt.changes.Any(c => c.path == path)) return;
            var change = new Change { path = path, beforeHash = Hash(path), afterHash = Hash(path) };
            if (File.Exists(path))
            {
                change.backupPath = receipt.snapshot + "/" + path;
                Directory.CreateDirectory(Path.GetDirectoryName(change.backupPath));
                File.Copy(path, change.backupPath, false);
            }
            receipt.changes.Add(change);
        }

        static void SaveReceipt(Receipt receipt)
        {
            foreach (var c in receipt.changes) c.afterHash = Hash(c.path);
            string json = JsonUtility.ToJson(receipt, true);
            File.WriteAllText(ReceiptPath, json);
            File.WriteAllText(receipt.snapshot + "/receipt.json", json);
        }

        [MenuItem("Last Signal/Rendering/HDRP Integrity/2. Repair Scoped Materials and Verify")]
        public static void Repair()
        {
            var p = LoadPlan();
            var previous = Previous();
            Preflight(p, previous);
            if (previous != null)
            {
                Verify(p);
                Debug.Log("P0 idempotency: approved outputs already present; no assets written.");
                return;
            }
            var receipt = new Receipt { status = "IN_PROGRESS", timestampUtc = DateTime.UtcNow.ToString("O"),
                planHash = Hash(PlanPath), materialCount = p.recipes.Length, prefabCount = p.prefabs.Length,
                snapshot = Evidence + "/material-integrity-backups/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") };
            Directory.CreateDirectory(receipt.snapshot);
            File.Copy(PlanPath, receipt.snapshot + "/plan.json");
            // Snapshot all existing affected bytes and metas before the first asset mutation.
            foreach (string path in p.recipes.SelectMany(r => new[] { r.targetPath, r.maskPath }).Concat(p.prefabs))
            {
                if (string.IsNullOrEmpty(path)) continue;
                Capture(receipt, path); Capture(receipt, path + ".meta");
            }
            SaveReceipt(receipt);
            try
            {
                foreach (var r in p.recipes) ApplyRecipe(r);
                var targetGuids = p.recipes.ToDictionary(r => r.targetPath, r => AssetDatabase.AssetPathToGUID(r.targetPath));
                var prefabGuids = p.prefabs.ToDictionary(path => path, path => AssetDatabase.AssetPathToGUID(path));
                foreach (string path in p.prefabs) receipt.replacedSlots += Reassign(p, path);
                // Only save assets touched above. Do not flush unrelated dirty user assets.
                foreach (var r in p.recipes)
                    AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<Material>(r.targetPath));
                foreach (var r in p.recipes)
                    AssetDatabase.ImportAsset(r.targetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                // Owner-operated reimport verifies that project overrides survive the embedded model reimport.
                foreach (var r in p.recipes.Where(x => x.kind == "medicine"))
                    AssetDatabase.ImportAsset(r.sourcePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                foreach (string path in p.prefabs)
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                foreach (var pair in targetGuids.Concat(prefabGuids))
                    if (AssetDatabase.AssetPathToGUID(pair.Key) != pair.Value)
                        throw new InvalidOperationException("GUID changed after reimport: " + pair.Key);
                foreach (var r in p.recipes)
                    AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<Material>(r.targetPath));
                Verify(p);
                foreach (var guard in p.guards.Where(g => !receipt.changes.Any(c => c.path == g.path)))
                    if (Hash(guard.path) != guard.sha256)
                        throw new InvalidOperationException("Protected source bytes changed: " + guard.path);
                receipt.checks.Add("Mask channels, remaps, importer linearity, target GUIDs, prefab slots, Medicine reimport, protected source hashes");
                receipt.status = "TECHNICAL_CHECKS_PASS";
                SaveReceipt(receipt);
                Debug.Log("P0 scoped repair verified. Visual acceptance remains pending. Receipt: " + ReceiptPath);
            }
            catch (Exception ex)
            {
                receipt.status = "FAIL"; receipt.error = ex.ToString(); SaveReceipt(receipt);
                throw;
            }
        }

        static void ApplyRecipe(Recipe r)
        {
            var source = Source(r);
            Material target;
            if (r.kind == "existing") target = source;
            else
            {
                HdrpMaterialMaskUtility.EnsureAssetFolder(Path.GetDirectoryName(r.targetPath).Replace('\\', '/'));
                target = r.kind == "medicine" ? new Material(Shader.Find("HDRP/Lit")) : new Material(source);
                target.name = Path.GetFileNameWithoutExtension(r.targetPath);
                AssetDatabase.CreateAsset(target, r.targetPath);
            }
            Texture mask = string.IsNullOrEmpty(r.maskPath) ? null :
                HdrpMaterialMaskUtility.WritePacked(r.maskPath, r.metallicPath, r.occlusionPath,
                    r.roughnessPath, "", r.maxSize, r.kind == "medicine");
            HdrpMaterialMaskUtility.Apply(target, mask, !string.IsNullOrEmpty(r.metallicPath),
                !string.IsNullOrEmpty(r.metallicPath) || !string.IsNullOrEmpty(r.roughnessPath),
                r.metallic, r.smoothness, r.occlusionStrength, !string.IsNullOrEmpty(r.occlusionPath), r.scale, r.offset);
            if (r.kind == "medicine")
            {
                target.SetColor("_BaseColor", Color.white);
                target.SetTexture("_BaseColorMap", AssetDatabase.LoadAssetAtPath<Texture2D>(r.baseMapPath));
                target.SetTextureScale("_BaseColorMap", r.scale); target.SetTextureOffset("_BaseColorMap", r.offset);
                target.SetFloat("_SurfaceType", 0); target.SetFloat("_AlphaCutoffEnable", 0);
            }
            if (r.kind == "vendor" && !r.emissionEnabled) target.SetColor("_EmissiveColor", Color.black);
            HDMaterial.ValidateMaterial(target);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssetIfDirty(target);
        }

        static int Reassign(Plan p, string path)
        {
            int count = 0;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    var slots = renderer.sharedMaterials;
                    bool changed = false;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        var recipe = p.recipes.FirstOrDefault(r => r.kind != "existing" && Matches(slots[i], r));
                        if (recipe == null) continue;
                        slots[i] = AssetDatabase.LoadAssetAtPath<Material>(recipe.targetPath);
                        if (!slots[i]) throw new InvalidOperationException("Missing derivative: " + recipe.targetPath);
                        changed = true; count++;
                    }
                    if (!changed) continue;
                    renderer.sharedMaterials = slots;
                    if (PrefabUtility.IsPartOfPrefabInstance(renderer))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
                if (count > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                    if (!success) throw new InvalidOperationException("Could not save " + path);
                }
                return count;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void Expect(float actual, float expected, string label)
        {
            if (Mathf.Abs(actual - expected) > .0001f) throw new InvalidOperationException(label + ": expected " + expected + ", got " + actual);
        }

        static void VerifyPreserved(Plan p)
        {
            if (p.validationMaterials == null) throw new InvalidOperationException("Missing preservation checks.");
            foreach (var check in p.validationMaterials)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(check.path);
                if (!mat || mat.shader.name != "HDRP/Lit" || AssetDatabase.AssetPathToGUID(check.path) != check.guid ||
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mat.GetTexture("_MaskMap"))) != check.maskGuid)
                    throw new InvalidOperationException("Preserved material reference changed: " + check.path);
                Expect(mat.GetFloat("_MetallicRemapMin"), check.metallicMin, check.path + " metallic min");
                Expect(mat.GetFloat("_MetallicRemapMax"), check.metallicMax, check.path + " metallic max");
                Expect(mat.GetFloat("_SmoothnessRemapMin"), check.smoothnessMin, check.path + " smoothness min");
                Expect(mat.GetFloat("_SmoothnessRemapMax"), check.smoothnessMax, check.path + " smoothness max");
                Expect(mat.GetFloat("_AORemapMin"), check.aoMin, check.path + " AO min");
                Expect(mat.GetFloat("_AORemapMax"), check.aoMax, check.path + " AO max");
            }
        }

        static void Verify(Plan p)
        {
            VerifyPreserved(p);
            foreach (var r in p.recipes)
            {
                var target = AssetDatabase.LoadAssetAtPath<Material>(r.targetPath);
                if (!target || target.shader.name != "HDRP/Lit") throw new InvalidOperationException("Invalid target: " + r.targetPath);
                bool mapped = !string.IsNullOrEmpty(r.metallicPath);
                Expect(target.GetFloat("_MetallicRemapMin"), mapped ? 0 : r.metallic, r.targetPath + " metallic min");
                Expect(target.GetFloat("_MetallicRemapMax"), mapped ? 1 : r.metallic, r.targetPath + " metallic max");
                Expect(target.GetFloat("_SmoothnessRemapMin"), mapped || !string.IsNullOrEmpty(r.roughnessPath) ? 0 : r.smoothness, r.targetPath + " smoothness min");
                Expect(target.GetFloat("_SmoothnessRemapMax"), r.smoothness, r.targetPath + " smoothness max");
                Expect(target.GetFloat("_Metallic"), mapped ? 1 : r.metallic, r.targetPath + " metallic scalar");
                Expect(target.GetFloat("_AORemapMax"), 1, r.targetPath + " AO max");
                Expect(target.GetFloat("_AORemapMin"), string.IsNullOrEmpty(r.occlusionPath) ? 1 : 1 - r.occlusionStrength, r.targetPath + " AO");
                if (!string.IsNullOrEmpty(r.maskPath))
                {
                    if (AssetDatabase.GetAssetPath(target.GetTexture("_MaskMap")) != r.maskPath || !target.IsKeywordEnabled("_MASKMAP"))
                        throw new InvalidOperationException("Mask reference/keyword lost: " + r.targetPath);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(r.maskPath);
                    if (importer.sRGBTexture || importer.alphaSource != TextureImporterAlphaSource.FromInput ||
                        importer.textureCompression != TextureImporterCompression.Uncompressed)
                        throw new InvalidOperationException("Invalid mask importer: " + r.maskPath);
                    VerifyPixels(r);
                }
                else if (target.GetTexture("_MaskMap") != null)
                    throw new InvalidOperationException("Unexpected scalar-only Mask Map: " + r.targetPath);
                if (r.kind == "medicine")
                {
                    if (AssetDatabase.GetAssetPath(target.GetTexture("_BaseColorMap")) != r.baseMapPath)
                        throw new InvalidOperationException("Medicine base color reference lost.");
                    Expect(target.GetFloat("_SurfaceType"), 0, "Medicine opaque surface");
                    Expect(target.GetFloat("_AlphaCutoffEnable"), 0, "Medicine alpha clipping");
                }
                if (r.kind == "existing" && AssetDatabase.AssetPathToGUID(r.targetPath) != r.sourceGuid)
                    throw new InvalidOperationException("Material GUID changed: " + r.targetPath);
            }
            var seen = new HashSet<string>(); int medicineSlots = 0;
            foreach (string path in p.prefabs)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (!material) throw new InvalidOperationException("Missing material in " + path);
                        foreach (var r in p.recipes.Where(x => x.kind != "existing"))
                        {
                            if (Matches(material, r)) throw new InvalidOperationException("Old source reference remains: " + path);
                            if (AssetDatabase.GetAssetPath(material) != r.targetPath) continue;
                            seen.Add(r.targetPath); if (r.kind == "medicine") medicineSlots++;
                        }
                    }
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            if (medicineSlots != 15 || p.recipes.Any(r => r.kind != "existing" && !seen.Contains(r.targetPath)))
                throw new InvalidOperationException("Derivative reference count changed after reimport.");
        }

        static void VerifyPixels(Recipe r)
        {
            Texture2D mask = null, metal = null, ao = null, rough = null;
            try
            {
                mask = HdrpMaterialMaskUtility.ReadRaw(r.maskPath, false);
                metal = HdrpMaterialMaskUtility.ReadRaw(r.metallicPath, r.kind == "medicine");
                ao = HdrpMaterialMaskUtility.ReadRaw(r.occlusionPath, false);
                rough = HdrpMaterialMaskUtility.ReadRaw(r.roughnessPath, r.kind == "medicine");
                // Independent spatial sample checks include edges and interior at actual output texel centers.
                for (int gy = 0; gy < 17; gy++) for (int gx = 0; gx < 17; gx++)
                {
                    int x = gx * (mask.width - 1) / 16, y = gy * (mask.height - 1) / 16;
                    float u = (x + .5f) / mask.width, v = (y + .5f) / mask.height;
                    var pixel = mask.GetPixel(x, y);
                    var expected = new Color(metal ? metal.GetPixelBilinear(u, v).r : 0,
                        ao ? ao.GetPixelBilinear(u, v).g : 1, 1,
                        rough ? 1 - rough.GetPixelBilinear(u, v).r : metal ? metal.GetPixelBilinear(u, v).a : 1);
                    for (int c = 0; c < 4; c++)
                        if (Mathf.Abs(pixel[c] - expected[c]) > 1.01f / 255)
                            throw new InvalidOperationException("Packed channel mismatch: " + r.maskPath + " channel " + c);
                }
            }
            finally { foreach (var t in new[] { mask, metal, ao, rough }) if (t) Object.DestroyImmediate(t); }
        }

        [MenuItem("Last Signal/Rendering/HDRP Integrity/3. Verify Existing Repair")]
        public static void VerifyExisting()
        {
            var p = LoadPlan(); var previous = Previous();
            if (previous == null) throw new InvalidOperationException("No successful repair receipt.");
            CheckGuards(p, previous); Verify(p);
            Debug.Log("P0 existing repair verified; no assets written. Visual acceptance remains pending.");
        }

        [MenuItem("Last Signal/Rendering/HDRP Integrity/Rollback Existing Files From Latest Snapshot")]
        public static void Rollback()
        {
            var receipt = JsonUtility.FromJson<Receipt>(File.ReadAllText(ReceiptPath));
            if (receipt.status == "ROLLED_BACK") throw new InvalidOperationException("Already rolled back.");
            // Fail closed if the owner edited any output after this receipt. Never delete generated assets/metas.
            foreach (var c in receipt.changes)
            {
                if (Hash(c.path) != c.afterHash) throw new InvalidOperationException("Output changed; manual review required: " + c.path);
                if (!string.IsNullOrEmpty(c.beforeHash) && Hash(c.backupPath) != c.beforeHash)
                    throw new InvalidOperationException("Snapshot missing or corrupt: " + c.backupPath);
            }
            foreach (var c in receipt.changes.Where(c => !string.IsNullOrEmpty(c.beforeHash)))
                File.Copy(c.backupPath, c.path, true);
            foreach (var c in receipt.changes.Where(c => !string.IsNullOrEmpty(c.beforeHash) && !c.path.EndsWith(".meta")))
                AssetDatabase.ImportAsset(c.path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            foreach (var c in receipt.changes.Where(c => !string.IsNullOrEmpty(c.beforeHash)))
                if (Hash(c.path) != c.beforeHash)
                {
                    receipt.status = "ROLLBACK_REIMPORT_CHANGED";
                    receipt.error = "Unity changed restored bytes during import: " + c.path;
                    SaveReceipt(receipt);
                    throw new InvalidOperationException(receipt.error);
                }
            receipt.status = "ROLLED_BACK";
            receipt.checks.Add("Existing files restored; generated derivatives/maps retained unreferenced for recovery.");
            SaveReceipt(receipt);
        }
    }
}
