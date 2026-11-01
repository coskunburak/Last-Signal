using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LastSignal.Gathering;
using LastSignal.Loot;
using LastSignal.Persistence;
using LastSignal.Vehicles;
using LastSignal.WorldCells;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal.Editor.WorldReconstruction
{
    [Serializable] public sealed class WorldStageObject
    {
        public string localId, parentId, name, components, persistentId, lootId, harvestId;
        public Vector3 localPosition, localScale;
        public Quaternion localRotation;
        public bool active;
    }
    [Serializable] public sealed class WorldStageReceipt
    {
        public string status = "IN_PROGRESS", runId, sourceScene, sourceHash, planHash, stageScene, stageGuid, stageHash, generation;
        public string saveSlot, terrainGeneration = "NOT_RUN", gameplay = "NOT_RUN", performance = "NOT_RUN", navigation = "NOT_RUN";
        public string architectureApproval = "OWNER_APPROVED_2026-10-24", error;
        public Bounds residentBounds;
        public WorldAssetCopy[] assets;
        public WorldStageObject[] objects;
        public string[] createdPaths;
    }

    /// <summary>Explicit owner-run G2 only. No terrain regeneration, bake, tests or production promotion.</summary>
    public static class WorldStageAuthoring
    {
        public const string SaveSlot = "wld_s001_naturalworld_stage";
        public const string ApprovedPlanHash = "22423e0e3085a53a5f37f3ff2465411ee1493d8b43dedd581d471ff44c98a38a";
        public const string LatestReceipt = "Docs/Implementation/World/WLD-S001/G2_Latest.json";
        const string SourceReceipt = "Docs/Implementation/World/WLD-S001/SourceReceipt.json";
        public static readonly Bounds ResidentBounds = new Bounds(new Vector3(-250, 20, 0), new Vector3(1000, 120, 500));
        static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
        internal static string Id(UnityEngine.Object obj)
        {
            if (!obj) return "";
            var id = GlobalObjectId.GetGlobalObjectIdSlow(obj);
            return id.identifierType + ":" + id.targetObjectId + ":" + id.targetPrefabId;
        }
        internal static WorldStageObject[] Snapshot(Scene scene) => All<Transform>(scene).Select(t => new WorldStageObject {
            localId = Id(t.gameObject), parentId = t.parent ? Id(t.parent.gameObject) : "", name = t.name,
            localPosition = t.localPosition, localRotation = t.localRotation, localScale = t.localScale, active = t.gameObject.activeSelf,
            components = string.Join("|", t.GetComponents<Component>().Select(c => c ? c.GetType().FullName : "MISSING")),
            persistentId = t.GetComponent<PersistentEntityId>()?.Id, lootId = t.GetComponent<LootSpawnPoint>()?.StableId,
            harvestId = t.GetComponent<ResourceNodeView>()?.nodeId
        }).OrderBy(o => o.localId, StringComparer.Ordinal).ToArray();

        internal static void Guard(string path)
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                PrefabStageUtility.GetCurrentPrefabStage() != null || SceneManager.sceneCount != 1 || scene.path != path || scene.isDirty)
                throw new InvalidOperationException("Open only the saved expected scene outside Play/prefab/import: " + path + ". No unsaved work is discarded.");
        }
        internal static void VerifySourceReceipt()
        {
            // Read the fixed path/hash map without introducing an additional JSON package dependency.
            var matches = Regex.Matches(File.ReadAllText(SourceReceipt), "\"((?:Assets|Packages|ProjectSettings)/[^\"]+)\"\\s*:\\s*\"([0-9a-f]{64})\"");
            if (matches.Count != 23) throw new InvalidDataException("G0 source receipt scope changed; reconcile before G2.");
            foreach (Match match in matches)
                if (!File.Exists(match.Groups[1].Value) || WorldReconstructionAudit.Hash(match.Groups[1].Value) != match.Groups[2].Value)
                    throw new InvalidDataException("G0 source changed: " + match.Groups[1].Value);
        }
        internal static void Folder(string path, List<string> created)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            if (Directory.Exists(path) || File.Exists(path + ".meta")) throw new IOException("Unimported output folder already exists: " + path);
            string parent = Path.GetDirectoryName(path).Replace('\\', '/'); Folder(parent, created);
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(path)))) throw new IOException("Cannot create folder: " + path);
            created.Add(path);
        }

        [MenuItem("Last Signal/World Reconstruction/WLD-S001/3 Create isolated G2 candidate")]
        public static void CreateCandidate()
        {
            var p = WorldReconstructionProfile.Load(); Guard(p.sourceScene); VerifySourceReceipt();
            if (WorldReconstructionAudit.Hash(WorldReconstructionProfile.PlanPath) != ApprovedPlanHash)
                throw new InvalidDataException("Plan differs from the owner-approved western expansion contract.");
            if (File.Exists(p.proposedStage) || File.Exists(p.proposedStage + ".meta") || File.Exists(LatestReceipt))
                throw new IOException("Candidate/receipt already exists. Use Validate saved G2 candidate; no overwrite or regeneration.");
            var survey = WorldReconstructionAudit.Inspect(p);
            if (survey.report.blockers.Any(b => !b.StartsWith("EXPANSION_OWNERSHIP:", StringComparison.Ordinal) &&
                !b.StartsWith("HARVEST_BASELINE:", StringComparison.Ordinal) && !b.StartsWith("NAVIGATION:", StringComparison.Ordinal)))
                throw new InvalidOperationException("Unexpected G0 blocker; candidate creation stopped.");
            var source = SceneManager.GetActiveScene();
            ValidateAuthority(source, false);
            var receipt = new WorldStageReceipt { runId = "G" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0,8),
                sourceScene = p.sourceScene, sourceHash = p.sourceSceneSha256, planHash = WorldReconstructionAudit.Hash(WorldReconstructionProfile.PlanPath),
                stageScene = p.proposedStage, residentBounds = ResidentBounds, saveSlot = SaveSlot, objects = Snapshot(source) };
            receipt.generation = p.generatedAssets + "/" + receipt.runId;
            var isolation = new WorldStageIsolation(p.sourceScene, receipt.generation);
            receipt.assets = isolation.copies.ToArray();
            var created = new List<string>();
            string evidence = "Docs/Implementation/World/WLD-S001/Evidence/" + receipt.runId + ".json";
            Directory.CreateDirectory(Path.GetDirectoryName(evidence));
            void Journal() { receipt.createdPaths = created.ToArray(); File.WriteAllText(evidence, JsonUtility.ToJson(receipt, true)); }
            Journal();
            try
            {
                Folder(receipt.generation, created); Folder(Path.GetDirectoryName(p.proposedStage).Replace('\\','/'), created);
                Journal(); isolation.CopyAll(created, Journal); Journal();
                created.Add(p.proposedStage);
                Journal();
                if (!AssetDatabase.CopyAsset(p.sourceScene, p.proposedStage)) throw new IOException("Scene copy failed.");
                isolation.RemapText(p.proposedStage);
                receipt.stageGuid = AssetDatabase.AssetPathToGUID(p.proposedStage);
                if (receipt.stageGuid == p.sourceSceneGuid || string.IsNullOrEmpty(receipt.stageGuid)) throw new IOException("Stage GUID is not isolated.");
                Journal();
                var stage = EditorSceneManager.OpenScene(p.proposedStage, OpenSceneMode.Single);
                var vehicle = new SerializedObject(All<VehicleWorld>(stage).Single());
                vehicle.FindProperty("residentBounds").boundsValue = ResidentBounds; vehicle.ApplyModifiedPropertiesWithoutUndo();
                var save = new SerializedObject(All<SaveSession>(stage).Single());
                save.FindProperty("saveSlot").stringValue = SaveSlot; save.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(stage);
                if (!EditorSceneManager.SaveScene(stage, p.proposedStage)) throw new IOException("Candidate save failed.");
                // Reopen our own saved candidate to verify persisted references and settings, not just in-memory edits.
                stage = EditorSceneManager.OpenScene(p.proposedStage, OpenSceneMode.Single);
                ValidateCandidate(stage, receipt, p);
                isolation.VerifyClosure(p.proposedStage); VerifySourceReceipt();
                receipt.stageHash = WorldReconstructionAudit.Hash(p.proposedStage);
                receipt.status = "IMPLEMENTED_OWNER_ACCEPTANCE_PENDING"; Journal();
                File.WriteAllText(LatestReceipt, JsonUtility.ToJson(receipt, true));
                Debug.Log("WLD G2 candidate saved and structurally verified. Terrain unchanged; G3/navigation/gameplay/performance NOT_RUN. Receipt: " + LatestReceipt);
            }
            catch (Exception failure)
            {
                receipt.status = "FAILED"; receipt.error = failure.ToString();
                // Only the scene created by this invocation can be discarded. No pre-existing stage is ever opened here.
                try
                {
                    if (SceneManager.GetActiveScene().path == p.proposedStage) EditorSceneManager.OpenScene(p.sourceScene, OpenSceneMode.Single);
                    // Defer imports until the whole owned set is removed. Deleting a parent prefab must not
                    // trigger import of its about-to-be-deleted variants midway through rollback.
                    AssetDatabase.StartAssetEditing();
                    try
                    {
                        foreach (string path in created.AsEnumerable().Reverse())
                        {
                            if (Directory.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any()) continue;
                            if ((File.Exists(path) || Directory.Exists(path)) && !AssetDatabase.DeleteAsset(path))
                                receipt.error += "\nRollback retained: " + path;
                        }
                    }
                    finally { AssetDatabase.StopAssetEditing(); }
                }
                catch (Exception rollback) { receipt.error += "\nRollback stopped; retain outputs for inspection: " + rollback; }
                Journal(); throw;
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        internal static void ValidateAuthority(Scene scene, bool candidate)
        {
            if (All<SessionFlow>(scene).Length != 1 || All<SaveSession>(scene).Length != 1 || All<VehicleWorld>(scene).Length != 1 || All<WorldCellManager>(scene).Length != 1)
                throw new InvalidOperationException("Expected exactly one session/save/vehicle/cell authority.");
            if (new SerializedObject(All<WorldCellManager>(scene).Single()).FindProperty("definitions").arraySize != 0)
                throw new InvalidOperationException("Resident-only contract changed: non-empty cell definitions.");
            var bounds = new SerializedObject(All<VehicleWorld>(scene).Single()).FindProperty("residentBounds").boundsValue;
            var expected = candidate ? ResidentBounds : new Bounds(new Vector3(0,10,0), new Vector3(500,100,500));
            if (bounds != expected || All<SaveSession>(scene).Single().SaveSlot != (candidate ? SaveSlot : "scn_s001_main_hdrp_review"))
                throw new InvalidOperationException("Unexpected resident bounds or save slot.");
            if (All<Transform>(scene).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)) != 0)
                throw new InvalidOperationException("Missing scripts in candidate/source.");
        }

        static void ValidateCandidate(Scene scene, WorldStageReceipt receipt, WorldReconstructionProfile p)
        {
            ValidateAuthority(scene, true);
            var current = Snapshot(scene);
            if (current.Length != receipt.objects.Length || current.Select(o => o.localId).Distinct().Count() != current.Length)
                throw new InvalidOperationException("Object count or unique local IDs changed.");
            for (int i = 0; i < current.Length; i++)
                if (JsonUtility.ToJson(current[i]) != JsonUtility.ToJson(receipt.objects[i]))
                    throw new InvalidOperationException("Protected hierarchy/transform/identity changed: " + receipt.objects[i].name);
            var terrain = All<Terrain>(scene).Single(); var data = terrain.terrainData;
            var original = AssetDatabase.LoadAssetAtPath<TerrainData>(AssetDatabase.GUIDToAssetPath(p.sourceTerrainGuid));
            if (!data || data == original || !AssetDatabase.GetAssetPath(data).StartsWith(receipt.generation + "/", StringComparison.Ordinal) ||
                data.size != p.sourceSize || data.heightmapResolution != p.heightmapResolution || terrain.transform.position != p.sourceOrigin ||
                terrain.GetComponent<TerrainCollider>()?.terrainData != data || data.treeInstanceCount != original.treeInstanceCount || data.detailResolution != original.detailResolution)
                throw new InvalidOperationException("Candidate terrain/collider isolation or geometry mismatch.");
            var before = original.GetHeights(0,0,p.heightmapResolution,p.heightmapResolution);
            var after = data.GetHeights(0,0,p.heightmapResolution,p.heightmapResolution);
            for (int z=0; z<p.heightmapResolution; z++) for (int x=0; x<p.heightmapResolution; x++)
                if (before[z,x] != after[z,x]) throw new InvalidOperationException("G2 unexpectedly changed a terrain height.");
            WorldStageIsolation.VerifyNoSourceDependencies(scene.path, receipt.assets);
        }

        [MenuItem("Last Signal/World Reconstruction/WLD-S001/4 Validate saved G2 candidate")]
        public static void ValidateSavedCandidate()
        {
            if (!File.Exists(LatestReceipt))
                throw new InvalidOperationException("No successful G2 candidate receipt. If Create failed, inspect its first error and Evidence journal; do not run Validate yet. After the fix, run 3 Create isolated G2 candidate from saved Main.");
            var p = WorldReconstructionProfile.Load(); Guard(p.proposedStage); VerifySourceReceipt();
            var receipt = JsonUtility.FromJson<WorldStageReceipt>(File.ReadAllText(LatestReceipt));
            if (receipt.status != "IMPLEMENTED_OWNER_ACCEPTANCE_PENDING" || receipt.stageScene != p.proposedStage ||
                receipt.stageGuid != AssetDatabase.AssetPathToGUID(p.proposedStage) || receipt.stageHash != WorldReconstructionAudit.Hash(p.proposedStage) ||
                receipt.planHash != WorldReconstructionAudit.Hash(WorldReconstructionProfile.PlanPath))
                throw new InvalidOperationException("Candidate/plan differs from creation receipt. Preserve and reconcile edits; no automatic overwrite.");
            foreach (var a in receipt.assets)
                if (WorldReconstructionAudit.Hash(a.source) != a.sourceHash || WorldReconstructionAudit.Hash(a.source + ".meta") != a.sourceMetaHash ||
                    WorldReconstructionAudit.Hash(a.destination) != a.destinationHash || AssetDatabase.AssetPathToGUID(a.destination) != a.destinationGuid ||
                    WorldReconstructionAudit.Hash(a.destination + ".meta") != a.destinationMetaHash ||
                    AssetDatabase.LoadAllAssetsAtPath(a.destination).Any(o => o && EditorUtility.IsDirty(o)))
                    throw new InvalidOperationException("Asset differs from isolation receipt: " + a.destination);
            ValidateCandidate(SceneManager.GetActiveScene(), receipt, p);
            Debug.Log("WLD G2 SAVED_CANDIDATE_VERIFIED. Objects=" + receipt.objects.Length + ", isolated assets=" + receipt.assets.Length +
                ". Bounds center(-250,20,0) size(1000,120,500); slot=" + SaveSlot + ". G3/gameplay/navigation/performance NOT_RUN.");
        }
    }
}
