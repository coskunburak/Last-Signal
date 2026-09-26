using UnityEditor;
using UnityEngine;
using System.Linq;

public static class BuildUtility
{
    public static void BuildP5Mac()
    {
        var scenes = new string[] { "Assets/LastSignal/Scenes/WorldPopulationAcceptance.unity" };
        
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/P5/LastSignal.app",
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.Development
        };
        
        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            Debug.Log("BUILD_SUCCESS");
        else
            Debug.LogError("BUILD_FAILED: " + report.summary.result);
    }
}
