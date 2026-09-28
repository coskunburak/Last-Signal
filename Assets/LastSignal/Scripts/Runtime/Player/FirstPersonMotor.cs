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
        float verticalSpeed;
        Noise.FootstepNoiseProducer footsteps;
        public void BindNoise(Noise.GameplayNoiseSystem authority, Noise.GameplayNoiseTuning tuning, ulong source)
        { footsteps = new Noise.FootstepNoiseProducer(authority, tuning, source); }
        public void ResetNoiseCadence() => footsteps?.Reset();
        void OnDisable() { IsSprinting = false; ResetNoiseCadence(); }
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
            if (input.GameplayActive && (!health || health.IsAlive)) Simulate(input.Move, input.SprintHeld, Time.deltaTime);
            else { IsSprinting = false; ResetNoiseCadence(); }
        }
        public void Simulate(Vector2 intent, bool sprint, float seconds)
        {
            IsSprinting = false;
            if (!capsule.enabled || !float.IsFinite(seconds) || seconds <= 0) return;
            // Bounded substeps keep collisions/gravity reliable during a slow render frame.
            float remaining = Mathf.Min(seconds, .25f);
            Vector2 move = Vector2.ClampMagnitude(intent, 1);
            sprint = sprint && move.sqrMagnitude > .001f && !stance.IsCrouching && (!stamina || stamina.CanSprint);
            float speed = stance.IsCrouching ? crouchMetersPerSecond : sprint ? sprintMetersPerSecond : walkMetersPerSecond;
            Vector3 horizontal = transform.TransformDirection(new Vector3(move.x, 0, move.y)) * speed;
            while (remaining > .00001f)
            {
                float dt = Mathf.Min(remaining, 1f / 60f);
                if (capsule.isGrounded && verticalSpeed < 0) verticalSpeed = groundStickMetersPerSecond;
                verticalSpeed = Mathf.Max(terminalMetersPerSecond, verticalSpeed + gravityMetersPerSecondSquared * dt);
                Vector3 before = transform.position;
                CollisionFlags flags = capsule.Move((horizontal + Vector3.up * verticalSpeed) * dt);
                if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
                footsteps?.Advance(transform.position - before, transform.position, capsule.isGrounded, sprint, stance.IsCrouching);
                if (stamina)
                {
                    Vector3 displacement = transform.position - before; displacement.y = 0;
                    if (sprint && displacement.sqrMagnitude > .0000001f) IsSprinting = true;
                    stamina.Tick(dt, sprint && displacement.sqrMagnitude > .0000001f);
                    if (sprint && !stamina.CanSprint) { sprint = false; horizontal = transform.TransformDirection(new Vector3(move.x, 0, move.y)) * walkMetersPerSecond; }
                }
                else if (sprint)
                {
                    Vector3 displacement = transform.position - before; displacement.y = 0;
                    if (displacement.sqrMagnitude > .0000001f) IsSprinting = true;
                }
                remaining -= dt;
            }
            IsSprinting &= sprint && capsule.isGrounded;
        }
    }
}
