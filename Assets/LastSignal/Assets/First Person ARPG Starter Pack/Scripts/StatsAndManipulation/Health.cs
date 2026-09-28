﻿using FPS.Scripts.Characters;
 using Main.StatsAndManipulation;
 using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Tooltip("Maximum amount of health")]
    public float maxHealth = 10f;
    [Tooltip("Health ratio at which the critical health vignette starts appearing")]
    public float criticalHealthRatio = 0.3f;

    public UnityAction<DamageInfo> onHitWithDamage;
    public UnityAction<DamageInfo> onDamaged;
    public UnityAction<float> onHealed;
    public UnityAction onDie;

    public float currentHealth { get; set; }

    public DamageBlocker currentDamageBlocker { private get; set; }
    public bool invincible { get; set; }
    public bool canPickup() => currentHealth < maxHealth;

    public float getRatio() => currentHealth / maxHealth;
    public bool isCritical() => getRatio() <= criticalHealthRatio;

    bool m_IsDead;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void Heal(float healAmount)
    {
        float healthBefore = currentHealth;
        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // call OnHeal action
        float trueHealAmount = currentHealth - healthBefore;
        if (trueHealAmount > 0f && onHealed != null)
        {
            onHealed.Invoke(trueHealAmount);
        }
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (invincible)
            return;
        
        onHitWithDamage?.Invoke(damageInfo);
        var damageAmount = damageInfo.damageAmount;
        if (currentDamageBlocker)
        {
            if (currentDamageBlocker.CanBlockAttack(damageInfo))
            {
                currentDamageBlocker.OnDamageBlocked(damageInfo);
                damageAmount = currentDamageBlocker.CalculatedDamageAmountAfterBlock(damageInfo);
            }
        }

        float healthBefore = currentHealth;
        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // call OnDamage action
        float trueDamageAmount = healthBefore - currentHealth;
        if (trueDamageAmount > 0.01f && onDamaged != null)
        {
            onDamaged.Invoke(damageInfo);
        }

        HandleDeath();
    }

    public void Kill()
    {
        currentHealth = 0f;

        // call OnDamage action
        if (onDamaged != null)
        {
            var damageInfo = new DamageInfo();
            damageInfo.damageAmount = maxHealth;
            onDamaged.Invoke(damageInfo);
        }

        HandleDeath();
    }

    private void HandleDeath()
    {
        if (m_IsDead)
            return;

        // call OnDie action
        if (currentHealth <= 0f)
        {
            if (onDie != null)
            {
                m_IsDead = true;
                onDie.Invoke();
            }
        }
    }
}
