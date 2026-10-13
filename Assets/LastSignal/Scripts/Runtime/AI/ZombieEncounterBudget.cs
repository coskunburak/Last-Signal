using UnityEngine;
namespace LastSignal
{
    // Authoring/debug seam; no player-power scaling and no hidden spawning authority.
    public sealed class ZombieEncounterBudget : MonoBehaviour
    {
        public ZombieEncounterRisk profile;
        [Min(0)] public int shamblers = 1, lurkers, runners;
        public ZombieEncounterContext context;
        public float Risk => profile ? profile.Score(shamblers, lurkers, runners, context) : 0;
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2,
                $"S={shamblers} L={lurkers} R={runners} risk={Risk:F1} " +
                (profile && Risk > profile.maximumAuthoredRisk ? "OVER BUDGET: widen escape / reduce pressure" : "authored budget") +
                $"\nvisibility={context.visibility:F1}, escapes={context.escapeRoutes}, interior={context.interior}, ambush={context.ambush}");
        }
#endif
    }
}
