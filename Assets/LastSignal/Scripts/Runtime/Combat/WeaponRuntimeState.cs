using System;
using UnityEngine;
using LastSignal.Inventory;

namespace LastSignal
{
    /// <summary>
    /// Authoritative high-level weapon states. Animator is driven BY this, never the reverse.
    /// </summary>
    public enum WeaponState { Holstered, Equipping, Ready, Reloading, Unequipping }

    /// <summary>
    /// Mutable runtime state for a single weapon instance. Plain C# — no MonoBehaviour.
    /// Testable in EditMode without GameObjects.
    /// </summary>
    public sealed class WeaponRuntimeState
    {
        readonly WeaponDefinition definition;
        readonly PlayerInventory inventory;
        public event Action MagazineChanged;

        public WeaponState State { get; private set; } = WeaponState.Holstered;
        public int CurrentMagazine { get; private set; }
        public int ReserveAmmo => inventory ? inventory.GetTotalQuantity(definition.Ammunition) : 0;
        public float FireCooldownRemaining { get; private set; }
        public float StateTimer { get; private set; }
        public float AimAmount { get; set; } // 0 = hip, 1 = ADS. Interpolated externally.
        public bool IsEmptyReload { get; private set; }
        public bool ReloadCommitted { get; private set; }
        public bool FireRequestConsumed { get; set; } // Semi-auto gate: set true on fire, cleared on release.

        // Track total ammo for invariant enforcement.
        public int TotalAmmo => CurrentMagazine + ReserveAmmo;


        public WeaponRuntimeState(WeaponDefinition def, int magazine, PlayerInventory reserveInventory)
        {
            definition = def ?? throw new ArgumentNullException(nameof(def));
            CurrentMagazine = Mathf.Clamp(magazine, 0, def.MagazineCapacity);
            inventory = reserveInventory;
        }

        public WeaponDefinition Definition => definition;

        /// <summary>Advance cooldown and state timers. Call every frame.</summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0) return;
            FireCooldownRemaining = Mathf.Max(0, FireCooldownRemaining - deltaTime);
            if (State == WeaponState.Equipping || State == WeaponState.Unequipping || State == WeaponState.Reloading)
                StateTimer += deltaTime;
        }

        // ── Equip / Unequip ─────────────────────────────────────────────

        public bool TryEquip()
        {
            if (State != WeaponState.Holstered) return false;
            TransitionTo(WeaponState.Equipping);
            return true;
        }

        public bool TryCompleteEquip()
        {
            if (State != WeaponState.Equipping) return false;
            if (StateTimer < definition.EquipSeconds) return false;
            TransitionTo(WeaponState.Ready);
            return true;
        }

        public bool TryUnequip()
        {
            if (State != WeaponState.Ready) return false;
            TransitionTo(WeaponState.Unequipping);
            return true;
        }

        public bool TryCompleteUnequip()
        {
            if (State != WeaponState.Unequipping) return false;
            if (StateTimer < definition.UnequipSeconds) return false;
            TransitionTo(WeaponState.Holstered);
            return true;
        }

        // ── Fire ─────────────────────────────────────────────────────────

        public bool CanFire => State == WeaponState.Ready &&
                               FireCooldownRemaining <= 0 &&
                               CurrentMagazine > 0;

        /// <summary>
        /// Consume one round. Returns true if the shot is valid.
        /// Caller is responsible for hit resolution and presentation.
        /// </summary>
        public bool TryConsumeShot()
        {
            if (!CanFire) return false;
            CurrentMagazine--;
            FireCooldownRemaining = definition.FireCooldownSeconds;
            FireRequestConsumed = true;
            MagazineChanged?.Invoke();
            return true;
        }

        // ── Reload ───────────────────────────────────────────────────────

        public bool CanReload => State == WeaponState.Ready &&
                                 CurrentMagazine < definition.MagazineCapacity &&
                                 ReserveAmmo > 0;

        public bool TryBeginReload()
        {
            if (!CanReload) return false;
            IsEmptyReload = CurrentMagazine == 0;
            ReloadCommitted = false;
            TransitionTo(WeaponState.Reloading);
            return true;
        }

        /// <summary>
        /// Commit the reload transaction. Ammo transfers exactly once.
        /// </summary>
        public bool TryCommitReload()
        {
            if (State != WeaponState.Reloading || ReloadCommitted) return false;
            float duration = IsEmptyReload ? definition.EmptyReloadSeconds : definition.TacticalReloadSeconds;
            float commit = IsEmptyReload ? definition.EmptyReloadCommitNormalized : definition.ReloadCommitNormalized;
            if (StateTimer < duration * commit) return false;
            int need = definition.MagazineCapacity - CurrentMagazine;
            int transfer = Mathf.Min(need, ReserveAmmo);
            // Publish only after both owners agree. Inventory removal validates before mutation,
            // then synchronously notifies observers; stage the magazine and guard first so even
            // reentrant observers see conserved totals and cannot commit twice.
            ReloadCommitted = true;
            if (transfer <= 0) return true;
            CurrentMagazine += transfer;
            if (!inventory.TryRemove(definition.Ammunition, transfer))
            {
                CurrentMagazine -= transfer;
                return false;
            }
            MagazineChanged?.Invoke();
            return true;
        }

        public bool TryCompleteReload()
        {
            if (State != WeaponState.Reloading) return false;
            float duration = IsEmptyReload ? definition.EmptyReloadSeconds : definition.TacticalReloadSeconds;
            if (StateTimer < duration) return false;
            // Auto-commit if not yet committed (safety net — should not normally happen).
            if (!ReloadCommitted) TryCommitReload();
            if (State != WeaponState.Reloading) return false; // A commit observer may cancel/holster.
            TransitionTo(WeaponState.Ready);
            return true;
        }

        /// <summary>
        /// Cancel a reload in progress. If not yet committed, ammo state is unchanged.
        /// If already committed, ammo transfer stands.
        /// </summary>
        public bool TryCancelReload()
        {
            if (State != WeaponState.Reloading) return false;
            TransitionTo(WeaponState.Ready);
            return true;
        }

        // ── Force transitions (death, scene unload) ─────────────────────

        public void ForceHolster()
        {
            TransitionTo(WeaponState.Holstered);
        }

        // ── Internal ─────────────────────────────────────────────────────

        void TransitionTo(WeaponState next)
        {
            State = next;
            StateTimer = 0;
        }

    }
}
