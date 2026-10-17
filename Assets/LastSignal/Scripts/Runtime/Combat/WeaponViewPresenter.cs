using UnityEngine;

namespace LastSignal
{
    /// <summary>
    /// Manages viewmodel position: hip pose, ADS pose interpolation, sway, bob, and visual recoil.
    /// Driven by WeaponRuntimeState.AimAmount. Does NOT move the gameplay player controller.
    /// </summary>
    [DefaultExecutionOrder(250)]
    public sealed class WeaponViewPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] WeaponController weapon;
        [SerializeField] PlayerInputReader input;
        [SerializeField] Transform viewmodelRoot; // The transform we move for hip/ADS pose
        [SerializeField] CharacterWeaponPoseProfile gripProfile;
        PlayerStance stance;
        FirstPersonMotor motor;

        [Header("Movement Pose")]
        [SerializeField] Vector3 crouchPositionOffset = new Vector3(0, -.025f, -.015f);
        [SerializeField] Vector3 crouchRotationOffset;
        [SerializeField] Vector3 slidePositionOffset = new Vector3(0, -.11f, -.07f);
        [SerializeField] Vector3 slideRotationOffset = new Vector3(7f, 0, 5f);
        [SerializeField, Min(.01f)] float crouchTransitionSeconds = .12f;
        [SerializeField, Min(.01f)] float slideTransitionSeconds = .09f;

        [Header("Hip Pose")]
        [SerializeField] Vector3 hipPosition = new Vector3(.15f, -.12f, .35f);
        [SerializeField] Vector3 hipRotation = Vector3.zero;

        [Header("ADS Pose")]
        [SerializeField] Vector3 adsPosition = new Vector3(0, -.06f, .2f);
        [SerializeField] Vector3 adsRotation = Vector3.zero;
        [SerializeField] bool alignToSight;
        [SerializeField] float sightDistance = .22f;

        [Header("Sway")]
        [SerializeField, Min(0)] float swayAmount = .002f;
        [SerializeField, Min(0)] float swaySmooth = 8f;

        [Header("Bob")]
        [SerializeField, Min(0)] float bobFrequency = 8f;
        [SerializeField, Min(0)] float bobAmplitude = .003f;

        [Header("Visual Recoil")]
        [SerializeField, Min(0)] float recoilKickback = .015f;
        [SerializeField, Min(0)] float recoilRecovery = 12f;

        Vector3 currentSway;
        float bobTimer;
        float recoilOffset;
        float crouchWeight;
        float slideWeight, sprintWeight, shotCarryDelay;
        public float SprintWeight => sprintWeight;
        float presentationAim;
        float readySeconds, readyWeight;
        public float ReadyWeight => readyWeight;
        public void SetGripProfile(CharacterWeaponPoseProfile profile)
        { gripProfile = profile; readySeconds = readyWeight = recoilOffset = presentationAim = sprintWeight = shotCarryDelay = 0; }
        public float TriggerPull => gripProfile ? Mathf.Clamp01(recoilOffset / Mathf.Max(.001f, gripProfile.RecoilDistance)) : 0;

        public void Configure(WeaponController ctrl, PlayerInputReader reader, Transform root)
        {
            Configure(ctrl, reader, root, GetComponentInParent<PlayerStance>(),
                GetComponentInParent<FirstPersonMotor>());
        }

        public void Configure(WeaponController ctrl, PlayerInputReader reader, Transform root,
            PlayerStance playerStance, FirstPersonMotor playerMotor)
        {
            weapon = ctrl;
            input = reader;
            viewmodelRoot = root;
            stance = playerStance;
            motor = playerMotor;
        }

        void OnEnable()
        {
            if (weapon) weapon.ShotFired += OnShotFired;
        }

        void OnDisable()
        {
            if (weapon) weapon.ShotFired -= OnShotFired;
            readySeconds = readyWeight = 0;
            recoilOffset = crouchWeight = slideWeight = sprintWeight = shotCarryDelay = bobTimer = presentationAim = 0; currentSway = Vector3.zero;
        }

        void LateUpdate()
        {
            if (!weapon || weapon.RuntimeState == null || !viewmodelRoot) return;

            float dt = weapon.IsSimulationActive ? Time.deltaTime : 0;
            if (gripProfile) { hipPosition = gripProfile.FpsHipPosition; hipRotation = gripProfile.FpsHipEuler; alignToSight = true; sightDistance = gripProfile.SightDistance; }
            crouchWeight = Mathf.MoveTowards(crouchWeight,
                stance && stance.IsCrouching ? 1f : 0f, dt / crouchTransitionSeconds);
            slideWeight = Mathf.MoveTowards(slideWeight,
                motor && motor.IsSliding ? 1f : 0f, dt / slideTransitionSeconds);
            shotCarryDelay = Mathf.Max(0, shotCarryDelay - dt);
            bool carrying = gripProfile && gripProfile.FpsSprintCarry && motor && motor.IsSprinting &&
                weapon.RuntimeState.State == WeaponState.Ready && weapon.RuntimeState.AimAmount < .01f && shotCarryDelay <= 0;
            sprintWeight = Mathf.MoveTowards(sprintWeight, carrying ? 1 : 0,
                dt / (gripProfile ? Mathf.Max(.01f, gripProfile.PoseBlendSeconds) : .15f));
            float aim = weapon.RuntimeState.AimAmount * (1f - slideWeight);
            if (gripProfile && !weapon.IsSimulationActive) aim = presentationAim;
            else presentationAim = aim;

            Vector3 readyPosition = hipPosition;
            Quaternion readyRotation = Quaternion.Euler(hipRotation);
            if (gripProfile && gripProfile.Pistol)
            {
                readySeconds = Mathf.Max(0, readySeconds - dt);
                readyWeight = gripProfile.StepReady(readyWeight, readySeconds, dt);
                gripProfile.ReadyPose(readyWeight, out readyPosition, out readyRotation);
            }
            // Compose low/raised ready before sight-space ADS, maintaining one arm target.
            Vector3 targetPos = Vector3.Lerp(readyPosition, adsPosition, aim);
            Quaternion targetRot = Quaternion.Slerp(readyRotation, Quaternion.Euler(adsRotation), aim);
            if (alignToSight && weapon.AimReference)
            {
                var sight = weapon.AimReference;
                Vector3 localSight = viewmodelRoot.InverseTransformPoint(sight.position);
                Quaternion localSightRotation = Quaternion.Inverse(viewmodelRoot.rotation) * sight.rotation;
                Quaternion alignedRotation = Quaternion.Inverse(localSightRotation);
                Vector3 alignedPosition = new Vector3(0, 0, sightDistance) - alignedRotation * Vector3.Scale(localSight, viewmodelRoot.localScale);
                targetPos = Vector3.Lerp(readyPosition, alignedPosition, aim);
                targetRot = Quaternion.Slerp(readyRotation, alignedRotation, aim);
            }
            if (gripProfile && gripProfile.Pistol)
            {
                PistolReloadPose.Sample(weapon.RuntimeState, out float reloadAssist, out _);
                targetPos += gripProfile.PistolReloadPosition * reloadAssist;
                targetRot *= Quaternion.Euler(gripProfile.PistolReloadEuler * reloadAssist);
            }

            if (gripProfile && gripProfile.FpsSprintCarry)
            {
                // One owner moves the complete weapon frame; IK follows both contacts.
                targetPos = Vector3.Lerp(targetPos, gripProfile.FpsSprintPosition, sprintWeight);
                targetRot = Quaternion.Slerp(targetRot, Quaternion.Euler(gripProfile.FpsSprintEuler), sprintWeight);
            }

            // Preserve the authored sight alignment at full ADS.
            float crouchHipWeight = crouchWeight * (1f - aim) * (1f - slideWeight);
            targetPos += crouchPositionOffset * crouchHipWeight + slidePositionOffset * slideWeight;
            targetRot *= Quaternion.Euler(
                crouchRotationOffset * crouchHipWeight + slideRotationOffset * slideWeight);

            // Sway from mouse look (suppressed by ADS).
            if (input)
            {
                Vector2 look = input.Look;
                Vector3 sway = new Vector3(-look.x * swayAmount, -look.y * swayAmount, 0) * (1f - aim);
                currentSway = Vector3.Lerp(currentSway, sway, dt * swaySmooth);
            }

            // Movement bob (suppressed by ADS).
            if (input && input.Move.sqrMagnitude > .01f && slideWeight < .01f)
                bobTimer += dt * bobFrequency * Mathf.Lerp(1, 1.45f, sprintWeight);
            else
                bobTimer = Mathf.Lerp(bobTimer, 0, dt * 4f);

            float amplitude = gripProfile && gripProfile.FpsSprintCarry
                ? Mathf.Lerp(bobAmplitude, gripProfile.FpsSprintBobAmplitude, sprintWeight) : bobAmplitude;
            float bobX = Mathf.Sin(bobTimer) * amplitude * (1f - aim) * (1f - slideWeight);
            float bobY = Mathf.Sin(bobTimer * 2f) * amplitude * .5f * (1f - aim) * (1f - slideWeight);
            targetRot *= Quaternion.Euler(Mathf.Sin(bobTimer * 2f) * .45f * sprintWeight,
                0, Mathf.Sin(bobTimer) * .7f * sprintWeight);

            // Visual recoil recovery.
            recoilOffset = Mathf.Lerp(recoilOffset, 0, dt * (gripProfile ? gripProfile.RecoilRecovery : recoilRecovery));

            // Compose final viewmodel transform.
            Vector3 finalPos = targetPos + currentSway * (1f - aim) + new Vector3(bobX, bobY, -recoilOffset);
            viewmodelRoot.localPosition = finalPos;
            viewmodelRoot.localRotation = targetRot * (gripProfile ? Quaternion.Euler(-Mathf.Clamp01(recoilOffset / Mathf.Max(.001f, gripProfile.RecoilDistance)) * gripProfile.RecoilPitch, 0, 0) : Quaternion.identity);
        }

        void OnShotFired(WeaponFireResolver.ShotResult result)
        {
            shotCarryDelay = .18f;
            if (gripProfile && gripProfile.Pistol) readySeconds = gripProfile.ReadyHoldSeconds;
            float kick = gripProfile ? gripProfile.RecoilDistance : recoilKickback;
            if (weapon.RuntimeState != null)
                kick *= Mathf.Lerp(1f, weapon.Definition.AdsRecoilMultiplier, weapon.RuntimeState.AimAmount);
            recoilOffset = gripProfile ? Mathf.Min(recoilOffset + kick, gripProfile.RecoilDistance * 2) : recoilOffset + kick;
        }
    }
}
