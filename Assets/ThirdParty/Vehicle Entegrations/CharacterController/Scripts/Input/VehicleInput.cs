using UnityEngine;
using MotionCore.Vehicle.Core;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MotionCore.Vehicle.Input
{
    public sealed class VehicleInput : MonoBehaviour
    {
        private const string HorizontalAxis = "Horizontal";
        private const string VerticalAxis = "Vertical";
        private const string HandbrakeButton = "Jump";

        [Header("Keyboard And Gamepad")]
        [SerializeField] private bool readKeyboard = true;
        [SerializeField] private bool readGamepad = true;

        private VehicleControllerBase vehicle;

        private void Awake()
        {
            vehicle = GetComponent<VehicleControllerBase>();
        }

        private void Update()
        {
            if (vehicle == null)
            {
                return;
            }

            vehicle.SetInput(ReadInput());
        }

        private CarInput ReadInput()
        {
            CarInput input = default;

#if ENABLE_INPUT_SYSTEM
            if (readKeyboard)
            {
                ApplyKeyboardInput(ref input, Keyboard.current);
            }

            if (readGamepad)
            {
                ApplyGamepadInput(ref input, Gamepad.current);
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (readKeyboard || readGamepad)
            {
                float vertical = GetAxisOrDefault(VerticalAxis);
                input.Throttle = Mathf.Max(input.Throttle, Mathf.Clamp01(vertical));
                input.Brake = Mathf.Max(input.Brake, Mathf.Clamp01(-vertical));

                input.Steer = Dominant(input.Steer, GetAxisOrDefault(HorizontalAxis));
                input.Handbrake = input.Handbrake || GetButtonOrDefault(HandbrakeButton);
                input.Nitro = input.Nitro || UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
            }
#endif

            input.Throttle = Mathf.Clamp01(input.Throttle);
            input.Brake = Mathf.Clamp01(input.Brake);
            input.Steer = Mathf.Clamp(input.Steer, -1f, 1f);
            return input;
        }

#if ENABLE_INPUT_SYSTEM
        private static void ApplyKeyboardInput(ref CarInput input, Keyboard keyboard)
        {
            if (keyboard == null)
            {
                return;
            }

            float keyboardSteer = 0f;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                keyboardSteer -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                keyboardSteer += 1f;
            }

            input.Throttle = Mathf.Max(input.Throttle, keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f);
            input.Brake = Mathf.Max(input.Brake, keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            input.Steer = Dominant(input.Steer, keyboardSteer);
            input.Handbrake = input.Handbrake || keyboard.spaceKey.isPressed;
            input.Nitro = input.Nitro || keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        }

        private static void ApplyGamepadInput(ref CarInput input, Gamepad gamepad)
        {
            if (gamepad == null)
            {
                return;
            }

            input.Throttle = Mathf.Max(input.Throttle, gamepad.rightTrigger.ReadValue());
            input.Brake = Mathf.Max(input.Brake, gamepad.leftTrigger.ReadValue());
            input.Steer = Dominant(input.Steer, gamepad.leftStick.ReadValue().x);
            input.Handbrake = input.Handbrake || gamepad.buttonSouth.isPressed;
            input.Nitro = input.Nitro || gamepad.rightShoulder.isPressed;
        }
#endif

        private static float GetAxisOrDefault(string axisName)
        {
            if (string.IsNullOrWhiteSpace(axisName))
            {
                return 0f;
            }

            try
            {
                return UnityEngine.Input.GetAxisRaw(axisName);
            }
            catch (System.ArgumentException)
            {
                return 0f;
            }
        }

        private static bool GetButtonOrDefault(string buttonName)
        {
            if (string.IsNullOrWhiteSpace(buttonName))
            {
                return false;
            }

            try
            {
                return UnityEngine.Input.GetButton(buttonName);
            }
            catch (System.ArgumentException)
            {
                return false;
            }
        }

        private static float Dominant(float current, float candidate)
        {
            return Mathf.Abs(candidate) > Mathf.Abs(current) ? candidate : current;
        }
    }
}
