using UnityEngine;
using LastSignal.Vehicles;
namespace LastSignal
{
    /// <summary>Visual pelvis alignment only; vehicle still owns seat, capsule, camera and exit.</summary>
    [DefaultExecutionOrder(250), RequireComponent(typeof(Animator))]
    public sealed class CharacterSeatPresentation : MonoBehaviour
    {
        [SerializeField] Vector3 seatedHipOffset;
        [SerializeField, Min(.01f)] float steeringRadius = .16f;
        Animator animator;
        PlayerLocomotionPresenter state;
        Transform hips;
        Transform[] upper, lower, hands;
        VehicleSteeringWheelPresenter wheel;
        bool wasSeated;
        Vector3 standingPosition;
        void Awake()
        {
            animator=GetComponent<Animator>(); state=GetComponent<PlayerLocomotionPresenter>();
            standingPosition=transform.localPosition;
            if(!animator.isHuman||!state){enabled=false;return;}
            hips=animator.GetBoneTransform(HumanBodyBones.Hips);
            upper=new[]{animator.GetBoneTransform(HumanBodyBones.LeftUpperArm),animator.GetBoneTransform(HumanBodyBones.RightUpperArm)};
            lower=new[]{animator.GetBoneTransform(HumanBodyBones.LeftLowerArm),animator.GetBoneTransform(HumanBodyBones.RightLowerArm)};
            hands=new[]{animator.GetBoneTransform(HumanBodyBones.LeftHand),animator.GetBoneTransform(HumanBodyBones.RightHand)};
        }
        void LateUpdate()
        {
            bool seated=state&&state.SeatedNow;
            if(seated&&!wasSeated)wheel=GetComponentInParent<VehicleSteeringWheelPresenter>();
            if(seated&&hips&&transform.parent)
            {
                Vector3 target=transform.parent.TransformPoint(seatedHipOffset);
                transform.position+=target-hips.position;
            }
            else if(wasSeated){transform.localPosition=standingPosition;wheel=null;}
            wasSeated=seated;
        }
        void OnAnimatorIK(int layer)
        {
            if(layer!=0||!state||!state.SeatedNow)return;
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,0);animator.SetIKRotationWeight(AvatarIKGoal.LeftHand,0);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand,0);animator.SetIKRotationWeight(AvatarIKGoal.RightHand,0);
            animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow,0);animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow,0);
            if(!state.Alive||!wheel||!wheel.Pivot)return;
            if(hips&&transform.parent)transform.position+=transform.parent.TransformPoint(seatedHipOffset)-hips.position;
            var pivot=wheel.Pivot;
            // Use the actual steering-wheel transform; no secondary steering simulation.
            Hand(AvatarIKGoal.LeftHand,pivot.TransformPoint(Vector3.left*steeringRadius),pivot.rotation*Quaternion.Euler(0,90,-90));
            Hand(AvatarIKGoal.RightHand,pivot.TransformPoint(Vector3.right*steeringRadius),pivot.rotation*Quaternion.Euler(0,-90,90));
        }
        void Hand(AvatarIKGoal goal,Vector3 position,Quaternion rotation)
        {
            int side=goal==AvatarIKGoal.LeftHand?0:1;
            if(upper==null||!upper[side]||!lower[side]||!hands[side])return;
            float reach=(Vector3.Distance(upper[side].position,lower[side].position)+Vector3.Distance(lower[side].position,hands[side].position))*.97f;
            Vector3 delta=position-upper[side].position;
            float weight=1-Mathf.InverseLerp(.02f,.18f,Mathf.Max(0,delta.magnitude-reach));
            position=upper[side].position+Vector3.ClampMagnitude(delta,reach);
            animator.SetIKPositionWeight(goal,.8f*weight);animator.SetIKRotationWeight(goal,.5f*weight);
            animator.SetIKPosition(goal,position);animator.SetIKRotation(goal,rotation);
        }
        void OnDisable(){transform.localPosition=standingPosition;wheel=null;wasSeated=false;}
    }
}
