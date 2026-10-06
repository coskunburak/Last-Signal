using Main.Characters;
using UnityEngine;
using UnityEngine.AI;

namespace FPS.Scripts.Characters.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyKnockback : CharacterKnockback
    {
        private NavMeshAgent navMeshAgent;
        protected override void InitialiseComponent()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        protected override void HandleKnockback()
        {
            if (ShouldApplyKnockback())
            {
                navMeshAgent.Move((currentKnockback*Time.deltaTime)*currentKnockbackDirection);
            }
        }
    }
}