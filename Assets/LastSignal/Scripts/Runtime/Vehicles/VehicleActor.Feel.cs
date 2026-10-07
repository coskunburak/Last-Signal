using Unity.Profiling;
using UnityEngine;

namespace LastSignal.Vehicles
{
    public enum VehicleBodyCue { Impact, FrontAxle, RearAxle }

    public sealed partial class VehicleActor
    {
        static readonly ProfilerMarker FeelMarker = new ProfilerMarker("LastSignal.Vehicle.BodyFeel");
        struct BodyPass
        {
            public ZombieHealth Health;
            public ZombieController Controller;
            public Vector3 Previous;
            public float Remaining, Strength;
            public byte Axles;
        }
        // Transient presentation only: no collider, force, damage, condition or noise writes.
        readonly BodyPass[] bodyPasses = new BodyPass[8];
        WheelCollider[] feelWheels;
        Vector3 feelVelocity, feelAngularVelocity;
        public Vector3 BodyFeelPosition { get; private set; }
        public Vector3 BodyFeelAngles { get; private set; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public int BodyImpactCueCount { get; private set; }
        public int FrontAxleCueCount { get; private set; }
        public int RearAxleCueCount { get; private set; }
#endif
        public int PendingBodyPasses { get; private set; }
        public event System.Action<VehicleBodyCue, float> BodyFeelPresented;

        void BeginBodyFeel(ZombieHealth health, in VehicleZombieImpactReceipt receipt)
        {
            float strength = Mathf.Lerp(.3f, 1, receipt.Severity) * (health.IsAlive ? .8f : 1);
            EmitBodyFeel(VehicleBodyCue.Impact, strength, transform.InverseTransformPoint(receipt.Position).x);
            if (feelWheels == null || feelWheels.Length == 0) return;
            var controller = health.GetComponent<ZombieController>();
            if (!controller || health.IsAlive && !controller.VehicleReactionActive) return;
            // A later semantic hit on the same body replaces, rather than stacks, traversal.
            int free = -1;
            for (int i = 0; i < bodyPasses.Length; i++)
            {
                if (bodyPasses[i].Health == health) return;
                if (bodyPasses[i].Remaining == 0 && free < 0) free = i;
            }
            if (free < 0) return;
            bodyPasses[free] = new BodyPass { Health = health, Controller = controller,
                Previous = transform.InverseTransformPoint(health.transform.position), Remaining = 4, Strength = strength };
            PendingBodyPasses++;
        }
        void EmitBodyFeel(VehicleBodyCue cue, float strength, float side)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (cue == VehicleBodyCue.Impact) BodyImpactCueCount++;
            else if (cue == VehicleBodyCue.FrontAxle) FrontAxleCueCount++;
            else RearAxleCueCount++;
#endif
            Vector3 offset = cue == VehicleBodyCue.Impact ? new Vector3(0, -.008f, .018f)
                : new Vector3(0, -.025f, -.004f);
            Vector3 angles = new Vector3(cue == VehicleBodyCue.RearAxle ? -.35f : .55f, 0, Mathf.Clamp(side, -1, 1) * .2f);
            BodyFeelPosition = Vector3.ClampMagnitude(BodyFeelPosition + offset * strength, .04f);
            BodyFeelAngles = Vector3.ClampMagnitude(BodyFeelAngles + angles * strength, 1.2f);
            BodyFeelPresented?.Invoke(cue, strength);
        }
        void AdvanceBodyFeel(float dt)
        {
            using (FeelMarker.Auto())
            {
                if (!Ready) { ClearBodyFeel(); return; }
                feelVelocity += (-120 * BodyFeelPosition - 16 * feelVelocity) * dt;
                feelAngularVelocity += (-120 * BodyFeelAngles - 16 * feelAngularVelocity) * dt;
                BodyFeelPosition = Vector3.ClampMagnitude(BodyFeelPosition + feelVelocity * dt, .04f);
                BodyFeelAngles = Vector3.ClampMagnitude(BodyFeelAngles + feelAngularVelocity * dt, 1.2f);
                for (int i = 0; i < bodyPasses.Length; i++)
                {
                    ref var pass = ref bodyPasses[i];
                    if (pass.Remaining == 0) continue;
                    if (!pass.Health || !pass.Health.isActiveAndEnabled || !pass.Controller ||
                        pass.Health.IsAlive && !pass.Controller.VehicleReactionActive || (pass.Remaining -= dt) <= 0)
                    { ReleaseBodyPass(i); continue; }
                    var point = transform.InverseTransformPoint(pass.Health.transform.position);
                    if (Mathf.Abs(point.y) > 1.5f || (point - pass.Previous).sqrMagnitude > 25)
                    { ReleaseBodyPass(i); continue; }
                    foreach (var wheel in feelWheels)
                    {
                        if (!wheel || !wheel.enabled || !wheel.isGrounded) continue;
                        var center = transform.InverseTransformPoint(wheel.transform.TransformPoint(wheel.center));
                        byte axle = (byte)(center.z > 0 ? 1 : 2);
                        if ((pass.Axles & axle) != 0) continue;
                        // Sweep a soft body footprint across the axle/undercarriage span. No delayed
                        // cue if we brake before reaching it, steer away, or merely glance it.
                        float dz = point.z - pass.Previous.z;
                        if (Mathf.Abs(dz) < .0001f || !body || body.linearVelocity.sqrMagnitude < .25f) continue;
                        float t = (center.z - pass.Previous.z) / dz;
                        if (t < 0 || t > 1 || Mathf.Abs(Mathf.Lerp(pass.Previous.x, point.x, t)) > Mathf.Abs(center.x) + .45f) continue;
                        pass.Axles |= axle;
                        EmitBodyFeel(axle == 1 ? VehicleBodyCue.FrontAxle : VehicleBodyCue.RearAxle, pass.Strength * .8f, point.x);
                    }
                    pass.Previous = point;
                    if (pass.Axles == 3) ReleaseBodyPass(i);
                }
            }
        }
        void ReleaseBodyPass(int index) { bodyPasses[index] = default; PendingBodyPasses--; }
        void ClearBodyFeel()
        {
            System.Array.Clear(bodyPasses, 0, bodyPasses.Length); PendingBodyPasses = 0;
            BodyFeelPosition = BodyFeelAngles = feelVelocity = feelAngularVelocity = Vector3.zero;
        }
    }
}
