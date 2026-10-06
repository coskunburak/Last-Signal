using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal.Editor
{
    public static class AssetHierarchyAudit
    {
        const string Report = "Docs/Architecture/AssetHierarchyAudit.txt";
        static readonly Regex GuidReference = new Regex(@"guid: ([0-9a-f]{32})", RegexOptions.Compiled);

        [MenuItem("Last Signal/Validation/Audit asset hierarchy and Inspector references")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Run the asset hierarchy audit in Edit Mode.");

            AssetDatabase.Refresh();
            var failures = new List<string>();
            int prefabs = 0, scenes = 0, data = 0, components = 0, serializedFiles = 0;

            foreach (var path in Directory.GetFiles("Assets/LastSignal/Prefabs", "*.prefab", SearchOption.AllDirectories))
            {
                prefabs++;
                var assetPath = path.Replace('\\', '/');
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (!root) { failures.Add("Prefab cannot load: " + assetPath); continue; }
                Inspect(root, assetPath, failures, ref components);
            }

            foreach (var directory in new[] { "Assets/LastSignal/Scenes/Production", "Assets/LastSignal/Scenes/Validation" })
            foreach (var path in Directory.GetFiles(directory, "*.unity", SearchOption.AllDirectories))
            {
                scenes++;
                var assetPath = path.Replace('\\', '/');
                var scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Single);
                if (!scene.IsValid()) { failures.Add("Scene cannot load: " + assetPath); continue; }
                foreach (var root in scene.GetRootGameObjects())
                    Inspect(root, assetPath, failures, ref components);
            }

            foreach (var path in Directory.GetFiles("Assets/LastSignal/Data", "*.asset", SearchOption.AllDirectories))
            {
                data++;
                var assetPath = path.Replace('\\', '/');
                var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (!asset) { failures.Add("Data asset cannot load: " + assetPath); continue; }
            }

            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.enabled && !File.Exists(scene.path))
                    failures.Add("Build scene missing: " + scene.path);

            foreach (var path in Directory.GetFiles("Assets/LastSignal", "*", SearchOption.AllDirectories))
            {
                if (path.Contains("/Scenes/Recovery/") || path.Contains("/Settings/Rendering/"))
                    continue;
                var extension = Path.GetExtension(path);
                if (extension != ".prefab" && extension != ".unity" && extension != ".asset" &&
                    extension != ".mat" && extension != ".controller" && extension != ".anim" &&
                    extension != ".playable" && extension != ".shader")
                    continue;
                serializedFiles++;
                foreach (Match match in GuidReference.Matches(File.ReadAllText(path)))
                {
                    var guid = match.Groups[1].Value;
                    if (guid.StartsWith("0000000000000000") || !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                        continue;
                    failures.Add(path + " references missing GUID " + guid);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Report));
            File.WriteAllText(Report,
                "Prefabs=" + prefabs + " Scenes=" + scenes + " Data=" + data +
                " Components=" + components + " SerializedFiles=" + serializedFiles +
                " Failures=" + failures.Count + "\n" +
                (failures.Count == 0 ? "" : string.Join("\n", failures) + "\n"));
            if (failures.Count != 0)
                throw new InvalidOperationException("Asset hierarchy audit failed. See " + Report);
            Debug.Log("Asset hierarchy and Inspector references valid: " + Report);
        }

        static void Inspect(GameObject root, string path, List<string> failures, ref int components)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                var go = transform.gameObject;
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (missing != 0) failures.Add(path + ": " + go.name + " has " + missing + " missing scripts");
                foreach (var component in go.GetComponents<Component>())
                {
                    if (!component) continue;
                    components++;
                }
            }
        }
    }
}
