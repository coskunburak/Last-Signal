using System;
using UnityEngine;

namespace LastSignal
{
    public enum ZombieState { Idle, Chasing, Searching, AttackWindup, AttackCommit, Recovering }

    // Only ZombieController writes this per-actor domain state. No Transform reference is retained.
    public sealed class ZombieRuntimeState
    {
        public ZombieState State { get; private set; }
        public float Confidence { get; private set; }
        public bool Visible { get; private set; }
        public bool HasMemory { get; private set; }
        public Vector3 LastKnownPosition { get; private set; }
        public Vector3 LastSeenDirection { get; private set; }
        public float MemoryAge { get; private set; }
        public float SearchAge { get; private set; }
        public int Transitions { get; private set; }

        public void Observe(bool visible, Vector3 observedPosition, Vector3 observedDirection, float seconds, ZombieDefinition tuning)
        {
            Visible = visible;
            Confidence = Mathf.Clamp01(Confidence + (visible ? seconds / tuning.AcquisitionTime : -seconds / tuning.ConfidenceDecayTime));
            if (!visible) return; // Hidden samples cannot change remembered information.
            LastKnownPosition = observedPosition;
            LastSeenDirection = observedDirection.sqrMagnitude > .001f ? observedDirection.normalized : Vector3.forward;
            HasMemory = true; MemoryAge = 0;
        }

        public void Advance(float seconds)
        {
            if (seconds <= 0) return;
            if (HasMemory) MemoryAge += seconds;
            if (State == ZombieState.Searching) SearchAge += seconds;
        }

        public static bool IsLegal(ZombieState from, ZombieState to) =>
            from == ZombieState.Idle && to == ZombieState.Chasing ||
            from == ZombieState.Chasing && (to == ZombieState.Searching || to == ZombieState.AttackWindup) ||
            from == ZombieState.Searching && (to == ZombieState.Chasing || to == ZombieState.Idle) ||
            from == ZombieState.AttackWindup && (to == ZombieState.AttackCommit || to == ZombieState.Chasing || to == ZombieState.Searching) ||
            from == ZombieState.AttackCommit && to == ZombieState.Recovering ||
            from == ZombieState.Recovering && (to == ZombieState.Chasing || to == ZombieState.Searching);

        public void Transition(ZombieState next)
        {
            if (!IsLegal(State, next)) throw new InvalidOperationException("Illegal zombie state transition: " + State + " -> " + next);
            Exit(State); State = next; Transitions++; Enter(next);
        }
        void Exit(ZombieState previous) { if (previous == ZombieState.Searching) SearchAge = 0; }
        void Enter(ZombieState next)
        {
            if (next == ZombieState.Searching) SearchAge = 0;
            if (next == ZombieState.Idle) ClearKnowledge();
        }
        void ClearKnowledge()
        { Confidence = 0; Visible = false; HasMemory = false; LastKnownPosition = LastSeenDirection = Vector3.zero; MemoryAge = SearchAge = 0; }
        public void Reset() { State = ZombieState.Idle; Transitions = 0; ClearKnowledge(); }
    }

    public sealed class ZombieDestinationPolicy
    {
        bool hasDestination;
        Vector3 last;
        float nextTime;
        public bool ShouldRefresh(Vector3 destination, float time, float interval, float distance)
        {
            if (hasDestination && (time < nextTime || (destination - last).sqrMagnitude < distance * distance)) return false;
            hasDestination = true; last = destination; nextTime = time + interval; return true;
        }
        public void Reset() { hasDestination = false; nextTime = 0; }
    }
}
