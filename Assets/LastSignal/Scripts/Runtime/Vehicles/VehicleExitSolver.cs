using System;
using UnityEngine;

namespace LastSignal.Vehicles
{
    /// <summary>Ordered, allocation-free exit validation. The composition supplies cell readiness;
    /// a missing readiness policy rejects rather than assuming an unloaded world is safe.</summary>
    public static class VehicleExitSolver
    {
        public static bool TryFind(Vector3[] doorways, Vector3[] candidates, float height, float radius,
            float maximumSlope, int solidMask, Func<Vector3, bool> worldReady, out Vector3 feet)
        {
            feet = default;
            if (candidates == null || doorways == null || doorways.Length != candidates.Length || worldReady == null || !float.IsFinite(height) ||
                !float.IsFinite(radius) || radius <= 0 || height < radius * 2 || !float.IsFinite(maximumSlope) ||
                maximumSlope < 0 || maximumSlope > 60 || solidMask == 0) return false;
            for (int i = 0; i < candidates.Length; i++)
            {
                Vector3 candidate = candidates[i];
                if (!Finite(candidate) || !Finite(doorways[i]) || !worldReady(doorways[i]) || !worldReady(candidate)) continue;
                if (!Physics.Raycast(candidate + Vector3.up, Vector3.down, out var ground, 2, solidMask, QueryTriggerInteraction.Ignore) ||
                    Vector3.Angle(ground.normal, Vector3.up) > maximumSlope) continue;
                Vector3 position = ground.point + Vector3.up * .03f;
                if (!worldReady(position)) continue;
                Vector3 bottom = position + Vector3.up * radius;
                Vector3 top = position + Vector3.up * (height - radius);
                if (Physics.CheckCapsule(bottom, top, radius, solidMask, QueryTriggerInteraction.Ignore)) continue;
                // Doorway is authored outside the body collider. This segment prevents teleporting
                // through an intervening wall even if the destination capsule itself is empty.
                Vector3 target = position + Vector3.up * (height * .5f);
                Vector3 route = target - doorways[i];
                if (Physics.CheckSphere(doorways[i], radius, solidMask, QueryTriggerInteraction.Ignore) ||
                    (route.sqrMagnitude > .0001f && Physics.SphereCast(doorways[i], radius, route.normalized, out _, route.magnitude, solidMask, QueryTriggerInteraction.Ignore))) continue;
                feet = position; return true;
            }
            return false;
        }
        static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
}
