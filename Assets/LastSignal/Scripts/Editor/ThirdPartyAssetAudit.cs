using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Editor.AssetMaintenance
{
    // Owner-triggered only. No InitializeOnLoad, tests, scene saves or material conversion.
    public static class ThirdPartyAssetAudit
    {
        const string Source = "Assets/ThirdParty/UI/kenney_input-prompts_1.5";
        const string Destination = "Assets/ThirdParty/10_Pipeline_Agnostic/UI/kenney_input-prompts_1.5";
        const string Evidence = "Docs/Implementation/Assets/Evidence";
        [Serializable] sealed class Entry { public string path, sha256, guid; }
        [Serializable] sealed class Dependency { public string path; public string[] dependencies; }
        [Serializable] sealed class ForestPrefab
        {
            public string path; public long trianglesAllMeshes; public int renderers, materialSlots, lodGroups, colliders;
            public string[] shaderNames, shadows; public float[] lodHeights;
        }
        [Serializable] sealed class Receipt
        {
            public string status, source = Source, destination = Destination, timestampUtc, error;
            public Entry[] files;
            public Dependency[] scenes;
            public Dependency[] implicitRoots;
            public ForestPrefab[] forestPrefabs;
            public string[] createdFolders;
        }
        static string Hash(string path)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        static void Idle()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Exit Play mode and wait for compilation/import to finish.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save or discard scene edits yourself before audit/move.");
        }
        static Entry[] Snapshot(string root)
        {
            var paths = Directory.GetFiles(root, "*", SearchOption.AllDirectories).Concat(new[] { root + ".meta" });
            return paths.Where(p => Path.GetFileName(p) != ".DS_Store").Select(p => p.Replace('\\', '/'))
                .OrderBy(p => p, StringComparer.Ordinal).Select(p => new Entry { path = p, sha256 = Hash(p),
                    guid = p.EndsWith(".meta", StringComparison.Ordinal) ? AssetDatabase.AssetPathToGUID(p.Substring(0, p.Length - 5)) : "" }).ToArray();
        }
        static string[] AssetPaths() => AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/", StringComparison.Ordinal)).ToArray();
        static Dependency[] SceneDependencies() => AssetPaths().Where(p => p.EndsWith(".unity", StringComparison.Ordinal) && !p.StartsWith("Assets/ThirdParty/", StringComparison.Ordinal))
            .Select(p => new Dependency { path = p, dependencies = AssetDatabase.GetDependencies(p, true).Select(AssetDatabase.AssetPathToGUID).Where(g => !string.IsNullOrEmpty(g)).OrderBy(g => g, StringComparer.Ordinal).ToArray() }).ToArray();
        static void CheckPairingAndGuids()
        {
            var guids = new HashSet<string>(StringComparer.Ordinal);
            foreach (string p in Directory.EnumerateFileSystemEntries("Assets/ThirdParty", "*", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(p).StartsWith(".", StringComparison.Ordinal)) continue;
                if (p.EndsWith(".meta", StringComparison.Ordinal))
                {
                    string asset = p.Substring(0, p.Length - 5).Replace('\\', '/');
                    if (!File.Exists(asset) && !Directory.Exists(asset)) throw new InvalidOperationException("Orphan meta: " + p);
                    var match = System.Text.RegularExpressions.Regex.Match(File.ReadAllText(p), @"(?m)^guid: ([a-f0-9]{32})\r?$");
                    if (!match.Success || !guids.Add(match.Groups[1].Value)) throw new InvalidOperationException("Invalid/duplicate GUID: " + p);
                    if (AssetDatabase.AssetPathToGUID(asset) != match.Groups[1].Value) throw new InvalidOperationException("Database/meta mismatch: " + asset);
                }
                else if (!File.Exists(p + ".meta")) throw new InvalidOperationException("Missing meta: " + p);
            }
        }
        static void Preflight()
        {
            Idle(); CheckPairingAndGuids();
            if (!AssetDatabase.IsValidFolder(Source)) throw new InvalidOperationException("Expected source absent; do not rerun after a move.");
            if (File.Exists(Destination + ".meta") || Directory.Exists(Destination)) throw new InvalidOperationException("Destination exists.");
            string[] paths = AssetPaths();
            if (paths.Any(p => string.Equals(p, Destination, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Case collision.");
            foreach (string p in paths.Where(p => !p.StartsWith(Source + "/", StringComparison.Ordinal) && p != Source && !AssetDatabase.IsValidFolder(p)))
                if (AssetDatabase.GetDependencies(p, false).Any(d => d == Source || d.StartsWith(Source + "/", StringComparison.Ordinal)))
                    throw new InvalidOperationException("New dependency; batch requires review: " + p);
            foreach (string p in Directory.GetFiles(Source, "*", SearchOption.AllDirectories))
            {
                string ext = Path.GetExtension(p).ToLowerInvariant();
                if (!new[] { ".png", ".svg", ".otf", ".ttf", ".xml", ".html", ".url", ".txt", ".meta" }.Contains(ext) && Path.GetFileName(p) != ".DS_Store")
                    throw new InvalidOperationException("Unexpected package content: " + p);
            }
            foreach (string root in new[] { "Assets", "Tools", "ProjectSettings", "Packages" })
                foreach (string p in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    if (p.Replace('\\', '/').EndsWith("/ThirdPartyAssetAudit.cs", StringComparison.Ordinal) || p.Replace('\\', '/').StartsWith(Source + "/", StringComparison.Ordinal)) continue;
                    if (!new[] { ".cs", ".py", ".sh", ".json", ".asmdef", ".asmref", ".rsp" }.Contains(Path.GetExtension(p))) continue;
                    string text = File.ReadAllText(p);
                    if (text.Contains(Source) || text.Contains("kenney_input-prompts_1.5"))
                        throw new InvalidOperationException("Path/name reference requires review: " + p);
                }
        }
        [MenuItem("Tools/Last Signal/AST-S001/1 Dry Run Candidate UI (no writes)")]
        public static void DryRun()
        {
            Preflight();
            Debug.Log("AST-S001 DRY_RUN_PASS: " + Source + " -> " + Destination + ". No writes. Owner acceptance remains required; see AST-S001-Manual-Acceptance.md.");
        }
        [MenuItem("Tools/Last Signal/AST-S001/2 Export Read-Only Dependencies")]
        public static void Audit()
        {
            Idle();
            var report = new Receipt { status = "READ_ONLY_DEPENDENCIES_NOT_VISUAL_ACCEPTANCE", timestampUtc = DateTime.UtcNow.ToString("O"), scenes = SceneDependencies(),
                implicitRoots = AssetPaths().Where(p => p.Contains("/Resources/") && !AssetDatabase.IsValidFolder(p))
                    .Concat(PlayerSettings.GetPreloadedAssets().Where(x => x).Select(AssetDatabase.GetAssetPath)).Distinct()
                    .Select(p => new Dependency { path = p, dependencies = AssetDatabase.GetDependencies(p, true) }).ToArray(),
                forestPrefabs = AssetPaths().Where(p => p.EndsWith(".prefab", StringComparison.Ordinal) &&
                    (p.StartsWith("Assets/Realistic Tree/Prefabs/HDRP/", StringComparison.Ordinal) || p.StartsWith("Assets/LastSignal/Art/ForestPrototype120/", StringComparison.Ordinal)))
                    .Select(ForestInfo).ToArray() };
            string output = Evidence + "/editor-audit-" + Guid.NewGuid().ToString("N") + ".json";
            Directory.CreateDirectory(Evidence); File.WriteAllText(output, JsonUtility.ToJson(report, true));
            Debug.Log("AST-S001 dependency evidence: " + output);
        }
        static ForestPrefab ForestInfo(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) throw new InvalidOperationException("Missing prefab: " + path);
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            var groups = prefab.GetComponentsInChildren<LODGroup>(true);
            long triangles = 0;
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh)
                    for (int sub = 0; sub < filter.sharedMesh.subMeshCount; sub++) triangles += filter.sharedMesh.GetIndexCount(sub) / 3;
            return new ForestPrefab { path = path, trianglesAllMeshes = triangles, renderers = renderers.Length,
                materialSlots = renderers.Sum(r => r.sharedMaterials.Length), lodGroups = groups.Length,
                colliders = prefab.GetComponentsInChildren<Collider>(true).Length,
                shaderNames = renderers.SelectMany(r => r.sharedMaterials).Select(m => m && m.shader ? m.shader.name : "MISSING").Distinct().ToArray(),
                shadows = renderers.Select(r => r.name + ": " + r.shadowCastingMode).ToArray(),
                lodHeights = groups.SelectMany(g => g.GetLODs()).Select(l => l.screenRelativeTransitionHeight).ToArray() };
        }
        static void Verify(Entry[] entries, string root)
        {
            var current = Snapshot(root);
            if (current.Length != entries.Length) throw new InvalidOperationException("Package file count changed.");
            foreach (var item in entries)
            {
                string p = root + item.path.Substring(Source.Length);
                if (!File.Exists(p) || Hash(p) != item.sha256) throw new InvalidOperationException("Bytes changed: " + p);
                if (!string.IsNullOrEmpty(item.guid) && AssetDatabase.AssetPathToGUID(p.Substring(0, p.Length - 5)) != item.guid)
                    throw new InvalidOperationException("GUID changed: " + p);
            }
        }
        static void EnsureFolder(string path, List<string> created)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent, created);
            if (Directory.Exists(path) || File.Exists(path) || File.Exists(path + ".meta")) throw new InvalidOperationException("Folder collision: " + path);
            if (AssetPaths().Any(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Case collision: " + path);
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(path)))) throw new IOException("CreateFolder failed: " + path);
            created.Add(path);
        }
        [MenuItem("Tools/Last Signal/AST-S001/3 Move Candidate UI (blocked until dependencies reviewed)")]
        public static void MoveSmallBatch()
        {
            var plan = JsonUtility.FromJson<ThirdPartyMoveManifestAudit.Plan>(File.ReadAllText("Docs/Implementation/Assets/AST-S001-MovePlan.json"));
            var selectedItem = plan.items.SingleOrDefault(i => i.source == Source && i.destination == Destination);
            if (!plan.executionApproved || selectedItem == null || !selectedItem.approved || selectedItem.riskLevel != "LOW_RISK")
                throw new InvalidOperationException("No authorized LOW_RISK batch. Kenney remains protected pending dependency review.");
            Preflight();
            if (!EditorUtility.DisplayDialog("AST-S001 small batch", Source + "\n→ " + Destination + "\n\nOnly this package. A verified local backup is created first. Gameplay acceptance remains pending.", "Move", "Cancel")) return;
            Preflight();
            var entries = Snapshot(Source); var scenes = SceneDependencies();
            // Preserve every existing vendor GUID/path, not only the moved package.
            var beforeGuids = AssetPaths().Where(p => p.StartsWith("Assets/ThirdParty/", StringComparison.Ordinal)).ToDictionary(p => p, AssetDatabase.AssetPathToGUID);
            string checkpoint = Evidence + "/kenney-batch-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(checkpoint);
            var receipt = new Receipt { status = "BACKUP_IN_PROGRESS", timestampUtc = DateTime.UtcNow.ToString("O"), files = entries, scenes = scenes };
            var created = new List<string>(); bool moved = false;
            Action write = () => { receipt.createdFolders = created.ToArray(); File.WriteAllText(checkpoint + "/receipt.json", JsonUtility.ToJson(receipt, true)); };
            write();
            try
            {
                foreach (var item in entries)
                {
                    string target = checkpoint + "/backup/" + item.path;
                    Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(item.path, target, false);
                    if (Hash(target) != item.sha256) throw new IOException("Backup verification failed: " + item.path);
                }
                Verify(entries, Source); receipt.status = "BACKUP_VERIFIED"; write();
                EnsureFolder(Destination.Substring(0, Destination.LastIndexOf('/')), created);
                string error = AssetDatabase.ValidateMoveAsset(Source, Destination);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                receipt.status = "MOVE_INTENT"; write(); // durable recovery record before mutation
                error = AssetDatabase.MoveAsset(Source, Destination);
                moved = AssetDatabase.IsValidFolder(Destination) && !AssetDatabase.IsValidFolder(Source);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                AssetDatabase.Refresh(); Verify(entries, Destination); CheckPairingAndGuids();
                foreach (var pair in beforeGuids)
                {
                    string p = pair.Key == Source || pair.Key.StartsWith(Source + "/", StringComparison.Ordinal) ? Destination + pair.Key.Substring(Source.Length) : pair.Key;
                    if (AssetDatabase.AssetPathToGUID(p) != pair.Value) throw new InvalidOperationException("Vendor GUID mismatch: " + p);
                }
                var after = SceneDependencies().ToDictionary(d => d.path, d => d.dependencies);
                foreach (var scene in scenes)
                    if (!after.TryGetValue(scene.path, out var deps) || !deps.SequenceEqual(scene.dependencies)) throw new InvalidOperationException("Scene dependency GUID closure changed: " + scene.path);
                receipt.status = "SAFE_MOVE_IMPLEMENTED_OWNER_VALIDATION_PENDING"; write();
                Debug.Log("AST-S001 move static checks passed; NOT ACCEPTED. Receipt and backup: " + checkpoint);
            }
            catch (Exception e)
            {
                receipt.error = e.ToString(); receipt.status = "FAILED_REVIEW_REQUIRED";
                if (moved)
                {
                    string rollback = AssetDatabase.MoveAsset(Destination, Source);
                    if (string.IsNullOrEmpty(rollback))
                    {
                        try { AssetDatabase.Refresh(); Verify(entries, Source); receipt.status = "ROLLED_BACK_OWNER_VALIDATION_PENDING"; }
                        catch (Exception failure) { receipt.error += "\nRollback verification: " + failure; }
                    }
                    else receipt.error += "\nRollback failed: " + rollback;
                }
                write(); throw;
            }
        }
    }
}
