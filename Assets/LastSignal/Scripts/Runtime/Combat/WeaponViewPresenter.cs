using UnityEngine;

namespace LastSignal
{
    /// <summary>
    /// Manages viewmodel position: hip pose, ADS pose interpolation, sway, bob, and visual recoil.
    /// Driven by WeaponRuntimeState.AimAmount. Does NOT move the gameplay player controller.
    /// </summary>
    public sealed class WeaponViewPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] WeaponController weapon;
        [SerializeField] PlayerInputReader input;
        [SerializeField] Transform viewmodelRoot; // The transform we move for hip/ADS pose
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
        float slideWeight;

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
        }

        void LateUpdate()
        {
            if (!weapon || weapon.RuntimeState == null || !viewmodelRoot) return;

            float dt = Time.deltaTime;
            crouchWeight = Mathf.MoveTowards(crouchWeight,
                stance && stance.IsCrouching ? 1f : 0f, dt / crouchTransitionSeconds);
            slideWeight = Mathf.MoveTowards(slideWeight,
                motor && motor.IsSliding ? 1f : 0f, dt / slideTransitionSeconds);
            float aim = weapon.RuntimeState.AimAmount * (1f - slideWeight);

            // Base pose: lerp between hip and ADS.
            Vector3 targetPos = Vector3.Lerp(hipPosition, adsPosition, aim);
            Quaternion targetRot = Quaternion.Slerp(Quaternion.Euler(hipRotation), Quaternion.Euler(adsRotation), aim);
            if (alignToSight && weapon.AimReference)
            {
                var sight = weapon.AimReference;
                Vector3 localSight = viewmodelRoot.InverseTransformPoint(sight.position);
                Quaternion localSightRotation = Quaternion.Inverse(viewmodelRoot.rotation) * sight.rotation;
                Quaternion alignedRotation = Quaternion.Inverse(localSightRotation);
                Vector3 alignedPosition = new Vector3(0, 0, sightDistance) - alignedRotation * Vector3.Scale(localSight, viewmodelRoot.localScale);
                targetPos = Vector3.Lerp(hipPosition, alignedPosition, aim);
                targetRot = Quaternion.Slerp(Quaternion.Euler(hipRotation), alignedRotation, aim);
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
                bobTimer += dt * bobFrequency;
            else
                bobTimer = Mathf.Lerp(bobTimer, 0, dt * 4f);

            float bobX = Mathf.Sin(bobTimer) * bobAmplitude * (1f - aim) * (1f - slideWeight);
            float bobY = Mathf.Sin(bobTimer * 2f) * bobAmplitude * .5f * (1f - aim) * (1f - slideWeight);

            // Visual recoil recovery.
            recoilOffset = Mathf.Lerp(recoilOffset, 0, dt * recoilRecovery);

            // Compose final viewmodel transform.
            Vector3 finalPos = targetPos + currentSway * (1f - aim) + new Vector3(bobX, bobY, -recoilOffset);
            viewmodelRoot.localPosition = finalPos;
            viewmodelRoot.localRotation = targetRot;
        }

        void OnShotFired(WeaponFireResolver.ShotResult result)
        {
            float kick = recoilKickback;
            if (weapon.RuntimeState != null)
                kick *= Mathf.Lerp(1f, weapon.Definition.AdsRecoilMultiplier, weapon.RuntimeState.AimAmount);
            recoilOffset += kick;
        }
    }
}
