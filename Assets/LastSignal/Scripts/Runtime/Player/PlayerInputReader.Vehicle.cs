using LastSignal.Vehicles;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastSignal
{
    public sealed partial class PlayerInputReader
    {
        InputActionMap vehicleMap;
        InputAction vehicleThrottle, vehicleBrake, vehicleReverse, vehicleSteer, vehicleLook;
        InputAction vehicleHandbrake, vehicleExit, vehicleHorn, vehicleLights, vehicleIgnition, vehiclePause;
        readonly VehicleInputGate vehicleGate = new VehicleInputGate();
        bool drivingContext;
        public bool DrivingActive { get; private set; }
        public VehicleControlIntent VehicleIntent { get; private set; } = VehicleControlIntent.Parked;
        public bool VehicleExitPressed { get; private set; }
        public bool VehicleHornPressed { get; private set; }
        public bool VehicleLightsPressed { get; private set; }
        public bool VehicleIgnitionPressed { get; private set; }
        public bool SetDrivingContext(bool driving, bool active)
        {
            if (!instance) return false;
            vehicleMap = instance.FindActionMap("Vehicle", false);
            if (driving && vehicleMap == null) return false;
            if (vehicleMap != null)
            {
                vehicleThrottle = vehicleMap.FindAction("Throttle", true); vehicleBrake = vehicleMap.FindAction("Brake", true);
                vehicleReverse = vehicleMap.FindAction("Reverse", true); vehicleSteer = vehicleMap.FindAction("Steer", true);
                vehicleLook = vehicleMap.FindAction("Look", true); vehicleHandbrake = vehicleMap.FindAction("Handbrake", true);
                vehicleExit = vehicleMap.FindAction("Exit", true); vehicleHorn = vehicleMap.FindAction("Horn", true);
                vehicleLights = vehicleMap.FindAction("Lights", true); vehicleIgnition = vehicleMap.FindAction("Ignition", true);
                vehiclePause = vehicleMap.FindAction("Pause", true);
            }
            drivingContext = driving; SetGameplay(active); return true;
        }
        void ResetVehicleInput()
        {
            vehicleGate.Reset(); VehicleIntent = VehicleControlIntent.Parked;
            VehicleExitPressed = VehicleHornPressed = VehicleLightsPressed = VehicleIgnitionPressed = false;
        }
        void UpdateVehicleInput()
        {
            Clear(); VehicleExitPressed = VehicleHornPressed = VehicleLightsPressed = VehicleIgnitionPressed = false;
            if (vehiclePause.WasPressedThisFrame()) { PauseRequested?.Invoke(); return; }
            bool ready = vehicleGate.ActionsAllowed;
            VehicleIntent = vehicleGate.Sample(new VehicleControlIntent(vehicleThrottle.ReadValue<float>(), vehicleBrake.ReadValue<float>(),
                vehicleSteer.ReadValue<float>(), vehicleHandbrake.IsPressed(), vehicleReverse.ReadValue<float>()), true,
                vehicleExit.IsPressed() || vehicleHorn.IsPressed() || vehicleLights.IsPressed() || vehicleIgnition.IsPressed());
            if (!ready) return;
            Look = vehicleLook.ReadValue<Vector2>();
            // Match the existing mouse sensitivity/FOV policy; gamepad is angular rate rather than per-frame delta.
            if (vehicleLook.activeControl?.device is Gamepad) Look *= 1000 * Time.deltaTime;
            VehicleExitPressed = vehicleExit.WasPressedThisFrame(); VehicleHornPressed = vehicleHorn.WasPressedThisFrame();
            VehicleLightsPressed = vehicleLights.WasPressedThisFrame(); VehicleIgnitionPressed = vehicleIgnition.WasPressedThisFrame();
        }
    }
}
