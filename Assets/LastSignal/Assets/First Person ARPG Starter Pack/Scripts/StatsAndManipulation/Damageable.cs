using FPS.Scripts.Characters;
using Main.StatsAndManipulation;
using UnityEngine;

public class Damageable : MonoBehaviour
{
    [Tooltip("Multiplier to apply to the received damage")]
    public float damageMultiplier = 1f;
    [Range(0, 1)]
    [Tooltip("Multiplier to apply to self damage")]
    public float sensibilityToSelfdamage = 0.5f;

    public Health health { get; private set; }

    void Awake()
    {
        // find the health component either at the same level, or higher in the hierarchy
        health = GetComponent<Health>();
        if (!health)
        {
            health = GetComponentInParent<Health>();
        }
    }

    public void InflictDamage(DamageInfo damageInfo)
    {
        if(health)
        {
            
            
            var totalDamage = damageInfo.damageAmount;

            // skip the crit multiplier if it's from an explosion
            if (!damageInfo.isExplosionDamage)
            {
                totalDamage *= damageMultiplier;
            }

            // potentially reduce damages if inflicted by self
            if (health.gameObject == damageInfo.damageSource)
            {
                totalDamage *= sensibilityToSelfdamage;
            }

            damageInfo.damageAmount = totalDamage;
            // apply the damages
            health.TakeDamage(damageInfo);
        }
    }
}
