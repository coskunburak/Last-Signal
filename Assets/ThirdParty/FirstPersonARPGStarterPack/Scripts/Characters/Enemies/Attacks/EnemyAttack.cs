using System.Collections;
using UnityEngine;

namespace FPS.Scripts.Characters.AI.Behaviours.Attacks{
    public abstract class EnemyAttack : MonoBehaviour{
        protected EnemyController owner;
        protected GameObject target;
        
        [Header("Attack Audio")]
        public AudioSource audioSource;
        public AudioClip attackAudio;
        public float attackAudioDelay;
        
        protected Animator attacksAnimator;
        
        public bool isAttacking { get; protected set; }

        public float attackRange;

        public abstract bool CanAttack();
        public virtual bool CanSwapAttack(){
            return !isAttacking; }
        
        public abstract bool PerformAttack();
        public virtual void OnSwappedToAttack() {}

        public virtual void OnAttackInitialised(EnemyController owner){
            this.owner = owner;
            attacksAnimator = owner.animator;
        }

        public virtual void OnEnemySwitchedFromAttackState(){
            
        }

        protected IEnumerator PlayAttackAudio(){
            yield return new WaitForSeconds(attackAudioDelay);
            audioSource.PlayOneShot(attackAudio);
        }

        public void SetTarget(GameObject target){
            this.target = target;
        }

        public virtual bool ShouldFollowEnemyWhileAttacking(){
            return !IsInAttackRange();
        }

        public abstract void OnAttackEnded();
        public abstract void OnAttackStarted(float attackAnimationLength);
        
        protected bool IsInAttackRange(){
            if (target == null) return false;
            return Vector3.Distance(owner.transform.position, target.transform.position) <= attackRange;
        }
    }
}