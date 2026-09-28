using System.Collections.Generic;
using FPS.Scripts.Characters;
using FPS.Scripts.Characters.AI;
using FPS.Scripts.Characters.AI.Behaviours.Attacks;
using FPS.Scripts.FX;
using FPS.Scripts.Items.Weapons;
using Main.StatsAndManipulation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Health), typeof(Actor), typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [System.Serializable]
    public struct RendererIndexData
    {
        public Renderer renderer;
        public int materialIndex;

        public RendererIndexData(Renderer renderer, int index)
        {
            this.renderer = renderer;
            this.materialIndex = index;
        }
    }

    [Header("Parameters")]
    [Tooltip("The Y height at which the enemy will be automatically killed (if it falls off of the level)")]
    public float selfDestructYHeight = -20f;
    [Tooltip("The distance at which the enemy considers that it has reached its current path destination point")]
    public float pathReachingRadius = 2f;
    [Tooltip("The speed at which the enemy rotates")]
    public float orientationSpeed = 10f;
    [Tooltip("Delay after death where the GameObject is destroyed (to allow for animation)")]
    public float deathDuration = 0f;
    [Tooltip("Object spawned when enemy dies)")]
    public GameObject corpse;

    public Animator animator;
    
    [FormerlySerializedAs("swapToNextWeapon")]
    [Header("Weapons Parameters")]
    [Tooltip("Allow weapon swapping for this enemy")]
    public bool swapToNextAttack = false;
    [FormerlySerializedAs("delayAfterWeaponSwap")] [Tooltip("Time delay between a weapon swap and the next attack")]
    public float delayAfterAttackSwap = 0f;

    [Header("Flash on hit")]
    [Tooltip("The material used for the body of the hoverbot")]
    public Material bodyMaterial;
    [Tooltip("The gradient representing the color of the flash on hit")]
    [GradientUsageAttribute(true)]
    public Gradient onHitBodyGradient;
    [Tooltip("The duration of the flash on hit")]
    public float flashOnHitDuration = 0.5f;

    [Header("Sounds")]
    [Tooltip("Sound played when recieving damages")]
    public AudioClip damageTick;

    [Header("VFX")]
    [Tooltip("The VFX prefab spawned when the enemy dies")]
    public GameObject deathVFX;
    [Tooltip("The point at which the death VFX is spawned")]
    public Transform deathVFXSpawnPoint;

    [Header("Loot")]
    [Tooltip("The object this enemy can drop when dying")]
    public GameObject lootPrefab;
    [Tooltip("The chance the object has to drop")]
    [Range(0, 1)]
    public float dropRate = 1f;

    [Header("Debug Display")]
    [Tooltip("Color of the sphere gizmo representing the path reaching range")]
    public Color pathReachingRangeColor = Color.yellow;
    [Tooltip("Color of the sphere gizmo representing the attack range")]
    public Color attackRangeColor = Color.red;
    [Tooltip("Color of the sphere gizmo representing the detection range")]
    public Color detectionRangeColor = Color.blue;

    public UnityAction onAttack;
    public UnityAction onDetectedTarget;
    public UnityAction onLostTarget;
    public UnityAction onDamaged;

    List<RendererIndexData> m_BodyRenderers = new List<RendererIndexData>();
    MaterialPropertyBlock m_BodyFlashMaterialPropertyBlock;
    float m_LastTimeDamaged = float.NegativeInfinity;
    
    public PatrolPath patrolPath { get; set; }
    public GameObject knownDetectedTarget => m_DetectionModule.knownDetectedTarget;
    public bool isSeeingTarget => m_DetectionModule.isSeeingTarget;
    public bool hadKnownTarget => m_DetectionModule.hadKnownTarget;
    public NavMeshAgent m_NavMeshAgent { get; private set; }
    public DetectionModule m_DetectionModule;

    int m_PathDestinationNodeIndex;
    EnemyManager m_EnemyManager;
    ActorsManager m_ActorsManager;
    Health m_Health;
    Actor m_Actor;
    Collider[] m_SelfColliders;
    bool m_WasDamagedThisFrame;
    float m_LastTimeAttackedOrAttackSwapped = Mathf.NegativeInfinity;
    int m_CurrentAttackIndex;
    public EnemyAttack m_CurrentAttack{ private set; get; }
    EnemyAttack[] m_Attacks;
    NavigationModule m_NavigationModule;

    void Start()
    {
        
        m_EnemyManager = FindObjectOfType<EnemyManager>();
        DebugUtility.HandleErrorIfNullFindObject<EnemyManager, EnemyController>(m_EnemyManager, this);

        m_ActorsManager = FindObjectOfType<ActorsManager>();
        DebugUtility.HandleErrorIfNullFindObject<ActorsManager, EnemyController>(m_ActorsManager, this);

        m_EnemyManager.RegisterEnemy(this);

        m_Health = GetComponent<Health>();
        DebugUtility.HandleErrorIfNullGetComponent<Health, EnemyController>(m_Health, this, gameObject);

        m_Actor = GetComponent<Actor>();
        DebugUtility.HandleErrorIfNullGetComponent<Actor, EnemyController>(m_Actor, this, gameObject);

        m_NavMeshAgent = GetComponent<NavMeshAgent>();
        m_SelfColliders = GetComponentsInChildren<Collider>();

        // Subscribe to damage & death actions
        m_Health.onDie += OnDie;
        m_Health.onDamaged += OnDamaged;

        // Find and initialize all attack objects
        FindAndInitializeAllWeapons();
        var attack = GetCurrentAttack();
        attack.OnSwappedToAttack();

        if (m_DetectionModule == null)
        {
            var detectionModules = GetComponentsInChildren<DetectionModule>();
            DebugUtility.HandleErrorIfNoComponentFound<DetectionModule, EnemyController>(detectionModules.Length, this,
                gameObject);
            DebugUtility.HandleWarningIfDuplicateObjects<DetectionModule, EnemyController>(detectionModules.Length,
                this, gameObject);
            m_DetectionModule = detectionModules[0];
        }

        // Initialize detection module
        m_DetectionModule.onDetectedTarget += OnDetectedTarget;
        m_DetectionModule.onLostTarget += OnLostTarget;
        onAttack += m_DetectionModule.OnAttack;

        var navigationModules = GetComponentsInChildren<NavigationModule>();
        // Override navmesh agent data
        if (navigationModules.Length > 0)
        {
            m_NavigationModule = navigationModules[0];
            m_NavMeshAgent.speed = m_NavigationModule.moveSpeed;
            m_NavMeshAgent.angularSpeed = m_NavigationModule.angularSpeed;
            m_NavMeshAgent.acceleration = m_NavigationModule.acceleration;
        }

        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                if (renderer.sharedMaterials[i] == bodyMaterial)
                {
                    m_BodyRenderers.Add(new RendererIndexData(renderer, i));
                }
            }
        }

        m_BodyFlashMaterialPropertyBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        EnsureIsWithinLevelBounds();

        if (m_DetectionModule == null) Debug.Log("didnt find detection module");
        m_DetectionModule.HandleTargetDetection(m_Actor, m_SelfColliders);

        Color currentColor = onHitBodyGradient.Evaluate((Time.time - m_LastTimeDamaged) / flashOnHitDuration);
        m_BodyFlashMaterialPropertyBlock.SetColor("_EmissionColor", currentColor);
        foreach (var data in m_BodyRenderers)
        {
            data.renderer.SetPropertyBlock(m_BodyFlashMaterialPropertyBlock, data.materialIndex);
        }

        m_WasDamagedThisFrame = false;
    }

    void EnsureIsWithinLevelBounds()
    {
        // at every frame, this tests for conditions to kill the enemy
        if (transform.position.y < selfDestructYHeight)
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnLostTarget()
    {
        onLostTarget.Invoke();
    }

    void OnDetectedTarget()
    {
        onDetectedTarget.Invoke();
    }

    public void OrientTowards(Vector3 lookPosition)
    {
        Vector3 lookDirection = Vector3.ProjectOnPlane(lookPosition - transform.position, Vector3.up).normalized;
        if (lookDirection.sqrMagnitude != 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * orientationSpeed);
        }
    }

    private bool IsPathValid()
    {
        return patrolPath && patrolPath.pathNodes.Count > 0;
    }

    public void ResetPathDestination()
    {
        m_PathDestinationNodeIndex = 0;
    }

    public void SetPathDestinationToClosestNode()
    {
        if (IsPathValid())
        {
            int closestPathNodeIndex = 0;
            for (int i = 0; i < patrolPath.pathNodes.Count; i++)
            {
                float distanceToPathNode = patrolPath.GetDistanceToNode(transform.position, i);
                if (distanceToPathNode < patrolPath.GetDistanceToNode(transform.position, closestPathNodeIndex))
                {
                    closestPathNodeIndex = i;
                }
            }

            m_PathDestinationNodeIndex = closestPathNodeIndex;
        }
        else
        {
            m_PathDestinationNodeIndex = 0;
        }
    }

    public Vector3 GetDestinationOnPath()
    {
        if (IsPathValid())
        {
            return patrolPath.GetPositionOfPathNode(m_PathDestinationNodeIndex);
        }
        else
        {
            return transform.position;
        }
    }

    public void SetNavDestination(Vector3 destination)
    {
        if (m_NavMeshAgent)
        {
            m_NavMeshAgent.SetDestination(destination);
        }
    }

    public void UpdatePathDestination(bool inverseOrder = false)
    {
        if (IsPathValid())
        {
            // Check if reached the path destination
            if ((transform.position - GetDestinationOnPath()).magnitude <= pathReachingRadius)
            {
                // increment path destination index
                m_PathDestinationNodeIndex = inverseOrder ? (m_PathDestinationNodeIndex - 1) : (m_PathDestinationNodeIndex + 1);
                if (m_PathDestinationNodeIndex < 0)
                {
                    m_PathDestinationNodeIndex += patrolPath.pathNodes.Count;
                }
                if (m_PathDestinationNodeIndex >= patrolPath.pathNodes.Count)
                {
                    m_PathDestinationNodeIndex -= patrolPath.pathNodes.Count;
                }
            }
        }
    }

    void OnDamaged(DamageInfo damageInfo)
    {
        var damageSource = damageInfo.damageSource;
        // test if the damage source is the player
        if (damageSource && damageSource.GetComponent<PlayerCharacterController>())
        {
            // pursue the player
            m_DetectionModule.OnDamaged(damageSource);

            if (onDamaged != null)
            {
                onDamaged.Invoke();
            }
            m_LastTimeDamaged = Time.time;

            // play the damage tick sound
            if (damageTick && !m_WasDamagedThisFrame)
                AudioUtility.CreateSFX(damageTick, transform.position, AudioUtility.AudioGroups.DamageTick, 0f);
            
            if (animator != null)
            {
                animator.SetTrigger(EnemyAnimatorConstants.onDamagedTrigger);
            }
            
            m_WasDamagedThisFrame = true;
        }
    }

    void OnDie()
    {
        // spawn a particle system when dying
        if (deathVFX)
        {
            var vfx = Instantiate(deathVFX, deathVFXSpawnPoint.position, Quaternion.identity);
            Destroy(vfx, 5f);
        }

        // tells the game flow manager to handle the enemy destuction
        m_EnemyManager.UnregisterEnemy(this);

        if (animator)
        {
            animator.SetTrigger(EnemyAnimatorConstants.onDeathTrigger);
        }

        // loot an object
        if (TryDropItem())
        {
            Instantiate(lootPrefab, transform.position, Quaternion.identity);
        }

        if (corpse)
        {
            var corpseInstance = Instantiate(corpse, transform.position, transform.rotation);
            Destroy(corpseInstance, 5f);
        }
        // this will call the OnDestroy function
        Destroy(gameObject, deathDuration);
    }

    private void OnDrawGizmosSelected()
    {
        // Path reaching range
        Gizmos.color = pathReachingRangeColor;
        Gizmos.DrawWireSphere(transform.position, pathReachingRadius);

        if (m_DetectionModule != null)
        {
            // Detection range
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, m_DetectionModule.detectionRange);

            // Attack range
            //Gizmos.color = attackRangeColor;
            //Gizmos.DrawWireSphere(transform.position, m_CurrentAttack.attackRange);
        }
    }

    public bool TryAttack()
    {
        if ((m_LastTimeAttackedOrAttackSwapped + delayAfterAttackSwap) >= Time.time)
            return false;

        bool performedAttack = GetCurrentAttack().PerformAttack();
        if (performedAttack && onAttack != null)
        {
            onAttack.Invoke();
            
            if (swapToNextAttack && m_Attacks.Length > 1)
            {
                int nextWeaponIndex = (m_CurrentAttackIndex + 1) % m_Attacks.Length;
                SetCurrentWeapon(nextWeaponIndex);
            }
        }

        return performedAttack;
    }

    public bool TryDropItem()
    {
        if (dropRate == 0 || lootPrefab == null)
            return false;
        else if (dropRate == 1)
            return true;
        else
            return (Random.value <= dropRate);
    }

    void FindAndInitializeAllWeapons()
    {
        // Check if we already found and initialized the weapons
        if (m_Attacks == null)
        {
            m_Attacks = GetComponentsInChildren<EnemyAttack>();
            DebugUtility.HandleErrorIfNoComponentFound<EnemyAttack, EnemyController>(m_Attacks.Length, this, gameObject);

            for (int i = 0; i < m_Attacks.Length; i++)
            {
                m_Attacks[i].OnAttackInitialised(this);
            }
        }
    }

    public EnemyAttack GetCurrentAttack()
    {
        FindAndInitializeAllWeapons();
        // Check if no weapon is currently selected
        if (m_CurrentAttack == null)
        {
            // Set the first weapon of the weapons list as the current weapon
            SetCurrentWeapon(0);
        }
        DebugUtility.HandleErrorIfNullGetComponent<WeaponController, EnemyController>(m_CurrentAttack, this, gameObject);

        return m_CurrentAttack;
    }

    void SetCurrentWeapon(int index)
    {
        m_CurrentAttackIndex = index;
        m_CurrentAttack = m_Attacks[m_CurrentAttackIndex];
        if (swapToNextAttack)
        {
            m_LastTimeAttackedOrAttackSwapped = Time.time;
        }
        else
        {
            m_LastTimeAttackedOrAttackSwapped = Mathf.NegativeInfinity;
        }
    }
}
