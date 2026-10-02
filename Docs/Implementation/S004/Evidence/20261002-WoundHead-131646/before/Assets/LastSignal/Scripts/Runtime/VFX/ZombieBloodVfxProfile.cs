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
            [Min(.01f)] public float spurtDuration = 1.6f;
            [Min(0)] public float dripDuration = 2;
            [Min(0)] public float spurtRate = 25;
            [Min(0)] public float dripRate = 5;
            [Range(0, 4)] public int surfaceMarks = 3;
            [Min(.01f)] public float markSize = .3f;
        }
        public GameObject burstPrefab, spurtPrefab, dripPrefab, surfacePrefab;
        public Region head = new Region();
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
        static bool Valid(Region r) => r != null && float.IsFinite(r.scale) && r.scale > 0 && r.burstCount > 0 && r.burstCount <= 32 &&
            float.IsFinite(r.burstConeAngle) && r.burstConeAngle >= 0 && r.burstConeAngle <= 90 &&
            float.IsFinite(r.spurtDuration) && r.spurtDuration > 0 && r.spurtDuration <= 10 &&
            float.IsFinite(r.dripDuration) && r.dripDuration >= 0 && r.dripDuration <= 10 &&
            float.IsFinite(r.spurtRate) && r.spurtRate >= 0 && r.spurtRate <= 100 &&
            float.IsFinite(r.dripRate) && r.dripRate >= 0 && r.dripRate <= 30 &&
            r.surfaceMarks >= 0 && r.surfaceMarks <= 4 && float.IsFinite(r.markSize) && r.markSize > 0;
    }
}
