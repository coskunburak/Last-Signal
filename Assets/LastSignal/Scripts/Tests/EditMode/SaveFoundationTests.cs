using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LastSignal.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace LastSignal.Tests
{
    public class SaveFoundationTests
    {
        string directory, path;
        SaveCodec codec;
        SaveFileStore store;
        [SetUp] public void Setup()
        {
            directory = Path.Combine(Path.GetTempPath(), "LastSignal-SaveTests-" + Guid.NewGuid().ToString("N"));
            path = Path.Combine(directory, "current.json");
            codec = new SaveCodec(Validation());
            store = new SaveFileStore(path, codec);
        }
        [TearDown] public void Cleanup() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        static SaveValidation Validation() => new SaveValidation("world.fixture", "content.1",
            new Dictionary<string, int> { ["ammo.rifle"] = 60, ["medical.bandage"] = 10 }, "rifle.1", 30);
        static TransformSnapshot Pose(float x = 0) => new TransformSnapshot { x = x, y = .05f, qw = 1 };
        static ContainerSnapshot Container(string id) => new ContainerSnapshot { id = id, capacity = 3, slots = new[] {
            new SlotSnapshot { definitionId = "ammo.rifle", quantity = 60 }, new SlotSnapshot(), new SlotSnapshot { definitionId = "ammo.rifle", quantity = 7 } } };
        public static SaveGame Fixture(long generation = 1) => new SaveGame
        {
            header = new SaveHeader { schemaVersion = 1, contentVersion = "content.1", worldId = "world.fixture", buildId = "test", seed = -987, generation = generation, timestampUtc = DateTimeOffset.UtcNow.ToString("O") },
            player = new PlayerSnapshot { id = "player.local", health = 73, pitch = -22, transform = Pose(12.345f), crouching = true },
            inventory = Container("player.inventory"), weapon = new WeaponSnapshot { definitionId = "rifle.1", magazine = 17 },
            shelter = new ShelterSnapshot { storage = Container("shelter.storage"), expeditionIndex = 2, onExpedition = true },
            world = new WorldSnapshot {
                doors = new[] { new DoorSnapshot { id = "door.a", open = true } },
                opportunities = new[] {
                    new LootOpportunitySnapshot { id = "point.a", outcome = OpportunityOutcome.Generated, entityId = "loot.a" },
                    new LootOpportunitySnapshot { id = "point.b", outcome = OpportunityOutcome.Generated, entityId = "loot.b" },
                    new LootOpportunitySnapshot { id = "point.c", outcome = OpportunityOutcome.Empty } },
                items = new[] {
                    new WorldItemSnapshot { id = "loot.a", definitionId = "ammo.rifle", origin = WorldItemOrigin.Loot, disposition = EntityDisposition.Consumed, quantity = 0, transform = Pose(1) },
                    new WorldItemSnapshot { id = "loot.b", definitionId = "ammo.rifle", origin = WorldItemOrigin.Loot, disposition = EntityDisposition.Present, quantity = 13, transform = Pose(2) },
                    new WorldItemSnapshot { id = "drop.a", definitionId = "medical.bandage", origin = WorldItemOrigin.Drop, disposition = EntityDisposition.Present, quantity = 3, transform = Pose(3) } },
                enemies = new[] { new EnemySnapshot { id = "enemy.a", health = 0, transform = Pose(9) } }
            }
        };
        [Test] public void DiskRoundtripPreservesSchemaSlotsDoorLootRemainderDropPlayerAndExistingOwners()
        {
            var input = Fixture(); Assert.That(store.Write(input, 0).Success, Is.True);
            Assert.That(store.Read(0, out var saved).Success, Is.True);
            Assert.AreEqual(1, saved.header.schemaVersion); Assert.AreEqual(-987, saved.header.seed);
            Assert.AreEqual(60, saved.inventory.slots[0].quantity); Assert.AreEqual(0, saved.inventory.slots[1].quantity);
            Assert.AreEqual(7, saved.inventory.slots[2].quantity); Assert.AreEqual(17, saved.weapon.magazine);
            Assert.AreEqual(67, saved.shelter.storage.slots[0].quantity + saved.shelter.storage.slots[2].quantity);
            Assert.IsTrue(saved.world.doors[0].open);
            Assert.AreEqual(EntityDisposition.Consumed, saved.world.items[0].disposition);
            Assert.AreEqual(13, saved.world.items[1].quantity);
            Assert.AreEqual(WorldItemOrigin.Drop, saved.world.items[2].origin); Assert.AreEqual(3, saved.world.items[2].quantity);
            Assert.AreEqual(12.345f, saved.player.transform.x, .0001f); Assert.AreEqual(73, saved.player.health);
            Assert.AreEqual(0, saved.world.enemies[0].health); Assert.IsTrue(saved.player.crouching);
            Assert.That(File.ReadAllText(path), Does.Not.Contain("instanceID"));
        }
        [Test] public void WorldStackMayExceedContainerMaxStackWithoutLoss()
        {
            var state=Fixture();state.world.items[1].quantity=80;
            Assert.IsTrue(store.Write(state,0).Success);
            Assert.IsTrue(store.Read(0,out var saved).Success);Assert.AreEqual(80,saved.world.items[1].quantity);
        }
        [Test] public void RepeatedReadReturnsDetachedSameSemanticDataAndNeverAppends()
        {
            Assert.IsTrue(store.Write(Fixture(), 0).Success);
            for (int i = 0; i < 50; i++)
            {
                Assert.IsTrue(store.Read(0, out var saved).Success);
                Assert.AreEqual(3, saved.inventory.slots.Length); Assert.AreEqual(3, saved.world.items.Length);
                Assert.AreEqual(60, saved.inventory.slots[0].quantity);
                saved.inventory.slots[0].quantity = 1; // Cannot poison later reads or the committed file.
            }
        }
        [TestCase("missingSection")][TestCase("unknownItem")][TestCase("schema")][TestCase("world")]
        [TestCase("content")][TestCase("duplicate")][TestCase("orphan")][TestCase("unknownOutcome")]
        [TestCase("tombstoneQuantity")][TestCase("invalidPose")][TestCase("negativeStack")][TestCase("capacity")]
        [TestCase("invalidHealth")][TestCase("invalidMagazine")][TestCase("missingSlots")][TestCase("unknownOrigin")]
        [TestCase("generatedMissingTombstone")][TestCase("sharedLootEntity")][TestCase("timestamp")]
        public void InvalidCandidatesNeverReplacePreviousSave(string invalid)
        {
            Assert.IsTrue(store.Write(Fixture(), 0).Success); var bytes = File.ReadAllBytes(path);
            var c = Fixture(2);
            switch (invalid)
            {
                case "missingSection": c.player = null; break;
                case "unknownItem": c.inventory.slots[0].definitionId = "unknown.required"; break;
                case "schema": c.header.schemaVersion = 99; break;
                case "world": c.header.worldId = "another.world"; break;
                case "content": c.header.contentVersion = "future"; break;
                case "duplicate": c.world.doors[0].id = c.inventory.id; break;
                case "orphan": c.world.items[2].origin = WorldItemOrigin.Loot; break;
                case "unknownOutcome": c.world.opportunities[0].outcome = OpportunityOutcome.Unknown; break;
                case "tombstoneQuantity": c.world.items[0].quantity = 1; break;
                case "invalidPose": c.player.transform.qw = 0; break;
                case "negativeStack": c.inventory.slots[2].quantity = -1; break;
                case "capacity": c.inventory.capacity = 2; break;
                case "invalidHealth": c.player.health = float.NaN; break;
                case "invalidMagazine": c.weapon.magazine = 31; break;
                case "missingSlots": c.inventory.slots = null; break;
                case "unknownOrigin": c.world.items[2].origin = (WorldItemOrigin)999; break;
                case "generatedMissingTombstone": c.world.items = new[] { c.world.items[1], c.world.items[2] }; break;
                case "sharedLootEntity": c.world.opportunities[1].entityId = "loot.a"; break;
                case "timestamp": c.header.timestampUtc = "yesterday"; break;
            }
            Assert.IsFalse(store.Write(c, 0).Success, invalid);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(path));
            Assert.IsTrue(store.Read(0, out var previous).Success); Assert.AreEqual(1, previous.header.generation);
        }
        [TestCase("")][TestCase("{")][TestCase("null")][TestCase("[]")][TestCase("{}")] 
        public void MalformedOrMissingEnvelopeReturnsTypedFailureAndNoState(string json)
        {
            Directory.CreateDirectory(directory); File.WriteAllText(path, json);
            var result = store.Read(0, out var state);
            Assert.IsFalse(result.Success); Assert.AreNotEqual(SaveError.None, result.Error); Assert.IsNull(state);
            Assert.AreEqual(json, File.ReadAllText(path));
        }
        [Test] public void MissingSaveNeverCreatesAnEmptyCheckpoint()
        {
            Assert.AreEqual(SaveError.MissingFile, store.Read(0, out var state).Error);
            Assert.IsNull(state); Assert.IsFalse(Directory.Exists(directory));
        }
        [Test] public void SuccessfulReplacementKeepsExactlyPreviousGenerationAsExplicitBackup()
        {
            for (int i = 1; i <= 3; i++) Assert.IsTrue(store.Write(Fixture(i), 0).Success);
            Assert.IsTrue(store.Read(0, out var current).Success); Assert.AreEqual(3, current.header.generation);
            Assert.IsTrue(store.Read(0, out var backup, true).Success); Assert.AreEqual(2, backup.header.generation);
            Assert.AreEqual(2, Directory.GetFiles(directory).Length);
        }
        [Test] public void CorruptedCurrentDoesNotOverwriteCurrentOrGoodBackup()
        {
            Assert.IsTrue(store.Write(Fixture(), 0).Success); Assert.IsTrue(store.Write(Fixture(2), 0).Success);
            string backup = File.ReadAllText(path + ".bak");
            File.WriteAllText(path, "broken");
            Assert.IsFalse(store.Write(Fixture(3), 0).Success);
            Assert.AreEqual("broken", File.ReadAllText(path)); Assert.AreEqual(backup, File.ReadAllText(path + ".bak"));
            Assert.IsTrue(store.Read(0, out var saved, true).Success); Assert.AreEqual(1, saved.header.generation);
        }
        [Test] public void ChecksumRejectsWellFormedPayloadCorruption()
        {
            Assert.IsTrue(codec.Encode(Fixture(), out var json).Success);
            json = json.Replace("12.345", "22.345");
            Assert.AreEqual(SaveError.ChecksumMismatch, codec.Decode(json, out var state).Error); Assert.IsNull(state);
        }
        [Test] public void OversizeFileRejectedBeforeParsing()
        {
            Directory.CreateDirectory(directory);
            using (var f = File.Create(path)) f.SetLength(SaveCodec.MaximumBytes + 1);
            Assert.AreEqual(SaveError.TooLarge, store.Read(0, out var state).Error); Assert.IsNull(state);
        }
        [Test] public void InvalidUtf8FailsGracefully()
        {
            Directory.CreateDirectory(directory); File.WriteAllBytes(path, new byte[] { 0xc3, 0x28 });
            Assert.AreEqual(SaveError.IoFailure, store.Read(0, out _).Error);
        }
        [Test] public void NonIncreasingGenerationAndStaleSessionCannotPublish()
        {
            Assert.IsTrue(store.Write(Fixture(8), 0).Success);
            Assert.AreEqual(SaveError.StaleGeneration, store.Write(Fixture(8), 0).Error);
            Assert.AreEqual(SaveError.StaleGeneration, store.Write(Fixture(7), 0).Error);
            store.AdvanceSession();
            Assert.AreEqual(SaveError.StaleSession, store.Read(0, out _).Error);
            Assert.AreEqual(SaveError.StaleSession, store.Write(Fixture(9), 0).Error);
            Assert.IsTrue(store.Read(1, out var state).Success); Assert.AreEqual(8, state.header.generation);
        }
        sealed class FaultFiles : ISaveFiles
        {
            readonly PhysicalSaveFiles real = new PhysicalSaveFiles();
            public string fault;
            public Action afterCandidate;
            public bool Exists(string p) => real.Exists(p);
            public string Read(string p) => real.Read(p);
            public void WriteCandidate(string p, string json)
            {
                if (fault == "beforeWrite") throw new IOException("injected");
                if (fault == "partialWrite") { real.WriteCandidate(p, json.Substring(0, json.Length / 2)); throw new IOException("injected disk full"); }
                real.WriteCandidate(p, fault == "corruptCandidate" ? "broken" : json);
                afterCandidate?.Invoke();
            }
            public void Publish(string a, string b, string c)
            {
                if (fault == "beforePublish") throw new IOException("injected publication failure");
                real.Publish(a, b, c);
            }
            public void DeleteCandidate(string p) => real.DeleteCandidate(p);
        }
        [TestCase("beforeWrite")][TestCase("partialWrite")][TestCase("corruptCandidate")][TestCase("beforePublish")]
        public void FailedCandidatePreservesCurrentAndBackupByteForByte(string fault)
        {
            Assert.IsTrue(store.Write(Fixture(), 0).Success); Assert.IsTrue(store.Write(Fixture(2), 0).Success);
            var current = File.ReadAllBytes(path); var backup = File.ReadAllBytes(path + ".bak");
            var failing = new SaveFileStore(path, codec, new FaultFiles { fault = fault });
            Assert.IsFalse(failing.Write(Fixture(3), 0).Success);
            CollectionAssert.AreEqual(current, File.ReadAllBytes(path)); CollectionAssert.AreEqual(backup, File.ReadAllBytes(path + ".bak"));
            Assert.IsTrue(store.Read(0, out var saved).Success); Assert.AreEqual(2, saved.header.generation);
            Assert.AreEqual(0, Directory.GetFiles(directory, "*.tmp").Length);
        }
        [Test] public void SessionChangedAfterWriteDiscardsCandidateAndRejectsReentry()
        {
            Assert.IsTrue(store.Write(Fixture(), 0).Success);
            var files = new FaultFiles(); var guarded = new SaveFileStore(path, codec, files);
            files.afterCandidate = () => {
                Assert.AreEqual(SaveError.Busy, guarded.Read(0, out _).Error);
                Assert.AreEqual(SaveError.Busy, guarded.Write(Fixture(3), 0).Error);
                guarded.AdvanceSession();
            };
            Assert.AreEqual(SaveError.StaleSession, guarded.Write(Fixture(2), 0).Error);
            Assert.IsTrue(store.Read(0, out var state).Success); Assert.AreEqual(1, state.header.generation);
        }
        [Test] public void SmallWorldCodecAndPhysicalIoMeasurement()
        {
            var data = Fixture(); var clock = System.Diagnostics.Stopwatch.StartNew();
            long before = GC.GetAllocatedBytesForCurrentThread();
            Assert.IsTrue(codec.Encode(data, out var json).Success); double serialize = clock.Elapsed.TotalMilliseconds;
            clock.Restart(); Assert.IsTrue(store.Write(data, 0).Success); double write = clock.Elapsed.TotalMilliseconds;
            clock.Restart(); Assert.IsTrue(store.Read(0, out _).Success); double read = clock.Elapsed.TotalMilliseconds;
            TestContext.Out.WriteLine($"FIXTURE_ONLY items=3 slots=6 bytes={Encoding.UTF8.GetByteCount(json)} encodeMs={serialize:F4} validatedWriteMs={write:F4} validatedReadMs={read:F4} combinedAllocatedBytes={GC.GetAllocatedBytesForCurrentThread()-before}. Capture/hydrate NOT_RUN; no release-scale claim.");
        }
    }
}
