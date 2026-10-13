using UnityEngine;
namespace LastSignal
{
    // Target-owned authority: no static state survives session replacement. Misses consume no slot.
    [DisallowMultipleComponent]
    public sealed class ZombieMeleeSpacing : MonoBehaviour
    {
        float readyAt = float.NegativeInfinity;
        Object lastAttacker;
        public bool TryContact(float simulationTime, Object attacker = null)
        {
            if (!float.IsFinite(simulationTime) || simulationTime < readyAt && (!attacker || attacker != lastAttacker)) return false;
            lastAttacker = attacker; readyAt = simulationTime + .28f; return true;
        }
        void OnDisable() { readyAt = float.NegativeInfinity; lastAttacker = null; }
    }
}
