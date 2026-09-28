using UnityEngine;
namespace LastSignal
{
    /// <summary>One nearest visible target per swing; bounded queries fail closed on saturation.</summary>
    public sealed class MeleeAttackResolver
    {
        readonly Collider[] candidates = new Collider[32];
        readonly RaycastHit[] blockers = new RaycastHit[32];
        public string LastResult { get; private set; } = "None";
        public Vector3 LastImpactPosition { get; private set; }
        public IDamageable LastTarget { get; private set; }
        public bool Resolve(Transform eye, Transform origin, MeleeWeaponDefinition definition, GameObject player, ulong swing)
        {
            LastTarget = null; LastImpactPosition = default; LastResult = "Miss";
            if (!eye || !origin || !definition || !definition.Valid || !player) { LastResult = "Invalid"; return false; }
            const int mask = ~(1 << 2);
            // Sphere checks also catch rays starting inside a wall. Self geometry is ignored explicitly.
            int n = Physics.OverlapSphereNonAlloc(origin.position, .025f, candidates, mask, QueryTriggerInteraction.Ignore);
            if (n == candidates.Length) { LastResult = "Saturated"; return false; }
            for (int i=0;i<n;i++) if (!Self(candidates[i],player) && candidates[i].GetComponentInParent<IDamageable>() == null) { LastResult="BlockedOrigin"; return false; }
            if (Blocked(eye.position, origin.position, player, null)) { LastResult="BlockedOrigin"; return false; }
            n = Physics.OverlapCapsuleNonAlloc(eye.position, eye.position + eye.forward * definition.Range, definition.Radius, candidates, mask, QueryTriggerInteraction.Ignore);
            if (n == candidates.Length) { LastResult = "Saturated"; return false; }
            Collider chosen = null; IDamageable target = null; Vector3 point = default; float nearest = float.MaxValue;
            for (int i=0;i<n;i++)
            {
                var c = candidates[i];
                if (Self(c,player)) continue;
                var damageable = c.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive) continue;
                Vector3 p = c.ClosestPoint(eye.position), delta = p - eye.position;
                float distance = delta.magnitude;
                if (distance > definition.Range || distance < .001f || Vector3.Dot(eye.forward, delta / distance) < .65f || distance >= nearest) continue;
                if (Blocked(eye.position, p, player, Canonical(damageable)) || Blocked(origin.position, p, player, Canonical(damageable))) continue;
                chosen=c; target=damageable; point=p; nearest=distance;
            }
            if (!chosen) return false;
            // Single dispatch guarantees multi-collider dedupe without a per-frame target collection.
            LastTarget = Canonical(target); LastImpactPosition = point; LastResult = "Hit";
            target.TakeDamage(new DamageInfo { Amount=definition.Damage, BaseAmount=definition.Damage, Multiplier=1, ShotId=swing,
                HitCollider=chosen, Direction=eye.forward, Category=DamageCategory.Melee, SourcePosition=origin.position,
                HitPoint=point, HitNormal=-eye.forward, Instigator=player });
            return true;
        }
        bool Blocked(Vector3 from, Vector3 to, GameObject player, IDamageable target)
        {
            Vector3 delta=to-from; float distance=delta.magnitude;
            if(distance < .001f) return false;
            int n=Physics.RaycastNonAlloc(from,delta/distance,blockers,distance,~(1<<2),QueryTriggerInteraction.Ignore);
            if(n==blockers.Length) return true;
            for(int i=0;i<n;i++)
            {
                var c=blockers[i].collider;
                if(Self(c,player)) continue;
                var d=c.GetComponentInParent<IDamageable>();
                if(target != null && Canonical(d)==target) continue;
                return true;
            }
            return false;
        }
        static bool Self(Collider c,GameObject player) => c.transform.IsChildOf(player.transform);
        static IDamageable Canonical(IDamageable d) => d is ZombieHitRegion r ? r.Owner : d;
    }
}
