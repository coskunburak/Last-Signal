using System;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Persistence;
using LastSignal.Vehicles;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public sealed class VehicleFoundationTests
    {
        PlayerInventory player;
        ItemDefinition fuel, ammo;
        ItemCatalog catalog;
        VehicleTuning tuning;
        VehicleResources vehicle;
        [SetUp] public void Setup()
        {
            player = new GameObject("VEH-001 inventory owner").AddComponent<PlayerInventory>();
            player.Initialize(4);
            fuel = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/LastSignal/Data/Items/Definitions/fuel.generator.asset");
            ammo = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/LastSignal/Data/Items/Definitions/ammo.rifle.asset");
            catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/LastSignal/Data/Items/Definitions/ItemCatalog.asset");
            Assert.IsNotNull(fuel); Assert.IsNotNull(ammo); Assert.IsNotNull(catalog);
            tuning = new VehicleTuning { cargoSlots = 1 };
            vehicle = new VehicleResources("veh.test.01", tuning, fuel, 10, .8);
        }
        [TearDown] public void Cleanup()
        { Object.DestroyImmediate(player.gameObject); Assert.IsFalse(OwnershipTransaction.Active); }
        [Test] public void VEH_FUEL_001_EmptyCannotStart()
        { var empty = new VehicleResources("empty", tuning, fuel, 0, 1); Assert.IsFalse(empty.TryStartEngine(true, true)); }
        [Test] public void EngineRequiresReadyDriverAndWorkingCondition()
        {
            Assert.IsFalse(vehicle.TryStartEngine(false, true)); Assert.IsFalse(vehicle.TryStartEngine(true, false));
            Assert.IsFalse(new VehicleResources("wreck", tuning, fuel, 10, 0).TryStartEngine(true, true));
        }
        [Test] public void VEH_FUEL_002_RefuelCommitsBeforeObserverAndBlocksReentry()
        {
            player.TryAdd(fuel, 2); int calls = 0; bool coherent = false, reentry = true, capture = true;
            player.InventoryChanged += () =>
            {
                calls++; coherent = player.GetTotalQuantity(fuel) == 1 && vehicle.FuelLiters == 15 && OwnershipTransaction.Active;
                reentry = vehicle.TryRefuel(player);
                capture = VehicleResourceSnapshots.Capture(vehicle, VehicleDefinition.UtilityPickupId, out _).Success;
            };
            Assert.IsTrue(vehicle.TryRefuel(player)); Assert.AreEqual(1, calls); Assert.IsTrue(coherent);
            Assert.IsFalse(reentry); Assert.IsFalse(capture); Assert.AreEqual(15, vehicle.FuelLiters);
        }
        [TestCase(60)] [TestCase(58)]
        public void VEH_FUEL_003_NoOverflowOrLostCan(double initial)
        {
            vehicle = new VehicleResources("full", tuning, fuel, initial, 1); player.TryAdd(fuel, 1);
            Assert.IsFalse(vehicle.TryRefuel(player)); Assert.AreEqual(initial, vehicle.FuelLiters); Assert.AreEqual(1, player.GetTotalQuantity(fuel));
        }
        [Test] public void RefuelRejectsAbsentCanAndRunningEngine()
        {
            Assert.IsFalse(vehicle.TryRefuel(player)); player.TryAdd(fuel, 1); vehicle.TryStartEngine(true, true);
            Assert.IsFalse(vehicle.TryRefuel(player)); Assert.AreEqual(1, player.GetTotalQuantity(fuel));
        }
        [TestCase(20)] [TestCase(120)]
        public void VEH_FUEL_004_ElapsedTimeIndependentOfFrameRate(int fps)
        {
            Assert.IsTrue(vehicle.TryStartEngine(true, true));
            for (int i = 0; i < fps * 60; i++) vehicle.Advance(1.0 / fps, .5f);
            Assert.AreEqual(10 - 60 * (.0003 + .5 * .0015), vehicle.FuelLiters, 1e-9);
        }
        [Test] public void ExhaustionStopsEngineAndCannotCreateNegativeFuel()
        { vehicle.TryStartEngine(true, true); vehicle.Advance(100000, 1); Assert.AreEqual(0, vehicle.FuelLiters); Assert.IsFalse(vehicle.EngineRunning); }
        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(-1)]
        public void InvalidElapsedCannotMutateFuel(double delta)
        { vehicle.TryStartEngine(true, true); Assert.Throws<ArgumentOutOfRangeException>(() => vehicle.Advance(delta, 1)); Assert.AreEqual(10, vehicle.FuelLiters); }
        [Test] public void VEH_CARGO_001_ObserversSeeBothOwnersCommitted()
        {
            player.TryAdd(ammo, 40); bool coherent = false; TransferReason reentry = TransferReason.Complete;
            player.InventoryChanged += () => { coherent = player.GetTotalQuantity(ammo) == 20 && vehicle.CargoQuantity(ammo) == 20; reentry = vehicle.Deposit(player, ammo, 1).Reason; };
            Assert.AreEqual(20, vehicle.Deposit(player, ammo, 20).Moved); Assert.IsTrue(coherent); Assert.AreEqual(TransferReason.Busy, reentry);
        }
        [Test] public void VEH_CARGO_002_FullCargoLeavesSourceUnchanged()
        {
            player.TryAdd(ammo, 70); vehicle.Deposit(player, ammo, 60);
            Assert.AreEqual(TransferReason.DestinationFull, vehicle.Deposit(player, ammo, 10).Reason);
            Assert.AreEqual(10, player.GetTotalQuantity(ammo)); Assert.AreEqual(60, vehicle.CargoQuantity(ammo));
        }
        [Test] public void VEH_CARGO_003_BidirectionalConservation()
        {
            player.TryAdd(ammo, 40);
            for (int i = 0; i < 50; i++)
            { Assert.AreEqual(40, vehicle.Deposit(player, ammo, 40).Moved); Assert.AreEqual(40, vehicle.Withdraw(player, ammo, 40).Moved); }
            Assert.AreEqual(40, player.GetTotalQuantity(ammo)); Assert.AreEqual(0, vehicle.CargoQuantity(ammo));
        }
        [Test] public void ResourceRoundtripPreservesFuelDamageCargoAndRestoresEngineOff()
        {
            player.TryAdd(ammo, 40); vehicle.Deposit(player, ammo, 40); vehicle.ApplyImpact(5); vehicle.TryStartEngine(true, true);
            Assert.IsTrue(VehicleResourceSnapshots.Capture(vehicle, VehicleDefinition.UtilityPickupId, out var snapshot).Success);
            var detached = JsonUtility.FromJson<VehicleResourceSnapshot>(JsonUtility.ToJson(snapshot));
            for (int i = 0; i < 50; i++)
            {
                Assert.IsTrue(VehicleResourceSnapshots.Restore(detached, vehicle.Id, VehicleDefinition.UtilityPickupId, tuning, fuel, catalog, out var restored).Success);
                Assert.AreEqual(vehicle.FuelLiters, restored.FuelLiters); Assert.AreEqual(vehicle.Condition, restored.Condition);
                Assert.AreEqual(40, restored.CargoQuantity(ammo)); Assert.IsFalse(restored.EngineRunning);
            }
            detached.cargo.slots[0].quantity = 1; Assert.AreEqual(40, vehicle.CargoQuantity(ammo));
        }
        [Test] public void UnknownDefinitionRejectsWithoutTouchingSource()
        {
            VehicleResourceSnapshots.Capture(vehicle, "unknown.vehicle", out var snapshot);
            string before = JsonUtility.ToJson(snapshot);
            Assert.AreEqual(SaveError.UnknownDefinition, VehicleResourceSnapshots.Restore(snapshot, vehicle.Id, VehicleDefinition.UtilityPickupId, tuning, fuel, catalog, out var restored).Error);
            Assert.IsNull(restored); Assert.AreEqual(before, JsonUtility.ToJson(snapshot));
        }
        [Test] public void UnknownCargoRejectsWholeResourceRestore()
        {
            player.TryAdd(ammo, 10); vehicle.Deposit(player, ammo, 10);
            VehicleResourceSnapshots.Capture(vehicle, VehicleDefinition.UtilityPickupId, out var snapshot); snapshot.cargo.slots[0].definitionId = "missing.item";
            Assert.AreEqual(SaveError.UnknownDefinition, VehicleResourceSnapshots.Restore(snapshot, vehicle.Id, VehicleDefinition.UtilityPickupId, tuning, fuel, catalog, out var restored).Error);
            Assert.IsNull(restored); Assert.AreEqual(10, vehicle.CargoQuantity(ammo));
        }
        [Test] public void VEH_INT_001_OneSeatOwnerAndRollback()
        {
            var seat = new VehicleSeatAuthority();
            Assert.IsFalse(seat.BeginEnter("driver", false)); Assert.IsTrue(seat.BeginEnter("driver", true));
            Assert.IsFalse(seat.BeginEnter("other", true)); Assert.IsFalse(seat.Stable); seat.Rollback(); Assert.IsNull(seat.DriverId);
            seat.BeginEnter("driver", true); seat.Commit(); Assert.AreEqual(VehicleSeatState.Occupied, seat.State);
        }
        [Test] public void VEH_INT_004_BlockedOrFastExitRetainsDriver()
        {
            var seat = new VehicleSeatAuthority(); seat.BeginEnter("driver", true); seat.Commit();
            Assert.IsFalse(seat.BeginExit("driver", 0, .5f, false)); Assert.IsFalse(seat.BeginExit("driver", 10, .5f, true));
            Assert.IsFalse(seat.BeginExit("other", 0, .5f, true)); Assert.AreEqual("driver", seat.DriverId);
            Assert.IsTrue(seat.BeginExit("driver", 0, .5f, true)); seat.Rollback(); Assert.AreEqual(VehicleSeatState.Occupied, seat.State);
        }
        [Test] public void SeatFiftyTransitionsLeaveNoOwner()
        {
            var seat = new VehicleSeatAuthority();
            for (int i = 0; i < 50; i++)
            { Assert.IsTrue(seat.BeginEnter("driver", true)); seat.Commit(); Assert.IsTrue(seat.BeginExit("driver", 0, .5f, true)); seat.Commit(); }
            Assert.IsNull(seat.DriverId); Assert.AreEqual(VehicleSeatState.Unoccupied, seat.State);
        }
        [Test] public void VEH_INP_001_002_003_ResetBlocksHeldThrottleUntilRelease()
        {
            var gate = new VehicleInputGate(); var held = new VehicleControlIntent(1, 1, 1, false);
            gate.Sample(default, true); Assert.AreEqual(1, gate.Sample(held, true).Throttle);
            Assert.AreEqual(0, gate.Sample(held, false).Throttle); Assert.IsFalse(gate.ActionsAllowed);
            for (int i = 0; i < 20; i++) Assert.AreEqual(0, gate.Sample(held, true).Throttle);
            gate.Sample(default, true, true); Assert.IsFalse(gate.ActionsAllowed);
            gate.Sample(default, true); Assert.AreEqual(1, gate.Sample(held, true).Throttle);
            gate.Reset(); Assert.AreEqual(0, gate.Sample(held, true).ServiceBrake);
        }
        [Test] public void VEH_DMG_001_002_CompoundAndContinuousContactsAreOneImpact()
        {
            var gate = new VehicleImpactGate(2);
            Assert.IsTrue(gate.Begin(1, 0)); Assert.IsFalse(gate.Begin(1, 0));
            gate.End(1); Assert.IsFalse(gate.Begin(1, 20)); gate.End(1); gate.End(1);
            Assert.IsTrue(gate.Begin(1, 21)); gate.End(1); Assert.IsFalse(gate.Begin(1, 21.1)); gate.End(1);
            Assert.IsTrue(gate.Begin(1, 23)); Assert.IsTrue(gate.Begin(2, 23)); Assert.IsFalse(gate.Begin(3, 23));
        }
        [Test] public void InvalidDefinitionAndNonfiniteIntentFailClosed()
        {
            tuning.fuelCapacityLiters = double.NaN; Assert.IsFalse(tuning.Valid);
            Assert.Throws<ArgumentException>(() => new VehicleResources("bad", tuning, fuel, 0, 1));
            var intent = new VehicleControlIntent(float.NaN, float.PositiveInfinity, float.NegativeInfinity, false); Assert.IsTrue(intent.Neutral);
        }
    }
}
