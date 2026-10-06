using UnityEngine;

namespace FPS.Scripts.Characters.AI{
    public class EnemyAnimatorConstants{
        public static readonly int movementSpeedParameter = Animator.StringToHash("Move Speed");
        public static readonly int alertedParameter = Animator.StringToHash("Alerted");
        public static readonly int attackSpeedParameter = Animator.StringToHash("Attack Speed");

        public static readonly int attackTrigger = Animator.StringToHash("Attack");
        public static readonly int onDamagedTrigger = Animator.StringToHash("Damaged");
        public static readonly int onEnemyStunnedTrigger = Animator.StringToHash("Stunned");
        public static readonly int onDeathTrigger = Animator.StringToHash("Died");
        
        public static readonly string idleStateName = "Idle";
    }
}