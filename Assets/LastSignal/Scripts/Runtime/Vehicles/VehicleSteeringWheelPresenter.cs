using Unity.Profiling;
using UnityEngine;

namespace LastSignal.Vehicles
{
    [DisallowMultipleComponent]
    public sealed class VehicleSteeringWheelPresenter : MonoBehaviour
    {
        static readonly ProfilerMarker Marker = new ProfilerMarker("LastSignal.Vehicle.SteeringVisual");
        [SerializeField] Transform pivot;
        [SerializeField] WheelCollider frontLeft, frontRight;
        [SerializeField] Vector3 localAxis = new Vector3(0, .331f, -.9436f);
        [SerializeField, Min(1)] float roadWheelFullLock = 32;
        [SerializeField, Range(1, 540)] float visualFullLock = 270;
        Quaternion centered;
        public Transform Pivot => pivot;
        public float VisualAngle { get; private set; }
        public bool Valid => pivot && frontLeft && frontRight && frontLeft != frontRight &&
            float.IsFinite(roadWheelFullLock) && roadWheelFullLock > 0 &&
            float.IsFinite(visualFullLock) && visualFullLock > 0 && visualFullLock <= 540 &&
            float.IsFinite(localAxis.sqrMagnitude) && localAxis.sqrMagnitude > .01f;
        public static float EvaluateAngle(float left, float right, float roadLock, float visualLock)
        {
            if (!float.IsFinite(left) || !float.IsFinite(right) || !float.IsFinite(roadLock) || roadLock <= 0 ||
                !float.IsFinite(visualLock) || visualLock <= 0) return 0;
            return Mathf.Clamp((left + right) * .5f / roadLock, -1, 1) * visualLock;
        }
        void Awake()
        {
            if (!Valid) { Debug.LogError("Invalid cockpit steering-wheel binding.", this); enabled = false; return; }
            centered = pivot.localRotation;
        }
        void LateUpdate()
        {
            using (Marker.Auto())
            {
                // MotionCore already rate-limits road-wheel steer. No second smoothing/lag.
                VisualAngle = EvaluateAngle(frontLeft.steerAngle, frontRight.steerAngle, roadWheelFullLock, visualFullLock);
                pivot.localRotation = centered * Quaternion.AngleAxis(VisualAngle, localAxis.normalized);
            }
        }
    }
}
