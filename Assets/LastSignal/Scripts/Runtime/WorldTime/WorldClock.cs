using System;
using System.Collections;
using LastSignal.Shelter;
using UnityEngine;

namespace LastSignal.WorldTime
{
    public enum SleepRejection { None, NotAtBed, ThreatNearby, Dead, InvalidDuration, TransitionInProgress, UnsafeState }
    public readonly struct SleepRequest
    {
        public readonly SleepRejection Reason;
        public bool Accepted => Reason == SleepRejection.None;
        public SleepRequest(SleepRejection reason) { Reason = reason; }
    }
    [DisallowMultipleComponent, RequireComponent(typeof(SessionFlow))]
    public sealed class WorldClock : MonoBehaviour, IWorldTimeParticipant
    {
        [SerializeField] WorldTimeSettings settings = new WorldTimeSettings();
        [SerializeField] WorldTimePresentation presentation;
        [SerializeField, Min(1)] float threatRadius = 20;
        [SerializeField] LayerMask threatLayers = 1 << 8; // Existing EnemyHitRegion layer; exclude environment colliders.
        [SerializeField, Min(.01f)] float exposureDebounce = .25f;
        [SerializeField, Range(0,1)] float rainProtection;
        readonly RaycastHit[] roofHits = new RaycastHit[32];
        readonly Collider[] threats = new Collider[128];
        SessionFlow flow;
        PlayerHealth health;
        PlayerInputReader input;
        ShelterLoop shelter;
        RestPoint sleepingAt;
        float pollElapsed, pendingElapsed;
        bool pendingProtection, protectedFromRain;
        Coroutine sleepRoutine;
        Action<double> recover;
        public WorldSimulation Simulation { get; private set; }
        public SessionFlow Flow => flow ? flow : flow = GetComponent<SessionFlow>();
        public bool Sleeping { get; private set; }
        public bool ProtectedFromRain => protectedFromRain;
        public double Exposure => protectedFromRain ? 0 : 1;
        public SleepRejection LastRejection { get; private set; }
        public AdvanceResult LastSleep { get; private set; }
        public string Feedback { get; private set; } = "";
        public void Configure(WorldTimePresentation view) => presentation = view;
        public void SetRainProtection(float protection)
        {
            if (!float.IsFinite(protection) || protection < 0 || protection > 1) throw new ArgumentOutOfRangeException(nameof(protection));
            rainProtection = protection;
        }
        public void Begin()
        {
            End();
            if (!float.IsFinite(threatRadius) || threatRadius <= 0 || !float.IsFinite(exposureDebounce) || exposureDebounce <= 0 || threatLayers.value == 0)
                throw new InvalidOperationException("Invalid threat/exposure authoring.");
            SetRainProtection(rainProtection);
            Simulation = new WorldSimulation(settings); flow = GetComponent<SessionFlow>();
            health = Flow.Player.GetComponent<PlayerHealth>(); input = Flow.Player.GetComponent<PlayerInputReader>();
            shelter = GetComponent<ShelterLoop>(); recover = Recover;
            Simulation.Register(this); protectedFromRain = pendingProtection = QueryRoof();
            LastRejection = SleepRejection.None; Feedback = ""; LastSleep = default;
            if (presentation) presentation.Bind(this);
        }
        public void End()
        {
            if (sleepRoutine != null) StopCoroutine(sleepRoutine);
            sleepRoutine = null; Sleeping = false; sleepingAt = null; Simulation = null;
            health = null; input = null; recover = null; pollElapsed = pendingElapsed = 0;
            if (presentation) presentation.Unbind();
        }
        void OnDisable() => End();
        void Update()
        {
            if (Simulation == null || !Flow.Player || Sleeping || Flow.Paused || Flow.Restoring || Flow.PlayerDead) return;
            PollExposure(Time.unscaledDeltaTime);
            Simulation.TickRealSeconds(Time.deltaTime, false, Exposure, rainProtection);
        }
        public void PollExposure(float realSeconds)
        {
            if (realSeconds < 0 || float.IsNaN(realSeconds) || float.IsInfinity(realSeconds)) throw new ArgumentOutOfRangeException(nameof(realSeconds));
            pollElapsed += realSeconds;
            if (pollElapsed < .1f) return;
            float elapsed = pollElapsed; pollElapsed = 0;
            bool covered = QueryRoof();
            if (covered != pendingProtection) { pendingProtection = covered; pendingElapsed = 0; }
            else pendingElapsed += elapsed;
            if (pendingElapsed >= exposureDebounce) protectedFromRain = pendingProtection;
        }
        public void RefreshExposure() { protectedFromRain = pendingProtection = QueryRoof(); pendingElapsed = pollElapsed = 0; }
        public bool QueryRoof()
        {
            if (!Flow.Player) return false;
            var position = Flow.Player.transform.position + Vector3.up * 1.5f;
            int count = Physics.RaycastNonAlloc(position, Vector3.up, roofHits, 30, ~((1 << 8) | (1 << 2)), QueryTriggerInteraction.Ignore);
            // Overflow is conservatively exposed; never award shelter based on a truncated query.
            if (count == roofHits.Length) return false;
            for (int i = 0; i < count; i++) if (roofHits[i].collider.GetComponentInParent<RainRoof>()) return true;
            return false;
        }
        public bool ThreatNearby()
        {
            if (!Flow.Player) return false;
            int count = Physics.OverlapSphereNonAlloc(Flow.Player.transform.position, threatRadius, threats, threatLayers, QueryTriggerInteraction.Collide);
            if (count == threats.Length) return true;
            for (int i = 0; i < count; i++)
            {
                var enemy = threats[i].GetComponentInParent<ZombieHealth>();
                if (enemy && enemy.isActiveAndEnabled && enemy.IsAlive && enemy.gameObject.scene == gameObject.scene) return true;
            }
            return false;
        }
        public SleepRejection Eligibility(RestPoint point, double seconds)
        {
            if (!WorldTimeSettings.Finite(seconds) || seconds <= 0 || Simulation == null || seconds > Simulation.MaximumSleepSeconds || seconds + Simulation.Seconds > WorldTimeSettings.MaximumTime) return SleepRejection.InvalidDuration;
            if (!health || !health.IsAlive) return SleepRejection.Dead;
            if (Sleeping || Flow.Restoring || (Simulation != null && Simulation.Advancing)) return SleepRejection.TransitionInProgress;
            if (!point || !point.isActiveAndEnabled || point.Clock != this || point.gameObject.scene != gameObject.scene ||
                (Flow.Player.transform.position - point.transform.position).sqrMagnitude > point.UseRange * point.UseRange) return SleepRejection.NotAtBed;
            if (Flow.Paused || !shelter || shelter.State != ExpeditionState.Shelter || !QueryRoof()) return SleepRejection.UnsafeState;
            if (ThreatNearby()) return SleepRejection.ThreatNearby;
            return SleepRejection.None;
        }
        public SleepRequest RequestSleep(RestPoint point, double seconds)
        {
            LastRejection = Eligibility(point, seconds);
            if (LastRejection != SleepRejection.None) { Feedback = RejectionText(LastRejection); return new SleepRequest(LastRejection); }
            Sleeping = true; sleepingAt = point; input.SetGameplay(false);
            var combat = Flow.Player.GetComponent<PlayerCombatController>(); if (combat) combat.CancelGameplayActions(false);
            sleepRoutine = StartCoroutine(Sleep(seconds)); return new SleepRequest(SleepRejection.None);
        }
        IEnumerator Sleep(double seconds)
        {
            double start = Simulation.Seconds, target = start + seconds;
            var reason = AdvanceReason.Completed;
            Feedback = "Resting… wetness reduces healing by up to 50%.";
            // Yield first: acceptance is observable before any world mutation; UI can present the transition.
            yield return null;
            while (Simulation != null && Simulation.Seconds < target)
            {
                protectedFromRain = QueryRoof();
                var step = Simulation.AdvanceUntil(Math.Min(target, Simulation.Seconds + 900), Exposure, rainProtection, recover);
                reason = step.Reason;
                if (reason != AdvanceReason.Completed) break;
                yield return null; // bounded 15-minute chunks allow live damage, cancellation and threat checks.
            }
            if (Simulation == null) yield break;
            LastSleep = new AdvanceResult(start, Simulation.Seconds, reason); Simulation.RecordSleep(LastSleep);
            Sleeping = false; sleepingAt = null; sleepRoutine = null;
            if (input) input.SetGameplay(!Flow.Paused && !Flow.PlayerDead && !Flow.InMenu);
            Feedback = reason == AdvanceReason.Completed ? "Awake. Rest and shelter help you dry and recover." : "Sleep interrupted: " + reason;
        }
        public double NextBoundary(double now) => double.PositiveInfinity;
        public void ApplyElapsed(double from, double to) { }
        public AdvanceReason Inspect(double now)
        {
            if (!health || !health.IsAlive) return AdvanceReason.Dead;
            if (!Sleeping) return AdvanceReason.Completed;
            if (Flow.Paused || Flow.Restoring || !sleepingAt || !QueryRoof() ||
                (Flow.Player.transform.position - sleepingAt.transform.position).sqrMagnitude > sleepingAt.UseRange * sleepingAt.UseRange) return AdvanceReason.Cancelled;
            return ThreatNearby() ? AdvanceReason.ThreatNearby : AdvanceReason.Completed;
        }
        void Recover(double amount) { if (health && health.IsAlive) health.RecoverHealth((float)amount); }
        public WorldTimeSnapshot Capture() => Simulation.Capture();
        public void Restore(WorldTimeSnapshot snapshot)
        {
            if (Sleeping) throw new InvalidOperationException("Cannot hydrate during sleep.");
            var restored = new WorldSimulation(snapshot); restored.Register(this); Simulation = restored;
            GetComponent<LastSignal.AI.WorldPopulationManager>()?.BindClock();
            protectedFromRain = pendingProtection = QueryRoof(); pendingElapsed = pollElapsed = 0;
            if (presentation) presentation.Refresh();
        }
        public static string RejectionText(SleepRejection reason)
        {
            switch (reason)
            {
                case SleepRejection.NotAtBed: return "Move closer to the shelter bed.";
                case SleepRejection.ThreatNearby: return "Cannot sleep: an active threat is nearby.";
                case SleepRejection.Dead: return "Cannot sleep while dead.";
                case SleepRejection.InvalidDuration: return "Choose a valid sleep duration.";
                case SleepRejection.TransitionInProgress: return "Wait for the current transition to finish.";
                case SleepRejection.UnsafeState: return "Return to shelter, close menus and rest under a roof.";
                default: return "Ready to sleep.";
            }
        }
    }
}
