using UnityEngine;
using Unity.Profiling;

namespace LastSignal
{
    [DefaultExecutionOrder(-50)]
    public sealed class FirstPersonLook : MonoBehaviour
    {
        static readonly ProfilerMarker VehicleCameraMarker = new ProfilerMarker("LastSignal.Vehicle.Camera");
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
            if (input.DrivingActive) { using (VehicleCameraMarker.Auto()) ApplyLook(input.Look); }
            else if (input.GameplayActive) ApplyLook(input.Look);
        }

        public void ApplyLook(Vector2 mouseDelta)
        {
            float yaw = mouseDelta.x * degreesPerMousePixel * sensitivityMultiplier;
            if (input && input.DrivingActive)
                transform.localRotation = Quaternion.Euler(0, Mathf.Clamp(Mathf.DeltaAngle(0, transform.localEulerAngles.y) + yaw, -110, 110), 0);
            else transform.Rotate(0, yaw, 0, Space.World);
            float pitchLimit = input && input.DrivingActive ? Mathf.Min(pitchLimitDegrees, 60) : pitchLimitDegrees;
            Pitch = Mathf.Clamp(Pitch - mouseDelta.y * degreesPerMousePixel * sensitivityMultiplier, -pitchLimit, pitchLimit);
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
