using UnityEngine;

namespace LastSignal
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerLocomotionPresenter : MonoBehaviour
    {
        static readonly int MoveX = Animator.StringToHash("MoveX");
        static readonly int MoveY = Animator.StringToHash("MoveY");
        static readonly int HorizontalSpeed = Animator.StringToHash("HorizontalSpeed");
        static readonly int PlaybackRate = Animator.StringToHash("PlaybackRate");
        static readonly int Crouching = Animator.StringToHash("IsCrouching");
        static readonly int Sliding = Animator.StringToHash("IsSliding");

        [SerializeField, Min(0f)] float directionDampingSeconds = .08f;
        [SerializeField, Range(.1f, 1f)] float walkingPlaybackRate = .65f;
        [SerializeField, Min(.1f)] float crouchMetersPerSecond = 1.6f;

        Animator bodyAnimator;
        PlayerInputReader input;
        PlayerStance stance;
        FirstPersonMotor motor;

        void Awake()
        {
            bodyAnimator = GetComponent<Animator>();
            input = GetComponentInParent<PlayerInputReader>();
            stance = GetComponentInParent<PlayerStance>();
            motor = GetComponentInParent<FirstPersonMotor>();
            if (bodyAnimator && input && stance && motor) return;

            Debug.LogError("Player locomotion requires Animator, input, stance and motor.", this);
            enabled = false;
        }

        void LateUpdate()
        {
            Vector2 direction = input.GameplayActive ? input.Move : Vector2.zero;
            bool sliding = motor.IsSliding;
            if (stance.IsCrouching)
                direction *= Mathf.Clamp01(motor.HorizontalMetersPerSecond / crouchMetersPerSecond);
            bodyAnimator.SetBool(Crouching, stance.IsCrouching);
            bodyAnimator.SetBool(Sliding, sliding);
            bodyAnimator.SetFloat(HorizontalSpeed, motor.HorizontalMetersPerSecond);
            bodyAnimator.SetFloat(PlaybackRate, motor.IsSprinting ? 1f : walkingPlaybackRate);
            bodyAnimator.SetFloat(MoveX, direction.x, directionDampingSeconds, Time.deltaTime);
            bodyAnimator.SetFloat(MoveY, direction.y, directionDampingSeconds, Time.deltaTime);
        }
    }
}
