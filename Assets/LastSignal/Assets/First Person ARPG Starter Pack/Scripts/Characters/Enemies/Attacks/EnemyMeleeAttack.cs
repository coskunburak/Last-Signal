using System.Collections.Generic;
using Main.StatsAndManipulation;
using UnityEngine;

namespace FPS.Scripts.Characters.AI.Behaviours.Attacks{
    [RequireComponent((typeof(Collider)))]
    public class EnemyMeleeAttack : EnemyAttack{
        private Collider attackCollider;
        private float timeTillSwingRetaliates = -1f;
        private float timeSwingStarted = -1f;

        private float timeLastAttackEnded = -1f;
        
        [Header("Attack Stats")]
        public DamageInfo weaponDamage;
        public float timeBetweenAttacks = 1.0f;

        private HashSet<GameObject> gameObjectsAttackedAlready = new HashSet<GameObject>();
        
        public override bool CanAttack(){
            return IsInAttackRange() && !isAttacking;
        }
        
        public override bool ShouldFollowEnemyWhileAttacking(){
            return false;
        }

        public override void OnAttackEnded(){
            // reset attack related variables
            isAttacking = false;
            gameObjectsAttackedAlready.Clear();
            // store last attack ended variable used to calculate other elements
            timeLastAttackEnded = Time.time;
        }

        public override void OnAttackStarted(float attackAnimationLength){
            isAttacking = true;
            // calculate part of animation that is swing retaliation (assumes half of animation is not attack motion)
            timeTillSwingRetaliates = Time.time + attackAnimationLength / 2;
            // calculates when the swing i.e. attack has actually started based on the animation
            timeSwingStarted = Time.time + attackAnimationLength / 5;
            if (attackAudio && audioSource)
            {
                StartCoroutine(PlayAttackAudio());
            }
        }

        public override bool PerformAttack() {
            // Check that another attack can be performed i.e. time between attacks has passed
            if (timeBetweenAttacks < Time.time - timeLastAttackEnded)
            {
                if (CanAttack())
                {
                    attacksAnimator.SetTrigger(EnemyAnimatorConstants.attackTrigger);
                    return true;
                }
            }
            else if(!isAttacking)
            {
                owner.OrientTowards(target.transform.position);
            }
            return false;
        }

        public override void OnAttackInitialised(EnemyController owner){
            base.OnAttackInitialised(owner);
            DebugUtility.HandleErrorIfNullGetComponent<Animator, EnemyMeleeAttack>(attacksAnimator, this, gameObject);
            
            attackCollider = GetComponent<Collider>();
            DebugUtility.HandleErrorIfNullGetComponent<Collider, EnemyMeleeAttack>(attackCollider, this, gameObject);
        }

        private void OnTriggerEnter(Collider other){
            // The swing started variable determines at what point in the attack animation the attack is actually being executed, so that time needs to be exceeded.
            // The retaliation time is the point the attack animation where the attack has stopped, so that cannot be exceeded.
            if (timeSwingStarted > Time.time || timeTillSwingRetaliates < Time.time) return;
            // Make sure not to attack the same enemy more than once.
            if (gameObjectsAttackedAlready.Contains(other.gameObject)) return;
            // Make sure the player is the object hit
            if (other.gameObject.GetComponentInChildren<PlayerCharacterController>())
            {
                Damageable damageable = other.GetComponent<Damageable>();
                if (damageable)
                {
                    weaponDamage.damageSource = owner.gameObject;
                    damageable.InflictDamage(weaponDamage);
                    gameObjectsAttackedAlready.Add(other.gameObject);
                }
            }
        }
    }
}