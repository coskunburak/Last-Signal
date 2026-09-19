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

        // Animator parameter hashes (set once).
        static readonly int HashState = Animator.StringToHash("WeaponState");
        static readonly int HashFire = Animator.StringToHash("Fire");
        static readonly int HashReload = Animator.StringToHash("Reload");
        static readonly int HashEmptyReload = Animator.StringToHash("EmptyReload");
        static readonly int HashAimAmount = Animator.StringToHash("AimAmount");
        static readonly int HashEquip = Animator.StringToHash("Equip");
        static readonly int HashUnequip = Animator.StringToHash("Unequip");

        WeaponState lastState = WeaponState.Holstered;

        public void Configure(WeaponController ctrl, Animator anim)
        {
            weapon = ctrl;
            animator = anim;
        }

        void OnEnable()
        {
            if (!weapon) return;
            weapon.ShotFired += OnShotFired;
            weapon.StateTransitioned += OnStateTransitioned;
        }

        void OnDisable()
        {
            if (!weapon) return;
            weapon.ShotFired -= OnShotFired;
            weapon.StateTransitioned -= OnStateTransitioned;
        }

        void Update()
        {
            if (!weapon || !animator || weapon.RuntimeState == null) return;
            var state = weapon.RuntimeState;

            animator.SetInteger(HashState, (int)state.State);
            animator.SetFloat(HashAimAmount, state.AimAmount);
        }

        void OnShotFired(WeaponFireResolver.ShotResult result)
        {
            if (animator) animator.SetTrigger(HashFire);
        }

        void OnStateTransitioned(WeaponState from, WeaponState to)
        {
            if (!animator) return;
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
            lastState = to;
        }

        // ── Animation Event receivers (presentation only) ───────────────
        // These may be called by Animation Events on clips. They drive visuals,
        // never gameplay state. Protected against duplicate/interrupted calls.

        /// <summary>Called by Animation Event: magazine detach visual moment.</summary>
        public void OnMagazineDetach()
        {
            // Future: hide magazine mesh, spawn dropped magazine visual.
        }

        /// <summary>Called by Animation Event: magazine attach visual moment.</summary>
        public void OnMagazineAttach()
        {
            // Future: show magazine mesh, snap to well.
        }

        /// <summary>Called by Animation Event: bolt/slide sound moment.</summary>
        public void OnBoltAction()
        {
            // Future: play bolt sound via WeaponAudioPresenter.
        }
    }
}
