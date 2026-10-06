using FPS.Scripts.Characters;
using Main.StatsAndManipulation;
using UnityEngine;

namespace FPS.Scripts.Items.Weapons.WeaponTypes.Melee{
    [RequireComponent(typeof(MeleeWeapon))]
    [RequireComponent(typeof(WeaponAnimatorDirectionUpdate))]
    [RequireComponent(typeof(AudioSource))]
    public class Parry : WeaponSecondaryBehaviour {
        private MeleeWeapon meleeController;
        private PlayerCharacterController playerCharacterController;
        private Stamina playerStamina;

        public float staminaForBlock = 20f;
        
        public Animator parryAnimator;
        public AudioClip parrySfx;

        private Health playerHealth;
        private DamageBlocker damageBlocker;
        private AudioSource weaponAudioSource;
    
        public bool isParrying{ get; set; }
        
        // calculates how much the camera needs to face the attacking enemy in order for parrying to be possible
        private float kDirectionTowardsDamageSourceSensitivity = 0.7f; 
        public override void HandleInputs(bool inputDown, bool inputHeld, bool inputReleased){
            if (inputDown)
            {
                // if player is charging, interrupt their charge
                if (meleeController.weaponSwingState == WeaponSwingState.CHARGING)
                {
                    meleeController.InterruptCharge();
                }
                else
                {
                    // If player is not charging, initiate start parry if possible
                    if (AnimatorInIdleState()) 
                    {
                        if (playerStamina && playerStamina.currentStamina > staminaForBlock)
                        {
                            parryAnimator.SetTrigger(MeleeAnimatorConstants.parryTrigger);
                        }
                        else
                        {
                            playerStamina.onMissingStamina.Invoke();
                        }
                    }
                }
                return;
            } 
            // stop parrying on input released
            if (inputReleased) 
            {
                if (isParrying)
                {
                    parryAnimator.SetTrigger(MeleeAnimatorConstants.stoppedParryTrigger);
                }
                return;
            }
            if(inputHeld)
            {
                // Check if player is sustaining the parry and can even continue to do so
                if (playerStamina && playerStamina.currentStamina < staminaForBlock)
                {
                    parryAnimator.SetTrigger(MeleeAnimatorConstants.stoppedParryTrigger);
                }
                else
                {
                    if (!isParrying)
                    {
                        parryAnimator.SetTrigger(MeleeAnimatorConstants.parryTrigger);
                    }
                }
            }
        }

        protected override void InitialiseSecondaryBehaviour(){
            meleeController = GetComponent<MeleeWeapon>();
            weaponAudioSource = GetComponent<AudioSource>();
        }

        public override void AssignOwner(GameObject owner) {
            base.AssignOwner(owner);
            playerStamina = owner.GetComponent<Stamina>();
            DebugUtility.HandleErrorIfNullGetComponent<Stamina, ParryDamageBlocker>(playerStamina, this, gameObject);
            
            playerCharacterController = owner.GetComponent<PlayerCharacterController>();

            gameObject.AddComponent<ParryDamageBlocker>();
        }

        private bool AnimatorInIdleState(){
            return parryAnimator.GetCurrentAnimatorStateInfo(0).IsName(MeleeAnimatorConstants.idleState) &&
                   !parryAnimator.IsInTransition(0);
        }

        public bool CanBlockAttack(DamageInfo damageInfo){
            if (!damageInfo.isBlockable) return false;
            float facingAttackSourceDotProduct = Vector3.Dot(transform.forward, damageInfo.damageSource.transform.forward);
            // negative dot product means the damage source and player are facing opposite directions, so block is not possible
            if (facingAttackSourceDotProduct < 0f 
                && Mathf.Abs(facingAttackSourceDotProduct) > kDirectionTowardsDamageSourceSensitivity)
            {
                if (playerStamina)
                {
                    // calculate if the parry is possible
                    return playerStamina.currentStamina > staminaForBlock && isParrying;
                }
                return isParrying;
            }
            return false;
        }

        public float CalculateDamageAfterBlock(float damage) {
            if (playerStamina)
            {
                // basic calculation of whether player can block the damage
                return playerStamina.currentStamina < staminaForBlock ? damage : 0;
            }
            return 0;
        }

        public void OnDamageBlocked(DamageInfo damage){
            if (playerStamina)
            {
                playerStamina.TryDrainStamina(staminaForBlock);
                // players that can no longer parry should stop
                if (playerStamina.currentStamina < staminaForBlock)
                {
                    parryAnimator.SetTrigger(MeleeAnimatorConstants.stoppedParryTrigger);
                    playerStamina.onMissingStamina.Invoke();
                }   
            }
            if (parrySfx)
            {
                weaponAudioSource.PlayOneShot(parrySfx);
            }
        }
    }
}