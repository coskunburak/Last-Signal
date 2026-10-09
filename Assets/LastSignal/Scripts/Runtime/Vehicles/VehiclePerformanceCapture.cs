#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LastSignal.AI;
using LastSignal.WorldCells;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;

namespace LastSignal.Vehicles
{
    /// <summary>Opt-in player-build capture of the real production pickup and session.</summary>
    public sealed class VehiclePerformanceCapture : MonoBehaviour
    {
        public const float ActiveForwardSeconds = 8;
        string output;
        int errors;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-veh001Performance")
                    new GameObject("VEH-001 performance capture").AddComponent<VehiclePerformanceCapture>().output = args[i + 1];
        }
        void Observe(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
        }
        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        IEnumerator Start()
        {
            if (output == null) yield break;
            Directory.CreateDirectory(output);
            Application.logMessageReceived += Observe;
            Exception failure = null;
            var stack = new Stack<IEnumerator>(); stack.Push(Run());
            while (stack.Count > 0)
            {
                bool next = false; object current = null;
                try { next = stack.Peek().MoveNext(); if (next) current = stack.Peek().Current; }
                catch (Exception exception) { failure = exception; break; }
                if (!next) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                if (current is IEnumerator nested) stack.Push(nested);
                else yield return current;
            }
            while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
            Application.logMessageReceived -= Observe;
            File.WriteAllText(Path.Combine(output, "result.txt"),
                (failure == null && errors == 0 ? "PASS capture completed; performance budget and manual acceptance remain separate" : "FAIL") +
                "\nErrors=" + errors + "\n" + failure);
            if (failure != null) Debug.LogException(failure);
            Application.Quit(failure == null && errors == 0 ? 0 : 1);
        }
        IEnumerator Run()
        {
            yield return null;
            var flow = FindAnyObjectByType<SessionFlow>();
            Require(flow && flow.Player, "Production SessionFlow/player missing.");
            flow.Resume();
            var world = flow.GetComponent<VehicleWorld>();
            Require(world && world.Actor, "Production pickup missing.");
            var car = world.Actor;
            var player = flow.Player;
            var capsule = player.GetComponent<CharacterController>();
            var cells = flow.GetComponent<WorldCellManager>();
            var population = flow.GetComponent<WorldPopulationManager>();
            Require(capsule && cells && population, "Production authorities missing.");
            Require(cells.CurrentCell == "resident", "Pickup is outside the resident cell.");
            File.WriteAllText(Path.Combine(output, "environment.txt"),
                $"Unity={Application.unityVersion}\nDevelopment={Debug.isDebugBuild}\nOS={SystemInfo.operatingSystem}\nCPU={SystemInfo.processorType}\nGPU={SystemInfo.graphicsDeviceName}\nResolution={Screen.width}x{Screen.height}\nScene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().path}\n" +
                "Route=production resident pickup; 50 parked enter/exit cycles, 50 session spawn/end cycles, then 5 s drive warmup and 20 s Development control-port drive. Session lifecycle is not cell streaming. No cell portal traversal.\n");
            for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
            var lifecycle = new List<string> { "cycle,mono_used,unity_allocated,unity_reserved,vehicle_count,noise_listeners,cell_ready_listeners" };
            for (int i = 0; i < 50; i++)
            {
                capsule.enabled = false;
                player.transform.position = car.transform.TransformPoint(new Vector3(-2, 0, .15f)) + Vector3.up * .1f;
                capsule.enabled = true;
                Physics.SyncTransforms();
                Require(car.TryEnter(), "Enter failed at cycle " + i);
                Require(car.TryExit(), "Exit failed at cycle " + i);
                Require(player == flow.Player && world.Actor == car, "Player or pickup identity changed.");
                if ((i + 1) % 10 != 0) continue;
                yield return null;
                int vehicleCount = ActiveVehicles();
                lifecycle.Add($"{i + 1},{Profiler.GetMonoUsedSizeLong()},{Profiler.GetTotalAllocatedMemoryLong()},{Profiler.GetTotalReservedMemoryLong()},{vehicleCount},{flow.Noise.ListenerCount},{cells.ReadyListenerCount}");
                Require(vehicleCount == 1, "Vehicle instance leak after cycle " + (i + 1));
                Require(errors == 0, "Runtime error during enter/exit cycles.");
            }
            File.WriteAllLines(Path.Combine(output, "lifecycle.csv"), lifecycle);
            var sessions = new List<string> { "cycle,mono_used,unity_allocated,unity_reserved,vehicle_count,noise_listeners,cell_ready_listeners" };
            for (int i = 0; i < 50; i++)
            {
                flow.ReturnToMenu();
                yield return null;
                Require(ActiveVehicles() == 0 && flow.Noise == null, "Vehicle or noise authority survived session end " + i);
                flow.BeginSession();
                yield return null;
                flow.Resume();
                car = world.Actor;
                player = flow.Player;
                capsule = player ? player.GetComponent<CharacterController>() : null;
                Require(car && capsule && ActiveVehicles() == 1, "Session failed to restore one pickup at cycle " + i);
                if ((i + 1) % 10 != 0) continue;
                sessions.Add($"{i + 1},{Profiler.GetMonoUsedSizeLong()},{Profiler.GetTotalAllocatedMemoryLong()},{Profiler.GetTotalReservedMemoryLong()},{ActiveVehicles()},{flow.Noise.ListenerCount},{cells.ReadyListenerCount}");
                Require(errors == 0, "Runtime error during session lifecycle cycles.");
            }
            File.WriteAllLines(Path.Combine(output, "sessions.csv"), sessions);
            // Controlled paired benchmark: pause the distant persistent encounter in both runs.
            // The active variant then adds five real, bound production infected on the resident route.
            foreach (var existingActor in FindObjectsByType<ZombieController>()) existingActor.SetPaused(true);
            VehicleZombieInspection infectedFixture = null;
            bool activeInfected = Array.IndexOf(Environment.GetCommandLineArgs(), "-veh001ActiveInfected") >= 0;
            capsule.enabled = false;
            player.transform.position = car.transform.TransformPoint(new Vector3(-2, 0, .15f)) + Vector3.up * .1f;
            capsule.enabled = true;
            Physics.SyncTransforms();
            Require(car.TryEnter() && car.TryIgnition(), "Driving setup failed.");
            for (int settle = 0; settle < 40; settle++) yield return new WaitForFixedUpdate();
            Require(car.GetComponent<IVehiclePhysicsPort>().IsGrounded, "Pickup wheels must be grounded before capture.");
            try
            {
                car.SetDevelopmentControl(new VehicleControlIntent(0, 0, 0, false));
                yield return null; yield return null;
                car.SetDevelopmentControl(new VehicleControlIntent(1, 0, 0, false));
                yield return new WaitForSeconds(5);
                Require(errors == 0, "Runtime error during drive warmup.");
                // Both variants warm up on the same clear route. Spawn real active actors
                // ahead of the current pickup only now, so impacts belong to the measured window.
                if (activeInfected)
                {
                    infectedFixture = VehicleZombieInspection.Create(flow);
                    Require(infectedFixture.ActiveCount == 5, "Five active infected required.");
                }
                var captureActors = FindObjectsByType<ZombieController>();
                var names = new[] { "Main Thread", "Physics.Simulate", "GC Allocated In Frame", "LastSignal.Vehicle.Physics",
                    "LastSignal.Vehicle.Update", "LastSignal.Vehicle.Noise", "LastSignal.Vehicle.Wheels",
                    "LastSignal.Vehicle.SteeringVisual", "LastSignal.Vehicle.Audio", "LastSignal.Vehicle.Camera", "LastSignal.Vehicle.ZombieImpact", "LastSignal.Vehicle.BodyFeel", "LastSignal.Zombie.AI", "LastSignal.Zombie.Perception", "LastSignal.Zombie.Navigation", "LastSignal.Zombie.Hearing" };
                var recorders = new Dictionary<string, ProfilerRecorder>();
                var values = new Dictionary<string, List<long>>();
                var handles = new List<ProfilerRecorderHandle>();
                ProfilerRecorderHandle.GetAvailable(handles);
                foreach (var name in names) values.Add(name, new List<long>(30000));
                foreach (var handle in handles)
                {
                    var description = ProfilerRecorderHandle.GetDescription(handle);
                    if (values.ContainsKey(description.Name) && !recorders.ContainsKey(description.Name))
                        recorders.Add(description.Name, ProfilerRecorder.StartNew(description.Category, description.Name, 1));
                }
                var frames = new List<double>(30000);
                int[] collections = { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };
                long initialMemory = Profiler.GetTotalAllocatedMemoryLong();
                long initialNoise = population.LastNoiseSequence;
                int initialImpacts = car.ZombieImpactCount;
                int initialBody = car.BodyImpactCueCount, initialFront = car.FrontAxleCueCount, initialRear = car.RearAxleCueCount;
                Vector3 lastPosition = car.transform.position;
                var rigidbody = car.GetComponent<Rigidbody>();
                double distance = 0, maxSpeed = 0;
                float minimumUpDot = 1, maximumVerticalSpeed = 0;
                int minActiveAi = int.MaxValue, maxActiveAi = 0;
                double start = Time.realtimeSinceStartupAsDouble, previous = start;
                int segment = -1;
                try
                {
                    while (Time.realtimeSinceStartupAsDouble - start < 20)
                    {
                        double elapsed = Time.realtimeSinceStartupAsDouble - start;
                        // Active-infected: cross the group, then brake inside the resident
                        // lane. Continuous throttle reaches the East boundary within 20s.
                        // Normal: exercise throttle/brake/reverse/steering segments for
                        // lifecycle and control-port coverage.
                        int nextSegment = activeInfected ? (elapsed < ActiveForwardSeconds ? 0 : 1)
                            : elapsed < 6 ? 0 : elapsed < 9 ? 1 : elapsed < 13 ? 2 : elapsed < 16 ? 3 : 4;
                        if (nextSegment != segment)
                        {
                            segment = nextSegment;
                            var intent = segment == 0 ? new VehicleControlIntent(1, 0, 0, false) :
                                segment == 1 ? new VehicleControlIntent(0, 1, 0, false) :
                                segment == 2 ? new VehicleControlIntent(0, 0, 0, false, 1) :
                                segment == 3 ? new VehicleControlIntent(1, 0, -1, false) :
                                new VehicleControlIntent(1, 0, 1, false);
                            car.SetDevelopmentControl(intent);
                        }
                        yield return null;
                        double now = Time.realtimeSinceStartupAsDouble;
                        frames.Add((now - previous) * 1000); previous = now;
                        distance += Vector3.Distance(lastPosition, car.transform.position);
                        lastPosition = car.transform.position;
                        Require(float.IsFinite(rigidbody.linearVelocity.sqrMagnitude), "Nonfinite vehicle velocity.");
                        minimumUpDot = Mathf.Min(minimumUpDot, Vector3.Dot(car.transform.up, Vector3.up));
                        maximumVerticalSpeed = Mathf.Max(maximumVerticalSpeed, Mathf.Abs(rigidbody.linearVelocity.y));
                        maxSpeed = Math.Max(maxSpeed, rigidbody.linearVelocity.magnitude);
                        int activeAi = ActiveInfected(captureActors);
                        minActiveAi = Math.Min(minActiveAi, activeAi);
                        maxActiveAi = Math.Max(maxActiveAi, activeAi);
                        foreach (var pair in recorders)
                            if (pair.Value.Valid && pair.Value.Count > 0) values[pair.Key].Add(pair.Value.LastValue);
                        Require(errors == 0 && !flow.Paused && world.Actor == car && cells.CurrentCell == "resident",
                            "Driving capture invalidated by runtime state.");
                    }

                    if (infectedFixture) Require(minActiveAi > 0, "Active AI capture lost all infected.");
                    frames.Sort();
                    double sum = 0; foreach (var value in frames) sum += value;
                    var lines = new List<string> {
                        "frames=" + frames.Count,
                        "frame_avg_ms=" + (sum / frames.Count).ToString("F4", CultureInfo.InvariantCulture),
                        "frame_p50_ms=" + Percentile(frames, .50),
                        "frame_p95_ms=" + Percentile(frames, .95),
                        "frame_p99_ms=" + Percentile(frames, .99),
                        "distance_m=" + distance.ToString("F3", CultureInfo.InvariantCulture),
                        "max_speed_mps=" + maxSpeed.ToString("F3", CultureInfo.InvariantCulture),
                        "minimum_up_dot=" + minimumUpDot.ToString("F4", CultureInfo.InvariantCulture),
                        "maximum_vertical_speed_mps=" + maximumVerticalSpeed.ToString("F4", CultureInfo.InvariantCulture),
                        "memory_allocated_start_bytes=" + initialMemory,
                        "memory_allocated_end_bytes=" + Profiler.GetTotalAllocatedMemoryLong(),
                        "memory_reserved_end_bytes=" + Profiler.GetTotalReservedMemoryLong(),
                        "managed_used_end_bytes=" + Profiler.GetMonoUsedSizeLong(),
                        $"gc_collections_delta={GC.CollectionCount(0) - collections[0]},{GC.CollectionCount(1) - collections[1]},{GC.CollectionCount(2) - collections[2]}",
                        "pressure_noise_sequence_delta=" + (population.LastNoiseSequence - initialNoise),
                        "active_ai_count_min=" + minActiveAi,
                        "active_ai_count_max=" + maxActiveAi,
                        "active_ai_count_end=" + ActiveInfected(captureActors),
                        "infected_impact_count=" + car.ZombieImpactCount,
                        "infected_impact_count_during_capture=" + (car.ZombieImpactCount - initialImpacts),
                        "impact_contact_cache_end=" + car.ImpactContactCount,
                        $"body_feel_impact_front_rear={car.BodyImpactCueCount},{car.FrontAxleCueCount},{car.RearAxleCueCount}",
                        "body_feel_pending_end=" + car.PendingBodyPasses,
                        "impact_contact_details=" + car.DescribeImpactContacts(),
                        "streaming_transitions=0; resident-only scope; portal driving not implemented"
                    };
                    foreach (var name in names)
                    {
                        var samples = values[name];
                        if (samples.Count == 0) { lines.Add(name + "=UNAVAILABLE"); continue; }
                        long total = 0, max = 0;
                        foreach (long value in samples) { total += value; if (value > max) max = value; }
                        lines.Add(name + "_mean_raw=" + (total / samples.Count) + "; max_raw=" + max +
                            "; samples=" + samples.Count + "; time=ns,memory=bytes");
                    }
                    File.WriteAllLines(Path.Combine(output, "drive.txt"), lines);
                    Require(frames.Count > 0 && distance > 1, "Pickup did not complete a measurable drive.");
                    Require(car.ImpactContactCount == 0, "Impact contacts must be released at capture end.");
                    Require(car.PendingBodyPasses == 0, "Body traversal presentation must release all pending targets.");
                    Require(car.BodyImpactCueCount - initialBody == car.ZombieImpactCount - initialImpacts,
                        "Each committed infected impact must present exactly one body hit.");
                    if (infectedFixture) Require(car.FrontAxleCueCount > initialFront && car.RearAxleCueCount > initialRear,
                        "Active capture must include front and rear body traversal presentation.");
                    Require(minimumUpDot > 0, "Vehicle overturned during capture.");
                    if (infectedFixture) Require(car.ZombieImpactCount - initialImpacts >= 3, "Active-AI measured window requires at least three real infected impacts.");
                    yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(Path.Combine(output, "drive.png"));
                    yield return null;
                }
                finally { foreach (var recorder in recorders.Values) recorder.Dispose(); }
            }
            finally
            {
                car.SetDevelopmentControl(new VehicleControlIntent(0, 0, 0, false));
                car.SetDevelopmentControl(null);
                flow.ReturnToMenu();
            }
        }
        static string Percentile(List<double> sorted, double fraction)
        {
            int index = Math.Min(sorted.Count - 1, (int)(sorted.Count * fraction));
            return sorted[index].ToString("F4", CultureInfo.InvariantCulture);
        }
        static int ActiveInfected(ZombieController[] actors)
        {
            int count = 0;
            foreach (var actor in actors)
                if (actor && actor.isActiveAndEnabled && !actor.IsDead && !actor.Paused && actor.CanHear) count++;
            return count;
        }
        static int ActiveVehicles()
        {
            int count = 0;
            foreach (var actor in Resources.FindObjectsOfTypeAll<VehicleActor>())
                if (actor.gameObject.scene.IsValid() && actor.gameObject.activeInHierarchy) count++;
            return count;
        }
    }
}
#endif
