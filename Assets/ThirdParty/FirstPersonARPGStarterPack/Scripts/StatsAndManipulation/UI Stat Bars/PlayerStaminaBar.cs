using FPS.Scripts.Characters.UI;
using UnityEngine;
using UnityEngine.Video;

public class PlayerStaminaBar : StatsBar
{
    Stamina m_PlayerStamina;

    private float timeMissingStaminaWasLastShown = -Mathf.Infinity;
    public GuiAlphaFlicker missingStaminaWarning;
    public float timeStaminaWarningIsShown = 1f;

    protected override void InitialiseStatBar()
    {
        PlayerCharacterController playerCharacterController = FindObjectOfType<PlayerCharacterController>();
        DebugUtility.HandleErrorIfNullFindObject<PlayerCharacterController, PlayerStaminaBar>(playerCharacterController, this);

        m_PlayerStamina = playerCharacterController.GetComponent<Stamina>();
        DebugUtility.HandleErrorIfNullGetComponent<Stamina, PlayerStaminaBar>(m_PlayerStamina, this, playerCharacterController.gameObject);
        m_PlayerStamina.onStaminaDrained += OnStaminaDrained;
        m_PlayerStamina.onStaminaRecovered += OnStaminaRecovered;
        m_PlayerStamina.onMissingStamina += OnInsufficientStamina;

        if (timeTillBarCatchesUp > m_PlayerStamina.timeTillRecoveryStart)
        {
            timeTillBarCatchesUp = m_PlayerStamina.timeTillRecoveryStart;
        }
    }

    protected override void UpdateStatBar()
    {
        if (Time.time > timeMissingStaminaWasLastShown + timeStaminaWarningIsShown)
        {
            missingStaminaWarning.shouldShow = false;
        }
    }

    protected override float GetStatFillPercent()
    {
        return m_PlayerStamina.currentStamina / m_PlayerStamina.maxStamina;
    }

    void OnStaminaDrained(float amountDrained)
    {
        OnStatValueDeducted();
    }

    void OnStaminaRecovered(float recoveryAmount)
    {
        OnStatValueIncremented();
    }

    void OnInsufficientStamina()
    {
        timeMissingStaminaWasLastShown = Time.time;
        missingStaminaWarning.shouldShow = true;
    }
}
