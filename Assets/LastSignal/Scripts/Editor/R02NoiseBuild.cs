using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace LastSignal.Editor
{
    public static class R02NoiseBuild
    {
        public static void Build() => BuildAt("R02", "Assets/LastSignal/Scenes/Validation/WorldPopulationAcceptance.unity");
        public static void BuildR03() => BuildAt("R03", "Assets/LastSignal/Scenes/Validation/ZombieAcceptance.unity");
        static void BuildAt(string milestone, string scene)
        {
            string evidence = "Docs/Implementation/PreS010-Recovery/Evidence/" + milestone + "/20260927-foundation/";
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = milestone == "R03" ? new[] { scene, "Assets/LastSignal/Scenes/Validation/WorldPopulationAcceptance.unity" } : new[] { scene },
                locationPathName = "Builds/" + milestone + "/LastSignal.app", target = BuildTarget.StandaloneOSX, options = BuildOptions.Development });
            Directory.CreateDirectory(evidence);
            File.WriteAllText(evidence + "build-result.txt", $"Result={report.summary.result}\nUnity={Application.unityVersion}\nTarget=StandaloneOSX\nDevelopment=true\nPath={report.summary.outputPath}\nTimestampUtc={DateTime.UtcNow:O}\nErrors={report.summary.totalErrors}\nWarnings={report.summary.totalWarnings}\nDuration={report.summary.totalTime}\n");
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException(milestone + " development build failed.");
        }
    }
}
