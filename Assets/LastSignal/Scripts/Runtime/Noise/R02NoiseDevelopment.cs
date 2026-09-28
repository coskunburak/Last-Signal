using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
#endif
namespace LastSignal.Noise
{
    /// <summary>Opt-in development instrumentation. Install only with -noiseDebug or -noiseAcceptance path.</summary>
    public sealed class R02NoiseDevelopment : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        SessionFlow flow;
        string directory;
        string stage = "R02 gameplay noise";
        readonly List<string> recent = new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args = Environment.GetCommandLineArgs(); bool show = false; string path = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-noiseDebug") show = true;
                if (args[i] == "-noiseAcceptance" && i + 1 < args.Length) { path = args[++i]; show = true; }
            }
            if (!show) return;
            var go = new GameObject("R02 development noise instrumentation");
            go.AddComponent<R02NoiseDevelopment>().directory = path;
        }
        IEnumerator Start()
        {
            yield return null; yield return null;
            flow = FindAnyObjectByType<SessionFlow>();
            if (string.IsNullOrEmpty(directory)) yield break;
            Directory.CreateDirectory(directory); var output = new StringBuilder();
            var stack = new Stack<IEnumerator>();
            stack.Push(R02NoiseAcceptanceRoute.Run(flow, directory, line => { output.AppendLine(line); stage = line; recent.Add(line); if (recent.Count > 5) recent.RemoveAt(0); }, true));
            Exception failure = null;
            while (stack.Count > 0)
            {
                object current = null; bool next = false;
                try { next = stack.Peek().MoveNext(); if (next) current = stack.Peek().Current; }
                catch (Exception error) { failure = error; break; }
                if (!next) { stack.Peek().DisposeIfPossible(); stack.Pop(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
            while (stack.Count > 0) stack.Pop().DisposeIfPossible();
            output.AppendLine(failure == null ? "STANDALONE R02 PASS" : "STANDALONE R02 FAIL\n" + failure);
            File.WriteAllText(Path.Combine(directory, "standalone-smoke.txt"), output.ToString());
            if (failure != null) Debug.LogException(failure);
            yield return null; Application.Quit(failure == null ? 0 : 1);
        }
        void OnGUI()
        {
            if (!flow || flow.Noise == null) return;
            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(12, Screen.height - 235, Screen.width - 24, 223), GUI.skin.box);
            GUILayout.Label("R02 DEVELOPMENT — LOCAL NOISE CANDIDATES / NO ZOMBIE HEARING BEHAVIOR");
            var system = flow.Noise;
            GUILayout.Label($"Epoch {system.Epoch} | events {system.AcceptedCount} | listeners {system.ListenerCount} | recent {system.RecentCount}/64");
            if (system.RecentCount > 0)
            {
                var trace = system.LastTrace; var e = trace.Event;
                GUILayout.Label($"Event {e.EventId} | {e.Category} | source {e.SourceId} | action {e.ActionId}");
                GUILayout.Label($"Origin {e.Position} | radius {e.BaseRadiusMeters} m | intensity {e.Intensity:F2} | time {e.SimulationTime:F2}");
                GUILayout.Label($"Candidates {trace.CandidateCount} | delivered {trace.DeliveryCount} | cells {trace.CellsVisited} | pressure forwarded {trace.PressureForwarded}");
            }
            GUILayout.Label(stage);
            GUILayout.EndArea();
        }
#endif
    }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    static class R02EnumeratorCleanup
    { public static void DisposeIfPossible(this IEnumerator iterator) { (iterator as IDisposable)?.Dispose(); } }
#endif
}
