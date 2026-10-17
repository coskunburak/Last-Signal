using UnityEngine;

namespace LastSignal
{
    /// <summary>Final firearm pose after Animator evaluation: stock contact, calibrated palms, arms and fingers.</summary>
    [DefaultExecutionOrder(300), DisallowMultipleComponent, RequireComponent(typeof(Animator))]
    public sealed class CharacterWeaponRig : MonoBehaviour
    {
        [SerializeField] CharacterWeaponPoseProfile[] profiles;
        [SerializeField,Min(0)] float rightPalmDepth=.012f, leftPalmDepth=.012f;
        [System.Serializable]
        public struct PresentationOverride
        {
            public CharacterWeaponPoseProfile Source;
            public CharacterWeaponPoseProfile Character;
        }
        [SerializeField] PresentationOverride[] presentationOverrides;
        // Match by source asset identity so cosmetic pistol variants keep their own geometry.
        public CharacterWeaponPoseProfile ResolvePresentation(CharacterWeaponPoseProfile source)
        {
            if (source && presentationOverrides != null)
                foreach (var entry in presentationOverrides)
                    if (entry.Source == source && entry.Character) return entry.Character;
            return source;
        }
        public void ConfigureOverrides(PresentationOverride[] values) => presentationOverrides = values;
        [Header("Character jacket envelope, including forearm/sleeve clearance (actor metres)")]
        [SerializeField] Vector3 torsoCenterOffset = new Vector3(0, .08f, .025f);
        [SerializeField] Vector3 torsoClearanceRadii = new Vector3(.25f, .34f, .225f);
        public CharacterArmClearance.Torso TorsoClearance => new CharacterArmClearance.Torso
        {
            Center = chest.position + TorsoFrame * (torsoCenterOffset * Mathf.Abs(transform.lossyScale.x)),
            Rotation = TorsoFrame,
            Radii = torsoClearanceRadii * Mathf.Abs(transform.lossyScale.x)
        };
        Quaternion TorsoFrame => (upperChest ? upperChest : chest).rotation * chestToBodyFrame;
        public float RightTorsoPenetration { get; private set; }
        public float LeftTorsoPenetration { get; private set; }
        Animator animator;
        PlayerLocomotionPresenter state;
        CharacterBodyVisibility visibility;
        Transform view;
        Transform chest, upperChest, rightShoulder, leftShoulder, head, rightHand, rightUpper, rightLower, leftHand, leftUpper, leftLower;
        CharacterHandPose rightPalm, leftPalm;
        Transform[] posedBones;
        Quaternion[] previousRotations;
        bool poseCaptured;
        float poseWeight, sprintWeight, slideWeight, aimPitch, aimWeight;
        Quaternion chestToBodyFrame;
        Vector3 rightBendLocal, leftBendLocal;
        float frameSeconds;
        float pistolSupportWeight, pistolReloadBlend;
        bool pistolSupportWarningIssued;
        public bool HasFirearmPose { get; private set; }
        public float SupportHandWeight { get; private set; }
        public float PoseWeight => poseWeight;
        public float SprintWeight => sprintWeight;
        public float SlideWeight => slideWeight;
        public float CosmeticRecoil => recoil;
        public Vector3 RightElbowHint { get; private set; }
        public Vector3 LeftElbowHint { get; private set; }
        public Vector3 RightSolvedWrist { get; private set; }
        public Vector3 LeftSolvedWrist { get; private set; }
        public float RightReachResidual { get; private set; }
        public float LeftReachResidual { get; private set; }
        public float MeleePlacementCorrection { get; private set; }
        public float TorsoReachYaw { get; private set; }
        public float HipClearanceDegrees { get; private set; }
        public float RightGripError => sockets && sockets.PrimaryGrip && weaponVisual && weaponVisual.activeSelf ? Vector3.Distance(rightPalm.PalmPosition, sockets.PrimaryGrip.position) : 0;
        public float LeftGripError => profile && weaponVisual && weaponVisual.activeSelf &&
            leftPalm != null && leftPalm.Valid &&
            (profile.TwoHandedMelee || (HasFirearmPose && !profile.SingleHanded) ||
             (profile.Pistol && profile.WorldTwoHandedPistol && SupportHandWeight > .001f))
                ? Vector3.Distance(leftPalm.PalmPosition, LeftPalmTarget) : 0;
        public bool HasValidMeleeContact => profile && profile.Melee && weaponVisual && weaponVisual.activeSelf &&
            RightGripError <= .02f && RightReachResidual <= .02f &&
            (!profile.TwoHandedMelee || (LeftGripError <= .02f && LeftReachResidual <= .02f));
        public Vector3 LeftPalmTarget { get; private set; }
        public Vector3 RightPalmPosition => rightPalm != null && rightPalm.Valid ? rightPalm.PalmPosition : Vector3.zero;
        public Vector3 LeftPalmPosition => leftPalm != null && leftPalm.Valid ? leftPalm.PalmPosition : Vector3.zero;
        public Vector3 RightWristTarget { get; private set; }
        public Vector3 LeftWristTarget { get; private set; }
        public Vector3 StockTarget { get; private set; }
        CharacterWeaponPoseProfile profile;
        CharacterWeaponSockets sockets;
        GameObject weaponVisual;
        WeaponController subscribedWeapon;
        MeleeWeaponController observedMelee;
        Vector3 magazineRest;
        Transform magazineParent;
        bool reloadContactFollowsMagazine;
        float recoil, readySeconds, readyWeight, shotCarryDelay;
        Vector3 meleePosition;
        Quaternion meleeRotation = Quaternion.identity;
        public float ReadyWeight => readyWeight;
        WeaponState lastWeaponState;
        public CharacterWeaponPoseProfile ActiveProfile => profile;
        public CharacterWeaponSockets Sockets => sockets;
        public GameObject WeaponVisual => weaponVisual;
        public CharacterWeaponPoseProfile[] Profiles => profiles;
        public void Configure(CharacterWeaponPoseProfile[] values) => profiles = values;
        void Awake()
        {
            animator = GetComponent<Animator>(); state = GetComponent<PlayerLocomotionPresenter>();
            visibility = GetComponent<CharacterBodyVisibility>();
            var look = GetComponentInParent<FirstPersonLook>(); view = look && look.View ? look.View.transform : null;
            if (!animator.isHuman || !state) { enabled = false; return; }
            chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            if (!chest) chest = animator.GetBoneTransform(HumanBodyBones.Spine);
            upperChest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
            rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            head = animator.GetBoneTransform(HumanBodyBones.Head);
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            rightUpper = animator.GetBoneTransform(HumanBodyBones.RightUpperArm); rightLower = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            leftUpper = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm); leftLower = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            if (!chest || !head || !rightHand || !leftHand || !rightUpper || !rightLower || !leftUpper || !leftLower)
            { enabled = false; return; }
            rightPalm = new CharacterHandPose(animator, false, rightPalmDepth); leftPalm = new CharacterHandPose(animator, true, leftPalmDepth);
            if (!rightPalm.Valid || !leftPalm.Valid) { enabled = false; return; }
            // Imported bone axes are not character axes. Cache their relationship once in the authored rest pose.
            chestToBodyFrame = Quaternion.Inverse((upperChest ? upperChest : chest).rotation) * transform.rotation;
            posedBones = new[] { chest, upperChest, head, rightShoulder, leftShoulder, rightUpper, rightLower, rightHand, leftUpper, leftLower, leftHand };
            previousRotations = new Quaternion[posedBones.Length];
        }
        void OnDisable()
        {
            RestorePose(); ResetPoseState();
            Subscribe(null); observedMelee = null; recoil = 0;
            if (weaponVisual) { weaponVisual.SetActive(false); Destroy(weaponVisual); }
            weaponVisual = null; sockets = null; profile = null; magazineParent = null;
            if (visibility) visibility.RefreshRenderers();
        }
        void ResetPoseState()
        {
            pistolSupportWeight = pistolReloadBlend = 0;
            pistolSupportWarningIssued = false;
            meleePosition = Vector3.zero; meleeRotation = Quaternion.identity;
            HasFirearmPose = false; SupportHandWeight = 0; readySeconds = readyWeight = shotCarryDelay = 0;
            poseWeight = sprintWeight = slideWeight = aimPitch = aimWeight = 0;
            rightBendLocal = leftBendLocal = Vector3.zero;
            RightReachResidual = LeftReachResidual = 0;
            MeleePlacementCorrection = 0;
            RightTorsoPenetration = LeftTorsoPenetration = 0;
            TorsoReachYaw = 0; HipClearanceDegrees = 0;
            RightWristTarget = LeftWristTarget = StockTarget = Vector3.zero;
            RightSolvedWrist = LeftSolvedWrist = LeftPalmTarget = Vector3.zero;
            RightElbowHint = LeftElbowHint = Vector3.zero;
        }
        void Subscribe(WeaponController next)
        {
            if (ReferenceEquals(subscribedWeapon, next)) return;
            // Unity null operator detects destroyed objects that ReferenceEquals misses.
            if (subscribedWeapon) subscribedWeapon.ShotFired -= OnShot;
            subscribedWeapon = next;
            if (subscribedWeapon) subscribedWeapon.ShotFired += OnShot;
        }
        void OnShot(WeaponFireResolver.ShotResult shot)
        { shotCarryDelay = .18f; if (profile && profile.Pistol) readySeconds = profile.ReadyHoldSeconds; if (state) state.InterruptCosmeticAction(); if (profile && state && !state.SeatedNow) recoil = Mathf.Min(profile.RecoilDistance * 2, recoil + profile.RecoilDistance); }
        void Update()
        {
            // Restore the preceding frame before Animator evaluation; never accumulate procedural offsets.
            RestorePose();
            HasFirearmPose = false;
            if (!state || !state.isActiveAndEnabled || !state.Combat)
            {
                Subscribe(null);
                if (weaponVisual || profile || observedMelee) ReplaceVisual(null, null);
                lastWeaponState = WeaponState.Holstered;
                return;
            }
            var combat = state.Combat;
            var firearm = combat.ActiveWeapon;
            var melee = combat.SelectedSlot == PlayerCombatController.CombatSlot.Melee ? combat.Melee : null;
            Subscribe(firearm);
            var current = firearm && firearm.RuntimeState != null ? firearm.RuntimeState.State : WeaponState.Holstered;
            if (current != lastWeaponState)
            {
                if (current == WeaponState.Reloading || current == WeaponState.Equipping) state.InterruptCosmeticAction();
                // If we left Reloading without completing the full sine curve, snap magazine back.
                if (lastWeaponState == WeaponState.Reloading && sockets && sockets.Magazine) sockets.Magazine.localPosition = magazineRest;
            }
            lastWeaponState = current;
            CharacterWeaponPoseProfile next = null;
            if (profiles != null) foreach (var candidate in profiles)
                if (candidate && ((firearm && candidate.Firearm == firearm.Definition) || (melee && candidate.Melee == melee.Definition)))
                { next = candidate; break; }
            if (firearm && firearm.PresentationProfile) next = firearm.PresentationProfile;
            next = ResolvePresentation(next);
            if (profile != next || observedMelee != melee) ReplaceVisual(next, melee);
            if (!weaponVisual) return;
            bool shown = (!profile.ProceduralGrip || (sockets && sockets.ValidFor(profile))) && state.Alive && !state.SeatedNow && !state.Treating && (!state.UpperActionBusy || melee) &&
                ((firearm && firearm.isActiveAndEnabled && firearm.RuntimeState != null && firearm.RuntimeState.State != WeaponState.Holstered) ||
                 (melee && melee.isActiveAndEnabled));
            weaponVisual.SetActive(shown);
            if (!shown || (melee && !profile.ProceduralGrip))
            {
                ResetPoseState(); recoil = 0;
                if (sockets && sockets.Magazine) sockets.Magazine.localPosition = magazineRest;
            }
            if (state.PresentationActive)
            {
                recoil *= Mathf.Exp(-Mathf.Max(.01f, profile.RecoilRecovery) * Time.deltaTime);
                shotCarryDelay = Mathf.Max(0, shotCarryDelay - Time.deltaTime);
            }
        }
        void ReplaceVisual(CharacterWeaponPoseProfile next, MeleeWeaponController melee)
        {
            if (weaponVisual) { weaponVisual.SetActive(false); Destroy(weaponVisual); }
            profile = next; observedMelee = melee; weaponVisual = null; sockets = null; recoil = 0;
            ResetPoseState(); magazineParent = null; reloadContactFollowsMagazine = false;
            if (profile && profile.WorldPrefab)
            {
                weaponVisual = Instantiate(profile.WorldPrefab, transform);
                weaponVisual.name = "WorldWeapon_" + profile.name;
                if (profile.Firearm || profile.ProceduralGrip) weaponVisual.transform.localScale *= profile.WorldScale;
                sockets = weaponVisual.GetComponent<CharacterWeaponSockets>();
                var mechanics = weaponVisual.GetComponent<PistolMechanics>();
                if (mechanics) mechanics.Bind(subscribedWeapon);
                if (sockets && sockets.Magazine)
                {
                    magazineRest = sockets.Magazine.localPosition; magazineParent = sockets.Magazine.parent;
                    reloadContactFollowsMagazine = sockets.ReloadReference && sockets.ReloadReference.IsChildOf(sockets.Magazine);
                }
            }
            if (visibility) visibility.RefreshRenderers();
        }
        bool WantsSprintCarry()
        {
            if (!state || !state.Motor || !state.Motor.IsSprinting) return false;
            if (!subscribedWeapon) return true;
            var runtime = subscribedWeapon.RuntimeState;
            return runtime != null && runtime.State == WeaponState.Ready && runtime.AimAmount < .01f && shotCarryDelay <= 0;
        }
        void LateUpdate()
        {
            // Animator action ownership can change after Update. Release in this same presentation frame.
            if (!state || !state.isActiveAndEnabled || !state.Alive || state.SeatedNow || state.Treating ||
                (state.UpperActionBusy && !observedMelee))
            {
                if (weaponVisual) weaponVisual.SetActive(false);
                if (sockets && sockets.Magazine) sockets.Magazine.localPosition = magazineRest;
                ResetPoseState(); recoil = 0;
                return;
            }
            if (profile && profile.ProceduralGrip)
            {
                if (!weaponVisual || !weaponVisual.activeSelf || !sockets || !sockets.ValidFor(profile)) return;
                frameSeconds = state.PresentationActive ? Mathf.Max(0, Time.deltaTime) : 0;
                float singleStep = frameSeconds / Mathf.Max(.01f, profile.PoseBlendSeconds);
                poseWeight = Mathf.MoveTowards(poseWeight, 1, singleStep);
                sprintWeight = Mathf.MoveTowards(sprintWeight, WantsSprintCarry() ? 1 : 0, singleStep);
                slideWeight = Mathf.MoveTowards(slideWeight, state.Sliding ? 1 : 0, singleStep);
                for (int i = 0; i < posedBones.Length; i++) if (posedBones[i]) previousRotations[i] = posedBones[i].localRotation;
                poseCaptured = true;
                if (profile.TwoHandedMelee) ApplyTwoHandMeleePose();
                else ApplySingleHandPose();
                return;
            }
            if (observedMelee && weaponVisual && profile && rightHand)
            {
                weaponVisual.transform.SetPositionAndRotation(rightHand.position + rightHand.rotation * profile.HandPosition, rightHand.rotation * Quaternion.Euler(profile.HandEuler));
                if (weaponVisual.activeSelf) rightPalm.Apply(profile.FingerGripWeight, false, 0);
                return;
            }
            if (!state || !state.isActiveAndEnabled || !profile || !sockets || !weaponVisual ||
                !sockets.HasFirearmGrips || !weaponVisual.activeSelf || !state.Alive || state.SeatedNow || state.Treating || state.UpperActionBusy) return;
            var runtime = subscribedWeapon ? subscribedWeapon.RuntimeState : null;
            if (runtime == null) return;
            frameSeconds = state.PresentationActive ? Mathf.Max(0, Time.deltaTime) : 0;
            float step = frameSeconds / Mathf.Max(.01f, profile.PoseBlendSeconds);
            float equipWeight = runtime.State == WeaponState.Unequipping
                ? 1 - Mathf.SmoothStep(0, 1, runtime.StateTimer / Mathf.Max(.01f, runtime.Definition.UnequipSeconds)) : 1;
            poseWeight = Mathf.MoveTowards(poseWeight, equipWeight, step);
            sprintWeight = Mathf.MoveTowards(sprintWeight, WantsSprintCarry() ? 1 : 0, step);
            slideWeight = Mathf.MoveTowards(slideWeight, state.Sliding ? 1 : 0, step);
            for (int i = 0; i < posedBones.Length; i++) if (posedBones[i]) previousRotations[i] = posedBones[i].localRotation;
            poseCaptured = true;
            ApplyFirearmPose(runtime);
        }
        void OnAnimatorIK(int layerIndex)
        {
            // Vehicle owns the Humanoid hand pass when seated. Firearms solve after all Animator
            // layers in LateUpdate so imported muscle/IK curves cannot overwrite calibrated palms.
            if (layerIndex != 0) return;
            if (state && state.SeatedNow) return;
            ClearIK();
        }
        void ApplyFirearmPose(WeaponRuntimeState runtime)
        {
            HasFirearmPose = true;
            // The combat layer may lower AimAmount during an input-modal pause; freeze cosmetic state here.
            if (frameSeconds > 0) aimWeight = Mathf.MoveTowards(aimWeight, Mathf.Clamp01(state.AimAmount),
                frameSeconds / Mathf.Max(.01f, profile.PoseBlendSeconds));
            float aim = aimWeight * (1 - slideWeight) * (1 - sprintWeight);
            float scale = Mathf.Abs(transform.lossyScale.x);
            var stockOffset = Vector3.Lerp(profile.HipStockOffset, profile.AimStockOffset, aim);
            Quaternion stanceRotation = Quaternion.Slerp(Quaternion.Euler(profile.HipEuler), Quaternion.Euler(profile.AimEuler), aim);
            stanceRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(profile.SprintEuler), sprintWeight * (1 - slideWeight));
            stanceRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(profile.SlideEuler), slideWeight);
            float desiredPitch = view ? Mathf.Clamp(Mathf.DeltaAngle(0, view.localEulerAngles.x), -55, 55) : 0;
            desiredPitch *= Mathf.Lerp(.35f, 1, aim);
            aimPitch = Mathf.Lerp(aimPitch, desiredPitch, 1 - Mathf.Exp(-frameSeconds / Mathf.Max(.01f, profile.PoseBlendSeconds)));
            float kick = Mathf.Clamp01(recoil / Mathf.Max(.001f, profile.RecoilDistance));
            float torsoPitch = (aimPitch * profile.TorsoPitchWeight - kick * profile.RecoilPitch * .35f) * poseWeight;
            // Target an anatomical heading, not an offset added to whichever yaw the current
            // locomotion/slide clip contributes. Additive yaw made reach depend on clip history.
            Quaternion animatedFrame = (upperChest ? upperChest : chest).rotation * chestToBodyFrame;
            float torsoYaw = CalculateTorsoYawCorrection(animatedFrame, transform.rotation, profile.TorsoYaw, poseWeight);
            Quaternion originalHead = head.rotation;
            // Distribute the stance across the chest chain, retaining Animator breathing and gait.
            float chestShare = upperChest ? .35f : 1;
            RotateTorso(chest, torsoPitch * chestShare, torsoYaw * chestShare);
            if (upperChest) RotateTorso(upperChest, torsoPitch * .65f, torsoYaw * .65f);
            head.rotation = Quaternion.AngleAxis(aimPitch * .4f * poseWeight, transform.right) * originalHead;
            Quaternion pocketFrame = (upperChest ? upperChest : chest).rotation * chestToBodyFrame;
            Quaternion facing = transform.rotation * Quaternion.Euler(aimPitch, 0, 0);
            float lower = 0;
            if (runtime.State == WeaponState.Equipping)
                lower = .10f * (1 - Mathf.SmoothStep(0, 1, runtime.StateTimer / Mathf.Max(.01f, runtime.Definition.EquipSeconds)));
            if (runtime.State == WeaponState.Unequipping)
                lower = .10f * Mathf.SmoothStep(0, 1, runtime.StateTimer / Mathf.Max(.01f, runtime.Definition.UnequipSeconds));
            // Capture the pocket before reach assistance: the clavicle must not chase a moving weapon target.
            StockTarget = rightUpper.position + pocketFrame * ((stockOffset + Vector3.down * lower) * scale)
                - facing * Vector3.forward * (Mathf.Min(.008f, recoil) * scale);
            // The clavicle is inside the jacket: its offset is only a pocket seed,
            // not a valid skin contact until projected onto the anterior envelope.
            StockTarget += TorsoClearance.FrontCorrection(StockTarget);
            weaponVisual.transform.rotation = facing * stanceRotation * Quaternion.Euler(-kick * profile.RecoilPitch, 0, 0);
            weaponVisual.transform.position += StockTarget - sockets.StockContact.position;

            float reload = runtime.State == WeaponState.Reloading
                ? Mathf.Clamp01(runtime.StateTimer / Mathf.Max(.01f, runtime.IsEmptyReload ? runtime.Definition.EmptyReloadSeconds : runtime.Definition.TacticalReloadSeconds)) : 0;
            // Ease in/out with zero endpoint velocity. Follow the displaced magazine, not its old rest location.
            float reloadReach = Mathf.Sin(reload * Mathf.PI);
            reloadReach *= reloadReach;
            Vector3 magazineDelta = Vector3.down * (.12f * reloadReach);
            if (sockets.Magazine) sockets.Magazine.localPosition = magazineRest + magazineDelta;
            Vector3 reloadContact = sockets.ReloadReference.position;
            if (sockets.Magazine && magazineParent && !reloadContactFollowsMagazine)
                reloadContact += magazineParent.TransformVector(magazineDelta);
            LeftPalmTarget = Vector3.Lerp(sockets.LeftGrip.position, reloadContact, reloadReach);
            rightPalm.WristPose(sockets.RightGrip.position, sockets.RightGrip.rotation, out var rightPosition, out var rightRotation);
            leftPalm.WristPose(LeftPalmTarget, Quaternion.Slerp(sockets.LeftGrip.rotation, sockets.ReloadReference.rotation, reloadReach), out var leftPosition, out var leftRotation);
            float weight = Mathf.Clamp01(profile.HandWeight * poseWeight);
            SupportHandWeight = weight;
            AssistShoulder(rightShoulder, rightUpper, rightLower, rightHand, rightPosition, weight);
            AssistShoulder(leftShoulder, leftUpper, leftLower, leftHand, leftPosition, weight);
            // Clavicle assistance alone cannot always recover the support contact after slide.
            // Turn the torso within its authored budget, keeping the rifle's aim and stock anchor.
            TorsoReachYaw = SupportReachYaw(leftPosition, rightPosition) * weight;
            if (Mathf.Abs(TorsoReachYaw) > .00001f)
            {
                Transform torso = upperChest ? upperChest : chest;
                Quaternion turn = Quaternion.AngleAxis(TorsoReachYaw, transform.up);
                Vector3 movedStock = torso.position + turn * (StockTarget - torso.position);
                Vector3 translation = movedStock - StockTarget;
                torso.rotation = turn * torso.rotation;
                StockTarget = movedStock;
                weaponVisual.transform.position += translation;
                LeftPalmTarget += translation;
                leftPosition += translation; rightPosition += translation;
                pocketFrame = torso.rotation * chestToBodyFrame;
            }
            Quaternion clearance = FindHipClearanceRotation(rightPosition, leftPosition, 12 * (1 - aimWeight));
            HipClearanceDegrees = CharacterArmIK.RotationDegrees(clearance);
            if (HipClearanceDegrees > .001f)
            {
                weaponVisual.transform.SetPositionAndRotation(StockTarget + clearance * (weaponVisual.transform.position - StockTarget),
                    clearance * weaponVisual.transform.rotation);
                LeftPalmTarget = StockTarget + clearance * (LeftPalmTarget - StockTarget);
                rightPosition = StockTarget + clearance * (rightPosition - StockTarget);
                leftPosition = StockTarget + clearance * (leftPosition - StockTarget);
                rightRotation = clearance * rightRotation;
                leftRotation = clearance * leftRotation;
            }
            RightWristTarget = rightPosition; LeftWristTarget = leftPosition;
            RightElbowHint = chest.position + pocketFrame * (profile.RightElbow * scale);
            LeftElbowHint = chest.position + pocketFrame * (profile.LeftElbow * scale);
            var right = SolveArm(rightUpper, rightLower, rightHand, rightPosition, rightRotation, RightElbowHint, weight, ref rightBendLocal);
            var left = SolveArm(leftUpper, leftLower, leftHand, leftPosition, leftRotation, LeftElbowHint, weight, ref leftBendLocal);
            RightSolvedWrist = right.Wrist; LeftSolvedWrist = left.Wrist;
            RightReachResidual = right.Residual; LeftReachResidual = left.Residual;
            rightPalm.Apply(profile.FingerGripWeight * weight, true, kick);
            leftPalm.Apply(profile.FingerGripWeight * weight * (1 - .55f * reloadReach), false, 0);
        }
        void ApplyTwoHandMeleePose()
        {
            // The anatomical torso frame owns BOTH contacts, including during bent locomotion poses.
            float scale = Mathf.Abs(transform.lossyScale.x);
            profile.SampleAttack(observedMelee ? observedMelee.Simulation : null, frameSeconds, ref meleePosition, ref meleeRotation);
            float pitch = view ? Mathf.Clamp(Mathf.DeltaAngle(0, view.localEulerAngles.x), -55, 55) : 0;
            aimPitch = Mathf.Lerp(aimPitch, pitch, 1 - Mathf.Exp(-frameSeconds / profile.PoseBlendSeconds));
            // Small authored shoulder/torso preparation; all offsets restore before Animator evaluation.
            float turn = Mathf.DeltaAngle(0, meleeRotation.eulerAngles.y) * .25f * poseWeight;
            RotateTorso(chest, aimPitch * .12f * poseWeight, turn);
            if (upperChest) RotateTorso(upperChest, aimPitch * .12f * poseWeight, turn);
            Quaternion bodyFrame = TorsoFrame;
            Quaternion facing = bodyFrame * Quaternion.Euler(aimPitch * .35f, 0, 0);
            // Slide changes the animated shoulder relationship. Use the character's
            // connected two-hand slide pose without stacking the full sprint drop.
            Vector3 offset = profile.HipGripOffset + meleePosition +
                profile.SprintOffset * sprintWeight * (1 - slideWeight) +
                profile.CrouchOffset * (state.Crouching && !state.Sliding ? 1 : 0) +
                profile.WorldMeleeSlideOffset * slideWeight + Vector3.down * (.10f * (1 - poseWeight));
            Quaternion rotation = facing * Quaternion.Euler(profile.HipEuler) * meleeRotation *
                Quaternion.Euler(profile.SprintEuler * sprintWeight * (1 - slideWeight)) * Quaternion.Euler(0, 0, 90);
            CharacterWeaponSockets.Attach(weaponVisual.transform, sockets.PrimaryGrip,
                rightUpper.position + bodyFrame * (offset * scale), rotation);
            rightPalm.WristPose(sockets.PrimaryGrip.position, sockets.PrimaryGrip.rotation, out var rp, out var rr);
            LeftPalmTarget = sockets.SecondaryGrip.position;
            leftPalm.WristPose(LeftPalmTarget, sockets.SecondaryGrip.rotation, out var lp, out var lr);
            // Lower the connected rig during equip; blending wrists independently would break contact.
            const float weight = 1;
            SupportHandWeight = weight;
            AssistShoulder(rightShoulder, rightUpper, rightLower, rightHand, rp, weight);
            AssistShoulder(leftShoulder, leftUpper, leftLower, leftHand, lp, weight);
            // Move the shared weapon root, never one contact in isolation. A bounded projection
            // preserves both grip sockets when Diesel's animated shoulder pose changes reach.
            Vector3 authoredPosition = weaponVisual.transform.position;
            for (int i = 0; i < 8; i++)
            {
                ShiftMeleeWithinReach(leftUpper, leftLower, leftHand, lp, authoredPosition);
                rightPalm.WristPose(sockets.PrimaryGrip.position, sockets.PrimaryGrip.rotation, out rp, out rr);
                ShiftMeleeWithinReach(rightUpper, rightLower, rightHand, rp, authoredPosition);
                rightPalm.WristPose(sockets.PrimaryGrip.position, sockets.PrimaryGrip.rotation, out rp, out rr);
                LeftPalmTarget = sockets.SecondaryGrip.position;
                leftPalm.WristPose(LeftPalmTarget, sockets.SecondaryGrip.rotation, out lp, out lr);
                var torso = TorsoClearance;
                Vector3 front = torso.FrontCorrection(rp);
                Vector3 leftFront = torso.FrontCorrection(lp);
                if (leftFront.sqrMagnitude > front.sqrMagnitude) front = leftFront;
                weaponVisual.transform.position = authoredPosition + Vector3.ClampMagnitude(
                    weaponVisual.transform.position + front - authoredPosition, .10f);
                rightPalm.WristPose(sockets.PrimaryGrip.position, sockets.PrimaryGrip.rotation, out rp, out rr);
                LeftPalmTarget = sockets.SecondaryGrip.position;
                leftPalm.WristPose(LeftPalmTarget, sockets.SecondaryGrip.rotation, out lp, out lr);
            }
            Vector3 safeShift = FindMeleeClearanceShift(rp, lp, authoredPosition,out var safeRotation);
            weaponVisual.transform.SetPositionAndRotation(rp+safeRotation*(weaponVisual.transform.position-rp)+safeShift,
                safeRotation*weaponVisual.transform.rotation);
            rightPalm.WristPose(sockets.PrimaryGrip.position, sockets.PrimaryGrip.rotation, out rp, out rr);
            LeftPalmTarget = sockets.SecondaryGrip.position;
            leftPalm.WristPose(LeftPalmTarget, sockets.SecondaryGrip.rotation, out lp, out lr);
            MeleePlacementCorrection = Vector3.Distance(authoredPosition, weaponVisual.transform.position);
            RightWristTarget = rp; LeftWristTarget = lp;
            RightElbowHint = chest.position + bodyFrame * (profile.RightElbow * scale);
            LeftElbowHint = chest.position + bodyFrame * (profile.LeftElbow * scale);
            var r = SolveArm(rightUpper, rightLower, rightHand, rp, rr, RightElbowHint, weight, ref rightBendLocal);
            var l = SolveArm(leftUpper, leftLower, leftHand, lp, lr, LeftElbowHint, weight, ref leftBendLocal);
            RightSolvedWrist = r.Wrist; LeftSolvedWrist = l.Wrist;
            RightReachResidual = r.Residual; LeftReachResidual = l.Residual;
            rightPalm.Apply(profile.FingerGripWeight, false, 0, profile.FingerClosure, profile);
            leftPalm.Apply(profile.FingerGripWeight, false, 0, profile.FingerClosure, profile, null, true);
        }
        Quaternion FindHipClearanceRotation(Vector3 right, Vector3 left, float limit)
        {
            if (limit < .001f) return Quaternion.identity;
            var torso = TorsoClearance;
            Quaternion best = Quaternion.identity;
            float score = ArmsClearanceCost(right, left, torso);
            if (score <= .0002f) return best;
            for (float step = 4; step >= .5f; step *= .5f)
            {
                for (int pass = 0; pass < 3; pass++)
                {
                    Quaternion previous = best;
                    for (int axis = 0; axis < 2; axis++)
                    for (int sign = -1; sign <= 1; sign += 2)
                    {
                        Vector3 direction = torso.Rotation * (axis == 0 ? Vector3.right : Vector3.up);
                        Quaternion candidate = CharacterArmIK.ClampRotation(
                            Quaternion.AngleAxis(step * sign, direction) * previous, limit);
                        float cost = ArmsClearanceCost(StockTarget + candidate * (right - StockTarget),
                            StockTarget + candidate * (left - StockTarget), torso);
                        if (cost < score) { score = cost; best = candidate; }
                    }
                    if (score <= .0002f || Quaternion.Angle(previous, best) < .001f) break;
                }
                if (score <= .0002f) break;
            }
            return best;
        }
        Vector3 FindMeleeClearanceShift(Vector3 right, Vector3 left, Vector3 authoredPosition,out Quaternion rotation)
        {
            var torso = TorsoClearance;
            Vector3 root = weaponVisual.transform.position;
            Vector3 best = Vector3.zero;
            Quaternion bestRotation=Quaternion.identity;
            rotation=bestRotation;
            float score = ArmsClearanceCost(right, left, torso);
            if (score <= .0002f) return best;
            float limit=Mathf.Clamp(profile.MeleeClearanceRotation,0,12);
            void Consider(Vector3 shift,Quaternion turn)
            {
                var rotatedRoot=right+turn*(root-right);
                shift=authoredPosition+Vector3.ClampMagnitude(rotatedRoot+shift-authoredPosition,.10f)-rotatedRoot;
                float cost=ArmsClearanceCost(right+shift,right+turn*(left-right)+shift,torso);
                if(cost<score){score=cost;best=shift;bestRotation=turn;}
            }
            // Reach projection can start on the boundary in the wrong direction. Seed
            // the entire bounded volume before refinement rather than following only six axes.
            for(int x=-1;x<=1&&score>.0002f;x++)
            for(int y=-1;y<=1&&score>.0002f;y++)
            for(int z=-1;z<=1&&score>.0002f;z++)
            {
                var direction=new Vector3(x,y,z).normalized;
                Consider(authoredPosition-root+torso.Rotation*direction*.10f,Quaternion.identity);
            }
            for (float step = .04f; step >= .001f; step *= .5f)
            {
                for (int pass = 0; pass < 3; pass++)
                {
                    Vector3 previous = best;
                    Quaternion previousRotation=bestRotation;
                    for (int axis = 0; axis < 3; axis++)
                    for (int sign = -1; sign <= 1; sign += 2)
                    {
                        Vector3 direction = torso.Rotation * (axis == 0 ? Vector3.forward : axis == 1 ? Vector3.up : Vector3.right);
                        Consider(previous+direction*(step*sign),previousRotation);
                        if(limit>0)Consider(previous,CharacterArmIK.ClampRotation(
                            Quaternion.AngleAxis(step*200*sign,direction)*previousRotation,limit));
                    }
                    if (score <= .0002f || ((best - previous).sqrMagnitude < 1e-10f&&CharacterArmIK.RotationDegrees(bestRotation*Quaternion.Inverse(previousRotation))<.001f)) break;
                }
                if (score <= .0002f) break;
            }
            rotation=bestRotation;
            return best;
        }
        float ArmsClearanceCost(Vector3 right, Vector3 left, CharacterArmClearance.Torso torso)
        {
            return Mathf.Max(ArmClearanceCost(rightUpper, rightLower, rightHand, right, false, torso),
                ArmClearanceCost(leftUpper, leftLower, leftHand, left, true, torso));
        }
        float ArmClearanceCost(Transform upper, Transform lower, Transform hand, Vector3 target, bool left, CharacterArmClearance.Torso torso)
        {
            Vector3 hint = chest.position + torso.Rotation * ((left ? profile.LeftElbow : profile.RightElbow) * Mathf.Abs(transform.lossyScale.x));
            var pose = CharacterArmIK.Solve(upper.position, target, Vector3.Distance(upper.position, lower.position),
                Vector3.Distance(lower.position, hand.position), hint, -transform.up, Vector3.zero, profile.MaximumReach, Mathf.PI);
            pose = CharacterArmClearance.AvoidTorso(pose, upper.position, torso);
            return pose.Residual + CharacterArmClearance.Penetration(torso, upper.position, pose.Elbow, pose.Wrist);
        }
        void ShiftMeleeWithinReach(Transform upper, Transform lower, Transform hand,
            Vector3 wristTarget, Vector3 authoredPosition)
        {
            Vector3 delta = wristTarget - upper.position;
            float distance = delta.magnitude;
            if (distance < .0001f) return;
            CharacterArmIK.ReachLimits(Vector3.Distance(upper.position, lower.position),
                Vector3.Distance(lower.position, hand.position), profile.MaximumReach, out float minimum, out float maximum);
            float corrected = Mathf.Clamp(distance, minimum + .002f, maximum - .002f);
            Vector3 correction = delta * ((corrected - distance) / distance);
            weaponVisual.transform.position = authoredPosition +
                Vector3.ClampMagnitude(weaponVisual.transform.position + correction - authoredPosition, .10f);
        }
        void ApplySingleHandPose()
        {
            var runtime = subscribedWeapon ? subscribedWeapon.RuntimeState : null;
            HasFirearmPose = profile.Pistol && runtime != null;
            float scale = Mathf.Abs(transform.lossyScale.x);
            float aim = runtime != null ? runtime.AimAmount * (1 - sprintWeight) * (1 - slideWeight) : 0;
            if (frameSeconds > 0) aimWeight = Mathf.MoveTowards(aimWeight, aim, frameSeconds / profile.PoseBlendSeconds);
            float pitch = view ? Mathf.Clamp(Mathf.DeltaAngle(0, view.localEulerAngles.x), -55, 55) : 0;
            aimPitch = Mathf.Lerp(aimPitch, pitch, 1 - Mathf.Exp(-frameSeconds / profile.PoseBlendSeconds));
            var facing = transform.rotation * Quaternion.Euler(aimPitch, 0, 0);
            profile.SampleAttack(observedMelee ? observedMelee.Simulation : null, frameSeconds, ref meleePosition, ref meleeRotation);
            float kick = Mathf.Clamp01(recoil / Mathf.Max(.001f, profile.RecoilDistance));
            readySeconds = Mathf.Max(0, readySeconds - frameSeconds);
            readyWeight = profile.StepReady(readyWeight, readySeconds, frameSeconds);
            var readyOffset = profile.Pistol ? Vector3.Lerp(profile.HipGripOffset, profile.RaisedGripOffset, readyWeight) : profile.HipGripOffset;
            var readyRotation = profile.Pistol ? Quaternion.Slerp(Quaternion.Euler(profile.HipEuler), Quaternion.Euler(profile.RaisedEuler), readyWeight) : Quaternion.Euler(profile.HipEuler);
            var offset = Vector3.Lerp(readyOffset, profile.AimGripOffset, aimWeight) + meleePosition +
                profile.SprintOffset * sprintWeight + profile.CrouchOffset * (state.Crouching && !state.Sliding ? 1 : 0) + Vector3.down * (.06f * slideWeight);
            float assist = 0, extraction = 0;
            if (profile.Pistol && sockets.ReloadReference) PistolReloadPose.Sample(runtime, out assist, out extraction);
            if (runtime != null)
            {
                float lower = runtime.State == WeaponState.Equipping ? 1 - Mathf.Clamp01(runtime.StateTimer / Mathf.Max(.01f, runtime.Definition.EquipSeconds)) :
                    runtime.State == WeaponState.Unequipping ? Mathf.Clamp01(runtime.StateTimer / Mathf.Max(.01f, runtime.Definition.UnequipSeconds)) : 0;
                offset += Vector3.down * (.10f * lower);
            }
            offset += profile.PistolReloadPosition * assist;
            var rotation = facing * Quaternion.Slerp(readyRotation, Quaternion.Euler(profile.AimEuler), aimWeight) *
                meleeRotation * Quaternion.Euler(profile.SprintEuler * sprintWeight) * Quaternion.Euler(profile.PistolReloadEuler * assist) * Quaternion.Euler(-kick * profile.RecoilPitch, 0, 90);
            var contact = rightUpper.position + facing * (offset * scale) - facing * Vector3.forward * recoil;
            rightPalm.WristPose(contact, rotation, out var position, out var wrist);
            AssistShoulder(rightShoulder, rightUpper, rightLower, rightHand, position, poseWeight);
            RightWristTarget = position;
            RightElbowHint = chest.position + transform.rotation * (profile.RightElbow * scale);
            var solved = SolveArm(rightUpper, rightLower, rightHand, position, wrist, RightElbowHint, poseWeight, ref rightBendLocal);
            RightSolvedWrist = solved.Wrist; RightReachResidual = solved.Residual;
            CharacterWeaponSockets.Attach(weaponVisual.transform, sockets.PrimaryGrip, rightPalm.PalmPosition, rightPalm.PalmRotation);
            rightPalm.Apply(profile.FingerGripWeight * poseWeight, profile.Pistol, kick, profile.FingerClosure, profile,
                profile.Pistol ? profile.WorldPistolFingers : null);
            ApplyPistolSupport(runtime, assist, extraction, scale);
        }
        void ApplyPistolSupport(WeaponRuntimeState runtime, float assist, float extraction, float scale)
        {
            if (sockets.Magazine)
                sockets.Magazine.localPosition = magazineRest + Vector3.down * (profile.PistolMagazineTravel * extraction);
            SupportHandWeight = 0;
            LeftReachResidual = LeftTorsoPenetration = 0;
            if (leftPalm == null || !leftPalm.Valid || !leftUpper || !leftLower || !leftHand || !chest) return;

            bool requested = profile.Pistol && profile.WorldTwoHandedPistol;
            if (!requested || !profile.ValidWorldPistolSupport || !sockets.HasWorldPistolSupport)
            {
                pistolSupportWeight = pistolReloadBlend = 0;
                if (requested && !pistolSupportWarningIssued)
                {
                    Debug.LogWarning("TPS pistol support configuration is invalid; using reload-only left hand.", this);
                    pistolSupportWarningIssued = true;
                }
                ApplyLegacyPistolReload(assist, scale);
                return;
            }

            float step = Mathf.Max(0, frameSeconds) / Mathf.Max(.01f, profile.PoseBlendSeconds);
            bool hold = runtime != null && (runtime.State == WeaponState.Ready || runtime.State == WeaponState.Reloading);
            // Carry with both hands through sprint/slide; the weapon pose owns lowering.
            float targetWeight = hold ? Mathf.Clamp01(poseWeight) * profile.HandWeight : 0;
            pistolSupportWeight = Mathf.MoveTowards(pistolSupportWeight, targetWeight, step);
            float reload = runtime != null && runtime.State == WeaponState.Reloading && sockets.ReloadReference
                ? Mathf.Clamp01(assist) : 0;
            pistolReloadBlend = Mathf.MoveTowards(pistolReloadBlend, reload, step);
            SupportHandWeight = pistolSupportWeight;
            if (pistolSupportWeight <= 0) return;

            // Resolve contacts after the right hand attaches the weapon. Filter only the
            // reload transition: world-space filtering would introduce recoil separation.
            var contact = sockets.PistolSupport.position;
            var rotation = sockets.PistolSupport.rotation;
            if (sockets.ReloadReference && pistolReloadBlend > 0)
            {
                contact = Vector3.Lerp(contact, sockets.ReloadReference.position, pistolReloadBlend);
                rotation = Quaternion.Slerp(rotation, sockets.ReloadReference.rotation, pistolReloadBlend);
            }
            LeftPalmTarget = contact;
            leftPalm.WristPose(contact, rotation, out var position, out var wrist);
            LeftWristTarget = position;
            LeftElbowHint = chest.position + transform.rotation * (profile.LeftElbow * scale);
            var solved = SolveArm(leftUpper, leftLower, leftHand, position, wrist,
                LeftElbowHint, pistolSupportWeight, ref leftBendLocal);
            LeftSolvedWrist = solved.Wrist;
            LeftReachResidual = solved.Residual;
            leftPalm.Apply(profile.FingerGripWeight * pistolSupportWeight * Mathf.Lerp(1, .5f, pistolReloadBlend),
                false, 0, Mathf.Lerp(profile.WorldSupportFingerClosure, .35f, pistolReloadBlend), profile, null, true);
        }
        void ApplyLegacyPistolReload(float assist, float scale)
        {
            if (assist <= 0 || !sockets.ReloadReference) return;
            SupportHandWeight = assist;
            LeftPalmTarget = sockets.ReloadReference.position;
            leftPalm.WristPose(LeftPalmTarget, sockets.ReloadReference.rotation, out var position, out var rotation);
            LeftWristTarget = position;
            LeftElbowHint = chest.position + transform.rotation * (profile.LeftElbow * scale);
            var solved = SolveArm(leftUpper, leftLower, leftHand, position, rotation, LeftElbowHint, assist, ref leftBendLocal);
            LeftSolvedWrist = solved.Wrist; LeftReachResidual = solved.Residual;
            leftPalm.Apply(profile.FingerGripWeight * assist * .5f, false, 0);
        }
        public static float CalculateTorsoYawCorrection(Quaternion anatomicalFrame, Quaternion actorFrame, float desiredYaw, float weight)
        {
            Vector3 forward = Quaternion.Inverse(actorFrame) * (anatomicalFrame * Vector3.forward);
            if (forward.x * forward.x + forward.z * forward.z < .0001f) return 0;
            float animatedYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            // Bound assistance for clips facing away/near vertical; never force an extreme torso twist.
            return Mathf.Clamp(Mathf.DeltaAngle(animatedYaw, desiredYaw), -60, 60) * Mathf.Clamp01(weight);
        }
        void RotateTorso(Transform bone, float pitch, float yaw)
        {
            bone.rotation = Quaternion.AngleAxis(pitch, transform.right) * Quaternion.AngleAxis(yaw, transform.up) * bone.rotation;
        }
        void AssistShoulder(Transform shoulder, Transform upper, Transform lower, Transform hand, Vector3 target, float weight)
        {
            if (!shoulder || weight <= 0 || profile.ShoulderReachDegrees <= 0) return;
            float length = Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, hand.position);
            shoulder.rotation = CharacterArmIK.ReachAssistance(shoulder.position, upper.position, target,
                length, profile.ShoulderReachDegrees, weight) * shoulder.rotation;
        }
        float SupportReachYaw(Vector3 leftTarget, Vector3 rightTarget)
        {
            float leftA = Vector3.Distance(leftUpper.position, leftLower.position);
            float leftB = Vector3.Distance(leftLower.position, leftHand.position);
            CharacterArmIK.ReachLimits(leftA, leftB, profile.MaximumReach, out _, out float leftReach);
            float yaw = CharacterArmIK.StockReachYaw(StockTarget - leftUpper.position,
                leftTarget - StockTarget, transform.up, leftReach, Mathf.Clamp(profile.TorsoReachYawDegrees, 0, 12));
            if (Mathf.Abs(yaw) < .00001f) return 0;
            float rightA = Vector3.Distance(rightUpper.position, rightLower.position);
            float rightB = Vector3.Distance(rightLower.position, rightHand.position);
            CharacterArmIK.ReachLimits(rightA, rightB, profile.MaximumReach, out float rightMin, out float rightMax);
            float distance = Vector3.Distance(rightUpper.position, rightTarget);
            float initialError = Mathf.Abs(distance - Mathf.Clamp(distance, rightMin, rightMax));
            // Preserve the higher-priority trigger-hand reach, including its minimum fold distance.
            float low = 0, high = 1;
            for (int i = 0; i < 12; i++)
            {
                float fraction = i == 0 ? 1 : (low + high) * .5f;
                float candidate = (Quaternion.AngleAxis(yaw * fraction, transform.up) *
                    (StockTarget - rightUpper.position) + rightTarget - StockTarget).magnitude;
                float error = Mathf.Abs(candidate - Mathf.Clamp(candidate, rightMin, rightMax));
                if (error <= initialError + .00001f)
                {
                    low = fraction;
                    if (fraction == 1) break;
                }
                else high = fraction;
            }
            return yaw * low;
        }
        void RestorePose()
        {
            rightPalm?.Restore(); leftPalm?.Restore();
            if (!poseCaptured) return;
            for (int i = 0; i < posedBones.Length; i++) if (posedBones[i]) posedBones[i].localRotation = previousRotations[i];
            poseCaptured = false;
        }
        CharacterArmIK.Solution SolveArm(Transform upper, Transform lower, Transform hand,
            Vector3 target, Quaternion rotation, Vector3 elbow, float weight, ref Vector3 previousBendLocal)
        {
            Vector3 origin = upper.position;
            float a = Vector3.Distance(origin, lower.position), b = Vector3.Distance(lower.position, hand.position);
            Vector3 blendedTarget = Vector3.Lerp(hand.position, target, weight);
            Vector3 hint = Vector3.Lerp(lower.position, elbow, Mathf.Clamp01(profile.ElbowWeight) * weight);
            bool supportedPistol = profile.Pistol && profile.WorldTwoHandedPistol;
            var solution = CharacterArmIK.Solve(origin, blendedTarget, a, b, hint, -transform.up,
                supportedPistol ? Vector3.zero : transform.rotation * previousBendLocal, profile.MaximumReach,
                profile.ElbowTurnDegreesPerSecond * Mathf.Deg2Rad * frameSeconds);
            if (!solution.Valid) return solution;
            solution = supportedPistol
                ? CharacterArmClearance.AvoidTorsoLowerElbow(solution, origin, TorsoClearance, transform.up)
                : CharacterArmClearance.AvoidTorso(solution, origin, TorsoClearance);
            if (supportedPistol)
                solution = CharacterArmIK.StabilizeBend(solution, origin, transform.rotation * previousBendLocal,
                    profile.ElbowTurnDegreesPerSecond, frameSeconds);
            if (weight > 0)
            {
                Quaternion initialHandRotation = hand.rotation;
                upper.rotation = Quaternion.FromToRotation(lower.position - origin, solution.Elbow - origin) * upper.rotation;
                lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, solution.Wrist - lower.position) * lower.rotation;
                Quaternion wristRotation = Quaternion.Slerp(initialHandRotation, rotation, weight);
                // Share pronation/supination with the forearm instead of concentrating all roll at the wrist.
                lower.rotation = CharacterArmIK.ForearmTwist(hand.rotation, wristRotation,
                    hand.position - lower.position, profile.ForearmTwistDegrees * weight) * lower.rotation;
                hand.rotation = wristRotation;
                previousBendLocal = Quaternion.Inverse(transform.rotation) * solution.Bend;
            }
            float penetration = CharacterArmClearance.Penetration(TorsoClearance, upper.position, lower.position, hand.position);
            if (hand == rightHand) RightTorsoPenetration = penetration;
            else LeftTorsoPenetration = penetration;
            // Report reach of the requested contact, independently from the intentional entry blend.
            float distance = Vector3.Distance(origin, target);
            CharacterArmIK.ReachLimits(a, b, profile.MaximumReach, out float min, out float max);
            solution.Residual = Mathf.Abs(distance - Mathf.Clamp(distance, min, max));
            return solution;
        }
        void ClearIK()
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0); animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0); animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0); animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 0);
        }
    }
}
