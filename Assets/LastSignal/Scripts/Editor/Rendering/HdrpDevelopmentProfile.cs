using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace LastSignal.Editor.Rendering
{
    // Opt-in asset only. Activation and performance acceptance require an owner benchmark.
    public static class HdrpDevelopmentProfile
    {
        public const string AssetPath = HdrpPipelineSetupUtility.HdrpSettingsFolder + "/LastSignal_HDRP_Development.asset";
        const string Evidence = "Docs/Implementation/HDRPProductionOptimization/Evidence/";
        const string SavedReceipt = Evidence + "development-profile.json";
        [Serializable] sealed class Guard { public string path, sha256; }
        [Serializable] sealed class Receipt
        {
            public string status, error, path = AssetPath, developmentSha256, timestampUtc;
            public string target = "MacBook M3 Pro / Metal; opt-in, NOT_ACTIVATED, NOT_BENCHMARKED";
            public Guard[] protectedInputs;
        }
        [MenuItem("Last Signal/Rendering/Create M3 Pro Development Profile")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit PlayMode first.");
            string source = HdrpPipelineSetupUtility.HighAssetPath;
            var receipt = new Receipt { status = "STARTED", timestampUtc = DateTime.UtcNow.ToString("O") };
            receipt.protectedInputs = new[] { source, source + ".meta", "ProjectSettings/GraphicsSettings.asset",
                "ProjectSettings/QualitySettings.asset", "ProjectSettings/ProjectSettings.asset" }
                .Select(p => new Guard { path = p, sha256 = Hash(p) }).ToArray();
            var high = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(source);
            if (!high) throw new InvalidOperationException("Missing High HDRP asset.");
            bool created = false;
            try
            {
                if (File.Exists(AssetPath) || File.Exists(AssetPath + ".meta"))
                {
                    var previous = File.Exists(SavedReceipt) ? JsonUtility.FromJson<Receipt>(File.ReadAllText(SavedReceipt)) : null;
                    if (previous == null || previous.path != AssetPath || previous.status != "CREATED_NOT_BENCHMARKED" ||
                        previous.developmentSha256 != Hash(AssetPath))
                        throw new InvalidOperationException("Existing Development asset has no matching receipt; preserve its tuning.");
                    if (previous.protectedInputs == null || previous.protectedInputs.Length != receipt.protectedInputs.Length ||
                        previous.protectedInputs.Any(g => Hash(g.path) != g.sha256))
                        throw new InvalidOperationException("Original profile inputs or active settings changed; review the existing Development asset.");
                    receipt.status = "EXISTING_UNCHANGED_NOT_BENCHMARKED";
                }
                else
                {
                    if (!AssetDatabase.CopyAsset(source, AssetPath)) throw new IOException("Cannot copy High profile.");
                    created = true;
                    var development = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(AssetPath);
                    development.name = "LastSignal_HDRP_Development";
                    var settings = development.currentPlatformRenderPipelineSettings;
                    settings.supportRayTracing = false;
                    settings.supportSSGI = false;
                    settings.supportSSRTransparent = false;
                    // Preserve opaque SSR, fog/cloud support and vegetation shader paths from High.
                    settings.hdShadowInitParams.maxDirectionalShadowMapResolution = Math.Min(settings.hdShadowInitParams.maxDirectionalShadowMapResolution, 2048);
                    settings.hdShadowInitParams.maxShadowRequests = Math.Min(settings.hdShadowInitParams.maxShadowRequests, 32);
                    settings.lightLoopSettings.maxPunctualLightsOnScreen = Math.Min(settings.lightLoopSettings.maxPunctualLightsOnScreen, 128);
                    settings.lightLoopSettings.maxAreaLightsOnScreen = Math.Min(settings.lightLoopSettings.maxAreaLightsOnScreen, 16);
                    settings.dynamicResolutionSettings.enabled = false;
                    development.currentPlatformRenderPipelineSettings = settings;
                    EditorUtility.SetDirty(development);
                    AssetDatabase.SaveAssetIfDirty(development);
                    receipt.status = "CREATED_NOT_BENCHMARKED";
                }
                var actual = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(AssetPath).currentPlatformRenderPipelineSettings;
                if (actual.supportRayTracing || actual.supportSSGI || actual.supportSSRTransparent || actual.dynamicResolutionSettings.enabled ||
                    actual.hdShadowInitParams.maxDirectionalShadowMapResolution > 2048 || actual.hdShadowInitParams.maxShadowRequests > 32 ||
                    actual.lightLoopSettings.maxPunctualLightsOnScreen > 128 || actual.lightLoopSettings.maxAreaLightsOnScreen > 16)
                    throw new InvalidOperationException("Development settings failed readback.");
                foreach (var guard in receipt.protectedInputs)
                    if (Hash(guard.path) != guard.sha256) throw new InvalidOperationException("Protected settings changed: " + guard.path);
                receipt.developmentSha256 = Hash(AssetPath);
                Directory.CreateDirectory(Evidence);
                if (created) File.WriteAllText(SavedReceipt, JsonUtility.ToJson(receipt, true) + "\n");
            }
            catch (Exception e)
            {
                receipt.status = "FAILED"; receipt.error = e.ToString();
                if (created && !AssetDatabase.DeleteAsset(AssetPath)) receipt.error += "\nCould not roll back newly created asset.";
                throw;
            }
            finally
            {
                Directory.CreateDirectory(Evidence);
                string output = Evidence + "development-profile-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ") + ".json";
                File.WriteAllText(output, JsonUtility.ToJson(receipt, true) + "\n");
                Debug.Log("Development profile: " + output + " — " + receipt.status);
            }
        }
        static string Hash(string path)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
    }
}
