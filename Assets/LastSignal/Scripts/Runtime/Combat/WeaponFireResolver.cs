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
            public Vector3 SourcePosition;
            public Vector3 EndPoint, Direction;
            public DamageReceipt Receipt;
            public ImpactSurfaceKind Surface;
            public WeaponCategory WeaponCategory;
            public FireMode FireMode;
            public bool DamageCommitted => Receipt.Committed;
            public bool ImpactAccepted => Receipt.PresentationAccepted;
            public bool Hit;
            public bool MuzzleObstructed;
            public bool BarrelClipped;
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
            var result = new ShotResult { ShotId = ++nextShotId, SourcePosition = muzzlePosition };

            maxRange = float.IsFinite(maxRange) ? Mathf.Max(0, maxRange) : 0;
            cameraForward = cameraForward.sqrMagnitude > .000001f ? cameraForward.normalized : Vector3.forward;
            // Step 1: Camera ray — where does the player INTEND to shoot?
            Vector3 targetPoint;
            if (Physics.Raycast(cameraOrigin, cameraForward, out RaycastHit cameraHit, maxRange, hitMask, QueryTriggerInteraction.Ignore))
                targetPoint = cameraHit.point;
            else
                targetPoint = cameraOrigin + cameraForward * maxRange;

            if (ZombieCorpseImpactReceiver.Raycast(new Ray(cameraOrigin, cameraForward), Vector3.Distance(cameraOrigin, targetPoint), hitMask,
                out _, out var aimedCorpse))
            {
                targetPoint = aimedCorpse.position;
                cameraHit = default; // The camera's farther collider is no longer a tested muzzle endpoint.
            }

            // Step 2: Muzzle obstruction — is there geometry between muzzle and the nearby area?
            Vector3 muzzleForward = (targetPoint - muzzlePosition).normalized;
            float pathLength = Vector3.Distance(muzzlePosition, targetPoint);
            result.EndPoint = targetPoint;
            result.HitPoint = targetPoint; // A miss has a real endpoint too.
            result.Direction = muzzleForward;
            result.Distance = pathLength;
            RaycastHit muzzleHit;

            // If the weapon barrel has clipped into or through a collider, the muzzle raycast will fail 
            // because it starts inside. We detect this by tracing from the camera (eye) to the muzzle.
            if (Physics.Linecast(cameraOrigin, muzzlePosition, out RaycastHit clipHit, hitMask, QueryTriggerInteraction.Ignore))
            {
                muzzleHit = clipHit;
                result.BarrelClipped = true;
                result.MuzzleObstructed = true;
            }
            else if (Physics.Raycast(muzzlePosition, muzzleForward, out muzzleHit, Mathf.Min(Mathf.Max(0, muzzleObstructionRange), pathLength + .1f), hitMask, QueryTriggerInteraction.Ignore))
            {
                // The nearest obstruction receives the shot, including a damageable at contact range.
                // Never continue through it to the camera-selected distant target.
                result.MuzzleObstructed = true;
            }
            else
            {
                float fullDistance = pathLength;
                if (!Physics.Raycast(muzzlePosition, muzzleForward, out muzzleHit, fullDistance, hitMask, QueryTriggerInteraction.Ignore))
                {
                    // Exact endpoint hits can fall outside PhysX's inclusive distance through rounding.
                    // Reuse the selected camera contact only after the complete muzzle path was clear.
                    if (TryCorpse(ref result, muzzlePosition, muzzleForward, fullDistance + .001f, hitMask, damage, instigator)) return result;
                    if (!cameraHit.collider) return result;
                    muzzleHit = cameraHit;
                }
            }

            if (!result.BarrelClipped && TryCorpse(ref result, muzzlePosition, muzzleForward,
                Vector3.Distance(muzzlePosition, muzzleHit.point), hitMask, damage, instigator)) return result;
            result.Hit = true;
            result.HitPoint = result.EndPoint = muzzleHit.point;
            result.HitNormal = muzzleHit.normal;
            result.Distance = Vector3.Distance(muzzlePosition, muzzleHit.point);
            result.Collider = muzzleHit.collider;
            result.Surface = ImpactSurface.Resolve(muzzleHit.collider);

            // Step 4: Dispatch once for the one selected collider (short or full ray).
            var damageable = muzzleHit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                if (damageable is Component)
                {
                    result.Target = damageable;
                    var info = new DamageInfo
                    {
                        Amount = damage, BaseAmount = damage, Multiplier = 1,
                        ShotId = result.ShotId, HitCollider = muzzleHit.collider,
                        Direction = muzzleForward, Category = DamageCategory.Bullet,
                        SourcePosition = muzzlePosition,
                        HitPoint = muzzleHit.point,
                        HitNormal = muzzleHit.normal,
                        Instigator = instigator
                    };
                    if (damageable is IDamageReceiptReceiver receiver) result.Receipt = receiver.ReceiveDamage(info);
                    else damageable.TakeDamage(info); // Legacy targets cannot claim a receipt.

                }
            }

            return result;
        }
        static bool TryCorpse(ref ShotResult result, Vector3 origin, Vector3 direction, float distance, int mask, float damage, GameObject instigator)
        {
            if (!ZombieCorpseImpactReceiver.Raycast(new Ray(origin, direction), distance, mask, out var corpse, out var contact)) return false;
            result.Hit = true; result.HitPoint = result.EndPoint = contact.position; result.HitNormal = contact.normal;
            result.Distance = contact.distance;
            result.Receipt = corpse.Receive(new DamageInfo { Amount = damage, BaseAmount = damage, Multiplier = 1,
                ShotId = result.ShotId, Direction = direction, Category = DamageCategory.Bullet,
                SourcePosition = origin, HitPoint = contact.position, HitNormal = contact.normal, Instigator = instigator }, contact);
            result.Collider = result.Receipt.Hit.HitCollider;
            result.Surface = contact.surface.part == ZombieBodyPart.Head ? ImpactSurfaceKind.Bone : ImpactSurfaceKind.Flesh;
            return true;
        }
    }
}
