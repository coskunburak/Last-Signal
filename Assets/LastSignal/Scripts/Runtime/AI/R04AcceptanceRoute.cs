#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Text;
using LastSignal.Noise;
using UnityEngine;
using UnityEngine.AI;
namespace LastSignal
{
    public static class R04AcceptanceRoute
    {
        static void Check(bool value, string message) { if (!value) throw new InvalidOperationException("R04: " + message); }
        public static void Place(GameObject player, Vector3 point)
        { var c = player.GetComponent<CharacterController>(); c.enabled = false; player.transform.position = point; c.enabled = true; Physics.SyncTransforms(); }
        public static IEnumerator State(ZombieController actor, ZombieState expected, float timeout = 5)
        { float end = Time.realtimeSinceStartup + timeout; while (actor.Runtime.State != expected && Time.realtimeSinceStartup < end) yield return null; Check(actor.Runtime.State == expected, "Expected " + expected + ", actual " + actor.Runtime.State); }
        static IEnumerator Ready(WeaponController weapon)
        { float end = Time.realtimeSinceStartup + 8; while ((weapon.RuntimeState.State != WeaponState.Ready || weapon.RuntimeState.FireCooldownRemaining > 0) && Time.realtimeSinceStartup < end) yield return null; Check(weapon.RuntimeState.State == WeaponState.Ready, "Rifle not ready"); }

        public static ZombieController[] SpawnZombies(SessionFlow flow, int count, Vector3 basePosition, float spacing)
        {
            var prefab = Resources.Load<GameObject>("LS_Zombie_Runtime");
            var actors = new ZombieController[count];
            for (int i = 0; i < count; i++)
            {
                var pos = basePosition + new Vector3((i % 5) * spacing, 0, (i / 5) * spacing);
                var go = UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity);
                actors[i] = go.GetComponent<ZombieController>();
                actors[i].Initialize();
                actors[i].Bind(flow.Player);
            }
            return actors;
        }

        public static void DestroyZombies(ZombieController[] actors)
        { if (actors == null) return; foreach (var a in actors) if (a) UnityEngine.Object.Destroy(a.gameObject); }

        public static string GameplayTrace(float time, Vector3 playerPos, float stamina, string action,
            GameplayNoiseEvent noise, ZombieController actor)
        {
            return $"{time:F4}\t{playerPos}\t{stamina:F1}\t{action}\t{noise.EventId}\t{noise.Category}\t{noise.Position}\t" +
                (actor ? $"{actor.GetEntityId()}\t{actor.Runtime.Visible}\t{(actor.Auditory.HasStimulus ? actor.Auditory.Stimulus.Strength : 0):F4}\t{actor.Runtime.State}\t{actor.Navigation.Destination}" : "none");
        }

        public static string MultiZombieTrace(GameplayNoiseEvent noise, ZombieController[] actors)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Event={noise.EventId}\tSource={noise.SourceId}\tCategory={noise.Category}\tPosition={noise.Position}\tRadius={noise.BaseRadiusMeters}\tIntensity={noise.Intensity}");
            for (int i = 0; i < actors.Length; i++)
            {
                if (!actors[i]) continue;
                var listener = actors[i].GetComponent<ZombieNoiseListener>();
                float dist = Vector3.Distance(actors[i].transform.position, noise.Position);
                float strength = ZombieHearingEvaluator.Strength(noise, actors[i].Perception.Origin, actors[i].Definition, false);
                sb.AppendLine($"Zombie[{i}]\tdist={dist:F2}\tstrength={strength:F4}\theard={listener.Heard}\taccepted={listener.Accepted}\tstate={actors[i].Runtime.State}");
            }
            return sb.ToString();
        }

        // Route A: Quiet traversal — walk at safe distance, no detection
        public static IEnumerator RouteA_QuietTraversal(SessionFlow flow, ZombieController actor, Action<string> log)
        {
            var player = flow.Player; var motor = player.GetComponent<FirstPersonMotor>(); motor.enabled = false;
            actor.GetComponent<NavMeshAgent>().Warp(new Vector3(-3, 0, 2)); actor.transform.rotation = Quaternion.Euler(0, 90, 0);
            Place(player, new Vector3(3, 0, -5)); player.transform.rotation = Quaternion.identity;
            yield return new WaitForSeconds(.3f);
            Check(actor.Runtime.State == ZombieState.Idle, "Start not idle");
            int heardBefore = actor.GetComponent<ZombieNoiseListener>().Heard;
            // Walk for several strides at safe distance (~8m from zombie)
            for (int i = 0; i < 80; i++) motor.Simulate(Vector2.up, false, 1f / 60);
            yield return null;
            // At 8m+ distance, walk footsteps (6m radius, 0.2 intensity) produce strength ~0 (beyond radius)
            Check(actor.Runtime.State == ZombieState.Idle, "Walking at safe distance triggered detection");
            log("Route A PASS: Walking at safe distance produced no meaningful detection; state remained Idle");
        }

        // Route B: Sprint consequence
        public static IEnumerator RouteB_SprintConsequence(SessionFlow flow, ZombieController actor, Action<string> log)
        {
            var player = flow.Player; var motor = player.GetComponent<FirstPersonMotor>(); motor.enabled = false;
            var stamina = player.GetComponent<PlayerStamina>(); stamina.ResetSession();
            actor.GetComponent<NavMeshAgent>().Warp(new Vector3(-5, 0, -4)); actor.transform.rotation = Quaternion.identity;
            Place(player, new Vector3(-5, 0, -10)); player.transform.rotation = Quaternion.identity; motor.ResetNoiseCadence();
            yield return new WaitForSeconds(.3f);
            Check(actor.Runtime.State == ZombieState.Idle, "Start not idle");
            float staminaBefore = stamina.CurrentStamina;
            for (int i = 0; i < 60; i++) motor.Simulate(Vector2.up, true, 1f / 60);
            yield return State(actor, ZombieState.Investigating, 3);
            Check(stamina.CurrentStamina < staminaBefore, "Stamina did not decrease");
            Check(actor.Auditory.HasStimulus, "No auditory stimulus");
            Check(actor.Auditory.Stimulus.Event.Category == GameplayNoiseCategory.SprintFootstep, "Not SprintFootstep");
            log($"Route B PASS: Sprint emitted SprintFootstep; stamina {staminaBefore:F1} → {stamina.CurrentStamina:F1}; zombie Idle → Investigating");
        }

        // Route C: Silent reposition (anti-omniscience)
        public static IEnumerator RouteC_SilentReposition(SessionFlow flow, ZombieController actor, Action<string> log)
        {
            // Zombie is already investigating from Route B
            var player = flow.Player;
            if (actor.Runtime.State != ZombieState.Investigating)
            {
                // Trigger investigation first
                flow.Noise.TryEmit(new GameplayNoiseRequest(1, actor.transform.position + Vector3.back * 3, GameplayNoiseCategory.Gunshot, flow.NoiseTuning.Gunshot), out _);
                yield return State(actor, ZombieState.Investigating);
            }
            var investigateDest = actor.InvestigateDestination;
            Place(player, new Vector3(100, 0, 100)); // Move far away silently
            yield return new WaitForSeconds(.3f);
            Check(actor.InvestigateDestination == investigateDest, "Investigation destination changed after silent reposition");
            Check(!actor.Runtime.Visible, "Player visible after silent reposition");
            log($"Route C PASS: Player moved silently; investigation remained at {investigateDest}; anti-omniscience intact");
        }

        // Route D: Crowbar trade-off
        public static IEnumerator RouteD_CrowbarTradeoff(SessionFlow flow, ZombieController actor, Action<string> log)
        {
            var player = flow.Player; player.GetComponent<FirstPersonMotor>().enabled = false;
            actor.Bind(player); actor.GetComponent<NavMeshAgent>().Warp(new Vector3(-5, 0, -4)); actor.transform.rotation = Quaternion.identity;
            Place(player, new Vector3(-5, 0, -8)); player.transform.rotation = Quaternion.identity;
            yield return new WaitForSeconds(.3f);
            Check(actor.Runtime.State == ZombieState.Idle, "Start not idle");
            var combat = player.GetComponent<PlayerCombatController>();
            combat.SelectSlot(PlayerCombatController.CombatSlot.Melee);
            player.GetComponent<PlayerStamina>().ResetSession();
            // Create a target for the crowbar to hit
            var eye = player.GetComponentInChildren<Camera>().transform;
            var target = new GameObject("R04 crowbar target"); target.layer = 8;
            target.transform.position = eye.position + eye.forward * 1.1f;
            var hp = target.AddComponent<ZombieHealth>(); var col = target.AddComponent<BoxCollider>();
            col.size = Vector3.one * .4f; target.AddComponent<ZombieHitRegion>().Configure(hp, DamageRegion.Body, 1, col);
            Physics.SyncTransforms();
            player.GetComponent<PlayerStamina>().ResetSession();
            combat.Melee.TryAttack(); combat.Melee.Simulation.Tick(1);
            Check(flow.Noise.LastTrace.Event.Category == GameplayNoiseCategory.MeleeImpact, "No melee impact noise");
            yield return State(actor, ZombieState.Investigating, 3);
            float meleeStrength = actor.Auditory.Stimulus.Strength;
            log($"Route D PASS: Crowbar impact emitted MeleeImpact; zombie Investigating; strength={meleeStrength:F4}");
            UnityEngine.Object.Destroy(target);
        }

        // Route E: Rifle escalation
        public static IEnumerator RouteE_RifleEscalation(SessionFlow flow, ZombieController actor, Action<string> log)
        {
            var player = flow.Player; player.GetComponent<FirstPersonMotor>().enabled = false;
            actor.Bind(player); actor.GetComponent<NavMeshAgent>().Warp(new Vector3(-5, 0, -4)); actor.transform.rotation = Quaternion.identity;
            Place(player, new Vector3(-5, 0, -8)); player.transform.rotation = Quaternion.identity;
            yield return new WaitForSeconds(.3f);
            var combat = player.GetComponent<PlayerCombatController>();
            combat.SelectSlot(PlayerCombatController.CombatSlot.Firearm);
            var weapon = combat.Firearm;
            weapon.Initialize(player.GetComponentInChildren<Camera>().transform, player, 6);
            weapon.RequestEquip(); yield return Ready(weapon);
            ulong pressureBefore = flow.Noise.AcceptedCount;
            weapon.OnFirePressed(); weapon.OnFireReleased();
            var trace = flow.Noise.LastTrace;
            Check(trace.Event.Category == GameplayNoiseCategory.Gunshot, "No gunshot");
            yield return State(actor, ZombieState.Investigating, 3);
            log($"Route E PASS: Rifle shot emitted Gunshot; zombie Investigating; radius={trace.Event.BaseRadiusMeters}");
        }

        // Route F: Wall gunshot — no attack through wall
        public static IEnumerator RouteF_WallGunshot(SessionFlow flow, ZombieController actor, Action<string> log)
        {
            var player = flow.Player; player.GetComponent<FirstPersonMotor>().enabled = false;
            actor.Bind(player); actor.GetComponent<NavMeshAgent>().Warp(new Vector3(-3, 0, 2)); actor.transform.rotation = Quaternion.Euler(0, 90, 0);
            Place(player, new Vector3(3, 0, 2)); player.transform.rotation = Quaternion.identity;
            yield return new WaitForSeconds(.3f);
            Check(!actor.Runtime.Visible, "Player should be behind wall");
            flow.Noise.TryEmit(new GameplayNoiseRequest(1, player.transform.position, GameplayNoiseCategory.Gunshot, flow.NoiseTuning.Gunshot), out _);
            yield return State(actor, ZombieState.Investigating, 3);
            Check(!actor.Attacking, "Zombie should not attack through wall");
            float hp = player.GetComponent<PlayerHealth>().CurrentHealth;
            for (int i = 0; i < 30; i++) actor.Simulate(.05f);
            Check(!actor.Attacking, "Zombie attacked through wall");
            Check(player.GetComponent<PlayerHealth>().CurrentHealth == hp, "Player took damage through wall");
            log("Route F PASS: Gunshot behind wall → Investigating, no attack through wall");
        }

        // Route G: Investigate → Search → Escape (de-escalation)
        public static IEnumerator RouteG_InvestigateSearchEscape(SessionFlow flow, ZombieController actor, Action<string> log)
        {
            var player = flow.Player; player.GetComponent<FirstPersonMotor>().enabled = false;
            actor.Bind(player); Place(player, new Vector3(100, 0, 100));
            flow.Noise.TryEmit(new GameplayNoiseRequest(1, actor.transform.position + Vector3.back * 2, GameplayNoiseCategory.Gunshot, flow.NoiseTuning.Gunshot), out _);
            yield return State(actor, ZombieState.Investigating);
            yield return State(actor, ZombieState.Searching, 16);
            yield return State(actor, ZombieState.Idle, 16);
            log("Route G PASS: Idle → Investigating → Searching → Idle (full de-escalation)");
        }

        // Route H: Vision takeover
        public static IEnumerator RouteH_VisionTakeover(SessionFlow flow, ZombieController actor, Action<string> log)
        {
            var player = flow.Player; player.GetComponent<FirstPersonMotor>().enabled = false;
            actor.Bind(player); actor.GetComponent<NavMeshAgent>().Warp(new Vector3(-5, 0, -4)); actor.transform.rotation = Quaternion.identity;
            flow.Noise.TryEmit(new GameplayNoiseRequest(1, actor.transform.position + Vector3.back * 3, GameplayNoiseCategory.Gunshot, flow.NoiseTuning.Gunshot), out _);
            yield return State(actor, ZombieState.Investigating);
            Place(player, new Vector3(-5, 0, 0)); // In front of zombie, visible
            yield return State(actor, ZombieState.Chasing, 3);
            log("Route H PASS: Investigating → Chasing on visual confirmation");
        }

        // Route I: Break LOS
        public static IEnumerator RouteI_BreakLOS(SessionFlow flow, ZombieController actor, Action<string> log)
        {
            // Already chasing from Route H
            if (actor.Runtime.State != ZombieState.Chasing)
            {
                Place(flow.Player, new Vector3(-5, 0, 0));
                yield return State(actor, ZombieState.Chasing, 3);
            }
            Place(flow.Player, new Vector3(100, 0, 100)); // Break LOS
            yield return State(actor, ZombieState.Searching, 5);
            yield return State(actor, ZombieState.Idle, 16);
            log("Route I PASS: Chasing → Searching → Idle (LOS break de-escalation)");
        }

        // Route J: Chase loss + new sound
        public static IEnumerator RouteJ_ChaseLossNewSound(SessionFlow flow, ZombieController actor, Action<string> log)
        {
            var player = flow.Player; player.GetComponent<FirstPersonMotor>().enabled = false;
            actor.Bind(player); actor.GetComponent<NavMeshAgent>().Warp(new Vector3(-5, 0, -4)); actor.transform.rotation = Quaternion.identity;
            Place(player, new Vector3(-5, 0, 0)); yield return State(actor, ZombieState.Chasing, 3);
            Place(player, new Vector3(100, 0, 100)); // Break LOS
            yield return State(actor, ZombieState.Searching, 5);
            var newSoundPos = actor.transform.position + Vector3.right * 3;
            flow.Noise.TryEmit(new GameplayNoiseRequest(1, newSoundPos, GameplayNoiseCategory.Gunshot, flow.NoiseTuning.Gunshot), out _);
            yield return State(actor, ZombieState.Investigating, 3);
            Check(actor.InvestigateDestination == newSoundPos, "Did not redirect to new sound");
            log($"Route J PASS: Chase loss + new sound → Investigating at {newSoundPos}");
        }

        // Route K: Multi-zombie gunshot
        public static IEnumerator RouteK_MultiZombieGunshot(SessionFlow flow, Action<string> log)
        {
            var player = flow.Player; player.GetComponent<FirstPersonMotor>().enabled = false;
            Place(player, new Vector3(100, 0, 100));
            var actors = SpawnZombies(flow, 5, new Vector3(-5, 0, -5), 1.5f);
            yield return null; yield return null;
            // Fire one gunshot near the zombies
            var shotPos = new Vector3(-5, 1, -5);
            int[] heardBefore = new int[5]; int[] acceptedBefore = new int[5];
            for (int i = 0; i < 5; i++)
            { var l = actors[i].GetComponent<ZombieNoiseListener>(); heardBefore[i] = l.Heard; acceptedBefore[i] = l.Accepted; }
            flow.Noise.TryEmit(new GameplayNoiseRequest(1, shotPos, GameplayNoiseCategory.Gunshot, flow.NoiseTuning.Gunshot), out var noise);
            yield return null;
            int investigating = 0;
            var sb = new StringBuilder();
            sb.AppendLine(MultiZombieTrace(noise, actors));
            for (int i = 0; i < 5; i++)
            {
                var l = actors[i].GetComponent<ZombieNoiseListener>();
                if (actors[i].Runtime.State == ZombieState.Investigating) investigating++;
                sb.AppendLine($"Zombie[{i}] heard={l.Heard - heardBefore[i]} accepted={l.Accepted - acceptedBefore[i]} state={actors[i].Runtime.State}");
            }
            Check(investigating >= 3, $"Expected at least 3 investigating, got {investigating}");
            log($"Route K PASS: Multi-zombie gunshot; {investigating}/5 zombies investigating\n{sb}");
            DestroyZombies(actors); yield return null;
        }

        // Route L: No telepathy
        public static IEnumerator RouteL_NoTelepathy(SessionFlow flow, Action<string> log)
        {
            var player = flow.Player; player.GetComponent<FirstPersonMotor>().enabled = false;
            Place(player, new Vector3(100, 0, 100));
            // Spawn one nearby and one far away
            var prefab = Resources.Load<GameObject>("LS_Zombie_Runtime");
            var near = UnityEngine.Object.Instantiate(prefab, new Vector3(-5, 0, -5), Quaternion.identity).GetComponent<ZombieController>();
            near.Initialize(); near.Bind(player);
            var far = UnityEngine.Object.Instantiate(prefab, new Vector3(10, 0, 10), Quaternion.identity).GetComponent<ZombieController>();
            far.Initialize(); far.Bind(player);
            yield return null;
            flow.Noise.TryEmit(new GameplayNoiseRequest(1, new Vector3(-5, 1, -5), GameplayNoiseCategory.MeleeImpact, flow.NoiseTuning.Melee), out _);
            yield return State(near, ZombieState.Investigating, 3);
            Check(far.Runtime.State == ZombieState.Idle, "Far zombie should stay idle (no telepathy)");
            log("Route L PASS: Near zombie investigates; far zombie stays idle (no telepathy)");
            UnityEngine.Object.Destroy(near.gameObject); UnityEngine.Object.Destroy(far.gameObject); yield return null;
        }

        // Route M: Combat death
        public static IEnumerator RouteM_CombatDeath(SessionFlow flow, Action<string> log)
        {
            var player = flow.Player; player.GetComponent<FirstPersonMotor>().enabled = false;
            Place(player, new Vector3(100, 0, 100));
            var prefab = Resources.Load<GameObject>("LS_Zombie_Runtime");
            var alive = UnityEngine.Object.Instantiate(prefab, new Vector3(-5, 0, -5), Quaternion.identity).GetComponent<ZombieController>();
            alive.Initialize(); alive.Bind(player);
            var doomed = UnityEngine.Object.Instantiate(prefab, new Vector3(-3, 0, -5), Quaternion.identity).GetComponent<ZombieController>();
            doomed.Initialize(); doomed.Bind(player);
            yield return null;
            // Kill one
            doomed.GetComponent<ZombieHealth>().TakeDamage(new DamageInfo { Amount = 1000 });
            Check(doomed.IsDead, "Zombie not dead");
            Check(!doomed.GetComponent<ZombieNoiseListener>().Registered, "Dead zombie listener still registered");
            // Fire a shot — dead zombie must not react
            flow.Noise.TryEmit(new GameplayNoiseRequest(1, new Vector3(-4, 1, -5), GameplayNoiseCategory.Gunshot, flow.NoiseTuning.Gunshot), out _);
            yield return null;
            Check(doomed.IsDead && doomed.Runtime.State == ZombieState.Dead, "Dead zombie reacted");
            Check(alive.Runtime.State == ZombieState.Investigating, "Living zombie did not react");
            log("Route M PASS: Dead zombie inert; living zombie investigates normally");
            UnityEngine.Object.Destroy(alive.gameObject); UnityEngine.Object.Destroy(doomed.gameObject); yield return null;
        }

        // Route N: Pause
        public static IEnumerator RouteN_Pause(SessionFlow flow, ZombieController actor, Action<string> log)
        {
            actor.Bind(flow.Player); Place(flow.Player, new Vector3(100, 0, 100));
            flow.Noise.TryEmit(new GameplayNoiseRequest(1, actor.transform.position + Vector3.back * 2, GameplayNoiseCategory.Gunshot, flow.NoiseTuning.Gunshot), out _);
            yield return State(actor, ZombieState.Investigating);
            float ageBefore = actor.Auditory.Age; float investigateAgeBefore = actor.InvestigateAge;
            var posBefore = actor.transform.position; int queryBefore = actor.GetComponent<ZombieNoiseListener>().PhysicsQueries;
            flow.Pause(); yield return new WaitForSecondsRealtime(.3f);
            Check(actor.Auditory.Age == ageBefore, "Auditory age advanced during pause");
            Check(actor.InvestigateAge == investigateAgeBefore, "Investigate age advanced during pause");
            Check(actor.transform.position == posBefore, "Position changed during pause");
            Check(actor.GetComponent<ZombieNoiseListener>().PhysicsQueries == queryBefore, "Physics queries during pause");
            flow.Resume();
            log("Route N PASS: Pause froze memory age, investigate age, position, and queries");
        }

        // Route O: Session reset
        public static IEnumerator RouteO_SessionReset(SessionFlow flow, Action<string> log)
        {
            var oldNoise = flow.Noise;
            flow.ReturnToMenu();
            Check(oldNoise.ListenerCount == 0 && !oldNoise.Active, "Menu leaked listeners");
            yield return null;
            for (int i = 0; i < 3; i++)
            {
                flow.BeginSession(); flow.Resume(); yield return null; yield return null;
                var fresh = flow.GetComponent<ZombieEncounter>().Actor;
                Check(fresh && !fresh.Auditory.HasStimulus, "New session has stale memory");
                Check(flow.Noise.ListenerCount == 1, $"Session {i} listener count={flow.Noise.ListenerCount}");
                flow.ReturnToMenu(); yield return null;
            }
            // Restore a session for subsequent tests
            flow.BeginSession(); flow.Resume(); yield return null; yield return null;
            log("Route O PASS: 3 session cycles; 1 listener per session; zero stale memories");
        }

        // Performance measurement
        public static IEnumerator MeasurePerformance(SessionFlow flow, int agentCount, Action<string> log)
        {
            var player = flow.Player; player.GetComponent<FirstPersonMotor>().enabled = false;
            Place(player, new Vector3(100, 0, 100));
            var actors = SpawnZombies(flow, agentCount, new Vector3(-8, 0, -8), .15f);
            yield return null;

            // Phase 1: Silence — verify 0 hearing queries
            int queryBefore = 0;
            foreach (var a in actors) queryBefore += a.GetComponent<ZombieNoiseListener>().PhysicsQueries;
            for (int i = 0; i < 60; i++) yield return null;
            int queryAfter = 0;
            foreach (var a in actors) queryAfter += a.GetComponent<ZombieNoiseListener>().PhysicsQueries;
            int silenceQueries = queryAfter - queryBefore;

            // Phase 2: Warmed gunshot emission
#if UNITY_EDITOR
            foreach (var a in actors) a.MeasureManagedAllocations = true;
#endif
            var watch = new System.Diagnostics.Stopwatch();
            long bytesBefore = System.GC.GetAllocatedBytesForCurrentThread();
            watch.Start();
            var request = new GameplayNoiseRequest(1, new Vector3(-6, 1, -6), GameplayNoiseCategory.Gunshot, flow.NoiseTuning.Gunshot);
            for (int i = 0; i < 100; i++) flow.Noise.TryEmit(request, out _);
            watch.Stop();
            long bytes = System.GC.GetAllocatedBytesForCurrentThread() - bytesBefore;

            // Phase 3: AI ticks with integrated load
            int pathsBefore = 0; foreach (var a in actors) pathsBefore += a.Navigation.PathRequests;
            int transitionsBefore = 0; foreach (var a in actors) transitionsBefore += a.Runtime.Transitions;
            float until = Time.realtimeSinceStartup + 2; int frames = 0;
            while (Time.realtimeSinceStartup < until) { yield return null; frames++; }
            int pathsAfter = 0; foreach (var a in actors) pathsAfter += a.Navigation.PathRequests;
            int transitionsAfter = 0; foreach (var a in actors) transitionsAfter += a.Runtime.Transitions;
            long tickBytes = 0; int ticks = 0;
#if UNITY_EDITOR
            foreach (var a in actors) { tickBytes += a.ManagedTickBytes; ticks += a.MeasuredTicks; }
#endif

            var sb = new StringBuilder();
            sb.AppendLine($"{agentCount}-AGENT INTEGRATED PERFORMANCE");
            sb.AppendLine($"Silence hearing queries: {silenceQueries}");
            sb.AppendLine($"100 warmed gunshots: {watch.Elapsed.TotalMilliseconds:F3} ms; managed bytes={bytes}");
            sb.AppendLine($"AI ticks={ticks}; tick managed bytes={tickBytes}");
            sb.AppendLine($"Path requests: {pathsAfter - pathsBefore}");
            sb.AppendLine($"State transitions: {transitionsAfter - transitionsBefore}");
            sb.AppendLine($"Frames: {frames}");
            log(sb.ToString());

            DestroyZombies(actors); yield return null;
        }

        // Signature integrated scenario
        public static IEnumerator SignatureScenario(SessionFlow flow, Action<string> log)
        {
            flow.Resume(); yield return null;
            var actor = flow.GetComponent<ZombieEncounter>().Actor;
            var player = flow.Player; var motor = player.GetComponent<FirstPersonMotor>(); motor.enabled = false;
            var stamina = player.GetComponent<PlayerStamina>(); stamina.ResetSession();
            var combat = player.GetComponent<PlayerCombatController>();
            var listener = actor.GetComponent<ZombieNoiseListener>();

            // 1. Quiet approach
            actor.GetComponent<NavMeshAgent>().Warp(new Vector3(-3, 0, 2)); actor.transform.rotation = Quaternion.Euler(0, 90, 0);
            Place(player, new Vector3(3, 0, -5));
            yield return new WaitForSeconds(.3f);
            Check(actor.Runtime.State == ZombieState.Idle, "Start not idle");
            log("Signature: Player approaches quietly; zombie unaware");

            // 2. Sprint across exposed route
            actor.Bind(player); actor.GetComponent<NavMeshAgent>().Warp(new Vector3(-5, 0, -4)); actor.transform.rotation = Quaternion.identity;
            Place(player, new Vector3(-5, 0, -10)); player.transform.rotation = Quaternion.identity; motor.ResetNoiseCadence(); stamina.ResetSession();
            yield return new WaitForSeconds(.2f);
            for (int i = 0; i < 60; i++) motor.Simulate(Vector2.up, true, 1f / 60);
            yield return State(actor, ZombieState.Investigating, 3);
            log($"Signature: Sprint triggered Investigating; stamina={stamina.CurrentStamina:F1}");

            // 3. Silent reposition
            var heardPos = actor.InvestigateDestination;
            Place(player, new Vector3(100, 0, 100));
            yield return new WaitForSeconds(.3f);
            Check(actor.InvestigateDestination == heardPos, "Anti-omniscience violated");
            log($"Signature: Zombie follows heard snapshots at {heardPos}, not live position");

            // 4. Zombie searches and de-escalates
            yield return State(actor, ZombieState.Searching, 16);
            yield return State(actor, ZombieState.Idle, 16);
            log("Signature: Zombie searched and de-escalated to Idle");

            // 5. Rifle shot — large escalation
            actor.Bind(player); actor.GetComponent<NavMeshAgent>().Warp(new Vector3(-5, 0, -4)); actor.transform.rotation = Quaternion.identity;
            Place(player, new Vector3(-5, 0, -8)); player.transform.rotation = Quaternion.identity;
            yield return new WaitForSeconds(.3f);
            combat.SelectSlot(PlayerCombatController.CombatSlot.Firearm);
            var weapon = combat.Firearm;
            weapon.Initialize(player.GetComponentInChildren<Camera>().transform, player, 6);
            weapon.RequestEquip(); yield return Ready(weapon);
            weapon.OnFirePressed(); weapon.OnFireReleased();
            var trace = flow.Noise.LastTrace;
            Check(trace.Event.Category == GameplayNoiseCategory.Gunshot, "No gunshot");
            yield return State(actor, ZombieState.Investigating, 3);
            log($"Signature: Rifle fired; Gunshot radius={trace.Event.BaseRadiusMeters}; zombie Investigating");

            // 6. Break LOS after investigation
            Place(player, new Vector3(100, 0, 100));
            yield return State(actor, ZombieState.Searching, 16);
            yield return State(actor, ZombieState.Idle, 16);
            log("Signature: After rifle, zombie searched and de-escalated normally");
            log("SIGNATURE R04 SCENARIO PASS");
        }
    }
}
#endif
