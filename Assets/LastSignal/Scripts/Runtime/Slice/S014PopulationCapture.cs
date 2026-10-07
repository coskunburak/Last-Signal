#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;

namespace LastSignal.Slice
{
    /// <summary>Opt-in load fixture, never installed in ordinary gameplay or retail builds.
    /// Measures production actors attacking a stationary target healed by this disposable fixture. It does not
    /// validate encounter fairness, population materialization, locomotion quality or saves.</summary>
    public sealed class S014PopulationCapture : MonoBehaviour
    {
        string output;
        int errors;
        SessionFlow flow;
        PlayerHealth fixtureHealth;
        readonly List<ZombieController> actors = new List<ZombieController>(20);
        public IReadOnlyList<ZombieController> ActiveActors => actors;
        readonly List<string> summary = new List<string>();
        static readonly string[] CounterNames = {
            "Main Thread", "Render Thread", "GPU Frame Time", "GC Allocated In Frame",
            "Total Used Memory", "GC Used Memory", "Draw Calls Count", "Batches Count",
            "LastSignal.Zombie.AI", "LastSignal.Zombie.Perception", "LastSignal.Zombie.Navigation",
            "LastSignal.Zombie.Presentation", "LastSignal.Zombie.Attack", "Animator.Update", "Skinning.Update" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-s014Population")
                    new GameObject("S014 population measurement").AddComponent<S014PopulationCapture>().output = args[i + 1];
        }
        void Observe(string message, string trace, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++; }
        static void Require(bool value, string reason)
        { if (!value) throw new InvalidOperationException(reason); }
        IEnumerator Start()
        {
            if (output == null) yield break;
            Directory.CreateDirectory(output);
            Require(!File.Exists(Path.Combine(output, "result.txt")), "Existing evidence cannot be overwritten.");
            Application.logMessageReceived += Observe;
            var stack = new Stack<IEnumerator>(); stack.Push(Run()); Exception failure = null;
            while (stack.Count > 0)
            {
                bool next = false; object current = null;
                try { next = stack.Peek().MoveNext(); if (next) current = stack.Peek().Current; }
                catch (Exception e) { failure = e; break; }
                if (!next) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                if (current is IEnumerator nested) stack.Push(nested); else yield return current;
            }
            while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
            ClearActors();
            Application.logMessageReceived -= Observe;
            File.WriteAllText(Path.Combine(output, "result.txt"),
                (failure == null && errors == 0 ? "MEASURED — capture complete; not D139 acceptance" : "FAIL") +
                "\nErrors=" + errors + "\n" + failure);
            if (failure != null) Debug.LogException(failure);
            Application.Quit(failure == null && errors == 0 ? 0 : 1);
        }
        IEnumerator Run()
        {
            yield return null;
            flow = FindAnyObjectByType<SessionFlow>();
            Require(flow && flow.Player, "Production session missing."); flow.Resume();
            var args = Environment.GetCommandLineArgs(); bool low = Array.IndexOf(args, "-s014Low") >= 0;
            if (low)
            {
                var source = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
                Require(source, "URP required."); var clone = Instantiate(source);
                clone.shadowDistance = 25; clone.shadowCascadeCount = 1; clone.msaaSampleCount = 1;
                QualitySettings.renderPipeline = clone;
            }
            File.WriteAllText(Path.Combine(output, "environment.txt"),
                $"Unity={Application.unityVersion}\nBuildGUID={Application.buildGUID}\nDevelopment={Debug.isDebugBuild}\nEditor={Application.isEditor}\nOS={SystemInfo.operatingSystem}\nDevice={SystemInfo.deviceModel}\nCPU={SystemInfo.processorType}\nCPUCount={SystemInfo.processorCount}\nGPU={SystemInfo.graphicsDeviceName}\nRAM_MB={SystemInfo.systemMemorySize}\nGraphicsMemory_MB={SystemInfo.graphicsMemorySize}\nResolution={Screen.width}x{Screen.height}\nFOV=75\nQuality={(low ? "S013 Low candidate" : "PC")}\nVSync={QualitySettings.vSyncCount}\nTargetFrameRate={Application.targetFrameRate}\n" +
                "Fixture=stationary player at resident pickup local offset (0,0,65) sampled within 2m on resident NavMesh; production zombie grid facing target. PlayerHealth remains active; this fixture heals each nonlethal HealthChanged via RecoverHealth; no fairness/treatment claim. Existing actors disabled; fixture actors are not population ledger entries. No user saves read/written. Preroll=2s on every repetition; warmup=5s; sample=20s; repetitions=5 per count; no deep profiling. 2s binary CPU profile before warmup on first repetition only. 30 actors NOT_RUN.\n");
            summary.Add("count,repeat,active_count,frames,mean_ms,median_ms,p95_ms,p99_ms,over50ms,gc0,gc1,gc2,unity_start_bytes,unity_end_bytes,managed_start_bytes,managed_end_bytes,path_requests,contact_attempts");
            foreach (int count in new[] { 1, 10, 20 })
                for (int repeat = 0; repeat < 5; repeat++)
                    yield return Sample(count, repeat);
        }
        IEnumerator Sample(int count, int repeat)
        {
            ClearActors(); flow.ReturnToMenu(); yield return null;
            flow.BeginSession(); yield return null; flow.Resume();
            PrepareFixture(flow, count);
            string prefix = count + "-" + repeat;
            if (repeat == 0)
            {
                bool oldEnabled = Profiler.enabled, oldBinary = Profiler.enableBinaryLog;
                string oldFile = Profiler.logFile;
                try
                {
                    Profiler.logFile = Path.Combine(output, prefix + "-cpu.raw");
                    Profiler.enableBinaryLog = true; Profiler.enabled = true;
                    yield return new WaitForSeconds(2);
                }
                finally { Profiler.enabled = oldEnabled; Profiler.enableBinaryLog = oldBinary; Profiler.logFile = oldFile; }
            }
            else yield return new WaitForSeconds(2);
            yield return new WaitForSeconds(5);
            Require(errors == 0 && !flow.Paused, "Warmup invalid or paused.");
            var handles = new List<ProfilerRecorderHandle>(); ProfilerRecorderHandle.GetAvailable(handles);
            var recorders = new ProfilerRecorder[CounterNames.Length];
            var sums = new double[CounterNames.Length]; var samples = new int[CounterNames.Length];
            var frames = new double[24000]; int n = 0, over50 = 0;
            long unityStart = 0, managedStart = 0;
            int[] gc = new int[3];
            try
            {
                for (int j = 0; j < CounterNames.Length; j++)
                    foreach (var handle in handles)
                    {
                        var d = ProfilerRecorderHandle.GetDescription(handle);
                        if (d.Name != CounterNames[j]) continue;
                        recorders[j] = ProfilerRecorder.StartNew(d.Category, d.Name, 1); break;
                    }
                for (int j = 0; j < 3; j++) gc[j] = GC.CollectionCount(j);
                unityStart = Profiler.GetTotalAllocatedMemoryLong(); managedStart = Profiler.GetMonoUsedSizeLong();
                double start = Time.realtimeSinceStartupAsDouble, previous = start;
                while (Time.realtimeSinceStartupAsDouble - start < 20)
                {
                    yield return null;
                    Require(errors == 0 && !flow.Paused, "Sample interrupted or error logged.");
                    Require(n < frames.Length, "Frame sample capacity exceeded.");
                    double now = Time.realtimeSinceStartupAsDouble; double ms = (now - previous) * 1000; previous = now;
                    frames[n++] = ms; if (ms > 50) over50++;
                    for (int j = 0; j < recorders.Length; j++)
                        if (recorders[j].Valid && recorders[j].Count > 0) { sums[j] += recorders[j].LastValue; samples[j]++; }
                }
                Require(n > 0 && actors.Count == count, "Incomplete population/frame sample.");
                long unityEnd = Profiler.GetTotalAllocatedMemoryLong(), managedEnd = Profiler.GetMonoUsedSizeLong();
                int gc0 = GC.CollectionCount(0)-gc[0], gc1 = GC.CollectionCount(1)-gc[1], gc2 = GC.CollectionCount(2)-gc[2];
                int active = FindObjectsByType<ZombieController>(FindObjectsSortMode.None).Length;
                Require(active == count, "Uncontrolled production actor count changed.");
                int paths = 0, contacts = 0;
                foreach (var actor in actors) { Require(actor && actor.CanHear && !actor.IsDead, "Actor inactive."); paths += actor.Navigation.PathRequests; contacts += actor.ContactAttempts; }
                Require(contacts > 0, "No real production attack contacts during fixture lifetime.");
                var raw = new string[n + 1]; raw[0] = "frame_ms"; double total = 0;
                for (int i = 0; i < n; i++) { total += frames[i]; raw[i + 1] = frames[i].ToString("R", CultureInfo.InvariantCulture); }
                File.WriteAllLines(Path.Combine(output, prefix + "-frames.csv"), raw);
                Array.Sort(frames, 0, n);
                summary.Add(FormattableString.Invariant($"{count},{repeat},{active},{n},{total/n:F4},{frames[n/2]:F4},{frames[(int)(n*.95)]:F4},{frames[(int)(n*.99)]:F4},{over50},{gc0},{gc1},{gc2},{unityStart},{unityEnd},{managedStart},{managedEnd},{paths},{contacts}"));
                File.WriteAllLines(Path.Combine(output, "summary.csv"), summary);
                var counters = new List<string>();
                for (int j = 0; j < CounterNames.Length; j++)
                    counters.Add(CounterNames[j] + "=" + (samples[j] == 0 || (CounterNames[j] == "GPU Frame Time" && sums[j] <= 0) ? "UNAVAILABLE" : (sums[j] / samples[j]).ToString("R", CultureInfo.InvariantCulture)) + "; samples=" + samples[j]);
                counters.Add("Time counters in ns, memory in bytes, rendering counters in counts. Recorder mean includes all threads for matching marker. Timeline capture outside measured window.");
                File.WriteAllLines(Path.Combine(output, prefix + "-counters.txt"), counters);
            }
            finally { foreach (var recorder in recorders) recorder.Dispose(); }
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, prefix + ".png")); yield return null;
        }
        // Public only for the dedicated Editor validation of this Development-only fixture.
        // Caller owns a disposable session; no user session/save restoration is implied.
        public void PrepareFixture(SessionFlow owner, int count)
        {
            Require(owner && owner.Player && actors.Count == 0, "Fresh disposable session required.");
            Require(count == 1 || count == 10 || count == 20, "Supported fixture counts are 1/10/20.");
            foreach (var actor in FindObjectsByType<ZombieController>(FindObjectsSortMode.None))
            { actor.SetPaused(true); actor.gameObject.SetActive(false); }
            var player = owner.Player;
            var car = owner.GetComponent<LastSignal.Vehicles.VehicleWorld>().Actor;
            Require(car, "Resident vehicle anchor missing.");
            var prefab = Resources.Load<GameObject>("LS_Zombie_Runtime");
            Require(prefab, "Production zombie missing.");
            var agent = prefab.GetComponent<NavMeshAgent>();
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            Vector3 center = car.transform.TransformPoint(new Vector3(0, 0, 65));
            Require(NavMesh.SamplePosition(center, out var target, 2, filter), "Target NavMesh missing.");
            var capsule = player.GetComponent<CharacterController>(); capsule.enabled = false;
            player.transform.position = target.position + Vector3.up * .05f;
            player.transform.rotation = car.transform.rotation; capsule.enabled = true;
            player.GetComponent<FirstPersonMotor>().enabled = false;
            fixtureHealth = player.GetComponent<PlayerHealth>();
            Require(fixtureHealth && fixtureHealth.isActiveAndEnabled, "Active production target health required.");
            fixtureHealth.HealthChanged += HealFixtureTarget;
            var look = player.GetComponent<FirstPersonLook>(); look.enabled = false;
            look.View.fieldOfView = 75; look.View.transform.localRotation = Quaternion.identity;
            for (int i = 0; i < count; i++)
            {
                var desired = center + car.transform.TransformDirection(new Vector3((i % 5 - 2) * 1.6f, 0, 7 + i / 5 * 1.6f));
                Require(NavMesh.SamplePosition(desired, out var hit, 2, filter), "Actor NavMesh missing: " + i);
                var body = prefab.GetComponent<CapsuleCollider>();
                Require(body && body.direction == 1, "Vertical production capsule required.");
                Vector3 capsuleCenter = hit.position + body.center;
                float half = Mathf.Max(0, body.height * .5f - body.radius);
                var overlaps = Physics.OverlapCapsule(capsuleCenter + Vector3.up * half,
                    capsuleCenter - Vector3.up * half, body.radius * .95f, ~(1 << 2), QueryTriggerInteraction.Ignore);
                if (overlaps.Length > 0)
                {
                    string names = "";
                    foreach (var overlap in overlaps) names += overlap.name + " (" + overlap.bounds + ") ";
                    throw new InvalidOperationException("Spawn overlaps scene collider: " + i + " at " + hit.position + ": " + names);
                }
                foreach (var existing in actors)
                    Require(Vector3.Distance(existing.transform.position, hit.position) > body.radius * 2 + .1f,
                        "NavMesh correction collapsed fixture spacing.");
                var actor = Instantiate(prefab, hit.position, Quaternion.LookRotation(center - hit.position)).GetComponent<ZombieController>();
                actors.Add(actor); Require(actor.Initialize() && actor.Bind(player), "Production actor initialization failed.");
            }
            Physics.SyncTransforms();
        }
        void HealFixtureTarget()
        { if (fixtureHealth && fixtureHealth.IsAlive && fixtureHealth.CurrentHealth < fixtureHealth.MaxHealth) fixtureHealth.RecoverHealth(fixtureHealth.MaxHealth); }
        public void ClearFixture() => ClearActors();
        void ClearActors()
        { if (fixtureHealth) fixtureHealth.HealthChanged -= HealFixtureTarget; fixtureHealth = null;
          foreach (var actor in actors) if (actor) { actor.Shutdown(); actor.gameObject.SetActive(false); Destroy(actor.gameObject); } actors.Clear(); }
        void OnDestroy() { Application.logMessageReceived -= Observe; ClearActors(); }
    }
}
#endif
