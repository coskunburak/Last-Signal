using Unity.Profiling;
using UnityEngine;

namespace LastSignal
{
    public enum MeleeResult { None, Hit, OutOfRange, OutsideArc, Occluded, TargetDead, InvalidTarget, InvalidWorld, Aborted }

    public static class ZombieMeleeValidator
    {
        static readonly ProfilerMarker Marker = new ProfilerMarker("LastSignal.Zombie.MeleeValidation");
        public static MeleeResult Validate(Vector3 origin, Vector3 forward, CharacterController capsule,
            PlayerHealth health, float reach, float halfAngle, int worldMask, out Vector3 point,
            out float distance, out float angle)
        {
            using (Marker.Auto())
            {
                point = origin; distance = angle = 0;
                if (!capsule || !capsule.enabled || !capsule.gameObject.activeInHierarchy || !health || !health.isActiveAndEnabled)
                    return MeleeResult.InvalidTarget;
                if (!health.IsAlive) return MeleeResult.TargetDead;
                point = capsule.ClosestPoint(origin);
                Vector3 offset = point - origin;
                distance = offset.magnitude;
                if (distance > reach) return MeleeResult.OutOfRange;
                Vector3 planar = capsule.transform.TransformPoint(capsule.center) - origin; planar.y = 0;
                forward.y = 0;
                angle = Vector3.Angle(forward, planar);
                if (planar.sqrMagnitude < .0001f || angle > halfAngle) return MeleeResult.OutsideArc;
                // Raycasts alone miss a wall when the ray starts inside it.
                if (Physics.CheckSphere(origin, .015f, worldMask, QueryTriggerInteraction.Ignore) ||
                    Physics.Linecast(origin, point, worldMask, QueryTriggerInteraction.Ignore)) return MeleeResult.Occluded;
                return MeleeResult.Hit;
            }
        }
    }
}
