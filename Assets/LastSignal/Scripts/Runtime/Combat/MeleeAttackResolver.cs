using UnityEngine;
namespace LastSignal
{
    public enum MeleeTargetOutcome { Miss, WorldBlock, Rejected, EnemyHit, HarvestHit }
    /// <summary>One nearest visible target per swing; bounded queries fail closed on saturation.</summary>
    public sealed class MeleeAttackResolver
    {
        readonly Collider[] candidates = new Collider[32];
        readonly RaycastHit[] blockers = new RaycastHit[32];
        public MeleeTargetOutcome LastOutcome { get; private set; }
        public string LastResult { get; private set; } = "None";
        public Vector3 LastImpactPosition { get; private set; }
        public bool TreeOwnsImpactAudio { get; private set; }
        public IDamageable LastTarget { get; private set; }
        public bool Resolve(Transform eye, Transform origin, MeleeWeaponDefinition definition, GameObject player, ulong swing, MeleeWeaponController weapon = null)
        {
            TreeOwnsImpactAudio=false; LastTarget = null; LastImpactPosition = default; LastResult = "Miss"; LastOutcome=MeleeTargetOutcome.Miss;
            if (!eye || !origin || !definition || !definition.Valid || !player) { LastResult = "Invalid"; LastOutcome=MeleeTargetOutcome.Rejected; return false; }
            const int mask = ~(1 << 2);
            // Sphere checks also catch rays starting inside a wall. Self geometry is ignored explicitly.
            int n = Physics.OverlapSphereNonAlloc(origin.position, .025f, candidates, mask, QueryTriggerInteraction.Ignore);
            if (n == candidates.Length) { LastResult = "Saturated"; LastOutcome=MeleeTargetOutcome.WorldBlock; return false; }
            for (int i=0;i<n;i++) if (!Self(candidates[i],player) && candidates[i].GetComponentInParent<IDamageable>() == null) { LastResult="BlockedOrigin"; LastOutcome=MeleeTargetOutcome.WorldBlock; return false; }
            if (Blocked(eye.position, origin.position, player, null)) { LastResult="BlockedOrigin"; LastOutcome=MeleeTargetOutcome.WorldBlock; return false; }
            n = Physics.OverlapCapsuleNonAlloc(eye.position, eye.position + eye.forward * definition.Range, definition.Radius, candidates, mask, QueryTriggerInteraction.Ignore);
            if (n == candidates.Length) { LastResult = "Saturated"; LastOutcome=MeleeTargetOutcome.WorldBlock; return false; }
            Collider chosen = null; IDamageable target = null; Gathering.ResourceNodeView harvest = null;
            Vector3 point = default; float nearest = float.MaxValue;
            for (int i=0;i<n;i++)
            {
                var c = candidates[i];
                if (Self(c,player)) continue;
                var damageable = Damageable(c);
                var node = weapon ? c.GetComponentInParent<Gathering.ResourceNodeView>() : null;
                if (damageable != null && !damageable.IsAlive) damageable=null;
                if (damageable == null && !node) continue;
                Vector3 p;
                if(node && node.physicsBody)
                {
                    // Chopping uses the aimed trunk contact, not the closest point at camera height.
                    if(!c.Raycast(new Ray(eye.position,eye.forward),out var treeHit,definition.Range) ||
                        !node.physicsBody.Accepts(c,treeHit.point)) continue;
                    p=treeHit.point;
                }
                else p=c.ClosestPoint(eye.position);
                Vector3 delta = p - eye.position;
                float distance = delta.magnitude;
                if (distance > definition.Range || distance < .001f || Vector3.Dot(eye.forward, delta / distance) < .65f) continue;
                // Exact distance ties prefer combat; other ties use live collider identity within this swing only (not stable across sessions).
                if (distance > nearest || (distance == nearest && chosen &&
                    (target != null && damageable == null || (target == null)==(damageable == null) && EntityId.ToULong(c.GetEntityId()) >= EntityId.ToULong(chosen.GetEntityId())))) continue;
                if (Blocked(eye.position, p, player, Canonical(damageable), node) || Blocked(origin.position, p, player, Canonical(damageable), node)) continue;
                chosen=c; target=damageable; harvest=damageable == null ? node : null; point=p; nearest=distance;
            }
            if (!chosen) return false;
            // Nearest visible candidate wins. Resource nodes never implement IDamageable.
            if (harvest)
            {
                var reason = harvest.world ? harvest.world.TryHit(harvest, weapon, swing, chosen, point) : Gathering.HarvestReason.Invalid;
                if(harvest.world) harvest.world.ReportHit(reason);
                LastResult = reason == Gathering.HarvestReason.Accepted ? "HarvestHit" : "Harvest:" + reason;
                LastOutcome=reason==Gathering.HarvestReason.Accepted ? MeleeTargetOutcome.HarvestHit : MeleeTargetOutcome.Rejected;
                if (reason != Gathering.HarvestReason.Accepted) return false;
                TreeOwnsImpactAudio=harvest.physicsBody; // Never substitute a flesh/metal impact for an unapproved tree recording.
                LastImpactPosition=point; return true;
            }
            // Single dispatch guarantees multi-collider dedupe without a per-frame target collection.
            var hit = new DamageInfo { Amount=definition.Damage, BaseAmount=definition.Damage, Multiplier=1, ShotId=swing,
                HitCollider=chosen, Direction=eye.forward, Category=DamageCategory.Melee, SourcePosition=origin.position,
                HitPoint=point, HitNormal=-eye.forward, Instigator=player };
            if (target is IDamageReceiptReceiver receiver)
            {
                if (!receiver.ReceiveDamage(hit).Committed) { LastResult = "Rejected"; LastOutcome=MeleeTargetOutcome.Rejected; return false; }
            }
            else target.TakeDamage(hit); // Legacy targets have no receipt contract.
            LastTarget = Canonical(target); LastImpactPosition = point; LastResult = "Hit"; LastOutcome=MeleeTargetOutcome.EnemyHit;
            return true;
        }
        bool Blocked(Vector3 from, Vector3 to, GameObject player, IDamageable target, Gathering.ResourceNodeView node = null)
        {
            Vector3 delta=to-from; float distance=delta.magnitude;
            if(distance < .001f) return false;
            int n=Physics.RaycastNonAlloc(from,delta/distance,blockers,distance,~(1<<2),QueryTriggerInteraction.Ignore);
            if(n==blockers.Length) return true;
            for(int i=0;i<n;i++)
            {
                var c=blockers[i].collider;
                if(Self(c,player)) continue;
                if (node && c.GetComponentInParent<Gathering.ResourceNodeView>() == node) continue;
                var d=Damageable(c);
                if(target != null && Canonical(d)==target) continue;
                return true;
            }
            return false;
        }
        static bool Self(Collider c,GameObject player) => c.transform.IsChildOf(player.transform);
        static IDamageable Damageable(Collider c)
        {
            // Anatomical surfaces take precedence even when health shares their GameObject.
            var region = c.GetComponent<ZombieHitRegion>();
            return region ? region : c.GetComponentInParent<IDamageable>();
        }
        static IDamageable Canonical(IDamageable d) => d is ZombieHitRegion r ? r.Owner : d;
    }
}
