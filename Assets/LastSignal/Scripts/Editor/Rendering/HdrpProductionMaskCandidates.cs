using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSignal.Editor.Rendering
{
    /// <summary>Owner-operated staging only. Never assigns materials or changes a vendor importer.</summary>
    public static class HdrpProductionMaskCandidates
    {
        const string PlanPath = "Docs/Implementation/HDRPProductionOptimization/MaterialRepairCandidates.json";
        const string Evidence = "Docs/Implementation/HDRPProductionOptimization/Evidence";
        const string Root = "Assets/LastSignal/Art/HdrpMaterialRecovery/ProductionCandidates";
        [Serializable] public sealed class Candidate
        {
            public string path, guid, status, sourceTexture, sourceTextureGuid;
            public long localId;
            public float sourceSmoothness;
        }
        [Serializable] public sealed class Plan
        {
            public int schemaVersion;
            public string auditSha256;
            public Candidate[] candidates;
            public HdrpMaterialIntegrityUtility.Guard[] guards;
        }
        [Serializable] public sealed class Output
        {
            public string source, mask, sha256;
            public int width, height;
            public string[] materials;
        }
        [Serializable] public sealed class Receipt
        {
            public string status = "IN_PROGRESS", planHash, generation, error;
            public string visualStatus = "NOT_APPROVED";
            public string policy = "Raw source R to metallic, G=1, B=1, source A to smoothness; no material assignment. RGBA8 output.";
            public List<Output> outputs = new List<Output>();
            public List<string> checks = new List<string>();
        }

        [Serializable] public sealed class AssignmentReceipt
        {
            public string status, snapshot, planHash, error;
            public string visualStatus = "NOT_APPROVED";
            public List<HdrpMaterialIntegrityUtility.Change> changes = new List<HdrpMaterialIntegrityUtility.Change>();
        }

        [MenuItem("Last Signal/Rendering/HDRP Production/Apply Verified Mask Candidates")]
        public static void ApplyVerified()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stop play/compilation first.");
            const string candidateReceiptPath = Evidence + "/mask-candidates-20261021T165556952-0aaf16f9.json";
            var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(PlanPath));
            var candidates = JsonUtility.FromJson<Receipt>(File.ReadAllText(candidateReceiptPath));
            if (plan == null || candidates == null || candidates.status != "CANDIDATES_VERIFIED_NOT_APPLIED" ||
                candidates.planHash != HdrpMaterialIntegrityUtility.Hash(PlanPath) || candidates.outputs.Count != 40)
                throw new InvalidOperationException("Unverified candidate receipt.");
            Guards(plan);
            var selected = plan.candidates.Where(c => c.status == "CHANNEL_PROVENANCE_RESOLVED_NOT_APPLIED").ToArray();
            if (selected.Length != 43) throw new InvalidOperationException("Unexpected material scope.");
            var materialPaths = new HashSet<string>(selected.Select(c => c.path));
            foreach (var output in candidates.outputs)
            {
                if (!output.mask.StartsWith(candidates.generation + "/", StringComparison.Ordinal) || output.mask.Contains("..") ||
                    HdrpMaterialIntegrityUtility.Hash(output.mask) != output.sha256)
                    throw new InvalidOperationException("Candidate changed: " + output.mask);
                var importer = AssetImporter.GetAtPath(output.mask) as TextureImporter;
                var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(output.mask);
                if (!importer || importer.sRGBTexture || importer.textureType != TextureImporterType.Default || !mask ||
                    mask.width != output.width || mask.height != output.height)
                    throw new InvalidOperationException("Candidate import mismatch: " + output.mask);
            }
            foreach (var c in selected)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(c.path);
                if (!material || material.shader.name != "HDRP/Lit" || AssetDatabase.AssetPathToGUID(c.path) != c.guid ||
                    material.GetTexture("_DetailMap") || AssetDatabase.GetAssetPath(material.GetTexture("_MaskMap")) != c.sourceTexture)
                    throw new InvalidOperationException("Material contract changed: " + c.path);
                if (candidates.outputs.Count(o => o.source == c.sourceTexture && o.materials.Contains(c.path)) != 1)
                    throw new InvalidOperationException("Candidate mapping ambiguous: " + c.path);
            }
            string stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" + Guid.NewGuid().ToString("N");
            string snapshot = Path.GetFullPath(Path.Combine(Application.dataPath, "../../LastSignal-Recovery/MaskAssignment-" + stamp));
            Directory.CreateDirectory(snapshot);
            var receipt = new AssignmentReceipt { status = "IN_PROGRESS", snapshot = snapshot, planHash = HdrpMaterialIntegrityUtility.Hash(PlanPath) };
            string receiptPath = Evidence + "/mask-assignment-" + stamp + ".json";
            foreach (var c in selected)
            foreach (string path in new[] { c.path, c.path + ".meta" })
            {
                string backup = Path.Combine(snapshot, path);
                Directory.CreateDirectory(Path.GetDirectoryName(backup)); File.Copy(path, backup, false);
                string hash = HdrpMaterialIntegrityUtility.Hash(path);
                if (HdrpMaterialIntegrityUtility.Hash(backup) != hash) throw new IOException("Snapshot mismatch: " + path);
                receipt.changes.Add(new HdrpMaterialIntegrityUtility.Change { path = path, beforeHash = hash, backupPath = backup });
            }
            File.WriteAllText(receiptPath, JsonUtility.ToJson(receipt, true));
            try
            {
                foreach (var c in selected)
                {
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(c.path);
                    var output = candidates.outputs.Single(o => o.source == c.sourceTexture);
                    var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(output.mask);
                    var scale = mat.GetTextureScale("_MaskMap"); var offset = mat.GetTextureOffset("_MaskMap");
                    HdrpMaterialMaskUtility.Apply(mat, mask, true, true, 1f, c.sourceSmoothness, 1f, false, scale, offset);
                    UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(mat);
                    EditorUtility.SetDirty(mat); AssetDatabase.SaveAssetIfDirty(mat);
                    if (mat.GetTexture("_MaskMap") != mask || mat.GetFloat("_MetallicRemapMax") != 1f ||
                        mat.GetFloat("_SmoothnessRemapMax") != c.sourceSmoothness || mat.GetFloat("_AORemapMin") != 1f ||
                        !mat.IsKeywordEnabled("_MASKMAP") || AssetDatabase.AssetPathToGUID(c.path) != c.guid)
                        throw new InvalidOperationException("Assignment verification failed: " + c.path);
                }
                foreach (var guard in plan.guards)
                    if (!materialPaths.Contains(guard.path) && HdrpMaterialIntegrityUtility.Hash(guard.path) != guard.sha256)
                        throw new InvalidOperationException("Protected source/meta changed: " + guard.path);
                receipt.status = "TECHNICAL_CHECKS_PASS";
            }
            catch (Exception e)
            {
                receipt.status = "FAILED_RESTORED_FROM_SNAPSHOT"; receipt.error = e.ToString();
                foreach (var change in receipt.changes) File.Copy(change.backupPath, change.path, true);
                foreach (var c in selected) AssetDatabase.ImportAsset(c.path, ImportAssetOptions.ForceSynchronousImport);
                throw;
            }
            finally
            {
                foreach (var change in receipt.changes) change.afterHash = HdrpMaterialIntegrityUtility.Hash(change.path);
                File.WriteAllText(receiptPath, JsonUtility.ToJson(receipt, true));
            }
            Debug.Log("43 material masks assigned, source/meta guards preserved: " + receiptPath);
        }

        static void Guards(Plan plan)
        {
            foreach (var guard in plan.guards)
                if (HdrpMaterialIntegrityUtility.Hash(guard.path) != guard.sha256)
                    throw new InvalidOperationException("Guard changed; review before rebuilding plan: " + guard.path);
        }

        [MenuItem("Last Signal/Rendering/HDRP Production/Prepare Mask Candidates (No Material Assignment)")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stop Play Mode and compilation first.");
            var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(PlanPath));
            if (plan == null || plan.schemaVersion != 1 || plan.candidates == null || plan.guards == null || plan.guards.Length == 0)
                throw new InvalidOperationException("Invalid candidate plan.");
            if (HdrpMaterialIntegrityUtility.Hash(Evidence + "/production_material_coverage.json") != plan.auditSha256)
                throw new InvalidOperationException("Owner audit changed; review current evidence before authoring.");
            Guards(plan);
            var selected = plan.candidates.Where(c => c.status == "CHANNEL_PROVENANCE_RESOLVED_NOT_APPLIED").ToArray();
            if (selected.Length != 43 || selected.Select(c => c.sourceTexture).Distinct().Count() != 40)
                throw new InvalidOperationException("Unexpected scope: expected 43 materials and 40 shared texture inputs.");
            // Validate the complete scope before creating any asset.
            foreach (var c in selected)
            {
                if (!c.path.StartsWith("Assets/LastSignal/", StringComparison.Ordinal) || c.path.Contains("..") ||
                    !c.sourceTexture.StartsWith("Assets/", StringComparison.Ordinal) || c.sourceTexture.Contains(".."))
                    throw new InvalidOperationException("Unsafe plan path.");
                var material = AssetDatabase.LoadAssetAtPath<Material>(c.path);
                if (!material || material.shader.name != "HDRP/Lit" || AssetDatabase.AssetPathToGUID(c.path) != c.guid ||
                    AssetDatabase.AssetPathToGUID(c.sourceTexture) != c.sourceTextureGuid ||
                    AssetDatabase.GetAssetPath(material.GetTexture("_MaskMap")) != c.sourceTexture)
                    throw new InvalidOperationException("Material/source identity changed: " + c.path);
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material, out string guid, out long localId) || localId != c.localId)
                    throw new InvalidOperationException("Material local ID changed: " + c.path);
                if (material.GetTexture("_DetailMap") || material.GetFloat("_UVBase") != 0 ||
                    float.IsNaN(c.sourceSmoothness) || c.sourceSmoothness < 0 || c.sourceSmoothness > 1)
                    throw new InvalidOperationException("Unreviewed detail/UV/smoothness contract: " + c.path);
                var importer = AssetImporter.GetAtPath(c.sourceTexture) as TextureImporter;
                if (!importer || importer.textureType != TextureImporterType.Default)
                    throw new InvalidOperationException("Expected ordinary data texture: " + c.sourceTexture);
                importer.GetSourceTextureWidthAndHeight(out int w, out int h);
                if (w <= 0 || h <= 0 || w > 8192 || h > 8192)
                    throw new InvalidOperationException("Source resolution requires separate review: " + c.sourceTexture);
            }
            string stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string generation = Root + "/G" + stamp;
            var receipt = new Receipt { generation = generation, planHash = HdrpMaterialIntegrityUtility.Hash(PlanPath) };
            Directory.CreateDirectory(Evidence);
            string reportPath = Evidence + "/mask-candidates-" + stamp + ".json";
            try
            {
                HdrpMaterialMaskUtility.EnsureAssetFolder(generation);
                foreach (var group in selected.GroupBy(c => c.sourceTexture))
                {
                    var c = group.First();
                    string staging = generation + "/decode_" + c.sourceTextureGuid + Path.GetExtension(c.sourceTexture);
                    string output = generation + "/mask_" + c.sourceTextureGuid + ".png";
                    if (!AssetDatabase.CopyAsset(c.sourceTexture, staging)) throw new IOException("Cannot stage " + c.sourceTexture);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(staging);
                    // Only this new project-owned copy is reimported. No vendor setting is changed.
                    importer.sRGBTexture = false;
                    importer.isReadable = true;
                    importer.alphaSource = TextureImporterAlphaSource.FromInput;
                    importer.alphaIsTransparency = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.crunchedCompression = false;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.maxTextureSize = 8192;
                    importer.mipmapEnabled = false;
                    importer.ClearPlatformTextureSettings("Standalone");
                    importer.SaveAndReimport();
                    var source = AssetDatabase.LoadAssetAtPath<Texture2D>(staging);
                    importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                    if (!source || source.width != width || source.height != height)
                        throw new InvalidOperationException("Staging changed source resolution: " + staging);
                    var values = source.GetPixels32();
                    for (int i = 0; i < values.Length; i++) values[i] = new Color32(values[i].r, 255, 255, values[i].a);
                    var packed = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
                    try
                    {
                        packed.SetPixels32(values); packed.Apply(false, false);
                        File.WriteAllBytes(output, ImageConversion.EncodeToPNG(packed));
                    }
                    finally { Object.DestroyImmediate(packed); }
                    AssetDatabase.ImportAsset(output, ImportAssetOptions.ForceSynchronousImport);
                    var maskImporter = (TextureImporter)AssetImporter.GetAtPath(output);
                    maskImporter.sRGBTexture = false;
                    maskImporter.alphaSource = TextureImporterAlphaSource.FromInput;
                    maskImporter.alphaIsTransparency = false;
                    maskImporter.textureCompression = TextureImporterCompression.Uncompressed;
                    maskImporter.maxTextureSize = 8192;
                    maskImporter.npotScale = TextureImporterNPOTScale.None;
                    maskImporter.mipmapEnabled = true;
                    maskImporter.isReadable = false;
                    maskImporter.wrapModeU = importer.wrapModeU;
                    maskImporter.wrapModeV = importer.wrapModeV;
                    maskImporter.wrapModeW = importer.wrapModeW;
                    maskImporter.filterMode = importer.filterMode;
                    maskImporter.anisoLevel = importer.anisoLevel;
                    maskImporter.SaveAndReimport();
                    var decoded = HdrpMaterialMaskUtility.ReadRaw(output, false);
                    try
                    {
                        if (decoded.width != width || decoded.height != height)
                            throw new InvalidOperationException("PNG dimensions changed: " + output);
                        var actual = decoded.GetPixels32();
                        for (int i = 0; i < values.Length; i++)
                            if (!actual[i].Equals(values[i])) throw new InvalidOperationException("Packed channel mismatch: " + output + " pixel " + i);
                    }
                    finally { Object.DestroyImmediate(decoded); }
                    receipt.outputs.Add(new Output { source = c.sourceTexture, mask = output,
                        width = width, height = height, sha256 = HdrpMaterialIntegrityUtility.Hash(output),
                        materials = group.Select(x => x.path).ToArray() });
                    // Delete only the newly-created staging asset after its output passes exact pixel verification.
                    if (!AssetDatabase.DeleteAsset(staging)) throw new IOException("Cannot remove owned decode staging: " + staging);
                    File.WriteAllText(reportPath, JsonUtility.ToJson(receipt, true));
                }
                Guards(plan);
                receipt.checks.Add("All 170 guarded material/source/meta files unchanged.");
                receipt.checks.Add("All packed PNG pixels match source R/A with constant G/B; full source dimensions preserved.");
                receipt.checks.Add("No material, prefab, scene or vendor importer assigned or saved.");
                receipt.status = "CANDIDATES_VERIFIED_NOT_APPLIED";
            }
            catch (Exception e) { receipt.error = e.ToString(); receipt.status = "FAILED_REVIEW_PARTIAL_GENERATION"; throw; }
            finally { File.WriteAllText(reportPath, JsonUtility.ToJson(receipt, true)); }
            Debug.Log("HDRP mask candidates ready for review; no material assignment. " + reportPath);
        }
    }
}
