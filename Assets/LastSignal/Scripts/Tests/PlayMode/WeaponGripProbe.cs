#if UNITY_EDITOR
using UnityEngine;
namespace LastSignal.Tests
{
    [DefaultExecutionOrder(10000)]
    public sealed class WeaponGripProbe : MonoBehaviour
    {
        public FirstPersonWeaponGrip Fps;
        public CharacterWeaponRig World;
        public int Samples;
        public float FpsError, WorldError, Residual, Support, Reload;
        public float FpsSecondaryError, FpsSecondaryResidual, WorldSecondaryError, WorldRightResidual, WorldSecondaryResidual;
        public bool Active;
        public Vector3 Palm, LeftHand, Sight, SecondaryPalm, PrimaryContact, VisualLocalPosition;
        public Quaternion VisualLocalRotation;
        public Vector3 SilhouetteViewport, PrimaryPalmViewport, SecondaryPalmViewport;
        public float WristBend, SampledFov, SampledAspect;
        public readonly Vector3[] PistolFingerSegments = new Vector3[30];
        public Vector3 MagazineLocalPosition;
        public float WorldRightPenetration, WorldLeftPenetration, WorldHipClearanceDegrees, WorldStockError;
        public string WorldAnatomy;
        void SampleWorld()
        {
            if (!World || !World.ActiveProfile || !World.WeaponVisual || !World.WeaponVisual.activeSelf) return;
            WorldError = World.RightGripError; Support = World.SupportHandWeight;
            WorldSecondaryError = World.LeftGripError; WorldRightResidual = World.RightReachResidual; WorldSecondaryResidual = World.LeftReachResidual;
            WorldRightPenetration = World.RightTorsoPenetration; WorldLeftPenetration = World.LeftTorsoPenetration;
            WorldHipClearanceDegrees = World.HipClearanceDegrees;
            WorldStockError = World.Sockets && World.Sockets.StockContact ? Vector3.Distance(World.StockTarget, World.Sockets.StockContact.position) : 0;
            var torso = World.TorsoClearance;
            var animator = World.GetComponent<Animator>();
            string Bone(HumanBodyBones bone) => torso.Local(animator.GetBoneTransform(bone).position).ToString("F4");
            WorldAnatomy = $"profile={World.ActiveProfile.name}, frame={Time.frameCount}, radii={torso.Radii:F4}, torsoRotation={torso.Rotation.eulerAngles:F2}, " +
                $"R upper/elbow/wrist={Bone(HumanBodyBones.RightUpperArm)}/{Bone(HumanBodyBones.RightLowerArm)}/{Bone(HumanBodyBones.RightHand)}, " +
                $"L upper/elbow/wrist={Bone(HumanBodyBones.LeftUpperArm)}/{Bone(HumanBodyBones.LeftLowerArm)}/{Bone(HumanBodyBones.LeftHand)}, " +
                $"requested R/L={torso.Local(World.RightWristTarget):F4}/{torso.Local(World.LeftWristTarget):F4}, " +
                $"contact R/L={WorldError:F5}/{WorldSecondaryError:F5}, residual R/L={WorldRightResidual:F5}/{WorldSecondaryResidual:F5}, adjustment={World.MeleePlacementCorrection:F5}, hipAngle={WorldHipClearanceDegrees:F3}, stockError={WorldStockError:F5}, actorRotation={World.transform.rotation.eulerAngles:F2}, grounded={GetComponent<FirstPersonMotor>().Grounded}, sliding={GetComponent<FirstPersonMotor>().IsSliding}, worldSlideOffset={World.ActiveProfile.WorldMeleeSlideOffset:F4}, animatorState={animator.GetCurrentAnimatorStateInfo(0).fullPathHash}";
        }
        void LateUpdate()
        {
            Samples++;
            SampleWorld();
            Active = Fps && Fps.HasPose;
            if (!Active) return;
            FpsSecondaryError = Fps.SecondaryContactError; FpsSecondaryResidual = Fps.SecondaryReachResidual;
            FpsError = Fps.ContactError; Residual = Fps.ReachResidual; Reload = Fps.ReloadAssist;
            SecondaryPalm = Fps.SecondaryPalmPosition; PrimaryContact = Fps.Sockets.PrimaryGrip.position;
            VisualLocalPosition = Fps.Visual.localPosition; VisualLocalRotation = Fps.Visual.localRotation;
            Palm = Fps.PalmPosition; LeftHand = Fps.LeftHand.position;
            WristBend = Fps.WristBendDegrees;
            if (Fps.Profile.Pistol)
            {
                var model = Fps.Sockets.PrimaryGrip.parent;
                for (int i=0;i<15;i++)
                {
                    var bone=Fps.RightFingers[i];
                    PistolFingerSegments[i*2]=model.InverseTransformPoint(bone.position);
                    // Arberry has no terminal tip transforms. Match the bind-relative
                    // distal endpoint used by its authoring, then follow the live joint rotation.
                    Vector3 endpoint = bone.childCount > 0 ? bone.GetChild(0).position :
                        bone.TransformPoint(Quaternion.Inverse(Fps.RightFingerRest[i]) * (bone.localPosition * .75f));
                    PistolFingerSegments[i*2+1]=model.InverseTransformPoint(endpoint);
                }
                if(Fps.Sockets.Magazine)MagazineLocalPosition=Fps.Sockets.Magazine.localPosition;
            }
            var look = GetComponent<FirstPersonLook>();
            if (look && look.View)
            {
                PrimaryPalmViewport = look.View.WorldToViewportPoint(Palm);
                SecondaryPalmViewport = look.View.WorldToViewportPoint(SecondaryPalm);
            }
            if (look && look.View && Fps.Sockets.SilhouetteReference)
            {
                SilhouetteViewport = look.View.WorldToViewportPoint(Fps.Sockets.SilhouetteReference.position);
                SampledFov = look.View.fieldOfView; SampledAspect = look.View.aspect;
            }
            if (Fps.Sockets.AimReference) Sight = Fps.Sockets.AimReference.position;

        }
    }
}
#endif
