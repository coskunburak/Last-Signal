using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;

namespace LastSignal
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class ZombieNavigation : MonoBehaviour
    {
        static readonly ProfilerMarker Marker = new ProfilerMarker("LastSignal.Zombie.Navigation");
        NavMeshAgent agent;
        ZombieDefinition tuning;
        NavMeshPath path;
        readonly ZombieDestinationPolicy policy = new ZombieDestinationPolicy();
        Vector3 progressOrigin;
        float progressAge, retryAt;
        int failures, stuckFailures;
        bool initialized, requested;
        public bool Ready => initialized && agent && agent.enabled && agent.isOnNavMesh;
        public bool Exhausted { get; private set; }
        public bool Stuck { get; private set; }
        public NavMeshPathStatus PathStatus { get; private set; } = NavMeshPathStatus.PathInvalid;
        public int PathRequests { get; private set; }
        public Vector3 Destination { get; private set; }
        public Vector3 Velocity => Ready ? agent.velocity : Vector3.zero;
        public bool Arrived => Ready && requested && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + .08f;
        public int AgentType => agent ? agent.agentTypeID : 0;

        public bool Initialize(ZombieDefinition definition)
        {
            tuning = definition; path = new NavMeshPath(); agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = false; agent.updateUpAxis = false; agent.autoRepath = false;
            agent.speed = tuning.Speed; agent.acceleration = tuning.Acceleration;
            agent.angularSpeed = tuning.TurnSpeed;
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (!NavMesh.SamplePosition(transform.position, out var hit, tuning.SpawnSampleRadius, filter))
            { agent.enabled = false; Debug.LogError("Zombie spawn has no matching NavMesh within bounded correction radius. Move the encounter spawn onto the baked Shambler surface.", this); return false; }
            // One small spawn correction only. Never used as chase or stuck recovery.
            if (!agent.enabled) { transform.position = hit.position; agent.enabled = true; }
            else if (!agent.isOnNavMesh) agent.Warp(hit.position);
            initialized = agent.isOnNavMesh;
            if (!initialized) { agent.enabled = false; Debug.LogError("Zombie could not bind to its Shambler NavMesh.", this); return false; }
            ResetPolicy(); Stop(); return true;
        }
        public void ResetPolicy()
        { policy.Reset(); failures = stuckFailures = 0; Exhausted = Stuck = false; retryAt = 0; progressAge = 0; progressOrigin = transform.position; }
        public void Stop()
        {
            requested = false; progressAge = 0;
            if (!Ready) return;
            agent.isStopped = true; agent.ResetPath(); agent.velocity = Vector3.zero;
            policy.Reset();
        }
        public void Suspend(bool paused)
        { if (Ready) { agent.isStopped = paused || !requested || Exhausted; if (paused) agent.velocity = Vector3.zero; } }
        public bool MoveTo(Vector3 destination, float time, float stoppingDistance)
        {
            if (!Ready || Exhausted) return false;
            agent.stoppingDistance = stoppingDistance;
            if (time < retryAt) return requested;
            bool retry = requested && PathStatus == NavMeshPathStatus.PathPartial && Arrived;
            if (!retry && !policy.ShouldRefresh(destination, time, tuning.RepathInterval, tuning.RepathDistance)) return requested;
            using (Marker.Auto())
            {
                Destination = destination; PathRequests++;
                bool calculated = NavMesh.CalculatePath(transform.position, destination,
                    new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask }, path);
                PathStatus = calculated ? path.status : NavMeshPathStatus.PathInvalid;
                if (PathStatus == NavMeshPathStatus.PathInvalid || !agent.SetPath(path))
                { Fail(time); return false; }
                requested = true; agent.isStopped = false;
                if (PathStatus == NavMeshPathStatus.PathPartial) retryAt = time + tuning.PathRetryInterval;
                else failures = 0;
                if (retry) { failures++; if (failures >= tuning.RecoveryAttempts) { Exhausted = true; Stop(); } }
            }
            return requested;
        }
        void Fail(float time)
        {
            failures++; Stop(); retryAt = time + tuning.PathRetryInterval;
            if (failures >= tuning.RecoveryAttempts) Exhausted = true;
        }
        public void Tick(float seconds, float time)
        {
            if (!Ready) { Exhausted = true; return; }
            using (Marker.Auto())
            {
                if (!requested || agent.isStopped || Arrived) { progressAge = 0; return; }
                Vector3 direction = agent.steeringTarget - transform.position; direction.y = 0;
                Face(direction, seconds);
                float alignment = direction.sqrMagnitude > .001f ? Vector3.Dot(transform.forward, direction.normalized) : 1;
                agent.speed = tuning.Speed * Mathf.Clamp01(alignment);
                if (alignment < .5f) { progressAge = 0; progressOrigin = transform.position; return; }
                progressAge += seconds;
                if (progressAge < tuning.StuckTimeout) return;
                float progress = Vector3.Distance(progressOrigin, transform.position);
                progressAge = 0; progressOrigin = transform.position;
                if (progress >= tuning.StuckProgress) { Stuck = false; stuckFailures = 0; return; }
                Stuck = true; stuckFailures++; Fail(time);
                if (stuckFailures >= tuning.RecoveryAttempts) { Exhausted = true; Stop(); } // Bounded repath after backoff, never teleport.
            }
        }
        public void Face(Vector3 direction, float seconds)
        {
            direction.y = 0;
            if (direction.sqrMagnitude > .001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), tuning.TurnSpeed * seconds);
        }
        public bool SampleSearch(Vector3 desired, out Vector3 point)
        {
            bool found = NavMesh.SamplePosition(desired, out var hit, tuning.SearchSampleRadius,
                new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask });
            point = hit.position; return found;
        }
        void OnDisable() { Stop(); initialized = false; if (agent) agent.enabled = false; }
    }
}
