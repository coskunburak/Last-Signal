using UnityEngine;

namespace LastSignal
{
    public struct DamageInfo
    {
        public float Amount;
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
