using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace LastSignal.Editor
{
    [InitializeOnLoad]
    public static class CombatRegressionRunner
    {
        static readonly TestRunnerApi api;
        static CombatRegressionRunner()
        {
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Results());
        }
        public static void Run(bool playMode)
        {
            SessionState.SetString("LastSignal.RegressionPath", RealAssetIntegration.Evidence + (playMode ? "/playmode-results.xml" : "/editmode-results.xml"));
            api.Execute(new ExecutionSettings(new Filter { testMode = playMode ? TestMode.PlayMode : TestMode.EditMode, assemblyNames = new[] { playMode ? "LastSignal.PlayModeTests" : "LastSignal.EditModeTests" } }));
        }
        sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                var path = SessionState.GetString("LastSignal.RegressionPath", "");
                if (string.IsNullOrEmpty(path)) return;
                Directory.CreateDirectory(RealAssetIntegration.Evidence);
                TestRunnerApi.SaveResultToFile(result, path);
                Debug.Log("REAL_ASSET_REGRESSION " + result.TestStatus + " passed=" + result.PassCount + " failed=" + result.FailCount);
                SessionState.EraseString("LastSignal.RegressionPath");
            }
        }
    }
}
