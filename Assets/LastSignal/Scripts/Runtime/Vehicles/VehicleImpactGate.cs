using System;

namespace LastSignal.Vehicles
{
    /// <summary>Bounded transient contact ledger. Runtime collider identity is NOT a save identity.
    /// Begin/End must pair per collider; compound contacts share their semantic target ID.</summary>
    public sealed class VehicleImpactGate
    {
        struct Contact { public ulong Target; public int Count; public double NextAllowed; }
        readonly Contact[] contacts;
        readonly double cooldownSeconds;
        public VehicleImpactGate(int capacity = 64, double cooldownSimulationSeconds = 1)
        {
            if (capacity < 1 || capacity > 1024 || !VehicleTuning.Positive(cooldownSimulationSeconds)) throw new ArgumentOutOfRangeException();
            contacts = new Contact[capacity]; cooldownSeconds = cooldownSimulationSeconds;
        }
        public bool Begin(ulong target, double now)
        {
            if (target == 0 || !VehicleTuning.Nonnegative(now)) return false;
            int free = -1;
            for (int i = 0; i < contacts.Length; i++)
            {
                ref var c = ref contacts[i];
                if (c.Target == target)
                {
                    if (c.Count == int.MaxValue) return false;
                    bool accepted = c.Count == 0 && now >= c.NextAllowed;
                    c.Count++;
                    if (accepted) c.NextAllowed = now + cooldownSeconds;
                    return accepted;
                }
                if (free < 0 && c.Count == 0 && now >= c.NextAllowed) free = i;
            }
            if (free < 0) return false; // Saturation rejects; no eviction of live contacts.
            contacts[free] = new Contact { Target = target, Count = 1, NextAllowed = now + cooldownSeconds };
            return true;
        }
        public void End(ulong target)
        {
            for (int i = 0; i < contacts.Length; i++)
                if (contacts[i].Target == target) { if (contacts[i].Count > 0) contacts[i].Count--; return; }
        }
        public void Reset() => Array.Clear(contacts, 0, contacts.Length);
    }
}
