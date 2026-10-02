using UnityEngine;

namespace LastSignal
{
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerStance : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] CharacterController capsule;
        [SerializeField] Transform view;
        [SerializeField, Min(.7f)] float standingHeightMeters = 1.8f;
        [SerializeField, Min(.7f)] float crouchingHeightMeters = 1.2f;
        [SerializeField, Min(.1f)] float eyeInsetMeters = .18f;
        [SerializeField] LayerMask clearanceMask = ~0;
        [SerializeField, Min(.1f)] float heightTransitionMetersPerSecond = 6f;
        FirstPersonMotor motor;
        readonly Collider[] overlaps = new Collider[32];
        public bool IsCrouching { get; private set; }
        public bool StandBlocked { get; private set; }
        public void Configure(PlayerInputReader reader, CharacterController controller, Transform camera)
        { input = reader; capsule = controller; view = camera; }
        void Awake()
        {
            motor = GetComponent<FirstPersonMotor>();
            if (!input || !capsule || !view || !motor)
            {
                Debug.LogError("Stance requires input, capsule, camera and motor.", this);
                enabled = false;
                return;
            }

            SetHeight(standingHeightMeters);
        }

        void OnEnable()
        {
            if (input) input.CrouchRequested += OnCrouchRequested;
        }

        void OnDisable()
        {
            if (input) input.CrouchRequested -= OnCrouchRequested;
        }

        void OnCrouchRequested()
        {
            if (motor.IsSliding) return;
            if (motor.TryStartSlide()) return;
            TrySetCrouching(!IsCrouching);
        }
        public bool TrySetCrouching(bool crouch)
        {
            if (!crouch && motor && motor.IsSliding) return false;
            if (IsCrouching == crouch) return true;

            StandBlocked = !crouch && !CanStand();
            if (StandBlocked) return false;

            IsCrouching = crouch;
            return true;
        }
        public bool CanStand()
        {
            float radius = capsule.radius - .015f;
            Vector3 foot = transform.position;
            int count = Physics.OverlapCapsuleNonAlloc(foot + Vector3.up * (capsule.radius + .04f),
                foot + Vector3.up * (standingHeightMeters - capsule.radius), radius,
                overlaps, clearanceMask, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return false;
            for (int i = 0; i < count; i++)
                if (overlaps[i] && !overlaps[i].transform.IsChildOf(transform)) return false;
            return true;
        }
        void LateUpdate()
        {
            float target = IsCrouching ? crouchingHeightMeters : standingHeightMeters;
            float next = Mathf.MoveTowards(
                capsule.height, target, heightTransitionMetersPerSecond * Time.deltaTime);

            // Hareket eden bir tavan genişleme sırasında araya girebilir.
            if (next > capsule.height && !CanStand())
            {
                IsCrouching = true;
                StandBlocked = true;
                next = Mathf.MoveTowards(
                    capsule.height, crouchingHeightMeters,
                    heightTransitionMetersPerSecond * Time.deltaTime);
            }

            SetHeight(next);
        }

        void SetHeight(float height)
        {
            capsule.height = height;
            capsule.center = Vector3.up * (height * .5f);
            view.localPosition = Vector3.up * (height - eyeInsetMeters);
        }
    }
}
