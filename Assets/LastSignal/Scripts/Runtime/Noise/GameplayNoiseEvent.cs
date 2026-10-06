using System;
using UnityEngine;

namespace LastSignal.Noise
{
    public enum GameplayNoiseCategory { Footstep, SprintFootstep, MeleeImpact, Gunshot, Generator, VehicleEngine, VehicleHorn, VehicleImpact }

    public readonly struct GameplayNoiseId : IEquatable<GameplayNoiseId>
    {
        public readonly ulong Epoch, Sequence;
        public GameplayNoiseId(ulong epoch, ulong sequence) { Epoch = epoch; Sequence = sequence; }
        public bool Equals(GameplayNoiseId other) => Epoch == other.Epoch && Sequence == other.Sequence;
        public override bool Equals(object other) => other is GameplayNoiseId id && Equals(id);
        public override int GetHashCode() => HashCode.Combine(Epoch, Sequence);
        public override string ToString() => $"{Epoch}:{Sequence}";
    }

    /// <summary>Snapshot at commit, in meters and simulation seconds. Intensity is normalized [0,1].</summary>
    public readonly struct GameplayNoiseEvent
    {
        public readonly GameplayNoiseId EventId;
        public readonly ulong SourceId, ActionId;
        public readonly Vector3 Position;
        public readonly GameplayNoiseCategory Category;
        public readonly float BaseRadiusMeters, Intensity;
        public readonly double SimulationTime, ExpiresAt;
        public GameplayNoiseEvent(GameplayNoiseId id, in GameplayNoiseRequest request, double time)
        {
            EventId = id; SourceId = request.SourceId; ActionId = request.ActionId;
            Position = request.Position; Category = request.Category;
            BaseRadiusMeters = request.Profile.RadiusMeters; Intensity = request.Profile.Intensity;
            SimulationTime = time; ExpiresAt = time + request.Profile.TtlSeconds;
        }
    }

    [Serializable]
    public struct GameplayNoiseProfile
    {
        public float RadiusMeters, Intensity, TtlSeconds;
        public GameplayNoiseProfile(float radius, float intensity, float ttl)
        { RadiusMeters = radius; Intensity = intensity; TtlSeconds = ttl; }
        public bool Valid => float.IsFinite(RadiusMeters) && RadiusMeters >= 0 && RadiusMeters <= 128 &&
            float.IsFinite(Intensity) && Intensity >= 0 && Intensity <= 1 &&
            float.IsFinite(TtlSeconds) && TtlSeconds > 0 && TtlSeconds <= 30;
    }

    /// <summary>INITIAL PRODUCTION TUNING — NOT FINAL BALANCE. One serialized owner on SessionFlow.</summary>
    [Serializable]
    public sealed class GameplayNoiseTuning
    {
        public GameplayNoiseProfile Walk = new GameplayNoiseProfile(6, .2f, 2);
        public GameplayNoiseProfile Sprint = new GameplayNoiseProfile(12, .4f, 2);
        public GameplayNoiseProfile Crouch = new GameplayNoiseProfile(3, .1f, 2);
        public GameplayNoiseProfile Melee = new GameplayNoiseProfile(10, .35f, 2);
        public GameplayNoiseProfile Gunshot = new GameplayNoiseProfile(96, 1, 3);
        public float StrideMeters = 1.6f;
        public bool Valid => Walk.Valid && Sprint.Valid && Crouch.Valid && Melee.Valid && Gunshot.Valid &&
            float.IsFinite(StrideMeters) && StrideMeters >= .5f && StrideMeters <= 3;
    }

    public readonly struct GameplayNoiseRequest
    {
        public readonly ulong SourceId, ActionId;
        public readonly Vector3 Position;
        public readonly GameplayNoiseCategory Category;
        public readonly GameplayNoiseProfile Profile;
        public GameplayNoiseRequest(ulong source, Vector3 position, GameplayNoiseCategory category,
            GameplayNoiseProfile profile, ulong actionId = 0)
        { SourceId = source; Position = position; Category = category; Profile = profile; ActionId = actionId; }
    }

    public interface IGameplayNoiseListener
    {
        ulong ListenerId { get; }
        void ReceiveNoise(in GameplayNoiseEvent noise);
    }
    public interface IGameplayNoisePressureSink { bool Forward(in GameplayNoiseEvent noise); }

    public readonly struct GameplayNoiseTrace
    {
        public readonly GameplayNoiseEvent Event;
        public readonly int CandidateCount, DeliveryCount, CellsVisited;
        public readonly bool PressureForwarded;
        public GameplayNoiseTrace(in GameplayNoiseEvent noise, int candidates, int delivered, int cells, bool pressure)
        { Event = noise; CandidateCount = candidates; DeliveryCount = delivered; CellsVisited = cells; PressureForwarded = pressure; }
        public override string ToString() => $"{Event.EventId}\tsource={Event.SourceId}\taction={Event.ActionId}\t{Event.Category}\t{Event.Position}\tradius={Event.BaseRadiusMeters}\tintensity={Event.Intensity}\ttime={Event.SimulationTime:F4}\tcandidates={CandidateCount}\tdelivered={DeliveryCount}\tcells={CellsVisited}\tpressure={PressureForwarded}";
    }
}
