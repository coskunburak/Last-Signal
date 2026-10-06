namespace LastSignal.Vehicles
{
    /// <summary>Read-only physics telemetry. Wheel speed is not simulated engine RPM.</summary>
    public interface IVehiclePresentationState
    {
        float WheelSpeed01 { get; }
        float DriveLoad { get; }
        bool BrakeApplied { get; }
        bool ReverseEngaged { get; }
    }
}
