using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LastSignal.Editor.S023
{
    public static class S023Build
    {
        // Called only by the user's explicit development-build gate after regression.
        public static void DevelopmentMac()
        {
            string output = Environment.GetEnvironmentVariable("LASTSIGNAL_S023_BUILD_OUTPUT");
            if (string.IsNullOrWhiteSpace(output) || !Path.IsPathRooted(output) || Directory.Exists(output))
                throw new InvalidOperationException("New absolute output required; use Tools/s023-verify.py development-build.");
            var check = new S023RegionAuthoring.Report(); S023RegionAuthoring.Validate(check);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S023RegionAuthoring.ScenePath);
            Directory.CreateDirectory(output);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { S023RegionAuthoring.ScenePath }, locationPathName = Path.Combine(output, "LastSignal.app"),
                target = BuildTarget.StandaloneOSX, options = BuildOptions.Development
            });
            File.WriteAllText(Path.Combine(output, "build.txt"), $"Result={report.summary.result}\nErrors={report.summary.totalErrors}\nWarnings={report.summary.totalWarnings}\nUnity={Application.unityVersion}\nScene={S023RegionAuthoring.ScenePath}\nRuntime=NOT_RUN\n");
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("S023 build failed; inspect evidence.");
        }
    }
}
