using Unity.Profiling;
using UnityEngine;

namespace LastSignal
{
    public sealed class ZombieAnimationPresenter : MonoBehaviour
    {
        static readonly ProfilerMarker Marker = new ProfilerMarker("LastSignal.Zombie.Presentation");
        static readonly int Idle = Animator.StringToHash("Idle"), Walk = Animator.StringToHash("Locomotion");
        [SerializeField] Animator animator;
        ZombieDefinition tuning;
        static readonly int Attack = Animator.StringToHash("Attack");
        bool attacking;
        AnimatorCullingMode savedCulling;
        int current;
        float smoothedSpeed;
        public bool HasAnimator => animator && animator.runtimeAnimatorController;
        public bool HasAttackPresentation(AnimationClip clip)
        {
            if (!HasAnimator || !animator.HasState(0, Attack)) return false;
            foreach (var motion in animator.runtimeAnimatorController.animationClips)
                if (motion == clip) return true;
            return false;
        }
        public void Configure(Animator value) => animator = value;
        public bool Initialize(ZombieDefinition definition)
        {
            tuning = definition;
            if (!animator || !animator.avatar || !animator.avatar.isValid || !animator.runtimeAnimatorController)
            { Debug.LogError("Zombie presentation requires the production Humanoid Animator and controller.", this); return false; }
            animator.applyRootMotion = false; smoothedSpeed = 0; current = Idle;
            var culling = animator.cullingMode; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind(); animator.speed = 1; animator.Play(Idle,0,0); animator.Update(0);
            animator.cullingMode = culling; return true;
        }
        public void BeginAttack()
        {
            if (!animator || attacking) return;
            attacking = true; current = Attack;
            savedCulling = animator.cullingMode; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 0; animator.Play(Attack, 0, 0); animator.Update(0);
        }
        public void AdvanceAttack(float seconds)
        {
            if (!animator || !attacking) return;
            // Manual presentation clock follows the controller; automatic Animator time is frozen.
            animator.speed = tuning.AttackPlaybackSpeed; animator.Update(seconds); animator.speed = 0;
        }
        public void EndAttack()
        {
            if (!animator || !attacking) return;
            attacking = false; animator.cullingMode = savedCulling;
            current = Idle; smoothedSpeed = 0; animator.speed = 1;
            animator.CrossFadeInFixedTime(Idle, tuning.AnimationBlendTime);
        }
        public void SetPaused(bool paused)
        { if (animator) animator.speed = paused || attacking ? 0 : 1; }
        public void Present(float actualSpeed, float seconds, bool paused)
        {
            if (!animator || !tuning || attacking) return;
            using (Marker.Auto())
            {
                if (paused) { animator.speed = 0; return; }
                smoothedSpeed = actualSpeed < .025f ? 0 : Mathf.MoveTowards(smoothedSpeed,actualSpeed,seconds*tuning.Acceleration);
                int desired = smoothedSpeed > (current == Walk ? .025f : .08f) ? Walk : Idle;
                if (desired != current) { current = desired; animator.CrossFadeInFixedTime(desired,tuning.AnimationBlendTime); }
                animator.speed = current == Idle ? 1 : Mathf.Clamp(smoothedSpeed/tuning.MeasuredWalkSpeed,.8f,1.15f);
            }
        }
    }
}
