using UnityEngine;

namespace LastSignal
{
    public enum DamageRegion { Unspecified, Body, Head }
    public enum DamageCategory { Unspecified, Bullet, Melee }

    public struct DamageInfo
    {
        public float Amount;
        public float BaseAmount, Multiplier;
        public ulong ShotId;
        public Collider HitCollider;
        public Vector3 Direction;
        public DamageRegion Region;
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
