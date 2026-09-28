using NUnit.Framework;
using LastSignal.Persistence;
using LastSignal.WorldTime;
using LastSignal.WorldCells;
using UnityEngine;

namespace LastSignal.Tests
{
    public class SaveValidationTests
    {
        SaveCodec Codec() => new SaveCodec(new SaveValidation("world.fixture","content.1",new System.Collections.Generic.Dictionary<string,int>{{"ammo.rifle",60},{"medical.bandage",10}},"rifle.1",30));

        static SaveGame CellFixture(double lastProcessed)
        {
            var save = SaveFoundationTests.Fixture();
            save.header.schemaVersion = 3;
            save.worldTime = new WorldSimulation(new WorldTimeSettings { startingSeconds = 100 }).Capture();
            save.cells = new CellWorldSnapshot {
                playerCell = "resident",
                cells = new[] { new CellSnapshot { id = "cell:1:0", lastProcessed = lastProcessed } }
            };
            return save;
        }

        [Test]
        public void CellLastProcessed_ExactEquality_Valid()
        {
            var save = CellFixture(100.0);
            var result = Codec().Encode(save, out _);
            Assert.IsTrue(result.Success, result.Message);
        }

        [Test]
        public void CellLastProcessed_TinyRepresentationalDrift_ValidWithinTolerance()
        {
            var save = CellFixture(100.005);
            var result = Codec().Encode(save, out _);
            Assert.IsTrue(result.Success, result.Message);
        }

        [Test]
        public void CellLastProcessed_MaterialFuture_Invalid()
        {
            var save = CellFixture(101.0);
            var result = Codec().Encode(save, out _);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Invalid cell identity/time.", result.Message);
        }

        [Test]
        public void CellLastProcessed_NaN_Invalid()
        {
            var save = CellFixture(double.NaN);
            var result = Codec().Encode(save, out _);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Invalid cell identity/time.", result.Message);
        }

        [Test]
        public void CellLastProcessed_Infinity_Invalid()
        {
            var save = CellFixture(double.PositiveInfinity);
            var result = Codec().Encode(save, out _);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Invalid cell identity/time.", result.Message);
        }
        
        [Test]
        public void CellLastProcessed_Negative_Invalid()
        {
            var save = CellFixture(-5.0);
            var result = Codec().Encode(save, out _);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Invalid cell identity/time.", result.Message);
        }
    }
}
