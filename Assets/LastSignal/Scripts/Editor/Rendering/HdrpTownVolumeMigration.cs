using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace LastSignal.Editor.Rendering
{
    // Owner-operated, pinned migration of the four reviewed TownVolume overrides.
    // Uses no Universal assembly reference; never regenerates the atmosphere or scenes.
    public static class HdrpTownVolumeMigration
    {
        const string PlanPath = "Docs/Implementation/HDRPProductionOptimization/Evidence/town-volume-plan.json";
        const string PlanHash = "d516ec42ff1c6d270c4301ac7cc0ed18db7ff13e627850bf5e6c6ccbf22b3932";
        const string ProfilePath = "Assets/LastSignal/Art/FirstTownAtmosphere/TownVolume.asset";
        const string Universal = "UnityEngine.Rendering.Universal.";
        [Serializable] sealed class Guard { public string path, sha256; }
        [Serializable] sealed class Plan { public string path, sha256, metaSha256; public Guard[] protectedInputs; }
        [Serializable] sealed class Receipt
        {
            public string status = "PREFLIGHT", error, rollback, path = ProfilePath, planSha256 = PlanHash;
            public string beforeSha256, afterSha256, metaSha256;
            public int replacedComponents, verifiedProtectedInputs;
            public string visualStatus = "OWNER_REVIEW_REQUIRED";
            public string bloomDisposition = "Common tuning retained; HDRP sampling defaults replace URP skipIterations, clamp, highQualityFiltering, filter, downscale, maxIterations. Original retained in before.asset.backup.";
        }
        static readonly Type[] NativeTypes = { typeof(Tonemapping), typeof(ColorAdjustments), typeof(Bloom), typeof(Vignette) };
        static readonly string[][] Fields = {
            new[] { "mode", "neutralHDRRangeReductionMode", "acesPreset", "hueShiftAmount", "detectPaperWhite", "paperWhite", "detectBrightnessLimits", "minNits", "maxNits" },
            new[] { "postExposure", "contrast", "colorFilter", "hueShift", "saturation" },
            new[] { "threshold", "intensity", "scatter", "tint", "dirtTexture", "dirtIntensity" },
            new[] { "color", "center", "intensity", "smoothness", "rounded" }
        };
        static string Hash(string path)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        static void VerifyGuards(Plan plan)
        {
            foreach (var guard in plan.protectedInputs)
                Require(Hash(guard.path) == guard.sha256, "Protected input changed: " + guard.path);
            Require(Hash(ProfilePath + ".meta") == plan.metaSha256, "Volume meta changed.");
        }
        static VolumeParameter Parameter(VolumeComponent component, string field)
        {
            return component.GetType().GetField(field)?.GetValue(component) as VolumeParameter
                ?? throw new InvalidOperationException("Missing volume parameter: " + component.GetType().Name + "." + field);
        }
        static void CopyParameter(VolumeComponent source, VolumeComponent target, string field)
        {
            var from = Parameter(source, field);
            var to = Parameter(target, field);
            var sourceValue = from.GetType().GetProperty("value");
            var targetValue = to.GetType().GetProperty("value");
            Require(sourceValue != null && targetValue != null && targetValue.CanWrite, "Unsupported parameter: " + field);
            object value = sourceValue.GetValue(from);
            // Pipeline-specific enum types are mapped by name, never by numeric coincidence.
            if (targetValue.PropertyType.IsEnum)
            {
                Require(value != null && Enum.IsDefined(targetValue.PropertyType, value.ToString()), "Unmapped enum: " + field);
                value = Enum.Parse(targetValue.PropertyType, value.ToString());
            }
            else Require(value == null || targetValue.PropertyType.IsInstanceOfType(value), "Incompatible value: " + field);
            targetValue.SetValue(to, value);
            to.overrideState = from.overrideState;
            Require(Equals(targetValue.GetValue(to), value), "HDRP clamped or changed parameter: " + field);
        }
        static string RootWithoutReferences(string yaml)
        {
            var match = Regex.Match(yaml, @"(?m)^--- !u!114 &11400000\r?\n[\s\S]*?(?=^--- !u!|\z)");
            Require(match.Success, "VolumeProfile main identity changed.");
            return Regex.Replace(match.Value, @"(?m)^  - \{fileID: -?\d+\}\r?\n", "");
        }
        static void VerifyNative(VolumeProfile profile, string guid, string originalRoot, string[] expected)
        {
            Require(profile && profile.components.Count == 4 && AssetDatabase.LoadAllAssetsAtPath(ProfilePath).Length == 5,
                "Unexpected volume object count.");
            Require(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(profile, out string actualGuid, out long id) &&
                actualGuid == guid && id == 11400000, "VolumeProfile persistent identity changed.");
            Require(RootWithoutReferences(File.ReadAllText(ProfilePath)) == originalRoot, "Unrelated VolumeProfile fields changed.");
            var disk = File.ReadAllText(ProfilePath);
            Require(Regex.Matches(disk, @"(?m)^--- !u!").Count == 5, "Unexpected serialized document count.");
            for (int i = 0; i < NativeTypes.Length; i++)
            {
                var component = profile.components.SingleOrDefault(x => x && x.GetType() == NativeTypes[i]);
                Require(component && AssetDatabase.GetAssetPath(component) == ProfilePath, "Missing persistent native override.");
                Require(JsonUtility.ToJson(component) == expected[i], "Saved override values changed: " + NativeTypes[i].Name);
                string scriptGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(component)));
                Require(!string.IsNullOrEmpty(scriptGuid) && disk.Contains("guid: " + scriptGuid), "Native script missing from disk.");
            }
            Require(!disk.Contains("UnityEngine.Rendering.Universal"), "Universal override remains.");
        }
        public static void Run()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && LastSignal.Rendering.RenderPipelineAdapter.IsHDRP,
                "Run outside Play mode with HDRP active.");
            Require(Hash(PlanPath) == PlanHash, "Reviewed volume plan changed.");
            var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(PlanPath));
            Require(plan.path == ProfilePath && Hash(ProfilePath) == plan.sha256, "TownVolume changed since review.");
            VerifyGuards(plan);
            string output = "Docs/Implementation/HDRPProductionOptimization/Evidence/town-volume-" +
                DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ") + "-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(output);
            File.Copy(PlanPath, output + "/plan.json");
            File.Copy(ProfilePath, output + "/before.asset.backup");
            File.Copy(ProfilePath + ".meta", output + "/before.meta.backup");
            var receipt = new Receipt { beforeSha256 = plan.sha256, metaSha256 = plan.metaSha256 };
            var created = new List<VolumeComponent>();
            bool touched = false;
            try
            {
                var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
                Require(profile && !EditorUtility.IsDirty(profile) && profile.components.Count == 4 &&
                    AssetDatabase.LoadAllAssetsAtPath(ProfilePath).Length == 5, "Unexpected or dirty volume contents.");
                Require(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(profile, out string guid, out long id) && id == 11400000,
                    "Unexpected VolumeProfile identity.");
                string originalRoot = RootWithoutReferences(File.ReadAllText(ProfilePath));
                var old = profile.components.ToArray();
                for (int i = 0; i < NativeTypes.Length; i++)
                {
                    var source = old.SingleOrDefault(x => x && x.GetType().FullName == Universal + NativeTypes[i].Name);
                    Require(source && !EditorUtility.IsDirty(source) && AssetDatabase.GetAssetPath(source) == ProfilePath,
                        "Unexpected source override: " + NativeTypes[i].Name);
                    var target = (VolumeComponent)ScriptableObject.CreateInstance(NativeTypes[i]);
                    created.Add(target);
                    target.name = NativeTypes[i].Name;
                    target.hideFlags = source.hideFlags;
                    target.active = source.active;
                    foreach (string field in Fields[i]) CopyParameter(source, target, field);
                }
                // Mapping must succeed for all four before any persistent object is changed.
                VerifyGuards(plan);
                Require(Hash(ProfilePath) == plan.sha256, "Volume changed during preflight.");
                touched = true;
                profile.components.Clear();
                foreach (var source in old)
                {
                    AssetDatabase.RemoveObjectFromAsset(source);
                    Require(!AssetDatabase.Contains(source), "Old override was not detached.");
                    Object.DestroyImmediate(source);
                }
                var expected = new string[created.Count];
                for (int i = 0; i < created.Count; i++)
                {
                    AssetDatabase.AddObjectToAsset(created[i], profile);
                    profile.components.Add(created[i]);
                    EditorUtility.SetDirty(created[i]);
                    expected[i] = JsonUtility.ToJson(created[i]);
                }
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
                AssetDatabase.ImportAsset(ProfilePath, ImportAssetOptions.ForceUpdate);
                VerifyNative(AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath), guid, originalRoot, expected);
                VerifyGuards(plan);
                receipt.afterSha256 = Hash(ProfilePath);
                receipt.replacedComponents = 4;
                receipt.verifiedProtectedInputs = plan.protectedInputs.Length;
                receipt.status = "APPLIED_VALUES_AND_IDENTITIES_VERIFIED_VISUAL_PENDING";
            }
            catch (Exception e)
            {
                receipt.status = "FAILED";
                receipt.error = e.ToString();
                if (touched)
                {
                    try
                    {
                        File.Copy(output + "/before.asset.backup", ProfilePath, true);
                        File.Copy(output + "/before.meta.backup", ProfilePath + ".meta", true);
                        AssetDatabase.ImportAsset(ProfilePath, ImportAssetOptions.ForceUpdate);
                        Require(Hash(ProfilePath) == plan.sha256, "Rollback content mismatch.");
                        VerifyGuards(plan);
                        receipt.rollback = "ORIGINAL_ASSET_META_AND_GUARDS_VERIFIED";
                    }
                    catch (Exception rollback) { receipt.rollback = "FAILED: " + rollback; }
                }
                throw;
            }
            finally
            {
                foreach (var component in created)
                    if (component && !AssetDatabase.Contains(component)) Object.DestroyImmediate(component);
                File.WriteAllText(output + "/migration.json", JsonUtility.ToJson(receipt, true) + "\n");
                Debug.Log("TownVolume migration receipt: " + output + "/migration.json");
            }
        }
    }
}
