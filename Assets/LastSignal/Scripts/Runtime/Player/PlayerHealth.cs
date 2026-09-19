using System;
using UnityEngine;

namespace LastSignal
{
    [DisallowMultipleComponent]
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] float maxHealth = 100;
        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0;
        public int DamageTransactions { get; private set; }
        public DamageInfo LastDamage { get; private set; }
        public event Action HealthChanged;
        public event Action Died;

        void Awake() => ResetForSession();
        public void ResetForSession()
        {
            if (float.IsNaN(maxHealth) || float.IsInfinity(maxHealth) || maxHealth <= 0)
            {
                CurrentHealth = 0;
                Debug.LogError("PlayerHealth requires finite positive max health.", this);
                return;
            }
            CurrentHealth = maxHealth;
            DamageTransactions = 0;
            LastDamage = default;
            HealthChanged?.Invoke();
        }
        public void TakeDamage(DamageInfo info)
        {
            if (!isActiveAndEnabled || !IsAlive || info.Amount <= 0 ||
                float.IsNaN(info.Amount) || float.IsInfinity(info.Amount)) return;
            CurrentHealth = Mathf.Max(0, CurrentHealth - info.Amount);
            LastDamage = info;
            DamageTransactions++;
            bool died = !IsAlive;
            HealthChanged?.Invoke();
            if (died) Died?.Invoke();
        }
    }
}
