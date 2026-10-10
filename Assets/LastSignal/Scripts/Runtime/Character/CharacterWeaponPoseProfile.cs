using UnityEngine;

namespace LastSignal
{
    [CreateAssetMenu(menuName = "Last Signal/Character/Weapon Pose")]
    public sealed class CharacterWeaponPoseProfile : ScriptableObject
    {
        public WeaponDefinition Firearm;
        public MeleeWeaponDefinition Melee;
        public GameObject WorldPrefab;
        [Header("Stock contact relative to right shoulder (metres / degrees)")]
        [Min(.1f)] public float WorldScale = .85f;
        public Vector3 HipStockOffset = new Vector3(-.02f, -.035f, .08f);
        public Vector3 HipEuler = new Vector3(10, 0, 0);
        public Vector3 AimStockOffset = new Vector3(-.06f, .06f, .08f);
        public Vector3 AimEuler;
        [Range(0, 60)] public float TorsoYaw = 35;
        [Range(0, 1)] public float TorsoPitchWeight = .6f;
        [Min(.01f)] public float PoseBlendSeconds = .15f;
        public Vector3 SprintEuler = new Vector3(35, -15, 5);
        public Vector3 SlideEuler = new Vector3(18, -12, 8);
        [Header("Melee: right hand socket")]
        public Vector3 HandPosition;
        public Vector3 HandEuler;
        [Header("IK: elbow hints relative to chest")]
        public Vector3 RightElbow = new Vector3(.4f, -.3f, .05f);
        public Vector3 LeftElbow = new Vector3(-.4f, -.3f, .15f);
        [Range(0, 1)] public float HandWeight = 1;
        [Range(0, 1)] public float FingerGripWeight = 1;
        [Range(0, 1)] public float ElbowWeight = .65f;
        [Range(.5f, 1)] public float MaximumReach = .97f;
        [Min(0)] public float RecoilDistance = .025f;
        [Min(.01f)] public float RecoilRecovery = 14;
    }
}
