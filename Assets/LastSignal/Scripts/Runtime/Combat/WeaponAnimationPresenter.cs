using UnityEngine;

namespace LastSignal
{
    /// <summary>
    /// Drives Animator parameters from authoritative WeaponRuntimeState.
    /// Never queries Animator for gameplay decisions. Handles Animation Events
    /// with duplicate/interrupt protection.
    /// </summary>
    public sealed class WeaponAnimationPresenter : MonoBehaviour
    {
        [SerializeField] WeaponController weapon;
        [SerializeField] Animator animator;
        FirstPersonMotor motor;

        // Animator parameter hashes (set once).
        static readonly int HashState = Animator.StringToHash("WeaponState");
        static readonly int HashFire = Animator.StringToHash("Fire");
        static readonly int HashReload = Animator.StringToHash("Reload");
        static readonly int HashEmptyReload = Animator.StringToHash("EmptyReload");
        static readonly int HashAimAmount = Animator.StringToHash("AimAmount");
        static readonly int HashEquip = Animator.StringToHash("Equip");
        static readonly int HashUnequip = Animator.StringToHash("Unequip");
        static readonly int HashSprinting = Animator.StringToHash("Sprinting");

        WeaponController subscribedWeapon;

        void Awake() => motor = GetComponentInParent<FirstPersonMotor>();
        void OnTransformParentChanged() => motor = GetComponentInParent<FirstPersonMotor>();

        public void Configure(WeaponController ctrl, Animator anim)
        {
            Unsubscribe();
            weapon = ctrl;
            animator = anim;
            motor = GetComponentInParent<FirstPersonMotor>();
            if (isActiveAndEnabled) Subscribe();
        }

        void OnEnable()
        {
            ResetPendingTriggers();
            Subscribe();
        }

        void Subscribe()
        {
            Unsubscribe();
            if (!weapon) return;
            subscribedWeapon = weapon;
            subscribedWeapon.ShotFired += OnShotFired;
            subscribedWeapon.StateTransitioned += OnStateTransitioned;
        }

        void OnDisable()
        {
            if (animator) animator.SetBool(HashSprinting, false);
            ResetPendingTriggers();
            Unsubscribe();
        }

        void Unsubscribe()
        {
            if (subscribedWeapon)
            {
                subscribedWeapon.ShotFired -= OnShotFired;
                subscribedWeapon.StateTransitioned -= OnStateTransitioned;
            }
            subscribedWeapon = null;
        }

        void ResetPendingTriggers()
        {
            if (!animator || !animator.runtimeAnimatorController) return;
            animator.ResetTrigger(HashFire);
            animator.ResetTrigger(HashReload);
            animator.ResetTrigger(HashEmptyReload);
            animator.ResetTrigger(HashEquip);
            animator.ResetTrigger(HashUnequip);
        }

        void Update()
        {
            if (!animator) return;
            // Inventory/modal input locks do not necessarily set Time.timeScale to zero.
            // Letting the clip advance while StateTimer is frozen shows an inserted
            // magazine before the inventory transaction can commit.
            animator.speed = weapon && weapon.IsSimulationActive ? 1f : 0f;
            if (!weapon || weapon.RuntimeState == null) return;
            var state = weapon.RuntimeState;

            animator.SetInteger(HashState, (int)state.State);
            animator.SetFloat(HashAimAmount, state.AimAmount);
            animator.SetBool(HashSprinting, motor && motor.IsSprinting && state.State == WeaponState.Ready);
        }

        void OnShotFired(WeaponFireResolver.ShotResult result)
        {
            if (animator) animator.SetTrigger(HashFire);
        }

        void OnStateTransitioned(WeaponState from, WeaponState to)
        {
            if (!animator) return;
            ResetPendingTriggers();
            switch (to)
            {
                case WeaponState.Equipping:
                    animator.SetTrigger(HashEquip);
                    break;
                case WeaponState.Unequipping:
                    animator.SetTrigger(HashUnequip);
                    break;
                case WeaponState.Reloading:
                    var state = weapon.RuntimeState;
                    if (state != null && state.IsEmptyReload)
                        animator.SetTrigger(HashEmptyReload);
                    else
                        animator.SetTrigger(HashReload);
                    break;
            }
        }

        // ── Animation Event receivers (presentation only) ───────────────
        // These may be called by Animation Events on clips. They drive visuals,
        // never gameplay state. Protected against duplicate/interrupted calls.

        /// <summary>Called by Animation Event: magazine detach visual moment.</summary>
        public void OnMagazineDetach()
        {
            GetComponent<WeaponAudioPresenter>()?.OnMagazineDetach();
        }

        /// <summary>Called by Animation Event: magazine attach visual moment.</summary>
        public void OnMagazineAttach()
        {
            GetComponent<WeaponAudioPresenter>()?.OnMagazineAttach();
        }

        /// <summary>Called by Animation Event: bolt/slide sound moment.</summary>
        public void OnBoltAction()
        {
            GetComponent<WeaponAudioPresenter>()?.OnBoltAction();
        }
    }
}
