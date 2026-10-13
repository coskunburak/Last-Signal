using System;
using Unity.Profiling;
using UnityEngine;

namespace LastSignal
{
    [DisallowMultipleComponent]
    public sealed class ZombieHealth : MonoBehaviour, IDamageReceiptReceiver
    {
        static readonly ProfilerMarker Marker = new ProfilerMarker("LastSignal.Zombie.Damage");
        [SerializeField, Min(1)] float maxHealth = 100;
        [SerializeField, Min(1)] float vehicleEffectiveMass = 80;
        Collider[] vehicleResponseColliders;
        void OnEnable()
        {
            vehicleResponseColliders = GetComponents<Collider>();
            Vehicles.VehicleInfectedContactResponse.Register(vehicleResponseColliders, false, vehicleEffectiveMass);
        }
        void OnDisable()
        {
            Vehicles.VehicleInfectedContactResponse.Unregister(vehicleResponseColliders, false);
            LifetimeGeneration++; Array.Clear(acceptedShots, 0, acceptedShots.Length); Array.Clear(acceptedSubHits, 0, acceptedSubHits.Length); nextAccepted = 0;
        }
        readonly ulong[] acceptedShots = new ulong[32];
        readonly int[] acceptedSubHits = new int[32];
        int nextAccepted;
        public uint LifetimeGeneration { get; private set; } = 1;
        ZombieDismemberment dismemberment;
        bool accepting = true;
        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0;
        public int DamageTransactions { get; private set; }
        public DamageInfo LastDamage { get; private set; }
        public float LastHealthBefore { get; private set; }
        public event Action<DamageInfo> Damaged;
        public event Action<DamageReceipt> DamageCommitted;
        public event Action Died;
        public event Action StateRestored;
        void Awake()
        {
            dismemberment = GetComponent<ZombieDismemberment>();
            CurrentHealth = float.IsFinite(maxHealth) && maxHealth > 0 ? maxHealth : 0;
        }
        internal void ApplyArchetypeHealth(float value)
        {
            if (DamageTransactions == 0 && CurrentHealth == maxHealth) CurrentHealth = value;
            maxHealth = value;
        }
        internal void RestoreHealth(float value)
        {
            LifetimeGeneration++; Array.Clear(acceptedShots, 0, acceptedShots.Length); Array.Clear(acceptedSubHits, 0, acceptedSubHits.Length); nextAccepted = 0;
            CurrentHealth = value; DamageTransactions = 0; LastDamage = default;
            StateRestored?.Invoke(); // Hydration is not a new kill or damage transaction.
        }
        public void SetDamageEnabled(bool value) => accepting = value;
        public void TakeDamage(DamageInfo info) => ReceiveDamage(info);
        public DamageReceipt ReceiveDamage(DamageInfo info)
        {
            // Root/navigation colliders are not anatomical damage surfaces.
            return info.HitCollider ? default : Apply(info);
        }
        internal DamageReceipt ReceiveRegion(DamageInfo info, ZombieHitRegion region)
        {
            if (!region || region.Owner != this || !region.isActiveAndEnabled) return default;
            return Apply(info);
        }
        DamageReceipt Apply(DamageInfo info)
        {
            if (!accepting || !isActiveAndEnabled || !IsAlive || !float.IsFinite(info.Amount) || info.Amount <= 0) return default;
            if (info.Category == DamageCategory.Bullet && (!ZombieWoundSurfaceResolver.Finite(info.HitPoint) ||
                !ZombieWoundSurfaceResolver.Finite(info.Direction) || !ZombieWoundSurfaceResolver.Finite(info.SourcePosition) || !ZombieWoundSurfaceResolver.Finite(info.HitNormal))) return default;
            if (info.ShotId != 0)
            {
                for (int i = 0; i < acceptedShots.Length; i++)
                    if (acceptedShots[i] == info.ShotId && acceptedSubHits[i] == info.SubHitId) return default;
                acceptedShots[nextAccepted] = info.ShotId; acceptedSubHits[nextAccepted] = info.SubHitId;
                nextAccepted = (nextAccepted + 1) % acceptedShots.Length;
            }
            using (Marker.Auto())
            {
                LastHealthBefore = CurrentHealth;
                int beforeMask = dismemberment ? dismemberment.SeveredPartsMask : 0;
                bool fatalSever = dismemberment && dismemberment.ResolveCommittedHit(info);
                CurrentHealth = fatalSever ? 0 : Mathf.Max(0, CurrentHealth - info.Amount);
                LastDamage = info; DamageTransactions++;
                var receipt = new DamageReceipt(info, this, LastHealthBefore - CurrentHealth, !IsAlive,
                    dismemberment && beforeMask != dismemberment.SeveredPartsMask);
                // Health and anatomy are committed before any cosmetic callback. Fatal VFX can
                // acquire a corpse lease before Died listeners release the population actor.
                if (dismemberment) dismemberment.PresentCommittedSever(info);
                try { DamageCommitted?.Invoke(receipt); }
                catch (Exception e) { Debug.LogException(e, this); }
                // Preserve the existing surviving-only Damaged contract.
                if (receipt.Terminal) Died?.Invoke();
                else if (IsAlive) Damaged?.Invoke(info);
                return receipt;
            }
        }
    }
}
