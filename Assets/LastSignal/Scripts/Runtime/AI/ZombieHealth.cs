using System;
using Unity.Profiling;
using UnityEngine;

namespace LastSignal
{
    [DisallowMultipleComponent]
    public sealed class ZombieHealth : MonoBehaviour, IDamageable
    {
        static readonly ProfilerMarker Marker = new ProfilerMarker("LastSignal.Zombie.Damage");
        [SerializeField, Min(1)] float maxHealth = 100;
        bool accepting = true;
        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0;
        public int DamageTransactions { get; private set; }
        public DamageInfo LastDamage { get; private set; }
        public float LastHealthBefore { get; private set; }
        public event Action<DamageInfo> Damaged;
        public event Action Died;
        void Awake()
        {
            CurrentHealth = float.IsFinite(maxHealth) && maxHealth > 0 ? maxHealth : 0;
        }
        internal void RestoreHealth(float value)
        {
            CurrentHealth = value; DamageTransactions = 0; LastDamage = default;
            if (!IsAlive) Died?.Invoke();
        }
        public void SetDamageEnabled(bool value) => accepting = value;
        public void TakeDamage(DamageInfo info)
        {
            // Collider hits belong to explicit regions, never arbitrary children of this health owner.
            if (info.HitCollider) return;
            Apply(info);
        }
        internal void ReceiveRegion(DamageInfo info, ZombieHitRegion region)
        {
            if (!region || region.Owner != this || !region.isActiveAndEnabled) return;
            Apply(info);
        }
        void Apply(DamageInfo info)
        {
            if (!accepting || !isActiveAndEnabled || !IsAlive || !float.IsFinite(info.Amount) || info.Amount <= 0) return;
            using (Marker.Auto())
            {
                LastHealthBefore = CurrentHealth;
                CurrentHealth = Mathf.Max(0, CurrentHealth - info.Amount);
                LastDamage = info; DamageTransactions++;
                // Commit terminal authority before external surviving-hit listeners can run.
                if (!IsAlive) Died?.Invoke();
                else Damaged?.Invoke(info);
            }
        }
    }
}
