using UnityEngine;

namespace LastSignal
{
    /// <summary>
    /// Resolves a single valid shot: camera ray → target → muzzle obstruction check → damage.
    /// Static utility with diagnostic shot IDs. Handles camera-vs-muzzle obstruction.
    /// </summary>
    public static class WeaponFireResolver
    {
        // Trace identity only; health remains per target. One nearest hit means one dispatch.
        static ulong nextShotId;

        public struct ShotResult
        {
            public ulong ShotId;
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
            var result = new ShotResult { ShotId = ++nextShotId };

            // Step 1: Camera ray — where does the player INTEND to shoot?
            Vector3 targetPoint;
            if (Physics.Raycast(cameraOrigin, cameraForward, out RaycastHit cameraHit, maxRange, hitMask, QueryTriggerInteraction.Ignore))
                targetPoint = cameraHit.point;
            else
                targetPoint = cameraOrigin + cameraForward * maxRange;

            // Step 2: Muzzle obstruction — is there geometry between muzzle and the nearby area?
            Vector3 muzzleForward = (targetPoint - muzzlePosition).normalized;
            RaycastHit muzzleHit;

            // If the weapon barrel has clipped into or through a collider, the muzzle raycast will fail 
            // because it starts inside. We detect this by tracing from the camera (eye) to the muzzle.
            if (Physics.Linecast(cameraOrigin, muzzlePosition, out RaycastHit clipHit, hitMask, QueryTriggerInteraction.Ignore))
            {
                muzzleHit = clipHit;
                result.MuzzleObstructed = true;
            }
            else if (Physics.Raycast(muzzlePosition, muzzleForward, out muzzleHit, muzzleObstructionRange, hitMask, QueryTriggerInteraction.Ignore))
            {
                // The nearest obstruction receives the shot, including a damageable at contact range.
                // Never continue through it to the camera-selected distant target.
                result.MuzzleObstructed = true;
            }
            else
            {
                float fullDistance = Vector3.Distance(muzzlePosition, targetPoint) + .1f;
                if (!Physics.Raycast(muzzlePosition, muzzleForward, out muzzleHit, fullDistance, hitMask, QueryTriggerInteraction.Ignore))
                    return result;
            }

            result.Hit = true;
            result.HitPoint = muzzleHit.point;
            result.HitNormal = muzzleHit.normal;
            result.Distance = muzzleHit.distance;
            result.Collider = muzzleHit.collider;

            // Step 4: Dispatch once for the one selected collider (short or full ray).
            var damageable = muzzleHit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                if (damageable is Component)
                {
                    result.Target = damageable;
                    damageable.TakeDamage(new DamageInfo
                    {
                        Amount = damage, BaseAmount = damage, Multiplier = 1,
                        ShotId = result.ShotId, HitCollider = muzzleHit.collider,
                        Direction = muzzleForward, Category = DamageCategory.Bullet,
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
