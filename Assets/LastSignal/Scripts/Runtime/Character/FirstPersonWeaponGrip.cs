using UnityEngine;

namespace LastSignal
{
    /// <summary>Final Generic-rig arm pass. Combat owns time; this component owns S021 arm contacts.</summary>
    [DefaultExecutionOrder(350), DisallowMultipleComponent]
    public sealed class FirstPersonWeaponGrip : MonoBehaviour
    {
        public CharacterWeaponPoseProfile Profile;
        public Transform PoseFrame, Visual;
        [Tooltip("Optional source-derived arm skeleton, anchored to the owning camera independently of weapon recoil/action lowering.")]
        public Transform AnatomicalRoot;
        public CharacterWeaponSockets Sockets;
        [SerializeField] CharacterWeaponRig.PresentationOverride[] profileOverrides;
        public CharacterWeaponPoseProfile ResolveProfile(CharacterWeaponPoseProfile source)
        {
            if (source && profileOverrides != null)
                foreach (var entry in profileOverrides)
                    if (entry.Source == source && entry.Character) return entry.Character;
            return source;
        }
        public void ConfigureOverrides(CharacterWeaponRig.PresentationOverride[] values) => profileOverrides = values;
        public Transform RightUpper, RightLower, RightHand, LeftUpper, LeftLower, LeftHand;
        public Transform[] RightFingers, LeftFingers;
        public bool AuthoredFingerCurl;
        [Min(0)] public float RightPalmDepth=.012f, LeftPalmDepth=.012f;
        public Quaternion[] RightFingerRest, LeftFingerRest;
        public bool ValidAuthoring
        {
            get
            {
                if (!Profile || !PoseFrame || !Visual || !Sockets || !Sockets.ValidFor(Profile) ||
                    !float.IsFinite(RightPalmDepth) || !float.IsFinite(LeftPalmDepth) || RightPalmDepth < 0 || LeftPalmDepth < 0 ||
                    !RightUpper || !RightLower || !RightHand || !LeftUpper || !LeftLower || !LeftHand ||
                    RightHand == LeftHand || !RightHand.IsChildOf(RightLower) || !RightLower.IsChildOf(RightUpper) ||
                    !LeftHand.IsChildOf(LeftLower) || !LeftLower.IsChildOf(LeftUpper) || (Visual.IsChildOf(RightHand) || Visual.IsChildOf(LeftHand))) return false;
                if (Profile.Pistol && (!Profile.FpsPistolFingers || !Profile.FpsPistolFingers.Valid)) return false;
                return ValidFingers(RightHand, RightFingers, RightFingerRest) && ValidFingers(LeftHand, LeftFingers, LeftFingerRest);
            }
        }
        static bool ValidFingers(Transform hand, Transform[] fingers, Quaternion[] rest)
        {
            if (fingers == null || rest == null || fingers.Length != 15 || rest.Length != 15) return false;
            for (int i = 0; i < 15; i++)
            {
                if (!fingers[i] || !fingers[i].IsChildOf(hand)) return false;
                for (int j = 0; j < i; j++) if (fingers[j] == fingers[i]) return false;
                var q = rest[i]; float norm = q.x*q.x + q.y*q.y + q.z*q.z + q.w*q.w;
                if (!float.IsFinite(norm) || Mathf.Abs(norm - 1) > .01f) return false;
            }
            return true;
        }
        CharacterHandPose Calibrate(Transform hand, Transform[] fingers, Quaternion[] rest, bool left)
        {
            var original = new Quaternion[fingers.Length];
            for (int i = 0; i < fingers.Length; i++) { original[i] = fingers[i].localRotation; fingers[i].localRotation = rest[i]; }
            try { return new CharacterHandPose(hand, fingers, left, left ? LeftPalmDepth : RightPalmDepth, false); }
            finally { for (int i = 0; i < fingers.Length; i++) fingers[i].localRotation = original[i]; }
        }
        CharacterHandPose rightPalm, leftPalm;
        Transform[] bones;
        Quaternion[] previous;
        Vector3[] previousPositions;
        Animator animator;
        float equip;
        Vector3 attackPosition;
        Quaternion attackRotation = Quaternion.identity;
        Quaternion offhandRotation;
        Vector3 visualPosition, visualScale, rightBend, leftBend;
        Quaternion visualRotation;
        Vector3 anatomicalPosition, previousAnatomicalPosition;
        Quaternion anatomicalRotation, previousAnatomicalRotation;
        bool captured, initialized;
        WeaponController firearm;
        WeaponViewPresenter weaponView;
        MeleeWeaponController melee;
        FirstPersonMotor motor;
        PlayerInputReader input;
        FirstPersonLook look;
        PlayerHealth health;
        PlayerSurvival survival;
        float sprint, pistolSupportWeight, pistolReloadBlend;
        public float SupportHandWeight => HasPose && Profile && (Profile.TwoHandedFirearm || Profile.TwoHandedMelee)
            ? 1 : pistolSupportWeight;
        public Vector3 SupportPalmTarget { get; private set; }
        public Vector3 PrimaryPalmTarget { get; private set; }
        bool SupportedPistol => Profile && Profile.ValidFpsPistolSupport;
        Vector3 magazineRest;
        public float ContactError { get; private set; }
        public float WristBendDegrees => initialized ? Vector3.Angle(rightPalm.PalmRotation * Vector3.forward, RightHand.position - RightLower.position) : 0;
        public float ReachResidual { get; private set; }
        public float SecondaryContactError { get; private set; }
        public float SecondaryReachResidual { get; private set; }
        public bool HasValidContact => HasPose && ContactError <= .02f && ReachResidual <= .02f &&
            (!(Profile.TwoHandedMelee || Profile.TwoHandedFirearm || (SupportedPistol && pistolSupportWeight >= .99f)) || (SecondaryContactError <= .02f && SecondaryReachResidual <= .02f));
        public Vector3 SecondaryPalmPosition => leftPalm != null && leftPalm.Valid ? leftPalm.PalmPosition : Vector3.zero;
        public float ReloadAssist { get; private set; }
        public bool HasPose { get; private set; }
        public Vector3 PalmPosition => rightPalm != null && rightPalm.Valid ? rightPalm.PalmPosition : Vector3.zero;
        public void BindVisual(CharacterWeaponPoseProfile profile, Transform visual, CharacterWeaponSockets sockets)
        {
            Restore(); Profile = ResolveProfile(profile); Visual = visual; Sockets = sockets;
            visualPosition = visual.localPosition; visualRotation = visual.localRotation; visualScale = visual.localScale;
            magazineRest = sockets.Magazine ? sockets.Magazine.localPosition : Vector3.zero;
            equip = sprint = pistolSupportWeight = pistolReloadBlend = 0; rightBend = leftBend = Vector3.zero;
            PrimaryPalmTarget = SupportPalmTarget = Vector3.zero;
        }
        void Awake()
        {
            animator = GetComponentInChildren<Animator>(true);
            weaponView = GetComponent<WeaponViewPresenter>();
            firearm = GetComponent<WeaponController>(); melee = GetComponent<MeleeWeaponController>();
            input = GetComponentInParent<PlayerInputReader>(); look = GetComponentInParent<FirstPersonLook>();
            motor = GetComponentInParent<FirstPersonMotor>(); health = GetComponentInParent<PlayerHealth>();
            survival = GetComponentInParent<PlayerSurvival>();
            if (!ValidAuthoring)
            { Debug.LogError("Incomplete FPS grip authoring.", this); enabled = false; return; }
            rightPalm = Calibrate(RightHand, RightFingers, RightFingerRest, false);
            leftPalm = Calibrate(LeftHand, LeftFingers, LeftFingerRest, true);
            if (!rightPalm.Valid || !leftPalm.Valid) { enabled = false; return; }
            bones = new[] { RightUpper, RightLower, RightHand, LeftUpper, LeftLower, LeftHand };
            previous = new Quaternion[bones.Length]; previousPositions = new Vector3[bones.Length];
            visualPosition = Visual.localPosition; visualRotation = Visual.localRotation; visualScale = Visual.localScale;
            if (AnatomicalRoot) { anatomicalPosition = AnatomicalRoot.localPosition; anatomicalRotation = AnatomicalRoot.localRotation; }
            offhandRotation = Quaternion.Inverse(PoseFrame.rotation) * LeftHand.rotation;
            if (Sockets.Magazine) magazineRest = Sockets.Magazine.localPosition;
            initialized = true;
        }
        void Update() => Restore();
        void OnDisable()
        {
            Restore(); sprint = equip = pistolSupportWeight = pistolReloadBlend = 0; attackPosition = Vector3.zero; attackRotation = Quaternion.identity;
            if (animator) animator.speed = 1; rightBend = leftBend = Vector3.zero;
            ReloadAssist = ContactError = ReachResidual = SecondaryContactError = SecondaryReachResidual = 0;
            PrimaryPalmTarget = SupportPalmTarget = Vector3.zero;
            if (initialized && Sockets && Sockets.Magazine) Sockets.Magazine.localPosition = magazineRest;
        }
        void Restore()
        {
            HasPose = false; ReloadAssist = 0;
            if (initialized && Sockets && Sockets.Magazine) Sockets.Magazine.localPosition = magazineRest;
            rightPalm?.Restore(); leftPalm?.Restore();
            if (!captured) return;
            for (int i = 0; i < bones.Length; i++) if (bones[i]) { bones[i].localRotation = previous[i]; bones[i].localPosition = previousPositions[i]; }
            if (Visual) { Visual.localPosition = visualPosition; Visual.localRotation = visualRotation; Visual.localScale = visualScale; }
            if (AnatomicalRoot) AnatomicalRoot.SetLocalPositionAndRotation(previousAnatomicalPosition, previousAnatomicalRotation);
            captured = false;
        }
        void LateUpdate()
        {
            if (!initialized || rightPalm == null || !rightPalm.Valid || !Profile || !Sockets || !Sockets.ValidFor(Profile) ||
                (input && input.InVehicle) || (health && !health.IsAlive) ||
                (survival && survival.ApplyingTreatment && !Profile.FpsWeaponOwnsContacts)) return;
            if (firearm && (firearm.RuntimeState == null || firearm.RuntimeState.State == WeaponState.Holstered)) return;
            if (melee && !melee.isActiveAndEnabled) return;
            for (int i = 0; i < bones.Length; i++) { previous[i] = bones[i].localRotation; previousPositions[i] = bones[i].localPosition; }
            if (AnatomicalRoot) { previousAnatomicalPosition = AnatomicalRoot.localPosition; previousAnatomicalRotation = AnatomicalRoot.localRotation; }
            captured = true;
            float dt = firearm ? (firearm.IsSimulationActive ? Time.deltaTime : 0) : (melee && melee.Allowed ? Time.deltaTime : 0);
            if (animator) animator.speed = dt > 0 ? 1 : 0;
            equip = Mathf.MoveTowards(equip, 1, dt / Mathf.Max(.01f, Profile.PoseBlendSeconds));
            // Camera-space shoulder anchors keep real open mesh ends behind the eye while arms articulate.
            // The visual root can ADS/recoil without translating the anatomical shoulder with the gun.
            Transform anchor = look && look.View ? look.View.transform : PoseFrame.parent;
            if (anchor)
            {
                if (AnatomicalRoot) AnatomicalRoot.SetPositionAndRotation(anchor.TransformPoint(anatomicalPosition), anchor.rotation * anatomicalRotation);
                RightUpper.position = anchor.TransformPoint(Profile.FpsRightShoulder);
                LeftUpper.position = anchor.TransformPoint(Profile.FpsLeftShoulder);
            }
            sprint = firearm && weaponView && Profile.FpsSprintCarry ? weaponView.SprintWeight :
                Mathf.MoveTowards(sprint, motor && motor.IsSprinting ? 1 : 0, dt / Profile.PoseBlendSeconds);
            if (melee)
            {
                Profile.SampleAttack(melee.Simulation, dt, ref attackPosition, ref attackRotation);
                Vector3 position = Profile.FpsHipPosition + attackPosition + Profile.SprintOffset * sprint + Vector3.down * (.10f * (1 - equip));
                Quaternion orientation = Quaternion.Euler(Profile.FpsHipEuler) * attackRotation * Quaternion.Euler(Profile.SprintEuler * sprint);
                CharacterWeaponSockets.Attach(Visual, Sockets.PrimaryGrip,
                    PoseFrame.TransformPoint(position), PoseFrame.rotation * orientation * Quaternion.Euler(0, 0, 90));
            }
            else if (firearm)
            {
                float lower = firearm.RuntimeState.State == WeaponState.Equipping ?
                    1 - Mathf.Clamp01(firearm.RuntimeState.StateTimer / firearm.Definition.EquipSeconds) :
                    firearm.RuntimeState.State == WeaponState.Unequipping ? Mathf.Clamp01(firearm.RuntimeState.StateTimer / firearm.Definition.UnequipSeconds) : 0;
                Visual.position += PoseFrame.rotation * ((Profile.FpsSprintCarry ? Vector3.zero : Profile.SprintOffset * sprint) + Vector3.down * (.10f * lower));
            }
            PrimaryPalmTarget = Sockets.PrimaryGrip.position + (SupportedPistol
                ? Sockets.PrimaryGrip.rotation * Profile.FpsPrimaryPalmOffset : Vector3.zero);
            rightPalm.WristPose(PrimaryPalmTarget, Sockets.PrimaryGrip.rotation, out var target, out var wrist);
            ReachResidual = Solve(RightUpper, RightLower, RightHand, target, wrist,
                (anchor ? anchor : PoseFrame).TransformPoint((Profile.FpsWeaponOwnsContacts || SupportedPistol) ? Profile.FpsRightElbow : Profile.RightElbow), ref rightBend, dt);
            // Legacy one-hand weapons follow the resolved palm. Character-specific and
            // two-hand contacts stay weapon-owned so reach failure cannot move the sights.
            if (!Profile.TwoHandedMelee && !Profile.TwoHandedFirearm && !Profile.FpsWeaponOwnsContacts && !SupportedPistol)
                CharacterWeaponSockets.Attach(Visual, Sockets.PrimaryGrip, rightPalm.PalmPosition, rightPalm.PalmRotation);
            ReloadAssist = 0;
            float magazineExtraction = 0;
            if (firearm && Profile.Pistol && Sockets.ReloadReference && firearm.RuntimeState.State == WeaponState.Reloading)
            {
                PistolReloadPose.Sample(firearm.RuntimeState, out float assist, out magazineExtraction);
                ReloadAssist = assist;
            }
            else if (firearm && Profile.TwoHandedFirearm && Sockets.ReloadReference && firearm.RuntimeState.State == WeaponState.Reloading)
            {
                float duration = firearm.RuntimeState.IsEmptyReload ? firearm.Definition.EmptyReloadSeconds : firearm.Definition.TacticalReloadSeconds;
                float phase = Mathf.Clamp01(firearm.RuntimeState.StateTimer / Mathf.Max(.01f, duration));
                ReloadAssist = Mathf.Pow(Mathf.Sin(phase * Mathf.PI), 2);
                magazineExtraction = ReloadAssist;
            }
            if (Sockets.Magazine) Sockets.Magazine.localPosition = magazineRest + Vector3.down * (Profile.PistolMagazineTravel * magazineExtraction);
            if (!SupportedPistol) pistolSupportWeight = pistolReloadBlend = 0;
            Vector3 leftTarget = PoseFrame.TransformPoint(Profile.FpsOffhandPosition);
            Quaternion leftRotation = PoseFrame.rotation * offhandRotation;
            if (SupportedPistol)
            {
                float step = Mathf.Max(0, dt) / Mathf.Max(.01f, Profile.PoseBlendSeconds);
                var state = firearm ? firearm.RuntimeState : null;
                bool holding = state != null && (state.State == WeaponState.Ready || state.State == WeaponState.Reloading);
                // A two-hand pistol keeps the support palm on the gun while running.
                // Releasing to the generic rest pose caused the arm to swing across the view.
                float active = holding ? equip : 0;
                pistolSupportWeight = Mathf.MoveTowards(pistolSupportWeight, active, step);
                pistolReloadBlend = Mathf.MoveTowards(pistolReloadBlend, ReloadAssist, step);
                // Offset is in metres, independent of imported visual scale.
                var primary = Sockets.PrimaryGrip;
                var contact = PrimaryPalmTarget + primary.rotation * Profile.FpsSupportPalmOffset;
                var contactRotation = primary.rotation * Quaternion.Euler(Profile.FpsSupportPalmEuler);
                if (Sockets.ReloadReference && pistolReloadBlend > 0)
                {
                    contact = Vector3.Lerp(contact, Sockets.ReloadReference.position, pistolReloadBlend);
                    contactRotation = Quaternion.Slerp(contactRotation, Sockets.ReloadReference.rotation, pistolReloadBlend);
                }
                SupportPalmTarget = contact;
                leftPalm.WristPose(contact, contactRotation, out var supportTarget, out var supportRotation);
                leftTarget = Vector3.Lerp(leftTarget, supportTarget, pistolSupportWeight);
                leftRotation = Quaternion.Slerp(leftRotation, supportRotation, pistolSupportWeight);
            }
            else if (Profile.TwoHandedMelee || Profile.TwoHandedFirearm)
            {
                pistolSupportWeight = pistolReloadBlend = 0;
                leftPalm.WristPose(Sockets.SecondaryGrip.position, Sockets.SecondaryGrip.rotation, out leftTarget, out leftRotation);
                if (ReloadAssist > 0)
                {
                    leftPalm.WristPose(Sockets.ReloadReference.position, Sockets.ReloadReference.rotation, out var reloadTarget, out var reloadRotation);
                    leftTarget = Vector3.Lerp(leftTarget, reloadTarget, ReloadAssist);
                    leftRotation = Quaternion.Slerp(leftRotation, reloadRotation, ReloadAssist);
                }
            }
            else if (ReloadAssist > 0)
            {
                leftPalm.WristPose(Sockets.ReloadReference.position, Sockets.ReloadReference.rotation, out var reloadTarget, out var reloadRotation);
                leftTarget = Vector3.Lerp(leftTarget, reloadTarget, ReloadAssist);
                leftRotation = Quaternion.Slerp(leftRotation, reloadRotation, ReloadAssist);
            }
            SecondaryReachResidual = Solve(LeftUpper, LeftLower, LeftHand, leftTarget, leftRotation,
                (anchor ? anchor : PoseFrame).TransformPoint((Profile.FpsWeaponOwnsContacts || SupportedPistol) ? Profile.FpsLeftElbow : Profile.LeftElbow), ref leftBend, dt);
            rightPalm.Apply(Profile.FingerGripWeight, Profile.Pistol || Profile.TwoHandedFirearm, weaponView ? weaponView.TriggerPull : 0, Profile.FingerClosure, Profile,
                Profile.FpsPistolFingers);
            if (SupportedPistol)
                leftPalm.Apply(Profile.FingerGripWeight * pistolSupportWeight, false, 0,
                    Mathf.Lerp(Profile.FpsSupportFingerClosure, .35f, pistolReloadBlend), Profile, null, true);
            else if (Profile.TwoHandedMelee || Profile.TwoHandedFirearm)
                leftPalm.Apply(Profile.FingerGripWeight, false, 0, Profile.FingerClosure, Profile, null, true);
            else leftPalm.Apply(.25f + .4f * ReloadAssist, false, 0, .35f);
            SecondaryContactError = SupportedPistol ? Vector3.Distance(leftPalm.PalmPosition, SupportPalmTarget) :
                Profile.TwoHandedMelee || Profile.TwoHandedFirearm
                ? Vector3.Distance(leftPalm.PalmPosition, ReloadAssist > 0
                    ? Vector3.Lerp(Sockets.SecondaryGrip.position, Sockets.ReloadReference.position, ReloadAssist)
                    : Sockets.SecondaryGrip.position) : 0;
            ContactError = Vector3.Distance(rightPalm.PalmPosition, SupportedPistol ? PrimaryPalmTarget : Sockets.PrimaryGrip.position);
            HasPose = true;
        }
        float Solve(Transform upper, Transform lower, Transform hand, Vector3 target, Quaternion rotation,
            Vector3 hint, ref Vector3 bend, float dt)
        {
            var solution = CharacterArmIK.Solve(upper.position, target, Vector3.Distance(upper.position, lower.position),
                Vector3.Distance(lower.position, hand.position), hint, -PoseFrame.up, PoseFrame.rotation * bend,
                Profile.MaximumReach, Profile.ElbowTurnDegreesPerSecond * Mathf.Deg2Rad * dt);
            if (!solution.Valid) return float.PositiveInfinity;
            upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, solution.Elbow - upper.position) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, solution.Wrist - lower.position) * lower.rotation;
            lower.rotation = CharacterArmIK.ForearmTwist(hand.rotation, rotation, hand.position - lower.position, Profile.ForearmTwistDegrees) * lower.rotation;
            hand.rotation = rotation; bend = Quaternion.Inverse(PoseFrame.rotation) * solution.Bend;
            return solution.Residual;
        }
    }
}
