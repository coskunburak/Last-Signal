using LastSignal.Noise;
using UnityEngine;

namespace LastSignal
{
    // Value-only snapshot: no target, source object, transform or position delegate.
    public readonly struct ZombieAuditoryStimulus
    {
        public readonly GameplayNoiseEvent Event;
        public readonly Vector3 ApproachDirection;
        public readonly float Strength;
        public readonly bool Occluded;
        public ZombieAuditoryStimulus(in GameplayNoiseEvent noise, Vector3 origin, float strength, bool occluded)
        { Event = noise; ApproachDirection = (noise.Position - origin).normalized; Strength = strength; Occluded = occluded; }
    }

    public static class ZombieHearingEvaluator
    {
        public static bool Valid(in GameplayNoiseEvent e, double now) =>
            e.EventId.Epoch != 0 && e.EventId.Sequence != 0 && e.SourceId != 0 &&
            GameplayNoiseSystem.ValidPosition(e.Position) && e.Category >= GameplayNoiseCategory.Footstep && e.Category <= GameplayNoiseCategory.Gunshot &&
            new GameplayNoiseProfile(e.BaseRadiusMeters, e.Intensity, (float)(e.ExpiresAt - e.SimulationTime)).Valid &&
            e.BaseRadiusMeters > 0 && double.IsFinite(now) && double.IsFinite(e.SimulationTime) && e.SimulationTime >= 0 &&
            e.SimulationTime <= now && double.IsFinite(e.ExpiresAt) && e.ExpiresAt > now;
        public static float Strength(in GameplayNoiseEvent e, Vector3 origin, ZombieDefinition tuning, bool occluded)
        {
            if (!GameplayNoiseSystem.ValidPosition(origin) || e.BaseRadiusMeters <= 0) return 0;
            return e.Intensity * tuning.HearingSensitivity(e.Category) *
                Mathf.Clamp01(1 - Vector3.Distance(origin, e.Position) / e.BaseRadiusMeters) * (occluded ? tuning.OccludedTransmission : 1);
        }
    }

    public sealed class ZombieAuditoryMemory
    {
        public ZombieAuditoryStimulus Stimulus { get; private set; }
        public float Age { get; private set; }
        public bool HasStimulus { get; private set; }
        public float Confidence(float duration) => HasStimulus ? Stimulus.Strength * Mathf.Clamp01(1 - Age / duration) : 0;
        // R02 is an ordered synchronous stream: a high-water mark rejects all replay, in O(1) space.
        ulong epoch, highWater;
        public bool Receive(in ZombieAuditoryStimulus stimulus, float duration)
        {
            var id = stimulus.Event.EventId;
            if (id.Epoch == 0 || id.Sequence == 0 || !float.IsFinite(stimulus.Strength) || stimulus.Strength <= 0 ||
                !float.IsFinite(duration) || duration <= 0 || !GameplayNoiseSystem.ValidPosition(stimulus.Event.Position)) return false;
            if (epoch != 0 && id.Epoch != epoch || id.Sequence <= highWater) return false;
            epoch = id.Epoch; highWater = id.Sequence;
            if (HasStimulus)
            {
                float difference = stimulus.Strength - Confidence(duration);
                if (difference < -.0001f) return false;
                if (Mathf.Abs(difference) <= .0001f && (stimulus.Event.SimulationTime < Stimulus.Event.SimulationTime ||
                    stimulus.Event.SimulationTime == Stimulus.Event.SimulationTime && id.Sequence <= Stimulus.Event.EventId.Sequence)) return false;
            }
            Stimulus = stimulus; Age = 0; HasStimulus = true; return true;
        }
        public void Advance(float seconds, float duration)
        { if (!HasStimulus || !float.IsFinite(seconds) || seconds <= 0) return; Age += seconds; if (Age >= duration) HasStimulus = false; }
        // Consume keeps identity and snapshot for traceability, but cannot restart the same investigation.
        public void Consume() => HasStimulus = false;
        public void Clear() { Stimulus = default; Age = 0; HasStimulus = false; epoch = highWater = 0; }
    }
}
