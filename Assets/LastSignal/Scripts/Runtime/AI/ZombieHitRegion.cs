using UnityEngine;

namespace LastSignal
{
    [DisallowMultipleComponent, RequireComponent(typeof(Collider))]
    public sealed class ZombieHitRegion : MonoBehaviour, IDamageReceiptReceiver
    {
        [SerializeField] ZombieHealth owner;
        [SerializeField] DamageRegion region = DamageRegion.Body;
        [SerializeField] ZombieBodyPart bodyPart = ZombieBodyPart.Unspecified;
        [SerializeField, Min(.01f)] float damageMultiplier = 1;
        [SerializeField, Range(0, .9f)] float protectionFraction;
        [SerializeField] Collider hitCollider;
        public ZombieHealth Owner => owner;
        public DamageRegion Region => region;
        public ZombieBodyPart BodyPart => bodyPart;
        public float DamageMultiplier => damageMultiplier;
        public float ProtectionFraction => protectionFraction;
        public Collider HitCollider => hitCollider;
        public bool IsAlive => isActiveAndEnabled && owner && owner.IsAlive;
        public void Configure(ZombieHealth health, DamageRegion type, float multiplier, Collider collider,
            ZombieBodyPart part = ZombieBodyPart.Unspecified, float protection = 0)
        { owner = health; region = type; bodyPart = part; damageMultiplier = multiplier;
            protectionFraction = Mathf.Clamp(protection, 0, .9f); hitCollider = collider; }
        public void TakeDamage(DamageInfo info) => ReceiveDamage(info);
        public DamageReceipt ReceiveDamage(DamageInfo info)
        {
            if (!IsAlive || !hitCollider || !hitCollider.enabled ||
                (info.HitCollider && info.HitCollider != hitCollider) ||
                !float.IsFinite(damageMultiplier) || damageMultiplier <= 0 ||
                !float.IsFinite(protectionFraction) || protectionFraction < 0 || protectionFraction > .9f) return default;
            info.BaseAmount = info.Amount; info.Multiplier = damageMultiplier * (1 - protectionFraction);
            info.Amount *= info.Multiplier; info.Region = region; info.BodyPart = bodyPart; info.HitCollider = hitCollider;
            return owner.ReceiveRegion(info, this);
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
