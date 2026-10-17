using UnityEngine;

namespace LastSignal
{
    // Zero preserves existing serialized rifle/crowbar assets until explicitly migrated.
    public enum WeaponGripMode { Legacy = 0, OneHandedPistol, OneHandedLightMelee, OneHandedHeavyMelee, TwoHandedRifle, TwoHandedHeavyMelee }
    public enum WeaponPrimaryHand { Right = 0 }
    public enum WeaponPoseOwnership { AnimatorHand = 0, ProfileArmTargets }

    [CreateAssetMenu(menuName = "Last Signal/Character/Weapon Pose")]
    public sealed class CharacterWeaponPoseProfile : ScriptableObject
    {
        public WeaponGripMode GripMode;
        public WeaponPrimaryHand PrimaryHand;
        public WeaponPoseOwnership AttackOwnership;
        public bool TwoHandedMelee => GripMode == WeaponGripMode.TwoHandedHeavyMelee;
        [Tooltip("Bounded world melee rotation about the primary wrist when translation alone cannot clear both sleeves.")]
        [Range(0,12)] public float MeleeClearanceRotation = 0;
        [Tooltip("Two-hand world grip displacement during slide, in the calibrated torso frame. FPS has its own stance pivot.")]
        public Vector3 WorldMeleeSlideOffset = new Vector3(0,-.02f,0);
        public bool ProceduralGrip => SingleHanded || TwoHandedMelee;
        public bool SingleHanded => GripMode == WeaponGripMode.OneHandedPistol ||
            GripMode == WeaponGripMode.OneHandedLightMelee || GripMode == WeaponGripMode.OneHandedHeavyMelee;
        public bool ValidGripData => PrimaryHand == WeaponPrimaryHand.Right &&
            float.IsFinite(MeleeClearanceRotation) && MeleeClearanceRotation >= 0 && MeleeClearanceRotation <= 12 &&
            float.IsFinite(WorldScale) && WorldScale > 0 && float.IsFinite(PoseBlendSeconds) && PoseBlendSeconds > 0 &&
            Finite(HipGripOffset) && Finite(AimGripOffset) && Finite(HipEuler) && Finite(AimEuler) &&
            Finite(FpsHipPosition) && Finite(FpsHipEuler) && Finite(FpsRightShoulder) && Finite(FpsLeftShoulder) &&
            (!FpsWeaponOwnsContacts || (Finite(FpsRightElbow) && Finite(FpsLeftElbow))) &&
            Finite(FpsRaisedPosition) && Finite(FpsRaisedEuler) && Finite(RaisedGripOffset) && Finite(RaisedEuler) &&
            Finite(FollowThroughOffset) && Finite(FollowThroughEuler) && Finite(FingerFlexion) && Finite(ThumbFlexion) && Finite(TriggerFlexion) &&
            float.IsFinite(ThumbOpposition) && ThumbOpposition >= 0 && ThumbOpposition <= 30 &&
            float.IsFinite(FingerClosure) && FingerClosure >= 0 && FingerClosure <= 1 &&
            float.IsFinite(ReadyHoldSeconds) && ReadyHoldSeconds >= 0 && float.IsFinite(FollowThroughFraction) && FollowThroughFraction > 0 && FollowThroughFraction < 1 &&
            (!FpsSprintCarry || (Finite(FpsSprintPosition) && Finite(FpsSprintEuler) &&
                float.IsFinite(FpsSprintBobAmplitude) && FpsSprintBobAmplitude >= 0 && FpsSprintBobAmplitude <= .02f)) &&
            (!FpsTwoHandedPistol || ValidFpsPistolSupport) &&
            Finite(FpsOffhandPosition) && Finite(WindupOffset) && Finite(StrikeOffset) && Finite(WindupEuler) && Finite(StrikeEuler) &&
            (!TwoHandedMelee || (Melee && Melee.Valid && AttackOwnership == WeaponPoseOwnership.ProfileArmTargets &&
                Finite(SecondaryFingerFlexion) && Finite(SecondaryThumbFlexion) && float.IsFinite(SecondaryThumbOpposition) &&
                SecondaryThumbOpposition >= 0 && SecondaryThumbOpposition <= 30 &&
                Finite(CrouchOffset) && Finite(SprintOffset) && Finite(SprintEuler) && Finite(SlideEuler) && Finite(WorldMeleeSlideOffset) &&
                Finite(RightElbow) && Finite(LeftElbow) && float.IsFinite(MaximumReach) && MaximumReach >= .5f && MaximumReach <= 1 &&
                float.IsFinite(ElbowTurnDegreesPerSecond) && ElbowTurnDegreesPerSecond > 0 &&
                float.IsFinite(ForearmTwistDegrees) && ForearmTwistDegrees >= 0 && ForearmTwistDegrees <= 90 &&
                float.IsFinite(ShoulderReachDegrees) && ShoulderReachDegrees >= 0 && ShoulderReachDegrees <= 12)) &&
            Finite(PistolReloadPosition) && Finite(PistolReloadEuler) && float.IsFinite(PistolMagazineTravel) && PistolMagazineTravel >= 0;
        static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
        public bool Pistol => GripMode == WeaponGripMode.OneHandedPistol;
        [Header("TPS pistol support")]
        [Tooltip("Enables the support hand only in CharacterWeaponRig. FPS is unchanged.")]
        public bool WorldTwoHandedPistol;

        [Range(0, 1)]
        public float WorldSupportFingerClosure = .65f;

        public bool ValidWorldPistolSupport =>
            Pistol && WorldTwoHandedPistol &&
            float.IsFinite(PoseBlendSeconds) && PoseBlendSeconds > 0 &&
            float.IsFinite(WorldSupportFingerClosure) &&
            WorldSupportFingerClosure >= 0 && WorldSupportFingerClosure <= 1 &&
            float.IsFinite(HandWeight) && HandWeight >= 0 && HandWeight <= 1 &&
            float.IsFinite(FingerGripWeight) &&
            FingerGripWeight >= 0 && FingerGripWeight <= 1 &&
            float.IsFinite(ElbowWeight) && ElbowWeight >= 0 && ElbowWeight <= 1 &&
            float.IsFinite(MaximumReach) && MaximumReach >= .5f && MaximumReach <= 1 &&
            float.IsFinite(ElbowTurnDegreesPerSecond) && ElbowTurnDegreesPerSecond > 0 &&
            float.IsFinite(ForearmTwistDegrees) &&
            ForearmTwistDegrees >= 0 && ForearmTwistDegrees <= 90 &&
            Finite(LeftElbow) &&
            Finite(SecondaryFingerFlexion) &&
            Finite(SecondaryThumbFlexion) &&
            float.IsFinite(SecondaryThumbOpposition) &&
            SecondaryThumbOpposition >= 0 && SecondaryThumbOpposition <= 30;

        [Header("FPS pistol support, relative to primary palm in metres")]
        public bool FpsTwoHandedPistol;
        [Tooltip("Character palm correction in primary socket axes/metres. Leaves the weapon and sights fixed.")]
        public Vector3 FpsPrimaryPalmOffset;
        public Vector3 FpsSupportPalmOffset = new Vector3(0, .045f, -.014f);
        public Vector3 FpsSupportPalmEuler = new Vector3(0, 0, 180);
        [Range(0, 1)] public float FpsSupportFingerClosure = .65f;
        public bool ValidFpsPistolSupport => Pistol && FpsTwoHandedPistol &&
            Finite(FpsPrimaryPalmOffset) && Finite(FpsSupportPalmOffset) && Finite(FpsSupportPalmEuler) &&
            Finite(FpsRightElbow) && Finite(FpsLeftElbow) &&
            float.IsFinite(FpsSupportFingerClosure) && FpsSupportFingerClosure >= 0 && FpsSupportFingerClosure <= 1 &&
            Finite(SecondaryFingerFlexion) && Finite(SecondaryThumbFlexion) &&
            float.IsFinite(SecondaryThumbOpposition) && SecondaryThumbOpposition >= 0 && SecondaryThumbOpposition <= 30;

        [Header("FPS sprint carry (camera space, weapon-owned contacts)")]
        public bool FpsSprintCarry;
        public Vector3 FpsSprintPosition = new Vector3(.06f, -.17f, .26f);
        public Vector3 FpsSprintEuler = new Vector3(20, -12, -8);
        [Range(0, .02f)] public float FpsSprintBobAmplitude = .006f;

        [Header("Pistol skeleton-specific bind-relative finger poses")]
        public PistolFingerPose FpsPistolFingers, WorldPistolFingers;
        [Tooltip("Keep FPS weapons on their authored sight/contact frame. Unreachable hands must not drag the weapon off ADS.")]
        public bool FpsWeaponOwnsContacts;
        public bool TwoHandedFirearm => GripMode == WeaponGripMode.TwoHandedRifle;
        [Header("FPS-only elbow hints in camera metres")]
        public Vector3 FpsRightElbow = new Vector3(.34f,-.35f,.05f);
        public Vector3 FpsLeftElbow = new Vector3(-.34f,-.35f,.08f);
        [Min(0)] public float PistolMagazineTravel = .08f;
        public Vector3 PistolReloadPosition = new Vector3(-.015f,.025f,0);
        public Vector3 PistolReloadEuler = new Vector3(0,-8,12);
        [Header("Single-hand primary contact relative to right shoulder (actor metres)")]
        public Vector3 HipGripOffset = new Vector3(.02f, -.20f, .36f);
        public Vector3 AimGripOffset = new Vector3(-.06f, -.06f, .43f);
        [Header("FPS visual root pose (camera metres); mesh sockets define contact")]
        public Vector3 FpsHipPosition = new Vector3(.14f, -.18f, .36f);
        public Vector3 FpsHipEuler;
        [Header("Pistol ready response; accepted shots raise presentation only")]
        public Vector3 FpsRaisedPosition = new Vector3(.12f, -.13f, .30f);
        public Vector3 FpsRaisedEuler = new Vector3(3, 0, -3);
        public Vector3 RaisedGripOffset = new Vector3(.02f, -.17f, .36f);
        public Vector3 RaisedEuler = new Vector3(4, 0, -4);
        [Min(0)] public float ReadyHoldSeconds = 1.1f;
        public float StepReady(float weight, float secondsLeft, float dt) => Mathf.MoveTowards(weight,
            secondsLeft > 0 ? 1 : 0, Mathf.Max(0, dt) / Mathf.Max(.01f, PoseBlendSeconds));
        public void ReadyPose(float weight, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.Lerp(FpsHipPosition, FpsRaisedPosition, Mathf.Clamp01(weight));
            rotation = Quaternion.Slerp(Quaternion.Euler(FpsHipEuler), Quaternion.Euler(FpsRaisedEuler), Mathf.Clamp01(weight));
        }
        public Vector3 FpsRightShoulder = new Vector3(.20f, -.18f, -.12f);
        public Vector3 FpsLeftShoulder = new Vector3(-.20f, -.18f, -.12f);
        public Vector3 FpsOffhandPosition = new Vector3(-.22f, -.36f, .10f);
        [Min(.1f)] public float SightDistance = .32f;
        public Vector3 CrouchOffset = new Vector3(0, -.025f, -.015f);
        public Vector3 SprintOffset = new Vector3(0, -.07f, -.04f);
        [Header("Melee contact motion; phases read from combat timing")]
        public Vector3 WindupOffset, StrikeOffset;
        public Vector3 WindupEuler, StrikeEuler;
        public Vector3 FollowThroughOffset, FollowThroughEuler;
        [Range(.05f, .5f)] public float FollowThroughFraction = .22f;
        [Header("Finger flexion in anatomical joint axes, proximal / middle / distal")]
        public Vector3 FingerFlexion = new Vector3(55, 70, 45);
        public Vector3 ThumbFlexion = new Vector3(20, 35, 25);
        public Vector3 TriggerFlexion = new Vector3(18, 32, 18);
        [Range(0, 30)] public float ThumbOpposition = 25;
        [Range(0, 1)] public float FingerClosure = 1;
        [Header("Heavy melee support-hand flexion; independent palmar orientation")]
        public Vector3 SecondaryFingerFlexion = new Vector3(58, 76, 48);
        public Vector3 SecondaryThumbFlexion = new Vector3(25, 38, 26);
        [Range(0, 30)] public float SecondaryThumbOpposition = 28;
        public void AttackPose(float progress, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero; rotation = Quaternion.identity;
            if (!Melee || progress <= 0) return;
            float total = Melee.WindupSeconds + Melee.ActiveSeconds + Melee.RecoverySeconds;
            float windup = Melee.WindupSeconds / total;
            float impact = (Melee.WindupSeconds + Melee.ActiveSeconds) / total;
            if (progress < windup) { float t = Mathf.SmoothStep(0, 1, progress / windup);
                position = WindupOffset * t; rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(WindupEuler), t); }
            else if (progress < impact) { float t = Mathf.Clamp01((progress - windup) / (impact - windup));
                // Accelerate through contact, instead of stopping at the end of the active window.
                position = Vector3.Lerp(WindupOffset, StrikeOffset, t * t);
                rotation = Quaternion.Slerp(Quaternion.Euler(WindupEuler), Quaternion.Euler(StrikeEuler), t * t); }
            else
            {
                float follow = impact + (1 - impact) * FollowThroughFraction;
                if (progress < follow)
                {
                    float t = Mathf.Clamp01((progress - impact) / (follow - impact));
                    Vector3 incoming = (StrikeOffset - WindupOffset) * (2 * (follow - impact) / (impact - windup));
                    position = Hermite(StrikeOffset, FollowThroughOffset, incoming, Vector3.zero, t);
                    rotation = Quaternion.Slerp(Quaternion.Euler(StrikeEuler), Quaternion.Euler(FollowThroughEuler), t * (2 - t));
                }
                else { float t = Mathf.SmoothStep(0, 1, (progress - follow) / (1 - follow));
                    position = Vector3.Lerp(FollowThroughOffset, Vector3.zero, t);
                    rotation = Quaternion.Slerp(Quaternion.Euler(FollowThroughEuler), Quaternion.identity, t); }
            }
        }
        public void SampleAttack(MeleeAttackState state, float dt, ref Vector3 position, ref Quaternion rotation)
        {
            if (state != null && state.State != MeleeState.Ready)
                AttackPose(state.PresentationProgress, out position, out rotation);
            else
            {
                float blend = 1 - Mathf.Exp(-Mathf.Max(0, dt) / Mathf.Max(.01f, PoseBlendSeconds));
                position = Vector3.Lerp(position, Vector3.zero, blend);
                rotation = Quaternion.Slerp(rotation, Quaternion.identity, blend);
            }
        }
        static Vector3 Hermite(Vector3 a, Vector3 b, Vector3 start, Vector3 end, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return (2 * t3 - 3 * t2 + 1) * a + (t3 - 2 * t2 + t) * start +
                (-2 * t3 + 3 * t2) * b + (t3 - t2) * end;
        }
        public WeaponDefinition Firearm;
        public MeleeWeaponDefinition Melee;
        public GameObject WorldPrefab;
        [Header("Stock contact in calibrated upper-chest axes (actor metres / degrees)")]
        [Min(.1f)] public float WorldScale = .85f;
        public Vector3 HipStockOffset = new Vector3(-.06f, -.015f, .045f);
        public Vector3 HipEuler = new Vector3(5, 0, 0);
        public Vector3 AimStockOffset = new Vector3(-.065f, .005f, .045f);
        public Vector3 AimEuler;
        [Tooltip("Target anatomical torso heading relative to the actor, in degrees; compensates locomotion clip yaw.")]
        [Range(0, 60)] public float TorsoYaw = 55;
        [Range(0, 1)] public float TorsoPitchWeight = .45f;
        [Min(.01f)] public float PoseBlendSeconds = .15f;
        public Vector3 SprintEuler = new Vector3(25, -8, 3);
        public Vector3 SlideEuler = new Vector3(15, -6, 5);
        [Header("Melee: right hand socket")]
        public Vector3 HandPosition;
        public Vector3 HandEuler;
        [Header("IK: elbow hints from chest in calibrated torso axes")]
        public Vector3 RightElbow = new Vector3(.32f, -.26f, .02f);
        public Vector3 LeftElbow = new Vector3(-.30f, -.25f, .16f);
        [Range(0, 1)] public float HandWeight = 1;
        [Range(0, 1)] public float FingerGripWeight = 1;
        [Range(0, 1)] public float ElbowWeight = 1;
        [Range(.5f, 1)] public float MaximumReach = .97f;
        [Tooltip("Maximum clavicle assistance in degrees, only near the arm reach limit. Zero disables it.")]
        [Range(0, 12)] public float ShoulderReachDegrees = 8;
        [Tooltip("Maximum additional torso yaw in degrees when the support wrist exceeds arm reach. Keeps stock contact and weapon aim.")]
        [Range(0, 12)] public float TorsoReachYawDegrees = 12;
        [Tooltip("Maximum elbow bend-plane turn speed in degrees per scaled second.")]
        [Min(1)] public float ElbowTurnDegreesPerSecond = 360;
        [Tooltip("Maximum wrist roll shared by the forearm in degrees. Swing and contact positions are unchanged.")]
        [Range(0, 90)] public float ForearmTwistDegrees = 60;
        [Tooltip("Bounded cosmetic muzzle rise per shot in degrees; does not change gameplay aim.")]
        [Range(0, 5)] public float RecoilPitch = 2;
        [Min(0)] public float RecoilDistance = .025f;
        [Min(.01f)] public float RecoilRecovery = 14;
    }
}
