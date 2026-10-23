using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Editor.AssetMaintenance
{
    /// <summary>Explicit read-only validation. No move executor and no startup hooks.</summary>
    public sealed class ThirdPartyMoveManifestAudit : EditorWindow
    {
        const string PlanPath = "Docs/Implementation/Assets/AST-S001-MovePlan.json";
        [Serializable] public sealed class Plan
        {
            public int schemaVersion;
            public bool executionApproved;
            public string baselineManifest, baselineSha256;
            public Item[] items;
        }
        [Serializable] public sealed class Item
        {
            public string source, destination, guid, packageId, riskLevel, backupRoot, backupStatus;
            public bool approved;
            public string[] hardcodedPaths, specialFolders, serializedReferencers, referencingScenes;
        }
        [Serializable] public sealed class FileEntry { public string path, sha256, guid; public long size; }
        [Serializable] public sealed class FileList { public FileEntry[] files; }
        string report = "Select a manifest audit menu command. No files are changed by validation.";
        Vector2 scroll;
        static string Hash(string p)
        {
            using var sha = SHA256.Create(); using var input = File.OpenRead(p);
            return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
        }
        static bool Within(string path, string root) => path == root || path == root + ".meta" || path.StartsWith(root + "/", StringComparison.Ordinal);
        static bool SafeAssetPath(string path) => !string.IsNullOrEmpty(path) && path.StartsWith("Assets/ThirdParty/", StringComparison.Ordinal) &&
            !path.Contains("\\") && !path.Split('/').Any(p => p == ".." || p == "." || p.Length == 0) && !path.EndsWith(".meta", StringComparison.Ordinal);
        static string[] Files(string root) => (Directory.Exists(root) ? Directory.GetFiles(root, "*", SearchOption.AllDirectories) : new[] { root })
            .Concat(new[] { root + ".meta" }).Select(p => p.Replace('\\', '/')).Where(p => Path.GetFileName(p) != ".DS_Store").OrderBy(p => p, StringComparer.Ordinal).ToArray();
        [MenuItem("Tools/Last Signal/AST-S001/4 Dry Run Manifest (no writes)")]
        public static void RunManifest() => Run(false);
        [MenuItem("Tools/Last Signal/AST-S001/5 Validate Selected Package Backup (no writes)")]
        public static void RunSelected() => Run(true);
        static void Run(bool selectedOnly)
        {
            var window = GetWindow<ThirdPartyMoveManifestAudit>("AST-S001 Dry Run");
            var log = new StringBuilder("AST-S001 manifest dry run — NO ASSET CHANGES\n");
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                    throw new InvalidOperationException("Wait for import/compilation and exit Play mode.");
                var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(PlanPath));
                if (plan == null || plan.schemaVersion != 1 || plan.items == null || plan.items.Length == 0) throw new InvalidOperationException("Unsupported/empty manifest.");
                if (string.IsNullOrEmpty(plan.baselineManifest) || !plan.baselineManifest.StartsWith("Docs/Implementation/Assets/Evidence/", StringComparison.Ordinal) || plan.baselineManifest.Contains(".."))
                    throw new InvalidOperationException("Invalid baseline path.");
                if (Hash(plan.baselineManifest) != plan.baselineSha256) throw new InvalidOperationException("Baseline digest mismatch.");
                var baseline = JsonUtility.FromJson<FileList>("{\"files\":" + File.ReadAllText(plan.baselineManifest) + "}").files;
                var selected = AssetDatabase.GetAssetPath(Selection.activeObject);
                var items = selectedOnly ? plan.items.Where(i => SafeAssetPath(i.source) && Within(selected, i.source)).ToArray() : plan.items;
                if (items.Length == 0) throw new InvalidOperationException("Select a package root or asset belonging to the manifest in the Project window.");
                string[] dbPaths = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/", StringComparison.Ordinal)).ToArray();
                // One pass across imported assets; avoid N packages times N project dependency queries.
                var consumers = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
                foreach (string p in dbPaths.Where(p => !AssetDatabase.IsValidFolder(p)))
                    foreach (string dep in AssetDatabase.GetDependencies(p, false))
                    {
                        if (p == dep || !dep.StartsWith("Assets/ThirdParty/", StringComparison.Ordinal)) continue;
                        if (!consumers.TryGetValue(dep, out var set)) consumers[dep] = set = new HashSet<string>(StringComparer.Ordinal);
                        set.Add(p);
                    }
                var code = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (string root in new[] { "Assets", "Tools", "Packages", "ProjectSettings" })
                    foreach (string p in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                    {
                        string normalized = p.Replace('\\', '/');
                        if (normalized.EndsWith("/ThirdPartyAssetAudit.cs", StringComparison.Ordinal) || normalized.EndsWith("/ThirdPartyMoveManifestAudit.cs", StringComparison.Ordinal)) continue;
                        if (new[] { ".cs", ".py", ".sh", ".json", ".asmdef", ".asmref", ".rsp" }.Contains(Path.GetExtension(p))) code[normalized] = File.ReadAllText(p);
                    }
                log.AppendLine("Baseline digest VERIFIED. Backup byte checks: " + (selectedOnly ? "selected package only" : "NOT_RUN (use selected-package command; historic evidence is not current verification)"));
                foreach (Item item in items)
                {
                    log.AppendLine("\n" + item.packageId + " | " + item.source + " -> " + item.destination);
                    var blockers = new List<string>();
                    try
                    {
                        if (!SafeAssetPath(item.source) || !SafeAssetPath(item.destination) || Within(item.destination, item.source) || Within(item.source, item.destination))
                            throw new InvalidOperationException("Invalid or overlapping source/destination.");
                        if (plan.items.Count(i => string.Equals(i.destination, item.destination, StringComparison.OrdinalIgnoreCase)) != 1) blockers.Add("Duplicate/case-colliding planned destination");
                        if (plan.items.Any(i => i != item && (Within(i.source, item.source) || Within(item.source, i.source)))) blockers.Add("Overlapping manifest sources");
                        if (!File.Exists(item.source) && !Directory.Exists(item.source)) throw new InvalidOperationException("Source absent");
                        if (File.Exists(item.destination) || Directory.Exists(item.destination) || File.Exists(item.destination + ".meta") || dbPaths.Any(p => string.Equals(p, item.destination, StringComparison.OrdinalIgnoreCase))) blockers.Add("Destination collision");
                        for (string parent = Path.GetDirectoryName(item.destination)?.Replace('\\', '/'); !string.IsNullOrEmpty(parent) && parent != "Assets"; parent = Path.GetDirectoryName(parent)?.Replace('\\', '/'))
                        {
                            if (File.Exists(parent) || (File.Exists(parent + ".meta") && !Directory.Exists(parent))) blockers.Add("Destination ancestor collision: " + parent);
                            if (dbPaths.Any(p => !string.Equals(p, parent, StringComparison.Ordinal) && string.Equals(p, parent, StringComparison.OrdinalIgnoreCase))) blockers.Add("Ancestor case collision: " + parent);
                        }
                        if (AssetDatabase.AssetPathToGUID(item.source) != item.guid) blockers.Add("Root GUID mismatch");
                        var expected = baseline.Where(f => Within(f.path, item.source)).ToArray();
                        if (expected.Length == 0) throw new InvalidOperationException("No baseline files for package");
                        var current = Files(item.source);
                        if (!current.SequenceEqual(expected.Select(f => f.path).OrderBy(p => p, StringComparer.Ordinal))) blockers.Add("File membership differs from baseline");
                        var guids = new HashSet<string>(StringComparer.Ordinal);
                        foreach (var f in expected)
                        {
                            if (!File.Exists(f.path) || new FileInfo(f.path).Length != f.size) { blockers.Add("Missing/size-changed file: " + f.path); continue; }
                            if (f.path.EndsWith(".meta", StringComparison.Ordinal))
                            {
                                var match = System.Text.RegularExpressions.Regex.Match(File.ReadAllText(f.path), @"(?m)^guid: ([a-f0-9]{32})\r?$");
                                string asset = f.path.Substring(0, f.path.Length - 5);
                                if (!match.Success || !guids.Add(match.Groups[1].Value) || AssetDatabase.AssetPathToGUID(asset) != match.Groups[1].Value || Hash(f.path) != f.sha256) blockers.Add("Meta/GUID baseline mismatch: " + f.path);
                            }
                        }
                        var refs = consumers.Where(k => Within(k.Key, item.source)).SelectMany(k => k.Value).Where(p => !Within(p, item.source)).Distinct().OrderBy(p => p).ToArray();
                        log.AppendLine("Current serialized external consumers: " + refs.Length + "\n" + string.Join("\n", refs.Take(20)));
                        if (refs.Length > 0) blockers.Add("External dependencies require scoped review");
                        var literals = code.Where(k => !Within(k.Key, item.source) && (k.Value.Contains(item.source) || k.Value.Contains(Path.GetFileName(item.source)))).Select(k => k.Key).ToArray();
                        if (literals.Length > 0) blockers.Add("Current path/name references: " + string.Join(", ", literals));
                        if (item.hardcodedPaths != null && item.hardcodedPaths.Length > 0) blockers.Add("Recorded path references require disposition: " + item.hardcodedPaths.Length);
                        var special = new[] { "Editor", "Resources", "StreamingAssets", "Plugins", "Gizmos", "Editor Default Resources", "Standard Assets" };
                        var detected = current.Where(p => p.Split('/').Any(special.Contains) || new[] { ".asmdef", ".asmref", ".rsp", ".dll", ".so", ".dylib", ".bundle" }.Contains(Path.GetExtension(p))).ToArray();
                        if (detected.Length > 0 || (item.specialFolders != null && item.specialFolders.Length > 0)) blockers.Add("Special path/compilation semantics require review");
                        if (selectedOnly)
                        {
                            if (string.IsNullOrEmpty(item.backupRoot) || !Path.IsPathRooted(item.backupRoot) || !Directory.Exists(item.backupRoot)) throw new InvalidOperationException("Backup root unavailable on this machine");
                            string backupPackage = Path.GetFullPath(Path.Combine(item.backupRoot, item.source));
                            if (string.Equals(backupPackage, Path.GetFullPath(item.source), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Backup is the source itself");
                            int verified = 0;
                            foreach (var f in expected)
                            {
                                string backup = Path.Combine(item.backupRoot, f.path);
                                if (!File.Exists(f.path) || !File.Exists(backup) || Hash(f.path) != f.sha256 || Hash(backup) != f.sha256) blockers.Add("Source/backup hash mismatch or missing: " + f.path);
                                else verified++;
                            }
                            log.AppendLine("Current source+backup matches: " + verified + "/" + expected.Length);
                        }
                        else blockers.Add("Current backup validation NOT_RUN; historical status " + item.backupStatus);
                        if (!plan.executionApproved || !item.approved) blockers.Add("No approved execution authorization in manifest");
                        if (item.riskLevel != "LOW_RISK") blockers.Add("Risk review: " + item.riskLevel);
                    }
                    catch (Exception ex) { blockers.Add(ex.Message); }
                    log.AppendLine(blockers.Count > 0 ? "BLOCKED\n- " + string.Join("\n- ", blockers.Distinct()) : "PARTIAL: static prerequisites passed; compiler/import/owner acceptance NOT_RUN. This command never executes moves.");
                }
                log.AppendLine("\nNo assets moved, deleted, imported, saved or converted. No tests/builds run.");
            }
            catch (Exception ex) { log.AppendLine("BLOCKED: " + ex); }
            window.report = log.ToString(); window.Repaint();
            Debug.Log("AST-S001 dry run is visible in its report window. No files written. Export is a separate explicit action.");
        }
        void OnGUI()
        {
            EditorGUILayout.HelpBox("Read-only dry run. Export writes only a new report under Docs, never Assets. No execution is available in this window.", MessageType.Info);
            if (GUILayout.Button("Export displayed report to a new Evidence file"))
            {
                string folder = "Docs/Implementation/Assets/Evidence"; Directory.CreateDirectory(folder);
                string path = folder + "/manifest-dry-run-" + Guid.NewGuid().ToString("N") + ".txt";
                using (var writer = new StreamWriter(new FileStream(path, FileMode.CreateNew))) writer.Write(report);
                Debug.Log("AST-S001 report: " + path);
            }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
    }
}
