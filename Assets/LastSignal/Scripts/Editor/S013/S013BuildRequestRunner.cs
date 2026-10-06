using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Art.Editor
{
    // An open-Editor build bridge for reproducible local QA. Never overwrites a build directory.
    [InitializeOnLoad]
    public static class S013BuildRequestRunner
    {
        const string Request = "Temp/LastSignalS013BuildRequest.json";
        static double nextPoll;
        [Serializable] sealed class BuildRequest { public string output; public bool baseline; public bool clean; public string result; }

        static S013BuildRequestRunner() { EditorApplication.update += Poll; }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 1;
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var request = JsonUtility.FromJson<BuildRequest>(File.ReadAllText(Request));
            File.Delete(Request);
            if (request == null || string.IsNullOrEmpty(request.output) || string.IsNullOrEmpty(request.result)) return;
            try
            {
                if (!Path.IsPathRooted(request.output) || !Path.IsPathRooted(request.result)) throw new ArgumentException("Absolute output and result paths required.");
                if (Directory.Exists(request.output)) throw new IOException("Build output already exists; refusing overwrite.");
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save or discard the unsaved scene before build.");
                S013ProductionAuthoring.Build(request.output, request.baseline, request.clean);
                File.WriteAllText(request.result, "PASS: " + request.output);
            }
            catch (Exception error)
            {
                File.WriteAllText(request.result, "FAIL: " + error);
                Debug.LogException(error);
            }
        }
    }
}
