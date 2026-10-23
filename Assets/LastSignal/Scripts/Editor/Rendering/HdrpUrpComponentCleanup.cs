using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LastSignal.Editor.Rendering
{
    public static class HdrpUrpComponentCleanup
    {
        const string Evidence = "Docs/Implementation/HDRPProductionOptimization/Evidence/";
        const string PlanPath = Evidence + "urp-component-cleanup-plan.json";
        const string PlanHash = "071798041f4d2f059409459df98b16bb81c4fe9735e42f4fce88c3cdc745014a";
        const string LegacyPlanPath = Evidence + "urp-legacy-components-plan.json";
        const string LegacyPlanHash = "d14cf765d7e99cd2b444435546b39d04c1766de41d916ed70927a9c60d098ec5";
        const string UrpCamera = "a79441f348de89743a2939f4d699eac1", UrpLight = "474bcb49853aa07438625e644c072ee6";
        const string HdCamera = "23c1ce4fb46143f46bc5cb5224c934f6";
        const string HdLight = "7a68c43fe1f2a47cfa234b5eeaa98012";
        [Serializable] sealed class Item { public string path, sha256, metaSha256; public int lights, cameras, newLights; }
        [Serializable] sealed class Plan { public Item[] items; }
        [Serializable] sealed class Guard { public string path, sha256; }
        [Serializable] sealed class Entry { public string path, beforeSha256, afterSha256, unitySaveSha256, status; public int lightsRemoved, camerasReplaced, lightsAdded, incidentalDocumentsRestored; }
        [Serializable] sealed class Receipt
        {
            public string status = "PREFLIGHT", error, rollback, planSha256 = PlanHash;
            public List<Entry> entries = new List<Entry>();
            public Guard[] protectedInputs;
        }
        sealed class Document { public string id, type, text; }
        static string Hash(string path)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
        static Dictionary<string, Document> Documents(string text)
        {
            var matches = Regex.Matches(text, @"(?m)^--- !u!(\d+) &(-?\d+)(?: stripped)?\r?$");
            if (matches.Count == 0) throw new InvalidOperationException("Expected text-serialized Unity asset.");
            var result = new Dictionary<string, Document>();
            for (int i = 0; i < matches.Count; i++)
            {
                var m = matches[i]; int end = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
                var doc = new Document { id = m.Groups[2].Value, type = m.Groups[1].Value, text = text.Substring(m.Index, end - m.Index).TrimEnd('\r', '\n') };
                result.Add(doc.id, doc);
            }
            return result;
        }
        static bool Script(Document d, string guid) => d.type == "114" && d.text.Contains("guid: " + guid);
        static string Owner(Document d) => Regex.Match(d.text, @"m_GameObject: \{fileID: (-?\d+)\}").Groups[1].Value;
        static bool IsPipelineReplacement(Document before, Document after)
        {
            // Scene serialization can reuse the destroyed URP component's fileID.
            // Permit only a same-owner, same-kind replacement, never arbitrary ID reuse.
            return !string.IsNullOrEmpty(Owner(before)) && Owner(before) == Owner(after) &&
                ((Script(before, UrpCamera) && Script(after, HdCamera)) ||
                 (Script(before, UrpLight) && Script(after, HdLight)));
        }
        static Document[] AddedPipelineData(Dictionary<string, Document> before, Dictionary<string, Document> after)
            => after.Values.Where(d => !before.TryGetValue(d.id, out var previous) || IsPipelineReplacement(previous, d)).ToArray();
        static string StripComponents(string text, IEnumerable<string> ids)
        {
            foreach (string id in ids)
                text = Regex.Replace(text, @"(?m)^  - component: \{fileID: " + Regex.Escape(id) + @"\}\r?\n", "");
            return text;
        }
        static void VerifyAddedData(Dictionary<string, Document> old, Document[] removed, Document[] added, Item item)
        {
            if (added.Length != item.cameras + item.newLights || added.Any(d => !Script(d, HdCamera) && !Script(d, HdLight)))
                throw new InvalidOperationException("Unexpected added object: " + item.path);
            var cameraOwners = removed.Where(d => Script(d, UrpCamera)).Select(Owner).OrderBy(x => x);
            if (!cameraOwners.SequenceEqual(added.Where(d => Script(d, HdCamera)).Select(Owner).OrderBy(x => x)))
                throw new InvalidOperationException("HDRP camera attached to wrong object: " + item.path);
            var existingLightOwners = new HashSet<string>(old.Values.Where(d => Script(d, HdLight)).Select(Owner));
            var newLightOwners = removed.Where(d => Script(d, UrpLight) && !existingLightOwners.Contains(Owner(d))).Select(Owner).OrderBy(x => x).ToArray();
            if (newLightOwners.Length != item.newLights || !newLightOwners.SequenceEqual(added.Where(d => Script(d, HdLight)).Select(Owner).OrderBy(x => x)))
                throw new InvalidOperationException("HDRP light additions disagree with guarded plan/owners: " + item.path);
        }
        // Unity may serialize new defaults on unrelated scripts while saving a prefab.
        // Keep only the requested component edit from that save; preserve the original
        // serialized records everywhere else, including gameplay and physics components.
        // New HDRP data is still created and serialized by Unity, never synthesized here.
        static string PreserveUnrelatedContent(string before, string unitySave, Item item, Entry entry)
        {
            var old = Documents(before); var saved = Documents(unitySave);
            var removed = old.Values.Where(d => Script(d, UrpCamera) || Script(d, UrpLight)).ToArray();
            var removedIds = new HashSet<string>(removed.Select(d => d.id));
            var added = AddedPipelineData(old, saved);
            if (removed.Count(d => Script(d, UrpCamera)) != item.cameras || removed.Count(d => Script(d, UrpLight)) != item.lights ||
                saved.Values.Any(d => Script(d, UrpCamera) || Script(d, UrpLight)) ||
                removed.Any(d => saved.TryGetValue(d.id, out var replacement) && !IsPipelineReplacement(d, replacement)))
                throw new InvalidOperationException("Unity save changed the component scope: " + item.path);
            VerifyAddedData(old, removed, added, item);
            var cameraOwners = removed.Where(d => Script(d, UrpCamera)).Select(Owner).OrderBy(x => x).ToArray();
            string newline = before.Contains("\r\n") ? "\r\n" : "\n";
            var result = new StringBuilder(before.Substring(0, before.IndexOf("--- !u!", StringComparison.Ordinal)));
            foreach (var doc in old.Values)
            {
                if (removedIds.Contains(doc.id)) continue;
                if (!saved.TryGetValue(doc.id, out var now) || now.type != doc.type)
                    throw new InvalidOperationException("Unity save removed or replaced an unrelated object: " + doc.id);
                string kept = doc.text;
                if (doc.type == "1")
                {
                    // Require Unity's component list to contain precisely the intended change.
                    var oldRefs = Regex.Matches(doc.text, @"(?m)^  - component: \{fileID: (-?\d+)\}\r?$").Cast<Match>()
                        .Select(m => m.Groups[1].Value).Where(id => !removedIds.Contains(id));
                    var newRefs = Regex.Matches(now.text, @"(?m)^  - component: \{fileID: (-?\d+)\}\r?$").Cast<Match>()
                        .Select(m => m.Groups[1].Value);
                    var owned = added.Where(d => Owner(d) == doc.id).Select(d => d.id).ToArray();
                    if (!oldRefs.Concat(owned).OrderBy(x => x).SequenceEqual(newRefs.OrderBy(x => x)))
                        throw new InvalidOperationException("Unexpected component-list change: " + doc.id);
                    kept = StripComponents(kept, removedIds);
                    foreach (string id in owned)
                    {
                        var list = Regex.Match(kept, @"(?m)^  m_Component:\r?\n(?:  - component: \{fileID: -?\d+\}\r?\n)+");
                        if (!list.Success) throw new InvalidOperationException("Missing component list: " + doc.id);
                        kept = kept.Insert(list.Index + list.Length, "  - component: {fileID: " + id + "}" + newline);
                    }
                }
                if (doc.type == "20" && cameraOwners.Contains(Owner(doc)))
                    kept = Regex.Replace(kept, @"(?m)^  (m_HDR|m_AllowMSAA): [01](\r?)$", "  $1: 0$2");
                if (kept != now.text) entry.incidentalDocumentsRestored++;
                result.Append(kept).Append(newline);
            }
            foreach (var doc in added) result.Append(doc.text).Append(newline);
            string merged = result.ToString();
            VerifySerializedChange(before, merged, item);
            return merged;
        }
        static void VerifySerializedChange(string before, string after, Item item)
        {
            var old = Documents(before); var current = Documents(after);
            var removed = old.Values.Where(d => Script(d, UrpCamera) || Script(d, UrpLight)).ToArray();
            if (removed.Count(d => Script(d, UrpLight)) != item.lights || removed.Count(d => Script(d, UrpCamera)) != item.cameras)
                throw new InvalidOperationException("Serialized removal scope mismatch: " + item.path);
            var added = AddedPipelineData(old, current);
            VerifyAddedData(old, removed, added, item);
            var cameraOwners = removed.Where(d => Script(d, UrpCamera)).Select(Owner).OrderBy(x => x).ToArray();
            var removedIds = new HashSet<string>(removed.Select(d => d.id));
            if (current.Values.Any(d => Script(d, UrpCamera) || Script(d, UrpLight)))
                throw new InvalidOperationException("URP component survived: " + item.path);
            foreach (var doc in old.Values)
            {
                if (removedIds.Contains(doc.id))
                {
                    if (current.TryGetValue(doc.id, out var replacement) && !IsPipelineReplacement(doc, replacement))
                        throw new InvalidOperationException("Removed ID reused by an unapproved object: " + doc.id);
                    continue;
                }
                if (!current.TryGetValue(doc.id, out var now)) throw new InvalidOperationException("Unrelated object removed: " + doc.id);
                string a = doc.text, b = now.text;
                if (doc.type == "1") { a = StripComponents(a, removedIds); b = StripComponents(b, added.Select(d => d.id)); }
                if (doc.type == "20" && cameraOwners.Contains(Owner(doc)))
                {
                    // HDRP OnEnable disables these two legacy camera flags; no lens, culling,
                    // depth, transform, enabled-state or gameplay field is allowed to change.
                    const string flags = @"(?m)^  (m_HDR|m_AllowMSAA): [01]\r?$";
                    a = Regex.Replace(a, flags, "  $1: 0"); b = Regex.Replace(b, flags, "  $1: 0");
                }
                if (a != b) throw new InvalidOperationException("Unrelated serialized content changed at fileID " + doc.id + ": " + item.path);
            }
        }
        static bool IsUrp(Component c) => c && (c.GetType().FullName == "UnityEngine.Rendering.Universal.UniversalAdditionalLightData" || c.GetType().FullName == "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
        static void Convert(GameObject[] roots, Item item, bool legacyStage, bool preflightOnly = false)
        {
            var legacy = roots.SelectMany(r => r.GetComponentsInChildren<Component>(true)).Where(IsUrp).ToArray();
            if (legacy.Count(c => c.GetComponent<Light>()) != item.lights || legacy.Count(c => c.GetComponent<Camera>()) != item.cameras)
                throw new InvalidOperationException("Loaded component scope mismatch: " + item.path);
            if (legacy.Count(c => c.GetComponent<Light>() && !c.GetComponent<HDAdditionalLightData>()) != item.newLights)
                throw new InvalidOperationException("Loaded HDRP light additions disagree with plan: " + item.path);
            foreach (var component in legacy)
            {
                var light = component.GetComponent<Light>();
                if (light)
                {
                    if (!light.GetComponent<HDAdditionalLightData>())
                    {
                        if (!legacyStage) throw new InvalidOperationException("Missing existing HDRP light data.");
                        if (light.type != LightType.Point && light.type != LightType.Spot && light.type != LightType.Directional)
                            throw new InvalidOperationException("Unreviewed legacy light type: " + item.path);
                        // HDRP currently renders these via its default additional-data singleton.
                        // Persist default data, preserving the Light's intensity/unit/range/shadows.
                        // Do not call InitDefaultHDAdditionalLightData: it changes intensity/units.
                        if (!preflightOnly) light.gameObject.AddComponent<HDAdditionalLightData>();
                    }
                }
                else
                {
                    var camera = component.GetComponent<Camera>();
                    if (!camera || camera.GetComponent<HDAdditionalCameraData>()) throw new InvalidOperationException("Unexpected camera precondition.");
                    var data = new SerializedObject(component);
                    int rendererIndex = data.FindProperty("m_RendererIndex").intValue;
                    if (data.FindProperty("m_CameraType").intValue != 0 || data.FindProperty("m_Cameras").arraySize != 0 ||
                        (rendererIndex != -1 && !(legacyStage && rendererIndex == 0)) || data.FindProperty("m_Antialiasing").intValue != 0 ||
                        (!legacyStage && data.FindProperty("m_RenderPostProcessing").boolValue) || data.FindProperty("m_UseScreenCoordOverride").boolValue)
                        throw new InvalidOperationException("Camera needs manual stack/post/renderer migration: " + item.path);
                    if (preflightOnly) continue;
                    var hd = camera.gameObject.AddComponent<HDAdditionalCameraData>();
                    hd.volumeLayerMask = data.FindProperty("m_VolumeLayerMask").intValue;
                    hd.volumeAnchorOverride = (Transform)data.FindProperty("m_VolumeTrigger").objectReferenceValue;
                    hd.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
                    hd.dithering = data.FindProperty("m_Dithering").boolValue;
                    hd.stopNaNs = data.FindProperty("m_StopNaN").boolValue;
                    hd.clearDepth = camera.clearFlags != CameraClearFlags.Nothing;
                    hd.clearColorMode = camera.clearFlags == CameraClearFlags.Skybox ? HDAdditionalCameraData.ClearColorMode.Sky :
                        camera.clearFlags == CameraClearFlags.SolidColor ? HDAdditionalCameraData.ClearColorMode.Color : HDAdditionalCameraData.ClearColorMode.None;
                    hd.backgroundColorHDR = camera.backgroundColor;
                    camera.allowHDR = false; camera.allowMSAA = false;
                    // Retain HDRP's existing default frame settings, as used before explicit camera
                    // data existed. Gameplay post/AA continues through the existing runtime adapter.
                }
                if (!preflightOnly) Object.DestroyImmediate(component);
            }
        }
        public static void Run() => RunPlan(false);
        public static void RunLegacy() => RunPlan(true);
        static void RunPlan(bool legacyStage)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !LastSignal.Rendering.RenderPipelineAdapter.IsHDRP)
                throw new InvalidOperationException("Run outside Play mode with HDRP active.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save and close dirty scenes first.");
            string planPath = legacyStage ? LegacyPlanPath : PlanPath, planHash = legacyStage ? LegacyPlanHash : PlanHash;
            if (Hash(planPath) != planHash) throw new InvalidOperationException("Component plan changed.");
            var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(planPath));
            if (plan.items.Length != (legacyStage ? 33 : 5) || plan.items.Sum(x => x.lights) != (legacyStage ? 326 : 42) ||
                plan.items.Sum(x => x.cameras) != (legacyStage ? 35 : 4) || plan.items.Sum(x => x.newLights) != (legacyStage ? 150 : 0))
                throw new InvalidOperationException("Unexpected component plan.");
            string output = Evidence + (legacyStage ? "urp-legacy-component-cleanup-" : "urp-component-cleanup-") + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ") + "-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(output); File.Copy(planPath, output + "/plan.json");
            var receipt = new Receipt { planSha256 = planHash }; var touched = new List<Item>();
            var guardedPaths = new List<string> {
                "Assets/LastSignal/Scenes/Validation/ForestPrototype120.unity",
                "Assets/LastSignal/Scenes/Validation/ForestPrototype120.unity.meta",
                "Assets/LastSignal/Scripts/Runtime/Gathering/TreePhysicsBody.cs",
                "Packages/manifest.json", "Packages/packages-lock.json"
            };
            string guardedScene = legacyStage ? "Assets/LastSignal/Scenes/Production/FirstTownCore.unity" :
                "Assets/LastSignal/Scenes/Validation/FirstTownTreePhysics_20261019163434-8e0f05b7.unity";
            guardedPaths.Add(guardedScene); guardedPaths.Add(guardedScene + ".meta");
            // S004 is a rendering-only target in the legacy plan. Its gameplay/physics
            // records are protected by the same exact document comparison as every target.
            receipt.protectedInputs = guardedPaths.Select(p => new Guard { path = p, sha256 = Hash(p) }).ToArray();
            string Backup(Item item) => output + "/before/" + item.path + ".backup";
            try
            {
                foreach (var item in plan.items)
                {
                    if (!item.path.StartsWith("Assets/LastSignal/", StringComparison.Ordinal) || item.path.Contains("..") ||
                        Hash(item.path) != item.sha256 || Hash(item.path + ".meta") != item.metaSha256)
                        throw new InvalidOperationException("Input changed: " + item.path);
                    string raw = File.ReadAllText(item.path);
                    foreach (var d in Documents(raw).Values.Where(d => Script(d, UrpCamera) || Script(d, UrpLight)))
                        if (Regex.Matches(raw, @"\{fileID: " + Regex.Escape(d.id) + @"\}").Count != 1)
                            throw new InvalidOperationException("URP data has other serialized consumers: " + item.path + " / " + d.id);
                    Directory.CreateDirectory(Path.GetDirectoryName(Backup(item)));
                    File.Copy(item.path, Backup(item)); File.Copy(item.path + ".meta", Backup(item) + ".meta");
                }
                if (legacyStage)
                {
                    // Check all loaded component counts/settings before the first asset save.
                    foreach (var item in plan.items) Visit(item, roots => Convert(roots, item, true, true), false);
                    foreach (var item in plan.items)
                        if (Hash(item.path) != item.sha256 || Hash(item.path + ".meta") != item.metaSha256)
                            throw new InvalidOperationException("Asset changed during loaded preflight: " + item.path);
                    foreach (var guard in receipt.protectedInputs)
                        if (Hash(guard.path) != guard.sha256) throw new InvalidOperationException("Protected input changed during preflight: " + guard.path);
                }
                foreach (var item in plan.items)
                {
                    if (Hash(item.path) != item.sha256) throw new InvalidOperationException("Input changed after preflight.");
                    touched.Add(item);
                    var entry = new Entry { path = item.path, beforeSha256 = item.sha256, status = "PROCESSING" }; receipt.entries.Add(entry);
                    Visit(item, roots => Convert(roots, item, legacyStage), true);
                    string before = File.ReadAllText(Backup(item)), unitySave = File.ReadAllText(item.path);
                    string savedEvidence = output + "/unity-save/" + item.path + ".backup";
                    Directory.CreateDirectory(Path.GetDirectoryName(savedEvidence));
                    File.Copy(item.path, savedEvidence);
                    entry.unitySaveSha256 = Hash(item.path);
                    string merged = PreserveUnrelatedContent(before, unitySave, item, entry);
                    File.WriteAllText(item.path, merged, new UTF8Encoding(false));
                    AssetDatabase.ImportAsset(item.path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                    VerifySerializedChange(before, File.ReadAllText(item.path), item);
                    if (Hash(item.path + ".meta") != item.metaSha256) throw new InvalidOperationException("GUID metadata changed.");
                    entry.afterSha256 = Hash(item.path); entry.lightsRemoved = item.lights; entry.camerasReplaced = item.cameras; entry.lightsAdded = item.newLights; entry.status = "VERIFIED_ALLOWED_COMPONENT_CHANGES_ONLY";
                }
                foreach (var guard in receipt.protectedInputs)
                    if (Hash(guard.path) != guard.sha256) throw new InvalidOperationException("Protected input changed: " + guard.path);
                receipt.status = "APPLIED_RUNTIME_AND_COVERAGE_VERIFICATION_REQUIRED";
            }
            catch (Exception e)
            {
                receipt.status = "FAILED"; receipt.error = e.ToString(); var errors = new List<string>();
                foreach (var item in touched.AsEnumerable().Reverse())
                {
                    try { File.Copy(Backup(item), item.path, true); File.Copy(Backup(item) + ".meta", item.path + ".meta", true);
                        AssetDatabase.ImportAsset(item.path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                        if (Hash(item.path) != item.sha256 || Hash(item.path + ".meta") != item.metaSha256) throw new IOException("Rollback hash mismatch."); }
                    catch (Exception restore) { errors.Add(item.path + ": " + restore.Message); }
                }
                receipt.rollback = errors.Count == 0 ? "All touched files restored (or no mutation started)." : "REVIEW_REQUIRED: " + string.Join("; ", errors);
                throw;
            }
            finally { File.WriteAllText(output + "/cleanup.json", JsonUtility.ToJson(receipt, true) + "\n"); Debug.Log("URP component cleanup: " + output + " — " + receipt.status); }
        }
        static void Visit(Item item, Action<GameObject[]> action, bool save)
        {
            if (item.path.EndsWith(".prefab", StringComparison.Ordinal))
            {
                var root = PrefabUtility.LoadPrefabContents(item.path);
                try
                {
                    action(new[] { root });
                    if (save) { PrefabUtility.SaveAsPrefabAsset(root, item.path, out bool saved); if (!saved) throw new IOException("Prefab save failed."); }
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            else
            {
                if (SceneManager.GetSceneByPath(item.path).isLoaded) throw new InvalidOperationException("Close target scene first: " + item.path);
                var previous = SceneManager.GetActiveScene();
                var scene = EditorSceneManager.OpenScene(item.path, OpenSceneMode.Additive);
                try { action(scene.GetRootGameObjects()); if (save && !EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed."); }
                finally { EditorSceneManager.CloseScene(scene, true); if (previous.IsValid()) SceneManager.SetActiveScene(previous); }
            }
        }
    }
}
