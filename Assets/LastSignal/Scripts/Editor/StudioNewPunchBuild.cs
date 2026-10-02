using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LastSignal.Editor
{
    public static class StudioNewPunchBuild
    {
        public static void BuildDevelopmentMac()
        {
            const string scene = "Assets/LastSignal/Scenes/RelayExpedition.unity";
            const string output = "Builds/S004-NewPunch/LastSignal.app";
            const string evidence = "Docs/Implementation/S004/Evidence/20261001-NewPunch-Manual/build-summary.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            Directory.CreateDirectory(Path.GetDirectoryName(evidence));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scene }, locationPathName = output,
                target = BuildTarget.StandaloneOSX, options = BuildOptions.Development
            });
            File.WriteAllText(evidence, "Result=" + report.summary.result + "\nErrors=" +
                report.summary.totalErrors + "\nWarnings=" + report.summary.totalWarnings +
                "\nDuration=" + report.summary.totalTime + "\nUnity=" + Application.unityVersion + "\n");
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Studio New Punch development build failed.");
        }
    }
}
