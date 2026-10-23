using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LastSignal.Forest;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace LastSignal.Editor.FirstTownForest
{
    /// <summary>Explicit read-only scene inspection. No scene opening, generation, saving or tests.</summary>
    public static class ForestPerformanceAudit
    {
        const string ScenePath = "Assets/LastSignal/Scenes/Validation/ForestPrototype120.unity";
        [Serializable] sealed class Count { public string type; public int total, active; }
        [Serializable] sealed class MeshRow
        {
            public string asset, guid, localId, name;
            public int vertices, submeshes, rendererSlots, activeRendererSlots;
            public long triangles, allLodPotentialTriangles;
        }
        [Serializable] sealed class LodRow
        {
            public string id, path, prefab, fadeMode;
            public bool enabled, animated;
            public float[] transitions;
            public long[] triangles;
            public int[] rendererSlots, materialSlots;
        }
        [Serializable] sealed class MaterialRow
        {
            public string id, name, shader, keywords;
            public bool instancing;
            public int rendererSlots, terrainReferences;
        }
        [Serializable] sealed class TerrainRow
        {
            public string id, data;
            public int trees, detailPrototypes, detailCellsSum, heightmapResolution;
            public float detailDistance, detailDensity, pixelError, basemapDistance;
            public bool instanced;
        }
        [Serializable] sealed class VolumeRow { public string id, profile, serializedProfile; }
        [Serializable] sealed class Report
        {
            public string utc, scene, generation, digest, unity, api, gpu, cpu, os, pipeline, quality;
            public string status = "READ_ONLY_INVENTORY_NOT_A_BENCHMARK";
            public string geometryScope = "Scene renderer slots across all assigned LOD levels, including inactive levels. Excludes Terrain geometry and terrain tree/detail prototype geometry. Potential geometry is NOT rendered triangles per frame.";
            public string variantScope = "Material shader+keyword signatures, NOT compiled or executed GPU variants.";
            public string visibleRenderers = "UNAVAILABLE: use Rendering Profiler/Frame Debugger; Renderer.isVisible includes other cameras and shadow views.";
            public bool sceneDirtyBefore, sceneDirtyAfter, srpBatcher;
            public int loadedScenes, uniqueMaterialKeywordSignatures, shadowCasterSlots, colliders, meshColliders;
            public int missingScripts, objects, activeObjects;
            public Count[] components;
            public MeshRow[] meshes;
            public LodRow[] lods;
            public MaterialRow[] materials;
            public TerrainRow[] terrains;
            public VolumeRow[] volumes;
            public string[] duplicateCandidates, warnings, cameraSettings;
        }
        static string Id(UnityEngine.Object obj) => obj ? GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString() : "null";
        static string Hierarchy(Transform t) => t.parent ? Hierarchy(t.parent) + "/" + t.name : t.name;
        static Mesh MeshOf(Renderer r) => r is SkinnedMeshRenderer skinned ? skinned.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
        static long Triangles(Mesh mesh)
        {
            long count = 0;
            if (mesh) for (int s = 0; s < mesh.subMeshCount; s++)
                if (mesh.GetTopology(s) == MeshTopology.Triangles) count += mesh.GetIndexCount(s) / 3;
            return count;
        }
        [MenuItem("Last Signal/Forest 120/Performance/Export read-only scene audit")]
        public static void Export()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Run the inventory outside Play Mode; use F9 for runtime measurements.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException("Open " + ScenePath + " yourself; this audit never opens or replaces scenes.");
            var report = new Report { utc = DateTime.UtcNow.ToString("O"), scene = scene.path,
                sceneDirtyBefore = scene.isDirty, loadedScenes = SceneManager.sceneCount,
                unity = Application.unityVersion, api = SystemInfo.graphicsDeviceType.ToString(),
                cpu = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName, os = SystemInfo.operatingSystem,
                pipeline = AssetDatabase.GetAssetPath(GraphicsSettings.currentRenderPipeline),
                quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                srpBatcher = GraphicsSettings.useScriptableRenderPipelineBatching };
            var transforms = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            var components = transforms.SelectMany(t => t.GetComponents<Component>()).Where(c => c).ToArray();
            var states = components.OfType<ForestPrototypeState>().ToArray();
            var warnings = new List<string>();
            if (states.Length != 1) warnings.Add("Expected one ForestPrototypeState; found " + states.Length);
            if (states.Length == 1) { report.generation = states[0].generationFolder; report.digest = states[0].placementDigest; }
            report.objects = transforms.Length; report.activeObjects = transforms.Count(t => t.gameObject.activeInHierarchy);
            report.missingScripts = transforms.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            report.components = components.GroupBy(c => c.GetType().FullName).OrderBy(g => g.Key)
                .Select(g => new Count { type = g.Key, total = g.Count(), active = g.Count(c => c.gameObject.activeInHierarchy &&
                    (!(c is Behaviour b) || b.enabled) && (!(c is Renderer r) || r.enabled) && (!(c is Collider col) || col.enabled)) }).ToArray();
            report.colliders = components.OfType<Collider>().Count(); report.meshColliders = components.OfType<MeshCollider>().Count();
            var renderers = components.OfType<Renderer>().ToArray();
            report.shadowCasterSlots = renderers.Count(r => r.enabled && r.gameObject.activeInHierarchy && r.shadowCastingMode != ShadowCastingMode.Off);
            var meshes = new Dictionary<Mesh, MeshRow>(); var materials = new Dictionary<Material, MaterialRow>();
            void RegisterMaterial(Material material, bool terrainReference)
            {
                if (!material) return;
                if (!materials.TryGetValue(material, out var row))
                    materials.Add(material, row = new MaterialRow { id = Id(material), name = material.name,
                        shader = material.shader ? material.shader.name : "MISSING", instancing = material.enableInstancing,
                        keywords = string.Join(" ", material.shaderKeywords.OrderBy(k => k)) });
                if (terrainReference) row.terrainReferences++; else row.rendererSlots++;
            }
            foreach (var renderer in renderers)
            {
                var mesh = MeshOf(renderer);
                if (mesh)
                {
                    if (!meshes.TryGetValue(mesh, out var row))
                    {
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string guid, out long fileId);
                        meshes.Add(mesh, row = new MeshRow { asset = AssetDatabase.GetAssetPath(mesh), guid = guid,
                            localId = fileId.ToString(CultureInfo.InvariantCulture), name = mesh.name,
                            vertices = mesh.vertexCount, submeshes = mesh.subMeshCount, triangles = Triangles(mesh) });
                    }
                    row.rendererSlots++; if (renderer.enabled && renderer.gameObject.activeInHierarchy) row.activeRendererSlots++;
                    row.allLodPotentialTriangles += row.triangles;
                    if (row.triangles > 1000000) warnings.Add("Million-triangle referenced mesh: " + Id(renderer) + " " + row.name);
                }
                foreach (var material in renderer.sharedMaterials.Distinct())
                {
                    if (!material) { warnings.Add("Missing material: " + Id(renderer)); continue; }
                    RegisterMaterial(material, false);
                }
            }
            foreach (var terrain in components.OfType<Terrain>())
            {
                RegisterMaterial(terrain.materialTemplate, true);
                if (!terrain.terrainData) continue;
                foreach (var detail in terrain.terrainData.detailPrototypes)
                    if (detail.prototype) foreach (var renderer in detail.prototype.GetComponentsInChildren<Renderer>(true))
                        foreach (var material in renderer.sharedMaterials.Distinct()) RegisterMaterial(material, true);
                foreach (var tree in terrain.terrainData.treePrototypes)
                    if (tree.prefab) foreach (var renderer in tree.prefab.GetComponentsInChildren<Renderer>(true))
                        foreach (var material in renderer.sharedMaterials.Distinct()) RegisterMaterial(material, true);
            }
            report.meshes = meshes.Values.OrderByDescending(m => m.allLodPotentialTriangles).ToArray();
            report.materials = materials.Values.OrderByDescending(m => m.rendererSlots).ToArray();
            report.uniqueMaterialKeywordSignatures = materials.Values.Select(m => m.shader + "|" + m.keywords).Distinct().Count();
            report.lods = components.OfType<LODGroup>().Select(group =>
            {
                var levels = group.GetLODs();
                float previous = float.PositiveInfinity;
                foreach (var level in levels)
                {
                    if (level.screenRelativeTransitionHeight >= previous || level.renderers.Length == 0 || level.renderers.Any(r => !r))
                        warnings.Add("LOD health requires review: " + Id(group));
                    previous = level.screenRelativeTransitionHeight;
                }
                if (levels.Length == 1) warnings.Add("Single-level cull group (no cheaper geometry): " + Id(group));
                return new LodRow { id = Id(group), path = Hierarchy(group.transform),
                    prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(group.gameObject), enabled = group.enabled,
                    fadeMode = group.fadeMode.ToString(), animated = group.animateCrossFading,
                    transitions = levels.Select(l => l.screenRelativeTransitionHeight).ToArray(),
                    triangles = levels.Select(l => l.renderers.Where(r => r).Sum(r => Triangles(MeshOf(r)))).ToArray(),
                    rendererSlots = levels.Select(l => l.renderers.Length).ToArray(),
                    materialSlots = levels.Select(l => l.renderers.Where(r => r).Sum(r => r.sharedMaterials.Length)).ToArray() };
            }).ToArray();
            report.terrains = components.OfType<Terrain>().Select(t =>
            {
                var data = t.terrainData;
                int details = 0;
                if (data) for (int layer = 0; layer < data.detailPrototypes.Length; layer++)
                    foreach (int n in data.GetDetailLayer(0, 0, data.detailWidth, data.detailHeight, layer)) details += n;
                return new TerrainRow { id = Id(t), data = AssetDatabase.GetAssetPath(data), trees = data ? data.treeInstanceCount : 0,
                    detailPrototypes = data ? data.detailPrototypes.Length : 0, detailCellsSum = details,
                    heightmapResolution = data ? data.heightmapResolution : 0, detailDistance = t.detailObjectDistance,
                    detailDensity = t.detailObjectDensity, pixelError = t.heightmapPixelError,
                    basemapDistance = t.basemapDistance, instanced = t.drawInstanced };
            }).ToArray();
            report.volumes = components.OfType<Volume>().Select(v => new VolumeRow { id = Id(v),
                profile = AssetDatabase.GetAssetPath(v.sharedProfile), serializedProfile = v.sharedProfile ?
                    string.Join("\n", v.sharedProfile.components.Where(c => c).Select(c => c.GetType().Name + ": " + JsonUtility.ToJson(c))) : "null" }).ToArray();
            report.cameraSettings = components.OfType<Camera>().Select(c =>
            {
                var hd = c.GetComponent<HDAdditionalCameraData>();
                return Id(c) + " " + c.name + " pixels=" + c.pixelWidth + "x" + c.pixelHeight +
                    " scaled=" + c.scaledPixelWidth + "x" + c.scaledPixelHeight + " FOV=" + c.fieldOfView +
                    " clips=" + c.nearClipPlane + "/" + c.farClipPlane + " HDRP=" + (hd ? JsonUtility.ToJson(hd) : "null");
            }).ToArray();
            report.duplicateCandidates = transforms.Where(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                .GroupBy(t => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) + "|" + Id(t.parent) + "|" + t.localPosition.ToString("R"))
                .Where(g => g.Count() > 1).Select(g => g.Key + " IDs=" + string.Join(",", g.Select(t => Id(t.gameObject)))).ToArray();
            report.warnings = warnings.ToArray(); report.sceneDirtyAfter = scene.isDirty;
            string output = "Docs/Implementation/Performance/Evidence/native-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ") + "-" + Guid.NewGuid().ToString("N") + ".json";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            using (var file = new StreamWriter(new FileStream(output, FileMode.CreateNew))) file.WriteLine(JsonUtility.ToJson(report, true));
            Debug.Log("Read-only forest audit: " + Path.GetFullPath(output) + ". No scene/assets saved; no performance test run.");
        }
    }
}
