using Unity.Profiling;
using UnityEngine;

namespace LastSignal
{
    public sealed class ZombieAnimationPresenter : MonoBehaviour
    {
        static readonly ProfilerMarker Marker = new ProfilerMarker("LastSignal.Zombie.Presentation");
        static readonly int Idle = Animator.StringToHash("Idle");
        static readonly int Walk = Animator.StringToHash("Locomotion");
        static readonly int Run = Animator.StringToHash("Run");
        static readonly int Attack = Animator.StringToHash("Attack");
        static readonly int AttackAlternate = Animator.StringToHash("AttackAlternate");
        static readonly int HitReact = Animator.StringToHash("HitReact");
        static readonly int HitReactSpeed = Animator.StringToHash("HitReactSpeed");
        static readonly int Death = Animator.StringToHash("Death");
        static readonly int FlyingBackDeath = Animator.StringToHash("FlyingBackDeath");
        [SerializeField] Animator animator;
        ZombieDefinition tuning;
        bool damagePresentation, dead, paused, attacking, proceduralReaction;
        float damageTime, activeDeathDuration, smoothedSpeed, locomotionPlayback = 1;
        int current;
        AnimatorCullingMode savedCulling;
        Transform reactionBone;
        Vector3 visualBasePosition;
        bool flyingDeath, vehicleKnockdown, vehicleDeath;
        float vehicleDeathStart;
        static readonly int VehicleFall = Animator.StringToHash("VehicleFall");
        static readonly int VehicleGetUp = Animator.StringToHash("VehicleGetUp");
        public bool VehicleKnockdown => vehicleKnockdown;
        public void BeginVehicleKnockdown(ZombieImpactReaction impact)
        {
            if (!HasAnimator || dead || vehicleKnockdown) return;
            BeginDamage(false, impact);
            vehicleKnockdown = true; proceduralReaction = false;
            animator.SetLayerWeight(1, 0);
            current = VehicleFall; animator.Play(VehicleFall, 0, 0); animator.Update(0);
        }
        public void BeginVehicleDeath(ZombieImpactReaction impact)
        {
            if (!HasAnimator || dead) return;
            float phase = vehicleKnockdown ? Mathf.Clamp01(damageTime / tuning.VehicleFallDuration) : 0;
            BeginDamage(true, impact);
            vehicleDeath = true; vehicleDeathStart = phase;
            current = VehicleFall; animator.Play(current, 0, phase); animator.Update(0);
        }
        ZombieImpactReaction reaction;

        public bool CorpseSettled { get; private set; }
        public float DamageTime => damageTime;
        public bool HasAnimator => animator && animator.runtimeAnimatorController;
        public void Configure(Animator value) => animator = value;

        public bool HasAttackPresentation(AnimationClip clip)
        {
            if (!HasAnimator || !animator.HasState(0, Attack)) return false;
            foreach (var motion in animator.runtimeAnimatorController.animationClips)
                if (motion == clip) return true;
            return false;
        }

        public bool Initialize(ZombieDefinition definition)
        {
            tuning = definition;
            if (!animator || !animator.avatar || !animator.avatar.isValid ||
                !animator.runtimeAnimatorController || !HasHitSpeedParameter())
            { Debug.LogError("Zombie presentation requires the production Humanoid Animator and controller.", this); return false; }
            reactionBone = animator.GetBoneTransform(HumanBodyBones.Chest);
            if (!reactionBone) reactionBone = animator.GetBoneTransform(HumanBodyBones.Spine);
            visualBasePosition = animator.transform.localPosition;
            damagePresentation = dead = paused = attacking = proceduralReaction = vehicleKnockdown = CorpseSettled = false;
            vehicleDeath = false; vehicleDeathStart = 0;
            damageTime = smoothedSpeed = 0; locomotionPlayback = 1; current = Idle;
            animator.enabled = true; animator.applyRootMotion = false;
            var culling = animator.cullingMode; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind(); animator.speed = 1; animator.SetFloat(HitReactSpeed, 1); animator.Play(Idle, 0, 0);
            if (animator.layerCount > 1) animator.SetLayerWeight(1, 0);
            animator.Update(0); animator.cullingMode = culling;
            return true;
        }

        public bool HasDamagePresentation() => tuning && tuning.IsDamagePresentationValid &&
            animator && animator.layerCount > 1 && animator.HasState(1, HitReact) &&
            animator.HasState(0, Death) && animator.HasState(0, FlyingBackDeath) &&
            HasHitSpeedParameter() &&
            HasAttackPresentation(tuning.HitReactClip) &&
            HasAttackPresentation(tuning.DeathClip) &&
            HasAttackPresentation(tuning.FlyingBackDeathClip);

        bool HasHitSpeedParameter()
        {
            foreach (var parameter in animator.parameters)
                if (parameter.nameHash == HitReactSpeed && parameter.type == AnimatorControllerParameterType.Float)
                    return true;
            return false;
        }

        public void BeginDamage(bool lethal) => BeginDamage(lethal, ZombieImpactReaction.Legacy);
        public void BeginDamage(bool lethal, ZombieImpactReaction impact)
        {
            if (!animator || dead) return;
            EndAttack(); vehicleKnockdown = false;
            if (!damagePresentation) savedCulling = animator.cullingMode;
            damagePresentation = true; dead = lethal; damageTime = 0; CorpseSettled = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            reaction = impact;
            proceduralReaction = !lethal && reactionBone &&
                (!impact.UseLegacyClip || impact.Side != ZombieImpactSide.Front);
            animator.SetLayerWeight(1, 0);
            if (lethal)
            {
                current = impact.UseFlyingBackDeath ? FlyingBackDeath : Death;
                flyingDeath = current == FlyingBackDeath;
                activeDeathDuration = current == FlyingBackDeath ? tuning.FlyingBackDeathClip.length : tuning.DeathClip.length;
                animator.Play(current, 0, 0);
                animator.speed = 1; animator.Update(0); animator.speed = 0;
            }
            else
            {
                if (impact.UseLegacyClip)
                {
                    animator.SetFloat(HitReactSpeed, 1f / locomotionPlayback);
                    animator.Play(HitReact, 1, 0);
                    // Commit the overlay state before the first timed step; otherwise the first
                    // damage tick only enters the state and its normalized time remains zero.
                    animator.speed = 1; animator.Update(0);
                    animator.SetFloat(HitReactSpeed, 1f / locomotionPlayback);
                }
                animator.speed = 0;
            }
        }

        public void AdvanceDamage(float seconds)
        {
            if (!animator || !damagePresentation || paused || CorpseSettled || seconds <= 0) return;
            if (vehicleDeath)
            {
                damageTime = Mathf.Min(damageTime + seconds, .55f);
                animator.Play(VehicleFall, 0, Mathf.Lerp(vehicleDeathStart, 1, damageTime / .55f));
                animator.Update(0);
                if (damageTime >= .55f) { CorpseSettled = true; animator.enabled = false; }
                return;
            }
            if (vehicleKnockdown && !dead)
            {
                damageTime = Mathf.Min(damageTime + seconds, tuning.VehicleReactionDuration);
                float getUpStart = tuning.VehicleFallDuration + tuning.VehicleGroundDuration;
                bool rising = damageTime >= getUpStart;
                current = rising ? VehicleGetUp : VehicleFall;
                float phase = rising ? (damageTime - getUpStart) / tuning.VehicleGetUpDuration
                    : damageTime / tuning.VehicleFallDuration;
                animator.Play(current, 0, Mathf.Clamp01(phase)); animator.Update(0);
                return;
            }
            float duration = dead ? activeDeathDuration : tuning.HitReactDuration;
            float step = Mathf.Min(seconds, Mathf.Max(0, duration - damageTime));
            damageTime += step;
            if (!dead && reaction.UseLegacyClip)
            {
                float fadeIn = Mathf.Clamp01(damageTime / .08f);
                float fadeOut = Mathf.Clamp01((duration - damageTime) / .12f);
                animator.SetLayerWeight(1, Mathf.Min(fadeIn, fadeOut));
            }
            if (!dead && reaction.UseLegacyClip) animator.SetFloat(HitReactSpeed, 1f / locomotionPlayback);
            animator.speed = 1;
            animator.Update(dead ? step : step * locomotionPlayback);
            animator.speed = 0;
            if (dead)
                animator.transform.localPosition = visualBasePosition +
                    Vector3.up * tuning.DeathGroundOffset(damageTime, flyingDeath);
            if (proceduralReaction)
            {
                float phase = Mathf.Clamp01(damageTime / duration);
                float angle = Mathf.Sin(phase * Mathf.PI) *
                    (reaction.Severity == ZombieImpactSeverity.Light ? tuning.LightImpactAngle : tuning.HeavyImpactAngle);
                Vector3 axis = reaction.Side == ZombieImpactSide.Left || reaction.Side == ZombieImpactSide.Right
                    ? animator.transform.forward : animator.transform.right;
                if (reaction.Side == ZombieImpactSide.Front || reaction.Side == ZombieImpactSide.Left) angle = -angle;
                reactionBone.rotation = Quaternion.AngleAxis(angle, axis) * reactionBone.rotation;
            }
            if (dead && damageTime >= duration)
            { CorpseSettled = true; animator.enabled = false; }
        }

        public void EndReaction()
        {
            if (!animator || !damagePresentation || dead) return;
            bool wasKnockedDown = vehicleKnockdown;
            damagePresentation = proceduralReaction = vehicleKnockdown = false;
            if (wasKnockedDown) { current = Idle; animator.CrossFadeInFixedTime(Idle, tuning.AnimationBlendTime); }
            animator.SetLayerWeight(1, 0);
            animator.cullingMode = savedCulling;
            animator.speed = paused ? 0 : locomotionPlayback;
            if (!paused) animator.Update(0);
        }

        public void BeginAttack() => BeginAttack(false);
        public void BeginAttack(bool alternate)
        {
            if (!animator || attacking || damagePresentation) return;
            attacking = true; current = alternate ? AttackAlternate : Attack;
            savedCulling = animator.cullingMode; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 0; animator.Play(current, 0, 0); animator.Update(0);
        }
        public void AdvanceAttack(float seconds)
        {
            if (!animator || !attacking || paused) return;
            animator.speed = tuning.AttackPlaybackSpeed; animator.Update(seconds); animator.speed = 0;
        }
        public void EndAttack()
        {
            if (!animator || !attacking) return;
            attacking = false; animator.cullingMode = savedCulling;
            current = Idle; smoothedSpeed = 0; locomotionPlayback = 1;
            animator.speed = paused ? 0 : 1;
            animator.CrossFadeInFixedTime(Idle, tuning.AnimationBlendTime);
        }
        public void SetPaused(bool value)
        { paused = value; if (animator) animator.speed = value || attacking || damagePresentation ? 0 : locomotionPlayback; }

        public void Present(float actualSpeed, float seconds, bool isPaused) => Present(actualSpeed, false, seconds, isPaused);
        public void Present(float actualSpeed, bool running, float seconds, bool isPaused)
        {
            if (!animator || !tuning || attacking || dead || vehicleKnockdown) return;
            using (Marker.Auto())
            {
                if (isPaused || paused) { animator.speed = 0; return; }
                smoothedSpeed = actualSpeed < .025f ? 0 : Mathf.MoveTowards(smoothedSpeed, actualSpeed, seconds * tuning.Acceleration);
                int desired = smoothedSpeed > (current == Idle ? .08f : .025f) ? running ? Run : Walk : Idle;
                if (desired != current)
                { current = desired; animator.CrossFadeInFixedTime(desired, tuning.AnimationBlendTime); }
                locomotionPlayback = current == Idle ? 1 : current == Run
                    ? Mathf.Clamp(smoothedSpeed / tuning.MeasuredRunSpeed, .8f, 1.15f)
                    : Mathf.Clamp(smoothedSpeed / tuning.MeasuredWalkSpeed, .8f, 3.2f);
                animator.speed = damagePresentation ? 0 : locomotionPlayback;
            }
        }
    }
}
