using MotionCore.Vehicle.Core;
using Unity.Profiling;
using UnityEngine;

namespace LastSignal.Vehicles.MotionCoreIntegration
{
    /// <summary>Only this assembly knows MotionCore. Owns input translation and the engine-off torque guard.
    /// Do not attach the vendor VehicleInput or a second VehicleControllerBase to this object.</summary>
    [DisallowMultipleComponent]
    public sealed class MotionCoreVehiclePhysicsAdapter : VehicleControllerBase, IVehiclePhysicsPort, IVehiclePresentationState
    {
        static readonly ProfilerMarker PhysicsMarker = new ProfilerMarker("LastSignal.Vehicle.Physics");
        static readonly ProfilerMarker VisualMarker = new ProfilerMarker("LastSignal.Vehicle.Wheels");
        [SerializeField, Min(1)] float serviceBrakeTorque = 3200;
        VehicleControlIntent intent = VehicleControlIntent.Parked;
        bool propulsion;
        public float WheelSpeed01 { get; private set; }
        public float DriveLoad => propulsion ? Mathf.Max(intent.Throttle, intent.Reverse) : 0;
        public bool BrakeApplied => intent.ServiceBrake > .01f || intent.Handbrake ||
            (propulsion && intent.Reverse > .01f && ForwardSpeed > .5f);
        public bool ReverseEngaged => propulsion && intent.Reverse > .01f && intent.ServiceBrake <= .01f && ForwardSpeed < .5f;
        public float SpeedMetersPerSecond => SpeedKph / 3.6f;
        public void SetControl(VehicleControlIntent value, bool propulsionEnabled)
        { intent = value; propulsion = propulsionEnabled; }
        public void ResetTransientInput()
        {
            intent = VehicleControlIntent.Parked; propulsion = false;
            SetInput(new CarInput { Handbrake = true });
            StopTorque(true);
        }
        protected override void Awake()
        {
            base.Awake();
            ResetTransientInput();
        }
        protected override void FixedUpdate()
        {
            using (PhysicsMarker.Auto())
            {
                // Vendor Brake switches from braking to reverse below .5 m/s. Only explicit
                // reverse intent reaches it; service braking always wins over either drive input.
                bool drive = propulsion && intent.ServiceBrake <= .01f;
                SetInput(new CarInput { Throttle = drive && intent.Reverse <= .01f ? intent.Throttle : 0,
                    Brake = drive ? intent.Reverse : 0, Steer = intent.Steering, Handbrake = intent.Handbrake });
                base.FixedUpdate();
                if (!drive) StopTorque(!propulsion && intent.Handbrake);
                float rpm = 0; int count = 0;
                for (int i = 0; i < Axles.Length; i++)
                {
                    if (Axles[i].leftWheel) { rpm += Mathf.Abs(Axles[i].leftWheel.rpm); count++; }
                    if (Axles[i].rightWheel) { rpm += Mathf.Abs(Axles[i].rightWheel.rpm); count++; }
                }
                WheelSpeed01 = count > 0 ? Mathf.Clamp01(rpm / (count * 340f)) : 0;
            }
        }
        protected override void Update()
        {
            // The supported MotionCore GetWorldPose path is the sole wheel visual writer.
            using (VisualMarker.Auto()) base.Update();
        }
        public WheelCollider Wheel(int index) => index >= 0 && Axles != null && index < Axles.Length * 2 ?
            (index % 2 == 0 ? Axles[index / 2].leftWheel : Axles[index / 2].rightWheel) : null;
        public Transform WheelVisual(int index) => index >= 0 && Axles != null && index < Axles.Length * 2 ?
            (index % 2 == 0 ? Axles[index / 2].leftVisual : Axles[index / 2].rightVisual) : null;
        public bool ValidateWheelPresentation(out string reason)
        {
            reason = null;
            if (Axles == null || Axles.Length != 2 || Axles[0] == null || Axles[1] == null)
                { reason = "Exactly two axles required."; return false; }
            if (!Axles[0].steering || Axles[1].steering) { reason = "Only front axle must steer."; return false; }
            for (int i = 0; i < 4; i++)
            {
                var wheel = Wheel(i); var visual = WheelVisual(i);
                if (!wheel || !visual || !float.IsFinite(wheel.radius) || wheel.radius <= 0 || wheel.suspensionDistance <= 0)
                    { reason = "Missing or invalid wheel binding " + i; return false; }
                if (!wheel.transform.IsChildOf(transform) || !visual.IsChildOf(transform) ||
                    (wheel.transform.lossyScale - Vector3.one).sqrMagnitude > .0001f)
                    { reason = "Wheel must belong to the unscaled physics root."; return false; }
                for (int j = 0; j < i; j++)
                    if (Wheel(j) == wheel || WheelVisual(j) == visual)
                        { reason = "Duplicate wheel/visual binding."; return false; }
            }
            return true;
        }
        void StopTorque(bool park)
        {
            if (Axles == null) return;
            float brake = park ? serviceBrakeTorque : intent.ServiceBrake * serviceBrakeTorque;
            for (int i = 0; i < Axles.Length; i++)
            {
                var axle = Axles[i];
                if (axle == null) continue;
                Brake(axle.leftWheel, brake); Brake(axle.rightWheel, brake);
            }
        }
        static void Brake(WheelCollider wheel, float torque)
        {
            if (!wheel) return;
            wheel.motorTorque = 0;
            wheel.brakeTorque = Mathf.Max(wheel.brakeTorque, torque);
        }
        void OnDisable() => ResetTransientInput();
        void OnApplicationFocus(bool focused) { if (!focused) ResetTransientInput(); }
        void OnApplicationPause(bool paused) { if (paused) ResetTransientInput(); }
    }
}
