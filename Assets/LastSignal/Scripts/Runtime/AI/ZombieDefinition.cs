using UnityEngine;

namespace LastSignal
{
    [CreateAssetMenu(menuName = "Last Signal/Zombie Definition", fileName = "Shambler")]
    public sealed class ZombieDefinition : ScriptableObject
    {
        [Header("Perception")]
        [SerializeField, Min(.1f)] float sightDistance = 15;
        [SerializeField, Range(1, 179)] float horizontalFov = 120;
        [SerializeField, Range(1, 89)] float verticalHalfAngle = 60;
        [SerializeField, Min(.02f)] float perceptionInterval = .1f;
        [SerializeField, Min(.05f)] float acquisitionTime = .35f;
        [SerializeField, Min(.05f)] float confidenceDecayTime = .5f;
        [SerializeField, Tooltip("Opaque world only; exclude player, enemies and presentation.")] LayerMask occlusionMask = 1;
        [Header("Hearing — initial production tuning")]
        [SerializeField] float hearingThreshold = .12f;
        [SerializeField] float hearingMemoryDuration = 15;
        [SerializeField] float investigateDuration = 12;
        [SerializeField] float investigateArrivalDistance = .6f;
        [SerializeField] float occludedTransmission = .4f;
        [SerializeField] float footstepSensitivity = 1;
        [SerializeField] float sprintSensitivity = 1.1f;
        [SerializeField] float meleeSensitivity = 1.2f;
        [SerializeField] float gunshotSensitivity = 1.5f;
        public float HearingThreshold => hearingThreshold;
        public float HearingMemoryDuration => hearingMemoryDuration;
        public float InvestigateDuration => investigateDuration;
        public float InvestigateArrivalDistance => investigateArrivalDistance;
        public float OccludedTransmission => occludedTransmission;
        public float HearingSensitivity(Noise.GameplayNoiseCategory category) => category switch {
            Noise.GameplayNoiseCategory.Footstep => footstepSensitivity,
            Noise.GameplayNoiseCategory.SprintFootstep => sprintSensitivity,
            Noise.GameplayNoiseCategory.MeleeImpact => meleeSensitivity,
            Noise.GameplayNoiseCategory.Gunshot => gunshotSensitivity,
            _ => 0 };
        public bool IsHearingValid => Positive(hearingThreshold) && Positive(hearingMemoryDuration) &&
            Positive(investigateDuration) && Positive(investigateArrivalDistance) &&
            float.IsFinite(occludedTransmission) && occludedTransmission > 0 && occludedTransmission <= 1 &&
            Positive(footstepSensitivity) && Positive(sprintSensitivity) && Positive(meleeSensitivity) && Positive(gunshotSensitivity);
        static bool Positive(float value) => float.IsFinite(value) && value > 0;
        [Header("Memory")]
        [SerializeField, Min(0)] float lossGrace = .45f;
        [SerializeField, Min(1)] float searchDuration = 12;
        [Header("Navigation")]
        [SerializeField, Min(.1f)] float speed = .92f;
        [SerializeField, Min(.1f)] float runSpeed = 2.6f;
        [SerializeField, Min(.1f)] float acceleration = 2;
        [SerializeField, Min(1)] float turnSpeed = 120;
        [SerializeField, Min(.1f)] float stopDistance = 1.2f;
        [SerializeField, Min(.1f)] float resumeDistance = 1.6f;
        [SerializeField, Min(.05f)] float repathInterval = .3f;
        [SerializeField, Min(.01f)] float repathDistance = .45f;
        [SerializeField, Min(.1f)] float pathRetryInterval = 2;
        [SerializeField, Min(1)] int recoveryAttempts = 2;
        [SerializeField, Min(.1f)] float stuckTimeout = 2;
        [SerializeField, Min(.01f)] float stuckProgress = .12f;
        [SerializeField, Min(.01f)] float spawnSampleRadius = .3f;
        [Header("Search")]
        [SerializeField, Min(.1f)] float searchRadius = 2;
        [SerializeField, Min(.1f)] float searchArrivalDistance = .35f;
        [SerializeField, Min(.1f)] float inspectionPause = 1;
        [SerializeField, Min(.01f)] float searchSampleRadius = .6f;
        [Header("Presentation")]
        [SerializeField, Min(.1f)] float measuredWalkSpeed = .38f;
        [SerializeField, Min(.1f)] float measuredRunSpeed = 2.6f;
        [SerializeField, Min(.01f)] float animationBlendTime = .12f;
        [Header("Damage presentation")]
        [SerializeField] AnimationClip hitReactClip;
        [SerializeField] AnimationClip deathClip;
        [SerializeField] AnimationClip flyingBackDeathClip;
        [SerializeField] AnimationCurve deathGroundOffset = AnimationCurve.Constant(0, 1, 0);
        [SerializeField] AnimationCurve flyingBackGroundOffset = AnimationCurve.Constant(0, 1, 0);
        [SerializeField, Min(.01f)] float hitReactCooldown = 1.5f;
        [SerializeField, Range(0, 1)] float heavyImpactHealthFraction = .25f;
        [SerializeField, Min(0)] float minimumHeavyImpact = 20f;
        [SerializeField, Range(0, 30)] float lightImpactAngle = 8f;
        [SerializeField, Range(0, 45)] float heavyImpactAngle = 15f;
        public float HeavyImpactHealthFraction => heavyImpactHealthFraction > 0 ? heavyImpactHealthFraction : .25f;
        public float MinimumHeavyImpact => minimumHeavyImpact > 0 ? minimumHeavyImpact : 20f;
        public float LightImpactAngle => lightImpactAngle > 0 ? lightImpactAngle : 8f;
        public float HeavyImpactAngle => heavyImpactAngle > 0 ? heavyImpactAngle : 15f;
        public AnimationClip HitReactClip => hitReactClip;
        public AnimationClip DeathClip => deathClip;
        public AnimationClip FlyingBackDeathClip => flyingBackDeathClip;
        public float DeathGroundOffset(float seconds, bool flyingBack) =>
            (flyingBack ? flyingBackGroundOffset : deathGroundOffset).Evaluate(seconds);
        public float HitReactDuration => hitReactClip ? hitReactClip.length : 0;
        public float DeathDuration => Mathf.Max(deathClip ? deathClip.length : 0,
            flyingBackDeathClip ? flyingBackDeathClip.length : 0);
        public float HitReactCooldown => hitReactCooldown;
        public bool IsDamagePresentationValid => hitReactClip && deathClip && flyingBackDeathClip &&
            deathGroundOffset != null && deathGroundOffset.length > 1 &&
            flyingBackGroundOffset != null && flyingBackGroundOffset.length > 1 &&
            !hitReactClip.isLooping && !deathClip.isLooping && !flyingBackDeathClip.isLooping &&
            HitReactDuration > 0 && DeathDuration > 0 &&
            float.IsFinite(hitReactCooldown) && hitReactCooldown > HitReactDuration;
        [Header("Melee — measured supplied Mixamo strikes")]
        [SerializeField] AnimationClip attackClip;
        [SerializeField] AnimationClip alternateAttackClip;
        [SerializeField, Range(.5f, 1.25f)] float attackPlaybackSpeed = 1.2f;
        [SerializeField, Range(0, 1)] float attackCommitNormalized = .35f;
        [SerializeField, Range(0, 1)] float attackContactNormalized = .42f;
        [SerializeField, Range(0, 1)] float attackRecoveryNormalized = .55f;
        [SerializeField, Range(0, 1)] float alternateAttackCommitNormalized = .26f;
        [SerializeField, Range(0, 1)] float alternateAttackContactNormalized = .32f;
        [SerializeField, Range(0, 1)] float alternateAttackRecoveryNormalized = .45f;
        [SerializeField, Range(0, 1)] float alternateAttackEndNormalized = .70f;
        [SerializeField, Min(1)] float attackDamage = 20;
        [SerializeField, Min(.1f)] float attackEnterRange = 1.08f;
        [SerializeField, Min(.1f)] float attackReach = 1.24f;
        [SerializeField, Min(.1f)] float attackAbortRange = 2.5f;
        [SerializeField, Range(1, 89)] float attackHalfAngle = 20;
        [SerializeField, Min(1)] float attackWindupTurnSpeed = 60;
        public AnimationClip AttackClip => attackClip;
        public AnimationClip AlternateAttackClip => alternateAttackClip;
        public float AttackPlaybackSpeed => attackPlaybackSpeed;
        public float AttackDuration => attackClip ? attackClip.length / attackPlaybackSpeed : 0;
        public float AttackCommitTime => AttackDuration * attackCommitNormalized;
        public float AttackContactTime => AttackDuration * attackContactNormalized;
        public float AttackRecoveryTime => AttackDuration * attackRecoveryNormalized;
        public float AttackClipDurationFor(bool alternate) => alternate ?
            (alternateAttackClip ? alternateAttackClip.length / attackPlaybackSpeed : 0) : AttackDuration;
        public float AttackDurationFor(bool alternate) => AttackClipDurationFor(alternate) *
            (alternate ? alternateAttackEndNormalized : 1);
        public float AttackCommitTimeFor(bool alternate) => AttackClipDurationFor(alternate) *
            (alternate ? alternateAttackCommitNormalized : attackCommitNormalized);
        public float AttackContactTimeFor(bool alternate) => AttackClipDurationFor(alternate) *
            (alternate ? alternateAttackContactNormalized : attackContactNormalized);
        public float AttackRecoveryTimeFor(bool alternate) => AttackClipDurationFor(alternate) *
            (alternate ? alternateAttackRecoveryNormalized : attackRecoveryNormalized);
        public float AttackDamage => attackDamage;
        public float AttackEnterRange => attackEnterRange;
        public float AttackReach => attackReach;
        public float AttackAbortRange => attackAbortRange;
        public float AttackHalfAngle => attackHalfAngle;
        public float AttackWindupTurnSpeed => attackWindupTurnSpeed;
        public bool IsAttackValid(out string reason)
        {
            float[] values = { attackPlaybackSpeed, attackDamage, attackEnterRange, attackReach,
                attackAbortRange, attackHalfAngle, attackWindupTurnSpeed, attackCommitNormalized,
                attackContactNormalized, attackRecoveryNormalized, alternateAttackCommitNormalized,
                alternateAttackContactNormalized, alternateAttackRecoveryNormalized, alternateAttackEndNormalized };
            foreach (float v in values)
                if (float.IsNaN(v) || float.IsInfinity(v) || v <= 0)
                { reason = "Attack values must be finite and positive."; return false; }
            if (!attackClip || !alternateAttackClip || attackClip.length <= 0 || alternateAttackClip.length <= 0 ||
                attackClip.isLooping || alternateAttackClip.isLooping ||
                attackCommitNormalized >= attackContactNormalized || attackContactNormalized >= attackRecoveryNormalized ||
                alternateAttackCommitNormalized <= 0 || alternateAttackCommitNormalized >= alternateAttackContactNormalized ||
                alternateAttackContactNormalized >= alternateAttackRecoveryNormalized ||
                alternateAttackRecoveryNormalized >= alternateAttackEndNormalized || alternateAttackEndNormalized > 1 ||
                attackRecoveryNormalized >= 1 || attackEnterRange > attackReach || attackAbortRange <= attackReach ||
                attackHalfAngle >= 90 || attackPlaybackSpeed < .5f || attackPlaybackSpeed > 1.25f ||
                stopDistance > attackEnterRange + .3f)
            { reason = "Melee requires a non-looping clip, ordered commit/contact/recovery, bounded arc and reachable stop distance."; return false; }
            reason = null; return true;
        }
        public float SightDistance => sightDistance;
        public float HorizontalFov => horizontalFov;
        public float VerticalHalfAngle => verticalHalfAngle;
        public float PerceptionInterval => perceptionInterval;
        public float AcquisitionTime => acquisitionTime;
        public float ConfidenceDecayTime => confidenceDecayTime;
        public int OcclusionMask => occlusionMask.value;
        public float LossGrace => lossGrace;
        public float SearchDuration => searchDuration;
        public float Speed => speed;
        public float RunSpeed => runSpeed;
        public float Acceleration => acceleration;
        public float TurnSpeed => turnSpeed;
        public float StopDistance => stopDistance;
        public float ResumeDistance => resumeDistance;
        public float RepathInterval => repathInterval;
        public float RepathDistance => repathDistance;
        public float PathRetryInterval => pathRetryInterval;
        public int RecoveryAttempts => recoveryAttempts;
        public float StuckTimeout => stuckTimeout;
        public float StuckProgress => stuckProgress;
        public float SpawnSampleRadius => spawnSampleRadius;
        public float SearchRadius => searchRadius;
        public float SearchArrivalDistance => searchArrivalDistance;
        public float InspectionPause => inspectionPause;
        public float SearchSampleRadius => searchSampleRadius;
        public float MeasuredWalkSpeed => measuredWalkSpeed;
        public float MeasuredRunSpeed => measuredRunSpeed;
        public float AnimationBlendTime => animationBlendTime;

        public bool IsValid(out string reason)
        {
            if (!IsHearingValid) { reason = "Invalid hearing threshold, duration, arrival, sensitivity or transmission."; return false; }
            // Validation runs once at activation, never in the hot path.
            float[] positive = { sightDistance, horizontalFov, verticalHalfAngle, perceptionInterval,
                acquisitionTime, confidenceDecayTime, searchDuration, speed, runSpeed, acceleration, turnSpeed,
                stopDistance, resumeDistance, repathInterval, repathDistance, pathRetryInterval,
                stuckTimeout, stuckProgress, spawnSampleRadius, searchRadius, searchArrivalDistance,
                inspectionPause, searchSampleRadius, measuredWalkSpeed, measuredRunSpeed, animationBlendTime };
            foreach (float value in positive)
                if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0)
                { reason = "All timing/distance/rate values must be finite and positive."; return false; }
            if (float.IsNaN(lossGrace) || float.IsInfinity(lossGrace) || lossGrace < 0 ||
                horizontalFov >= 180 || verticalHalfAngle >= 90 || resumeDistance <= stopDistance ||
                stopDistance < .7f || sightDistance <= resumeDistance || runSpeed <= speed ||
                measuredRunSpeed <= measuredWalkSpeed || recoveryAttempts < 1 ||
                occlusionMask.value == 0 || searchDuration <= lossGrace)
            { reason = "Invalid FOV, stop/resume clearance, memory, retry count or world mask."; return false; }
            reason = null; return true;
        }
    }
}
