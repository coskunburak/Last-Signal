using UnityEngine;

namespace Main.Characters.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerKnockback : CharacterKnockback
    {
        private CharacterController characterMovement;
        
        protected override void InitialiseComponent()
        {
            characterMovement = GetComponent<CharacterController>();
            DebugUtility.HandleErrorIfNullGetComponent<CharacterController, PlayerKnockback>(characterMovement, this, gameObject);
        }

        protected override void HandleKnockback()
        {
            if (ShouldApplyKnockback())
            {
                characterMovement.SimpleMove(currentKnockbackDirection * (currentKnockback * Time.deltaTime));
            }
        }
    }
}