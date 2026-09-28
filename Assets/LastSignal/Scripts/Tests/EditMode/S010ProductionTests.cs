using System;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Shelter;
using LastSignal.WorldTime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public class S010ProductionTests
    {
        ShelterRecipe recipe;
        ItemDefinition scrap, ammo, fuel;
        InventoryContainer source;
        ShelterProduction production;
        [SetUp] public void Setup()
        {
            scrap = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Game/Items/Definitions/material.scrap.asset");
            ammo = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Game/Items/Definitions/ammo.rifle.asset");
            fuel = scrap; // Deterministic fixture; shipped fuel is separately authored.
            recipe = ScriptableObject.CreateInstance<ShelterRecipe>(); recipe.recipeId = "test.ammo"; recipe.input = scrap; recipe.output = ammo;
            source = new InventoryContainer(4); source.TryAdd(scrap, 20);
            production = New(); Assert.IsTrue(production.Claim()); Assert.IsTrue(production.Install(ShelterModule.Workbench, source));
        }
        ShelterProduction New() => new ShelterProduction("test.cabin", recipe, scrap, fuel, 2, 6, 1800, 0);
        [TearDown] public void Cleanup() => UnityEngine.Object.DestroyImmediate(recipe);
        [Test] public void InvalidClaimAndModuleOperationsConsumeNothing()
        {
            int before = source.GetTotalQuantity(scrap);
            Assert.IsFalse(production.Claim()); Assert.IsFalse(production.Install(ShelterModule.Workbench, source));
            Assert.IsFalse(production.Install((ShelterModule)999, source));
            var unclaimed = New(); Assert.IsFalse(unclaimed.Install(ShelterModule.Bed, source));
            Assert.AreEqual(before, source.GetTotalQuantity(scrap));
        }
        [Test] public void StartReservesExactlyOnceAndRejectsDoubleSpending()
        {
            int before = source.GetTotalQuantity(scrap);
            Assert.IsTrue(production.Start(source)); Assert.IsFalse(production.Start(source));
            Assert.AreEqual(before - 2, source.GetTotalQuantity(scrap)); Assert.AreEqual(2, production.Capture().job.inputQuantity);
        }
        [Test] public void MissingIngredientsAndToolRejectWithoutMutation()
        {
            source.Clear(); source.TryAdd(scrap, 1); Assert.IsFalse(production.Start(source)); Assert.AreEqual(1, source.GetTotalQuantity(scrap));
            source.TryAdd(scrap, 5); recipe.tool = ammo; Assert.IsFalse(production.Start(source)); Assert.AreEqual(6, source.GetTotalQuantity(scrap));
        }
        [TestCase(0)] [TestCase(100)] [TestCase(599)]
        public void CancellationRefundsExactlyOnce(double progress)
        {
            production.Refuel(source); production.ToggleGenerator(); int before = source.GetTotalQuantity(scrap);
            Assert.IsTrue(production.Start(source)); production.ApplyElapsed(0, progress);
            Assert.IsTrue(production.Cancel()); Assert.IsFalse(production.Cancel()); Assert.IsTrue(production.Collect(source)); Assert.IsFalse(production.Collect(source));
            Assert.AreEqual(before, source.GetTotalQuantity(scrap)); Assert.AreEqual(0, source.GetTotalQuantity(ammo));
        }
        [Test] public void CompletedResultCannotBeCancelledAndFullOutputIsRetained()
        {
            production.Refuel(source); production.ToggleGenerator(); Assert.IsTrue(production.Start(source)); production.ApplyElapsed(0, 600);
            var full = new InventoryContainer(1); full.TryAdd(scrap, 20);
            Assert.IsFalse(production.Cancel());
            for (int i = 0; i < 5; i++) Assert.IsFalse(production.Collect(full));
            Assert.AreEqual(CraftStatus.CompletedWaitingOutput, production.Status);
            full.TryRemove(scrap, 20); Assert.IsTrue(production.Collect(full)); Assert.IsFalse(production.Collect(full)); Assert.AreEqual(10, full.GetTotalQuantity(ammo));
        }
        [Test] public void FullRefundRemainsEscrowAcrossLoad()
        {
            production.Start(source); production.Cancel(); var full = new InventoryContainer(1); full.TryAdd(ammo, 60);
            Assert.IsFalse(production.Collect(full)); var restored = New(); restored.Restore(production.Capture(), 0);
            Assert.IsFalse(restored.Collect(full)); full.Clear(); Assert.IsTrue(restored.Collect(full)); Assert.AreEqual(2, full.GetTotalQuantity(scrap)); Assert.IsFalse(restored.Collect(full));
        }
        [Test] public void ZeroFuelPauseAndFuelBoundaryNeverProduceFreeWork()
        {
            production.Start(source); production.ToggleGenerator(); production.ApplyElapsed(0, 100); Assert.AreEqual(0, production.Progress);
            production.Refuel(source); var clock = new WorldSimulation(new WorldTimeSettings { startingSeconds = 100 }); clock.Register(production);
            clock.TickRealSeconds(20, true, 0); Assert.AreEqual(0, production.Progress); Assert.AreEqual(1800, production.FuelSeconds);
            clock.AdvanceUntil(10000, 0); Assert.AreEqual(0, production.FuelSeconds); Assert.AreEqual(CraftStatus.CompletedWaitingOutput, production.Status);
            production.Collect(source); production.Start(source); clock.AdvanceUntil(20000, 0); Assert.AreEqual(0, production.Progress);
        }
        [TestCase(300)] [TestCase(600)]
        public void SaveLoadPreservesRunningAndPendingExactlyOnce(double progress)
        {
            production.Refuel(source); production.ToggleGenerator(); production.Start(source); production.ApplyElapsed(0, progress);
            var snap = JsonUtility.FromJson<ShelterProductionSnapshot>(JsonUtility.ToJson(production.Capture()));
            var restored = New(); restored.Restore(snap, progress);
            for (int i = 0; i < 10; i++) restored.Restore(restored.Capture(), progress);
            restored.ApplyElapsed(progress, 600); Assert.IsTrue(restored.Collect(source)); Assert.IsFalse(restored.Collect(source)); Assert.AreEqual(10, source.GetTotalQuantity(ammo));
        }
        [Test] public void SnapshotIsDetachedAndMalformedEscrowFailsClosed()
        {
            production.Start(source); var snap = production.Capture(); snap.job.inputQuantity++;
            Assert.IsFalse(production.CanRestore(snap, 0)); Assert.AreEqual(2, production.Capture().job.inputQuantity);
            snap = production.Capture(); snap.job.id = "duplicate"; Assert.IsFalse(production.CanRestore(snap, 0));
            snap = production.Capture(); snap.fuelSeconds = double.NaN; Assert.IsFalse(production.CanRestore(snap, 0));
            snap = production.Capture(); snap.job.status = CraftStatus.CompletedWaitingOutput; Assert.IsFalse(production.CanRestore(snap, 0));
            snap = production.Capture(); snap.lastProcessed = 1; Assert.IsFalse(production.CanRestore(snap, 0));
        }
        [Test] public void UpgradePaysOnceAndProvidesDoubleSpeedAfterLoad()
        {
            int before = source.GetTotalQuantity(scrap); Assert.IsTrue(production.Upgrade(source)); Assert.IsFalse(production.Upgrade(source));
            Assert.AreEqual(before - 6, source.GetTotalQuantity(scrap));
            var restored = New(); restored.Restore(production.Capture(), 0); restored.Refuel(source); restored.ToggleGenerator(); restored.Start(source);
            Assert.AreEqual(300, restored.Capture().job.duration); restored.ApplyElapsed(0, 300); Assert.AreEqual(CraftStatus.CompletedWaitingOutput, restored.Status);
        }
        [Test] public void ReentrantObserversCannotCraftCollectOrSaveDuringCommit()
        {
            int calls = 0;
            source.InventoryChanged += () => { calls++; Assert.IsFalse(production.Start(source)); Assert.IsFalse(production.Cancel()); Assert.Throws<InvalidOperationException>(() => production.Capture()); };
            Assert.IsTrue(production.Start(source)); Assert.AreEqual(1, calls);
        }
        [Test] public void MaterialConservationThroughCancelCompleteUpgradeAndRepeatedLoad()
        {
            // 20 scrap: 2 bench + 1 fuel + 2 completed craft + 6 upgrade + 9 stored.
            production.Refuel(source); production.ToggleGenerator(); production.Start(source); production.ApplyElapsed(0, 200);
            production.Cancel(); production.Collect(source); production.Start(source); production.ApplyElapsed(200, 800);
            var restored = New(); restored.Restore(production.Capture(), 800); restored.Collect(source); restored.Upgrade(source);
            Assert.AreEqual(20, source.GetTotalQuantity(scrap) + 2 + 1 + 2 + 6); Assert.AreEqual(10, source.GetTotalQuantity(ammo));
        }
        [Test] public void InvalidRecipesAndConversionCyclesRejected()
        {
            Assert.IsTrue(ShelterRecipe.ValidateCatalog(new[] { recipe })); recipe.inputQuantity = 0; Assert.IsFalse(recipe.Valid); recipe.inputQuantity = 2;
            recipe.durationWorldSeconds = double.NaN; Assert.IsFalse(recipe.Valid); recipe.durationWorldSeconds = 600;
            var reverse = ScriptableObject.CreateInstance<ShelterRecipe>();
            try { reverse.recipeId = "reverse"; reverse.input = ammo; reverse.output = scrap; Assert.IsFalse(ShelterRecipe.ValidateCatalog(new[] { recipe, reverse })); }
            finally { UnityEngine.Object.DestroyImmediate(reverse); }
            Assert.IsFalse(ShelterRecipe.ValidateCatalog(new[] { recipe, recipe })); recipe.output = null; Assert.IsFalse(recipe.Valid);
        }
        [Test] public void CodecHandlesLegacyMissingExtensionAndIdleJobWithoutAcceptingCorruption()
        {
            var validator = new LastSignal.Persistence.SaveValidation("world.fixture", "content.1",
                new System.Collections.Generic.Dictionary<string,int> { ["ammo.rifle"] = 60, ["medical.bandage"] = 10, ["material.scrap"] = 20 }, "rifle.1", 30);
            var codec = new LastSignal.Persistence.SaveCodec(validator);
            var save = SaveFoundationTests.Fixture();
            Assert.IsTrue(codec.Encode(save, out var json).Success); var result = codec.Decode(json, out var legacy);
            Assert.IsTrue(result.Success, result.Message); Assert.IsNull(legacy.shelter.production);
            save.header.schemaVersion = 2; save.worldTime = new WorldSimulation(new WorldTimeSettings { startingSeconds = 0 }).Capture();
            save.shelter.production = production.Capture();
            Assert.IsTrue(codec.Encode(save, out json).Success); result = codec.Decode(json, out var idle);
            Assert.IsTrue(result.Success, result.Message); Assert.IsNull(idle.shelter.production.job); Assert.IsTrue(idle.shelter.production.workbench);
            save.shelter.production.version = 0; Assert.IsFalse(codec.Encode(save, out _).Success);
        }
        [Test] public void WarmedClockParticipantHasNoManagedAllocations()
        {
            production.Refuel(source); production.ToggleGenerator(); production.Start(source);
            double t = 0;
            for (int i = 0; i < 100; i++) { production.NextBoundary(t); production.ApplyElapsed(t, t + .01); t += .01; }
            var watch = System.Diagnostics.Stopwatch.StartNew();
            long watchBytes = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) { production.NextBoundary(t); production.ApplyElapsed(t, t + .01); t += .01; }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - watchBytes;
            watch.Stop();
            Assert.AreEqual(0, allocated);
            TestContext.WriteLine("S010 participant: 10000 warmed ticks, bytes=" + allocated + ", ms=" + watch.Elapsed.TotalMilliseconds);
        }
        [Test] public void LargeAndIncrementalAdvanceAreEquivalent()
        {
            production.Refuel(source); production.ToggleGenerator(); production.Start(source); var other = New(); other.Restore(production.Capture(), 0);
            production.ApplyElapsed(0, 7200); for (int i = 0; i < 7200; i++) other.ApplyElapsed(i, i + 1);
            Assert.AreEqual(JsonUtility.ToJson(production.Capture()), JsonUtility.ToJson(other.Capture()));
        }
    }
}
