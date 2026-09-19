using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace LastSignal.Editor
{
    // Explicit file-triggered Editor QA commands. No player-build code or runtime scene searches.
    [InitializeOnLoad]
    public static class ZombieValidationRunner
    {
        const string Request = "Temp/LastSignalZombieValidation.json";
        [System.Serializable] sealed class Command { public string action; public string filter; public string output; }
        static ZombieValidationRunner() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
            var text=File.ReadAllText(Request);File.Delete(Request);
            var command=JsonUtility.FromJson<Command>(text);
            try
            {
                switch(command.action)
                {
                    case "edit": Run(false,command);break;
                    case "play": Run(true,command);break;
                    case "compose": ZombieRuntimeAuthoring.Compose();break;
                    case "normal": ZombieRuntimeAuthoring.IntegrateNormal();break;
                    case "open": UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ZombieAcceptanceAuthoring.ScenePath);break;
                    case "start": EditorApplication.isPlaying=true;break;
                    case "stop": EditorApplication.isPlaying=false;break;
                    case "status": break;
                    default: throw new System.ArgumentException("Unknown validation command");
                }
                File.WriteAllText("Temp/LastSignalZombieValidation-result.json",JsonUtility.ToJson(new Status { action=command.action, compileFailed=EditorUtility.scriptCompilationFailed, playing=EditorApplication.isPlaying }));
            }
            catch(System.Exception e) { File.WriteAllText("Temp/LastSignalZombieValidation-error.txt",e.ToString());Debug.LogException(e); }
        }
        [System.Serializable] sealed class Status { public string action; public bool compileFailed,playing; }
        static void Run(bool play,Command command)
        {
            SessionState.SetString("LastSignal.RegressionPath",command.output);
            var filter=new Filter { testMode=play?TestMode.PlayMode:TestMode.EditMode,assemblyNames=new[]{play?"LastSignal.PlayModeTests":"LastSignal.EditModeTests"} };
            if(!string.IsNullOrEmpty(command.filter))filter.testNames=new[]{command.filter};
            ScriptableObject.CreateInstance<TestRunnerApi>().Execute(new ExecutionSettings(filter));
        }
    }
}
