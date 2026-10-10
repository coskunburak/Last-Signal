using UnityEngine;

namespace LastSignal
{
    /// <summary>Final firearm pose after Animator evaluation: stock contact, calibrated palms, arms and fingers.</summary>
    [DefaultExecutionOrder(300), DisallowMultipleComponent, RequireComponent(typeof(Animator))]
    public sealed class CharacterWeaponRig : MonoBehaviour
    {
        [SerializeField] CharacterWeaponPoseProfile[] profiles;
        Animator animator;
        PlayerLocomotionPresenter state;
        CharacterBodyVisibility visibility;
        Transform view;
        Transform chest, head, rightHand, rightUpper, rightLower, leftHand, leftUpper, leftLower;
        CharacterHandPose rightPalm, leftPalm;
        Transform[] posedBones;
        Quaternion[] previousRotations;
        bool poseCaptured;
        float poseWeight;
        public Vector3 RightWristTarget { get; private set; }
        public Vector3 LeftWristTarget { get; private set; }
        public Vector3 StockTarget { get; private set; }
        CharacterWeaponPoseProfile profile;
        CharacterWeaponSockets sockets;
        GameObject weaponVisual;
        WeaponController subscribedWeapon;
        MeleeWeaponController observedMelee;
        Vector3 magazineRest;
        float recoil;
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
            head = animator.GetBoneTransform(HumanBodyBones.Head);
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            rightUpper = animator.GetBoneTransform(HumanBodyBones.RightUpperArm); rightLower = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            leftUpper = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm); leftLower = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            if (!chest || !head || !rightHand || !leftHand || !rightUpper || !rightLower || !leftUpper || !leftLower)
            { enabled = false; return; }
            rightPalm = new CharacterHandPose(animator, false); leftPalm = new CharacterHandPose(animator, true);
            if (!rightPalm.Valid || !leftPalm.Valid) { enabled = false; return; }
            posedBones = new[] { chest, head, rightUpper, rightLower, rightHand, leftUpper, leftLower, leftHand };
            previousRotations = new Quaternion[posedBones.Length];
        }
        void OnDisable()
        {
            RestorePose(); poseWeight = 0;
            Subscribe(null); observedMelee = null; recoil = 0;
            if (weaponVisual) { weaponVisual.SetActive(false); Destroy(weaponVisual); }
            weaponVisual = null; sockets = null; profile = null;
            if (visibility) visibility.RefreshRenderers();
        }
        void Subscribe(WeaponController next)
        {
            if (ReferenceEquals(subscribedWeapon, next)) return;
            if (!ReferenceEquals(subscribedWeapon, null)) subscribedWeapon.ShotFired -= OnShot;
            subscribedWeapon = next;
            if (subscribedWeapon) subscribedWeapon.ShotFired += OnShot;
        }
        void OnShot(WeaponFireResolver.ShotResult shot)
        { if (state) state.InterruptCosmeticAction(); if (profile) recoil = Mathf.Min(.08f, recoil + profile.RecoilDistance); }
        void Update()
        {
            // Restore the preceding frame before Animator evaluation; never accumulate procedural offsets.
            RestorePose();
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
            if (current != lastWeaponState && (current == WeaponState.Reloading || current == WeaponState.Equipping)) state.InterruptCosmeticAction();
            lastWeaponState = current;
            CharacterWeaponPoseProfile next = null;
            if (profiles != null) foreach (var candidate in profiles)
                if (candidate && ((firearm && candidate.Firearm == firearm.Definition) || (melee && candidate.Melee == melee.Definition)))
                { next = candidate; break; }
            if (profile != next || observedMelee != melee) ReplaceVisual(next, melee);
            if (!weaponVisual) return;
            bool shown = state.Alive && !state.SeatedNow && !state.Treating && (!state.UpperActionBusy || melee) &&
                ((firearm && firearm.isActiveAndEnabled && firearm.RuntimeState != null && firearm.RuntimeState.State != WeaponState.Holstered) ||
                 (melee && melee.isActiveAndEnabled));
            weaponVisual.SetActive(shown);
            if (!shown || melee) poseWeight = 0;
            if (state.PresentationActive) recoil = Mathf.MoveTowards(recoil, 0, Time.deltaTime * profile.RecoilRecovery * .025f);
        }
        void ReplaceVisual(CharacterWeaponPoseProfile next, MeleeWeaponController melee)
        {
            if (weaponVisual) { weaponVisual.SetActive(false); Destroy(weaponVisual); }
            profile = next; observedMelee = melee; weaponVisual = null; sockets = null; recoil = 0;
            poseWeight = 0;
            if (profile && profile.WorldPrefab)
            {
                weaponVisual = Instantiate(profile.WorldPrefab, transform);
                weaponVisual.name = "WorldWeapon_" + profile.name;
                if (profile.Firearm) weaponVisual.transform.localScale *= profile.WorldScale;
                sockets = weaponVisual.GetComponent<CharacterWeaponSockets>();
                if (sockets && sockets.Magazine) magazineRest = sockets.Magazine.localPosition;

            }
            if (visibility) visibility.RefreshRenderers();
        }
        void LateUpdate()
        {
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
            if (state.PresentationActive) poseWeight = Mathf.MoveTowards(poseWeight, 1, Time.deltaTime / Mathf.Max(.01f, profile.PoseBlendSeconds));
            for (int i = 0; i < posedBones.Length; i++) previousRotations[i] = posedBones[i].localRotation;
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
            float aim = state.AimAmount;
            var stockOffset = Vector3.Lerp(profile.HipStockOffset, profile.AimStockOffset, aim);
            var angles = Vector3.Lerp(profile.HipEuler, profile.AimEuler, aim);
            if (state.Motor && state.Motor.IsSprinting) angles += profile.SprintEuler;
            if (state.Sliding) angles += profile.SlideEuler;
            float pitch = 0;
            if (view) pitch = Mathf.Clamp(Mathf.DeltaAngle(0, view.localEulerAngles.x), -55, 55);
            pitch *= Mathf.Lerp(.35f, 1, aim);
            Quaternion originalHead = head.rotation;
            chest.rotation = Quaternion.AngleAxis(pitch * profile.TorsoPitchWeight * poseWeight, transform.right) *
                Quaternion.AngleAxis(profile.TorsoYaw * poseWeight, transform.up) * chest.rotation;
            // Counter the stance yaw at the neck/head; the character keeps looking along gameplay aim.
            head.rotation = Quaternion.AngleAxis(pitch * .4f * poseWeight, transform.right) * originalHead;
            Quaternion facing = transform.rotation * Quaternion.Euler(pitch, 0, 0);
            float lower = 0;
            if (runtime.State == WeaponState.Equipping)
                lower += .10f * (1 - Mathf.Clamp01(runtime.StateTimer / Mathf.Max(.01f, runtime.Definition.EquipSeconds)));
            if (runtime.State == WeaponState.Unequipping)
                lower += .10f * Mathf.Clamp01(runtime.StateTimer / Mathf.Max(.01f, runtime.Definition.UnequipSeconds));
            StockTarget = rightUpper.position + transform.rotation * (stockOffset + Vector3.down * lower) - facing * Vector3.forward * Mathf.Min(.012f, recoil);
            weaponVisual.transform.rotation = facing * Quaternion.Euler(angles);
            // Position by the actual rear stock surface. Never pull the whole rifle backwards to reach a hand.
            weaponVisual.transform.position += StockTarget - sockets.StockContact.position;
            float reload = runtime.State == WeaponState.Reloading
                ? Mathf.Clamp01(runtime.StateTimer / Mathf.Max(.01f, runtime.IsEmptyReload ? runtime.Definition.EmptyReloadSeconds : runtime.Definition.TacticalReloadSeconds)) : 0;
            float reloadReach = Mathf.Sin(reload * Mathf.PI);
            Vector3 leftTarget = Vector3.Lerp(sockets.LeftGrip.position, sockets.ReloadReference.position, reloadReach);
            if (sockets.Magazine) sockets.Magazine.localPosition = magazineRest + Vector3.down * (.12f * reloadReach);
            rightPalm.WristPose(sockets.RightGrip.position, sockets.RightGrip.rotation, out var rightPosition, out var rightRotation);
            leftPalm.WristPose(leftTarget, Quaternion.Slerp(sockets.LeftGrip.rotation, sockets.ReloadReference.rotation, reloadReach), out var leftPosition, out var leftRotation);
            RightWristTarget = rightPosition; LeftWristTarget = leftPosition;
            float weight = profile.HandWeight * poseWeight;
            SolveArm(rightUpper, rightLower, rightHand, rightPosition, rightRotation, profile.RightElbow, weight);
            SolveArm(leftUpper, leftLower, leftHand, leftPosition, leftRotation, profile.LeftElbow, weight);
            rightPalm.Apply(profile.FingerGripWeight * weight, true, Mathf.Clamp01(recoil / Mathf.Max(.001f, profile.RecoilDistance)));
            leftPalm.Apply(profile.FingerGripWeight * weight * (1 - .55f * reloadReach), false, 0);
        }
        void RestorePose()
        {
            rightPalm?.Restore(); leftPalm?.Restore();
            if (!poseCaptured) return;
            for (int i = 0; i < posedBones.Length; i++) if (posedBones[i]) posedBones[i].localRotation = previousRotations[i];
            poseCaptured = false;
        }
        void SolveArm(Transform upper, Transform lower, Transform hand,
            Vector3 target, Quaternion rotation, Vector3 elbow, float weight)
        {
            Vector3 origin = upper.position;
            float a = Vector3.Distance(origin, lower.position), b = Vector3.Distance(lower.position, hand.position);
            target = Vector3.Lerp(hand.position, target, weight);
            Vector3 delta = target - origin;
            if (a < .0001f || b < .0001f || delta.sqrMagnitude < .000001f) return;
            float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(a - b) + .001f, (a + b) * profile.MaximumReach);
            Vector3 direction = delta.normalized;
            Vector3 hint = Vector3.Lerp(lower.position, chest.position + transform.rotation * elbow, profile.ElbowWeight);
            Vector3 bend = Vector3.ProjectOnPlane(hint - origin, direction);
            if (bend.sqrMagnitude < .000001f) bend = Vector3.ProjectOnPlane(-transform.up, direction);
            if (bend.sqrMagnitude < .000001f) bend = Vector3.ProjectOnPlane(transform.right, direction);
            float along = (a * a + distance * distance - b * b) / (2 * distance);
            Vector3 middle = origin + direction * along + bend.normalized * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            Quaternion initialHandRotation = hand.rotation;
            upper.rotation = Quaternion.FromToRotation(lower.position - origin, middle - origin) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, origin + direction * distance - lower.position) * lower.rotation;
            hand.rotation = Quaternion.Slerp(initialHandRotation, rotation, weight);
        }
        void ClearIK()
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0); animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0); animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0); animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 0);
        }
    }
}
