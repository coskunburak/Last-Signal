using System;
using UnityEngine;

namespace LastSignal
{
    /// <summary>
    /// Authoritative weapon state machine. Lives on the weapon viewmodel.
    /// Validates and executes fire/reload/equip transactions. Animator is driven
    /// by events emitted from here — never queried for state.
    /// </summary>
    public sealed class WeaponController : MonoBehaviour
    {
        [SerializeField] WeaponDefinition definition;
        [SerializeField] Transform muzzle;
        [SerializeField] Transform aimReference;
        [SerializeField] LayerMask hitMask = ~(1 << 2); // Ignore Raycast excluded

        WeaponRuntimeState runtimeState;
        Transform cameraTransform;
        GameObject instigator;
        bool fireInputHeld;

        // ── Public state (read-only for presentation) ───────────────────
        public WeaponRuntimeState RuntimeState => runtimeState;
        public WeaponDefinition Definition => definition;
        public Transform Muzzle => muzzle;
        public Transform AimReference => aimReference;

        // ── Events for presentation layers ──────────────────────────────
        public event Action<WeaponState> StateChanged;
        public event Action<WeaponFireResolver.ShotResult> ShotFired;
        public event Action ShotRejected; // Dry fire / empty
        public event Action ReloadCommitted;
        public event Action<WeaponState, WeaponState> StateTransitioned;

        // ── Initialization ──────────────────────────────────────────────

        public void Initialize(Transform camera, GameObject shooter, int magazine = -1, int reserve = -1)
        {
            if (!definition) { Debug.LogError("WeaponController requires a WeaponDefinition.", this); enabled = false; return; }
            cameraTransform = camera;
            instigator = shooter;
            int mag = magazine >= 0 ? magazine : definition.MagazineCapacity;
            int res = reserve >= 0 ? reserve : definition.MaxReserve;
            runtimeState = new WeaponRuntimeState(definition, mag, res);
        }

        void Update()
        {
            if (runtimeState == null) return;
            runtimeState.Tick(Time.deltaTime);
            ProcessStateTimers();
            ProcessAutoFire();
        }

        // ── Input commands (called by PlayerCombatController) ───────────

        public void OnFirePressed()
        {
            if (runtimeState == null || runtimeState.State != WeaponState.Ready) return;
            fireInputHeld = true;
            runtimeState.FireRequestConsumed = false;
            TryFire();
        }

        public void OnFireReleased()
        {
            fireInputHeld = false;
            if (runtimeState != null) runtimeState.FireRequestConsumed = false;
        }

        public void OnReloadRequested()
        {
            if (runtimeState == null) return;
            var previous = runtimeState.State;
            if (runtimeState.TryBeginReload())
            {
                StateChanged?.Invoke(runtimeState.State);
                StateTransitioned?.Invoke(previous, runtimeState.State);
            }
        }

        public void RequestEquip()
        {
            if (runtimeState == null) return;
            if (runtimeState.TryEquip())
            {
                var prev = WeaponState.Holstered;
                StateChanged?.Invoke(runtimeState.State);
                StateTransitioned?.Invoke(prev, runtimeState.State);
            }
        }

        public void RequestUnequip()
        {
            if (runtimeState == null) return;
            // Cancel reload if pre-commit
            if (runtimeState.State == WeaponState.Reloading && !runtimeState.ReloadCommitted)
                runtimeState.TryCancelReload();
            if (runtimeState.TryUnequip())
            {
                StateChanged?.Invoke(runtimeState.State);
                StateTransitioned?.Invoke(WeaponState.Ready, runtimeState.State);
            }
        }

        // ── Internal ────────────────────────────────────────────────────

        void TryFire()
        {
            // Semi-auto gate: already consumed this press.
            if (definition.FireMode == FireMode.SemiAutomatic && runtimeState.FireRequestConsumed)
                return;

            if (!runtimeState.CanFire)
            {
                if (runtimeState.CurrentMagazine == 0)
                    ShotRejected?.Invoke(); // Dry fire
                return;
            }

            if (!runtimeState.TryConsumeShot()) return;

            // Resolve the shot.
            if (!cameraTransform || !muzzle)
            {
                Debug.LogWarning("WeaponController: missing camera or muzzle transform.");
                return;
            }

            var result = WeaponFireResolver.Resolve(
                cameraTransform.position, cameraTransform.forward,
                muzzle.position,
                definition.MaxRange, definition.MuzzleObstructionRange,
                definition.BaseDamage, instigator,
                hitMask);

            ShotFired?.Invoke(result);
        }

        void ProcessAutoFire()
        {
            if (runtimeState == null) return;
            if (definition.FireMode != FireMode.Automatic) return;
            if (!fireInputHeld) return;
            if (runtimeState.State != WeaponState.Ready) return;
            if (runtimeState.FireCooldownRemaining > 0) return;
            TryFire();
        }

        void ProcessStateTimers()
        {
            if (runtimeState == null) return;
            var prevState = runtimeState.State;

            switch (runtimeState.State)
            {
                case WeaponState.Equipping:
                    if (runtimeState.TryCompleteEquip())
                    {
                        StateChanged?.Invoke(runtimeState.State);
                        StateTransitioned?.Invoke(prevState, runtimeState.State);
                    }
                    break;

                case WeaponState.Unequipping:
                    if (runtimeState.TryCompleteUnequip())
                    {
                        StateChanged?.Invoke(runtimeState.State);
                        StateTransitioned?.Invoke(prevState, runtimeState.State);
                    }
                    break;

                case WeaponState.Reloading:
                    if (!runtimeState.ReloadCommitted && runtimeState.TryCommitReload())
                        ReloadCommitted?.Invoke();
                    if (runtimeState.TryCompleteReload())
                    {
                        StateChanged?.Invoke(runtimeState.State);
                        StateTransitioned?.Invoke(prevState, runtimeState.State);
                    }
                    break;
            }
        }

        void OnDestroy()
        {
            runtimeState?.ForceHolster();
        }
    }
}
