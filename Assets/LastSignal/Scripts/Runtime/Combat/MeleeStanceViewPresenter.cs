using UnityEngine;

namespace LastSignal
{
    [DefaultExecutionOrder(120)]
    public sealed class MeleeStanceViewPresenter : MonoBehaviour
    {
        [SerializeField] Transform visualRoot;
        [SerializeField] Vector3 crouchPositionOffset = new Vector3(0, -.025f, -.015f);
        [SerializeField] Vector3 slidePositionOffset = new Vector3(0, -.09f, -.06f);
        [SerializeField] Vector3 slideRotationOffset = new Vector3(6f, 0, 5f);
        [SerializeField, Min(.01f)] float transitionSeconds = .1f;
        [Header("Wide FOV Pose")]
        [SerializeField] float referenceFov = 75f;
        [SerializeField] float wideFov = 100f;
        [SerializeField] Vector3 wideFovPositionOffset = new Vector3(0, 0, -.12f);

        PlayerStance stance;
        FirstPersonMotor motor;
        Camera view;
        Transform stancePivot;
        Vector3 basePosition;
        Quaternion baseRotation;
        float crouchWeight;
        float slideWeight;

        void Awake()
        {
            if (!visualRoot) visualRoot = transform.Find("VisualRoot");
            if (!visualRoot)
            {
                Debug.LogError("Melee stance requires VisualRoot.", this);
                enabled = false;
                return;
            }

            // The Animator owns VisualRoot, including equip translation and swing
            // rotation. Compose stance above it, without moving the gameplay origin.
            stancePivot = new GameObject("StancePivot").transform;
            stancePivot.gameObject.layer = visualRoot.gameObject.layer;
            stancePivot.SetParent(visualRoot.parent, false);
            visualRoot.SetParent(stancePivot, false);
            basePosition = stancePivot.localPosition;
            baseRotation = stancePivot.localRotation;
        }

        public void Configure(PlayerStance playerStance, FirstPersonMotor playerMotor)
        {
            stance = playerStance;
            motor = playerMotor;
            var look = GetComponentInParent<FirstPersonLook>();
            view = look ? look.View : null;
        }

        void LateUpdate()
        {
            if (!stance || !motor || !visualRoot) return;

            float step = Time.deltaTime / transitionSeconds;
            crouchWeight = Mathf.MoveTowards(crouchWeight, stance.IsCrouching ? 1f : 0f, step);
            slideWeight = Mathf.MoveTowards(slideWeight, motor.IsSliding ? 1f : 0f, step);
            float crouchPose = crouchWeight * (1f - slideWeight);
            // Keep the open shoulder ends behind the camera at wide gameplay FOVs.
            // Only the visual pivot moves; meleeOrigin retains its gameplay pose.
            float fovWeight = view ? Mathf.InverseLerp(referenceFov, wideFov, view.fieldOfView) : 0;

            stancePivot.localPosition = basePosition +
                crouchPositionOffset * crouchPose + slidePositionOffset * slideWeight +
                wideFovPositionOffset * fovWeight;
            stancePivot.localRotation = baseRotation *
                Quaternion.Euler(slideRotationOffset * slideWeight);
        }

        void OnDisable()
        {
            if (!stancePivot) return;
            stancePivot.localPosition = basePosition;
            stancePivot.localRotation = baseRotation;
            crouchWeight = slideWeight = 0;
        }
    }
}
