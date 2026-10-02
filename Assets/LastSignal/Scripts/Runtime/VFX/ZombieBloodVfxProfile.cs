using System;
using UnityEngine;

namespace LastSignal
{
    [CreateAssetMenu(menuName = "Last Signal/Zombie Blood VFX")]
    public sealed class ZombieBloodVfxProfile : ScriptableObject
    {
        [Serializable] public sealed class Region
        {
            [Min(.01f)] public float scale = 1;
            [Range(1, 32)] public int burstCount = 18;
            [Range(0, 90)] public float burstConeAngle = 65;
            [Range(.1f, 8)] public float burstSpeed = 2.8f;
            [Range(.1f, 6)] public float spurtSpeed = 1.6f;
            [Range(1, 45)] public float spurtConeAngle = 18;
            [Range(1, 6)] public float pulseFrequency = 3.501409f;
            [Range(0, 1)] public float pulseFloor = .15f;
            [Range(0, .3f)] public float pressureHold = 0;
            [Range(.1f, 1)] public float terminalSpeedFraction = 1;
            [Min(.01f)] public float spurtDuration = 1.6f;
            [Min(0)] public float dripDuration = 2;
            [Min(0)] public float spurtRate = 25;
            [Min(0)] public float dripRate = 5;
            [Range(0, 4)] public int surfaceMarks = 3;
            [Min(.01f)] public float markSize = .3f;
        }
        public GameObject burstPrefab, spurtPrefab, dripPrefab, surfacePrefab;
        public Region head = new Region { scale = 1.1f, burstCount = 32, burstSpeed = 4.6f,
            spurtSpeed = 3.1f, spurtConeAngle = 24, pulseFrequency = 3.2f, pulseFloor = .28f,
            pressureHold = .2f, terminalSpeedFraction = .32f, spurtDuration = 2.1f,
            spurtRate = 72, dripDuration = 2.2f, dripRate = 7, markSize = .34f };
        public Region arm = new Region { scale = .7f, burstCount = 12, burstConeAngle = 32, spurtDuration = 1.1f, dripDuration = 1.5f, spurtRate = 18, surfaceMarks = 2, markSize = .22f };
        public Region hand = new Region { scale = .42f, burstCount = 7, burstConeAngle = 20, spurtDuration = .65f, dripDuration = 1, spurtRate = 10, dripRate = 3, surfaceMarks = 1, markSize = .13f };
        public AnimationCurve pressure = AnimationCurve.Linear(0, 1, 1, 0);
        [Range(1, 32)] public int effectCapacity = 16;
        [Range(1, 64)] public int markCapacity = 32;
        public LayerMask surfaceLayers = 1;
        [Min(.1f)] public float probeDistance = 3;
        [Min(1)] public float markLifetime = 25;
        [Min(.1f)] public float poolGrowthDuration = 4;
        public Region ForPart(ZombieBodyPart part) => part == ZombieBodyPart.Head ? head :
            part == ZombieBodyPart.LeftArm || part == ZombieBodyPart.RightArm ? arm :
            part == ZombieBodyPart.LeftHand || part == ZombieBodyPart.RightHand ? hand : null;
        public bool IsValid => burstPrefab && spurtPrefab && dripPrefab && surfacePrefab &&
            effectCapacity > 0 && effectCapacity <= 32 && markCapacity > 0 && markCapacity <= 64 &&
            float.IsFinite(markLifetime) && markLifetime > 0 && float.IsFinite(probeDistance) && probeDistance > 0 &&
            poolGrowthDuration > 0 && pressure != null && Valid(head) && Valid(arm) && Valid(hand);
        public float PressureAt(Region region, float age)
        {
            if (age < 0 || age >= region.spurtDuration) return 0;
            float t = Mathf.InverseLerp(region.pressureHold, region.spurtDuration, age);
            return Mathf.Clamp01(pressure.Evaluate(t));
        }
        public static float PulseAt(Region region, float age) => region.pulseFloor +
            (1 - region.pulseFloor) * Mathf.Pow(Mathf.Max(0, Mathf.Cos(age * region.pulseFrequency * 2 * Mathf.PI)), 2);
        static bool Within(float value, float min, float max) => float.IsFinite(value) && value >= min && value <= max;
        static bool Valid(Region r) => r != null && float.IsFinite(r.scale) && r.scale > 0 && r.burstCount > 0 && r.burstCount <= 32 &&
            Within(r.burstSpeed, .1f, 8) && Within(r.spurtSpeed, .1f, 6) && Within(r.spurtConeAngle, 1, 45) &&
            Within(r.pulseFrequency, 1, 6) && Within(r.pulseFloor, 0, 1) &&
            Within(r.pressureHold, 0, .3f) && r.pressureHold < r.spurtDuration && Within(r.terminalSpeedFraction, .1f, 1) &&
            float.IsFinite(r.burstConeAngle) && r.burstConeAngle >= 0 && r.burstConeAngle <= 90 &&
            float.IsFinite(r.spurtDuration) && r.spurtDuration > 0 && r.spurtDuration <= 10 &&
            float.IsFinite(r.dripDuration) && r.dripDuration >= 0 && r.dripDuration <= 10 &&
            float.IsFinite(r.spurtRate) && r.spurtRate >= 0 && r.spurtRate <= 100 &&
            float.IsFinite(r.dripRate) && r.dripRate >= 0 && r.dripRate <= 30 &&
            r.surfaceMarks >= 0 && r.surfaceMarks <= 4 && float.IsFinite(r.markSize) && r.markSize > 0;
    }
}
