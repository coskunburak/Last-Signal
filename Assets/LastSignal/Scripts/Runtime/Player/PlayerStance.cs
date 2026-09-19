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
        readonly Collider[] overlaps = new Collider[32];
        public bool IsCrouching { get; private set; }
        public bool StandBlocked { get; private set; }
        public void Configure(PlayerInputReader reader, CharacterController controller, Transform camera)
        { input = reader; capsule = controller; view = camera; }
        void Awake()
        {
            if (!input || !capsule || !view) { Debug.LogError("Stance requires input, capsule and camera.", this); enabled = false; return; }
            SetHeight(standingHeightMeters);
        }
        void OnEnable() { if (input) input.CrouchRequested += Toggle; }
        void OnDisable() { if (input) input.CrouchRequested -= Toggle; }
        void Toggle() => TrySetCrouching(!IsCrouching);
        public bool TrySetCrouching(bool crouch)
        {
            StandBlocked = !crouch && !CanStand();
            if (StandBlocked) return false;
            IsCrouching = crouch;
            SetHeight(crouch ? crouchingHeightMeters : standingHeightMeters);
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
        void SetHeight(float height)
        {
            capsule.height = height;
            capsule.center = Vector3.up * (height * .5f);
            view.localPosition = Vector3.up * (height - eyeInsetMeters);
        }
    }
}
