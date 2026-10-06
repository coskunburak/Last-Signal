using FPS.Scripts.Items.Weapons.WeaponTypes.Melee;
using UnityEngine;

public class ChargeSwingBehaviour : StateMachineBehaviour
{
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        GetWeaponController(animator).OnChargeStarted();
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        GetWeaponController(animator).OnChargedEnded();
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
    
    MeleeWeapon GetWeaponController(Animator animator){
        PlayerWeaponsManager weaponController = animator.GetComponentInParent<PlayerWeaponsManager>();
        if (weaponController == null)
        {
            weaponController = animator.GetComponent<PlayerWeaponsManager>();
        }
        return (MeleeWeapon)weaponController.GetActiveWeapon();
    }
}
