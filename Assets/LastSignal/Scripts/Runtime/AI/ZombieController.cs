using Unity.Profiling;
using UnityEngine;

namespace LastSignal
{
    [RequireComponent(typeof(ZombieNavigation), typeof(ZombiePerception), typeof(ZombieNoiseListener))]
    public sealed class ZombieController : MonoBehaviour
    {
        static readonly ProfilerMarker Marker = new ProfilerMarker("LastSignal.Zombie.AI");
        [SerializeField] ZombieDefinition definition;
        [SerializeField] Transform meleeOrigin;
        ZombieHealth health;
        ZombieDismemberment dismemberment;
        Collider[] ownedColliders;
        float reactionRemaining, reactionReadyAt;
        CapsuleCollider vehicleCapsule;
        bool vehicleReaction, vehicleCapsuleWasEnabled;
        public bool VehicleReactionActive => vehicleReaction;
        void ReleaseVehicleReaction(bool restore)
        {
            if (restore && vehicleCapsule && vehicleCapsuleWasEnabled) vehicleCapsule.enabled = true;
            vehicleReaction = false; vehicleCapsuleWasEnabled = false;
        }
        ZombieImpactSeverity reactionPriority;
        public ZombieImpactReaction LastImpact { get; private set; }
        ZombieState reactionReturn;
        bool reactionWasInvestigating;
        public bool IsDead => runtime.State == ZombieState.Dead;
        public float ReactionRemaining => reactionRemaining;
        PlayerHealth targetHealth;
        CharacterController targetCapsule;
        bool combatReady, contactConsumed;
        bool alternateAttack;
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
        readonly ZombieAuditoryMemory auditory = new ZombieAuditoryMemory();
        ZombieNoiseListener noiseListener;
        Vector3 investigateDestination, investigateDirection;
        float investigateAge;
        bool auditorySearch;
        static readonly ProfilerMarker InvestigateMarker = new ProfilerMarker("LastSignal.Zombie.Investigate");
        public ZombieAuditoryMemory Auditory => auditory;
        public Vector3 InvestigateDestination => investigateDestination;
        public float InvestigateAge => investigateAge;
        public bool SearchFromHearing => auditorySearch;
        public bool CanHear => isActiveAndEnabled && initialized && !IsDead && target && target.activeInHierarchy && (!targetHealth || targetHealth.IsAlive);
        public bool ReceiveAuditoryStimulus(in ZombieAuditoryStimulus stimulus)
        {
            if (!CanHear || paused || !auditory.Receive(stimulus, definition.HearingMemoryDuration)) return false;
            // Do not reset the investigation clock for repeated nearby footsteps, or for retargeting.
            if (runtime.State == ZombieState.Investigating)
            {
                if ((stimulus.Event.Position - investigateDestination).sqrMagnitude >= definition.RepathDistance * definition.RepathDistance)
                { investigateDestination = stimulus.Event.Position; investigateDirection = stimulus.ApproachDirection; }
            }
            else if (!(runtime.Visible && runtime.Confidence >= 1) &&
                (runtime.State == ZombieState.Idle || runtime.State == ZombieState.Searching)) ChangeState(ZombieState.Investigating);
            return true;
        }
        void ClearAuditory()
        { auditory.Clear(); investigateAge = 0; investigateDestination = investigateDirection = Vector3.zero; auditorySearch = false; }
        ZombieState AfterCombat() => runtime.Visible ? ZombieState.Chasing : auditory.HasStimulus ? ZombieState.Investigating : ZombieState.Searching;
        void Investigate(float seconds)
        {
            using (InvestigateMarker.Auto())
            {
                if (runtime.Visible && runtime.Confidence >= 1) { ChangeState(ZombieState.Chasing); return; }
                investigateAge += seconds;
                navigation.MoveTo(investigateDestination, clock, definition.InvestigateArrivalDistance);
                if (!auditory.HasStimulus || investigateAge >= definition.InvestigateDuration || navigation.Exhausted ||
                    (transform.position - investigateDestination).sqrMagnitude <= definition.InvestigateArrivalDistance * definition.InvestigateArrivalDistance ||
                    navigation.Arrived && navigation.PathStatus == UnityEngine.AI.NavMeshPathStatus.PathComplete)
                {
                    auditorySearch = true; auditory.Consume(); ChangeState(ZombieState.Searching);
                }
            }
        }
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
            if (IsDead) return false;
            if (!definition || !definition.IsValid(out _))
            { Debug.LogError("Zombie requires a valid Shambler definition.", this); enabled = false; return false; }
            navigation = GetComponent<ZombieNavigation>(); perception = GetComponent<ZombiePerception>();
            noiseListener = GetComponent<ZombieNoiseListener>();
            if (GetComponents<ZombieNoiseListener>().Length != 1)
            { Debug.LogError("Zombie requires exactly one auditory listener.", this); enabled = false; return false; }
            Shutdown(); paused = false;
            initialized = navigation.Initialize(definition);
            presentation = GetComponent<ZombieAnimationPresenter>();
            if (presentation && !presentation.Initialize(definition)) initialized = false;
            combatReady = presentation && meleeOrigin && definition.IsAttackValid(out _) &&
                presentation.HasAttackPresentation(definition.AttackClip) &&
                presentation.HasAttackPresentation(definition.AlternateAttackClip);
            if (presentation && !combatReady)
            { Debug.LogError("Zombie melee requires MeleeOrigin and valid measured attack tuning. Combat disabled.", this); initialized = false; }
            health = GetComponent<ZombieHealth>();
            dismemberment = GetComponent<ZombieDismemberment>();
            if (health)
            {
                health.Damaged -= OnDamaged; health.Died -= OnDeath;
                health.Damaged += OnDamaged; health.Died += OnDeath;
                ownedColliders = GetComponentsInChildren<Collider>(true);
                if (!presentation || !presentation.HasDamagePresentation() || !health.IsAlive) initialized = false;
                health.SetDamageEnabled(initialized);
            }
            // Stable per-instance phase. No shared scheduler or per-tick randomness.
            perceptionDue = (uint)GetEntityId().GetHashCode() % 17 / 17f * definition.PerceptionInterval;
            return initialized;
        }
        public bool Bind(GameObject player)
        {
            if (IsDead) return false;
            // Rebinding is an explicit lifecycle boundary, including a rejected/null replacement.
            // A strike committed against A can never become a strike against B.
            ReleaseVehicleReaction(health && health.IsAlive);
            if (presentation) presentation.EndReaction();
            reactionRemaining = reactionReadyAt = 0; reactionPriority = ZombieImpactSeverity.Light;
            LastImpact = ZombieImpactReaction.Legacy;
            noiseListener?.Release(); ClearAuditory();
            ClearAttack(); runtime.Reset(); search.Clear(); holding = false;
            target = null; targetHealth = null; targetCapsule = null;
            if (navigation) navigation.Stop();
            if (!initialized || !perception.Bind(player))
            { Debug.LogError("Zombie target requires the active player capsule and FirstPersonLook camera.", this); return false; }
            target = player;
            targetHealth = player.GetComponent<PlayerHealth>(); targetCapsule = player.GetComponent<CharacterController>();
            if (combatReady && !targetHealth)
                Debug.LogWarning("Zombie target has no PlayerHealth; melee is disabled for this binding.", this);
            var noiseContext = player.GetComponent<Noise.GameplayNoiseContext>();
            if (!noiseContext) { Debug.LogError("Zombie hearing requires explicit GameplayNoiseContext on the bound player.", this); Shutdown(); return false; }
            if (!noiseListener || !noiseListener.Bind(noiseContext)) { Shutdown(); return false; }
            return true;
        }
        public void SetPaused(bool value)
        {
            paused = value; if (health) health.SetDamageEnabled(initialized && !value && !IsDead);
            if (navigation && !IsDead) navigation.Suspend(value);
            if (presentation) presentation.SetPaused(value);
            if (!value) { perceptionDue = clock; observationAge = 0; } // Revalidate immediately; no paused evidence accrues.
        }
        public void Shutdown()
        {
            ReleaseVehicleReaction(health && health.IsAlive);
            noiseListener?.Release(); ClearAuditory();
            if (health) health.SetDamageEnabled(false);
            if (presentation && !IsDead) presentation.EndReaction();
            reactionRemaining = reactionReadyAt = 0; reactionPriority = ZombieImpactSeverity.Light;
            LastImpact = ZombieImpactReaction.Legacy;
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
            if (IsDead) { if (!paused && seconds > 0 && presentation) presentation.AdvanceDamage(seconds); return; }
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
            clock += seconds;
            auditory.Advance(seconds, definition.HearingMemoryDuration);
            if (runtime.State == ZombieState.HitReact)
            {
                // Vehicle recoil temporarily owns NavMesh movement within the existing HitReact.
                bool vehicleRecoil = navigation.AdvanceVehicleImpact(seconds);
                if (!vehicleReaction && !vehicleRecoil && reactionReturn == ZombieState.Chasing) Chase(seconds);
                else if (!vehicleReaction && !vehicleRecoil && reactionReturn == ZombieState.Investigating)
                    navigation.MoveTo(investigateDestination, clock, definition.InvestigateArrivalDistance);
                navigation.SetRunning(reactionReturn == ZombieState.Chasing);
                if (!vehicleReaction && !vehicleRecoil) navigation.Tick(seconds, clock);
                presentation.Present(navigation.Velocity.magnitude, navigation.Running, seconds, false);
                reactionRemaining = Mathf.Max(0, reactionRemaining - seconds);
                presentation.AdvanceDamage(seconds);
                if (reactionRemaining <= 0)
                {
                    ReleaseVehicleReaction(true);
                    presentation.EndReaction();
                    reactionPriority = ZombieImpactSeverity.Light;
                    // A sound accepted during the reaction is usable once its commitment ends.
                    if (auditory.HasStimulus && !(runtime.Visible && runtime.Confidence >= 1) &&
                        (reactionReturn == ZombieState.Idle || reactionReturn == ZombieState.Searching))
                        reactionReturn = ZombieState.Investigating;
                    runtime.Transition(reactionReturn);
                    if (reactionReturn == ZombieState.Investigating)
                    {
                        if (!reactionWasInvestigating) EnterState(ZombieState.Investigating);
                        else if (auditory.HasStimulus)
                        { investigateDestination = auditory.Stimulus.Event.Position; investigateDirection = auditory.Stimulus.ApproachDirection; }
                    }
                    perceptionDue = clock; observationAge = 0;
                }
                return; // Perception and attack decisions resume after the brief reaction.
            }
            observationAge += seconds; runtime.Advance(seconds);
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
                case ZombieState.Investigating: Investigate(seconds); break;
                case ZombieState.Chasing:
                    if (navigation.Exhausted || runtime.MemoryAge >= definition.LossGrace)
                        ChangeState(auditory.HasStimulus && !runtime.Visible ? ZombieState.Investigating : ZombieState.Searching);
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
            navigation.SetRunning(runtime.State == ZombieState.Chasing);
            navigation.Tick(seconds, clock);
            if (presentation && !Attacking)
                presentation.Present(navigation.Velocity.magnitude, navigation.Running, seconds, false);
        }
        void OnDamaged(DamageInfo info)
        {
            if (!initialized || paused || IsDead) return;
            bool severed = dismemberment && dismemberment.IsSevered(info.BodyPart);
            var impact = ZombieImpactReaction.Select(info, transform.rotation, health.MaxHealth, severed,
                definition.HeavyImpactHealthFraction, definition.MinimumHeavyImpact);
            LastImpact = impact;
            if (vehicleReaction) return; // Damage/death commits remain canonical; no reaction restart.
            bool vehicleHit = info.Category == DamageCategory.VehicleImpact;
            if (vehicleHit && !vehicleReaction)
            {
                vehicleCapsule = GetComponent<CapsuleCollider>();
                vehicleCapsuleWasEnabled = vehicleCapsule && vehicleCapsule.enabled;
                if (vehicleCapsule) vehicleCapsule.enabled = false;
                vehicleReaction = true;
                navigation.BeginVehicleImpact(info.Direction, Mathf.Lerp(.6f, 2.5f, Mathf.Clamp01(info.Amount / 100f)));
            }
            bool reacting = runtime.State == ZombieState.HitReact;
            if (!vehicleHit && (reacting && impact.Severity <= reactionPriority ||
                !reacting && clock < reactionReadyAt && impact.Severity == ZombieImpactSeverity.Light)) return;
            if (reacting)
            {
                reactionPriority = impact.Severity;
                reactionRemaining = vehicleHit ? definition.VehicleReactionDuration : definition.HitReactDuration;
                reactionReadyAt = clock + definition.HitReactCooldown;
                if (vehicleHit) { presentation.BeginVehicleKnockdown(impact); navigation.Suspend(true); }
                else presentation.BeginDamage(false, impact);
                return;
            }
            reactionWasInvestigating = runtime.State == ZombieState.Investigating;
            reactionReturn = Attacking ? AfterCombat() : runtime.State;
            // Preserve search progress when already searching; an interrupted melee starts search only if unseen.
            if (Attacking && reactionReturn == ZombieState.Searching)
                search.Begin(runtime.LastKnownPosition, runtime.LastSeenDirection);
            ClearAttack();
            runtime.Transition(ZombieState.HitReact);
            reactionRemaining = vehicleHit ? definition.VehicleReactionDuration : definition.HitReactDuration; reactionReadyAt = clock + definition.HitReactCooldown;
            reactionPriority = impact.Severity;
            if (vehicleHit) { presentation.BeginVehicleKnockdown(impact); navigation.Suspend(true); }
            else presentation.BeginDamage(false, impact);
        }
        void OnDeath()
        {
            if (IsDead) return;
            if (health && health.LastDamage.Amount > 0)
                LastImpact = ZombieImpactReaction.Select(health.LastDamage, transform.rotation,
                    health.MaxHealth, dismemberment && dismemberment.IsSevered(health.LastDamage.BodyPart),
                    definition.HeavyImpactHealthFraction, definition.MinimumHeavyImpact);
            ReleaseVehicleReaction(false);
            noiseListener?.Release(); ClearAuditory();
            ClearAttack(); reactionRemaining = 0; sequence = 0;
            runtime.Transition(ZombieState.Dead); search.Clear(); holding = false;
            target = null; targetHealth = null; targetCapsule = null;
            initialized = false; combatReady = false;
            if (perception) { perception.Clear(); perception.enabled = false; }
            if (navigation) { navigation.Stop(); navigation.enabled = false; }
            if (ownedColliders != null) foreach (var collider in ownedColliders) if (collider) collider.enabled = false;
            if (presentation)
            {
                if (health && health.LastDamage.Category == DamageCategory.VehicleImpact || presentation.VehicleKnockdown)
                    presentation.BeginVehicleDeath(LastImpact);
                else presentation.BeginDamage(true, LastImpact);
            }
        }
        void OnDestroy()
        {
            if (health) { health.Damaged -= OnDamaged; health.Died -= OnDeath; }
        }
        bool TryBeginAttack()
        {
            if (!combatReady || (dismemberment && !dismemberment.CanUseRightArmAttack) ||
                !meleeOrigin || !presentation || !presentation.HasAnimator || !targetHealth || !targetHealth.IsAlive ||
                !runtime.Visible || !navigation.Ready || navigation.Exhausted) return false;
            if (ZombieMeleeValidator.Validate(MeleeOrigin, transform.forward, targetCapsule, targetHealth,
                definition.AttackEnterRange, definition.AttackHalfAngle, definition.OcclusionMask, out _, out _, out _) != MeleeResult.Hit) return false;
            sequence++; alternateAttack = (sequence & 1) == 0;
            attackTime = 0; contactConsumed = false; lockedForward = Vector3.zero;
            LastMeleeResult = MeleeResult.None;
            ChangeState(ZombieState.AttackWindup);
            presentation.BeginAttack(alternateAttack);
            return true;
        }
        void TickAttack(float seconds)
        {
            using (AttackMarker.Auto())
            {
                if (runtime.State != ZombieState.Recovering && dismemberment && !dismemberment.CanUseRightArmAttack)
                {
                    ClearAttack(); LastMeleeResult = MeleeResult.Aborted;
                    ChangeState(runtime.State == ZombieState.AttackCommit ? ZombieState.Recovering : AfterCombat());
                    return;
                }
                if (!meleeOrigin || !presentation || !presentation.HasAnimator || !targetHealth || !targetHealth.IsAlive || !targetCapsule || !targetCapsule.enabled || !navigation.Ready)
                { Shutdown(); return; }
                if (runtime.State == ZombieState.AttackWindup)
                {
                    if (Vector3.Distance(MeleeOrigin, targetCapsule.ClosestPoint(MeleeOrigin)) > definition.AttackAbortRange)
                    {
                        ClearAttack(); LastMeleeResult = MeleeResult.Aborted;
                        ChangeState(AfterCombat()); return;
                    }
                    // Never track a hidden live transform, and never rotate past the commitment boundary.
                    float turnSeconds = Mathf.Min(seconds, Mathf.Max(0, definition.AttackCommitTimeFor(alternateAttack) - attackTime));
                    if (runtime.Visible)
                    {
                        var direction = runtime.LastKnownPosition - transform.position; direction.y = 0;
                        if (direction.sqrMagnitude > .001f)
                            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), definition.AttackWindupTurnSpeed * turnSeconds);
                    }
                }
                attackTime += seconds;
                presentation.AdvanceAttack(seconds);
                if (runtime.State == ZombieState.AttackWindup && attackTime >= definition.AttackCommitTimeFor(alternateAttack))
                { lockedForward = transform.forward; ChangeState(ZombieState.AttackCommit); }
                if (runtime.State == ZombieState.AttackCommit && !contactConsumed && attackTime >= definition.AttackContactTimeFor(alternateAttack))
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
                        targetHealth.TakeDamage(new DamageInfo { Amount = definition.AttackDamage, Instigator = gameObject, Category = DamageCategory.Melee,
                            SourcePosition = MeleeOrigin, HitPoint = point, HitNormal = -lockedForward });
                        if (!initialized || !target) return; // A health listener may end the session synchronously.
                    }
                }
                if (runtime.State == ZombieState.AttackCommit && attackTime >= definition.AttackRecoveryTimeFor(alternateAttack))
                    ChangeState(ZombieState.Recovering);
                if (runtime.State == ZombieState.Recovering && attackTime >= definition.AttackDurationFor(alternateAttack))
                {
                    ClearAttack();
                    ChangeState(runtime.Visible && !navigation.Exhausted ? ZombieState.Chasing : auditory.HasStimulus && !runtime.Visible ? ZombieState.Investigating : ZombieState.Searching);
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
            if (next != ZombieState.Searching) auditorySearch = false;
            ExitState(runtime.State); runtime.Transition(next); EnterState(next);
        }
        void ExitState(ZombieState previous)
        { navigation.Stop(); holding = false; if (previous == ZombieState.Searching) search.Clear(); }
        void EnterState(ZombieState next)
        {
            switch (next)
            {
                case ZombieState.Idle: auditory.Consume(); navigation.ResetPolicy(); break;
                case ZombieState.Investigating:
                    investigateAge = 0; investigateDestination = auditory.Stimulus.Event.Position;
                    investigateDirection = auditory.Stimulus.ApproachDirection; navigation.ResetPolicy(); break;
                case ZombieState.Chasing: navigation.ResetPolicy(); break;
                case ZombieState.Searching: searchLostSight = !runtime.Visible; search.Begin(auditorySearch ? investigateDestination : runtime.LastKnownPosition, auditorySearch ? investigateDirection : runtime.LastSeenDirection); break;
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
            if (auditory.HasStimulus) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(auditory.Stimulus.Event.Position, .3f);
                UnityEditor.Handles.Label(auditory.Stimulus.Event.Position, "Heard " + auditory.Stimulus.Event.EventId + " " + auditory.Stimulus.Event.Category + " strength=" + auditory.Stimulus.Strength + " age=" + auditory.Age); }
            if (runtime.HasMemory) { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(runtime.LastKnownPosition,.2f); Gizmos.DrawLine(transform.position,runtime.LastKnownPosition); }
            if (runtime.State == ZombieState.Searching) { Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(search.Destination,.3f); }
            if (navigation) { Gizmos.color = Color.blue; Gizmos.DrawWireSphere(navigation.Destination,.15f); }
        }
#endif
    }
}
