namespace LastSignal
{
    /// <summary>Authority receipt. Corpse receipts explicitly never claim a health commit.</summary>
    public readonly struct DamageReceipt
    {
        public readonly DamageInfo Hit;
        public readonly ZombieHealth Target;
        public readonly float AppliedDamage;
        public readonly bool Committed, Terminal, Severed, CorpseImpact;
        public readonly uint Generation;
        public readonly ZombieWoundSurfaceResolver.Contact SurfaceContact;
        public bool PresentationAccepted => Committed || CorpseImpact;
        public ZombieImpactSeverity Severity => Severed ? ZombieImpactSeverity.Sever :
            Terminal || Hit.Amount >= 25 ? ZombieImpactSeverity.Heavy : ZombieImpactSeverity.Light;
        public DamageReceipt(DamageInfo hit, ZombieHealth target, float applied, bool terminal, bool severed)
        {
            Hit = hit; Target = target; AppliedDamage = applied; Terminal = terminal; Severed = severed;
            Committed = true; CorpseImpact = false; Generation = target ? target.LifetimeGeneration : 0; SurfaceContact = default;
        }
        internal DamageReceipt(DamageInfo hit, ZombieHealth target, ZombieWoundSurfaceResolver.Contact contact)
        {
            Hit = hit; Target = target; AppliedDamage = 0; Terminal = Severed = Committed = false; CorpseImpact = true;
            Generation = target.LifetimeGeneration; SurfaceContact = contact;
        }
    }
    public interface IDamageReceiptReceiver : IDamageable
    {
        DamageReceipt ReceiveDamage(DamageInfo info);
    }
}
