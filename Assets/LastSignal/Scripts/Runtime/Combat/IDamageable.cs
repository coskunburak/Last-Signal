using UnityEngine;

namespace LastSignal
{
    public enum DamageRegion { Unspecified, Body, Head }
    public enum DamageCategory { Unspecified, Bullet, Melee }
    // Anatomical identity is separate from the legacy damage multiplier region.
    // This allows hit attribution without changing existing damage or death authority.
    public enum ZombieBodyPart { Unspecified, Head, Torso, LeftArm, RightArm, LeftHand, RightHand, LeftLeg, RightLeg }

    public struct DamageInfo
    {
        public float Amount;
        public float BaseAmount, Multiplier;
        public ulong ShotId;
        public Collider HitCollider;
        public Vector3 Direction;
        public DamageRegion Region;
        public ZombieBodyPart BodyPart;
        public DamageCategory Category;
        public Vector3 SourcePosition;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
        public GameObject Instigator;
    }

    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(DamageInfo info);
    }
}
