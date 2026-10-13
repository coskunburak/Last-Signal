using UnityEngine;

namespace LastSignal
{
    /// <summary>Geometry used by CharacterWeaponRig's final two-bone pass. All distances are world metres.</summary>
    public static class CharacterArmIK
    {
        public struct Solution
        {
            public Vector3 Elbow, Wrist, Bend;
            public float Residual;
            public bool Valid;
        }

        public static Solution Solve(Vector3 origin, Vector3 target, float upperLength, float lowerLength,
            Vector3 hint, Vector3 fallbackBend, Vector3 previousBend, float maximumReach, float bendRadians)
        {
            var result = new Solution { Elbow = origin, Wrist = origin };
            if (!Finite(origin) || !Finite(target) || !Finite(hint) || !Finite(fallbackBend) || !Finite(previousBend) ||
                !float.IsFinite(maximumReach) || !float.IsFinite(bendRadians) || !float.IsFinite(upperLength) || !float.IsFinite(lowerLength) ||
                upperLength < .0001f || lowerLength < .0001f) return result;
            float total = upperLength + lowerLength;
            ReachLimits(upperLength, lowerLength, maximumReach, out float minimum, out float maximum);
            Vector3 delta = target - origin;
            // A target at the shoulder is still solvable; retain a deterministic plane instead of skipping the arm.
            Vector3 direction = delta.sqrMagnitude > 1e-10f ? delta.normalized : Vector3.Cross(fallbackBend, Vector3.right).normalized;
            if (direction.sqrMagnitude < .5f) direction = Vector3.forward;
            float distance = Mathf.Clamp(delta.magnitude, minimum, maximum);
            Vector3 bend = Vector3.ProjectOnPlane(hint - origin, direction);
            Vector3 previous = Vector3.ProjectOnPlane(previousBend, direction);
            // Near a collinear hint, keep the last valid plane. A tiny noisy projection must not flip an elbow.
            if (bend.sqrMagnitude < total * total * .0001f) bend = previous;
            if (bend.sqrMagnitude < 1e-10f) bend = Vector3.ProjectOnPlane(fallbackBend, direction);
            if (bend.sqrMagnitude < 1e-10f)
                bend = Vector3.ProjectOnPlane(Mathf.Abs(direction.y) < .9f ? Vector3.up : Vector3.right, direction);
            bend.Normalize();
            if (previous.sqrMagnitude > 1e-10f)
            {
                previous.Normalize();
                // Rotate in the plane explicitly, including the antipodal case (Vector3.Slerp is ambiguous there).
                float angle = Vector3.SignedAngle(previous, bend, direction);
                float limit = Mathf.Max(0, bendRadians) * Mathf.Rad2Deg;
                bend = Quaternion.AngleAxis(Mathf.Clamp(angle, -limit, limit), direction) * previous;
            }
            float along = (upperLength * upperLength + distance * distance - lowerLength * lowerLength) / (2 * distance);
            result.Elbow = origin + direction * along + bend * Mathf.Sqrt(Mathf.Max(0, upperLength * upperLength - along * along));
            result.Wrist = origin + direction * distance;
            result.Bend = bend;
            result.Residual = Vector3.Distance(target, result.Wrist);
            result.Valid = true;
            return result;
        }

        public static void ReachLimits(float upperLength, float lowerLength, float maximumReach, out float minimum, out float maximum)
        {
            float total = upperLength + lowerLength;
            float clearance = Mathf.Min(total * .0001f, Mathf.Min(upperLength, lowerLength) * .1f);
            minimum = Mathf.Abs(upperLength - lowerLength) + clearance;
            maximum = Mathf.Max(minimum, total * Mathf.Clamp(maximumReach, .5f, .9999f));
        }

        /// <summary>Extract only roll about the forearm; swing remains owned by the two-bone geometry.</summary>
        public static Quaternion ForearmTwist(Quaternion currentHand, Quaternion desiredHand, Vector3 axis, float maximumDegrees)
        {
            if (axis.sqrMagnitude < 1e-10f) return Quaternion.identity;
            axis.Normalize();
            Quaternion delta = desiredHand * Quaternion.Inverse(currentHand);
            Vector3 projected = axis * Vector3.Dot(new Vector3(delta.x, delta.y, delta.z), axis);
            float length = Mathf.Sqrt(projected.sqrMagnitude + delta.w * delta.w);
            // A 180-degree pure swing has no defined twist component.
            if (length < .00001f) return Quaternion.identity;
            var twist = new Quaternion(projected.x / length, projected.y / length, projected.z / length, delta.w / length);
            return Quaternion.RotateTowards(Quaternion.identity, twist, Mathf.Clamp(maximumDegrees, 0, 90));
        }

        public static Quaternion ReachAssistance(Vector3 shoulder, Vector3 upper, Vector3 target,
            float armLength, float maximumDegrees, float weight)
        {
            if (armLength < .0001f || (upper - shoulder).sqrMagnitude < 1e-10f || (target - shoulder).sqrMagnitude < 1e-10f)
                return Quaternion.identity;
            float need = Mathf.InverseLerp(armLength * .9f, armLength, Vector3.Distance(upper, target));
            var toward = Quaternion.FromToRotation(upper - shoulder, target - shoulder);
            return Quaternion.RotateTowards(Quaternion.identity, toward, Mathf.Clamp(maximumDegrees, 0, 12) * need * Mathf.Clamp01(weight));
        }

        /// <summary>Smallest bounded torso yaw that brings a stock-anchored wrist within reach.
        /// The shoulder-to-stock vector rotates with the torso; stock-to-wrist retains weapon aim.</summary>
        public static float StockReachYaw(Vector3 shoulderToStock, Vector3 stockToWrist, Vector3 up,
            float reach, float maximumDegrees)
        {
            if ((shoulderToStock + stockToWrist).magnitude <= reach || up.sqrMagnitude < .5f) return 0;
            up.Normalize();
            Vector3 a = Vector3.ProjectOnPlane(shoulderToStock, up);
            Vector3 b = Vector3.ProjectOnPlane(stockToWrist, up);
            if (a.sqrMagnitude < 1e-10f || b.sqrMagnitude < 1e-10f) return 0;
            float best = Mathf.Clamp(Vector3.SignedAngle(a, -b, up), -maximumDegrees, maximumDegrees);
            if ((Quaternion.AngleAxis(best, up) * shoulderToStock + stockToWrist).magnitude >= reach) return best;
            // Reach is monotonic between zero and the nearest minimum. Keep the authored heading
            // as far as possible instead of always applying the full assistance angle.
            float low = 0, high = 1;
            for (int i = 0; i < 12; i++)
            {
                float middle = (low + high) * .5f;
                if ((Quaternion.AngleAxis(best * middle, up) * shoulderToStock + stockToWrist).magnitude > reach)
                    low = middle;
                else high = middle;
            }
            return best * high;
        }

        static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
