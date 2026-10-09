using UnityEngine;

namespace LastSignal
{
    /// <summary>One simulation-seconds resource shared by locomotion and combat.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerStamina : MonoBehaviour
    {
        public const float Maximum = 100, SprintDrainRate = 18, MeleeAttackCost = 25;
        public const float RegenDelay = 1.25f, RegenRate = 22, ResumeThreshold = 25;
        public float CurrentStamina { get; private set; } = Maximum;
        public float MaxStamina => Maximum;
        public bool Exhausted { get; private set; }
        public bool CanSprint => !Exhausted && CurrentStamina > 0;
        public float RegenDelayRemaining { get; private set; }
        float recoveryMultiplier = 1;
        public void SetRecoveryMultiplier(float value) => recoveryMultiplier = float.IsFinite(value) ? Mathf.Clamp(value, .1f, 1) : 1;
        public bool CanSpend(float amount) => float.IsFinite(amount) && amount >= 0 && CurrentStamina >= amount;
        public bool TrySpend(float amount)
        {
            if (!CanSpend(amount)) return false;
            if (amount == 0) return true;
            CurrentStamina = Mathf.Max(0, CurrentStamina - amount);
            RegenDelayRemaining = RegenDelay;
            if (CurrentStamina == 0) Exhausted = true;
            return true;
        }
        // Called once by the motor, including standing frames. No independent Update clock.
        public void Tick(float seconds, bool sprinting)
        {
            if (!float.IsFinite(seconds) || seconds <= 0) return;
            if (sprinting && CanSprint)
            {
                TrySpend(Mathf.Min(CurrentStamina, SprintDrainRate * seconds));
                return;
            }
            float delayed = Mathf.Min(seconds, RegenDelayRemaining);
            RegenDelayRemaining -= delayed;
            CurrentStamina = Mathf.Min(Maximum, CurrentStamina + RegenRate * recoveryMultiplier * (seconds - delayed));
            if (CurrentStamina >= ResumeThreshold) Exhausted = false;
        }
        public void Restore(float value, bool exhausted = false, float delay = RegenDelay)
        {
            CurrentStamina = float.IsFinite(value) ? Mathf.Clamp(value, 0, Maximum) : Maximum;
            Exhausted = CurrentStamina == 0 || (exhausted && CurrentStamina < ResumeThreshold);
            RegenDelayRemaining = float.IsFinite(delay) ? Mathf.Clamp(delay, 0, RegenDelay) : RegenDelay;
        }
        public void ResetSession() => Restore(Maximum, false, 0);
    }
}
