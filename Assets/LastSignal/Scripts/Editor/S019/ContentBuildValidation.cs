using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal.Editor.S019
{
    // ProcessScene sees the actual BuildPlayer scene selection, including custom build entry points.
    public sealed class ContentBuildValidation : IPreprocessBuildWithReport, IProcessSceneWithReport
    {
        public int callbackOrder => -100;
        public void OnPreprocessBuild(BuildReport report) => RequireValid(ContentValidation.Project());
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report != null) RequireValid(ContentValidation.Roots(scene.GetRootGameObjects(), scene.path));
        }
        public static void RequireValid(IEnumerable<ContentIssue> issues)
        {
            var errors = ContentValidation.Sort(issues).Where(i => i.severity == ContentSeverity.Error).ToArray();
            if (errors.Length > 0) throw new BuildFailedException("S019 content validation failed:\n" + string.Join("\n", errors.Select(x => x.ToString())));
        }
        [MenuItem("Last Signal/S019/Validate project and open scenes")]
        public static void ValidateMenu()
        {
            var issues = ContentValidation.Project();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded) issues.AddRange(ContentValidation.Roots(scene.GetRootGameObjects(), scene.path));
            }
            foreach (var issue in ContentValidation.Sort(issues))
                if (issue.severity == ContentSeverity.Error) Debug.LogError(issue.ToString()); else Debug.LogWarning(issue.ToString());
            Debug.Log($"S019 validation: {issues.Count} issues. Read-only; no scene or asset changes.");
        }
    }
}
