using System;
namespace LastSignal
{
    public enum MeleeState { Ready, Windup, Active, Recovery }
    /// <summary>Animation-independent timeline; crossed boundaries are processed even on a slow frame.</summary>
    public sealed class MeleeAttackState
    {
        static ulong nextSwing;
        readonly MeleeWeaponDefinition definition;
        float remaining;
        public MeleeState State { get; private set; }
        // Read-only timeline for animation sampling, never an animation completion authority.
        public float PresentationProgress
        {
            get
            {
                if (!definition || State == MeleeState.Ready) return 0;
                float total = definition.WindupSeconds + definition.ActiveSeconds + definition.RecoverySeconds;
                float after = State == MeleeState.Windup ? definition.ActiveSeconds + definition.RecoverySeconds :
                    State == MeleeState.Active ? definition.RecoverySeconds : 0;
                return UnityEngine.Mathf.Clamp01(1 - (remaining + after) / total);
            }
        }
        public ulong SwingId { get; private set; }
        public event Action<MeleeState> Changed;
        public event Action ActiveWindow;
        public MeleeAttackState(MeleeWeaponDefinition value) { definition = value; }
        public bool TryBegin(PlayerStamina stamina)
        {
            if (State != MeleeState.Ready || !definition || !definition.Valid || !stamina || !stamina.TrySpend(PlayerStamina.MeleeAttackCost)) return false;
            SwingId = ++nextSwing;
            Enter(MeleeState.Windup, definition.WindupSeconds);
            return true;
        }
        public void Tick(float seconds)
        {
            if (!float.IsFinite(seconds) || seconds <= 0 || State == MeleeState.Ready) return;
            // At most three boundaries. Listeners can cancel synchronously.
            for (int i = 0; i < 3 && State != MeleeState.Ready; i++)
            {
                if (seconds < remaining) { remaining -= seconds; return; }
                seconds -= remaining;
                if (State == MeleeState.Windup)
                {
                    Enter(MeleeState.Active, definition.ActiveSeconds);
                    if (State == MeleeState.Active) ActiveWindow?.Invoke();
                }
                else if (State == MeleeState.Active) Enter(MeleeState.Recovery, definition.RecoverySeconds);
                else Enter(MeleeState.Ready, 0);
            }
        }
        public void Cancel() { if (State != MeleeState.Ready) Enter(MeleeState.Ready, 0); }
        void Enter(MeleeState state, float duration) { State = state; remaining = duration; Changed?.Invoke(state); }
    }
}
