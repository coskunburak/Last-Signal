using System;

namespace LastSignal.Vehicles
{
    public readonly struct VehicleControlIntent
    {
        public readonly float Throttle, ServiceBrake, Reverse, Steering;
        public readonly bool Handbrake;
        public VehicleControlIntent(float throttle, float serviceBrake, float steering, bool handbrake, float reverse = 0)
        {
            Throttle = Sanitize(throttle, 0, 1); ServiceBrake = Sanitize(serviceBrake, 0, 1); Reverse = Sanitize(reverse, 0, 1);
            Steering = Sanitize(steering, -1, 1); Handbrake = handbrake;
        }
        static float Sanitize(float n, float min, float max) => float.IsFinite(n) ? Math.Max(min, Math.Min(max, n)) : 0;
        public bool Neutral => Throttle <= .01f && ServiceBrake <= .01f && Reverse <= .01f && Math.Abs(Steering) <= .01f && !Handbrake;
        public static VehicleControlIntent Parked => new VehicleControlIntent(0, 0, 0, true);
    }

    /// <summary>Sample raw controls even while blocked. Unlock only after a neutral sample;
    /// the neutral sample itself never produces gameplay input.</summary>
    public sealed class VehicleInputGate
    {
        bool neutralRequired = true;
        public void Reset() => neutralRequired = true;
        public VehicleControlIntent Sample(VehicleControlIntent raw, bool allowed, bool actionHeld = false)
        {
            if (!allowed) { Reset(); return VehicleControlIntent.Parked; }
            if (neutralRequired)
            {
                neutralRequired = !raw.Neutral || actionHeld;
                return VehicleControlIntent.Parked;
            }
            return raw;
        }
        public bool ActionsAllowed => !neutralRequired;
    }

    public interface IVehiclePhysicsPort
    {
        float SpeedMetersPerSecond { get; }
        bool IsGrounded { get; }
        void SetControl(VehicleControlIntent intent, bool propulsionEnabled);
        void ResetTransientInput();
    }
}
