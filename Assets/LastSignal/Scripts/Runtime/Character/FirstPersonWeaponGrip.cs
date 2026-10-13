using UnityEngine;

namespace LastSignal
{
    /// <summary>Final Generic-rig arm pass. Combat owns time; this component owns S021 arm contacts.</summary>
    [DefaultExecutionOrder(350), DisallowMultipleComponent]
    public sealed class FirstPersonWeaponGrip : MonoBehaviour
    {
        public CharacterWeaponPoseProfile Profile;
        public Transform PoseFrame, Visual;
        public CharacterWeaponSockets Sockets;
        public Transform RightUpper, RightLower, RightHand, LeftUpper, LeftLower, LeftHand;
        public Transform[] RightFingers, LeftFingers;
        public bool AuthoredFingerCurl;
        public Quaternion[] RightFingerRest, LeftFingerRest;
        public bool ValidAuthoring
        {
            get
            {
                if (!Profile || !PoseFrame || !Visual || !Sockets || !Sockets.ValidFor(Profile) ||
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
            try { return new CharacterHandPose(hand, fingers, left, .012f, false); }
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
        bool captured, initialized;
        WeaponController firearm;
        WeaponViewPresenter weaponView;
        MeleeWeaponController melee;
        FirstPersonMotor motor;
        PlayerInputReader input;
        FirstPersonLook look;
        PlayerHealth health;
        PlayerSurvival survival;
        float sprint;
        Vector3 magazineRest;
        public float ContactError { get; private set; }
        public float WristBendDegrees => initialized ? Vector3.Angle(rightPalm.PalmRotation * Vector3.forward, RightHand.position - RightLower.position) : 0;
        public float ReachResidual { get; private set; }
        public float SecondaryContactError { get; private set; }
        public float SecondaryReachResidual { get; private set; }
        public bool HasValidContact => HasPose && ContactError <= .02f && ReachResidual <= .02f &&
            (!Profile.TwoHandedMelee || (SecondaryContactError <= .02f && SecondaryReachResidual <= .02f));
        public Vector3 SecondaryPalmPosition => leftPalm != null && leftPalm.Valid ? leftPalm.PalmPosition : Vector3.zero;
        public float ReloadAssist { get; private set; }
        public bool HasPose { get; private set; }
        public Vector3 PalmPosition => rightPalm != null && rightPalm.Valid ? rightPalm.PalmPosition : Vector3.zero;
        public void BindVisual(CharacterWeaponPoseProfile profile, Transform visual, CharacterWeaponSockets sockets)
        {
            Restore(); Profile = profile; Visual = visual; Sockets = sockets;
            visualPosition = visual.localPosition; visualRotation = visual.localRotation; visualScale = visual.localScale;
            magazineRest = sockets.Magazine ? sockets.Magazine.localPosition : Vector3.zero;
            equip = sprint = 0; rightBend = leftBend = Vector3.zero;
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
            offhandRotation = Quaternion.Inverse(PoseFrame.rotation) * LeftHand.rotation;
            if (Sockets.Magazine) magazineRest = Sockets.Magazine.localPosition;
            initialized = true;
        }
        void Update() => Restore();
        void OnDisable()
        {
            Restore(); sprint = equip = 0; attackPosition = Vector3.zero; attackRotation = Quaternion.identity;
            if (animator) animator.speed = 1; rightBend = leftBend = Vector3.zero;
            ReloadAssist = ContactError = ReachResidual = SecondaryContactError = SecondaryReachResidual = 0;
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
            captured = false;
        }
        void LateUpdate()
        {
            if (!initialized || rightPalm == null || !rightPalm.Valid || !Profile || !Sockets || !Sockets.ValidFor(Profile) ||
                (input && input.InVehicle) || (health && !health.IsAlive) || (survival && survival.ApplyingTreatment)) return;
            if (firearm && (firearm.RuntimeState == null || firearm.RuntimeState.State == WeaponState.Holstered)) return;
            if (melee && !melee.isActiveAndEnabled) return;
            for (int i = 0; i < bones.Length; i++) { previous[i] = bones[i].localRotation; previousPositions[i] = bones[i].localPosition; }
            captured = true;
            float dt = firearm ? (firearm.IsSimulationActive ? Time.deltaTime : 0) : (melee && melee.Allowed ? Time.deltaTime : 0);
            if (animator) animator.speed = dt > 0 ? 1 : 0;
            equip = Mathf.MoveTowards(equip, 1, dt / Mathf.Max(.01f, Profile.PoseBlendSeconds));
            // Camera-space shoulder anchors keep real open mesh ends behind the eye while arms articulate.
            // The visual root can ADS/recoil without translating the anatomical shoulder with the gun.
            Transform anchor = look && look.View ? look.View.transform : PoseFrame.parent;
            if (anchor)
            {
                RightUpper.position = anchor.TransformPoint(Profile.FpsRightShoulder);
                LeftUpper.position = anchor.TransformPoint(Profile.FpsLeftShoulder);
            }
            sprint = Mathf.MoveTowards(sprint, motor && motor.IsSprinting ? 1 : 0, dt / Profile.PoseBlendSeconds);
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
                Visual.position += PoseFrame.rotation * (Profile.SprintOffset * sprint + Vector3.down * (.10f * lower));
            }
            rightPalm.WristPose(Sockets.PrimaryGrip.position, Sockets.PrimaryGrip.rotation, out var target, out var wrist);
            ReachResidual = Solve(RightUpper, RightLower, RightHand, target, wrist,
                (anchor ? anchor : PoseFrame).TransformPoint(Profile.RightElbow), ref rightBend, dt);
            // One-hand weapons follow the resolved palm. Axe contacts remain weapon-owned;
            // moving its root here would conceal right reach failure and invalidate the left target.
            if (!Profile.TwoHandedMelee)
                CharacterWeaponSockets.Attach(Visual, Sockets.PrimaryGrip, rightPalm.PalmPosition, rightPalm.PalmRotation);
            ReloadAssist = 0;
            float magazineExtraction = 0;
            if (firearm && Profile.Pistol && Sockets.ReloadReference && firearm.RuntimeState.State == WeaponState.Reloading)
            {
                PistolReloadPose.Sample(firearm.RuntimeState, out float assist, out magazineExtraction);
                ReloadAssist = assist;
            }
            if (Sockets.Magazine) Sockets.Magazine.localPosition = magazineRest + Vector3.down * (Profile.PistolMagazineTravel * magazineExtraction);
            Vector3 leftTarget = PoseFrame.TransformPoint(Profile.FpsOffhandPosition);
            Quaternion leftRotation = PoseFrame.rotation * offhandRotation;
            if (Profile.TwoHandedMelee)
                leftPalm.WristPose(Sockets.SecondaryGrip.position, Sockets.SecondaryGrip.rotation, out leftTarget, out leftRotation);
            else if (ReloadAssist > 0)
            {
                leftPalm.WristPose(Sockets.ReloadReference.position, Sockets.ReloadReference.rotation, out var reloadTarget, out var reloadRotation);
                leftTarget = Vector3.Lerp(leftTarget, reloadTarget, ReloadAssist);
                leftRotation = Quaternion.Slerp(leftRotation, reloadRotation, ReloadAssist);
            }
            SecondaryReachResidual = Solve(LeftUpper, LeftLower, LeftHand, leftTarget, leftRotation,
                (anchor ? anchor : PoseFrame).TransformPoint(Profile.LeftElbow), ref leftBend, dt);
            rightPalm.Apply(Profile.FingerGripWeight, Profile.Pistol, weaponView ? weaponView.TriggerPull : 0, Profile.FingerClosure, Profile,
                Profile.Pistol ? Profile.FpsPistolFingers : null);
            if (Profile.TwoHandedMelee)
                leftPalm.Apply(Profile.FingerGripWeight, false, 0, Profile.FingerClosure, Profile, null, true);
            else leftPalm.Apply(.25f + .4f * ReloadAssist, false, 0, .35f);
            SecondaryContactError = Profile.TwoHandedMelee ? Vector3.Distance(leftPalm.PalmPosition, Sockets.SecondaryGrip.position) : 0;
            ContactError = Vector3.Distance(rightPalm.PalmPosition, Sockets.PrimaryGrip.position);
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
