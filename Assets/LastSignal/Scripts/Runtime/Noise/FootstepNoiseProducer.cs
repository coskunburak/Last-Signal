using UnityEngine;
namespace LastSignal.Noise
{
    /// <summary>Called with actual motor substep displacement, never transform deltas across loads/teleports.</summary>
    public sealed class FootstepNoiseProducer
    {
        readonly GameplayNoiseSystem system;
        readonly GameplayNoiseTuning tuning;
        readonly ulong source;
        double distance;
        public FootstepNoiseProducer(GameplayNoiseSystem authority, GameplayNoiseTuning profiles, ulong sourceId)
        { system = authority; tuning = profiles; source = sourceId; }
        public void Reset() => distance = 0;
        public void Advance(Vector3 displacement, Vector3 position, bool grounded, bool sprint, bool crouch)
        {
            if (!system.CanEmit || !grounded || !GameplayNoiseSystem.ValidPosition(displacement)) { Reset(); return; }
            displacement.y = 0;
            float traveled = displacement.magnitude;
            if (traveled > 2) { Reset(); return; } // Motor substeps are <= 1/60 s; discontinuities are not walking.
            if (traveled < .00001f) return;
            distance += traveled;
            while (distance + .00001 >= tuning.StrideMeters)
            {
                distance = System.Math.Max(0, distance - tuning.StrideMeters);
                var category = sprint && !crouch ? GameplayNoiseCategory.SprintFootstep : GameplayNoiseCategory.Footstep;
                var profile = crouch ? tuning.Crouch : sprint ? tuning.Sprint : tuning.Walk;
                system.TryEmit(new GameplayNoiseRequest(source, position, category, profile), out _);
            }
        }
    }
}
