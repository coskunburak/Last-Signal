#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Text;
using LastSignal.Noise;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LastSignal.Tests
{
    public sealed class R02NoisePlayTests
    {
        SessionFlow flow;
        const string Evidence = "Docs/Implementation/PreS010-Recovery/Evidence/R02/20260927-foundation/";
        [UnityTest] public IEnumerator ProductionProducersPressureLifecycleAndSaveLoad()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/WorldPopulationAcceptance.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            flow = Object.FindAnyObjectByType<SessionFlow>(); Assert.IsNotNull(flow);
            var trace = new StringBuilder(); File.WriteAllText(Evidence + "noise-trace.txt", "");
            try { yield return R02NoiseAcceptanceRoute.Run(flow, Evidence + "play-route", line => { trace.AppendLine(line); File.AppendAllText(Evidence + "noise-trace.txt", line + "\n"); }); }
            finally { File.WriteAllText(Evidence + "noise-trace.txt", trace.ToString()); }
        }
        [UnityTest] public IEnumerator ProbeDisableDestroyAndMoveCleanIndex()
        {
            var noise = new GameplayNoiseSystem(() => 0, () => true);
            var go = new GameObject("probe"); var probe = go.AddComponent<NoiseAcceptanceProbe>(); probe.Bind(noise, 1);
            Assert.AreEqual(1, noise.ListenerCount); go.SetActive(false); Assert.AreEqual(0, noise.ListenerCount);
            go.SetActive(true); Assert.AreEqual(1, noise.ListenerCount);
            go.transform.position = Vector3.right * 1000; yield return null;
            noise.TryEmit(new GameplayNoiseRequest(1, Vector3.zero, GameplayNoiseCategory.Footstep, new GameplayNoiseProfile(10, 1, 1)), out _);
            Assert.AreEqual(0, probe.ReceivedCount); Assert.AreEqual(0, noise.LastTrace.CandidateCount);
            Object.Destroy(go); yield return null; Assert.AreEqual(0, noise.ListenerCount); Assert.AreEqual(0, noise.BucketCount);
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (flow) flow.ReturnToMenu(); Time.timeScale = 1; yield return null; }
    }
}
#endif
