using FPS.Scripts.Characters;
using FPS.Scripts.Characters.UI;
using Main.StatsAndManipulation;
using UnityEngine;

public class PlayerHealthBar : StatsBar
{
    Health m_PlayerHealth;

    protected override void InitialiseStatBar()
    {
        PlayerCharacterController playerCharacterController = GameObject.FindObjectOfType<PlayerCharacterController>();
        DebugUtility.HandleErrorIfNullFindObject<PlayerCharacterController, PlayerHealthBar>(playerCharacterController, this);

        m_PlayerHealth = playerCharacterController.GetComponent<Health>();
        DebugUtility.HandleErrorIfNullGetComponent<Health, PlayerHealthBar>(m_PlayerHealth, this, playerCharacterController.gameObject);
        m_PlayerHealth.onDamaged += OnDamaged;
        m_PlayerHealth.onHealed += OnHealed;
    }
    
    protected override float GetStatFillPercent()
    {
        return fillImage.fillAmount = m_PlayerHealth.currentHealth / m_PlayerHealth.maxHealth;
    }

    void OnDamaged(DamageInfo damage)
    {
        OnStatValueDeducted();
    }

    void OnHealed(float healAmount)
    {
        OnStatValueIncremented();
    }
}
