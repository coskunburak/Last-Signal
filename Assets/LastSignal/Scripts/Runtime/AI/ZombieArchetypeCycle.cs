using UnityEngine;
namespace LastSignal
{
    public enum ZombieArchetypePhase { Normal, Dormant, Suspicious, Preparation, Burst, Recovery }
    // Pure bounded policy. Advance receives scaled simulation seconds from the shared controller.
    // At most one transition per tick: a slow frame cannot consume a newly emitted warning.
    public sealed class ZombieArchetypeCycle
    {
        public ZombieArchetypePhase Phase { get; private set; }
        public float Remaining { get; private set; }
        public float Cooldown { get; private set; }
        public bool Hold => Phase == ZombieArchetypePhase.Dormant || Phase == ZombieArchetypePhase.Suspicious ||
            Phase == ZombieArchetypePhase.Preparation || Phase == ZombieArchetypePhase.Recovery;
        public bool CanAttack => Phase == ZombieArchetypePhase.Normal || Phase == ZombieArchetypePhase.Burst;
        public void Reset(bool ambush) { Phase = ambush ? ZombieArchetypePhase.Dormant : ZombieArchetypePhase.Normal; Remaining = Cooldown = 0; }
        public void Interrupt(ZombieDefinition d)
        { if (d.ArchetypeId == ZombieArchetypes.Shambler) return; Phase = ZombieArchetypePhase.Recovery; Remaining = d.RecoverySeconds; Cooldown = d.BurstCooldown; }
        public void Advance(ZombieDefinition d, float seconds, bool visible, bool acquired, bool pursuing, bool heard, float distance)
        {
            if (!float.IsFinite(seconds) || seconds <= 0 || d.ArchetypeId == ZombieArchetypes.Shambler) return;
            Cooldown = Mathf.Max(0, Cooldown - seconds);
            Remaining = Mathf.Max(0, Remaining - seconds);
            switch (Phase)
            {
                case ZombieArchetypePhase.Dormant:
                    if (visible || heard) Phase = ZombieArchetypePhase.Suspicious;
                    break;
                case ZombieArchetypePhase.Suspicious:
                    if (visible && acquired && distance <= d.AmbushRange) Prepare(d);
                    else if (!visible && !heard) Phase = ZombieArchetypePhase.Dormant;
                    else if (heard && !visible) Phase = ZombieArchetypePhase.Normal; // Investigate the heard position only.
                    break;
                case ZombieArchetypePhase.Normal:
                    if (visible && acquired && pursuing && Cooldown <= 0 && distance > d.AttackEnterRange) Prepare(d);
                    break;
                case ZombieArchetypePhase.Preparation:
                    if (!visible || !pursuing) Interrupt(d);
                    else if (Remaining <= 0) { Phase = ZombieArchetypePhase.Burst; Remaining = d.BurstSeconds; }
                    break;
                case ZombieArchetypePhase.Burst:
                    if (!visible || !pursuing || Remaining <= 0) Interrupt(d);
                    break;
                case ZombieArchetypePhase.Recovery:
                    if (Remaining <= 0) Phase = ZombieArchetypePhase.Normal;
                    break;
            }
        }
        void Prepare(ZombieDefinition d) { Phase = ZombieArchetypePhase.Preparation; Remaining = d.PreparationSeconds; }
    }
}
