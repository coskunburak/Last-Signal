using UnityEngine;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;

namespace LastSignal
{
    /// <summary>One world Animator writer. Reads gameplay; never completes gameplay actions.</summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent, RequireComponent(typeof(Animator))]
    public sealed class PlayerLocomotionPresenter : MonoBehaviour
    {
        static readonly int MoveX = Animator.StringToHash("MoveX"), MoveY = Animator.StringToHash("MoveY"),
            Speed = Animator.StringToHash("HorizontalSpeed"), Rate = Animator.StringToHash("PlaybackRate"),
            Crouch = Animator.StringToHash("IsCrouching"), Slide = Animator.StringToHash("IsSliding"),
            Ground = Animator.StringToHash("Grounded"), Sprint = Animator.StringToHash("Sprinting"),
            Vertical = Animator.StringToHash("VerticalVelocity"), Direction = Animator.StringToHash("MovementDirection"),
            Dead = Animator.StringToHash("Dead"), Seated = Animator.StringToHash("Seated"),
            Aim = Animator.StringToHash("AimAmount"), Weapon = Animator.StringToHash("WeaponState"),
            ReloadTime = Animator.StringToHash("ReloadTime"), Melee = Animator.StringToHash("MeleeActive"), MeleeTime = Animator.StringToHash("MeleeTime"),
            Treatment = Animator.StringToHash("Treating"), Land = Animator.StringToHash("Land"),
            Interact = Animator.StringToHash("Interact"), Pickup = Animator.StringToHash("Pickup"),
            Consume = Animator.StringToHash("Consume"), Hit = Animator.StringToHash("Hit");
        static readonly int[] Transients = { Land, Interact, Pickup, Consume, Hit };
        [SerializeField, Min(0)] float directionDampingSeconds = .08f;
        [SerializeField, Min(.1f)] float runReferenceMetersPerSecond = 3.2f;
        [SerializeField, Min(.1f)] float crouchMetersPerSecond = 1.6f;
        Animator bodyAnimator;
        PlayerInputReader input;
        PlayerStance stance;
        FirstPersonMotor motor;
        PlayerCombatController combat;
        PlayerHealth health;
        PlayerSurvival survival;
        InteractionController interaction;
        bool wasGrounded, hasSample, wasAlive;
        float pendingActionSeconds;
        bool actionInterrupted;
        public Animator BodyAnimator => bodyAnimator;
        public bool SeatedNow => input && input.InVehicle;
        public bool Alive => !health || health.IsAlive;
        public bool PresentationActive => !input || input.GameplayActive || input.DrivingActive || !Alive;
        public float AimAmount { get; private set; }
        public bool UpperActionBusy => bodyAnimator && bodyAnimator.layerCount > 1 &&
            (bodyAnimator.GetCurrentAnimatorStateInfo(1).IsTag("Action") ||
             (bodyAnimator.IsInTransition(1) && bodyAnimator.GetNextAnimatorStateInfo(1).IsTag("Action")));
        public bool Treating => survival && survival.ApplyingTreatment;
        public bool Crouching => !SeatedNow && stance && stance.IsCrouching;
        public bool Sliding => motor && motor.IsSliding;
        public PlayerCombatController Combat => combat;
        public FirstPersonMotor Motor => motor;
        public PlayerInputReader Input => input;
        public void BindSurvival(PlayerSurvival value)
        {
            if (survival) survival.ItemConsumed -= OnConsumed;
            survival = value;
            if (survival && isActiveAndEnabled) survival.ItemConsumed += OnConsumed;
        }

        void Awake()
        {
            bodyAnimator = GetComponent<Animator>();
            input = GetComponentInParent<PlayerInputReader>(); stance = GetComponentInParent<PlayerStance>();
            motor = GetComponentInParent<FirstPersonMotor>(); combat = GetComponentInParent<PlayerCombatController>();
            health = GetComponentInParent<PlayerHealth>(); survival = GetComponentInParent<PlayerSurvival>();
            interaction = GetComponentInParent<InteractionController>();
            bodyAnimator.applyRootMotion = false;
            if (!bodyAnimator.runtimeAnimatorController || !motor || !stance)
            { Debug.LogError("World character requires its controller, motor and stance.", this); enabled = false; }
        }
        void OnEnable()
        {
            hasSample = false; wasAlive = Alive;
            if (health) health.DamageAccepted += OnDamage;
            if (interaction) interaction.InteractionPresented += OnInteraction;
            if (survival) survival.ItemConsumed += OnConsumed;
        }
        void OnDisable()
        {
            if (health) health.DamageAccepted -= OnDamage;
            if (interaction) interaction.InteractionPresented -= OnInteraction;
            if (survival) survival.ItemConsumed -= OnConsumed;
            ResetTransients();
            if (bodyAnimator) bodyAnimator.speed = 1;
        }
        void ResetTransients()
        {
            pendingActionSeconds = 0;
            actionInterrupted = false;
            if (bodyAnimator && bodyAnimator.runtimeAnimatorController)
            {
                foreach (int id in Transients) bodyAnimator.ResetTrigger(id);
                if (bodyAnimator.layerCount > 1) bodyAnimator.SetLayerWeight(1, 0);
            }
        }
        public void InterruptCosmeticAction()
        {
            if (!bodyAnimator || bodyAnimator.layerCount < 2) return;
            bodyAnimator.ResetTrigger(Interact); bodyAnimator.ResetTrigger(Pickup);
            bodyAnimator.ResetTrigger(Consume); bodyAnimator.ResetTrigger(Hit);
            pendingActionSeconds = 0;
            actionInterrupted = true;
            bodyAnimator.SetLayerWeight(1, 0);
            bodyAnimator.CrossFadeInFixedTime("Character Actions.No Action", .08f, 1);
        }
        void RequestAction(int trigger)
        {
            if (!Alive || SeatedNow || !bodyAnimator) return;
            pendingActionSeconds = .5f;
            actionInterrupted = false;
            if (bodyAnimator.layerCount > 1) bodyAnimator.SetLayerWeight(1, 1);
            bodyAnimator.SetTrigger(trigger);
        }
        void OnDamage(DamageInfo info) { if (Alive) RequestAction(Hit); }
        void OnInteraction(IInteractable target)
        {
            if (!Alive || SeatedNow) return;
            RequestAction(target is WorldItem ? Pickup : Interact);
        }
        void OnConsumed(ItemUse use)
        { if (use == ItemUse.Hydrate || use == ItemUse.Nourish) RequestAction(Consume); }

        void Update() => Present(Time.deltaTime);
        public void Present(float seconds)
        {
            if (!bodyAnimator || !motor || !stance) return;
            float dt = float.IsFinite(seconds) ? Mathf.Max(0, seconds) : 0;
            bool alive = Alive, seated = SeatedNow;
            if (alive != wasAlive)
            {
                ResetTransients();
                if (alive) { bodyAnimator.Rebind(); hasSample = false; }
                wasAlive = alive;
            }
            bodyAnimator.speed = PresentationActive ? 1 : 0;
            bodyAnimator.updateMode = alive ? AnimatorUpdateMode.Normal : AnimatorUpdateMode.UnscaledTime;
            Vector3 velocity = alive && !seated ? motor.transform.InverseTransformDirection(motor.HorizontalVelocity) : Vector3.zero;
            if (!PresentationActive) velocity = Vector3.zero;
            float speed = velocity.magnitude;
            Vector2 direction = new Vector2(velocity.x, velocity.z);
            if (direction.sqrMagnitude > .0001f) direction.Normalize();
            if (Crouching) direction *= Mathf.Clamp01(speed / crouchMetersPerSecond);
            bool grounded = motor.Grounded;
            if (hasSample && !wasGrounded && grounded && alive && !seated && !Crouching && !Sliding) bodyAnimator.SetTrigger(Land);
            if (Crouching || Sliding || seated || !alive) bodyAnimator.ResetTrigger(Land);
            wasGrounded = grounded; hasSample = !seated;
            AimAmount = combat && combat.ActiveWeapon && combat.ActiveWeapon.RuntimeState != null
                ? combat.ActiveWeapon.RuntimeState.AimAmount : 0;
            var state = combat && combat.ActiveWeapon ? combat.ActiveWeapon.RuntimeState : null;
            float reload = 0;
            if (state != null && state.State == LastSignal.WeaponState.Reloading)
            {
                float duration = state.IsEmptyReload ? state.Definition.EmptyReloadSeconds : state.Definition.TacticalReloadSeconds;
                reload = Mathf.Clamp01(state.StateTimer / Mathf.Max(.01f, duration));
            }
            bodyAnimator.SetFloat(MoveX, direction.x, directionDampingSeconds, dt);
            bodyAnimator.SetFloat(MoveY, direction.y, directionDampingSeconds, dt);
            bodyAnimator.SetFloat(Speed, speed);
            // One coherent directional gait family. Cadence follows actual displacement, including
            // the 3.2 m/s normal movement and 5.5 m/s sprint; never reverse animation time.
            bodyAnimator.SetFloat(Rate, Mathf.Clamp(speed / Mathf.Max(.1f, runReferenceMetersPerSecond), .05f, 2f));
            bodyAnimator.SetBool(Crouch, Crouching); bodyAnimator.SetBool(Slide, Sliding);
            bodyAnimator.SetBool(Ground, grounded || seated); bodyAnimator.SetBool(Sprint, alive && motor.IsSprinting);
            bodyAnimator.SetFloat(Vertical, motor.VerticalVelocity);
            bodyAnimator.SetFloat(Direction, speed > .01f ? Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg : 0);
            bodyAnimator.SetBool(Dead, !alive); bodyAnimator.SetBool(Seated, seated);
            bodyAnimator.SetFloat(Aim, AimAmount); bodyAnimator.SetInteger(Weapon, state == null ? 0 : (int)state.State);
            bodyAnimator.SetFloat(ReloadTime, reload);
            bool meleeActive = alive && combat && combat.SelectedSlot == PlayerCombatController.CombatSlot.Melee &&
                combat.Melee && combat.Melee.Simulation != null && combat.Melee.Simulation.State != MeleeState.Ready;
            bodyAnimator.SetBool(Melee, meleeActive);
            bodyAnimator.SetFloat(MeleeTime, combat && combat.Melee && combat.Melee.Simulation != null ? combat.Melee.Simulation.PresentationProgress : 0);
            bodyAnimator.SetBool(Treatment, alive && Treating);
            // An empty Write Defaults Off state is not a neutral pose. Mute the action layer
            // after its action ends so a retained pickup/interaction pose cannot override arm IK.
            if (UpperActionBusy) pendingActionSeconds = 0;
            else
            {
                actionInterrupted = false;
                if (PresentationActive) pendingActionSeconds = Mathf.Max(0, pendingActionSeconds - dt);
            }
            if (bodyAnimator.layerCount > 1)
                bodyAnimator.SetLayerWeight(1, alive && !seated &&
                    ((!actionInterrupted && UpperActionBusy) || meleeActive || pendingActionSeconds > 0) ? 1 : 0);
            // Calibrated per-avatar finger closure is applied after arm solving. The old generic
            // pistol finger layer remains an asset reference but must not compete with that pose.
            if (bodyAnimator.layerCount > 2) bodyAnimator.SetLayerWeight(2, 0);
        }
    }
}
