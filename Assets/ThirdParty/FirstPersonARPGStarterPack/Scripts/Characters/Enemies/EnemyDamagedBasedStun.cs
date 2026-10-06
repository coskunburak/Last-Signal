using System;
using Main.StatsAndManipulation;
using UnityEngine;

namespace FPS.Scripts.Characters.AI
{
    [RequireComponent(typeof(Health))]
    public class EnemyDamagedBasedStun : MonoBehaviour
    {
        public Animator animator;

        public float damageThreshold;
        
        public bool isStunned { private set; get; }
        
        private void Awake()
        {
            Health enemyHealth = GetComponent<Health>();
            enemyHealth.onDamaged += OnDamaged;
        }

        public void OnDamaged(DamageInfo damageInfo)
        {
            if (damageInfo.damageAmount > damageThreshold)
            {
                isStunned = true;
                animator.SetTrigger(EnemyAnimatorConstants.onEnemyStunnedTrigger);
            }
        }

        public void OnRecoveredFromStun()
        {
            isStunned = false;
        }
    }
}