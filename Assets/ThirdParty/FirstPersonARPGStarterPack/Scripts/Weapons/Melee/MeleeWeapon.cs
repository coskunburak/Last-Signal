using System;
using System.Collections.Generic;
using FPS.Scripts.Characters;
using FPS.Scripts.FX;
using FPS.Scripts.UI;
using Main.Items.Weapons.Melee;
using Main.StatsAndManipulation;
using UnityEngine;
using UnityEngine.Serialization;

namespace FPS.Scripts.Items.Weapons.WeaponTypes.Melee {
    public enum WeaponSwingState{
        IDLE,
        SWINGING,
        PREPARING_TO_CHARGE,
        CHARGING,
    }

    [RequireComponent(typeof(WeaponAnimatorDirectionUpdate))]
    public class MeleeWeapon : WeaponController
    {
        [Header("Base Stats")] [Tooltip("Stamina needed to wield weapon")]
        public float staminaRequirementForAttack = 12f;
        [Tooltip("Controls rate of the attack animation speed and thus at which attacks are executed. For example, a rate of 0.5 means the animation is played at half the original speed.")]
        [Range(0.0f, 10f)]
        public float baseAttackSpeedModifier = 1f;
        [Tooltip("Base animation speed at which charging is executed")]
        [Range(0.0f, 10f)]
        public float baseChargeSpeedModifier = 1f;
        [Tooltip("Base damage dealt when an enemy is hit")]
        public DamageInfo baseDamage;
        [Header("Charge Stats")]
        [Tooltip("Extra damage dealt when weapon is fully charged")]
        public float maxChargeExtraDamageAmount = 12f;
        [Tooltip("Extra damage dealt when player lands a perfect swing (overrides charge damage)")]
        public float perfectSwingExtraDamage = 30f;
        [Tooltip("Percentage position of where the perfect swing bar starts")]
        [Range(0.0f, 1.0f)]
        public float perfectSwingChargeOffset = 0.4f;
        [Tooltip("Percentage size of the perfect swing")]
        [Range(0.0f, 0.7f)]
        public float perfectSwingChargeSize = 0.4f;
        [Header("Quick Swing Stat Modifiers")]
        [Tooltip("Added to the original stamina required for a quick swing")]
        public float quickSwingStaminaRequirementMod = -4f;
        [Range(1.0f, 5.0f)]
        [Tooltip("Added to the animation/attack speed when executing a quick swing")]
        public float quickSwingAttackSpeedMod = 1f;
        [Tooltip("Added to the base damage of the swing dealt for quick strikes")]
        public float quickSwingDamageMod = -10f;
        [Tooltip("Added to the knockback of the swing for quick strikes")]
        public float quickSwingKnockbackMod = -6f;
        [Header("Weapon Effects")]
        [Tooltip("FX that are instiated when player hits something that is not an enemy")]
        public GameObject defaultHitFX;
        [Tooltip("SFX that are played when player hits default objects (like walls etc.)")]
        public AudioClip defaultHitSFX;
        [Tooltip("SFX that are played when player starts swinging at the enemy")]
        public AudioClip swingSFXClip;
        [Header("Swing Stats")]
        public SwingStats rightSwingStats;
        public SwingStats leftSwingStats;
        public SwingStats upSwingStats;
        public SwingStats backSwingStats;
        private SwingStats currentSwingStats;

        private PlayerInputHandler.ActionDirection currentSwingActionDirection;
        
        private Stamina playerStamina;        
        private MeleeAttackBar meleeAttackBar;

        private float lastChargingStartTime = -1f;
        private float lastSwingStartTime = -1f;

        private bool chargeInterrupted = false;

        private bool executingQuickSwing;
        private const float kChargeTimeThatIsQuickSwing = 0.2f;

        private Transform cameraTransform;

        public WeaponSwingState weaponSwingState { private set; get; } = WeaponSwingState.IDLE;
        
        HashSet<Collider> enemyCollidersHitThisSwing = new HashSet<Collider>();

        private float currentAttackDamage;

        public override void InitialiseWeapon()
        {
            DebugUtility.HandleErrorIfNullGetComponent<Animator, MeleeWeapon>(weaponAnimator, this, gameObject);
            
            meleeAttackBar = FindObjectOfType<MeleeAttackBar>();
            DebugUtility.HandleErrorIfNullGetComponent<MeleeAttackBar, MeleeWeapon>(meleeAttackBar, this, gameObject);
        }
        
        public override void AssignOwner(GameObject owner){
            base.AssignOwner(owner);
            
            // these can only be assigned when the owner is assigend as well, otherwise null
            playerStamina = owner.GetComponent<Stamina>();
            DebugUtility.HandleErrorIfNullGetComponent<Stamina, MeleeWeapon>(playerStamina, this, gameObject);

            cameraTransform = owner.GetComponentInChildren<Camera>().transform;
        }
        
        public override void UpdateWeapon(){
            if (weaponSwingState == WeaponSwingState.SWINGING)
            {
                Vector3 swingDirection;
                switch (currentSwingActionDirection)
                {
                    case PlayerInputHandler.ActionDirection.UP:
                        swingDirection = cameraTransform.up;
                        break;
                    case PlayerInputHandler.ActionDirection.DOWN:
                        swingDirection = -cameraTransform.up;
                        break;
                    case PlayerInputHandler.ActionDirection.LEFT:
                        swingDirection = -cameraTransform.right;
                        break;
                    case PlayerInputHandler.ActionDirection.RIGHT:
                        swingDirection = cameraTransform.right;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                float swingCompletionPercent =
                    Mathf.Clamp(GetCurrentAnimationCompletionPercent(lastSwingStartTime), 0f, 1.0f);
                // Subtract 0.5 to have negative values for swings so the attacks go from the far left to far right in relation to the camera
                float motionValue = (currentSwingStats.motionOverTime.Evaluate(swingCompletionPercent) - 0.5f) 
                                    * currentSwingStats.motionRangeMultiplier;
                Vector3 rayOrigin = Camera.main.transform.position - (swingDirection * motionValue );
                float swingHitDistance = currentSwingStats.hitDistanceOverTime.Evaluate(swingCompletionPercent) 
                                         * currentSwingStats.hitDistanceRangeMultiplier;
                RaycastHit hit;
                Debug.DrawRay(rayOrigin, Camera.main.transform.forward * swingHitDistance, Color.yellow, 30f);
                if (Physics.Raycast(rayOrigin, Camera.main.transform.forward, out hit, swingHitDistance))
                {
                    OnHitObject(hit);
                }
            }
        }

        public override void OnAttackAnimStarted(){
            // store time that last attack swing was started
            lastSwingStartTime = Time.time;
            // handle attack FX
            if (swingSFXClip)
            {
                mWeaponAudioSource.PlayOneShot(swingSFXClip);
            }
            // calculate and adjust attack animation depending on the type of swing being executed
            if (executingQuickSwing)
            {
                weaponAnimator.SetFloat(MeleeAnimatorConstants.swingTimeMultiplier, 
                    baseAttackSpeedModifier + currentSwingStats.attackSpeedMod + quickSwingAttackSpeedMod);
            }
            else
            {
                weaponAnimator.SetFloat(MeleeAnimatorConstants.swingTimeMultiplier, 
                    baseAttackSpeedModifier + currentSwingStats.attackSpeedMod);
            }
        }

        public override void OnAttackAnimEnded()
        {
            OnSwingEnded();
        }

        private void OnSwingEnded()
        {
            weaponSwingState = WeaponSwingState.IDLE;
            executingQuickSwing = false;
        }

        public void OnChargeStarted()
        {
            weaponSwingState = WeaponSwingState.CHARGING;
            lastChargingStartTime = Time.time; // actual charging animation only starts now!
            int animDirectionAsInt = Mathf.FloorToInt(weaponAnimator.GetFloat(MeleeAnimatorConstants.actionDirection));
            currentSwingActionDirection = (PlayerInputHandler.ActionDirection) animDirectionAsInt;
            switch (currentSwingActionDirection)
            {
                case PlayerInputHandler.ActionDirection.UP:
                    currentSwingStats = upSwingStats;
                    break;
                case PlayerInputHandler.ActionDirection.DOWN:
                    currentSwingStats = backSwingStats;
                    break;
                case PlayerInputHandler.ActionDirection.LEFT:
                    currentSwingStats = leftSwingStats;
                    break;
                case PlayerInputHandler.ActionDirection.RIGHT:
                    currentSwingStats = rightSwingStats;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (currentSwingStats == null)
            {
                Debug.LogWarning("no swing stat for this weapon detected");
                currentSwingStats = ScriptableObject.CreateInstance<SwingStats>();
            }
            // calculate charging time
            weaponAnimator.SetFloat(MeleeAnimatorConstants.chargingTimeMultiplier, 
                baseChargeSpeedModifier + currentSwingStats.chargeSpeedMod);
            // automatically perform a quick swing if player wouldn't otherwise have the stamina to perform a charged one
            if (playerStamina && playerStamina.currentStamina < staminaRequirementForAttack)
            {
                InitiateSwing();
                executingQuickSwing = true;
            }
        }

        public void OnChargedEnded(){
            // check that player has stamina and the weapon attack was not interrupted
            if (playerStamina && !chargeInterrupted)
            {
                // check for the type of swing being used
                if (executingQuickSwing)
                {
                    playerStamina.TryDrainStamina(staminaRequirementForAttack + quickSwingStaminaRequirementMod);
                }
                else
                {
                    playerStamina.TryDrainStamina(staminaRequirementForAttack);
                }
            }
            // reset stats for the state transition to actually executing the attack
            enemyCollidersHitThisSwing.Clear();
            onAttack?.Invoke();
            weaponSwingState = WeaponSwingState.SWINGING;
            // reset charge interrupted flag for next potential attack charge
            chargeInterrupted = false;
        }

        public override bool HandleAttackInputs(bool inputDown, bool inputHeld, bool inputReleased){
            if (inputDown)
            {
                // player needs to have at least stamina to execute quick attack
                if (playerStamina && 
                    playerStamina.currentStamina < staminaRequirementForAttack + quickSwingStaminaRequirementMod) 
                {
                    // otherwise inform them
                    playerStamina.onMissingStamina.Invoke();
                    return false;
                }
                // when stamina is sufficient, start charging
                if (weaponAnimator)
                {
                    weaponAnimator.SetTrigger(MeleeAnimatorConstants.startedChargingTrigger); 
                }
                weaponSwingState = WeaponSwingState.PREPARING_TO_CHARGE; 
                return false;
            }
            // releasing means player wants to attack, make sure weapon state reflects this
            if (inputReleased)
            {
                if (weaponSwingState == WeaponSwingState.CHARGING || 
                    weaponSwingState == WeaponSwingState.PREPARING_TO_CHARGE)
                {
                    InitiateSwing();
                    return true;
                } 
            }
            // If player continues to hold down, make sure that the GUI is updated
            if (inputHeld)
            {
                if (weaponSwingState == WeaponSwingState.CHARGING)
                {
                    UpdateChargeGUI();
                }
            }
            return false;
        }

        private void UpdateChargeGUI()
        {
            // do not show the charge bar if the player is executing a quick swing, not charging
            if (weaponSwingState == WeaponSwingState.CHARGING 
                && lastChargingStartTime + kChargeTimeThatIsQuickSwing < Time.time)
            {
                // calculate the percent that the swing is charged
                var chargingPercent = GetCurrentAnimationCompletionPercent(lastChargingStartTime);
                // use to update the attack charge bar
                meleeAttackBar.UpdateAttackBar(chargingPercent, perfectSwingChargeOffset,
                    perfectSwingChargeSize);
            }
        }

        private void InitiateSwing()
        {
            // determine if player is executing quick swing by checking if they started charging and have not released before
            // surpassing the defined quick swing time threshold
            executingQuickSwing = weaponSwingState == WeaponSwingState.PREPARING_TO_CHARGE || 
                                  lastChargingStartTime + kChargeTimeThatIsQuickSwing > Time.time;
            // we want to calculate and store the actual damage value after the player has released the attack button,
            // not when the charge animation actually ends. We are storing the damage amount in a variable so we can
            // apply it to enemies once the attack animation starts playing.
            if (executingQuickSwing)
            {
                // quick swing means base damage is multiplied by the quick swing percent as well as the current swing damage percent of the base damage
                currentAttackDamage = baseDamage.damageAmount + currentSwingStats.damageMod + quickSwingDamageMod;
            }
            else
            {
                // if player executed a perfect strike, final charged daamge is base damage with perfect swing damage added
                // otherwise, calculate the additional damage based on how much the player has charged their attack
                // both are multiplied by the percentage of damage defined by the current swing 
                currentAttackDamage = IsPerfectStrike()
                    ? baseDamage.damageAmount + currentSwingStats.damageMod + perfectSwingExtraDamage
                    : baseDamage.damageAmount + currentSwingStats.damageMod + 
                      (maxChargeExtraDamageAmount * GetCurrentAnimationCompletionPercent(lastChargingStartTime));
            }
            if (weaponAnimator)
            {
                // initiate attack via the animator, transition confirmation to attacking is done once the animator has started it
                weaponAnimator.SetTrigger(MeleeAnimatorConstants.startedSwingTrigger);
            }
        }

        public void InterruptCharge(){
            if (weaponSwingState == WeaponSwingState.CHARGING)
            {
                // animator transition straight to idle
                weaponAnimator.SetTrigger(MeleeAnimatorConstants.interruptedChargingTrigger);
                // set flag before initiating end of charge to make sure an attack is not executed
                chargeInterrupted = true;
                OnSwingEnded();
            }
        }

        private float GetCurrentAnimationCompletionPercent(float animStartTime) {
            // current animator state info contains length of animation. We need to multiply that by the known animation speed multiplier
            // which we can retrive using the hash that we pass in to the function (these are stored in  integers) to get the real time
            // in seconds that the current animation is going to play from.
            var totalAnimTime = weaponAnimator.GetCurrentAnimatorStateInfo(MeleeAnimatorConstants.attacksAnimationStateLayer).length;
            // pass in the time that the animation started playing to determine for how many seconds it has been playing.
            var currentAnimTime = Mathf.Clamp(Time.time - animStartTime, 0.001f, totalAnimTime);
            // calculate the percentage of completion that the animation has been playing for
            return currentAnimTime / totalAnimTime;
        }

        private bool AnimatorInIdleState(){
            // use animator to determine if an animation is executed
            return weaponAnimator.GetCurrentAnimatorStateInfo(MeleeAnimatorConstants.attacksAnimationStateLayer).IsName(MeleeAnimatorConstants.idleState) &&
                   !weaponAnimator.IsInTransition(MeleeAnimatorConstants.attacksAnimationStateLayer);
        }

        public override bool CanSwitchWeapon(){
            return AnimatorInIdleState() && !IsSwitchingWeapon();
        }

        private bool IsPerfectStrike(){
            // calculate the charging percent
            var chargingPercent = GetCurrentAnimationCompletionPercent(lastChargingStartTime);
            // a perfect strike depends on whether the charging percent is between the offset and the offset + charge bar size
            return chargingPercent > perfectSwingChargeOffset &&
                   chargingPercent < (perfectSwingChargeOffset + perfectSwingChargeSize);
        }

        public void OnHitObject(RaycastHit hit){
            if (weaponSwingState != WeaponSwingState.SWINGING) return;
            // interrupt attack if weapon can only hit one enemy and has already hit one
            if (!currentSwingStats.canDamageMultipleEnemies && enemyCollidersHitThisSwing.Count > 0) return;
            
            Damageable damageable = hit.collider.gameObject.GetComponent<Damageable>();
            // we hit the environment instead of an enemy or destroyable entity
            if (!damageable)
            {
                // if player hit object in the environment that is not damageable, stun them
                weaponAnimator.SetTrigger(MeleeAnimatorConstants.playerStunnedTrigger);
                weaponSwingState = WeaponSwingState.IDLE;
                // initiate effects when player hits the environment
                if (defaultHitFX)
                {
                    GameObject impactVFXInstance = Instantiate(defaultHitFX, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(impactVFXInstance.gameObject, 5.0f);
                }
                if (defaultHitSFX)
                {
                    AudioUtility.CreateSFX(defaultHitSFX, hit.point, AudioUtility.AudioGroups.Impact, 1f, 3f);
                }
            }
            else if (!enemyCollidersHitThisSwing.Contains(hit.collider)) // check we aren't handling the same items more than once
            {
                // add to our colliders hit check list
                enemyCollidersHitThisSwing.Add(hit.collider);
                // clone the damage info object, because we are about to assign some new values to it
                DamageInfo damageToApply = (DamageInfo) baseDamage.Clone();
                // we calculated this value earlier when we initiated the attack
                damageToApply.damageAmount = currentAttackDamage;
                // the owner value is assigned when the weapon is equipped
                damageToApply.damageSource = owner;
                // calculate the damage knockback from the current swing and base knockback, add knockback manipulator
                float knockbackForce = executingQuickSwing ? baseDamage.targetKnockbackOnHitForce + currentSwingStats.knockbackMod + quickSwingKnockbackMod :
                baseDamage.targetKnockbackOnHitForce + currentSwingStats.knockbackMod;
                damageToApply.targetKnockbackOnHitForce = knockbackForce;
                damageable.InflictDamage(damageToApply);
            }
        }
    }
}