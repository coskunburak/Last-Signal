using System;
using LastSignal.Noise;
using UnityEngine;

namespace LastSignal.Vehicles
{
    /// <summary>One canonical noise stream. Cadence uses elapsed simulation seconds, while the
    /// stream stamps its own session clock. Never derive gameplay radius from AudioSource volume.</summary>
    public sealed class VehicleNoiseProducer
    {
        readonly GameplayNoiseSystem noise;
        readonly ulong source;
        readonly double interval, hornInterval;
        double elapsed, hornCooldown;
        ulong lastImpact;
        readonly GameplayNoiseProfile idle, loaded, horn, impact;
        public VehicleNoiseProducer(GameplayNoiseSystem authority, ulong sourceId, VehicleTuning tuning)
        {
            if (authority == null || sourceId == 0 || tuning == null || !tuning.Valid) throw new ArgumentException("Invalid vehicle noise binding.");
            noise = authority; source = sourceId; interval = tuning.engineNoiseIntervalSimulationSeconds; hornInterval = tuning.hornIntervalSimulationSeconds;
            idle = tuning.idleNoise; loaded = tuning.loadedNoise; horn = tuning.hornNoise; impact = tuning.impactNoise;
        }
        public void Advance(double simulationSeconds, bool engineRunning, float load, Vector3 position)
        {
            if (!VehicleTuning.Nonnegative(simulationSeconds) || !float.IsFinite(load) || load < 0 || load > 1 || !GameplayNoiseSystem.ValidPosition(position))
                throw new ArgumentOutOfRangeException(nameof(simulationSeconds));
            if (!noise.CanEmit) { elapsed = 0; return; }
            hornCooldown = Math.Max(0, hornCooldown - simulationSeconds);
            if (!engineRunning) { elapsed = 0; return; }
            elapsed += simulationSeconds;
            if (elapsed + 1e-9 < interval) return;
            // At most one pulse per call after a hitch; discard missed pulses instead of a burst.
            elapsed = Math.Max(0, elapsed % interval);
            if (elapsed >= interval - 1e-9) elapsed = 0;
            var profile = new GameplayNoiseProfile(Mathf.Lerp(idle.RadiusMeters, loaded.RadiusMeters, load),
                Mathf.Lerp(idle.Intensity, loaded.Intensity, load), idle.TtlSeconds);
            noise.TryEmit(new GameplayNoiseRequest(source, position, GameplayNoiseCategory.VehicleEngine, profile), out _);
        }
        public bool Horn(Vector3 position)
        {
            if (hornCooldown > 1e-9 || !noise.TryEmit(new GameplayNoiseRequest(source, position, GameplayNoiseCategory.VehicleHorn, horn), out _)) return false;
            hornCooldown = hornInterval; return true;
        }
        public bool Impact(Vector3 position, ulong actionId, float severity = 1)
        {
            // Producer-local monotonic committed impact receipt; contact callbacks must first
            // pass VehicleImpactGate. Replaying a receipt never creates another stream event.
            if (!float.IsFinite(severity) || severity <= 0 || severity > 1 || actionId == 0 || actionId <= lastImpact) return false;
            ulong previous = lastImpact; lastImpact = actionId;
            if (noise.TryEmit(new GameplayNoiseRequest(source, position, GameplayNoiseCategory.VehicleImpact, new GameplayNoiseProfile(impact.RadiusMeters * Mathf.Lerp(.25f, 1, severity), impact.Intensity * severity, impact.TtlSeconds), actionId), out _)) return true;
            lastImpact = previous; return false;
        }
        public void Reset() { elapsed = 0; hornCooldown = 0; }
    }
}
