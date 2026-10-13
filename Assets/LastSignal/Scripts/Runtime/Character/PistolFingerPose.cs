using UnityEngine;

namespace LastSignal
{
    /// <summary>One calibrated right-hand skeleton. Thumb/index/middle/ring/little, three joints each.</summary>
    [CreateAssetMenu(menuName = "Last Signal/Character/Pistol Finger Pose")]
    public sealed class PistolFingerPose : ScriptableObject
    {
        public string Rig;
        public Quaternion[] GripRelative = new Quaternion[15];
        public Quaternion[] TriggerRelative = new Quaternion[15];
        // Bone endpoints in weapon model metres, for editor inspection; not runtime IK targets.
        public Vector3[] AuthoredEndpoints = new Vector3[15];
        public bool Valid => ValidRotations(GripRelative) && ValidRotations(TriggerRelative);
        static bool ValidRotations(Quaternion[] values)
        {
            if (values == null || values.Length != 15) return false;
            foreach (var q in values)
            {
                float n = q.x*q.x + q.y*q.y + q.z*q.z + q.w*q.w;
                if (!float.IsFinite(n) || Mathf.Abs(n - 1) > .001f) return false;
            }
            return true;
        }
        public Quaternion Sample(int joint, float trigger) =>
            Quaternion.Slerp(GripRelative[joint], TriggerRelative[joint], Mathf.Clamp01(trigger));
    }
}
