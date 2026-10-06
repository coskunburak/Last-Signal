using FPS.Scripts.Characters.AI.Behaviours.Attacks;
using UnityEngine;

public class EnemyAttackStateBehaviour : StateMachineBehaviour
{
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        EnemyAttack enemyAttack = GetEnemyController(animator).m_CurrentAttack;
        enemyAttack.OnAttackStarted(stateInfo.length);
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex){
        EnemyAttack enemyAttack = GetEnemyController(animator).m_CurrentAttack;
        enemyAttack.OnAttackEnded();
    }

    private static EnemyController GetEnemyController(Animator animator){
        var enemyController = animator.gameObject.GetComponentInParent<EnemyController>();
        if (enemyController == null)
        {
            enemyController = animator.gameObject.GetComponent<EnemyController>();
        }
        return enemyController;
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
}
