using NUnit.Framework;
using UnityEngine;
using LastSignal.AI;
using LastSignal.WorldTime;
using LastSignal.WorldCells;

namespace LastSignal.Tests
{
    public class WorldPopulationManagerTests
    {
        GameObject go;
        WorldPopulationManager mgr;
        WorldClock clock;
        WorldCellManager cells;

        [SetUp]
        public void Setup()
        {
            go = new GameObject();
            cells = go.AddComponent<WorldCellManager>();
            clock = go.AddComponent<WorldClock>();
            mgr = go.AddComponent<WorldPopulationManager>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(go);
        }

        [Test]
        public void PressureBounds_AreRespected()
        {
            mgr.ReportNoise("r1", "cell:0:0", 1000f); // High intensity
            Assert.That(mgr.GetPressure("cell:0:0").Pressure, Is.LessThanOrEqualTo(mgr.MaxPressure));
            Assert.That(mgr.GetPressure("cell:0:0").Pressure, Is.GreaterThan(0));
        }

        [Test]
        public void DuplicateNoiseId_DoesNotDoubleApply()
        {
            mgr.ReportNoise("r1", "cell:0:0", 1f);
            float p1 = mgr.GetPressure("cell:0:0").Pressure;
            mgr.ReportNoise("r1", "cell:0:0", 1f);
            float p2 = mgr.GetPressure("cell:0:0").Pressure;
            Assert.That(p1, Is.EqualTo(p2));
        }

        [Test]
        public void PopulationConservation_Maintained()
        {
            var l = mgr.GetLedger("cell:0:0");
            l.Logical = 15;
            int total = mgr.TotalConservation("cell:0:0");
            Assert.That(total, Is.EqualTo(15));
            
            // Apply high pressure to force migration
            mgr.ReportNoise("r1", "cell:1:0", 5f);
            
            // Check that conservation is maintained for all cells involved
            // It might pull from cell:2:0, so we just check total in target cell after arrival
            total = mgr.TotalConservation("cell:1:0");
            Assert.That(total, Is.EqualTo(15)); // Target cell also started with 15 logically
            
            bool migrationHappened = mgr.GetLedger("cell:2:0").Logical < 15 || mgr.GetLedger("cell:0:0").Logical < 15;
            Assert.That(migrationHappened, Is.True, "Migration did not happen");
        }
    }
}
