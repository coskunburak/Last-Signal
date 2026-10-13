using UnityEngine;

namespace LastSignal
{
    /// <summary>Bounded presentation-only receiver. Uses visible skinned triangles, never re-enables AI colliders.
    /// The weapon compares these intersections with its existing nearest opaque obstruction.</summary>
    [DisallowMultipleComponent]
    public sealed class ZombieCorpseImpactReceiver : MonoBehaviour
    {
        static readonly ZombieCorpseImpactReceiver[] active = new ZombieCorpseImpactReceiver[16];
        ZombieHealth health;
        ZombieLocalizedWounds wounds;
        ZombieBloodVfxPresenter blood;
        ZombieHitRegion[] regions;
        readonly ulong[] shots = new ulong[32]; readonly int[] subHits = new int[32]; int nextShot;
        public bool Receiving { get; private set; }
        public float Expires { get; private set; }
        public int ImpactCount { get; private set; }
        public void Initialize(ZombieHealth owner, ZombieLocalizedWounds surfaces, ZombieBloodVfxPresenter presenter)
        { health = owner; wounds = surfaces; blood = presenter; regions = GetComponentsInChildren<ZombieHitRegion>(true); }
        public void Retain(float expires)
        {
            if (Receiving || !health || health.IsAlive || !wounds || wounds.Resolver == null) return;
            Expires = expires; Receiving = true;
            for (int i = 0; i < active.Length; i++) if (!active[i]) { active[i] = this; return; }
            Receiving = false; // Pool and registry are both bounded; never grow to handle overflow.
        }
        public static bool Raycast(Ray ray, float maxDistance, int hitMask, out ZombieCorpseImpactReceiver owner, out ZombieWoundSurfaceResolver.Contact contact)
        {
            owner = null; contact = default;
            for (int i = 0; i < active.Length; i++)
            {
                var c = active[i];
                if (!c || !c.Receiving || !c.isActiveAndEnabled || !c.health || c.health.IsAlive || Time.time >= c.Expires) continue;
                if (!c.wounds.Resolver.Raycast(ray, maxDistance, ZombieBodyPart.Unspecified, out var hit)) continue;
                var region = c.Region(hit.surface.part);
                if (!region || (hitMask & (1 << region.gameObject.layer)) == 0) continue;
                owner = c; contact = hit; maxDistance = hit.distance;
            }
            return owner;
        }
        ZombieHitRegion Region(ZombieBodyPart part)
        {
            if (regions != null) foreach (var r in regions) if (r && r.Owner == health && r.BodyPart == part) return r;
            return null;
        }
        public DamageReceipt Receive(DamageInfo hit, ZombieWoundSurfaceResolver.Contact contact)
        {
            if (!Receiving || !isActiveAndEnabled || !health || health.IsAlive || Time.time >= Expires || !contact.Valid ||
                !wounds.Resolver.Surfaces.Contains(contact.surface) || hit.Category != DamageCategory.Bullet ||
                !float.IsFinite(hit.Amount) || hit.Amount <= 0 || !ZombieWoundSurfaceResolver.Finite(hit.Direction) || hit.Direction.sqrMagnitude < .0001f ||
                !ZombieWoundSurfaceResolver.Finite(hit.SourcePosition) || !ZombieWoundSurfaceResolver.Finite(contact.position) || !ZombieWoundSurfaceResolver.Finite(contact.normal)) return default;
            if (hit.ShotId != 0)
            {
                for (int i = 0; i < shots.Length; i++) if (shots[i] == hit.ShotId && subHits[i] == hit.SubHitId) return default;
                shots[nextShot] = hit.ShotId; subHits[nextShot] = hit.SubHitId; nextShot = (nextShot + 1) % shots.Length;
            }
            var region = Region(contact.surface.part); if (!region) return default;
            hit.HitCollider = region.HitCollider; hit.BodyPart = region.BodyPart; hit.Region = region.Region;
            hit.HitPoint = contact.position; hit.HitNormal = contact.normal;
            var receipt = new DamageReceipt(hit, health, contact); ImpactCount++;
            // No ZombieHealth transaction, sever threshold, AI or Died event on this route.
            try { if (blood) blood.PresentImpact(receipt); } catch (System.Exception e) { Debug.LogException(e, this); }
            return receipt;
        }
        public void Clear()
        {
            Receiving = false; Expires = 0; ImpactCount = nextShot = 0;
            System.Array.Clear(shots, 0, shots.Length); System.Array.Clear(subHits, 0, subHits.Length);
            for (int i = 0; i < active.Length; i++) if (active[i] == this) active[i] = null;
        }
        void OnDisable() => Clear();
    }
}
