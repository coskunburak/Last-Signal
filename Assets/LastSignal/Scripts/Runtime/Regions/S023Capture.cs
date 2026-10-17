using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LastSignal.Persistence;
using LastSignal.WorldCells;
using LastSignal.WorldTime;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastSignal.Regions
{
    /// <summary>Opt-in user measurement on the isolated pilot. Never advances a route or creates gameplay state.</summary>
    public sealed class S023Capture : MonoBehaviour
    {
        public string scenario = "resident-day";
        public string profilerOverhead = "NOT_MEASURED; record attached profiler/deep profiling configuration";
        public string sourceRevision = "UNKNOWN";
        public bool sourceDirty;
        public string LastOutput { get; private set; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        const int Capacity = 24000;
        readonly double[] frames = new double[Capacity];
        readonly List<Counter> counters = new List<Counter>();
        sealed class Counter
        {
            public string name, unit; public ProfilerRecorder recorder;
            public long[] values = new long[Capacity]; public int count;
        }
        static readonly string[] Wanted = { "Main Thread", "GPU Frame Time", "Draw Calls Count", "Batches Count",
            "GC Allocated In Frame", "GC Used Memory", "Total Used Memory", "Texture Memory", "Mesh Memory" };
        WorldCellManager cells; WorldClock clock; SaveSession saves;
        string[] ids; CellState[] previousStates;
        int count, loads, unloads, pausedSamples, errors; double start, previous; bool recording; string label;
        readonly List<double> latencies = new List<double>();
        [Serializable] sealed class Metric { public string name, unit, status; public double median, p95, p99; public int samples; }
        [Serializable] sealed class Capture
        {
            public string status = "CAPTURED_NOT_ACCEPTED", scenario, scene, sourceRevision, unity, platform, cpu, gpu, quality, profilerOverhead;
            public bool sourceDirty, editor;
            public int seed, width, height, ramMB, reportedVramMB, samples, loads, unloads, spikesOver50ms, pausedSamples, errors;
            public double worldSeconds, captureMs, writeMs, hydrateMs;
            public bool raining;
            public float playerX, playerY, playerZ;
            public Metric frame, streaming;
            public List<Metric> counters = new List<Metric>();
        }
        void Start()
        {
            cells = GetComponent<WorldCellManager>(); clock = GetComponent<WorldClock>(); saves = GetComponent<SaveSession>();
            // Capturing is enabled only in this scene and disposable save identity.
            enabled = cells && saves && saves.SaveSlot == "s023-pilot";
            if (enabled) { cells.CellReady += Loaded; Application.logMessageReceived += ObserveLog; }
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-s023Scenario") scenario = args[i + 1];
        }
        void ObserveLog(string message, string trace, LogType type)
        { if (recording && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) errors++; }
        void Loaded(string id)
        {
            if (!recording) return;
            if (Time.realtimeSinceStartupAsDouble - start < 5) return;
            loads++; latencies.Add(cells.LoadMilliseconds(id));
        }
        void Update()
        {
            var keys = Keyboard.current;
            if (keys != null && keys.f12Key.wasPressedThisFrame)
            {
                if (keys.leftShiftKey.isPressed || keys.rightShiftKey.isPressed) Rain();
                else if (keys.leftCtrlKey.isPressed || keys.rightCtrlKey.isPressed) Night();
                else if (keys.leftAltKey.isPressed || keys.rightAltKey.isPressed) Day();
                else if (recording) FinishCapture(); else BeginCapture();
            }
            if (!recording) return;
            double now = Time.realtimeSinceStartupAsDouble;
            if (now - start >= 5)
            {
                if (count == Capacity) { FinishCapture(); return; }
                if (previous > 0)
                {
                    frames[count++] = (now - previous) * 1000;
                    if (cells.GetComponent<SessionFlow>().Paused) pausedSamples++;
                    foreach (var counter in counters)
                        if (counter.recorder.Valid && counter.recorder.Count > 0) counter.values[counter.count++] = counter.recorder.LastValue;
                }
                for (int i = 0; i < ids.Length; i++)
                {
                    var state = cells.State(ids[i]);
                    if (previousStates[i] != CellState.Unloaded && state == CellState.Unloaded) unloads++;
                    previousStates[i] = state;
                }
            }
            else for (int i = 0; i < ids.Length; i++) previousStates[i] = cells.State(ids[i]);
            previous = now;
            if (now - start >= 65) FinishCapture();
        }
        [ContextMenu("S023/Begin 5s warmup plus 60s capture (F12)")]
        public void BeginCapture()
        {
            if (!Application.isPlaying || !enabled || recording || !cells || cells.GetComponent<SessionFlow>().InMenu) return;
            label = scenario; count = loads = unloads = pausedSamples = errors = 0; latencies.Clear(); counters.Clear();
            ids = cells.DefinedCellIds.ToArray(); previousStates = ids.Select(cells.State).ToArray();
            var handles = new List<ProfilerRecorderHandle>(); ProfilerRecorderHandle.GetAvailable(handles);
            foreach (string name in Wanted)
                foreach (var handle in handles)
                {
                    var description = ProfilerRecorderHandle.GetDescription(handle);
                    if (description.Name != name) continue;
                    counters.Add(new Counter { name = name, unit = description.UnitType.ToString(), recorder = ProfilerRecorder.StartNew(description.Category, name, 1) });
                    break;
                }
            start = Time.realtimeSinceStartupAsDouble; previous = 0; recording = true;
            Debug.Log("S023 capture started: " + label + ". 5s warmup, 60s sample. F12 finishes early.", this);
        }
        [ContextMenu("S023/Finish capture (F12)")]
        public void FinishCapture()
        {
            if (!recording) return; recording = false;
            try
            {
                string root = Path.Combine(Application.persistentDataPath, "S023Captures");
                LastOutput = Path.Combine(root, DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ") + "-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(LastOutput);
                var player = cells.GetComponent<SessionFlow>().Player;
                var position = player ? player.transform.position : Vector3.zero;
                var report = new Capture {
                    scenario = label, scene = gameObject.scene.path, sourceRevision = sourceRevision, sourceDirty = sourceDirty,
                    unity = Application.unityVersion, editor = Application.isEditor, platform = SystemInfo.operatingSystem,
                    cpu = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName,
                    ramMB = SystemInfo.systemMemorySize, reportedVramMB = SystemInfo.graphicsMemorySize,
                    width = Screen.width, height = Screen.height, quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                    seed = GetComponent<LastSignal.Loot.LootPopulationService>().Seed, samples = count,
                    worldSeconds = clock.Simulation.Seconds, raining = clock.Simulation.Raining,
                    playerX = position.x, playerY = position.y, playerZ = position.z,
                    profilerOverhead = profilerOverhead, frame = Summarize("Frame", "ms", frames.Take(count).ToArray()),
                    streaming = Summarize("CellReady latency", "ms", latencies.ToArray()),
                    loads = loads, unloads = unloads, pausedSamples = pausedSamples, errors = errors, spikesOver50ms = frames.Take(count).Count(v => v > 50),
                    captureMs = saves.CaptureMilliseconds, writeMs = saves.WriteMilliseconds, hydrateMs = saves.HydrateMilliseconds
                };
                foreach (string name in Wanted)
                {
                    var counter = counters.FirstOrDefault(c => c.name == name);
                    report.counters.Add(counter == null ? new Metric { name = name, status = "UNAVAILABLE" } :
                        Summarize(name, counter.unit, counter.values.Take(counter.count).Select(v => (double)v).ToArray()));
                }
                if (count == 0) report.status = "INVALID_NO_SAMPLES";
                else if (errors > 0) report.status = "INVALID_ERRORS";
                File.WriteAllText(Path.Combine(LastOutput, "capture.json"), JsonUtility.ToJson(report, true));
                File.WriteAllLines(Path.Combine(LastOutput, "frames.csv"), new[] { "sample,frame_ms" }.Concat(Enumerable.Range(0, count).Select(i => i + "," + frames[i].ToString("R", CultureInfo.InvariantCulture))));
                foreach (var counter in counters)
                    File.WriteAllLines(Path.Combine(LastOutput, counter.name.Replace(' ', '-') + ".csv"), new[] { "sample,value_" + counter.unit }.Concat(Enumerable.Range(0, counter.count).Select(i => i + "," + counter.values[i].ToString(CultureInfo.InvariantCulture))));
                Debug.Log("S023 capture written: " + LastOutput + "; acceptance remains pending.", this);
            }
            finally { DisposeCounters(); }
        }
        static Metric Summarize(string name, string unit, double[] values)
        {
            var metric = new Metric { name = name, unit = unit, status = values.Length == 0 ? "NOT_MEASURED" : "MEASURED", samples = values.Length };
            if (values.Length == 0) return metric; Array.Sort(values);
            double P(double p) => values[Math.Max(0, (int)Math.Ceiling(values.Length * p) - 1)];
            metric.median = P(.5); metric.p95 = P(.95); metric.p99 = P(.99); return metric;
        }
        // Existing world simulation/presentation only; never a new weather system.
        [ContextMenu("S023/Visual condition - day clear")] public void Day() => Condition(false, false);
        [ContextMenu("S023/Visual condition - night clear")] public void Night() => Condition(true, false);
        [ContextMenu("S023/Visual condition - day rain")] public void Rain() => Condition(false, true);
        void Condition(bool night, bool rain)
        {
            if (!Application.isPlaying || !enabled || recording || !clock || clock.Simulation == null) return;
            double current = clock.Simulation.Seconds;
            double target = Math.Floor(current / 86400) * 86400 + (night ? 82800 : 43200);
            if (target < current) target += 86400;
            var advance = clock.Simulation.AdvanceUntil(target, 0);
            if (advance.Reason != AdvanceReason.Completed) throw new InvalidOperationException("World condition advance interrupted: " + advance.Reason);
            var state = clock.Capture(); state.settings.worldSecondsPerRealSecond = 1;
            state.weather = rain ? 1 : 0; state.weatherStarted = state.seconds - 300; state.nextWeather = state.seconds + 3600;
            clock.Restore(state);
        }
        void DisposeCounters() { foreach (var counter in counters) counter.recorder.Dispose(); counters.Clear(); }
        void OnDestroy() { Application.logMessageReceived -= ObserveLog; if (cells) cells.CellReady -= Loaded; recording = false; DisposeCounters(); }
#endif
    }
}
