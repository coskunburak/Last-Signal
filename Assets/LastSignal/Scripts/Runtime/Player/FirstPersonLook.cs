using UnityEngine;

namespace LastSignal
{
    [DefaultExecutionOrder(-50)]
    public sealed class FirstPersonLook : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] Camera view;
        [SerializeField, Range(0, 1)] float degreesPerMousePixel = .12f;
        [SerializeField, Range(60, 100)] float fieldOfViewDegrees = 75;
        [SerializeField, Range(1, 89)] float pitchLimitDegrees = 85;
        public float Pitch { get; private set; }
        public Camera View => view;
        public float BaseFOV => fieldOfViewDegrees;

        float fovOverride = -1; // Negative means no override.
        float sensitivityMultiplier = 1;

        public void Configure(PlayerInputReader reader, Camera camera) { input = reader; view = camera; }

        void Awake()
        {
            if (!input || !view) { Debug.LogError("Look requires input and camera.", this); enabled = false; }
        }

        void Update()
        {
            float targetFov = fovOverride > 0 ? fovOverride : fieldOfViewDegrees;
            view.fieldOfView = targetFov;
            if (input.GameplayActive) ApplyLook(input.Look);
        }

        public void ApplyLook(Vector2 mouseDelta)
        {
            transform.Rotate(0, mouseDelta.x * degreesPerMousePixel * sensitivityMultiplier, 0, Space.World);
            Pitch = Mathf.Clamp(Pitch - mouseDelta.y * degreesPerMousePixel * sensitivityMultiplier, -pitchLimitDegrees, pitchLimitDegrees);
            view.transform.localRotation = Quaternion.Euler(Pitch, 0, 0);
        }

        internal void RestorePitch(float value)
        { Pitch = value; view.transform.localRotation = Quaternion.Euler(Pitch, 0, 0); }

        /// <summary>Apply camera recoil (pitch up, yaw). Separate from mouse look.</summary>
        public void ApplyRecoil(float pitchDegrees, float yawDegrees)
        {
            transform.Rotate(0, yawDegrees, 0, Space.World);
            Pitch = Mathf.Clamp(Pitch + pitchDegrees, -pitchLimitDegrees, pitchLimitDegrees);
            view.transform.localRotation = Quaternion.Euler(Pitch, 0, 0);
        }

        /// <summary>Set FOV override for ADS. Pass -1 to clear.</summary>
        public void SetFOVOverride(float fov) => fovOverride = fov;
        public void SetSensitivityMultiplier(float value) => sensitivityMultiplier = Mathf.Clamp01(value);
    }
}
