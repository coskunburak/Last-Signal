using System;

namespace LastSignal.Vehicles
{
    public enum VehicleSeatState { Unoccupied, Entering, Occupied, Exiting }
    /// <summary>Transition authority, independent of parenting and animation events.</summary>
    public sealed class VehicleSeatAuthority
    {
        public VehicleSeatState State { get; private set; }
        public string DriverId { get; private set; }
        public bool Stable => State == VehicleSeatState.Unoccupied || State == VehicleSeatState.Occupied;
        public bool BeginEnter(string driver, bool available)
        {
            if (!available || State != VehicleSeatState.Unoccupied || string.IsNullOrWhiteSpace(driver)) return false;
            DriverId = driver; State = VehicleSeatState.Entering; return true;
        }
        public bool BeginExit(string driver, float speed, float threshold, bool safeExit)
        {
            if (State != VehicleSeatState.Occupied || driver != DriverId || !safeExit || !float.IsFinite(speed) || speed < 0 ||
                !float.IsFinite(threshold) || threshold < 0 || speed > threshold) return false;
            State = VehicleSeatState.Exiting; return true;
        }
        public void Commit()
        {
            if (State == VehicleSeatState.Entering) State = VehicleSeatState.Occupied;
            else if (State == VehicleSeatState.Exiting) { DriverId = null; State = VehicleSeatState.Unoccupied; }
            else throw new InvalidOperationException("No seat transition to commit.");
        }
        public void Rollback()
        {
            if (State == VehicleSeatState.Entering) { DriverId = null; State = VehicleSeatState.Unoccupied; }
            else if (State == VehicleSeatState.Exiting) State = VehicleSeatState.Occupied;
        }
    }
}
