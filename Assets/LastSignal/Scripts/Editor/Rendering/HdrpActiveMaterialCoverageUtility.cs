using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace LastSignal.Editor.Rendering
{
    /// <summary>Owner-run authored-use audit. No Save, Import, material assignment or scene promotion.</summary>
    public static class HdrpActiveMaterialCoverageUtility
    {
        const string ScopePath = "Docs/Implementation/VisualRealismMigration/MaterialCoverageScope.json";
        const string ReportPath = "Docs/Implementation/VisualRealismMigration/Evidence/active_material_coverage.json";
        [Serializable] public sealed class Scope
        {
            public int schemaVersion;
            public string[] scenes, prefabs, dataAssets, runtimePathsReviewed;
            public string runtimeLimit;
        }
        [Serializable] public sealed class Use
        {
            public string root, hierarchy, component, property, kind, lod;
            public int slot;
            public bool enabled, activeInHierarchy, prefabOverride;
        }
        [Serializable] public sealed class MaterialRecord
        {
            public string identity, path, guid, name, shader, shaderPath, classification, sourceScope;
            public long localId;
            public bool hdrpSubshader, shaderSupported, shaderError;
            public int effectiveSlots, serializedInputs;
            public List<string> issues = new List<string>();
            public List<string> observations = new List<string>();
            public List<Use> uses = new List<Use>();
        }
        [Serializable] public sealed class MeshRecord
        {
            public string path, guid, name, identity;
            public long localId;
            public int vertices, submeshes;
            public long indexCount;
            public Vector3 boundsCenter, boundsSize;
            public bool normals, uv0, isReadable;
            public List<string> topology = new List<string>();
        }
        [Serializable] public sealed class Count { public string key; public int count; }
        [Serializable] public sealed class Report
        {
            public string status, timestampUtc, scopeHash, unityVersion, activePipeline;
            public string visualStatus = "VISUAL_NOT_APPROVED";
            public string defaultPipeline, hdrpGlobalSettings;
            public List<string> qualityPipelines = new List<string>();
            public int rootCount, resolvedRendererSlots, materialObjects, sourceOnlyObjects;
            public List<MaterialRecord> materials = new List<MaterialRecord>();
            public List<Count> classificationCounts = new List<Count>();
            public List<Count> shaderFamilies = new List<Count>();
            public List<Use> missingSlots = new List<Use>();
            public List<MeshRecord> medicineMeshes = new List<MeshRecord>();
            public List<string> scannedRoots = new List<string>();
            public List<string> findings = new List<string>();
            public List<string> errors = new List<string>();
            public List<string> limitations = new List<string>();
        }

        sealed class Audit
        {
            internal readonly Report report = new Report();
            readonly Dictionary<string, MaterialRecord> records = new Dictionary<string, MaterialRecord>();
            readonly Dictionary<string, string> hashes = new Dictionary<string, string>();
            readonly HashSet<string> dependencies = new HashSet<string>();
            readonly HashSet<string> scannedData = new HashSet<string>();
            readonly HashSet<string> scannedPrefabs = new HashSet<string>();
            readonly Queue<string> prefabs = new Queue<string>();

            internal void AddDependencies(string path)
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) throw new InvalidOperationException("Missing audit root: " + path);
                foreach (string dependency in AssetDatabase.GetDependencies(path, true).Append(path))
                {
                    if (!dependency.StartsWith("Assets/", StringComparison.Ordinal) || !dependencies.Add(dependency)) continue;
                    foreach (string file in new[] { dependency, dependency + ".meta" })
                        if (File.Exists(file)) hashes[file] = HdrpMaterialIntegrityUtility.Hash(file);
                }
            }

            internal void QueueGameObjectAsset(string path)
            {
                if (string.IsNullOrEmpty(path)) return;
                if (!new[] { ".prefab", ".fbx", ".obj", ".blend" }.Contains(Path.GetExtension(path).ToLowerInvariant())) return;
                AddDependencies(path);
                prefabs.Enqueue(path);
            }

            MaterialRecord Record(Material material)
            {
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material, out string guid, out long id))
                    throw new InvalidOperationException("Transient material in authored audit: " + material.name);
                string key = guid + ":" + id;
                if (records.TryGetValue(key, out var record)) return record;
                record = new MaterialRecord { identity = key, guid = guid, localId = id,
                    path = AssetDatabase.GetAssetPath(material), name = material.name };
                record.sourceScope = record.path.StartsWith("Assets/ThirdParty/", StringComparison.Ordinal) ? "VENDOR" :
                    record.path.StartsWith("Assets/LastSignal/", StringComparison.Ordinal) ? "PROJECT" : "BUILTIN_OR_PACKAGE";
                var shader = material.shader;
                if (!shader) record.issues.Add("Missing shader");
                else
                {
                    record.shader = shader.name; record.shaderPath = AssetDatabase.GetAssetPath(shader);
                    record.shaderSupported = shader.isSupported; record.shaderError = ShaderUtil.ShaderHasError(shader);
                    for (int i = 0; i < shader.subshaderCount; i++)
                        if (shader.FindSubshaderTagValue(i, new ShaderTagId("RenderPipeline")).name == "HDRenderPipeline")
                            record.hdrpSubshader = true;
                    if (shader.name.StartsWith("HDRP/", StringComparison.Ordinal) || shader.name.StartsWith("LastSignal/HDRP/", StringComparison.Ordinal))
                        record.hdrpSubshader = true;
                    if (!record.shaderSupported) record.issues.Add("Shader unsupported on current Editor platform");
                    if (record.shaderError) record.issues.Add("Shader reports compiler errors");
                    if (!record.hdrpSubshader) record.issues.Add("No explicit HDRP subshader; manual pipeline review required");
                    if (shader.name == "HDRP/Lit")
                    {
                        CheckMap(material, record, "_MaskMap", "_MASKMAP", true);
                        record.issues.AddRange(ValidateNormalMapKeywords(material));
                        foreach (string prefix in new[] { "_MetallicRemap", "_SmoothnessRemap", "_AORemap" })
                        {
                            if (!material.HasProperty(prefix + "Min") || !material.HasProperty(prefix + "Max"))
                            { record.issues.Add("Missing HDRP range: " + prefix); continue; }
                            float min = material.GetFloat(prefix + "Min"), max = material.GetFloat(prefix + "Max");
                            if (float.IsNaN(min) || float.IsNaN(max) || min < 0 || max > 1 || min > max)
                                record.issues.Add("Invalid HDRP range: " + prefix + " " + min + ".." + max);
                            record.observations.Add(prefix + "=" + min.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ".." +
                                max.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                        }
                        // Zero metallic ranges are valid nonmetallic cases, not an automatic repair trigger.
                    }
                }
                records.Add(key, record); return record;
            }

            static void CheckMap(Material material, MaterialRecord record, string property, string keyword, bool linear)
            {
                if (!material.HasProperty(property)) { record.issues.Add("Missing property " + property); return; }
                var texture = material.GetTexture(property);
                if (material.IsKeywordEnabled(keyword) && !texture) record.issues.Add("Enabled map keyword without texture: " + keyword);
                if (!texture) return;
                string path = AssetDatabase.GetAssetPath(texture);
                record.observations.Add(property + "=" + path);
                if (property == "_MaskMap" && !material.IsKeywordEnabled(keyword)) record.issues.Add("Assigned Mask Map with disabled keyword");
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (linear && importer != null && importer.sRGBTexture) record.issues.Add("Mask Map has sRGB importer: " + path);
                if (linear && importer == null) record.observations.Add("Mask Map importer is not TextureImporter; channel interpretation needs review");
            }

            void AddUse(Material material, Use use)
            {
                if (!material) { report.missingSlots.Add(use); return; }
                var record = Record(material);
                record.uses.Add(use);
                if (use.kind == "renderer-slot" || use.kind == "terrain-template") record.effectiveSlots++;
                else record.serializedInputs++;
            }

            static string Hierarchy(Transform value)
            {
                var parts = new List<string>();
                while (value) { parts.Add(value.name + "[" + value.GetSiblingIndex() + "]"); value = value.parent; }
                parts.Reverse(); return string.Join("/", parts);
            }

            void InspectSerialized(Object obj, string rootPath, string hierarchy)
            {
                var serialized = new SerializedObject(obj);
                var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var referenced = property.objectReferenceValue;
                    if (referenced is Material material)
                        AddUse(material, new Use { root = rootPath, hierarchy = hierarchy, component = obj.GetType().Name,
                            property = property.propertyPath, kind = "serialized-runtime-input", slot = -1 });
                    else if (referenced is GameObject prefab)
                        QueueGameObjectAsset(AssetDatabase.GetAssetPath(prefab));
                    else if (referenced == null && property.objectReferenceEntityIdValue.IsValid())
                        report.findings.Add("Unresolved serialized reference: " + rootPath + " | " + hierarchy + " | " + property.propertyPath);
                }
            }

            internal void InspectRoots(IEnumerable<GameObject> roots, string path)
            {
                report.scannedRoots.Add(path);
                foreach (var root in roots)
                {
                    var lods = new Dictionary<Renderer, List<string>>();
                    foreach (var group in root.GetComponentsInChildren<LODGroup>(true))
                    {
                        var levels = group.GetLODs();
                        for (int i = 0; i < levels.Length; i++) foreach (var renderer in levels[i].renderers)
                        {
                            if (!renderer) { report.findings.Add("Null LOD renderer: " + path + " | " + Hierarchy(group.transform)); continue; }
                            if (!lods.TryGetValue(renderer, out var values)) lods[renderer] = values = new List<string>();
                            values.Add(Hierarchy(group.transform) + ":LOD" + i);
                        }
                    }
                    foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    {
                        var materials = renderer.sharedMaterials;
                        var serialized = new SerializedObject(renderer);
                        var array = serialized.FindProperty("m_Materials");
                        for (int slot = 0; slot < materials.Length; slot++)
                        {
                            var use = new Use { root = path, hierarchy = Hierarchy(renderer.transform), component = renderer.GetType().Name,
                                kind = "renderer-slot", property = "m_Materials.Array.data[" + slot + "]", slot = slot,
                                activeInHierarchy = renderer.gameObject.activeInHierarchy, enabled = renderer.enabled,
                                prefabOverride = array != null && slot < array.arraySize && array.GetArrayElementAtIndex(slot).prefabOverride,
                                lod = lods.TryGetValue(renderer, out var levels) ? string.Join(";", levels) : "" };
                            AddUse(materials[slot], use); report.resolvedRendererSlots++;
                        }
                        if (materials.Length == 0) report.findings.Add("Renderer has zero material slots: " + path + " | " + Hierarchy(renderer.transform));
                    }
                    foreach (var terrain in root.GetComponentsInChildren<Terrain>(true))
                    {
                        if (terrain.materialTemplate) AddUse(terrain.materialTemplate, new Use { root = path,
                            hierarchy = Hierarchy(terrain.transform), kind = "terrain-template", component = "Terrain", property = "materialTemplate", slot = 0 });
                        else report.findings.Add("Terrain uses pipeline default material; review visually: " + path + " | " + Hierarchy(terrain.transform));
                        if (!terrain.terrainData) { report.findings.Add("TerrainData missing: " + path); continue; }
                        foreach (var tree in terrain.terrainData.treePrototypes) if (tree.prefab) QueueGameObjectAsset(AssetDatabase.GetAssetPath(tree.prefab));
                        foreach (var detail in terrain.terrainData.detailPrototypes) if (detail.prototype) QueueGameObjectAsset(AssetDatabase.GetAssetPath(detail.prototype));
                    }
                    foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    foreach (var component in transform.GetComponents<MonoBehaviour>())
                        if (component)
                        {
                            if (component.GetType().Namespace == "UnityEngine.Rendering.Universal")
                                report.findings.Add("Serialized URP component: " + path + " | " + Hierarchy(transform) + " | " + component.GetType().Name);
                            InspectSerialized(component, path, Hierarchy(transform));
                        }
                        else report.findings.Add("Missing MonoBehaviour: " + path + " | " + Hierarchy(transform));
                }
            }

            internal void ResolveReferencedPrefabsAndData()
            {
                // Do not independently activate every nested vendor base prefab from GetDependencies:
                // load only explicit roots and prefabs referenced by gameplay fields/terrain prototypes.
                while (prefabs.Count > 0 || dependencies.Any(p => IsData(p) && !scannedData.Contains(p)))
                {
                    foreach (string path in dependencies.Where(IsData).Where(p => !scannedData.Contains(p)).ToArray())
                    {
                        scannedData.Add(path);
                        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                            if (obj is ScriptableObject) InspectSerialized(obj, path, obj.name);
                    }
                    while (prefabs.Count > 0)
                    {
                        string path = prefabs.Dequeue(); if (!scannedPrefabs.Add(path)) continue;
                        if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                        {
                            var root = PrefabUtility.LoadPrefabContents(path);
                            try { InspectRoots(new[] { root }, path); }
                            finally { PrefabUtility.UnloadPrefabContents(root); }
                        }
                        else
                        {
                            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                            if (!model) throw new InvalidOperationException("Referenced model cannot be loaded: " + path);
                            InspectRoots(new[] { model }, path);
                        }
                    }
                }
            }

            static bool IsData(string path) => path.StartsWith("Assets/LastSignal/", StringComparison.Ordinal) && path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase);

            internal void Finish()
            {
                // Enumerate ALL material subassets, but keep unused import defaults separate from effective uses.
                foreach (string path in dependencies.OrderBy(p => p))
                {
                    string extension = Path.GetExtension(path).ToLowerInvariant();
                    if (!new[] { ".mat", ".fbx", ".obj", ".blend", ".asset" }.Contains(extension)) continue;
                    foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                    {
                        if (obj is Material material) Record(material);
                        if (obj is Mesh mesh && path.EndsWith("MedicinePack_JimmyPV.fbx", StringComparison.Ordinal))
                        {
                            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string guid, out long id);
                            var record = new MeshRecord { path = path, guid = guid, localId = id, identity = guid + ":" + id,
                                name = mesh.name, vertices = mesh.vertexCount, submeshes = mesh.subMeshCount,
                                boundsCenter = mesh.bounds.center, boundsSize = mesh.bounds.size, isReadable = mesh.isReadable,
                                normals = mesh.HasVertexAttribute(VertexAttribute.Normal), uv0 = mesh.HasVertexAttribute(VertexAttribute.TexCoord0) };
                            for (int i = 0; i < mesh.subMeshCount; i++) { record.indexCount += mesh.GetIndexCount(i); record.topology.Add(mesh.GetTopology(i).ToString()); }
                            report.medicineMeshes.Add(record);
                        }
                    }
                }
                foreach (var record in records.Values)
                {
                    if (record.uses.Count == 0) record.classification = "INACTIVE_DEFERRED";
                    else if (record.effectiveSlots == 0) record.classification = "SOURCE_UNVERIFIED";
                    else if (!record.hdrpSubshader || !record.shaderSupported || record.shaderError) record.classification = "ACTIVE_UNSUPPORTED";
                    else if (record.issues.Count > 0) record.classification = "ACTIVE_NEEDS_REPAIR";
                    else if (!record.path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase) && !record.path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) record.classification = "SOURCE_UNVERIFIED";
                    else record.classification = record.shader != null && record.shader.StartsWith("HDRP/", StringComparison.Ordinal) ?
                        "ACTIVE_HDRP_COMPATIBLE" : "ACTIVE_CUSTOM_COMPATIBLE";
                }
                foreach (var pair in hashes)
                    if (HdrpMaterialIntegrityUtility.Hash(pair.Key) != pair.Value) report.errors.Add("Protected dependency changed during audit: " + pair.Key);
                report.materials = records.Values.OrderBy(r => r.identity, StringComparer.Ordinal).ToList();
                report.classificationCounts = report.materials.GroupBy(r => r.classification).OrderBy(g => g.Key)
                    .Select(g => new Count { key = g.Key, count = g.Count() }).ToList();
                report.shaderFamilies = report.materials.GroupBy(r => r.shader ?? "MISSING").OrderBy(g => g.Key)
                    .Select(g => new Count { key = g.Key, count = g.Count() }).ToList();
                report.materialObjects = records.Count; report.sourceOnlyObjects = records.Values.Count(r => r.uses.Count == 0);
                report.rootCount = report.scannedRoots.Count;
            }
        }

        [MenuItem("Last Signal/Rendering/HDRP Integrity/4. Audit Active Material Coverage (Read Only)")]
        public static void Run()
        {
            RunScope(false);
        }

        [MenuItem("Last Signal/Rendering/HDRP Production/Audit Production and Forest (Read Only)")]
        public static void RunProduction()
        {
            RunScope(true);
        }

        /// <summary>HDRP 17.5 LitAPI normal-map contract. Does not mutate material keywords.</summary>
        public static string[] ValidateNormalMapKeywords(Material material)
        {
            if (!material || !material.HasProperty("_NormalMapSpace")) return new[] { "Missing normal map space property" };
            var issues = new List<string>();
            bool tangent = material.GetFloat("_NormalMapSpace") == 0f;
            string property = tangent ? "_NormalMap" : "_NormalMapOS";
            var normal = material.HasProperty(property) ? material.GetTexture(property) : null;
            // HDRP permits a detail map to supply tangent-space normals with no base normal map.
            bool detail = tangent && material.HasProperty("_DetailMap") && material.GetTexture("_DetailMap");
            bool expected = normal || detail;
            if (material.IsKeywordEnabled("_NORMALMAP") != expected)
                issues.Add(expected ? "Assigned normal/detail map with disabled _NORMALMAP" : "Enabled _NORMALMAP without normal/detail texture");
            if (material.IsKeywordEnabled("_NORMALMAP_TANGENT_SPACE") != tangent)
                issues.Add("Normal map space keyword disagrees with _NormalMapSpace");
            if (tangent && normal)
            {
                var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normal)) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.NormalMap)
                    issues.Add("Tangent normal texture is not imported as NormalMap: " + AssetDatabase.GetAssetPath(normal));
            }
            return issues.ToArray();
        }

        static void RunScope(bool production)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before authored coverage audit.");
            var scope = JsonUtility.FromJson<Scope>(File.ReadAllText(ScopePath));
            if (scope == null || scope.schemaVersion != 1 || scope.scenes == null || scope.prefabs == null || scope.dataAssets == null)
                throw new InvalidOperationException("Invalid material coverage scope.");
            string reportPath = ReportPath;
            if (production)
            {
                scope.scenes = scope.scenes.Concat(new[] {
                    "Assets/LastSignal/Scenes/Production/FirstTownCore.unity",
                    "Assets/LastSignal/Scenes/Validation/ForestPrototype120.unity"
                }).Concat(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path))
                    .Distinct().OrderBy(p => p, StringComparer.Ordinal).ToArray();
                reportPath = "Docs/Implementation/HDRPProductionOptimization/Evidence/production_material_coverage.json";
            }
            var audit = new Audit();
            var report = audit.report;
            report.timestampUtc = DateTime.UtcNow.ToString("O");
            using (var sha = System.Security.Cryptography.SHA256.Create())
                report.scopeHash = BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(scope))))
                    .Replace("-", "").ToLowerInvariant();
            report.unityVersion = Application.unityVersion;
            report.activePipeline = GraphicsSettings.currentRenderPipeline ? GraphicsSettings.currentRenderPipeline.GetType().FullName : "BuiltIn";
            report.limitations.Add(scope.runtimeLimit);
            report.limitations.Add("ACTIVE means authored renderer/terrain use in the declared roots, including disabled and potentially spawned objects; not proof of live gameplay visibility.");
            report.limitations.Add("COMPATIBLE means shader support and inspected property/importer checks only, not original channel equivalence or visual acceptance.");
            report.limitations.Add("INACTIVE_DEFERRED means no effective or serialized-field use found in this scope, not globally unused.");
            report.limitations.Add("Serialized runtime inputs include lookup-only palette sources; these remain SOURCE_UNVERIFIED until their behavior is traced.");
            report.limitations.Add("Terrain detail textures, dynamic shader variants, property blocks, source-model geometry holes and Windows platform imports require focused follow-up.");
            try
            {
                if (!(GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset)) throw new InvalidOperationException("Expected active HDRP configuration.");
                if (production)
                {
                    report.defaultPipeline = AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline);
                    if (!(GraphicsSettings.defaultRenderPipeline is HDRenderPipelineAsset))
                        report.errors.Add("Graphics default pipeline is not HDRP.");
                    var global = GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>();
                    report.hdrpGlobalSettings = AssetDatabase.GetAssetPath(global);
                    if (!global) report.errors.Add("HDRP Global Settings are missing.");
                    for (int i = 0; i < QualitySettings.names.Length; i++)
                    {
                        var asset = QualitySettings.GetRenderPipelineAssetAt(i);
                        var effective = asset ? asset : GraphicsSettings.defaultRenderPipeline;
                        report.qualityPipelines.Add(QualitySettings.names[i] + "=" + AssetDatabase.GetAssetPath(effective));
                        if (!(effective is HDRenderPipelineAsset)) report.errors.Add("Quality level is not HDRP: " + QualitySettings.names[i]);
                    }
                }
                foreach (string path in scope.scenes.Concat(scope.prefabs).Concat(scope.dataAssets)) audit.AddDependencies(path);
                foreach (string path in scope.prefabs) audit.QueueGameObjectAsset(path);
                foreach (string path in scope.scenes)
                {
                    var scene = EditorSceneManager.OpenPreviewScene(path);
                    try { audit.InspectRoots(scene.GetRootGameObjects(), path); }
                    finally { EditorSceneManager.ClosePreviewScene(scene); }
                }
                audit.ResolveReferencedPrefabsAndData(); audit.Finish();
                report.status = report.errors.Count > 0 ? "AUDIT_FAILED" : "AUDIT_COMPLETE_REVIEW_REQUIRED";
            }
            catch (Exception ex) { report.errors.Add(ex.ToString()); report.status = "AUDIT_FAILED"; }
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
            if (report.errors.Count > 0) throw new InvalidOperationException("Material coverage audit failed. See " + reportPath);
            Debug.Log($"Authored material audit complete: {report.materialObjects} objects, {report.resolvedRendererSlots} slots, {report.missingSlots.Count} missing slots. Review required: {reportPath}");
        }
    }
}
