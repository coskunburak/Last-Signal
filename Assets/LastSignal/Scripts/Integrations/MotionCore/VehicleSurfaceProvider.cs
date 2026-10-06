using MotionCore.Vehicle.Core;
using UnityEngine;
using LastSignal.WorldTime;

namespace LastSignal.Vehicles.MotionCoreIntegration
{
    [DisallowMultipleComponent]
    public sealed class VehicleSurfaceProvider : MonoBehaviour, IVehicleSurfaceProvider
    {
        VehicleActor actor;
        WorldClock clock;
        void Awake() => actor = GetComponent<VehicleActor>();
        public SurfaceProperties GetSurface(WheelCollider wheel, in WheelHit hit)
        {
            if (!clock && actor && actor.Owner) clock = actor.Owner.GetComponent<WorldClock>();
            float wet = clock && clock.Simulation != null ? (float)clock.Simulation.RainPresentation : 0;
            var surface = hit.collider ? hit.collider.GetComponentInParent<VehicleSurface>() : null;
            return new SurfaceProperties {
                forwardGripMultiplier = (surface ? surface.ForwardGrip : 1) * Mathf.Lerp(1, .9f, wet),
                sidewaysGripMultiplier = (surface ? surface.SidewaysGrip : 1) * Mathf.Lerp(1, .85f, wet)
            };
        }
    }
}
