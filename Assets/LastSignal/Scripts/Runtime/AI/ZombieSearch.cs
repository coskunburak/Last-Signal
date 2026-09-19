using UnityEngine;

namespace LastSignal
{
    // Fixed bounded pattern, generated from remembered information only. No player reference.
    public sealed class ZombieSearch
    {
        Vector3 center, forward, destination;
        int pointIndex;
        float pauseRemaining;
        bool inspecting;
        public Vector3 Destination => destination;
        public int PointIndex => pointIndex;
        public bool Inspecting => inspecting;
        public void Begin(Vector3 lastKnownPosition, Vector3 lastSeenDirection)
        {
            center = destination = lastKnownPosition;
            forward = new Vector3(lastSeenDirection.x, 0, lastSeenDirection.z).normalized;
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            pointIndex = 0; pauseRemaining = 0; inspecting = false;
        }
        public void Tick(ZombieNavigation navigation, ZombieDefinition tuning, float seconds, float time)
        {
            if (!inspecting)
            {
                navigation.MoveTo(destination, time, tuning.SearchArrivalDistance);
                if (!navigation.Arrived && !navigation.Exhausted) return;
                navigation.Stop(); inspecting = true; pauseRemaining = tuning.InspectionPause;
            }
            Vector3 direction = Quaternion.Euler(0, pointIndex % 2 == 0 ? 55 : -55, 0) * forward;
            navigation.Face(direction, seconds);
            pauseRemaining -= seconds;
            if (pauseRemaining > 0 || navigation.Exhausted || pointIndex >= 3) return;
            pointIndex++;
            Vector3 offset = Quaternion.Euler(0, pointIndex == 1 ? 0 : pointIndex == 2 ? 65 : -65, 0) * forward * tuning.SearchRadius;
            if (navigation.SampleSearch(center + offset, out var sampled))
            { destination = sampled; inspecting = false; navigation.ResetPolicy(); }
            else pauseRemaining = tuning.InspectionPause;
        }
        public void Clear() { center = forward = destination = Vector3.zero; pointIndex = 0; pauseRemaining = 0; inspecting = false; }
    }
}
