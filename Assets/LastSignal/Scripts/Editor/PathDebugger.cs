using UnityEditor;
using UnityEngine;

namespace LastSignal.Editor
{
    public static class PathDebugger
    {
        [MenuItem("Last Signal/S002/Debug MR POLY Path")]
        public static void DebugPath()
        {
            string guid = "ec8b84958e5a376498beac2632afff24";
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log($"GUID {guid} maps to path: '{path}'");
            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            Debug.Log($"Loaded object: {(obj != null ? obj.name + " (Type: " + obj.GetType().Name + ")" : "NULL")}");
        }
    }
}
