using System;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Persistence;

namespace LastSignal.Vehicles
{
    /// <summary>Semantic authority only. No vendor, scene, input, or audio ownership.</summary>
    public sealed class VehicleResources
    {
        readonly VehicleTuning tuning;
        readonly ItemDefinition fuelItem;
        public string Id { get; }
        public double FuelLiters { get; private set; }
        public double Condition { get; private set; }
        public bool EngineRunning { get; private set; }
        internal InventoryContainer Cargo { get; }
        public long CargoRevision => Cargo.Revision;
        public int CargoCapacity => Cargo.Capacity;
        public event Action CargoChanged { add => Cargo.InventoryChanged += value; remove => Cargo.InventoryChanged -= value; }
        public InventorySlot GetCargoSlot(int index) => Cargo.GetSlot(index);
        public int CargoQuantity(ItemDefinition item) => Cargo.GetTotalQuantity(item);
        public bool Busy => OwnershipTransaction.Active || Cargo.IsBusy;
        public VehicleResources(string id, VehicleTuning configuration, ItemDefinition fuel, double initialFuel, double condition)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 96 || configuration == null || !configuration.Valid ||
                !fuel || !fuel.Id.IsValid || !VehicleTuning.Nonnegative(initialFuel) || initialFuel > configuration.fuelCapacityLiters ||
                !VehicleTuning.Nonnegative(condition) || condition > 1) throw new ArgumentException("Invalid vehicle resources.");
            Id = id; tuning = configuration.Copy(); fuelItem = fuel; FuelLiters = initialFuel; Condition = condition;
            Cargo = new InventoryContainer(tuning.cargoSlots);
        }
        public bool TryStartEngine(bool occupied, bool sessionReady)
        {
            if (Busy || !occupied || !sessionReady || FuelLiters <= 0 || Condition <= 0) return false;
            EngineRunning = true; return true;
        }
        public void StopEngine() => EngineRunning = false;
        public void Advance(double simulationSeconds, float load)
        {
            if (!VehicleTuning.Nonnegative(simulationSeconds) || !float.IsFinite(load) || load < 0 || load > 1)
                throw new ArgumentOutOfRangeException(nameof(simulationSeconds));
            if (Busy || !EngineRunning) return;
            FuelLiters = Math.Max(0, FuelLiters - simulationSeconds * (tuning.idleLitersPerSimulationSecond + load * tuning.loadLitersPerSimulationSecond));
            if (FuelLiters == 0) EngineRunning = false;
        }
        public bool TryRefuel(PlayerInventory source)
        {
            if (!source || Busy || EngineRunning || FuelLiters + tuning.fuelCanLiters > tuning.fuelCapacityLiters) return false;
            double next = FuelLiters + tuning.fuelCanLiters;
            // Exchange commits the item and fuel before observers, under the existing save barrier.
            return source.Container.Exchange(fuelItem, 1, null, 0, () => FuelLiters = next);
        }
        public TransferResult Deposit(PlayerInventory source, ItemDefinition item, int count)
            => source && !Busy ? source.Container.TransferTo(Cargo, item, count) : Rejected(count);
        public TransferResult Withdraw(PlayerInventory target, ItemDefinition item, int count)
            => target && !Busy ? Cargo.TransferTo(target.Container, item, count) : Rejected(count);
        static TransferResult Rejected(int count) => new TransferResult(count, 0, TransferReason.Busy);
        public bool ApplyInfectedImpact(float severity)
        {
            if (Busy || !float.IsFinite(severity) || severity <= 0 || severity > 1) return false;
            Condition = Math.Max(0, Condition - tuning.zombieImpact.Cost(severity));
            if (Condition == 0) StopEngine();
            return true;
        }
        public bool ApplyImpact(float relativeSpeedMetersPerSecond)
        {
            if (Busy || !float.IsFinite(relativeSpeedMetersPerSecond) || relativeSpeedMetersPerSecond <= tuning.impactMinimumMetersPerSecond) return false;
            Condition = Math.Max(0, Condition - (relativeSpeedMetersPerSecond - tuning.impactMinimumMetersPerSecond) * tuning.impactConditionPerMeterPerSecond);
            if (Condition == 0) StopEngine();
            return true;
        }
    }
}
