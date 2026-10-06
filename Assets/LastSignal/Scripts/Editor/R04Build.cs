using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace LastSignal.Editor
{
    public static class R04Build
    {
        public static void Build()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/LastSignal/Scenes/Validation/ZombieAcceptance.unity" },
                locationPathName = "Builds/R04/LastSignal.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development
            });
            var evidence = "Docs/Implementation/PreS010-Recovery/Evidence/R04/20260927-150600/";
            File.WriteAllText(evidence + "build-result.txt", $"Result={report.summary.result}\nUnity={Application.unityVersion}\nTarget=StandaloneOSX\nDevelopment=true\nPath={report.summary.outputPath}\nTimestampUtc={DateTime.UtcNow:O}\nErrors={report.summary.totalErrors}\nWarnings={report.summary.totalWarnings}\nDuration={report.summary.totalTime}\n");
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("R04 development build failed.");
        }
    }
}
