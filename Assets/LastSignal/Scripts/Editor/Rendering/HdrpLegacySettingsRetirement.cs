using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastSignal.Editor.Rendering
{
    // Reversible retirement of a reviewed, unused seven-asset settings dependency group.
    public static class HdrpLegacySettingsRetirement
    {
        const string PlanPath = "Docs/Implementation/HDRPProductionOptimization/Evidence/retire-urp-settings-plan.json";
        const string PlanHash = "a116c8bb2aec9c2378aa268e83c5c1f2b3c2bd7c912c21cd6f320d4d59b78ceb";
        const string GraphicsPath = "ProjectSettings/GraphicsSettings.asset";
        const string GlobalPath = "Assets/LastSignal/Settings/Rendering/UniversalRenderPipelineGlobalSettings.asset";
        const string Registration = "    UnityEngine.Rendering.Universal.UniversalRenderPipeline: {fileID: 11400000, guid: 18dc0cd2c080841dea60987a38ce93fa, type: 2}";
        [Serializable] sealed class Item { public string path, sha256, metaSha256, guid; }
        [Serializable] sealed class Guard { public string path, sha256; }
        [Serializable] sealed class Plan { public Item[] items; public string graphicsSha256; public Guard[] protectedInputs; }
        [Serializable] sealed class Entry { public string originalPath, archivedPath, sha256, metaSha256; }
        [Serializable] sealed class Receipt
        {
            public string status = "PREFLIGHT", error, rollback, planSha256 = PlanHash, graphicsBeforeSha256, graphicsAfterSha256;
            public List<Entry> archived = new List<Entry>();
            public string packageStatus = "URP_STILL_INSTALLED_VENDOR_DEPENDENCIES_PENDING";
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
            foreach (var guard in plan.protectedInputs)
                Require(Hash(guard.path) == guard.sha256, "Protected input changed: " + guard.path);
        }
        public static void Run()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && LastSignal.Rendering.RenderPipelineAdapter.IsHDRP,
                "Run outside Play mode with HDRP active.");
            Require(Hash(PlanPath) == PlanHash, "Reviewed retirement plan changed.");
            var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(PlanPath));
            Require(plan.items.Length == 7 && plan.items.Select(x => x.path).Distinct().Count() == 7, "Unexpected retirement scope.");
            VerifyGuards(plan);
            Require(Hash(GraphicsPath) == plan.graphicsSha256, "GraphicsSettings changed since review.");
            foreach (var item in plan.items)
                Require(item.path.StartsWith("Assets/LastSignal/Settings/Rendering/", StringComparison.Ordinal) &&
                    !item.path.Contains("..") && Hash(item.path) == item.sha256 && Hash(item.path + ".meta") == item.metaSha256 &&
                    AssetDatabase.AssetPathToGUID(item.path) == item.guid, "Settings input changed: " + item.path);
            // Use Unity's registration API to keep memory and disk consistent. Reflection avoids
            // introducing a Universal assembly reference solely for this one-time cleanup.
            var pipelineType = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetType("UnityEngine.Rendering.Universal.UniversalRenderPipeline")).FirstOrDefault(x => x != null);
            Require(pipelineType != null, "URP must remain installed during settings retirement.");
            var methods = typeof(GraphicsSettings).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var unregister = methods.Single(x => x.Name == "UnregisterRenderPipelineSettings" && x.IsGenericMethodDefinition && x.GetParameters().Length == 0).MakeGenericMethod(pipelineType);
            var register = methods.Single(x => x.Name == "RegisterRenderPipelineSettings" && x.IsGenericMethodDefinition && x.GetParameters().Length == 1).MakeGenericMethod(pipelineType);
            Require(AssetDatabase.GetAssetPath(GraphicsSettings.GetSettingsForRenderPipeline(pipelineType)) == GlobalPath,
                "Unexpected URP registration.");
            string original = File.ReadAllText(GraphicsPath);
            string line = Registration + (original.Contains(Registration + "\r\n") ? "\r\n" : "\n");
            Require(original.Split(new[] { Registration }, StringSplitOptions.None).Length == 2 && original.Contains(line), "Unexpected registration serialization.");
            byte[] beforeGraphics = File.ReadAllBytes(GraphicsPath);
            Require(new UTF8Encoding(false, true).GetBytes(original).SequenceEqual(beforeGraphics), "Unexpected graphics encoding.");
            byte[] expectedGraphics = new UTF8Encoding(false, true).GetBytes(original.Replace(line, ""));
            string output = "Docs/Implementation/HDRPProductionOptimization/Evidence/retired-urp-settings-" +
                DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ") + "-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(output);
            File.Copy(PlanPath, output + "/plan.json");
            File.WriteAllBytes(output + "/GraphicsSettings.before.backup", beforeGraphics);
            foreach (var item in plan.items)
            {
                string backup = output + "/before/" + item.path;
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                File.Copy(item.path, backup); File.Copy(item.path + ".meta", backup + ".meta");
            }
            var receipt = new Receipt { graphicsBeforeSha256 = plan.graphicsSha256 };
            bool touched = false;
            try
            {
                touched = true;
                unregister.Invoke(null, null);
                EditorUtility.SetDirty(GraphicsSettings.GetGraphicsSettings());
                AssetDatabase.SaveAssets();
                Require(File.ReadAllBytes(GraphicsPath).SequenceEqual(expectedGraphics), "Graphics save changed more than the URP registration.");
                foreach (var item in plan.items)
                {
                    Require(Hash(item.path) == item.sha256 && Hash(item.path + ".meta") == item.metaSha256, "Settings changed before archival.");
                    string archived = output + "/retired/" + item.path;
                    Directory.CreateDirectory(Path.GetDirectoryName(archived));
                    File.Move(item.path, archived); File.Move(item.path + ".meta", archived + ".meta");
                    receipt.archived.Add(new Entry { originalPath = item.path, archivedPath = archived, sha256 = item.sha256, metaSha256 = item.metaSha256 });
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                foreach (var entry in receipt.archived)
                    Require(!File.Exists(entry.originalPath) && !File.Exists(entry.originalPath + ".meta") &&
                        Hash(entry.archivedPath) == entry.sha256 && Hash(entry.archivedPath + ".meta") == entry.metaSha256,
                        "Archived settings content mismatch.");
                Require(GraphicsSettings.GetSettingsForRenderPipeline(pipelineType) == null &&
                    File.ReadAllBytes(GraphicsPath).SequenceEqual(expectedGraphics), "URP registration returned or graphics changed.");
                Require(LastSignal.Rendering.RenderPipelineAdapter.IsHDRP, "Active pipeline changed.");
                VerifyGuards(plan);
                receipt.graphicsAfterSha256 = Hash(GraphicsPath);
                receipt.status = "SEVEN_ASSETS_AND_METAS_ARCHIVED_ONLY_URP_REGISTRATION_REMOVED";
            }
            catch (Exception e)
            {
                receipt.status = "FAILED"; receipt.error = e.ToString();
                if (touched)
                {
                    try
                    {
                        foreach (var item in plan.items)
                        {
                            File.Copy(output + "/before/" + item.path, item.path, true);
                            File.Copy(output + "/before/" + item.path + ".meta", item.path + ".meta", true);
                        }
                        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                        var global = AssetDatabase.LoadAssetAtPath<RenderPipelineGlobalSettings>(GlobalPath);
                        Require(global, "Cannot reload original URP global settings.");
                        register.Invoke(null, new object[] { global });
                        EditorUtility.SetDirty(GraphicsSettings.GetGraphicsSettings());
                        AssetDatabase.SaveAssets();
                        File.WriteAllBytes(GraphicsPath, beforeGraphics);
                        foreach (var item in plan.items)
                            Require(Hash(item.path) == item.sha256 && Hash(item.path + ".meta") == item.metaSha256, "Retirement rollback hash mismatch.");
                        VerifyGuards(plan);
                        Require(Hash(GraphicsPath) == plan.graphicsSha256, "Graphics rollback mismatch.");
                        receipt.rollback = "ALL_SEVEN_ORIGINALS_METAS_GRAPHICS_AND_GUARDS_VERIFIED";
                    }
                    catch (Exception rollback) { receipt.rollback = "FAILED: " + rollback; }
                }
                throw;
            }
            finally
            {
                File.WriteAllText(output + "/retirement.json", JsonUtility.ToJson(receipt, true) + "\n");
                Debug.Log("Legacy settings retirement receipt: " + output + "/retirement.json");
            }
        }
    }
}
