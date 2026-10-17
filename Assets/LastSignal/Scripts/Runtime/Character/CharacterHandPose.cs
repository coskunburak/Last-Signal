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
        readonly bool preserveAuthoredCurl;
        struct Joint
        {
            public Transform Bone;
            public Quaternion Bind, Previous, Opposition;
            public Vector3 FlexAxis;
        }
        public bool Valid { get; }
        public Vector3 PalmPosition => hand.TransformPoint(palmLocal);
        public Quaternion PalmRotation => hand.rotation * handToPalm;
        public CharacterHandPose(Animator animator, bool left)
            : this(animator,left,.012f) { }
        public CharacterHandPose(Animator animator, bool left, float palmDepth)
            : this(animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand),
                HumanoidFingers(animator, left), left, palmDepth * Mathf.Abs(animator.transform.lossyScale.x), false) { }
        static Transform[] HumanoidFingers(Animator animator, bool left)
        {
            var bones = new Transform[15];
            string[] fingers = { "Thumb", "Index", "Middle", "Ring", "Little" };
            string[] segments = { "Proximal", "Intermediate", "Distal" };
            for (int f = 0; f < 5; f++) for (int s = 0; s < 3; s++)
                bones[f * 3 + s] = animator.GetBoneTransform((HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),
                    (left ? "Left" : "Right") + fingers[f] + segments[s]));
            return bones;
        }
        // Generic FPS rigs supply explicit serialized bone references; no Humanoid assumption.
        public CharacterHandPose(Transform wrist, Transform[] fingers, bool left, float palmDepth, bool authoredCurl)
        {
            hand = wrist; preserveAuthoredCurl = authoredCurl;
            if (fingers == null || fingers.Length != 15) return;
            var middle = fingers[6]; var index = fingers[3]; var little = fingers[12];
            if (!hand || !middle || !index || !little) return;
            Vector3 forward = (middle.position - hand.position).normalized;
            Vector3 normal = Vector3.Cross(index.position - little.position, forward).normalized * (left ? 1 : -1);
            if (normal.sqrMagnitude < .5f) return;
            handToPalm = Quaternion.Inverse(hand.rotation) * Quaternion.LookRotation(forward, normal);
            // Palm contact is between wrist and knuckles, on the palmar surface, not at the wrist joint.
            palmLocal = hand.InverseTransformPoint(Vector3.Lerp(hand.position, middle.position, .65f) + normal * palmDepth);
            for (int f = 0; f < 5; f++)
            for (int s = 0; s < 3; s++)
            {
                var bone = fingers[f * 3 + s];
                if (!bone) return;
                Vector3 direction = bone.childCount > 0 ? bone.GetChild(0).position - bone.position
                    : bone.parent ? bone.position - bone.parent.position : forward;
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
        public void Apply(float weight, bool triggerHand, float triggerPull, float closure = 1, CharacterWeaponPoseProfile profile = null, PistolFingerPose authored = null, bool secondary = false)
        {
            if (!Valid) return;
            if (applied) Restore(); // A second presentation sample must not replace the original restoration snapshot.
            bool useAuthored = authored && authored.Valid;
            bool useSecondary = secondary && profile &&
                (profile.TwoHandedMelee || (profile.Pistol && (profile.WorldTwoHandedPistol || profile.FpsTwoHandedPistol)));
            for (int i = 0; i < joints.Length; i++)
            {
                ref var joint = ref joints[i];
                if (!joint.Bone) continue;
                int finger = i / 3, segment = i % 3;
                float curl = segment == 0 ? 55 : segment == 1 ? 70 : 45;
                if (finger == 0) curl = segment == 0 ? 20 : segment == 1 ? 35 : 25;
                if (triggerHand && finger == 1) curl = (segment == 0 ? 18 : segment == 1 ? 32 : 18) + triggerPull * 12;
                if (profile)
                {
                    var angles = finger == 0 ? profile.ThumbFlexion : triggerHand && finger == 1 ? profile.TriggerFlexion : profile.FingerFlexion;
                    if (useSecondary) angles = finger == 0 ? profile.SecondaryThumbFlexion : profile.SecondaryFingerFlexion;
                    curl = angles[segment] + (triggerHand && finger == 1 ? triggerPull * 12 : 0);
                }
                joint.Previous = joint.Bone.localRotation;
                var grip = preserveAuthoredCurl ? joint.Bind : joint.Bind * Quaternion.Slerp(Quaternion.identity, joint.Opposition, profile ? (useSecondary ? profile.SecondaryThumbOpposition : profile.ThumbOpposition) / 30f : 1) * Quaternion.AngleAxis(curl * Mathf.Clamp01(closure), joint.FlexAxis);
                if (useAuthored) grip = joint.Bind * authored.Sample(i, triggerPull);
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
