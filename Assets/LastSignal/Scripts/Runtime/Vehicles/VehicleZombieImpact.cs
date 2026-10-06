using System;
using UnityEngine;

namespace LastSignal.Vehicles
{
    [Serializable]
    public sealed class VehicleZombieImpactTuning
    {
        public float minimumSpeed = 2.5f, fullSeveritySpeed = 11.5f;
        public float maximumDamage = 140f, maximumConditionCost = .035f;
        public bool Valid => VehicleTuning.Positive(minimumSpeed) && fullSeveritySpeed > minimumSpeed &&
            float.IsFinite(fullSeveritySpeed) && VehicleTuning.Positive(maximumDamage) &&
            VehicleTuning.Positive(maximumConditionCost) && maximumConditionCost <= .1f;
        public VehicleZombieImpactTuning Copy() => (VehicleZombieImpactTuning)MemberwiseClone();
        public float Severity(float closingSpeed) => !float.IsFinite(closingSpeed) ? 0 :
            Mathf.InverseLerp(minimumSpeed, fullSeveritySpeed, closingSpeed);
        public float Damage(float severity) => maximumDamage * severity;
        public float Cost(float severity) => maximumConditionCost * severity;
        public static Vector3 RelativePointMotion(Vector3 linear, Vector3 angular, Vector3 center,
            Vector3 point, Vector3 targetMotion) => linear + Vector3.Cross(angular, point - center) - targetMotion;
        // Contact normal points out of the other surface toward the receiving chassis.
        public static float ClosingSpeed(Vector3 relativeMotion, Vector3 normal) =>
            Mathf.Max(0, Vector3.Dot(relativeMotion, -normal.normalized));
    }

    public readonly struct VehicleZombieImpactReceipt
    {
        public readonly ulong Transaction, Target;
        public readonly Vector3 Position, Direction;
        public readonly float ClosingSpeed, Severity, Damage;
        public readonly double ConditionCost;
        public VehicleZombieImpactReceipt(ulong transaction, ulong target, Vector3 position, Vector3 direction,
            float speed, float severity, float damage, double cost)
        { Transaction = transaction; Target = target; Position = position; Direction = direction;
            ClosingSpeed = speed; Severity = severity; Damage = damage; ConditionCost = cost; }
    }
}
