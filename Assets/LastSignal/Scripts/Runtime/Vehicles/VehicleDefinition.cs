using System;
using UnityEngine;
using LastSignal.Inventory.Data;

namespace LastSignal.Vehicles
{
    [CreateAssetMenu(menuName = "Last Signal/Vehicles/Definition")]
    public sealed class VehicleDefinition : ScriptableObject
    {
        public const string UtilityPickupId = "vehicle.utility_pickup.01";
        [SerializeField] string stableId = UtilityPickupId;
        [SerializeField] VehicleTuning tuning = new VehicleTuning();
        [SerializeField] ItemDefinition fuelItem;
        public string StableId => stableId;
        public ItemDefinition FuelItem => fuelItem;
        public VehicleTuning CreateTuning() => tuning?.Copy();
        public bool Valid => !string.IsNullOrWhiteSpace(stableId) && stableId.Length <= 96 && tuning != null && tuning.Valid && fuelItem && fuelItem.Id.IsValid;
    }

    /// <summary>Initial utility-pickup hypotheses; copied into the domain, never hot-mutated by presentation.</summary>
    [Serializable]
    public sealed class VehicleTuning
    {
        public double fuelCapacityLiters = 60, fuelCanLiters = 5;
        public double idleLitersPerSimulationSecond = .0003, loadLitersPerSimulationSecond = .0015;
        public int cargoSlots = 16;
        public float massKg = 1800, maximumSpeedMetersPerSecond = 18, exitSpeedMetersPerSecond = .5f;
        public float impactMinimumMetersPerSecond = 3, impactConditionPerMeterPerSecond = .015f;
        public double engineNoiseIntervalSimulationSeconds = 1, hornIntervalSimulationSeconds = .5;
        public Noise.GameplayNoiseProfile idleNoise = new Noise.GameplayNoiseProfile(18, .2f, 2);
        public Noise.GameplayNoiseProfile loadedNoise = new Noise.GameplayNoiseProfile(40, .5f, 2);
        public Noise.GameplayNoiseProfile hornNoise = new Noise.GameplayNoiseProfile(80, .8f, 2);
        public Noise.GameplayNoiseProfile impactNoise = new Noise.GameplayNoiseProfile(48, .6f, 2);
        public VehicleZombieImpactTuning zombieImpact = new VehicleZombieImpactTuning();
        public VehicleTuning Copy() { var copy = (VehicleTuning)MemberwiseClone(); copy.zombieImpact = zombieImpact?.Copy(); return copy; }
        public bool Valid => zombieImpact != null && zombieImpact.Valid && Positive(fuelCapacityLiters) && fuelCapacityLiters <= 1000 && Positive(fuelCanLiters) && fuelCanLiters <= fuelCapacityLiters &&
            Nonnegative(idleLitersPerSimulationSecond) && idleLitersPerSimulationSecond <= 1 &&
            Nonnegative(loadLitersPerSimulationSecond) && loadLitersPerSimulationSecond <= 1 && cargoSlots >= 1 && cargoSlots <= 256 &&
            Positive(massKg) && massKg <= 10000 && Positive(maximumSpeedMetersPerSecond) && maximumSpeedMetersPerSecond <= 50 &&
            Nonnegative(exitSpeedMetersPerSecond) && exitSpeedMetersPerSecond <= 1 &&
            Positive(impactMinimumMetersPerSecond) && Positive(impactConditionPerMeterPerSecond) && impactConditionPerMeterPerSecond <= 1 &&
            Positive(engineNoiseIntervalSimulationSeconds) && engineNoiseIntervalSimulationSeconds >= .1 &&
            Positive(hornIntervalSimulationSeconds) && hornIntervalSimulationSeconds >= .1 &&
            idleNoise.Valid && loadedNoise.Valid && hornNoise.Valid && impactNoise.Valid;
        public static bool Nonnegative(double n) => !double.IsNaN(n) && !double.IsInfinity(n) && n >= 0;
        public static bool Positive(double n) => Nonnegative(n) && n > 0;
    }
}
