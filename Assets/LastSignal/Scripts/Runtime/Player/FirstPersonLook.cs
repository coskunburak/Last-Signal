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
        public float MouseSensitivity => degreesPerMousePixel;
        Vector2 gamepadDegreesPerSecond = new Vector2(Audio.AudioPreferences.DefaultGamepadYaw, Audio.AudioPreferences.DefaultGamepadPitch);
        bool gamepadInvertX, gamepadInvertY;

        public void ApplyPreferences(Audio.AudioPreferences preferences)
        {
            SetBaseFOV(preferences.FovDegrees);
            SetMouseSensitivity(preferences.MouseSensitivity);
            gamepadDegreesPerSecond = new Vector2(preferences.GamepadYaw, preferences.GamepadPitch);
            gamepadInvertX = preferences.GamepadInvertX;
            gamepadInvertY = preferences.GamepadInvertY;
            if (input) input.SetGamepadLookDeadzone(preferences.GamepadDeadzone);
        }

        public Vector2 CalculateLookDegrees(Vector2 value, bool gamepad, float seconds)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y)) return Vector2.zero;
            if (!gamepad) return value * (degreesPerMousePixel * sensitivityMultiplier);
            if (!float.IsFinite(seconds) || seconds <= 0) return Vector2.zero;
            // Linear radial response after Input System deadzone; stick is angular rate.
            value = Vector2.ClampMagnitude(value, 1);
            return Vector2.Scale(Vector2.Scale(value, gamepadDegreesPerSecond),
                new Vector2(gamepadInvertX ? -1 : 1, gamepadInvertY ? -1 : 1)) * (seconds * sensitivityMultiplier);
        }

        Vehicles.VehicleActor feelVehicle;
        Vector3 appliedFeelPosition;
        Quaternion appliedFeelRotation = Quaternion.identity;
        bool feelApplied;
        void RemoveVehicleFeel()
        {
            if (!feelApplied || !view) return;
            view.transform.localPosition -= appliedFeelPosition;
            view.transform.localRotation *= Quaternion.Inverse(appliedFeelRotation);
            feelApplied = false;
        }
        void LateUpdate()
        {
            if (!input || !input.DrivingActive || !view) return;
            if (!feelVehicle) feelVehicle = GetComponentInParent<Vehicles.VehicleActor>();
            if (!feelVehicle || !feelVehicle.Ready || !feelVehicle.Occupied) return;
            appliedFeelPosition = view.transform.parent.InverseTransformVector(
                feelVehicle.transform.TransformVector(feelVehicle.BodyFeelPosition));
            appliedFeelRotation = Quaternion.Euler(feelVehicle.BodyFeelAngles);
            view.transform.localPosition += appliedFeelPosition;
            view.transform.localRotation *= appliedFeelRotation;
            feelApplied = true;
        }
        internal void ResetVehicleBodyFeel() { RemoveVehicleFeel(); feelVehicle = null; }
        void OnDisable() => ResetVehicleBodyFeel();

        float fovOverride = -1; // Negative means no override.
        float sensitivityMultiplier = 1;

        public void Configure(PlayerInputReader reader, Camera camera) { input = reader; view = camera; }
        public void SetBaseFOV(float value) { if (float.IsFinite(value)) fieldOfViewDegrees = Mathf.Clamp(value, 60, 100); }
        public void SetMouseSensitivity(float value) { if (float.IsFinite(value)) degreesPerMousePixel = Mathf.Clamp(value, .02f, .5f); }

        void Awake()
        {
            if (!input || !view) { Debug.LogError("Look requires input and camera.", this); enabled = false; }
        }

        void Update()
        {
            RemoveVehicleFeel();
            if (!input.DrivingActive) feelVehicle = null;
            float targetFov = fovOverride > 0 ? fovOverride : fieldOfViewDegrees;
            view.fieldOfView = targetFov;
            if (input.DrivingActive) { using (VehicleCameraMarker.Auto()) ApplyInputLook(); }
            else if (input.GameplayActive) ApplyInputLook();
        }

        void ApplyInputLook() => ApplyAngles(CalculateLookDegrees(input.Look, input.LookUsesGamepad, Time.deltaTime));

        // Existing callers supply mouse pixels, including movement regression fixtures.
        public void ApplyLook(Vector2 mouseDelta) => ApplyAngles(CalculateLookDegrees(mouseDelta, false, 0));

        void ApplyAngles(Vector2 degrees)
        {
            float yaw = degrees.x;
            if (input && input.DrivingActive)
                transform.localRotation = Quaternion.Euler(0, Mathf.Clamp(Mathf.DeltaAngle(0, transform.localEulerAngles.y) + yaw, -110, 110), 0);
            else transform.Rotate(0, yaw, 0, Space.World);
            float pitchLimit = input && input.DrivingActive ? Mathf.Min(pitchLimitDegrees, 60) : pitchLimitDegrees;
            Pitch = Mathf.Clamp(Pitch - degrees.y, -pitchLimit, pitchLimit);
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
