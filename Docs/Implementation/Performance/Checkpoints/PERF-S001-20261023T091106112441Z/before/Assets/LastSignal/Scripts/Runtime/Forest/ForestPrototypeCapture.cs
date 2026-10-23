using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace LastSignal.Forest
{
    /// <summary>Owner-triggered F9 capture. Inert in release Players; never changes rendering or gameplay settings.</summary>
    public sealed class ForestPrototypeCapture : MonoBehaviour
    {
        public ForestPrototypeState state;
        [Tooltip("Optional experiment/route label; set in Play Mode before F9.")]
        public string scenario = "A-baseline";
        const int Capacity = 32768;
        const double Duration = 30, WarmupSeconds = 10;
        struct Sample
        {
            public int frame, width, height, gc0, gc1, gc2, focused;
            public double elapsed, frameMs, main, render, gpu, cpu, draws, batches, setPass, triangles, vertices, memory, gcBytes;
            public ulong timingStamp;
            public float scaleX, scaleY;
        }
        [Serializable] sealed class Metadata
        {
            public int schema = 2, frames, warmupFrames = 180, bufferCapacity = Capacity;
            public string utc, scenario, status, scene, generation, digest, unity, os, device, cpu, gpu, driver, api, pipeline, quality;
            public int ramMB, reportedGraphicsMemoryMB, cpuCores, vsync, targetFrameRate, qualityIndex, pixelWidth, pixelHeight, scaledWidth, scaledHeight;
            public bool editor, developmentBuild, frameTimingEnabled, dynamicResolutionAllowed, overcast, srpBatcher, configurationChanged;
            public float fieldOfView;
            public string antialiasing, cameraStart, cameraEnd;
            public string treeShadowSetting;
            public double warmupSeconds = WarmupSeconds, requestedSeconds = Duration, actualSeconds;
            public double averageFPS, medianFPS, p50ms, p95ms, p99ms;
            public int gpuSamples, cpuTimingSamples, unfocusedFrames;
            public string[] counterAvailability;
            public string visibleRenderers = "UNAVAILABLE: use Rendering Profiler/Frame Debugger; no scene-wide scan in the sample loop.";
            public string powerTemperature = "OWNER_REQUIRED: power mode, AC/battery, thermal state and discrete GPU selection.";
            public string timingNotes = "frame_ms is wall time between Update calls. Recorder counters are completed-frame samples and not guaranteed aligned with frame_ms. FTM CPU/GPU values are delayed; ftm_timestamp identifies their frame. Nonpositive or repeated FTM samples are blank, not zero. Main/Render Thread markers include waits. GC collection deltas do not measure pause duration; use Timeline. Export work is outside capture.";
        }
        readonly FrameTiming[] timings = new FrameTiming[1];
        Sample[] samples;
        ProfilerRecorder main, render, draws, batches, setPass, triangles, vertices, memory, gcBytes;
        int count, warmup, previousGc0, previousGc1, previousGc2;
        double requested, started, previous;
        ulong lastTimingStamp;
        bool capturing;
        Metadata metadata;
        RenderPipelineAsset capturedPipeline;

        public void Begin()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (capturing) return;
            if (!Application.isPlaying || !state || !state.gameplayCamera || !state.generated)
            { Debug.LogWarning("Forest capture requires the authored scene in Play Mode."); return; }
            samples = samples ?? new Sample[Capacity]; // One bounded allocation before warmup, no per-frame arrays or list growth.
            count = 0; warmup = 180; requested = Time.realtimeSinceStartupAsDouble; started = 0; lastTimingStamp = 0;
            previousGc0 = GC.CollectionCount(0); previousGc1 = GC.CollectionCount(1); previousGc2 = GC.CollectionCount(2);
            try
            {
                main = Start(ProfilerCategory.Internal, "Main Thread"); render = Start(ProfilerCategory.Internal, "Render Thread");
                draws = Start(ProfilerCategory.Render, "Draw Calls Count"); batches = Start(ProfilerCategory.Render, "Batches Count");
                setPass = Start(ProfilerCategory.Render, "SetPass Calls Count"); triangles = Start(ProfilerCategory.Render, "Triangles Count");
                vertices = Start(ProfilerCategory.Render, "Vertices Count"); memory = Start(ProfilerCategory.Memory, "Total Used Memory");
                gcBytes = Start(ProfilerCategory.Memory, "GC Allocated In Frame");
                var camera = state.gameplayCamera; var hd = camera.GetComponent<HDAdditionalCameraData>();
                var lighting = state.generated.GetComponentInChildren<ForestPrototypeLighting>();
                var shadows = GetComponent<ForestTreeShadowSettings>();
                capturedPipeline = GraphicsSettings.currentRenderPipeline;
                metadata = new Metadata { utc = DateTime.UtcNow.ToString("O"), scenario = scenario,
                    treeShadowSetting = shadows ? (shadows.TreeShadowsEnabled ? "ON" : "OFF") : "UNAVAILABLE",
                    scene = state.gameObject.scene.path, generation = state.generationFolder, digest = state.placementDigest,
                    unity = Application.unityVersion, os = SystemInfo.operatingSystem, device = SystemInfo.deviceModel,
                    cpu = SystemInfo.processorType, cpuCores = SystemInfo.processorCount, gpu = SystemInfo.graphicsDeviceName,
                    driver = SystemInfo.graphicsDeviceVersion, api = SystemInfo.graphicsDeviceType.ToString(),
                    ramMB = SystemInfo.systemMemorySize, reportedGraphicsMemoryMB = SystemInfo.graphicsMemorySize,
                    editor = Application.isEditor, developmentBuild = Debug.isDebugBuild,
                    pipeline = GraphicsSettings.currentRenderPipeline ? GraphicsSettings.currentRenderPipeline.name : "null",
                    quality = QualitySettings.names[QualitySettings.GetQualityLevel()], qualityIndex = QualitySettings.GetQualityLevel(), vsync = QualitySettings.vSyncCount,
                    targetFrameRate = Application.targetFrameRate, pixelWidth = camera.pixelWidth, pixelHeight = camera.pixelHeight,
                    scaledWidth = camera.scaledPixelWidth, scaledHeight = camera.scaledPixelHeight,
                    frameTimingEnabled = FrameTimingManager.IsFeatureEnabled(), dynamicResolutionAllowed = hd && hd.allowDynamicResolution,
                    antialiasing = hd ? hd.antialiasing.ToString() : "UNAVAILABLE", overcast = lighting && lighting.isOvercast, fieldOfView = camera.fieldOfView,
                    srpBatcher = GraphicsSettings.useScriptableRenderPipelineBatching, cameraStart = Pose(camera) };
                capturing = true; enabled = true;
                Debug.Log("Forest F9: warmup >=180 frames AND >=10 seconds, then 30 seconds. No settings changed. Keep the route/configuration fixed.");
            }
            catch { DisposeRecorders(); capturing = false; enabled = false; throw; }
#endif
        }
        static ProfilerRecorder Start(ProfilerCategory category, string name)
        {
            try { return ProfilerRecorder.StartNew(category, name, 1); }
            catch (ArgumentException) { return default; } // Counter is unavailable on this backend.
        }
        static double Read(ProfilerRecorder r, double divisor = 1) => r.Valid && r.Count > 0 ? r.LastValue / divisor : double.NaN;
        static string Pose(Camera camera) => camera.transform.position.ToString("R") + " / " + camera.transform.eulerAngles.ToString("R");
        void Update()
        {
            if (!capturing) return;
            if (!state || !state.gameplayCamera) { enabled = false; return; }
            double now = Time.realtimeSinceStartupAsDouble;
            bool timingEnabled = FrameTimingManager.IsFeatureEnabled();
            metadata.frameTimingEnabled |= timingEnabled;
            metadata.configurationChanged |= QualitySettings.vSyncCount != metadata.vsync || Application.targetFrameRate != metadata.targetFrameRate ||
                QualitySettings.GetQualityLevel() != metadata.qualityIndex || state.gameplayCamera.pixelWidth != metadata.pixelWidth ||
                state.gameplayCamera.pixelHeight != metadata.pixelHeight || GraphicsSettings.currentRenderPipeline != capturedPipeline;
            if (timingEnabled) FrameTimingManager.CaptureFrameTimings();
            // Continue warming until BOTH conditions hold; excluded from exported sample statistics.
            if (warmup-- > 0 || now - requested < WarmupSeconds)
            {
                previous = now;
                previousGc0 = GC.CollectionCount(0); previousGc1 = GC.CollectionCount(1); previousGc2 = GC.CollectionCount(2);
                if (timingEnabled && FrameTimingManager.GetLatestTimings(1, timings) > 0) lastTimingStamp = timings[0].frameStartTimestamp;
                return;
            }
            if (started == 0) { started = now; previous = now; return; }
            ref var sample = ref samples[count];
            sample = new Sample { frame = Time.frameCount, elapsed = now - started, frameMs = (now - previous) * 1000,
                main = Read(main, 1000000), render = Read(render, 1000000), gpu = double.NaN, cpu = double.NaN,
                draws = Read(draws), batches = Read(batches), setPass = Read(setPass), triangles = Read(triangles), vertices = Read(vertices),
                memory = Read(memory), gcBytes = Read(gcBytes), scaleX = float.NaN, scaleY = float.NaN,
                width = state.gameplayCamera.scaledPixelWidth, height = state.gameplayCamera.scaledPixelHeight,
                focused = Application.isFocused ? 1 : 0 };
            // FTM is delayed; omit the first four measured updates to avoid importing warmup timings.
            if (count >= 4 && timingEnabled && FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].frameStartTimestamp > lastTimingStamp)
            {
                var timing = timings[0]; lastTimingStamp = timing.frameStartTimestamp; sample.timingStamp = lastTimingStamp;
                if (timing.gpuFrameTime > 0) sample.gpu = timing.gpuFrameTime;
                if (timing.cpuFrameTime > 0) sample.cpu = timing.cpuFrameTime;
                if (timing.widthScale > 0) sample.scaleX = timing.widthScale;
                if (timing.heightScale > 0) sample.scaleY = timing.heightScale;
            }
            int gc0 = GC.CollectionCount(0), gc1 = GC.CollectionCount(1), gc2 = GC.CollectionCount(2);
            sample.gc0 = gc0 - previousGc0; sample.gc1 = gc1 - previousGc1; sample.gc2 = gc2 - previousGc2;
            previousGc0 = gc0; previousGc1 = gc1; previousGc2 = gc2; previous = now; count++;
            if (now - started >= Duration || count == Capacity) Finish(now - started >= Duration ? "COMPLETED" : "BUFFER_LIMIT_PARTIAL");
        }
        static string Number(double v) => double.IsNaN(v) || double.IsInfinity(v) ? "" : v.ToString("R", CultureInfo.InvariantCulture);
        static string Availability(string name, ProfilerRecorder r) => name + ":" + (r.Valid && r.Count > 0 ? "AVAILABLE_AT_END" : "UNAVAILABLE");
        void Finish(string status)
        {
            metadata.status = status; metadata.frames = count; metadata.actualSeconds = samples[count - 1].elapsed;
            metadata.cameraEnd = Pose(state.gameplayCamera);
            metadata.counterAvailability = new[] { Availability("Main Thread", main), Availability("Render Thread", render),
                Availability("Draw Calls Count", draws), Availability("Batches Count", batches), Availability("SetPass Calls Count", setPass),
                Availability("Triangles Count", triangles), Availability("Vertices Count", vertices), Availability("Total Used Memory", memory),
                Availability("GC Allocated In Frame", gcBytes) };
            capturing = false; enabled = false; // Dispose before sorting, formatting and disk I/O.
            var frames = new double[count];
            for (int i = 0; i < count; i++)
            {
                frames[i] = samples[i].frameMs;
                if (!double.IsNaN(samples[i].gpu)) metadata.gpuSamples++;
                if (!double.IsNaN(samples[i].cpu)) metadata.cpuTimingSamples++;
                if (samples[i].focused == 0) metadata.unfocusedFrames++;
            }
            Array.Sort(frames);
            double Percentile(double p) => frames[Mathf.Clamp((int)Math.Ceiling(count * p) - 1, 0, count - 1)];
            metadata.averageFPS = 1000 / frames.Average(); metadata.p50ms = Percentile(.5);
            metadata.medianFPS = 1000 / metadata.p50ms; metadata.p95ms = Percentile(.95); metadata.p99ms = Percentile(.99);
            string label = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ") + "-" + Guid.NewGuid().ToString("N");
            string path = Path.Combine(Application.persistentDataPath, "ForestPrototype120", label);
            try
            {
                Directory.CreateDirectory(path);
                using (var file = new StreamWriter(Path.Combine(path, "frames.csv")))
                {
                    file.WriteLine("frame,elapsed_s,frame_ms,main_thread_ms,render_thread_ms,ftm_cpu_ms,ftm_gpu_ms,ftm_timestamp,draw_calls,batches,setpass,triangles,vertices,memory_bytes,gc_alloc_bytes,gc0_delta,gc1_delta,gc2_delta,scaled_width,scaled_height,ftm_scale_x,ftm_scale_y,focused");
                    for (int i = 0; i < count; i++)
                    {
                        var s = samples[i];
                        file.WriteLine(string.Join(",", new[] { Number(s.frame), Number(s.elapsed), Number(s.frameMs), Number(s.main), Number(s.render),
                            Number(s.cpu), Number(s.gpu), s.timingStamp == 0 ? "" : s.timingStamp.ToString(CultureInfo.InvariantCulture),
                            Number(s.draws), Number(s.batches), Number(s.setPass), Number(s.triangles), Number(s.vertices), Number(s.memory), Number(s.gcBytes),
                            Number(s.gc0), Number(s.gc1), Number(s.gc2), Number(s.width), Number(s.height), Number(s.scaleX), Number(s.scaleY), Number(s.focused) }));
                    }
                }
                File.WriteAllText(Path.Combine(path, "summary.json"), JsonUtility.ToJson(metadata, true) + "\n");
                File.WriteAllText(Path.Combine(path, "summary.txt"), "Status=" + status + "\nFrames=" + count + "\nAverageFPS=" + Number(metadata.averageFPS) +
                    "\nP50ms=" + Number(metadata.p50ms) + "\nP95ms=" + Number(metadata.p95ms) + "\nP99ms=" + Number(metadata.p99ms) +
                    "\nGPU samples=" + metadata.gpuSamples + "\nFull metadata: summary.json\n" + metadata.timingNotes + "\n");
                Debug.Log("Forest performance capture written: " + path);
            }
            catch (IOException error) { Debug.LogError("Forest capture export failed: " + error.Message); }
        }
        void DisposeRecorders()
        {
            main.Dispose(); render.Dispose(); draws.Dispose(); batches.Dispose(); setPass.Dispose();
            triangles.Dispose(); vertices.Dispose(); memory.Dispose(); gcBytes.Dispose();
            main = render = draws = batches = setPass = triangles = vertices = memory = gcBytes = default;
        }
        void OnDisable()
        {
            DisposeRecorders();
            if (capturing) Debug.LogWarning("Forest capture cancelled before completion; no complete benchmark report exported.");
            capturing = false;
        }
    }
}
