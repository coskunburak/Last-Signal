using System;
using System.Collections.Generic;
using System.IO;
using LastSignal.Inventory;
using LastSignal.Persistence;
using LastSignal.Shelter;
using LastSignal.WorldTime;
using NUnit.Framework;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class P02CraftPersistenceTests
    {
        P02CraftFixture f;
        SaveCodec codec;
        [SetUp] public void Setup()
        {
            f = new P02CraftFixture();
            var limits = new Dictionary<string,int> { ["medical.bandage"] = 10 };
            foreach (var item in new[] { f.A, f.B, f.C, f.Tool, f.Output, f.Fuel }) limits[item.Id.Value] = item.MaxStack;
            codec = new SaveCodec(new SaveValidation("world.fixture", "content.1", limits, "rifle.1", 30));
        }
        [TearDown] public void Cleanup() => f.Dispose();
        SaveGame Fixture(long generation = 1)
        {
            var save = SaveFoundationTests.Fixture(generation);
            save.header.schemaVersion = 2;
            save.inventory.revisionVersion = 1;
            save.worldTime = new WorldSimulation(new WorldTimeSettings { startingSeconds = f.Production.Capture().lastProcessed }).Capture();
            save.shelter.production = f.Production.Capture();
            var slots = new SlotSnapshot[f.Source.Capacity];
            for (int i = 0; i < slots.Length; i++) { var s = f.Source.GetSlot(i); slots[i] = new SlotSnapshot { definitionId = s.IsEmpty ? null : s.Item.Id.Value, quantity = s.Quantity }; }
            save.shelter.storage = new ContainerSnapshot { id = "shelter.storage", revisionVersion = 1, revision = f.Source.Revision, capacity = slots.Length, slots = slots };
            return save;
        }
        InventoryContainer RestoreSource(ContainerSnapshot saved)
        {
            var result = new InventoryContainer(saved.capacity);
            foreach (var slot in saved.slots)
            {
                if (slot.quantity == 0) continue;
                var definition = Array.Find(new[] { f.A, f.B, f.C, f.Tool, f.Output, f.Fuel }, item => item.Id.Value == slot.definitionId);
                Assert.IsNotNull(definition); Assert.AreEqual(slot.quantity, result.TryAdd(definition, slot.quantity));
            }
            return result;
        }
        [TestCase("legacyEmpty")] [TestCase("legacyRunning")] [TestCase("running")]
        [TestCase("refund")] [TestCase("completed")] [TestCase("collected")]
        public void FullSaveCodecRoundtripKeepsSingleOwnerAndReceiptRetry(string fixture)
        {
            var request = f.Request();
            if (fixture == "legacyRunning") f.Production.Start(f.Source);
            else if (fixture != "legacyEmpty")
            {
                f.Production.StartCraft(request, f.Source);
                if (fixture == "refund") { f.Production.ApplyElapsed(0, 5); f.Production.Cancel(); }
                if (fixture == "completed" || fixture == "collected") f.Production.ApplyElapsed(0, 10);
                if (fixture == "collected") f.Production.CollectCraft(f.Source, "shelter.storage", f.Source.Revision);
            }
            var input = Fixture(); Assert.IsTrue(codec.Encode(input, out var json).Success);
            var result = codec.Decode(json, out var loaded); Assert.IsTrue(result.Success, result.Message);
            var restored = f.Create(loaded.worldTime.seconds); Assert.IsTrue(restored.CanRestore(loaded.shelter.production, loaded.worldTime.seconds));
            restored.Restore(loaded.shelter.production, loaded.worldTime.seconds);
            var source = RestoreSource(loaded.shelter.storage); long revision = source.Revision;
            if (!fixture.StartsWith("legacy", StringComparison.Ordinal))
            {
                Assert.AreEqual(CraftReason.Accepted, restored.StartCraft(request, source).reason);
                Assert.AreEqual(revision, source.Revision, "Reload/retry cannot spend again even when live revisions differ.");
                Assert.AreEqual(4, source.GetTotalQuantity(f.A));
                if (fixture == "running") { restored.ApplyElapsed(0, 10); fixture = "completed"; }
                if (fixture == "refund" || fixture == "completed")
                {
                    Assert.AreEqual(CraftReason.Accepted, restored.CollectCraft(source, "shelter.storage", source.Revision));
                    Assert.AreEqual(fixture == "refund" ? 6 : 4, source.GetTotalQuantity(f.A));
                    Assert.AreEqual(fixture == "refund" ? 0 : 3, source.GetTotalQuantity(f.Output));
                    Assert.AreEqual(CraftReason.NoPendingResult, restored.CollectCraft(source, "shelter.storage", source.Revision));
                }
                else Assert.AreEqual(3, source.GetTotalQuantity(f.Output));
            }
            else
            {
                Assert.AreEqual(0, loaded.shelter.production.craftingVersion); Assert.IsNull(loaded.shelter.production.crafting);
                if (fixture == "legacyRunning") { restored.ApplyElapsed(0, 10); Assert.IsTrue(restored.Collect(source)); Assert.AreEqual(3, source.GetTotalQuantity(f.Output)); }
                else Assert.IsNull(restored.Status);
            }
        }
        [Test] public void PreS010SaveWithoutProductionStillDecodesWithoutNewExtension()
        {
            var legacy = SaveFoundationTests.Fixture(); Assert.IsTrue(codec.Encode(legacy, out var json).Success);
            Assert.IsTrue(codec.Decode(json, out var restored).Success); Assert.IsNull(restored.shelter.production);
        }
        [TestCase("version")] [TestCase("payload")] [TestCase("doubleOwner")] [TestCase("revision")]
        [TestCase("hash")] [TestCase("status")] [TestCase("epoch")] [TestCase("marker")] [TestCase("shelterId")] [TestCase("receipt")] [TestCase("highWater")]
        public void InvalidSnapshotCannotReplaceLiveState(string corruption)
        {
            f.Production.StartCraft(f.Request(), f.Source); var before = JsonUtility.ToJson(f.Production.Capture());
            var bad = f.Production.Capture();
            switch (corruption)
            {
                case "version": bad.craftingVersion = 2; break;
                case "epoch": bad.crafting.sessionEpoch = Guid.NewGuid().ToString("N"); break;
                case "marker": bad.craftingVersion = 0; break;
                case "shelterId": bad.shelterId = null; break;
                case "payload": bad.crafting.job.escrow[0].quantity++; break;
                case "doubleOwner": bad.crafting.job.pendingOutput = new[] { new CraftItemSnapshot { itemId = f.Output.Id.Value, quantity = 3 } }; break;
                case "revision": bad.crafting.job.revision++; break;
                case "hash": bad.crafting.job.definitionHash = new string('0', 64); break;
                case "status": bad.crafting.job.status = CraftStatus.CompletedWaitingOutput; break;
                case "receipt": bad.crafting.receipts[0].afterRevision++; break;
                case "highWater": bad.crafting.highWater = 0; break;
            }
            Assert.IsFalse(f.Production.CanRestore(bad, 0)); Assert.Throws<ArgumentException>(() => f.Production.Restore(bad, 0));
            Assert.AreEqual(before, JsonUtility.ToJson(f.Production.Capture())); Assert.AreEqual(4, f.Source.GetTotalQuantity(f.A));
        }
        [Test] public void PublishedGenerationContainsInventoryEscrowAndReceiptAndInvalidWriteKeepsBytes()
        {
            string dir = Path.Combine(Path.GetTempPath(), "S005-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
            try
            {
                string path = Path.Combine(dir, "current.json"); var store = new SaveFileStore(path, codec);
                Assert.IsTrue(store.Write(Fixture(), 0).Success);
                var request = f.Request(); f.Production.StartCraft(request, f.Source);
                Assert.IsTrue(store.Write(Fixture(2), 0).Success); byte[] bytes = File.ReadAllBytes(path);
                var invalid = Fixture(3); invalid.shelter.production.crafting.job.escrow[0].itemId = "missing.item";
                Assert.IsFalse(store.Write(invalid, 0).Success); CollectionAssert.AreEqual(bytes, File.ReadAllBytes(path));
                Assert.IsTrue(store.Read(0, out var saved).Success); Assert.AreEqual(2, saved.header.generation);
                var restored = f.Create(); restored.Restore(saved.shelter.production, 0); var source = RestoreSource(saved.shelter.storage);
                Assert.AreEqual(CraftReason.Accepted, restored.StartCraft(request, source).reason); Assert.AreEqual(4, source.GetTotalQuantity(f.A));
                Assert.AreEqual(2, P02CraftFixture.Count(restored.Capture().crafting.job.escrow, f.A));
                Assert.IsTrue(store.Read(0, out var backup, backup: true).Success); Assert.AreEqual(1, backup.header.generation);
            }
            finally { Directory.Delete(dir, true); }
        }
        [Test] public void ChangedRecipeWithoutRevisionBumpCannotReinterpretSavedEscrow()
        {
            f.Production.StartCraft(f.Request(), f.Source); var snapshot = f.Production.Capture();
            f.Recipe.outputDefinitions[0].quantity++;
            Assert.IsFalse(f.Create().CanRestore(snapshot, 0));
        }
        [Serializable] sealed class LegacyProductionWire
        {
            public int version;
            public string shelterId;
            public bool claimed, bed, storage, workbench, upgraded, generatorEnabled;
            public double fuelSeconds, lastProcessed;
            public long sequence;
            public CraftSnapshot job;
        }
        [Serializable] sealed class WireEnvelope { public string format, checksum, payload; }
        [TestCase(false)] [TestCase(true)]
        public void ActualLegacyWireWithoutS005FieldsRestoresEmptyOrRunningJob(bool running)
        {
            if (running) Assert.IsTrue(f.Production.Start(f.Source));
            var save = Fixture(); var p = save.shelter.production;
            var legacy = new LegacyProductionWire { version = p.version, shelterId = p.shelterId, claimed = p.claimed,
                bed = p.bed, storage = p.storage, workbench = p.workbench, upgraded = p.upgraded, generatorEnabled = p.generatorEnabled,
                fuelSeconds = p.fuelSeconds, lastProcessed = p.lastProcessed, sequence = p.sequence, job = p.job };
            string payload = JsonUtility.ToJson(save).Replace(JsonUtility.ToJson(p), JsonUtility.ToJson(legacy));
            Assert.That(payload, Does.Not.Contain("craftingVersion"));
            string checksum;
            using (var sha = System.Security.Cryptography.SHA256.Create())
                checksum = BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload))).Replace("-", "").ToLowerInvariant();
            var result = codec.Decode(JsonUtility.ToJson(new WireEnvelope { format = "last-signal-save-1", checksum = checksum, payload = payload }), out var decoded);
            Assert.IsTrue(result.Success, result.Message); Assert.IsNull(decoded.shelter.production.crafting);
            var restored = f.Create(); restored.Restore(decoded.shelter.production, 0);
            Assert.AreEqual(running ? (CraftStatus?)CraftStatus.Running : null, restored.Status);
        }
        [Test] public void InventoryRevisionRoundtripPreservesNewRevisionAndAcceptsLegacyShape()
        {
            var owner = new GameObject("S005 revision fixture");
            var inventory = owner.AddComponent<PlayerInventory>(); inventory.Initialize(12);
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<LastSignal.Inventory.Data.ItemCatalog>("Assets/LastSignal/Data/Items/Definitions/ItemCatalog.asset");
            try
            {
                inventory.TryAdd(f.A, 6); inventory.TryAdd(f.B, 4);
                for (int i = 0; i < 12; i++) { inventory.TryAdd(f.A, 1); inventory.TryRemove(f.A, 1); }
                long revision = inventory.Revision;
                Assert.IsTrue(InventorySnapshots.Capture(inventory, "player.inventory", out var snapshot).Success);
                Assert.AreEqual(1, snapshot.revisionVersion); Assert.AreEqual(revision, snapshot.revision);
                inventory.Clear();
                Assert.IsTrue(InventorySnapshots.Restore(inventory, snapshot, "player.inventory", catalog).Success);
                Assert.AreEqual(revision, inventory.Revision); Assert.AreEqual(6, inventory.GetTotalQuantity(f.A));
                snapshot.revisionVersion = 0; snapshot.revision = 0;
                Assert.IsTrue(InventorySnapshots.Restore(inventory, snapshot, "player.inventory", catalog).Success);
                Assert.AreEqual(revision + 1, inventory.Revision, "Legacy hydration retains its original revision increment.");
                snapshot.revision = 100;
                Assert.IsFalse(InventorySnapshots.Restore(inventory, snapshot, "player.inventory", catalog).Success);
                Assert.AreEqual(revision + 1, inventory.Revision); Assert.AreEqual(6, inventory.GetTotalQuantity(f.A));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }
        [Test] public void CraftGenerationWithoutMatchingPersistedSourceRevisionFailsClosed()
        {
            f.Production.StartCraft(f.Request(), f.Source); var snapshot = Fixture();
            snapshot.shelter.storage.revisionVersion = 0; snapshot.shelter.storage.revision = 0;
            Assert.IsFalse(codec.Encode(snapshot, out _).Success);
            snapshot = Fixture(); snapshot.shelter.storage.revision--;
            Assert.IsFalse(codec.Encode(snapshot, out _).Success);
            Assert.AreEqual(4, f.Source.GetTotalQuantity(f.A));
        }
    }
}
