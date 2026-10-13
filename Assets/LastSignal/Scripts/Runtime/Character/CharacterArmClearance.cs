using UnityEngine;

namespace LastSignal
{
    /// <summary>Cosmetic torso envelope, expanded by sleeve radius. Does not change bone length or hand targets.</summary>
    public static class CharacterArmClearance
    {
        public struct Torso
        {
            public Vector3 Center, Radii;
            public Quaternion Rotation;
            public bool Valid => float.IsFinite(Radii.x) && float.IsFinite(Radii.y) && float.IsFinite(Radii.z) &&
                Radii.x > .0001f && Radii.y > .0001f && Radii.z > .0001f;
            public Vector3 Local(Vector3 point) => Quaternion.Inverse(Rotation) * (point - Center);
            public float Penetration(Vector3 point)
            {
                if (!Valid) return 0;
                Vector3 p = Local(point);
                float radius = new Vector3(p.x / Radii.x, p.y / Radii.y, p.z / Radii.z).magnitude;
                return Mathf.Max(0, 1 - radius) * Mathf.Min(Radii.x, Mathf.Min(Radii.y, Radii.z));
            }
            public Vector3 FrontCorrection(Vector3 point)
            {
                if (!Valid) return Vector3.zero;
                Vector3 p = Local(point);
                float crossSection = 1 - p.x * p.x / (Radii.x * Radii.x) - p.y * p.y / (Radii.y * Radii.y);
                if (crossSection <= 0) return Vector3.zero;
                float front = Radii.z * Mathf.Sqrt(crossSection);
                return Rotation * Vector3.forward * Mathf.Max(0, front + .003f - p.z);
            }
        }

        static float SegmentPenetration(Torso torso, Vector3 from, Vector3 to)
        {
            if (!torso.Valid) return 0;
            Vector3 a = torso.Local(from), b = torso.Local(to);
            a = new Vector3(a.x / torso.Radii.x, a.y / torso.Radii.y, a.z / torso.Radii.z);
            b = new Vector3(b.x / torso.Radii.x, b.y / torso.Radii.y, b.z / torso.Radii.z);
            Vector3 delta = b - a;
            float t = delta.sqrMagnitude > 1e-12f ? Mathf.Clamp01(-Vector3.Dot(a, delta) / delta.sqrMagnitude) : 0;
            return Mathf.Max(0, 1 - (a + delta * t).magnitude) * Mathf.Min(torso.Radii.x, Mathf.Min(torso.Radii.y, torso.Radii.z));
        }
        public static float Penetration(Torso torso, Vector3 shoulder, Vector3 elbow, Vector3 wrist)
        {
            return Mathf.Max(SegmentPenetration(torso, Vector3.Lerp(shoulder, elbow, .25f), elbow),
                SegmentPenetration(torso, elbow, wrist));
        }

        public static CharacterArmIK.Solution AvoidTorso(CharacterArmIK.Solution pose, Vector3 shoulder, Torso torso)
        {
            if (!pose.Valid || Penetration(torso, shoulder, pose.Elbow, pose.Wrist) <= .0001f) return pose;
            Vector3 axis = (pose.Wrist - shoulder).normalized;
            Vector3 center = shoulder + axis * Vector3.Dot(pose.Elbow - shoulder, axis);
            Vector3 radial = pose.Elbow - center;
            float bestDepth = Penetration(torso, shoulder, pose.Elbow, pose.Wrist);
            var best = pose;
            // Nearest clear swivel about the shoulder/wrist axis; both segment lengths and contact stay exact.
            for (int step = 1; step <= 18; step++)
            {
                bool clear = false;
                float chosenAngle = 0;
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    float angle = step * 10f * sign;
                    Vector3 elbow = center + Quaternion.AngleAxis(angle, axis) * radial;
                    float depth = Penetration(torso, shoulder, elbow, pose.Wrist);
                    if (depth < bestDepth)
                    {
                        bestDepth = depth; best.Elbow = elbow;
                        best.Bend = (elbow - center).normalized; chosenAngle = angle;
                    }
                    if (depth <= .0001f) { clear = true; chosenAngle = angle; best.Elbow = elbow; best.Bend = (elbow - center).normalized; break; }
                }
                if (!clear) continue;
                // Refine the boundary to avoid visible ten-degree jumps at the envelope surface.
                float low = 0, high = chosenAngle;
                for (int i = 0; i < 8; i++)
                {
                    float angle = (low + high) * .5f;
                    Vector3 elbow = center + Quaternion.AngleAxis(angle, axis) * radial;
                    if (Penetration(torso, shoulder, elbow, pose.Wrist) <= .0001f) high = angle;
                    else low = angle;
                }
                best.Elbow = center + Quaternion.AngleAxis(high, axis) * radial;
                best.Bend = (best.Elbow - center).normalized;
                return best;
            }
            // An impossible wrist/torso relationship stays a reported failure; never stretch or detach a palm.
            return best;
        }
    }
}
