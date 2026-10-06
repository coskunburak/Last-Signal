using Main.StatsAndManipulation;
using UnityEngine;

namespace FPS.Scripts.Characters {
    public abstract class DamageBlocker : MonoBehaviour{
        protected Health characterHealth;

        public abstract bool CanBlockAttack(DamageInfo damageInfo);
        public abstract float CalculatedDamageAmountAfterBlock(DamageInfo damage);
        public abstract void OnDamageBlocked(DamageInfo damage);

        public virtual void InitDamageBlocker(){
            InitCharacterHealthComponent();
        }
        
        private void InitCharacterHealthComponent(){
            characterHealth = GetComponentInParent<Health>();
            if (!characterHealth)
            {
                characterHealth = GetComponent<Health>();
            }
            DebugUtility.HandleErrorIfNullGetComponent<Health, DamageBlocker>(characterHealth, this, gameObject);
        }

        private void Awake(){
            InitDamageBlocker();
        }
    }
}