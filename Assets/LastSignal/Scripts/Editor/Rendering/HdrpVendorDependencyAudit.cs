using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastSignal.Editor.Rendering
{
    // No conversion, asset save/import, scene load or shader compilation request.
    public static class HdrpVendorDependencyAudit
    {
        const string PlanPath = "Docs/Implementation/HDRPProductionOptimization/Evidence/vendor-closure-plan.json";
        const string PlanHash = "7751fcfd68f1952819c29fe27f8ebe1133d54ba4d1025a304472a6a0c40aa0f6";
        [Serializable] sealed class Item { public string path, sha256, metaSha256; public string[] knownUrpTargets; }
        [Serializable] sealed class Guard { public string path, sha256; }
        [Serializable] sealed class Plan { public Item[] items; public string[] productionRoots; public Guard[] protectedInputs; }
        [Serializable] sealed class Entry
        {
            public string path, kind, shader, shaderPath, classification;
            public bool projectReachable, productionReachable, buildReachable, resourcesReachable, preloadedOrGraphicsReachable;
            public bool nativeHdrpShader, shaderHasError;
            public int obsoleteUrpVersionObjects;
            public string[] urpDependencies, allDependencies, nativeVariantCandidates, productionRoots, directProjectConsumers;
        }
        [Serializable] sealed class Report
        {
            public string status = "READ_ONLY_AUDIT_COMPLETE_REVIEW_REQUIRED", timestampUtc, planSha256 = PlanHash;
            public int projectAssetCount, resourcesAssetCount;
            public string[] buildScenes, resourcesRoots;
            public string graphicsRegistration = "Project URP global setting intentionally retained until package removal to prevent vendor auto-upgrade.";
            public List<Entry> entries = new List<Entry>();
            public List<string> errors = new List<string>();
            public string limitation = "Reachability includes serialized dependencies, not proof of visible rendering. Dynamic Addressables/AssetBundle/string loads and vendor licensing/source preservation still require review. ShaderGraph multi-target import is not visual certification. No assets are disposable solely because absent from these closures.";
        }
        static string Hash(string path)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
        static void Verify(Plan plan)
        {
            foreach (var item in plan.items)
                if (Hash(item.path) != item.sha256 || Hash(item.path + ".meta") != item.metaSha256)
                    throw new InvalidOperationException("Audit input changed: " + item.path);
            foreach (var guard in plan.protectedInputs)
                if (Hash(guard.path) != guard.sha256) throw new InvalidOperationException("Protected input changed: " + guard.path);
        }
        static HashSet<string> Dependencies(string[] paths) => paths.Length == 0
            ? new HashSet<string>() : new HashSet<string>(AssetDatabase.GetDependencies(paths, true));
        static bool IsUrp(string path) => path.StartsWith("Packages/com.unity.render-pipelines.universal", StringComparison.Ordinal);
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !LastSignal.Rendering.RenderPipelineAdapter.IsHDRP)
                throw new InvalidOperationException("Run outside Play mode with HDRP active.");
            if (Hash(PlanPath) != PlanHash) throw new InvalidOperationException("Vendor audit plan changed.");
            var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(PlanPath));
            Verify(plan);
            var report = new Report { timestampUtc = DateTime.UtcNow.ToString("O") };
            string output = "Docs/Implementation/HDRPProductionOptimization/Evidence/vendor-closure-" +
                DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ") + "-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(output);
            File.Copy(PlanPath, output + "/plan.json");
            try
            {
                // Query the already-imported database, not another filesystem discovery audit.
                var paths = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(p)).ToArray();
                var project = paths.Where(p => p.StartsWith("Assets/LastSignal/", StringComparison.Ordinal)).ToArray();
                var resources = paths.Where(p => p.Contains("/Resources/")).ToArray();
                var builds = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                var implicitRoots = PlayerSettings.GetPreloadedAssets().Where(x => x).Select(AssetDatabase.GetAssetPath).Where(p => !string.IsNullOrEmpty(p)).ToList();
                var graphics = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
                var alwaysIncluded = graphics.FindProperty("m_AlwaysIncludedShaders");
                if (alwaysIncluded != null)
                    for (int i = 0; i < alwaysIncluded.arraySize; i++)
                    {
                        string p = AssetDatabase.GetAssetPath(alwaysIncluded.GetArrayElementAtIndex(i).objectReferenceValue);
                        if (!string.IsNullOrEmpty(p)) implicitRoots.Add(p);
                    }
                var projectDeps = Dependencies(project);
                var resourceDeps = Dependencies(resources);
                var buildDeps = Dependencies(builds);
                var implicitDeps = Dependencies(implicitRoots.ToArray());
                var production = plan.productionRoots.ToDictionary(p => p, p => Dependencies(new[] { p }));
                var directConsumers = plan.items.ToDictionary(x => x.path, x => new List<string>());
                foreach (string p in project)
                    foreach (string dep in AssetDatabase.GetDependencies(p, false))
                        if (dep != p && directConsumers.TryGetValue(dep, out var consumers)) consumers.Add(p);
                report.projectAssetCount = project.Length;
                report.resourcesAssetCount = resources.Length;
                report.buildScenes = builds;
                report.resourcesRoots = resources;
                foreach (var item in plan.items)
                {
                    var deps = AssetDatabase.GetDependencies(item.path, true);
                    var entry = new Entry { path = item.path, kind = Path.GetExtension(item.path),
                        projectReachable = projectDeps.Contains(item.path), buildReachable = buildDeps.Contains(item.path),
                        resourcesReachable = resourceDeps.Contains(item.path), preloadedOrGraphicsReachable = implicitDeps.Contains(item.path),
                        productionRoots = production.Where(x => x.Value.Contains(item.path)).Select(x => x.Key).ToArray(),
                        directProjectConsumers = directConsumers[item.path].Distinct().OrderBy(x => x).ToArray(),
                        urpDependencies = deps.Where(IsUrp).OrderBy(x => x).ToArray(), allDependencies = deps.OrderBy(x => x).ToArray() };
                    entry.productionReachable = entry.productionRoots.Length > 0;
                    string counterpart = item.path.Replace("/URP/", "/HDRP/").Replace("_URP", "_HDRP");
                    entry.nativeVariantCandidates = counterpart != item.path && File.Exists(counterpart) ? new[] { counterpart } : Array.Empty<string>();
                    if (item.path.EndsWith(".mat", StringComparison.Ordinal))
                    {
                        var material = AssetDatabase.LoadAssetAtPath<Material>(item.path);
                        var shader = material ? material.shader : null;
                        entry.shader = shader ? shader.name : "MISSING";
                        entry.shaderPath = shader ? AssetDatabase.GetAssetPath(shader) : "";
                        if (shader)
                        {
                            entry.shaderHasError = ShaderUtil.ShaderHasError(shader);
                            for (int i = 0; i < shader.subshaderCount; i++)
                                entry.nativeHdrpShader |= shader.FindSubshaderTagValue(i, new ShaderTagId("RenderPipeline")).name == "HDRenderPipeline";
                            entry.nativeHdrpShader |= shader.name.StartsWith("HDRP/", StringComparison.Ordinal);
                        }
                        entry.obsoleteUrpVersionObjects = AssetDatabase.LoadAllAssetsAtPath(item.path)
                            .Count(x => x && x.GetType().FullName == "UnityEditor.Rendering.Universal.AssetVersion");
                    }
                    bool buildInput = entry.buildReachable || entry.resourcesReachable || entry.preloadedOrGraphicsReachable;
                    entry.classification = buildInput ? "BUILD_OR_RESOURCES_INPUT_REQUIRES_RESOLUTION" :
                        entry.productionReachable ? "PRODUCTION_DEPENDENCY_REQUIRES_RESOLUTION" :
                        entry.projectReachable ? "OTHER_PROJECT_DEPENDENCY_REVIEW" : "NO_REACHABILITY_IN_CHECKED_ROOTS_NOT_DISPOSAL_APPROVAL";
                    report.entries.Add(entry);
                }
                Verify(plan);
            }
            catch (Exception e)
            {
                report.status = "FAILED"; report.errors.Add(e.ToString()); throw;
            }
            finally
            {
                File.WriteAllText(output + "/audit.json", JsonUtility.ToJson(report, true) + "\n");
                Debug.Log("Vendor dependency audit: " + output + "/audit.json");
            }
        }
    }
}
