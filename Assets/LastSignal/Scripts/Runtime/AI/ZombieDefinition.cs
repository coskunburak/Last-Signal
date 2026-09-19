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
        [Header("Memory")]
        [SerializeField, Min(0)] float lossGrace = .45f;
        [SerializeField, Min(1)] float searchDuration = 12;
        [Header("Navigation")]
        [SerializeField, Min(.1f)] float speed = .92f;
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
        [SerializeField, Min(.1f)] float measuredWalkSpeed = .918734f;
        [SerializeField, Min(.01f)] float animationBlendTime = .12f;
        [Header("Melee — measured Zombie@Attack01")]
        [SerializeField] AnimationClip attackClip;
        [SerializeField, Range(.5f, 1.25f)] float attackPlaybackSpeed = .8f;
        [SerializeField, Range(0, 1)] float attackCommitNormalized = .15f;
        [SerializeField, Range(0, 1)] float attackContactNormalized = .25f;
        [SerializeField, Range(0, 1)] float attackRecoveryNormalized = .325f;
        [SerializeField, Min(1)] float attackDamage = 20;
        [SerializeField, Min(.1f)] float attackEnterRange = 1.08f;
        [SerializeField, Min(.1f)] float attackReach = 1.24f;
        [SerializeField, Min(.1f)] float attackAbortRange = 2.5f;
        [SerializeField, Range(1, 89)] float attackHalfAngle = 20;
        [SerializeField, Min(1)] float attackWindupTurnSpeed = 60;
        public AnimationClip AttackClip => attackClip;
        public float AttackPlaybackSpeed => attackPlaybackSpeed;
        public float AttackDuration => attackClip ? attackClip.length / attackPlaybackSpeed : 0;
        public float AttackCommitTime => AttackDuration * attackCommitNormalized;
        public float AttackContactTime => AttackDuration * attackContactNormalized;
        public float AttackRecoveryTime => AttackDuration * attackRecoveryNormalized;
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
                attackContactNormalized, attackRecoveryNormalized };
            foreach (float v in values)
                if (float.IsNaN(v) || float.IsInfinity(v) || v <= 0)
                { reason = "Attack values must be finite and positive."; return false; }
            if (!attackClip || attackClip.length <= 0 || attackClip.isLooping ||
                attackCommitNormalized >= attackContactNormalized || attackContactNormalized >= attackRecoveryNormalized ||
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
        public float AnimationBlendTime => animationBlendTime;

        public bool IsValid(out string reason)
        {
            // Validation runs once at activation, never in the hot path.
            float[] positive = { sightDistance, horizontalFov, verticalHalfAngle, perceptionInterval,
                acquisitionTime, confidenceDecayTime, searchDuration, speed, acceleration, turnSpeed,
                stopDistance, resumeDistance, repathInterval, repathDistance, pathRetryInterval,
                stuckTimeout, stuckProgress, spawnSampleRadius, searchRadius, searchArrivalDistance,
                inspectionPause, searchSampleRadius, measuredWalkSpeed, animationBlendTime };
            foreach (float value in positive)
                if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0)
                { reason = "All timing/distance/rate values must be finite and positive."; return false; }
            if (float.IsNaN(lossGrace) || float.IsInfinity(lossGrace) || lossGrace < 0 ||
                horizontalFov >= 180 || verticalHalfAngle >= 90 || resumeDistance <= stopDistance ||
                stopDistance < .7f || sightDistance <= resumeDistance || recoveryAttempts < 1 ||
                occlusionMask.value == 0 || searchDuration <= lossGrace)
            { reason = "Invalid FOV, stop/resume clearance, memory, retry count or world mask."; return false; }
            reason = null; return true;
        }
    }
}
