using System;
using UnityEngine;
namespace LastSignal
{
    [Serializable]
    public struct ZombieEncounterContext
    {
        [Range(0, 1)] public float visibility, pressure, exposure;
        [Min(0)] public int escapeRoutes;
        [Min(0)] public float approachDistance, recoverySpacing;
        public bool interior, ambush, constrainedTraversal;
    }
    [CreateAssetMenu(menuName = "Last Signal/Zombie Encounter Risk")]
    public sealed class ZombieEncounterRisk : ScriptableObject
    {
        public float shamblerWeight = 1, lurkerWeight = 1.5f, runnerWeight = 2;
        public float obscurityWeight = .6f, interiorWeight = .2f, ambushWeight = .35f, congestionWeight = .3f;
        public float maximumAuthoredRisk = 12;
        static bool Weight(float value) => float.IsFinite(value) && value >= 0 && value <= 100;
        public bool IsValid => Weight(shamblerWeight) && Weight(lurkerWeight) && Weight(runnerWeight) &&
            shamblerWeight + lurkerWeight + runnerWeight > 0 && Weight(obscurityWeight) && Weight(interiorWeight) &&
            Weight(ambushWeight) && Weight(congestionWeight) && float.IsFinite(maximumAuthoredRisk) && maximumAuthoredRisk > 0;
        public float Score(int shamblers, int lurkers, int runners, ZombieEncounterContext c)
        {
            if (!IsValid || c.escapeRoutes < 0 || shamblers < 0 || lurkers < 0 || runners < 0 || !float.IsFinite(c.visibility) ||
                !float.IsFinite(c.pressure) || !float.IsFinite(c.exposure) || !float.IsFinite(c.approachDistance) ||
                !float.IsFinite(c.recoverySpacing)) throw new ArgumentException("Invalid encounter context.");
            float threat = shamblers * shamblerWeight + lurkers * lurkerWeight + runners * runnerWeight;
            float context = 1 + (1 - Mathf.Clamp01(c.visibility)) * obscurityWeight +
                (c.interior ? interiorWeight : 0) + (c.ambush && lurkers > 0 ? ambushWeight : 0) +
                (c.constrainedTraversal ? congestionWeight : 0) + (c.escapeRoutes <= 0 ? .6f : c.escapeRoutes == 1 ? .25f : 0) +
                Mathf.Clamp01(c.pressure) * .4f + Mathf.Clamp01(c.exposure) * .25f;
            float relief = Mathf.Clamp(c.approachDistance, 0, 20) * .01f + Mathf.Clamp(c.recoverySpacing, 0, 5) * .04f;
            return Mathf.Max(0, threat * Mathf.Max(.5f, context - relief));
        }
    }
}
