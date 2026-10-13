using UnityEngine;

namespace LastSignal
{
    [CreateAssetMenu(menuName = "Last Signal/Zombie Wounds")]
    public sealed class ZombieWoundProfile : ScriptableObject
    {
        [Range(1, 8)] public int capacity = 8;
        [Range(1, 8)] public int lowCapacity = 4;
        [Range(.01f, .08f)] public float radius = .035f;
        [Range(.005f, .08f)] public float mergeDistance = .025f;
        [Range(.05f, .6f)] public float surfaceSearchDistance = .35f;
        [Range(0, .006f)] public float maximumIndent = .003f;
        [Range(1, 120)] public float drySeconds = 35;
        [Range(.1f, 3)] public float bleedSeconds = .8f;
        [Range(0, 3)] public float dripSeconds = 1.2f;
        [Range(1, 16)] public int corpseCapacity = 8;
        [Range(5, 120)] public float corpseSeconds = 30;
        public bool IsValid => capacity >= 1 && capacity <= 8 && lowCapacity >= 1 && lowCapacity <= capacity &&
            Within(radius, .01f, .08f) && Within(mergeDistance, .005f, .08f) && Within(surfaceSearchDistance, .05f, .6f) &&
            Within(maximumIndent, 0, .006f) && Within(drySeconds, 1, 120) && Within(bleedSeconds, .1f, 3) &&
            Within(dripSeconds, 0, 3) && corpseCapacity >= 1 && corpseCapacity <= 16 && Within(corpseSeconds, 5, 120);
        static bool Within(float v, float min, float max) => float.IsFinite(v) && v >= min && v <= max;
    }
}
