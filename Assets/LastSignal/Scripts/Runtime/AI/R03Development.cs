using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
#endif
namespace LastSignal
{
    /// <summary>Opt-in development instrumentation. Install only with -hearingDebug or -hearingAcceptance path.</summary>
    public sealed class R03Development : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        SessionFlow flow;
        string directory;
        string stage = "R03 gameplay noise";
        readonly List<string> recent = new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args = Environment.GetCommandLineArgs(); bool show = false; string path = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-hearingDebug") show = true;
                if (args[i] == "-hearingAcceptance" && i + 1 < args.Length) { path = args[++i]; show = true; }
            }
            if (!show) return;
            var go = new GameObject("R03 development noise instrumentation");
            DontDestroyOnLoad(go);
            go.AddComponent<R03Development>().directory = path;
        }
        IEnumerator Start()
        {
            yield return null; yield return null;
            flow = FindAnyObjectByType<SessionFlow>();
            if (string.IsNullOrEmpty(directory)) yield break;
            Directory.CreateDirectory(directory); var output = new StringBuilder();
            var stack = new Stack<IEnumerator>();
            stack.Push(AllRoutes(line => { output.AppendLine(line); stage = line; recent.Add(line); if (recent.Count > 5) recent.RemoveAt(0); }));
            Exception failure = null;
            while (stack.Count > 0)
            {
                object current = null; bool next = false;
                try { next = stack.Peek().MoveNext(); if (next) current = stack.Peek().Current; }
                catch (Exception error) { failure = error; break; }
                if (!next) { (stack.Peek() as IDisposable)?.Dispose(); stack.Pop(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
            while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
            output.AppendLine(failure == null ? "STANDALONE R03 PASS" : "STANDALONE R03 FAIL\n" + failure);
            File.WriteAllText(Path.Combine(directory, "standalone-smoke.txt"), output.ToString());
            if (failure != null) Debug.LogException(failure);
            yield return null; Application.Quit(failure == null ? 0 : 1);
        }
        IEnumerator AllRoutes(Action<string> log)
        {
            yield return R03AcceptanceRoute.Run(flow, directory, log, true);
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("WorldPopulationAcceptance");
            yield return null; yield return null;
            flow = FindAnyObjectByType<SessionFlow>();
            yield return Noise.R02NoiseAcceptanceRoute.Run(flow, Path.Combine(directory, "producer-save-regression"), log, true);
        }
        void OnGUI()
        {
            if (!flow || flow.Noise == null) return;
            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(12, Screen.height - 330, Screen.width - 24, 318), GUI.skin.box);
            GUILayout.Label("R03 DEVELOPMENT — AUDITORY SNAPSHOT / INVESTIGATION");
            var system = flow.Noise;
            GUILayout.Label($"Epoch {system.Epoch} | events {system.AcceptedCount} | listeners {system.ListenerCount} | recent {system.RecentCount}/64");
            if (system.RecentCount > 0)
            {
                var trace = system.LastTrace; var e = trace.Event;
                GUILayout.Label($"Event {e.EventId} | {e.Category} | source {e.SourceId} | action {e.ActionId}");
                GUILayout.Label($"Origin {e.Position} | radius {e.BaseRadiusMeters} m | intensity {e.Intensity:F2} | time {e.SimulationTime:F2}");
                GUILayout.Label($"Candidates {trace.CandidateCount} | delivered {trace.DeliveryCount} | cells {trace.CellsVisited} | pressure forwarded {trace.PressureForwarded}");
            }
            var actor = flow.GetComponent<ZombieEncounter>()?.Actor;
            if (actor) {
                var memory=actor.Auditory; var heard=memory.Stimulus;
                GUILayout.Label($"State={actor.Runtime.State} | Visible={actor.Runtime.Visible} | visual confidence={actor.Runtime.Confidence:F2}");
                GUILayout.Label($"Heard={heard.Event.EventId} {heard.Event.Category} strength={heard.Strength:F3} occluded={heard.Occluded} age={memory.Age:F2}");
                GUILayout.Label($"Snapshot={heard.Event.Position} | investigate={actor.InvestigateDestination} | age={actor.InvestigateAge:F2}");
                GUILayout.Label($"Search anchor={actor.Search.Anchor} | source={(actor.SearchFromHearing ? "Hearing" : "Vision")}");
            }
            GUILayout.Label(stage);
            GUILayout.EndArea();
        }
#endif
    }
}
