using System;
using System.IO;
using LastSignal.Persistence;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastSignal.Editor
{
    public static class PersistenceBuild
    {
        public const string ScenePath="Assets/LastSignal/Scenes/PersistenceAcceptance.unity";
        public const string Evidence="Docs/Implementation/RoadmapRecovery/Evidence/20260921-112305";
        [MenuItem("Last Signal/Recovery/Build Persistence Development")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop PlayMode before build.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save or discard scene changes first.");
            EditorSceneManager.OpenScene(ScenePath);
            var save=UnityEngine.Object.FindAnyObjectByType<SaveSession>();
            if(!save || !save.ValidateAuthoring().Success)throw new InvalidOperationException("Persistent identity/content authoring failed.");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/RoadmapRecovery/LastSignal.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Evidence+"/build-result.txt",$"{report.summary.result}\nUnity={Application.unityVersion}\nTarget=StandaloneOSX\nDevelopment=true\nErrors={report.summary.totalErrors}\nWarnings={report.summary.totalWarnings}\nDuration={report.summary.totalTime}\nPath={report.summary.outputPath}\n");
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Persistence Development build failed.");
        }
    }
}
