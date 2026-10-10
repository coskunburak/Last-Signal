using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace LastSignal.Editor
{
    /// <summary>User-invoked verification only. No InitializeOnLoad, menu auto-run or import hook.</summary>
    public static class CharacterVerification
    {
        public static void DevelopmentMac()
        {
            string output=Environment.GetEnvironmentVariable("LASTSIGNAL_CHARACTER_EVIDENCE");
            if(string.IsNullOrWhiteSpace(output)||!Directory.Exists(output))throw new InvalidOperationException("Invoke Tools/character-verify.py development-build manually.");
            string destination=Path.Combine(output,"LastSignal.app");
            if(Directory.Exists(destination))throw new IOException("Refusing to overwrite an existing build.");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=new[]{"Assets/LastSignal/Scenes/Production/S013Cabin.unity"},
                target=BuildTarget.StandaloneOSX,locationPathName=destination,options=BuildOptions.Development});
            File.WriteAllText(Path.Combine(output,"build-summary.txt"),
                $"Result={report.summary.result}\nErrors={report.summary.totalErrors}\nWarnings={report.summary.totalWarnings}\nDuration={report.summary.totalTime}\nUnity={Application.unityVersion}\n");
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Development build failed: "+report.summary.result);
        }
    }
}
