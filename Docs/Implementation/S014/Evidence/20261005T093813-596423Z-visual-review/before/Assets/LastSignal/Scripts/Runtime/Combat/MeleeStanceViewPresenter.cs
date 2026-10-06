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

        PlayerStance stance;
        FirstPersonMotor motor;
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

            basePosition = visualRoot.localPosition;
            baseRotation = visualRoot.localRotation;
        }

        public void Configure(PlayerStance playerStance, FirstPersonMotor playerMotor)
        {
            stance = playerStance;
            motor = playerMotor;
        }

        void LateUpdate()
        {
            if (!stance || !motor || !visualRoot) return;

            float step = Time.deltaTime / transitionSeconds;
            crouchWeight = Mathf.MoveTowards(crouchWeight, stance.IsCrouching ? 1f : 0f, step);
            slideWeight = Mathf.MoveTowards(slideWeight, motor.IsSliding ? 1f : 0f, step);
            float crouchPose = crouchWeight * (1f - slideWeight);

            visualRoot.localPosition = basePosition +
                crouchPositionOffset * crouchPose + slidePositionOffset * slideWeight;
            visualRoot.localRotation = baseRotation *
                Quaternion.Euler(slideRotationOffset * slideWeight);
        }

        void OnDisable()
        {
            if (!visualRoot) return;
            visualRoot.localPosition = basePosition;
            visualRoot.localRotation = baseRotation;
            crouchWeight = slideWeight = 0;
        }
    }
}
