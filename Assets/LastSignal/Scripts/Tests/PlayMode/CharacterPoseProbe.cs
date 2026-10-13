#if UNITY_EDITOR
using UnityEngine;

namespace LastSignal.Tests
{
    // Observes the production LateUpdate without invoking, replacing or mocking the solver or Animator.
    [DefaultExecutionOrder(10000)]
    public sealed class CharacterPoseProbe : MonoBehaviour
    {
        public CharacterWeaponRig Rig;
        public Animator Animator;
        public int Samples;
        public bool Active;
        public float StockError, RightError, LeftError, RightResidual, LeftResidual, Recoil, Weight;
        public Vector3 Stock, MuzzleDirection;
        public float SampleTime, DeltaSeconds, StateTime, LeftTargetDistance, LeftArmLength, LeftWristError, TorsoHeading;
        public int BaseState, NextBaseState;
        public bool Transitioning;
        public readonly Quaternion[] Rotations = new Quaternion[8];
        static readonly HumanBodyBones[] Bones = { HumanBodyBones.Chest, HumanBodyBones.UpperChest,
            HumanBodyBones.RightShoulder, HumanBodyBones.LeftShoulder, HumanBodyBones.RightLowerArm,
            HumanBodyBones.LeftLowerArm, HumanBodyBones.RightHand, HumanBodyBones.LeftHand };
        public string Describe() => $"t={SampleTime:F3}, dt={DeltaSeconds:F5}, weight={Weight:F3}, state={BaseState}, next={NextBaseState}, transition={Transitioning}, normalized={StateTime:F3}, torsoHeading={TorsoHeading:F1}, reachYaw={Rig.TorsoReachYaw:F2}, leftTarget={LeftTargetDistance:F4}m, arm={LeftArmLength:F4}m, residual={LeftResidual:F4}m, wristError={LeftWristError:F4}m";
        void LateUpdate()
        {
            if (!Rig || !Animator) return;
            Samples++; Active = Rig.HasFirearmPose;
            SampleTime = Time.time; DeltaSeconds = Time.deltaTime;
            var current = Animator.GetCurrentAnimatorStateInfo(0);
            BaseState = current.shortNameHash; StateTime = current.normalizedTime;
            Transitioning = Animator.IsInTransition(0); NextBaseState = Animator.GetNextAnimatorStateInfo(0).shortNameHash;
            Recoil = Rig.CosmeticRecoil; Weight = Rig.PoseWeight;
            for (int i = 0; i < Bones.Length; i++) Rotations[i] = Animator.GetBoneTransform(Bones[i]).localRotation;
            if (!Active) return;
            Stock = Rig.StockTarget; MuzzleDirection = Rig.Sockets.Muzzle.forward;
            StockError = Rig.Sockets.StockContact ? Vector3.Distance(Rig.StockTarget, Rig.Sockets.StockContact.position) : 0;
            RightError = Rig.RightGripError; LeftError = Rig.LeftGripError;
            RightResidual = Rig.RightReachResidual; LeftResidual = Rig.LeftReachResidual;
            var upper = Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var lower = Animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            var hand = Animator.GetBoneTransform(HumanBodyBones.LeftHand);
            LeftTargetDistance = Vector3.Distance(upper.position, Rig.LeftWristTarget);
            LeftArmLength = Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, hand.position);
            LeftWristError = Vector3.Distance(hand.position, Rig.LeftWristTarget);
            Vector3 shoulderLine = Rig.transform.InverseTransformDirection(
                Animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position - upper.position);
            TorsoHeading = Mathf.Atan2(-shoulderLine.z, shoulderLine.x) * Mathf.Rad2Deg;
        }
    }

}
#endif
