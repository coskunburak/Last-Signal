using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
namespace LastSignal.Editor
{
    public static class CombatFeedbackBuild
    {
        // User-run only. The runner supplies a unique evidence directory, never an existing build output.
        public static void DevelopmentMac()
        {
            string output = Environment.GetEnvironmentVariable("LS_COMBAT_EVIDENCE");
            if (string.IsNullOrEmpty(output) || !Directory.Exists(output))
                throw new InvalidOperationException("Use the combat feedback runner to supply a unique evidence directory.");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/LastSignal/Scenes/Validation/ZombieAcceptance.unity", "Assets/LastSignal/Scenes/Production/RelayExpedition.unity" },
                locationPathName = Path.Combine(output, "LastSignal.app"),
                target = BuildTarget.StandaloneOSX, options = BuildOptions.Development });
            File.WriteAllText(Path.Combine(output, "build-summary.txt"), "Result=" + report.summary.result + "\nErrors=" + report.summary.totalErrors +
                "\nWarnings=" + report.summary.totalWarnings + "\nDuration=" + report.summary.totalTime + "\n");
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Combat feedback development build failed.");
        }
    }
}
