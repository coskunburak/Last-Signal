using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Editor
{
    // Read-only Editor audit. Production states are not rebound before retarget/pose acceptance.
    public static class ZombieMixamoAudit
    {
        const string Root = "Assets/ThirdParty/Animation/AnimationLibraries/Zombie Animation/";
        static readonly string[] Names = {
            "Flying Back Death", "Zombie Attack (1)", "Zombie Attack", "Zombie Crawl",
            "Zombie Dying", "Zombie Run", "Zombie Walk"
        };

        [MenuItem("Last Signal/Zombie/Audit Mixamo Retargeting")]
        public static void Run()
        {
            var report = new StringBuilder("Last Signal Mixamo Humanoid audit\n");
            bool valid = true;
            foreach (string name in Names)
            {
                string path = Root + name + ".fbx";
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                report.Append("\n").Append(path).Append('\n');
                if (!importer) { report.Append("MISSING IMPORTER\n"); valid = false; continue; }
                report.Append("type=").Append(importer.animationType)
                    .Append(" avatarSetup=").Append(importer.avatarSetup).Append('\n');
                bool avatarValid = false, clipValid = false;
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is Avatar avatar)
                    {
                        report.Append("avatar=").Append(avatar.name)
                            .Append(" valid=").Append(avatar.isValid)
                            .Append(" human=").Append(avatar.isHuman).Append('\n');
                        avatarValid |= avatar.isValid && avatar.isHuman;
                    }
                    else if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                    {
                        report.Append("clip=").Append(clip.name)
                            .Append(" seconds=").Append(clip.length.ToString("F3"))
                            .Append(" fps=").Append(clip.frameRate.ToString("F1"))
                            .Append(" humanMotion=").Append(clip.humanMotion)
                            .Append(" loop=").Append(clip.isLooping).Append('\n');
                        clipValid |= clip.length > 0 && clip.humanMotion;
                    }
                }
                if (importer.animationType != ModelImporterAnimationType.Human || !avatarValid || !clipValid)
                { report.Append("RETARGET CHECK FAILED\n"); valid = false; }
            }
            string output = Environment.GetEnvironmentVariable("LS_ANIMATION_AUDIT_PATH");
            if (!string.IsNullOrEmpty(output))
            {
                if (File.Exists(output)) throw new IOException("Refusing to overwrite animation evidence: " + output);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
                File.WriteAllText(output, report.ToString());
            }
            Debug.Log(report.ToString());
            if (!valid) throw new InvalidOperationException("Mixamo Humanoid import audit failed; see the report.");
        }
    }
}
