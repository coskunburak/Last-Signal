using FPS.Scripts.Characters;
using Main.StatsAndManipulation;
using UnityEngine;

namespace Main.Characters
{
    public abstract class CharacterKnockback : MonoBehaviour
    {
        [Tooltip("Multiplier to apply the hit knockback when receiving damage")]
        public float knockbackMultiplier = 1f;

        protected float currentKnockback;
        protected float timeLastKnockbackApplied = -Mathf.Infinity;
        protected Vector3 currentKnockbackDirection;
        
        protected const float KnockbackLength = 0.2f;

        private void Awake()
        {
            Health health = GetComponent<Health>();
            health.onDamaged += ApplyKnockback;

            InitialiseComponent();
        }

        private void Update()
        {
            HandleKnockback();
        }

        protected abstract void InitialiseComponent();
        protected abstract void HandleKnockback();

        public virtual void ApplyKnockback(DamageInfo damageInfo)
        {
            currentKnockback = damageInfo.targetKnockbackOnHitForce * knockbackMultiplier;
            currentKnockbackDirection = damageInfo.damageSource.transform.forward;
            timeLastKnockbackApplied = Time.time;
        }

        protected bool ShouldApplyKnockback()
        {
            return timeLastKnockbackApplied + KnockbackLength > Time.time;
        }
    }
}