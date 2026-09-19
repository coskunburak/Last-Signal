using Unity.Profiling;
using UnityEngine;

namespace LastSignal
{
    [RequireComponent(typeof(ZombieNavigation), typeof(ZombiePerception))]
    public sealed class ZombieController : MonoBehaviour
    {
        static readonly ProfilerMarker Marker = new ProfilerMarker("LastSignal.Zombie.AI");
        [SerializeField] ZombieDefinition definition;
        [SerializeField] Transform meleeOrigin;
        PlayerHealth targetHealth;
        CharacterController targetCapsule;
        bool combatReady, contactConsumed;
        float attackTime;
        ulong sequence;
        Vector3 lockedForward;
        static readonly ProfilerMarker AttackMarker = new ProfilerMarker("LastSignal.Zombie.Attack");
        public ulong AttackSequence => sequence;
        public float AttackTime => attackTime;
        public bool ContactConsumed => contactConsumed;
        public Vector3 LockedForward => lockedForward;
        public MeleeResult LastMeleeResult { get; private set; }
        public float LastContactDistance { get; private set; }
        public float LastContactAngle { get; private set; }
        public ulong LastContactSequence { get; private set; }
        public int ContactAttempts { get; private set; }
        public int SuccessfulHits { get; private set; }
        public Vector3 MeleeOrigin => meleeOrigin ? meleeOrigin.position : transform.position;
        public bool Attacking => runtime.State == ZombieState.AttackWindup || runtime.State == ZombieState.AttackCommit || runtime.State == ZombieState.Recovering;
        public void ConfigureCombat(Transform origin) => meleeOrigin = origin;
        ZombieNavigation navigation;
        ZombiePerception perception;
        ZombieAnimationPresenter presentation;
        readonly ZombieRuntimeState runtime = new ZombieRuntimeState();
        readonly ZombieSearch search = new ZombieSearch();
        GameObject target;
        float clock, perceptionDue, observationAge, failureCooldown;
        bool initialized, paused, holding, searchLostSight;
#if UNITY_EDITOR
        public bool MeasureManagedAllocations { get; set; }
        public long ManagedTickBytes { get; private set; }
        public int MeasuredTicks { get; private set; }
#endif
        public ZombieRuntimeState Runtime => runtime;
        public ZombieDefinition Definition => definition;
        public ZombieNavigation Navigation => navigation;
        public ZombiePerception Perception => perception;
        public ZombieSearch Search => search;
        public GameObject Target => target;
        public bool Paused => paused;
        public float GameTime => clock;
        public bool Holding => holding;
        public void Configure(ZombieDefinition value) => definition = value;
        public bool Initialize()
        {
            if (!definition || !definition.IsValid(out _))
            { Debug.LogError("Zombie requires a valid Shambler definition.", this); enabled = false; return false; }
            navigation = GetComponent<ZombieNavigation>(); perception = GetComponent<ZombiePerception>();
            Shutdown(); paused = false;
            initialized = navigation.Initialize(definition);
            presentation = GetComponent<ZombieAnimationPresenter>();
            if (presentation && !presentation.Initialize(definition)) initialized = false;
            combatReady = presentation && meleeOrigin && definition.IsAttackValid(out _) && presentation.HasAttackPresentation(definition.AttackClip);
            if (presentation && !combatReady)
            { Debug.LogError("Zombie melee requires MeleeOrigin and valid measured attack tuning. Combat disabled.", this); initialized = false; }
            // Stable per-instance phase. No shared scheduler or per-tick randomness.
            perceptionDue = (uint)GetEntityId().GetHashCode() % 17 / 17f * definition.PerceptionInterval;
            return initialized;
        }
        public bool Bind(GameObject player)
        {
            // Rebinding is an explicit lifecycle boundary, including a rejected/null replacement.
            // A strike committed against A can never become a strike against B.
            ClearAttack(); runtime.Reset(); search.Clear(); holding = false;
            target = null; targetHealth = null; targetCapsule = null;
            if (navigation) navigation.Stop();
            if (!initialized || !perception.Bind(player))
            { Debug.LogError("Zombie target requires the active player capsule and FirstPersonLook camera.", this); return false; }
            target = player;
            targetHealth = player.GetComponent<PlayerHealth>(); targetCapsule = player.GetComponent<CharacterController>();
            if (combatReady && !targetHealth)
                Debug.LogWarning("Zombie target has no PlayerHealth; melee is disabled for this binding.", this);
            return true;
        }
        public void SetPaused(bool value)
        {
            paused = value; if (navigation) navigation.Suspend(value);
            if (presentation) presentation.SetPaused(value);
            if (!value) { perceptionDue = clock; observationAge = 0; } // Revalidate immediately; no paused evidence accrues.
        }
        public void Shutdown()
        {
            target = null; targetHealth = null; targetCapsule = null;
            ClearAttack(); sequence = 0; ContactAttempts = SuccessfulHits = 0; LastContactSequence = 0;
            initialized = false; runtime.Reset(); search.Clear(); holding = false;
            clock = perceptionDue = observationAge = failureCooldown = 0; paused = searchLostSight = false;
            if (perception) perception.Clear(); if (navigation) navigation.Stop();
        }
        void Update() => Simulate(Time.deltaTime);
        public void Simulate(float seconds)
        {
            if (!isActiveAndEnabled || !float.IsFinite(seconds)) return;
            if (!initialized) return;
            if (!target || !target.activeInHierarchy) { Shutdown(); return; }
            if (targetHealth && !targetHealth.IsAlive) { Shutdown(); return; }
            if (paused || seconds <= 0) return;
#if UNITY_EDITOR
            long allocationStart = MeasureManagedAllocations ? System.GC.GetAllocatedBytesForCurrentThread() : 0;
#endif
            using (Marker.Auto()) Tick(seconds);
#if UNITY_EDITOR
            if (MeasureManagedAllocations) { ManagedTickBytes += System.GC.GetAllocatedBytesForCurrentThread() - allocationStart; MeasuredTicks++; }
#endif
        }
        void Tick(float seconds)
        {
            clock += seconds; observationAge += seconds; runtime.Advance(seconds);
            if (clock >= perceptionDue)
            {
                var observation = perception.Evaluate(definition);
                // Bound evidence after slow frames; one stale sample cannot grant instant certainty.
                float evidenceTime = Mathf.Min(observationAge, definition.PerceptionInterval);
                runtime.Observe(observation.Visible, observation.Position, observation.Direction,
                    observation.Visible ? evidenceTime * observation.Evidence : evidenceTime, definition);
                observationAge = 0; perceptionDue = clock + definition.PerceptionInterval;
            }
            switch (runtime.State)
            {
                case ZombieState.Idle:
                    if (runtime.Visible && runtime.Confidence >= 1 && clock >= failureCooldown) ChangeState(ZombieState.Chasing);
                    else if (!runtime.Visible && runtime.Confidence <= 0) runtime.Reset();
                    break;
                case ZombieState.Chasing:
                    if (navigation.Exhausted || runtime.MemoryAge >= definition.LossGrace)
                        ChangeState(ZombieState.Searching);
                    else if (!TryBeginAttack()) Chase(seconds);
                    break;
                case ZombieState.Searching:
                    if (!runtime.Visible) searchLostSight = true;
                    if (runtime.Visible && runtime.Confidence >= 1 && (!navigation.Exhausted || searchLostSight)) ChangeState(ZombieState.Chasing);
                    else if (runtime.SearchAge >= definition.SearchDuration)
                    { failureCooldown = clock + definition.PathRetryInterval; ChangeState(ZombieState.Idle); }
                    else search.Tick(navigation, definition, seconds, clock);
                    break;
                case ZombieState.AttackWindup:
                case ZombieState.AttackCommit:
                case ZombieState.Recovering:
                    TickAttack(seconds);
                    break;
            }
            navigation.Tick(seconds, clock);
            if (presentation && !Attacking) presentation.Present(navigation.Velocity.magnitude, seconds, false);
        }
        bool TryBeginAttack()
        {
            if (!combatReady || !meleeOrigin || !presentation || !presentation.HasAnimator || !targetHealth || !targetHealth.IsAlive || !runtime.Visible || !navigation.Ready || navigation.Exhausted) return false;
            if (ZombieMeleeValidator.Validate(MeleeOrigin, transform.forward, targetCapsule, targetHealth,
                definition.AttackEnterRange, definition.AttackHalfAngle, definition.OcclusionMask, out _, out _, out _) != MeleeResult.Hit) return false;
            sequence++; attackTime = 0; contactConsumed = false; lockedForward = Vector3.zero;
            LastMeleeResult = MeleeResult.None;
            ChangeState(ZombieState.AttackWindup);
            presentation.BeginAttack();
            return true;
        }
        void TickAttack(float seconds)
        {
            using (AttackMarker.Auto())
            {
                if (!meleeOrigin || !presentation || !presentation.HasAnimator || !targetHealth || !targetHealth.IsAlive || !targetCapsule || !targetCapsule.enabled || !navigation.Ready)
                { Shutdown(); return; }
                if (runtime.State == ZombieState.AttackWindup)
                {
                    if (Vector3.Distance(MeleeOrigin, targetCapsule.ClosestPoint(MeleeOrigin)) > definition.AttackAbortRange)
                    {
                        ClearAttack(); LastMeleeResult = MeleeResult.Aborted;
                        ChangeState(runtime.Visible ? ZombieState.Chasing : ZombieState.Searching); return;
                    }
                    // Never track a hidden live transform, and never rotate past the commitment boundary.
                    float turnSeconds = Mathf.Min(seconds, Mathf.Max(0, definition.AttackCommitTime - attackTime));
                    if (runtime.Visible)
                    {
                        var direction = runtime.LastKnownPosition - transform.position; direction.y = 0;
                        if (direction.sqrMagnitude > .001f)
                            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), definition.AttackWindupTurnSpeed * turnSeconds);
                    }
                }
                attackTime += seconds;
                presentation.AdvanceAttack(seconds);
                if (runtime.State == ZombieState.AttackWindup && attackTime >= definition.AttackCommitTime)
                { lockedForward = transform.forward; ChangeState(ZombieState.AttackCommit); }
                if (runtime.State == ZombieState.AttackCommit && !contactConsumed && attackTime >= definition.AttackContactTime)
                {
                    contactConsumed = true; // Spend before callbacks; every outcome, including a miss, consumes the sequence.
                    ContactAttempts++; LastContactSequence = sequence;
                    LastMeleeResult = ZombieMeleeValidator.Validate(MeleeOrigin, lockedForward, targetCapsule, targetHealth,
                        definition.AttackReach, definition.AttackHalfAngle, definition.OcclusionMask,
                        out var point, out var distance, out var angle);
                    LastContactDistance = distance; LastContactAngle = angle;
                    if (LastMeleeResult == MeleeResult.Hit)
                    {
                        SuccessfulHits++;
                        targetHealth.TakeDamage(new DamageInfo { Amount = definition.AttackDamage, Instigator = gameObject,
                            SourcePosition = MeleeOrigin, HitPoint = point, HitNormal = -lockedForward });
                        if (!initialized || !target) return; // A health listener may end the session synchronously.
                    }
                }
                if (runtime.State == ZombieState.AttackCommit && attackTime >= definition.AttackRecoveryTime)
                    ChangeState(ZombieState.Recovering);
                if (runtime.State == ZombieState.Recovering && attackTime >= definition.AttackDuration)
                {
                    ClearAttack();
                    ChangeState(runtime.Visible && !navigation.Exhausted ? ZombieState.Chasing : ZombieState.Searching);
                }
            }
        }
        void ClearAttack()
        {
            attackTime = 0; contactConsumed = false; lockedForward = Vector3.zero;
            if (presentation) presentation.EndAttack();
        }
        void Chase(float seconds)
        {
            // LastKnownPosition is the sole destination, even during the brief loss grace.
            Vector3 offset = runtime.LastKnownPosition - transform.position; offset.y = 0;
            float distance = offset.magnitude;
            if (holding && (!runtime.Visible || distance >= definition.ResumeDistance)) holding = false;
            if (!holding && runtime.Visible && distance <= definition.StopDistance + .08f) { holding = true; navigation.Stop(); }
            if (holding) navigation.Face(offset, seconds);
            else navigation.MoveTo(runtime.LastKnownPosition, clock, runtime.Visible ? definition.StopDistance : definition.SearchArrivalDistance);
        }
        void ChangeState(ZombieState next)
        {
            ExitState(runtime.State); runtime.Transition(next); EnterState(next);
        }
        void ExitState(ZombieState previous)
        { navigation.Stop(); holding = false; if (previous == ZombieState.Searching) search.Clear(); }
        void EnterState(ZombieState next)
        {
            switch (next)
            {
                case ZombieState.Idle: navigation.ResetPolicy(); break;
                case ZombieState.Chasing: navigation.ResetPolicy(); break;
                case ZombieState.Searching: searchLostSight = !runtime.Visible; search.Begin(runtime.LastKnownPosition, runtime.LastSeenDirection); break;
            }
        }
        void OnDisable() => Shutdown();
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.1f,
                runtime.State + " visible=" + runtime.Visible + " confidence=" + runtime.Confidence.ToString("F2") +
                "\nAttack=" + sequence + " t=" + attackTime.ToString("F3") + " contact=" + (definition ? definition.AttackContactTime : 0).ToString("F3") + " d=" + LastContactDistance.ToString("F2") + " angle=" + LastContactAngle.ToString("F1") + " result=" + LastMeleeResult + " HP=" + (targetHealth ? targetHealth.CurrentHealth : 0) + "\nPath=" + (navigation ? navigation.PathStatus.ToString() : "None") + " target=" + (target ? target.name : "None"));
            if (runtime.HasMemory) { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(runtime.LastKnownPosition,.2f); Gizmos.DrawLine(transform.position,runtime.LastKnownPosition); }
            if (runtime.State == ZombieState.Searching) { Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(search.Destination,.3f); }
            if (navigation) { Gizmos.color = Color.blue; Gizmos.DrawWireSphere(navigation.Destination,.15f); }
        }
#endif
    }
}
