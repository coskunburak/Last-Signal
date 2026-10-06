using UnityEngine;

namespace LastSignal.Vehicles
{
    /// <summary>Authored contact surface; consumed by MotionCore's existing grip extension.</summary>
    [DisallowMultipleComponent]
    public sealed class VehicleSurface : MonoBehaviour
    {
        [SerializeField, Range(.2f, 1.2f)] float forwardGrip = .85f;
        [SerializeField, Range(.2f, 1.2f)] float sidewaysGrip = .78f;
        public float ForwardGrip => forwardGrip;
        public float SidewaysGrip => sidewaysGrip;
    }
}
