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
        public bool Grounded => capsule.isGrounded;
        public void Configure(PlayerInputReader reader, CharacterController controller, PlayerStance playerStance)
        { input = reader; capsule = controller; stance = playerStance; }
        void Awake()
        {
            if (!input || !capsule || !stance) { Debug.LogError("Motor requires input, capsule and stance.", this); enabled = false; }
        }
        void Update()
        {
            if (input.GameplayActive) Simulate(input.Move, input.SprintHeld, Time.deltaTime);
        }
        public void Simulate(Vector2 intent, bool sprint, float seconds)
        {
            if (!capsule.enabled || seconds <= 0) return;
            // Bounded substeps keep collisions/gravity reliable during a slow render frame.
            float remaining = Mathf.Min(seconds, .25f);
            Vector2 move = Vector2.ClampMagnitude(intent, 1);
            float speed = stance.IsCrouching ? crouchMetersPerSecond : sprint ? sprintMetersPerSecond : walkMetersPerSecond;
            Vector3 horizontal = transform.TransformDirection(new Vector3(move.x, 0, move.y)) * speed;
            while (remaining > .00001f)
            {
                float dt = Mathf.Min(remaining, 1f / 60f);
                if (capsule.isGrounded && verticalSpeed < 0) verticalSpeed = groundStickMetersPerSecond;
                verticalSpeed = Mathf.Max(terminalMetersPerSecond, verticalSpeed + gravityMetersPerSecondSquared * dt);
                CollisionFlags flags = capsule.Move((horizontal + Vector3.up * verticalSpeed) * dt);
                if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
                remaining -= dt;
            }
        }
    }
}
