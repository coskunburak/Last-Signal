using System;
using FPS.Scripts.Items.Weapons.WeaponTypes;
using UnityEngine;

namespace FPS.Scripts.Items.Weapons{
    [RequireComponent(typeof(WeaponController))]
    public class WeaponAnimatorDirectionUpdate : MonoBehaviour{
        private PlayerInputHandler playerInputHandler;
        private Animator weaponAnimator;
        
        private static readonly int directionAnimatorParamHash = MeleeAnimatorConstants.actionDirection;
        
        void Start(){
            if (weaponAnimator == null)
            {
                weaponAnimator = GetComponent<WeaponController>().weaponAnimator;
                DebugUtility.HandleErrorIfNullGetComponent<Animator, PlayerWeaponsManager>(weaponAnimator, this,
                    gameObject);
            }

            playerInputHandler = GetComponentInParent<PlayerInputHandler>();
            DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, PlayerWeaponsManager>(playerInputHandler, this, gameObject);
        }

        void Update(){
            // each frame, update the player weapons animators direction variable
            if (playerInputHandler != null)
            {
                var actionDirectionValue = 0.5f + Convert.ToInt32(playerInputHandler.GetActionDirection());
                weaponAnimator.SetFloat(directionAnimatorParamHash, actionDirectionValue);
            }
        }

        PlayerInputHandler.ActionDirection GetCurrentActionDirection(){
            return playerInputHandler.GetActionDirection();
        }
    }
}