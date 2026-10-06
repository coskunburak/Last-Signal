using UnityEditor;
class BuildScript {
    static void Build() {
        var options = new BuildPlayerOptions {
            scenes = new[] { "Assets/LastSignal/Scenes/Validation/S001Acceptance.unity", "Assets/LastSignal/Scenes/Validation/CombatAcceptance.unity" },
            locationPathName = "Builds/R03/LastSignal.app",
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.Development
        };
        BuildPipeline.BuildPlayer(options);
    }
}
