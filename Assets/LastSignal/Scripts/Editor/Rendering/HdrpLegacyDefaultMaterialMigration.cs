using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace LastSignal.Editor.Rendering
{
    // Exact material-reference edits; never opens/resaves scenes or touches tree physics.
    public static class HdrpLegacyDefaultMaterialMigration
    {
        const string PlanPath = "Docs/Implementation/HDRPProductionOptimization/Evidence/legacy-default-material-plan.json";
        const string PlanHash = "b8c7624b8c2c712bf74e217854ec9c6b60a85532e3946f75a5b6b989e18f14ac";
        const string OldGuid = "31321ba15b8f8eb4c954353edc038b1d";
        const string OldReference = "{fileID: 2100000, guid: " + OldGuid + ", type: 2}";
        const string MaterialPath = "Assets/LastSignal/Settings/Rendering/HDRP/LegacyDefaultLit.mat";
        [Serializable] sealed class Guard { public string path, sha256; }
        [Serializable] sealed class Item { public string path, sha256, metaSha256; public string[] rendererIds; }
        [Serializable] sealed class Plan { public string sourcePath, sourceSha256; public Item[] items; public Guard[] protectedInputs; }
        [Serializable] sealed class Entry { public string path, beforeSha256, afterSha256, metaSha256; public int slots; }
        [Serializable] sealed class Receipt
        {
            public string status = "PREFLIGHT", error, rollback, planSha256 = PlanHash;
            public string materialPath = MaterialPath, materialGuid, materialSha256, materialMetaSha256;
            public long materialLocalId;
            public string visualStatus = "OWNER_REVIEW_REQUIRED";
            public List<Entry> entries = new List<Entry>();
        }
        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        static string Hash(string path)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
        static void VerifyGuards(Plan plan)
        {
            Require(Hash(plan.sourcePath) == plan.sourceSha256, "Package source material changed.");
            foreach (var guard in plan.protectedInputs)
                Require(Hash(guard.path) == guard.sha256, "Protected input changed: " + guard.path);
            foreach (var item in plan.items)
                Require(Hash(item.path + ".meta") == item.metaSha256, "Scene meta changed: " + item.path);
        }
        static string ReplaceReferences(string original, Item item, string replacement)
        {
            var expected = new HashSet<string>(item.rendererIds);
            Require(expected.Count == item.rendererIds.Length, "Duplicate renderer plan ID.");
            int count = 0;
            foreach (Match document in Regex.Matches(original, @"^--- !u!(\d+) &(-?\d+)[^\r\n]*\r?\n.*?(?=^--- !u!|\z)", RegexOptions.Multiline | RegexOptions.Singleline))
            {
                if (!document.Value.Contains(OldGuid)) continue;
                Require(document.Groups[1].Value == "23" && expected.Remove(document.Groups[2].Value),
                    "Unexpected reference owner in " + item.path);
                var slots = Regex.Match(document.Value, @"(?m)^  m_Materials:\r?\n(?:  - [^\r\n]*\r?\n)+").Value;
                Require(Regex.Matches(document.Value, OldGuid).Count == 1 && slots.Contains(OldReference),
                    "Reference is not the reviewed MeshRenderer material slot.");
                count++;
            }
            Require(expected.Count == 0 && count == item.rendererIds.Length && Regex.Matches(original, OldGuid).Count == count,
                "Scene reference inventory changed: " + item.path);
            // Every remaining byte, including all physics/gameplay/transform records, stays exact.
            return original.Replace(OldReference, replacement);
        }
        static void VerifyMaterial(Material material)
        {
            Require(material && material.shader && material.shader.name == "HDRP/Lit" &&
                material.shader.isSupported && !ShaderUtil.ShaderHasError(material.shader), "Native material shader is unavailable.");
            Require(material.GetColor("_BaseColor") == new Color(.5f, .5f, .5f, 1f) &&
                material.GetFloat("_Metallic") == 0f && material.GetFloat("_Smoothness") == .5f &&
                material.GetFloat("_SurfaceType") == 0f && material.GetFloat("_AlphaCutoffEnable") == 0f &&
                material.GetColor("_EmissiveColor") == Color.black, "Native material tuning changed.");
            Require(!material.enableInstancing && !material.doubleSidedGI, "Material flags changed.");
        }
        public static void Run()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && LastSignal.Rendering.RenderPipelineAdapter.IsHDRP,
                "Run outside Play mode with HDRP active.");
            Require(Hash(PlanPath) == PlanHash, "Reviewed material plan changed.");
            var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(PlanPath));
            Require(plan.items.Length == 5 && plan.items.Select(x => x.path).Distinct().Count() == 5 &&
                plan.items.Sum(x => x.rendererIds.Length) == 23, "Unexpected scene plan scope.");
            VerifyGuards(plan);
            Require(!File.Exists(MaterialPath) && !File.Exists(MaterialPath + ".meta"), "Destination material already exists; review previous receipt.");
            var originals = new Dictionary<string, byte[]>();
            foreach (var item in plan.items)
            {
                Require(item.path.StartsWith("Assets/LastSignal/", StringComparison.Ordinal) && item.path.EndsWith(".unity", StringComparison.Ordinal) &&
                    !item.path.Contains("..") && Hash(item.path) == item.sha256, "Scene changed or invalid path: " + item.path);
                originals.Add(item.path, File.ReadAllBytes(item.path));
                ReplaceReferences(File.ReadAllText(item.path), item, OldReference); // Complete preflight before mutation.
            }
            string output = "Docs/Implementation/HDRPProductionOptimization/Evidence/legacy-default-material-" +
                DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ") + "-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(output);
            File.Copy(PlanPath, output + "/plan.json");
            foreach (var item in plan.items)
            {
                string backup = output + "/before/" + item.path + ".backup";
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                File.Copy(item.path, backup);
                File.Copy(item.path + ".meta", backup + ".meta");
            }
            File.Copy(plan.sourcePath, output + "/source-material.backup");
            var receipt = new Receipt();
            var touched = new List<Item>();
            Material material = null;
            bool created = false;
            try
            {
                var shader = Shader.Find("HDRP/Lit");
                Require(shader && shader.isSupported && !ShaderUtil.ShaderHasError(shader), "HDRP/Lit not ready.");
                material = new Material(shader) { name = "LegacyDefaultLit", enableInstancing = false, doubleSidedGI = false };
                material.SetColor("_BaseColor", new Color(.5f, .5f, .5f, 1f));
                material.SetFloat("_Metallic", 0f);
                material.SetFloat("_Smoothness", .5f);
                material.SetFloat("_SurfaceType", 0f);
                material.SetFloat("_AlphaCutoffEnable", 0f);
                material.SetColor("_EmissiveColor", Color.black);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                HDMaterial.ValidateMaterial(material);
                VerifyMaterial(material);
                VerifyGuards(plan);
                foreach (var item in plan.items) Require(Hash(item.path) == item.sha256, "Scene changed during preflight.");
                created = true;
                AssetDatabase.CreateAsset(material, MaterialPath);
                AssetDatabase.SaveAssetIfDirty(material);
                Require(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material, out string guid, out long id) && id != 0,
                    "Cannot resolve native material identity.");
                receipt.materialGuid = guid; receipt.materialLocalId = id;
                string replacement = "{fileID: " + id + ", guid: " + guid + ", type: 2}";
                var afterBytes = new Dictionary<string, byte[]>();
                foreach (var item in plan.items)
                {
                    string before = File.ReadAllText(item.path);
                    Require(Hash(item.path) == item.sha256, "Scene changed before assignment: " + item.path);
                    byte[] changed = new UTF8Encoding(false, true).GetBytes(ReplaceReferences(before, item, replacement));
                    // Refuse non-UTF8 or BOM-normalizing changes rather than rewriting unrelated bytes.
                    Require(new UTF8Encoding(false, true).GetBytes(before).SequenceEqual(originals[item.path]), "Scene encoding would change.");
                    afterBytes.Add(item.path, changed);
                }
                foreach (var item in plan.items)
                {
                    touched.Add(item);
                    File.WriteAllBytes(item.path, afterBytes[item.path]);
                    AssetDatabase.ImportAsset(item.path, ImportAssetOptions.ForceUpdate);
                    receipt.entries.Add(new Entry { path = item.path, beforeSha256 = item.sha256,
                        afterSha256 = Hash(item.path), metaSha256 = item.metaSha256, slots = item.rendererIds.Length });
                }
                // Recheck the complete batch after imports, not only each immediately after its write.
                foreach (var item in plan.items)
                    Require(File.ReadAllBytes(item.path).SequenceEqual(afterBytes[item.path]), "Import changed unrelated scene data: " + item.path);
                AssetDatabase.ImportAsset(MaterialPath, ImportAssetOptions.ForceUpdate);
                var reloaded = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                VerifyMaterial(reloaded);
                Require(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(reloaded, out string savedGuid, out long savedId) &&
                    savedGuid == guid && savedId == id, "Saved material identity changed.");
                VerifyGuards(plan);
                receipt.materialSha256 = Hash(MaterialPath);
                receipt.materialMetaSha256 = Hash(MaterialPath + ".meta");
                receipt.status = "APPLIED_23_SLOTS_EXACT_OTHER_BYTES_VERIFIED_VISUAL_PENDING";
            }
            catch (Exception e)
            {
                receipt.status = "FAILED"; receipt.error = e.ToString();
                try
                {
                    foreach (var item in touched)
                    {
                        File.WriteAllBytes(item.path, originals[item.path]);
                        AssetDatabase.ImportAsset(item.path, ImportAssetOptions.ForceUpdate);
                    }
                    foreach (var item in plan.items) Require(Hash(item.path) == item.sha256, "Scene rollback mismatch: " + item.path);
                    VerifyGuards(plan);
                    // Only a newly created material can be removed, after all original scenes are verified.
                    if (created && File.Exists(MaterialPath))
                        Require(AssetDatabase.DeleteAsset(MaterialPath), "New material rollback failed.");
                    receipt.rollback = "ORIGINAL_SCENES_METAS_AND_GUARDS_VERIFIED";
                }
                catch (Exception rollback) { receipt.rollback = "FAILED: " + rollback; }
                throw;
            }
            finally
            {
                if (material && !AssetDatabase.Contains(material)) UnityEngine.Object.DestroyImmediate(material);
                File.WriteAllText(output + "/migration.json", JsonUtility.ToJson(receipt, true) + "\n");
                Debug.Log("Legacy default material receipt: " + output + "/migration.json");
            }
        }
    }
}
