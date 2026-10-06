using System;
using LastSignal.Inventory.Data;
using LastSignal.Persistence;

namespace LastSignal.Vehicles
{
    /// <summary>Detached resource snapshot nested by the versioned vehicle world extension.
    /// Vendor physics and presentation transients are never serialized.</summary>
    [Serializable]
    public sealed class VehicleResourceSnapshot
    {
        public int version;
        public string vehicleId, definitionId;
        public double fuelLiters, condition;
        public ContainerSnapshot cargo;
    }
    public static class VehicleResourceSnapshots
    {
        public static SaveResult Capture(VehicleResources source, string definitionId, out VehicleResourceSnapshot snapshot)
        {
            snapshot = null;
            if (source == null || string.IsNullOrWhiteSpace(definitionId) || definitionId.Length > 96)
                return new SaveResult(SaveError.InvalidData, "Missing vehicle/definition.");
            var result = InventorySnapshots.Capture(source.Cargo, source.Id + ":cargo", out var cargo);
            if (!result.Success) return result;
            snapshot = new VehicleResourceSnapshot { version = 1, vehicleId = source.Id, definitionId = definitionId,
                fuelLiters = source.FuelLiters, condition = source.Condition, cargo = cargo };
            return SaveResult.Ok;
        }
        public static SaveResult Restore(VehicleResourceSnapshot source, string expectedVehicleId, string expectedDefinitionId,
            VehicleTuning tuning, ItemDefinition fuel, ItemCatalog catalog, out VehicleResources restored)
        {
            restored = null;
            if (OwnershipTransaction.Active) return new SaveResult(SaveError.Busy, "Ownership transaction in progress.");
            if (source == null || source.version != 1 || string.IsNullOrWhiteSpace(expectedVehicleId) ||
                source.vehicleId != expectedVehicleId || source.vehicleId.Length > 96 || tuning == null || !tuning.Valid ||
                !fuel || !fuel.Id.IsValid || !VehicleTuning.Nonnegative(source.fuelLiters) || source.fuelLiters > tuning.fuelCapacityLiters ||
                !VehicleTuning.Nonnegative(source.condition) || source.condition > 1 || source.cargo == null || source.cargo.capacity != tuning.cargoSlots)
                return new SaveResult(SaveError.InvalidData, "Invalid vehicle resource snapshot.");
            if (string.IsNullOrWhiteSpace(expectedDefinitionId) || source.definitionId != expectedDefinitionId)
                return new SaveResult(SaveError.UnknownDefinition, "Unknown vehicle definition.");
            var candidate = new VehicleResources(expectedVehicleId, tuning, fuel, source.fuelLiters, source.condition);
            var result = InventorySnapshots.Restore(candidate.Cargo, source.cargo, expectedVehicleId + ":cargo", catalog);
            if (!result.Success) return result;
            // Engine always OFF. No event replay and no caller-visible mutation until all cargo validates.
            restored = candidate; return SaveResult.Ok;
        }
    }
}
