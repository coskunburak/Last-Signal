using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Editor
{
    public static class AnimationExtractorToFile
    {
        [MenuItem("Last Signal/S002/Analyze VAL To File")]
        public static void AnalyzeVAL()
        {
            string path = "Assets/LastSignal/VAL.fbx";
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var sb = new StringBuilder();
            sb.AppendLine($"--- Analyzing {path} ---");
            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    sb.AppendLine($"Clip found: {clip.name} | Duration: {clip.length}s | Frame Rate: {clip.frameRate} | IsLooping: {clip.isLooping}");
                }
            }
            sb.AppendLine("--- Analysis Complete ---");
            File.WriteAllText("val_anim_analysis.txt", sb.ToString());
            Debug.Log("Wrote analysis to val_anim_analysis.txt");
        }
    }
}
