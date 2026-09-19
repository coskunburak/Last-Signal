using UnityEditor;
using UnityEngine;

namespace LastSignal.Editor
{
    public static class AnimationExtractor
    {
        [MenuItem("Last Signal/S002/Analyze VAL Animations")]
        public static void AnalyzeVAL()
        {
            string path = "Assets/LastSignal/VAL.fbx";
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            Debug.Log($"--- Analyzing {path} ---");
            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    Debug.Log($"Clip found: {clip.name} | Duration: {clip.length}s | Frame Rate: {clip.frameRate} | IsLooping: {clip.isLooping}");
                }
            }
            Debug.Log("--- Analysis Complete ---");
        }
    }
}
