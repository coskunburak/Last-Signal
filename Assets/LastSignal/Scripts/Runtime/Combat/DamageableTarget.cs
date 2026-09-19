using UnityEngine;

namespace LastSignal
{
    /// <summary>
    /// Controlled combat test target. Implements IDamageable for testing damage delivery,
    /// hit detection, and multi-collider deduplication.
    /// </summary>
    public sealed class DamageableTarget : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] float maxHealth = 100f;
        float currentHealth;
        int hitCount;
        float totalDamageReceived;

        public bool IsAlive => currentHealth > 0;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public int HitCount => hitCount;
        public float TotalDamageReceived => totalDamageReceived;

        void Awake() => currentHealth = maxHealth;

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive) return;
            float clamped = Mathf.Max(0, info.Amount);
            currentHealth = Mathf.Max(0, currentHealth - clamped);
            totalDamageReceived += clamped;
            hitCount++;
        }

        public void ResetTarget()
        {
            currentHealth = maxHealth;
            hitCount = 0;
            totalDamageReceived = 0;
        }
    }
}
