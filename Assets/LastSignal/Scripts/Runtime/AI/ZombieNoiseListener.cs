using LastSignal.Noise;
using Unity.Profiling;
using UnityEngine;
namespace LastSignal
{
    [DisallowMultipleComponent, RequireComponent(typeof(ZombieController))]
    public sealed class ZombieNoiseListener : MonoBehaviour, IGameplayNoiseListener
    {
        static readonly ProfilerMarker Marker = new ProfilerMarker("LastSignal.Zombie.Hearing");
        ZombieController controller;
        GameplayNoiseContext context;
        GameplayNoiseSystem authority;
        ulong highWater;
        bool registered;
        static long nextId;
        public ulong ListenerId { get; } = (1UL << 63) | (ulong)System.Threading.Interlocked.Increment(ref nextId);
        public int Candidates { get; private set; }
        public int PhysicsQueries { get; private set; }
        public int Heard { get; private set; }
        public int Accepted { get; private set; }
        public ZombieState LastStateBefore { get; private set; }
        public ZombieState LastStateAfter { get; private set; }
        public bool Registered => registered && authority != null && authority.Active;
        public ZombieAuditoryStimulus LastHeard { get; private set; }
        public bool Bind(GameplayNoiseContext value)
        {
            Release(); controller = GetComponent<ZombieController>(); context = value;
            authority = context ? context.System : null;
            if (authority == null || !authority.Active) { Debug.LogError("Zombie hearing requires the active session GameplayNoiseSystem.", this); return false; }
            // Existing world-only occlusion mask must exclude every owned collider, including hit regions.
            foreach (var collider in GetComponentsInChildren<Collider>(true))
                if ((controller.Definition.OcclusionMask & (1 << collider.gameObject.layer)) != 0)
                { Debug.LogError("Zombie hearing world mask includes an owned collider layer.", this); return false; }
            return Register();
        }
        bool Register()
        {
            if (registered) return true;
            if (!isActiveAndEnabled || !controller || !controller.CanHear || authority == null || !authority.Active) return false;
            registered = authority.Register(this, controller.Perception.Origin);
            if (!registered) Debug.LogError("Zombie hearing registration failed.", this);
            return registered;
        }
        public void Release()
        { authority?.Unregister(this); registered = false; authority = null; context = null; highWater = 0; LastHeard = default; }
        void OnEnable() { if (authority != null) Register(); }
        void OnDisable() { authority?.Unregister(this); registered = false; }
        void OnDestroy() => Release();
        void LateUpdate()
        { if (Registered && controller.CanHear) authority.UpdatePosition(this, controller.Perception.Origin); }
        public void ReceiveNoise(in GameplayNoiseEvent noise)
        {
            if (!Registered || !isActiveAndEnabled || !controller.CanHear || controller.Paused || !authority.CanEmit ||
                !context || noise.EventId.Epoch != authority.Epoch || noise.EventId.Sequence <= highWater ||
                !ZombieHearingEvaluator.Valid(noise, context.SimulationTime)) return;
            highWater = noise.EventId.Sequence;
            using (Marker.Auto())
            {
                Candidates++;
                var tuning = controller.Definition; var origin = controller.Perception.Origin;
                float clear = ZombieHearingEvaluator.Strength(noise, origin, tuning, false);
                if (clear < tuning.HearingThreshold) return;
                var offset = noise.Position - origin;
                PhysicsQueries++;
                bool blocked = Physics.Raycast(origin, offset.normalized, offset.magnitude, tuning.OcclusionMask, QueryTriggerInteraction.Ignore);
                float strength = clear * (blocked ? tuning.OccludedTransmission : 1);
                if (strength < tuning.HearingThreshold) return;
                LastHeard = new ZombieAuditoryStimulus(noise, origin, strength, blocked); Heard++;
                LastStateBefore = controller.Runtime.State;
                if (controller.ReceiveAuditoryStimulus(LastHeard)) Accepted++;
                LastStateAfter = controller.Runtime.State;
            }
        }
    }
}
