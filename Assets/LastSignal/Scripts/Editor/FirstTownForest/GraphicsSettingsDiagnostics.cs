using LastSignal.RenderingQuality;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Editor.FirstTownForest
{
    public static class GraphicsSettingsDiagnostics
    {
        [MenuItem("Last Signal/Graphics/Restore authored configuration (Play Mode)")]
        public static void Restore()
        {
            if (!Application.isPlaying || !GraphicsSettingsManager.Instance) { Debug.LogWarning("Enter Play Mode in a supported gameplay scene first."); return; }
            GraphicsSettingsManager.Instance.RestoreAuthored();
            Debug.Log(GraphicsSettingsManager.Instance.Message);
        }
        [MenuItem("Last Signal/Graphics/Report effective shadow distance (Play Mode)")]
        public static void Report()
        {
            var manager = GraphicsSettingsManager.Instance;
            if (!Application.isPlaying || !manager) { Debug.LogWarning("Enter Play Mode first. No tests or captures started."); return; }
            var camera = Camera.main;
            bool ok = manager.Adapter.TryReadEffectiveDistance(camera, out float distance);
            Debug.Log("Graphics state: " + JsonUtility.ToJson(manager.State) + "; authored override=" + manager.Suspended +
                "; registered tree groups=" + manager.RegisteredTreeGroups + "; effective Volume shadow distance=" +
                (ok ? distance.ToString("0.##") : "UNAVAILABLE") + "; adapter=" + manager.Adapter.Status +
                ". This evaluates Volume composition; rendered shadow appearance and GPU cost still require owner verification.");
        }
    }
}
