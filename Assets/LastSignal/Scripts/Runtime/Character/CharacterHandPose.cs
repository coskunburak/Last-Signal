using System;
using UnityEngine;

namespace LastSignal
{
    /// <summary>Avatar-specific wrist/palm calibration and finger flexion, independent of imported bone axes.</summary>
    public sealed class CharacterHandPose
    {
        readonly Transform hand;
        readonly Quaternion handToPalm;
        readonly Vector3 palmLocal;
        readonly Joint[] joints = new Joint[15];
        bool applied;
        struct Joint
        {
            public Transform Bone;
            public Quaternion Bind, Previous, Opposition;
            public Vector3 FlexAxis;
        }
        public bool Valid { get; }
        public CharacterHandPose(Animator animator, bool left)
        {
            hand = animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            var middle = animator.GetBoneTransform(left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal);
            var index = animator.GetBoneTransform(left ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal);
            var little = animator.GetBoneTransform(left ? HumanBodyBones.LeftLittleProximal : HumanBodyBones.RightLittleProximal);
            if (!hand || !middle || !index || !little) return;
            Vector3 forward = (middle.position - hand.position).normalized;
            Vector3 normal = Vector3.Cross(index.position - little.position, forward).normalized * (left ? 1 : -1);
            if (normal.sqrMagnitude < .5f) return;
            handToPalm = Quaternion.Inverse(hand.rotation) * Quaternion.LookRotation(forward, normal);
            // Palm contact is between wrist and knuckles, on the palmar surface, not at the wrist joint.
            palmLocal = hand.InverseTransformPoint(Vector3.Lerp(hand.position, middle.position, .65f) + normal * .012f);
            string[] fingers = { "Thumb", "Index", "Middle", "Ring", "Little" };
            string[] segments = { "Proximal", "Intermediate", "Distal" };
            for (int f = 0; f < 5; f++)
            for (int s = 0; s < 3; s++)
            {
                var bone = animator.GetBoneTransform((HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),
                    (left ? "Left" : "Right") + fingers[f] + segments[s]));
                if (!bone) return;
                Vector3 direction = bone.childCount > 0 ? bone.GetChild(0).position - bone.position : forward;
                Vector3 flex = Vector3.Cross(direction.normalized, normal).normalized;
                var opposition = Quaternion.identity;
                if (f == 0 && s == 0)
                {
                    var turn = Quaternion.FromToRotation(direction, middle.position - bone.position);
                    turn = Quaternion.RotateTowards(Quaternion.identity, turn, 30);
                    opposition = Quaternion.Inverse(bone.rotation) * turn * bone.rotation;
                }
                joints[f * 3 + s] = new Joint { Bone = bone, Bind = bone.localRotation,
                    FlexAxis = bone.InverseTransformDirection(flex), Opposition = opposition };
            }
            Valid = true;
        }
        public void WristPose(Vector3 contact, Quaternion palmRotation, out Vector3 position, out Quaternion rotation)
        {
            rotation = palmRotation * Quaternion.Inverse(handToPalm);
            position = contact - rotation * Vector3.Scale(palmLocal, hand.lossyScale);
        }
        public void Apply(float weight, bool triggerHand, float triggerPull)
        {
            if (!Valid) return;
            for (int i = 0; i < joints.Length; i++)
            {
                ref var joint = ref joints[i];
                if (!joint.Bone) continue;
                int finger = i / 3, segment = i % 3;
                float curl = segment == 0 ? 55 : segment == 1 ? 70 : 45;
                if (finger == 0) curl = segment == 0 ? 20 : segment == 1 ? 35 : 25;
                if (triggerHand && finger == 1) curl = (segment == 0 ? 18 : segment == 1 ? 32 : 18) + triggerPull * 12;
                joint.Previous = joint.Bone.localRotation;
                var grip = joint.Bind * joint.Opposition * Quaternion.AngleAxis(curl, joint.FlexAxis);
                joint.Bone.localRotation = Quaternion.Slerp(joint.Previous, grip, Mathf.Clamp01(weight));
            }
            applied = true;
        }
        public void Restore()
        {
            if (!applied) return;
            foreach (var joint in joints) if (joint.Bone) joint.Bone.localRotation = joint.Previous;
            applied = false;
        }
    }
}
