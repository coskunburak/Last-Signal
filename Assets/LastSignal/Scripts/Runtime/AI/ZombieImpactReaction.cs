using UnityEngine;

namespace LastSignal
{
    public enum ZombieImpactSide { Front, Back, Left, Right }
    public enum ZombieImpactSeverity { Light, Heavy, Sever }

    // A presentation-only snapshot. DamageInfo.Direction is travel direction, not source direction.
    public readonly struct ZombieImpactReaction
    {
        public readonly ZombieImpactSide Side;
        public readonly ZombieImpactSeverity Severity;
        public readonly ZombieBodyPart Part;
        public readonly bool UseLegacyClip;
        public bool UseFlyingBackDeath =>
            (Part == ZombieBodyPart.Head && Severity == ZombieImpactSeverity.Sever) ||
            (Part == ZombieBodyPart.Torso && Side == ZombieImpactSide.Front && Severity == ZombieImpactSeverity.Heavy);
        public static ZombieImpactReaction Legacy => new ZombieImpactReaction(
            ZombieImpactSide.Front, ZombieImpactSeverity.Light, ZombieBodyPart.Torso, true);

        ZombieImpactReaction(ZombieImpactSide side, ZombieImpactSeverity severity,
            ZombieBodyPart part, bool useLegacyClip)
        { Side = side; Severity = severity; Part = part; UseLegacyClip = useLegacyClip; }

        public static ZombieImpactReaction Select(in DamageInfo hit, Quaternion hitRotation,
            float maxHealth, bool newlySevered, float heavyFraction = .25f, float minimumHeavy = 20f)
        {
            Vector3 source = -hit.Direction;
            source.y = 0;
            if (source.sqrMagnitude < .000001f)
            {
                source = hit.SourcePosition.sqrMagnitude > .000001f
                    ? hit.SourcePosition - hit.HitPoint : Vector3.zero;
                source.y = 0;
            }
            // Unknown source preserves the old frontal presentation deterministically.
            ZombieImpactSide side = ZombieImpactSide.Front;
            if (source.sqrMagnitude >= .000001f)
            {
                Vector3 local = Quaternion.Inverse(hitRotation) * source;
                side = Mathf.Abs(local.z) >= Mathf.Abs(local.x)
                    ? local.z >= 0 ? ZombieImpactSide.Front : ZombieImpactSide.Back
                    : local.x >= 0 ? ZombieImpactSide.Right : ZombieImpactSide.Left;
            }
            ZombieBodyPart part = hit.BodyPart != ZombieBodyPart.Unspecified ? hit.BodyPart
                : hit.Region == DamageRegion.Head ? ZombieBodyPart.Head : ZombieBodyPart.Torso;
            ZombieImpactSeverity severity = newlySevered ? ZombieImpactSeverity.Sever
                : hit.Amount >= Mathf.Max(minimumHeavy, maxHealth * heavyFraction) ? ZombieImpactSeverity.Heavy
                : ZombieImpactSeverity.Light;
            // The production upper-body mask keeps locomotion active. A directional spine response
            // is composed with this clip for side/rear impacts instead of rotating the actor.
            return new ZombieImpactReaction(side, severity, part, part != ZombieBodyPart.Head);
        }
    }
}
