using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace LastSignal.Editor
{
    public static class R05Build
    {
        public static void Build()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/LastSignal/Scenes/ZombieAcceptance.unity" },
                locationPathName = "Builds/R05/LastSignal.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development
            });
            var text = $"Result={report.summary.result}\nUnity={Application.unityVersion}\nTarget=StandaloneOSX\nDevelopment=true\nPath={report.summary.outputPath}\nTimestampUtc={DateTime.UtcNow:O}\nErrors={report.summary.totalErrors}\nWarnings={report.summary.totalWarnings}\nDuration={report.summary.totalTime}\nBundleExists={Directory.Exists(report.summary.outputPath)}\nExecutableExists={File.Exists(report.summary.outputPath + "/Contents/MacOS/LastSignal")}\n";
            File.WriteAllText("build-report.txt", text);
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("R05 development build failed.");
        }
    }
}
