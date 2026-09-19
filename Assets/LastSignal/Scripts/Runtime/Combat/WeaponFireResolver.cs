using System.Collections.Generic;
using UnityEngine;

namespace LastSignal
{
    /// <summary>
    /// Resolves a single valid shot: camera ray → target → muzzle obstruction check → damage.
    /// Static utility. No state. Handles the classic camera-vs-muzzle exploit.
    /// </summary>
    public static class WeaponFireResolver
    {
        // Reusable hash set for multi-collider deduplication per shot.
        static readonly HashSet<Transform> hitRoots = new HashSet<Transform>();

        public struct ShotResult
        {
            public bool Hit;
            public bool MuzzleObstructed;
            public Vector3 HitPoint;
            public Vector3 HitNormal;
            public float Distance;
            public IDamageable Target;
            public Collider Collider;
        }

        /// <summary>
        /// Resolve a hitscan shot.
        /// 1. Camera ray determines intended target direction.
        /// 2. Muzzle-to-target check validates physical line of fire.
        /// 3. First valid hit applies damage exactly once (multi-collider dedup).
        /// </summary>
        /// <param name="cameraOrigin">Camera world position (eye).</param>
        /// <param name="cameraForward">Camera forward direction.</param>
        /// <param name="muzzlePosition">Physical muzzle world position.</param>
        /// <param name="maxRange">Maximum raycast distance.</param>
        /// <param name="muzzleObstructionRange">Short-range check from muzzle to prevent wall-shooting.</param>
        /// <param name="damage">Damage amount if target is hit.</param>
        /// <param name="instigator">The shooting player/entity.</param>
        /// <param name="hitMask">LayerMask for raycasts.</param>
        public static ShotResult Resolve(
            Vector3 cameraOrigin, Vector3 cameraForward,
            Vector3 muzzlePosition,
            float maxRange, float muzzleObstructionRange,
            float damage, GameObject instigator,
            LayerMask hitMask)
        {
            var result = new ShotResult();

            // Step 1: Camera ray — where does the player INTEND to shoot?
            Vector3 targetPoint;
            if (Physics.Raycast(cameraOrigin, cameraForward, out RaycastHit cameraHit, maxRange, hitMask, QueryTriggerInteraction.Ignore))
                targetPoint = cameraHit.point;
            else
                targetPoint = cameraOrigin + cameraForward * maxRange;

            // Step 2: Muzzle obstruction — is there geometry between muzzle and the nearby area?
            Vector3 muzzleForward = (targetPoint - muzzlePosition).normalized;
            if (Physics.Raycast(muzzlePosition, muzzleForward, out RaycastHit obstructionHit, muzzleObstructionRange, hitMask, QueryTriggerInteraction.Ignore))
            {
                // The weapon muzzle is blocked by nearby geometry. Shot is physically blocked.
                result.MuzzleObstructed = true;
                result.HitPoint = obstructionHit.point;
                result.HitNormal = obstructionHit.normal;
                result.Collider = obstructionHit.collider;
                // Still apply impact VFX at obstruction point, but no target damage at distance.
                return result;
            }

            // Step 3: Full muzzle-to-target ray for actual hit resolution.
            float fullDistance = Vector3.Distance(muzzlePosition, targetPoint) + .1f;
            if (!Physics.Raycast(muzzlePosition, muzzleForward, out RaycastHit muzzleHit, fullDistance, hitMask, QueryTriggerInteraction.Ignore))
            {
                // Missed — no geometry hit from muzzle.
                return result;
            }

            result.Hit = true;
            result.HitPoint = muzzleHit.point;
            result.HitNormal = muzzleHit.normal;
            result.Distance = muzzleHit.distance;
            result.Collider = muzzleHit.collider;

            // Step 4: Find damageable on the hit object. Deduplicate multi-collider hits.
            hitRoots.Clear();
            var damageable = muzzleHit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                Transform root = (damageable as Component)?.transform;
                if (root != null && hitRoots.Add(root))
                {
                    result.Target = damageable;
                    damageable.TakeDamage(new DamageInfo
                    {
                        Amount = damage,
                        SourcePosition = muzzlePosition,
                        HitPoint = muzzleHit.point,
                        HitNormal = muzzleHit.normal,
                        Instigator = instigator
                    });
                }
            }

            return result;
        }
    }
}
