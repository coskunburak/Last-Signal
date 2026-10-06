using System.Collections.Generic;
using FPS.Scripts.Characters.AI;
using FPS.Scripts.FX;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

[RequireComponent(typeof(EnemyController))]
public class EnemyMobile : MonoBehaviour
{
    public enum AIState
    {
        Patrol,
        Follow,
        Attack,
    }

    public Animator animator;
    [Tooltip("Fraction of the enemy's attack range at which it will stop moving towards target while attacking")]
    [Range(0f, 1f)]
    public float attackStopDistanceRatio = 0.5f;
    public ParticleSystem[] onDetectVFX;
    public AudioClip onDetectSFX;
    
    [FormerlySerializedAs("followingWalkSpeedMod")] public float chasingWalkSpeedMod = 1f;
    private NavMeshAgent navMeshAgent;
    private float initWalkSpeed;
    
    [Header("Sound")]
    public AudioClip MovementSound;
    public MinMaxFloat PitchDistortionMovementSpeed;

    public AIState aiState { get; set; }
    EnemyController m_EnemyController;
    AudioSource m_AudioSource;

    private EnemyDamagedBasedStun damagedBasedStun;
    
    void Start()
    {
        m_EnemyController = GetComponent<EnemyController>();
        DebugUtility.HandleErrorIfNullGetComponent<EnemyController, EnemyMobile>(m_EnemyController, this, gameObject);

        m_EnemyController.onAttack += OnAttack;
        m_EnemyController.onDetectedTarget += OnDetectedTarget;
        m_EnemyController.onLostTarget += OnLostTarget;
        m_EnemyController.SetPathDestinationToClosestNode();

        damagedBasedStun = GetComponent<EnemyDamagedBasedStun>();

        navMeshAgent = GetComponent<NavMeshAgent>();
        initWalkSpeed = navMeshAgent.speed;
        
        // Start patrolling
        aiState = AIState.Patrol;

        // adding a audio source to play the movement sound on it
        m_AudioSource = GetComponent<AudioSource>();
        DebugUtility.HandleErrorIfNullGetComponent<AudioSource, EnemyMobile>(m_AudioSource, this, gameObject);
        m_AudioSource.clip = MovementSound;
        m_AudioSource.Play();
    }

    void Update()
    {
        UpdateAIStateTransitions();
        UpdateCurrentAIState();

        float moveSpeed = m_EnemyController.m_NavMeshAgent.velocity.magnitude;

        // Update animator speed parameter
        animator.SetFloat(EnemyAnimatorConstants.movementSpeedParameter, moveSpeed);

        // changing the pitch of the movement sound depending on the movement speed
        m_AudioSource.pitch = Mathf.Lerp(PitchDistortionMovementSpeed.min, PitchDistortionMovementSpeed.max, moveSpeed / m_EnemyController.m_NavMeshAgent.speed);
    }

    void UpdateAIStateTransitions()
    {
        // Handle transitions 
        switch (aiState)
        {
            case AIState.Patrol:
                navMeshAgent.speed = initWalkSpeed;
                break;
            case AIState.Follow:
                if (m_EnemyController.isSeeingTarget)
                {
                    m_EnemyController.m_CurrentAttack.SetTarget(m_EnemyController.knownDetectedTarget);
                    if (m_EnemyController.m_CurrentAttack.CanAttack())
                    {
                        aiState = AIState.Attack;
                        m_EnemyController.SetNavDestination(transform.position);   
                    }
                    navMeshAgent.speed = initWalkSpeed * chasingWalkSpeedMod;
                }
                break;
            case AIState.Attack:
                if (!m_EnemyController.m_CurrentAttack.CanAttack() && 
                    !m_EnemyController.m_CurrentAttack.isAttacking)
                {
                    m_EnemyController.m_CurrentAttack.OnEnemySwitchedFromAttackState();
                    aiState = AIState.Follow;
                }
                break;
        }
    }

    void UpdateCurrentAIState()
    {
        if (damagedBasedStun && damagedBasedStun.isStunned) return;
        // Handle logic 
        switch (aiState)
        {
            case AIState.Patrol:
                navMeshAgent.speed = initWalkSpeed;
                m_EnemyController.UpdatePathDestination();
                m_EnemyController.SetNavDestination(m_EnemyController.GetDestinationOnPath());
                break;
            case AIState.Follow:
                m_EnemyController.SetNavDestination(m_EnemyController.knownDetectedTarget.transform.position);
                m_EnemyController.OrientTowards(m_EnemyController.knownDetectedTarget.transform.position);
                navMeshAgent.speed = initWalkSpeed * chasingWalkSpeedMod;
                break;
            case AIState.Attack:
                m_EnemyController.m_CurrentAttack.SetTarget(m_EnemyController.knownDetectedTarget);
                if (m_EnemyController.m_CurrentAttack.ShouldFollowEnemyWhileAttacking())
                {
                    m_EnemyController.SetNavDestination(m_EnemyController.knownDetectedTarget.transform.position);
                    m_EnemyController.OrientTowards(m_EnemyController.knownDetectedTarget.transform.position);
                }
                else
                {
                    m_EnemyController.SetNavDestination(transform.position);
                }
                m_EnemyController.TryAttack();
                break;
        }
    }

    void OnAttack()
    {
    }

    void OnDetectedTarget()
    {
        if (aiState == AIState.Patrol)
        {
            aiState = AIState.Follow;
        }
        
        for (int i = 0; i < onDetectVFX.Length; i++)
        {
            onDetectVFX[i].Play();
        }

        if (onDetectSFX)
        {
            AudioUtility.CreateSFX(onDetectSFX, transform.position, AudioUtility.AudioGroups.EnemyDetection, 1f);
        }

        animator.SetBool(EnemyAnimatorConstants.alertedParameter, true);
    }

    void OnLostTarget()
    {
        if (aiState == AIState.Follow || aiState == AIState.Attack)
        {
            aiState = AIState.Patrol;
        }

        for (int i = 0; i < onDetectVFX.Length; i++)
        {
            onDetectVFX[i].Stop();
        }

        animator.SetBool(EnemyAnimatorConstants.alertedParameter, false);
    }
}
