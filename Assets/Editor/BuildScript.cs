using UnityEditor;
class BuildScript {
    static void Build() {
        var options = new BuildPlayerOptions {
            scenes = new[] { "Assets/LastSignal/Scenes/S001Acceptance.unity", "Assets/LastSignal/Scenes/CombatAcceptance.unity" },
            locationPathName = "Builds/R03/LastSignal.app",
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.Development
        };
        BuildPipeline.BuildPlayer(options);
    }
}
