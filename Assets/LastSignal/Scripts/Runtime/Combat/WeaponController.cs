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
        bool dryFireReported;
        LastSignal.Inventory.PlayerInventory inventory;
        PlayerInputReader input;
        PlayerHealth health;
        public event Action AmmoChanged;
        bool GameplayAllowed => isActiveAndEnabled && instigator && instigator.activeInHierarchy &&
            Time.timeScale > 0 && (!input || input.GameplayActive) && (!health || health.IsAlive);

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

        public void Initialize(Transform camera, GameObject shooter, int magazine = -1)
        {
            if (!definition) { Debug.LogError("WeaponController requires a WeaponDefinition.", this); enabled = false; return; }
            if (!definition.HasValidAmmunitionConfiguration)
            { Debug.LogError("WeaponController requires valid ammunition configuration.", this); enabled = false; return; }
            UnsubscribeAmmo();
            runtimeState?.ForceHolster();
            cameraTransform = camera;
            instigator = shooter;
            inventory = shooter ? shooter.GetComponent<LastSignal.Inventory.PlayerInventory>() : null;
            input = shooter ? shooter.GetComponent<PlayerInputReader>() : null;
            health = shooter ? shooter.GetComponent<PlayerHealth>() : null;
            if (!inventory)
            { Debug.LogError("WeaponController requires the shooter's PlayerInventory.", this); enabled = false; return; }
            int mag = magazine >= 0 ? magazine : definition.StartingMagazine;
            runtimeState = new WeaponRuntimeState(definition, mag, inventory);
            fireInputHeld = dryFireReported = false;
            SubscribeAmmo();
            NotifyAmmoChanged();
        }

        void Update()
        {
            if (runtimeState == null || !GameplayAllowed) return;
            runtimeState.Tick(Time.deltaTime);
            ProcessStateTimers();
            ProcessAutoFire();
        }

        // ── Input commands (called by PlayerCombatController) ───────────

        public void OnFirePressed()
        {
            if (runtimeState == null || !GameplayAllowed || runtimeState.State != WeaponState.Ready) return;
            if (fireInputHeld) return;
            fireInputHeld = true;
            dryFireReported = false;
            runtimeState.FireRequestConsumed = false;
            TryFire();
        }

        public void OnFireReleased()
        {
            fireInputHeld = false;
            dryFireReported = false;
            if (runtimeState != null) runtimeState.FireRequestConsumed = false;
        }

        public void OnReloadRequested()
        {
            if (runtimeState == null || !GameplayAllowed) return;
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
            if (!GameplayAllowed || !cameraTransform || !muzzle) return;
            // Semi-auto gate: already consumed this press.
            if (definition.FireMode == FireMode.SemiAutomatic && runtimeState.FireRequestConsumed)
                return;

            if (!runtimeState.CanFire)
            {
                if (runtimeState.CurrentMagazine == 0 && !dryFireReported)
                {
                    dryFireReported = true;
                    ShotRejected?.Invoke(); // At most once until trigger release.
                }
                return;
            }

            if (!runtimeState.TryConsumeShot()) return;

            // Resolve accepted shots once; misses and wall impacts still cost a round.
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

        void NotifyAmmoChanged() => AmmoChanged?.Invoke();
        void SubscribeAmmo()
        {
            UnsubscribeAmmo();
            if (inventory) inventory.InventoryChanged += NotifyAmmoChanged;
            if (runtimeState != null) runtimeState.MagazineChanged += NotifyAmmoChanged;
        }
        void UnsubscribeAmmo()
        {
            if (inventory) inventory.InventoryChanged -= NotifyAmmoChanged;
            if (runtimeState != null) runtimeState.MagazineChanged -= NotifyAmmoChanged;
        }
        void OnEnable() => SubscribeAmmo();
        void OnDisable()
        {
            OnFireReleased();
            runtimeState?.ForceHolster();
            UnsubscribeAmmo();
            NotifyAmmoChanged();
        }
        void OnDestroy()
        {
            UnsubscribeAmmo();
            runtimeState?.ForceHolster();
        }
    }
}
