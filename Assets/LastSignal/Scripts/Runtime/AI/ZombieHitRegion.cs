using UnityEngine;

namespace LastSignal
{
    [DisallowMultipleComponent, RequireComponent(typeof(Collider))]
    public sealed class ZombieHitRegion : MonoBehaviour, IDamageable
    {
        [SerializeField] ZombieHealth owner;
        [SerializeField] DamageRegion region = DamageRegion.Body;
        [SerializeField, Min(.01f)] float damageMultiplier = 1;
        [SerializeField] Collider hitCollider;
        public ZombieHealth Owner => owner;
        public DamageRegion Region => region;
        public float DamageMultiplier => damageMultiplier;
        public Collider HitCollider => hitCollider;
        public bool IsAlive => isActiveAndEnabled && owner && owner.IsAlive;
        public void Configure(ZombieHealth health, DamageRegion type, float multiplier, Collider collider)
        { owner = health; region = type; damageMultiplier = multiplier; hitCollider = collider; }
        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive || !hitCollider || !hitCollider.enabled ||
                (info.HitCollider && info.HitCollider != hitCollider) ||
                !float.IsFinite(damageMultiplier) || damageMultiplier <= 0) return;
            info.BaseAmount = info.Amount; info.Multiplier = damageMultiplier;
            info.Amount *= damageMultiplier; info.Region = region; info.HitCollider = hitCollider;
            owner.ReceiveRegion(info, this);
        }
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (!hitCollider) return;
            Gizmos.color = region == DamageRegion.Head ? Color.yellow : Color.cyan;
            Gizmos.DrawWireCube(hitCollider.bounds.center, hitCollider.bounds.size);
            UnityEditor.Handles.Label(hitCollider.bounds.center, region + " x" + damageMultiplier + " HP=" + (owner ? owner.CurrentHealth : 0));
        }
#endif
    }
}
