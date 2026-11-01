using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSignal.Editor.WorldReconstruction
{
    [Serializable] public sealed class WorldAssetCopy
    {
        public string source, sourceGuid, sourceHash, sourceMetaHash, destination, destinationGuid, destinationHash, destinationMetaHash;
    }

    // Copies native assets plus model importers that point back into that mutable closure.
    internal sealed class WorldStageIsolation
    {
        internal readonly List<WorldAssetCopy> copies = new List<WorldAssetCopy>();
        readonly Dictionary<string, WorldAssetCopy> byGuid = new Dictionary<string, WorldAssetCopy>();
        readonly Dictionary<string, Dictionary<long, Object>> localObjects = new Dictionary<string, Dictionary<long, Object>>();
        static readonly Regex GuidReference = new Regex(@"\bguid: ([0-9a-f]{32})\b");

        internal WorldStageIsolation(string scene, string folder)
        {
            var dependencies = AssetDatabase.GetDependencies(scene, true).Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) && p != scene).ToArray();
            var nativeExtensions = new HashSet<string> { ".asset", ".mat", ".terrainlayer", ".prefab", ".lighting" };
            var selected = new HashSet<string>(dependencies.Where(p => nativeExtensions.Contains(Path.GetExtension(p).ToLowerInvariant())), StringComparer.Ordinal);
            // A shared FBX may itself reference a source .mat through importer remaps. Include that container too.
            bool grew;
            do
            {
                grew = false;
                foreach (var path in dependencies.Where(p => !selected.Contains(p)))
                    if (AssetDatabase.GetDependencies(path, false).Any(d => d != path && selected.Contains(d)))
                    {
                        if (!(AssetImporter.GetAtPath(path) is ModelImporter))
                            throw new InvalidOperationException("Unsupported immutable-to-mutable dependency boundary: " + path);
                        selected.Add(path); grew = true;
                    }
            } while (grew);
            foreach (string path in selected.OrderBy(p => p, StringComparer.Ordinal))
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (EditorUtility.IsDirty(AssetImporter.GetAtPath(path))) throw new InvalidOperationException("Unsaved importer: " + path);
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (asset && EditorUtility.IsDirty(asset)) throw new InvalidOperationException("Unsaved dependency: " + path);
                if (ext == ".prefab" && !File.ReadAllText(path).StartsWith("%YAML", StringComparison.Ordinal))
                    throw new InvalidOperationException("Prefab must use text serialization: " + path);
                string guid = AssetDatabase.AssetPathToGUID(path);
                var copy = new WorldAssetCopy { source = path, sourceGuid = guid,
                    sourceHash = WorldReconstructionAudit.Hash(path), sourceMetaHash = WorldReconstructionAudit.Hash(path + ".meta"),
                    destination = folder + "/" + guid + ext };
                copies.Add(copy); byGuid.Add(guid, copy);
            }
            long bytes = copies.Sum(c => new FileInfo(c.source).Length) + new FileInfo(scene).Length;
            if (copies.Count == 0 || copies.Count > 1024 || bytes > 1024L * 1024 * 1024)
                throw new InvalidOperationException("Isolation exceeds the first-stage 1024 asset / 1 GiB budget. Review dependencies.");
            if (new DriveInfo(Path.GetPathRoot(Path.GetFullPath(scene))).AvailableFreeSpace < bytes * 3 + 2L * 1024 * 1024 * 1024)
                throw new IOException("Insufficient disk headroom for isolated candidate and imports.");
        }

        internal void CopyAll(List<string> created, Action journal)
        {
            for (int i = 0; i < copies.Count; i++)
            {
                var c = copies[i];
                if (EditorUtility.DisplayCancelableProgressBar("WLD G2 — isolation", c.source, i / (float)copies.Count))
                    throw new OperationCanceledException("Owner cancelled candidate creation.");
                if (File.Exists(c.destination) || File.Exists(c.destination + ".meta")) throw new IOException("Output exists: " + c.destination);
                // Record the reserved destination before copying so partial copies can be rolled back.
                created.Add(c.destination);
                journal();
                if (!AssetDatabase.CopyAsset(c.source, c.destination)) throw new IOException("Copy failed: " + c.source);
                c.destinationGuid = AssetDatabase.AssetPathToGUID(c.destination);
                if (string.IsNullOrEmpty(c.destinationGuid) || c.destinationGuid == c.sourceGuid)
                    throw new IOException("Copy did not receive an isolated GUID: " + c.source);
                journal();
            }
            // Preserve copied importer settings/local IDs, but redirect any external material/object remaps.
            foreach (var c in copies)
            {
                string meta = File.ReadAllText(c.destination + ".meta");
                string remapped = GuidReference.Replace(meta, m => byGuid.TryGetValue(m.Groups[1].Value, out var target) ? "guid: " + target.destinationGuid : m.Value);
                if (meta == remapped) continue;
                File.WriteAllText(c.destination + ".meta", remapped);
                AssetDatabase.ImportAsset(c.destination, ImportAssetOptions.ForceSynchronousImport);
            }
            // Prefab YAML keeps its local fileIDs. Remapping the source-prefab GUID also handles nested instances.
            foreach (var c in copies.Where(c => c.destination.EndsWith(".prefab", StringComparison.Ordinal))) RemapText(c.destination);
            foreach (var c in copies.Where(c => !c.destination.EndsWith(".prefab", StringComparison.Ordinal)))
            {
                if (AssetImporter.GetAtPath(c.destination) is ModelImporter) continue; // Imported subassets are controlled by their copied importer.
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(c.destination))
                    if (asset)
                    {
                        RemapObject(asset);
                        // Terrain's native layers/prototypes are not all exposed by SerializedObject.GetIterator.
                        if (asset is TerrainData terrain) RemapTerrain(terrain);
                        AssetDatabase.SaveAssetIfDirty(asset);
                    }
            }
        }

        internal void RemapText(string path)
        {
            string text = File.ReadAllText(path);
            if (!text.StartsWith("%YAML", StringComparison.Ordinal)) throw new InvalidOperationException("Expected Unity YAML: " + path);
            text = GuidReference.Replace(text, m => byGuid.TryGetValue(m.Groups[1].Value, out var c) ? "guid: " + c.destinationGuid : m.Value);
            File.WriteAllText(path, text);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        Object Replacement(Object original)
        {
            if (!original || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original, out string guid, out long id) || !byGuid.TryGetValue(guid, out var copy)) return original;
            if (!localObjects.TryGetValue(copy.destination, out var objects))
            {
                objects = new Dictionary<long, Object>();
                void Add(Object asset)
                {
                    if (asset && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string _, out long localId)) objects[localId] = asset;
                }
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(copy.destination))
                {
                    Add(asset);
                    if (asset is GameObject root) foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    { Add(t.gameObject); foreach (var component in t.GetComponents<Component>()) Add(component); }
                }
                localObjects.Add(copy.destination, objects);
            }
            if (!objects.TryGetValue(id, out var replacement) || replacement.GetType() != original.GetType())
                throw new InvalidOperationException("Copied local object ID/type mismatch: " + copy.source + " #" + id);
            return replacement;
        }

        void RemapObject(Object target)
        {
            var so = new SerializedObject(target); var it = so.GetIterator(); bool changed = false;
            while (it.Next(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference || !it.objectReferenceValue) continue;
                var replacement = Replacement(it.objectReferenceValue);
                if (replacement == it.objectReferenceValue) continue;
                it.objectReferenceValue = replacement; changed = true;
            }
            if (changed) { so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target); }
        }

        void RemapTerrain(TerrainData terrain)
        {
            // Use a fresh prototype object rather than editing an object returned from a native getter.
            // Preserve every density, color, size and rendering setting in the copy constructors.
            var layers = terrain.terrainLayers;
            bool changed = false;
            for (int i = 0; i < layers.Length; i++)
            {
                var replacement = (TerrainLayer)Replacement(layers[i]);
                changed |= replacement != layers[i]; layers[i] = replacement;
            }
            if (changed) terrain.terrainLayers = layers;

            var details = terrain.detailPrototypes.Select(p => new DetailPrototype(p)).ToArray();
            bool detailsChanged = false;
            foreach (var detail in details)
            {
                var prefab = (GameObject)Replacement(detail.prototype);
                var texture = (Texture2D)Replacement(detail.prototypeTexture);
                detailsChanged |= prefab != detail.prototype || texture != detail.prototypeTexture;
                detail.prototype = prefab; detail.prototypeTexture = texture;
            }
            if (detailsChanged) terrain.detailPrototypes = details;

            var trees = terrain.treePrototypes.Select(p => new TreePrototype(p)).ToArray();
            bool treesChanged = false;
            foreach (var tree in trees)
            {
                var prefab = (GameObject)Replacement(tree.prefab);
                treesChanged |= prefab != tree.prefab; tree.prefab = prefab;
            }
            if (treesChanged) terrain.treePrototypes = trees;
            if (changed || detailsChanged || treesChanged) EditorUtility.SetDirty(terrain);
        }

        internal static void VerifyNoSourceDependencies(string root, IEnumerable<WorldAssetCopy> assets)
        {
            var originals = new HashSet<string>(assets.Select(c => c.source), StringComparer.Ordinal);
            // Retain exact dependency edges in the failure journal before rollback removes the candidate.
            var pending = new Queue<string>(); var visited = new HashSet<string>(StringComparer.Ordinal);
            var leaks = new List<string>(); pending.Enqueue(root);
            while (pending.Count > 0)
            {
                string parent = pending.Dequeue(); if (!visited.Add(parent)) continue;
                foreach (string dependency in AssetDatabase.GetDependencies(parent, false))
                {
                    if (dependency == parent) continue;
                    if (originals.Contains(dependency)) leaks.Add(parent + " -> " + dependency);
                    else if (!visited.Contains(dependency)) pending.Enqueue(dependency);
                }
            }
            if (leaks.Count > 0) throw new InvalidOperationException("Candidate retains " + leaks.Count +
                " mutable source dependency edges:\n" + string.Join("\n", leaks.OrderBy(p => p, StringComparer.Ordinal)));
        }

        internal void VerifyClosure(string stage)
        {
            VerifyNoSourceDependencies(stage, copies);
            foreach (var c in copies)
            {
                if (WorldReconstructionAudit.Hash(c.source) != c.sourceHash || WorldReconstructionAudit.Hash(c.source + ".meta") != c.sourceMetaHash)
                    throw new InvalidOperationException("Source dependency changed during isolation: " + c.source);
                c.destinationHash = WorldReconstructionAudit.Hash(c.destination);
                c.destinationMetaHash = WorldReconstructionAudit.Hash(c.destination + ".meta");
            }
        }
    }
}
