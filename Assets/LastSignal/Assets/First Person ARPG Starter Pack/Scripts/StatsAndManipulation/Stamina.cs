using UnityEngine;
using UnityEngine.Events;

public class Stamina : MonoBehaviour
{
    [Tooltip("Maximum amount of stamina")]
    public float maxStamina = 100f;
    [Tooltip("Seconds till recovery of Stamina begins")]
    public float timeTillRecoveryStart = 6.0f;

    public UnityAction<float> onStaminaDrained;
    public UnityAction<float> onStaminaRecovered;
    public UnityAction onMissingStamina;

    private float lastTimeStaminaWasRecovered = -1.0f;
    [Tooltip("Interval rate in Seconds at which Stamina is replenished")]
    public float recoveryIntervalTime = 2.0f;
    [Tooltip("Amount of Stamina recovered after each Interval")]
    public float recoveryIntervalAmount = 5.0f;

    private float timeSinceLastStaminaDrainingAction = 0.0f;
    public float currentStamina { get; set; }
    
    // Start is called before the first frame update
    void Start()
    {
        currentStamina = maxStamina;
        timeSinceLastStaminaDrainingAction = timeTillRecoveryStart;
    }

    // Update is called once per frame
    void Update()
    {
        if (timeSinceLastStaminaDrainingAction + timeTillRecoveryStart < Time.time)
        {
            if (currentStamina < maxStamina)
            {
                if (lastTimeStaminaWasRecovered + recoveryIntervalTime < Time.time)
                {
                    if (currentStamina < maxStamina)
                    {
                        RecoverStamina(recoveryIntervalAmount);
                        lastTimeStaminaWasRecovered = Time.time;
                    }
                }
            }
        }
    }

    public bool TryDrainStamina(float drainAmount, bool invokeMissingStaminaOnFailEvent = true)
    {
        if (drainAmount < currentStamina)
        {
            DrainStamina(drainAmount);
            return true;
        }
        if (invokeMissingStaminaOnFailEvent)
        {
            onMissingStamina?.Invoke();
        }
        return false;
    }

    private void DrainStamina(float drainAmount)
    {
        float staminaBefore = currentStamina;
        currentStamina -= drainAmount;
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

        float trueDrainAmount = staminaBefore - currentStamina;
        if (trueDrainAmount > 0f && onStaminaDrained != null)
        {
            onStaminaDrained.Invoke(trueDrainAmount);
        }
        timeSinceLastStaminaDrainingAction = Time.time;
    }

    public void RecoverStamina(float recoveryAmount)
    {
        float staminaBefore = currentStamina;
        currentStamina += recoveryAmount;
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

        float trueRecoveryAmount = currentStamina - staminaBefore;
        if (trueRecoveryAmount > 0f && onStaminaRecovered != null)
        {
            onStaminaRecovered.Invoke(trueRecoveryAmount);
        }
    }
}
