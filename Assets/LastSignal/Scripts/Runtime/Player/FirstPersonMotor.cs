using UnityEngine;

namespace LastSignal
{
    public sealed class FirstPersonMotor : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] CharacterController capsule;
        [SerializeField] PlayerStance stance;
        [SerializeField, Min(0)] float walkMetersPerSecond = 3.2f;
        [SerializeField, Min(0)] float sprintMetersPerSecond = 5.5f;
        [SerializeField, Min(0)] float crouchMetersPerSecond = 1.6f;
        [SerializeField] float gravityMetersPerSecondSquared = -22f;
        [SerializeField] float groundStickMetersPerSecond = -2f;
        [SerializeField] float terminalMetersPerSecond = -40f;
        [SerializeField, Min(0)] float slideMinimumEntryMetersPerSecond = 4f;
        [SerializeField, Min(0)] float slideEntryMultiplier = 1.1f;
        [SerializeField, Min(0)] float slideMaximumMetersPerSecond = 7.5f;
        [SerializeField, Min(0)] float slideDecelerationMetersPerSecondSquared = 7f;
        [SerializeField, Min(0)] float slideExitMetersPerSecond = 2f;
        [SerializeField, Min(.05f)] float slideMaximumSeconds = .8f;
        [SerializeField, Range(0, 180)] float slideSteeringDegreesPerSecond = 75f;
        [SerializeField, Range(0, 1)] float slideSlopeGravityFactor = .45f;
        [SerializeField] LayerMask slideGroundLayers = ~0;

        readonly RaycastHit[] slideGroundHits = new RaycastHit[8];
        Vector3 measuredHorizontalVelocity;
        Vector3 slideVelocity;
        float slideElapsedSeconds;

        public event System.Action<Vector3, bool, bool, bool> PresentationMoved;
        public bool IsSliding { get; private set; }
        public float HorizontalMetersPerSecond => measuredHorizontalVelocity.magnitude;
        float verticalSpeed;
        Noise.FootstepNoiseProducer footsteps;
        public void BindNoise(Noise.GameplayNoiseSystem authority, Noise.GameplayNoiseTuning tuning, ulong source)
        { footsteps = new Noise.FootstepNoiseProducer(authority, tuning, source); }
        public void ResetNoiseCadence() => footsteps?.Reset();
        void OnDisable()
        {
            IsSprinting = false;
            EndSlide(true);
            ResetNoiseCadence();
        }        
        PlayerStamina stamina;
        PlayerHealth health;
        public bool Grounded => capsule.isGrounded;
        public bool IsSprinting { get; private set; }
        public void Configure(PlayerInputReader reader, CharacterController controller, PlayerStance playerStance)
        { input = reader; capsule = controller; stance = playerStance; }
        void Awake()
        {
            stamina = GetComponent<PlayerStamina>();
            health = GetComponent<PlayerHealth>();
            if (!input || !capsule || !stance) { Debug.LogError("Motor requires input, capsule and stance.", this); enabled = false; }
        }
        void Update()
        {
            if (input.GameplayActive && (!health || health.IsAlive))
            {
                Simulate(input.Move, input.SprintHeld, Time.deltaTime);
            }
            else
            {
                IsSprinting = false;
                EndSlide(true);
                measuredHorizontalVelocity = Vector3.zero;
                ResetNoiseCadence();
            }
        }
        public void Simulate(Vector2 intent, bool sprintIntent, float seconds)
        {
            IsSprinting = false;
            if (!capsule.enabled || !float.IsFinite(seconds) || seconds <= 0) return;

            float remaining = Mathf.Min(seconds, .25f);
            Vector2 move = Vector2.ClampMagnitude(intent, 1f);

            while (remaining > .00001f)
            {
                float dt = Mathf.Min(remaining, 1f / 60f);
                SimulateStep(move, sprintIntent, dt);
                remaining -= dt;
            }

            IsSprinting &= capsule.isGrounded;
        }

        public bool TryStartSlide()
        {
            if (IsSliding || !input.GameplayActive || !capsule.enabled ||
                !capsule.isGrounded || !IsSprinting ||
                measuredHorizontalVelocity.magnitude < slideMinimumEntryMetersPerSecond)
                return false;

            if (!stance.TrySetCrouching(true)) return false;

            IsSliding = true;
            IsSprinting = false;
            slideElapsedSeconds = 0f;

            float entrySpeed = Mathf.Min(
                Mathf.Max(slideMaximumMetersPerSecond, slideMinimumEntryMetersPerSecond),
                measuredHorizontalVelocity.magnitude * slideEntryMultiplier);

            slideVelocity = measuredHorizontalVelocity.normalized * entrySpeed;
            ResetNoiseCadence();
            return true;
        }

        void SimulateStep(Vector2 move, bool sprintIntent, float dt)
        {
            bool sprint = sprintIntent && !IsSliding &&
                move.sqrMagnitude > .001f && !stance.IsCrouching &&
                (!stamina || stamina.CanSprint);

            float speed = stance.IsCrouching
                ? crouchMetersPerSecond
                : sprint ? sprintMetersPerSecond : walkMetersPerSecond;

            Vector3 horizontal =
                transform.TransformDirection(new Vector3(move.x, 0, move.y)) * speed;

            if (IsSliding)
            {
                AdvanceSlide(move, dt);
                horizontal = slideVelocity;
                sprint = false;
            }

            if (capsule.isGrounded && verticalSpeed < 0)
                verticalSpeed = groundStickMetersPerSecond;

            verticalSpeed = Mathf.Max(
                terminalMetersPerSecond,
                verticalSpeed + gravityMetersPerSecondSquared * dt);

            Vector3 before = transform.position;
            CollisionFlags flags =
                capsule.Move((horizontal + Vector3.up * verticalSpeed) * dt);

            if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0)
                verticalSpeed = 0;

            Vector3 displacement = transform.position - before;
            if (IsSliding) ResetNoiseCadence();
            else footsteps?.Advance(
                displacement, transform.position, capsule.isGrounded,
                sprint, stance.IsCrouching);

            PresentationMoved?.Invoke(displacement, capsule.isGrounded && !IsSliding, sprint, stance.IsCrouching);
            displacement.y = 0;
            measuredHorizontalVelocity = displacement / dt;
            bool moved = displacement.sqrMagnitude > .0000001f;

            if (stamina) stamina.Tick(dt, sprint && moved);
            IsSprinting = sprint && moved && (!stamina || stamina.CanSprint);

            if (IsSliding &&
                (!capsule.isGrounded ||
                measuredHorizontalVelocity.magnitude <= slideExitMetersPerSecond ||
                slideElapsedSeconds >= slideMaximumSeconds))
                EndSlide(false);
        }

        void AdvanceSlide(Vector2 move, float dt)
        {
            slideElapsedSeconds += dt;
            slideVelocity = Vector3.MoveTowards(
                slideVelocity, Vector3.zero,
                slideDecelerationMetersPerSecondSquared * dt);

            if (TryGetSlideGroundNormal(out Vector3 normal))
            {
                Vector3 downhill = Vector3.ProjectOnPlane(Physics.gravity, normal);
                downhill.y = 0;
                slideVelocity += downhill * (slideSlopeGravityFactor * dt);
            }

            Vector3 desired =
                transform.TransformDirection(new Vector3(move.x, 0, move.y));

            if (desired.sqrMagnitude > .001f &&
                slideVelocity.sqrMagnitude > .0001f)
            {
                slideVelocity = Vector3.RotateTowards(
                    slideVelocity, desired.normalized * slideVelocity.magnitude,
                    slideSteeringDegreesPerSecond * Mathf.Deg2Rad * dt, 0);
            }

            slideVelocity.y = 0;
            slideVelocity = Vector3.ClampMagnitude(
                slideVelocity,
                Mathf.Max(slideMaximumMetersPerSecond,
                        slideMinimumEntryMetersPerSecond));
        }

        bool TryGetSlideGroundNormal(out Vector3 normal)
        {
            normal = Vector3.up;
            Vector3 origin =
                transform.position + Vector3.up * (capsule.radius + .2f);
            float distance = capsule.radius + .5f;
            int count = Physics.RaycastNonAlloc(
                origin, Vector3.down, slideGroundHits, distance,
                slideGroundLayers, QueryTriggerInteraction.Ignore);

            float nearest = float.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = slideGroundHits[i];
                if (!hit.collider ||
                    hit.collider.transform.IsChildOf(transform) ||
                    hit.distance >= nearest ||
                    Vector3.Angle(hit.normal, Vector3.up) > capsule.slopeLimit)
                    continue;

                nearest = hit.distance;
                normal = hit.normal;
                found = true;
            }

            return found;
        }

        void EndSlide(bool stayCrouched)
        {
            if (!IsSliding) return;

            IsSliding = false;
            slideVelocity = Vector3.zero;
            slideElapsedSeconds = 0f;

            if (!stayCrouched && !input.CrouchHeld)
                stance.TrySetCrouching(false);
        }
    }
}
