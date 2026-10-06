using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace LastSignal.Editor
{
    public static class R03Authoring
    {
        public static void Apply()
        {
            var report = new System.Text.StringBuilder();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/LastSignal/Prefabs/Enemies", "Assets/LastSignal/Prefabs/Resources" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!asset.GetComponentInChildren<ZombieController>(true)) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (var actor in root.GetComponentsInChildren<ZombieController>(true))
                    {
                        var listeners = actor.GetComponents<ZombieNoiseListener>();
                        if (listeners.Length > 1) throw new InvalidOperationException("Duplicate hearing listeners: " + path);
                        if (listeners.Length == 0) actor.gameObject.AddComponent<ZombieNoiseListener>();
                        if (!actor.Definition || !actor.Definition.IsValid(out _)) throw new InvalidOperationException("Invalid zombie definition: " + path);
                        EditorUtility.SetDirty(actor.Definition);
                        report.AppendLine(path + ": exactly one listener; hearing tuning valid");
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Docs/Implementation/PreS010-Recovery/Evidence/R03/20260927-foundation/authoring.txt", report.ToString());
        }
    }
}
