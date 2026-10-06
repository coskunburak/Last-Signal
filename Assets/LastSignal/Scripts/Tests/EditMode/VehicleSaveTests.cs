using System.Collections.Generic;
using LastSignal.Persistence;
using LastSignal.Vehicles;
using NUnit.Framework;

namespace LastSignal.Tests
{
    public sealed class VehicleSaveTests
    {
        SaveCodec Codec() => new SaveCodec(new SaveValidation("world.fixture", "content.1",
            new Dictionary<string, int> { ["ammo.rifle"] = 60, ["medical.bandage"] = 10 }, "rifle.1", 30));
        SaveGame Fixture()
        {
            var save = SaveFoundationTests.Fixture(); save.header.vehicleVersion = 1;
            var slots = new SlotSnapshot[16]; for (int i = 0; i < slots.Length; i++) slots[i] = new SlotSnapshot();
            slots[3] = new SlotSnapshot { definitionId = "ammo.rifle", quantity = 17 };
            save.vehicles = new VehicleWorldSnapshot { version = 1, occupiedVehicleId = VehicleDefinition.UtilityPickupId,
                vehicles = new[] { new VehicleSnapshot { ownerCell = "resident", lights = true, pose = new TransformSnapshot { x = -100, qw = 1 },
                    resources = new VehicleResourceSnapshot { version = 1, vehicleId = VehicleDefinition.UtilityPickupId,
                        definitionId = VehicleDefinition.UtilityPickupId, fuelLiters = 23.456, condition = .72,
                        cargo = new ContainerSnapshot { id = VehicleDefinition.UtilityPickupId + ":cargo", capacity = 16, slots = slots } } } } };
            return save;
        }
        [Test] public void AdditiveExtensionRoundtripsWithoutChangingBaseSchema()
        {
            var codec = Codec(); Assert.IsTrue(codec.Encode(Fixture(), out var json).Success);
            var result = codec.Decode(json, out var save); Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(1, save.header.schemaVersion); Assert.AreEqual(1, save.header.vehicleVersion);
            Assert.AreEqual(23.456, save.vehicles.vehicles[0].resources.fuelLiters, 1e-9);
            Assert.AreEqual(17, save.vehicles.vehicles[0].resources.cargo.slots[3].quantity);
            Assert.IsTrue(save.vehicles.vehicles[0].lights); StringAssert.DoesNotContain("rpm", json);
        }
        [Test] public void LegacyAbsentExtensionRemainsAbsent()
        {
            var codec = Codec(); Assert.IsTrue(codec.Encode(SaveFoundationTests.Fixture(), out var json).Success);
            var result = codec.Decode(json, out var save); Assert.IsTrue(result.Success, result.Message);
            Assert.IsNull(save.vehicles); Assert.AreEqual(0, save.header.vehicleVersion);
        }
        [TestCase(0)] [TestCase(2)] public void UnknownOrUnmarkedExtensionRejected(int marker)
        { var save = Fixture(); save.header.vehicleVersion = marker; Assert.IsFalse(Codec().Encode(save, out _).Success); }
        [Test] public void MissingRequiredVehicleSectionRejected()
        { var save = Fixture(); save.vehicles = null; Assert.IsFalse(Codec().Encode(save, out _).Success); }
        [TestCase(double.NaN)] [TestCase(-1)] [TestCase(61)] public void InvalidFuelRejected(double fuel)
        { var save = Fixture(); save.vehicles.vehicles[0].resources.fuelLiters = fuel; Assert.IsFalse(Codec().Encode(save, out _).Success); }
        [Test] public void UnknownCargoAndWrongSeatOwnerRejected()
        {
            var save = Fixture(); save.vehicles.vehicles[0].resources.cargo.slots[3].definitionId = "missing";
            Assert.IsFalse(Codec().Encode(save, out _).Success);
            save = Fixture(); save.vehicles.occupiedVehicleId = "missing"; Assert.IsFalse(Codec().Encode(save, out _).Success);
        }
    }
}
