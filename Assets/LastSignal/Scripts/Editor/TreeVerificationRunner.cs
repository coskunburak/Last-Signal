using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace LastSignal.Editor
{
    /// <summary>Explicit MCP/owner invocation. Records real Unity Test Runner results across domain reloads.</summary>
    [InitializeOnLoad]
    public static class TreeVerificationRunner
    {
        const string Key="LastSignal.S004.VerificationRun";
        static readonly Recorder recorder=new Recorder();
        static TreeVerificationRunner() { TestRunnerApi.RegisterTestCallback(recorder); }
        public static string Start(string label,bool play,string[] names,string stage)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || !string.IsNullOrEmpty(SessionState.GetString(Key,"")))
                throw new InvalidOperationException("Finish the active run/Play Mode/compilation first.");
            if(!string.IsNullOrEmpty(stage))
            {
                if(!stage.StartsWith("Assets/LastSignal/Scenes/Validation/FirstTownTreePhysics_",StringComparison.Ordinal)||!File.Exists(stage))
                    throw new ArgumentException("Expected existing isolated S004 stage.");
                Environment.SetEnvironmentVariable("LASTSIGNAL_S004_STAGE",stage);
            }
            Environment.SetEnvironmentVariable("LASTSIGNAL_S004_CHOPS","4");
            string folder="Docs/Implementation/BLD/P01/S004/Evidence/MCP-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+label;
            Directory.CreateDirectory(folder);
            File.WriteAllText(folder+"/request.json",JsonUtility.ToJson(new Request {mode=play?"PlayMode":"EditMode",stage=stage,tests=names},true));
            SessionState.SetString(Key,folder);
            try
            {
                var api=ScriptableObject.CreateInstance<TestRunnerApi>();
                api.Execute(new ExecutionSettings(new Filter {testMode=play?TestMode.PlayMode:TestMode.EditMode,testNames=names}));
            }
            catch { SessionState.EraseString(Key); throw; }
            return folder;
        }
        public static string ActiveRun => SessionState.GetString(Key,"");
        [Serializable] sealed class Request { public string mode,stage; public string[] tests; }
        [Serializable] sealed class Summary { public string status; public int passed,failed,skipped,inconclusive; public double seconds; }
        sealed class Recorder : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result)
            {
                var folder=ActiveRun; if(string.IsNullOrEmpty(folder) || result.Test.IsSuite) return;
                File.AppendAllText(folder+"/tests.log",result.Test.FullName+" | "+result.TestStatus+" | "+result.Message+"\n"+result.StackTrace+"\n");
            }
            public void RunFinished(ITestResultAdaptor result)
            {
                var folder=ActiveRun; if(string.IsNullOrEmpty(folder)) return;
                try
                {
                    TestRunnerApi.SaveResultToFile(result,folder+"/tests.xml");
                    File.WriteAllText(folder+"/result.json",JsonUtility.ToJson(new Summary {status=result.TestStatus.ToString(),passed=result.PassCount,failed=result.FailCount,skipped=result.SkipCount,inconclusive=result.InconclusiveCount,seconds=result.Duration},true));
                    Debug.Log("S004_MCP_TEST_COMPLETE "+folder+" passed="+result.PassCount+" failed="+result.FailCount+" skipped="+result.SkipCount);
                }
                finally { SessionState.EraseString(Key); }
            }
        }
    }
}
