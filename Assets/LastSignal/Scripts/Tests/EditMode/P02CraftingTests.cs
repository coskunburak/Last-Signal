using System;
using LastSignal.Inventory;
using LastSignal.Shelter;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    public sealed class P02CraftingTests
    {
        P02CraftFixture f;
        [SetUp] public void Setup() => f = new P02CraftFixture();
        [TearDown] public void Cleanup() => f.Dispose();
        void Quantities(int a, int b, int c, int output = 0)
        {
            Assert.AreEqual(a, f.Source.GetTotalQuantity(f.A)); Assert.AreEqual(b, f.Source.GetTotalQuantity(f.B));
            Assert.AreEqual(c, f.Source.GetTotalQuantity(f.C)); Assert.AreEqual(output, f.Source.GetTotalQuantity(f.Output));
            Assert.AreEqual(1, f.Source.GetTotalQuantity(f.Tool), "Tool is never consumed.");
        }
        void Escrow(int a, int b, int c)
        {
            var payload = f.Production.Capture().crafting?.job?.escrow;
            Assert.AreEqual(a, P02CraftFixture.Count(payload, f.A)); Assert.AreEqual(b, P02CraftFixture.Count(payload, f.B));
            Assert.AreEqual(c, P02CraftFixture.Count(payload, f.C));
        }
        [Test] public void ThirdIngredientMissingConsumesNothingAndRecordsOnlyRejection()
        {
            // Initial A6/B4/C0/tool1, rev captured after C removal; request nonce1/current revision.
            f.Source.TryRemove(f.C, 2); long revision = f.Source.Revision; int events = 0; f.Source.InventoryChanged += () => events++;
            var result = f.Production.StartCraft(f.Request(), f.Source);
            Assert.AreEqual(CraftReason.MissingIngredients, result.reason); Quantities(6, 4, 0); Escrow(0, 0, 0);
            Assert.AreEqual(revision, f.Source.Revision); Assert.AreEqual(0, events); Assert.IsNull(f.Production.Status);
            Assert.AreEqual(revision, result.afterRevision); Assert.IsEmpty(result.consumed);
            Assert.AreEqual(1, f.Production.Capture().crafting.highWater, "Only rejection receipt changes save data; material ownership remains 10+tool.");
        }
        [Test] public void StartCommitsAllInputsAndOneRevisionBeforeAnyObserver()
        {
            long revision = f.Source.Revision; int events = 0; bool coherent = false, blocked = false, captureBlocked = false;
            f.Source.InventoryChanged += () => {
                events++; coherent = f.Source.GetTotalQuantity(f.A) == 4 && f.Production.Status == CraftStatus.Running;
                blocked = !f.Production.Cancel() && f.Production.StartCraft(f.Request(2), f.Source).reason == CraftReason.Busy && !f.Production.Start(f.Source);
                try { f.Production.Capture(); } catch (InvalidOperationException) { captureBlocked = true; }
            };
            var result = f.Production.StartCraft(f.Request(), f.Source);
            Assert.AreEqual(CraftReason.Accepted, result.reason); Quantities(4, 2, 0); Escrow(2, 2, 2);
            Assert.AreEqual(revision + 1, f.Source.Revision); Assert.AreEqual(1, events);
            Assert.IsTrue(coherent); Assert.IsTrue(blocked); Assert.IsTrue(captureBlocked);
            Assert.AreEqual(6, f.Source.GetTotalQuantity(f.A) + P02CraftFixture.Count(result.consumed, f.A));
        }
        [Test] public void ThrowingObserverCannotUndoCommitOrSuppressLaterObserver()
        {
            bool later = false; var request = f.Request();
            f.Source.InventoryChanged += () => throw new InvalidOperationException("S005 observer fault");
            f.Source.InventoryChanged += () => later = true;
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: S005 observer fault");
            Assert.AreEqual(CraftReason.Accepted, f.Production.StartCraft(request, f.Source).reason);
            Assert.IsTrue(later); Quantities(4, 2, 0); Escrow(2, 2, 2);
            Assert.AreEqual(CraftReason.Accepted, f.Production.StartCraft(request, f.Source).reason); Quantities(4, 2, 0);
        }
        [Test] public void StaleRevisionNeverConsumesOrCreatesEscrow()
        {
            var request = f.Request(); f.Source.TryAdd(f.A, 1); long revision = f.Source.Revision;
            Assert.AreEqual(CraftReason.StaleSource, f.Production.StartCraft(request, f.Source).reason);
            Quantities(7, 4, 2); Escrow(0, 0, 0); Assert.AreEqual(revision, f.Source.Revision);
        }
        [Test] public void SameNonceSamePayloadReturnsDetachedReceiptAndDifferentPayloadConflicts()
        {
            var request = f.Request(); var result = f.Production.StartCraft(request, f.Source); long revision = f.Source.Revision;
            result.consumed[0].quantity = 999;
            Assert.AreEqual(2, f.Production.StartCraft(request, f.Source).consumed[0].quantity);
            Assert.AreEqual(CraftReason.NonceConflict, f.Production.StartCraft(f.Request(), f.Source).reason);
            Assert.AreEqual(revision, f.Source.Revision); Quantities(4, 2, 0); Escrow(2, 2, 2);
        }
        [Test] public void CancelAndFullRefundDestinationPreserveExactOwnership()
        {
            var request = f.Request(); f.Production.StartCraft(request, f.Source); f.Production.ApplyElapsed(0, 5);
            Assert.IsTrue(f.Production.Cancel()); Assert.IsFalse(f.Production.Cancel());
            var full = new InventoryContainer(1); full.TryAdd(f.Output, f.Output.MaxStack); long revision = full.Revision;
            Assert.AreEqual(CraftReason.DestinationFull, f.Production.CollectCraft(full, "shelter.storage", revision));
            Assert.AreEqual(revision, full.Revision); Escrow(2, 2, 2); Quantities(4, 2, 0);
            Assert.AreEqual(CraftReason.Accepted, f.Production.CollectCraft(f.Source, "shelter.storage", f.Source.Revision));
            Quantities(6, 4, 2); Escrow(0, 0, 0); long after = f.Source.Revision;
            Assert.AreEqual(CraftReason.NoPendingResult, f.Production.CollectCraft(f.Source, "shelter.storage", after));
            Assert.AreEqual(after, f.Source.Revision); Assert.AreEqual(CraftReason.Accepted, f.Production.StartCraft(request, f.Source).reason);
            Quantities(6, 4, 2); Assert.IsNull(f.Production.Status);
        }
        [Test] public void FullOutputDestinationRetainsOneResultAndCollectsOnlyOnce()
        {
            f.Production.StartCraft(f.Request(), f.Source); f.Production.ApplyElapsed(0, 1000);
            var snapshot = f.Production.Capture(); Assert.IsEmpty(snapshot.crafting.job.escrow);
            Assert.AreEqual(3, P02CraftFixture.Count(snapshot.crafting.job.pendingOutput, f.Output));
            var full = new InventoryContainer(1); full.TryAdd(f.Output, f.Output.MaxStack); long rev = full.Revision;
            Assert.AreEqual(CraftReason.DestinationFull, f.Production.CollectCraft(full, "shelter.storage", rev));
            Assert.AreEqual(rev, full.Revision); Assert.AreEqual(CraftStatus.CompletedWaitingOutput, f.Production.Status);
            Assert.AreEqual(CraftReason.Accepted, f.Production.CollectCraft(f.Source, "shelter.storage", f.Source.Revision));
            Quantities(4, 2, 0, 3); Assert.AreEqual(CraftReason.NoPendingResult, f.Production.CollectCraft(f.Source, "shelter.storage", f.Source.Revision));
            Quantities(4, 2, 0, 3); Assert.IsNull(f.Production.Capture().crafting.job);
        }
        [Test] public void MultipleOutputsAreAllOrNothingWhenLastOutputCannotFit()
        {
            f.Recipe.outputDefinitions = new[] { P02CraftFixture.Amount(f.Output, 3), P02CraftFixture.Amount(f.Fuel, 1) };
            f.Production = f.Create(); f.Production.StartCraft(f.Request(), f.Source); f.Production.ApplyElapsed(0, 10);
            var target = new InventoryContainer(1); long rev = target.Revision;
            Assert.AreEqual(CraftReason.DestinationFull, f.Production.CollectCraft(target, "player.inventory", rev));
            Assert.AreEqual(0, target.GetTotalQuantity(f.Output)); Assert.AreEqual(0, target.GetTotalQuantity(f.Fuel)); Assert.AreEqual(rev, target.Revision);
            Assert.AreEqual(2, f.Production.Capture().crafting.job.pendingOutput.Length);
        }
        [Test] public void MultiStackIngredientsCommitInOneNotification()
        {
            f.Recipe.inputIngredients[1].quantity = f.A.MaxStack + 2; f.Production = f.Create();
            f.Source.TryAdd(f.A, f.A.MaxStack); long rev = f.Source.Revision; int events = 0; f.Source.InventoryChanged += () => events++;
            Assert.AreEqual(CraftReason.Accepted, f.Production.StartCraft(f.Request(), f.Source).reason);
            Assert.AreEqual(4, f.Source.GetTotalQuantity(f.A)); Assert.AreEqual(rev + 1, f.Source.Revision); Assert.AreEqual(1, events);
        }
        [Test] public void EvictedNonceCannotExecuteAfterCollectionOrReload()
        {
            var original = f.Request(); f.Production.StartCraft(original, f.Source); f.Production.Cancel();
            f.Production.CollectCraft(f.Source, "shelter.storage", f.Source.Revision);
            for (int i = 2; i <= 40; i++) f.Production.StartCraft(f.Request(i, 0), f.Source);
            var snapshot = f.Production.Capture(); Assert.AreEqual(MultiCraftSnapshot.ReceiptLimit, snapshot.crafting.receipts.Length);
            var restored = f.Create(); restored.Restore(snapshot, 0);
            Assert.AreEqual(CraftReason.ReceiptExpired, restored.StartCraft(original, f.Source).reason); Quantities(6, 4, 2);
        }
        [Test] public void LegacyAndMultiJobsCannotOwnInputsAtTheSameTime()
        {
            f.Production.StartCraft(f.Request(), f.Source); long revision = f.Source.Revision;
            Assert.IsFalse(f.Production.Start(f.Source)); Assert.AreEqual(revision, f.Source.Revision);
            f.Production.Cancel(); f.Production.CollectCraft(f.Source, "shelter.storage", f.Source.Revision);
            Assert.IsTrue(f.Production.Start(f.Source)); revision = f.Source.Revision;
            Assert.AreEqual(CraftReason.Busy, f.Production.StartCraft(f.Request(2), f.Source).reason); Assert.AreEqual(revision, f.Source.Revision);
        }
        [TestCase("station", CraftReason.MissingStation)] [TestCase("tool", CraftReason.MissingTool)]
        [TestCase("unlock", CraftReason.Locked)] [TestCase("source", CraftReason.UnsupportedSource)]
        [TestCase("recipeRevision", CraftReason.RecipeRevisionMismatch)]
        public void GatesRejectWithoutMaterialMutation(string gate, CraftReason expected)
        {
            if (gate == "unlock") f.Recipe.unlockRequirement = CraftUnlock.UpgradedWorkbench;
            if (gate == "source") f.Recipe.inputIngredients[0].sourcePolicy = IngredientSource.PlayerInventory;
            f.Production = f.Create(modules: gate != "station");
            if (gate == "station") f.Production.Claim();
            if (gate == "tool") f.Source.TryRemove(f.Tool, 1);
            long rev = f.Source.Revision; var before = new[] { f.Source.GetTotalQuantity(f.A), f.Source.GetTotalQuantity(f.B), f.Source.GetTotalQuantity(f.C) };
            var request = gate == "station" ? f.Request(source: "player.inventory") : f.Request();
            if (gate == "recipeRevision") request = new CraftRequest("shelter.s005", f.Production.CraftEpoch, 1, f.Recipe.recipeId, 2, "shelter.storage", rev);
            Assert.AreEqual(expected, f.Production.StartCraft(request, f.Source).reason);
            CollectionAssert.AreEqual(before, new[] { f.Source.GetTotalQuantity(f.A), f.Source.GetTotalQuantity(f.B), f.Source.GetTotalQuantity(f.C) });
            Assert.AreEqual(rev, f.Source.Revision); Escrow(0, 0, 0); Assert.IsNull(f.Production.Status);
        }
        [Test] public void PriorSessionRequestAndUnknownContainerAreRejectedWithoutReceiptMutation()
        {
            var old = f.Request(); f.Production = f.Create(); long revision = f.Source.Revision;
            Assert.AreEqual(CraftReason.InvalidRequest, f.Production.StartCraft(old, f.Source).reason);
            Assert.AreEqual(CraftReason.UnsupportedSource, f.Production.StartCraft(f.Request(source: "vehicle.cargo"), f.Source).reason);
            Assert.AreEqual(0, f.Production.CraftHighWater); Assert.AreEqual(revision, f.Source.Revision); Quantities(6, 4, 2); Escrow(0, 0, 0);
        }
        [Test] public void AliasedFinalIngredientAbortsEntireStagedPlanWithoutPartialConsumption()
        {
            var alias = UnityEngine.Object.Instantiate(f.A);
            try
            {
                f.Source = new InventoryContainer(12); f.Source.TryAdd(alias, 6); f.Source.TryAdd(f.B, 4); f.Source.TryAdd(f.C, 2); f.Source.TryAdd(f.Tool, 1);
                long revision = f.Source.Revision;
                Assert.AreEqual(CraftReason.MissingIngredients, f.Production.StartCraft(f.Request(), f.Source).reason);
                Assert.AreEqual(revision, f.Source.Revision); Quantities(6, 4, 2); Escrow(0, 0, 0); Assert.IsNull(f.Production.Status);
            }
            finally { UnityEngine.Object.DestroyImmediate(alias); }
        }
        [Test] public void StaleCollectionAndWrongRefundOwnerLeavePendingPayloadUntouched()
        {
            f.Production.StartCraft(f.Request(), f.Source); f.Production.Cancel(); long revision = f.Source.Revision;
            Assert.AreEqual(CraftReason.StaleSource, f.Production.CollectCraft(f.Source, "shelter.storage", revision - 1));
            Assert.AreEqual(CraftReason.UnsupportedSource, f.Production.CollectCraft(f.Source, "player.inventory", revision));
            Assert.AreEqual(revision, f.Source.Revision); Quantities(4, 2, 0); Escrow(2, 2, 2);
        }
        [Test] public void UpgradeCannotChangeActiveJobAndNewWorkbenchJobUsesUpgradedDuration()
        {
            f.Production.StartCraft(f.Request(), f.Source); long revision = f.Source.Revision;
            Assert.IsFalse(f.Production.Upgrade(f.Source)); Assert.AreEqual(revision, f.Source.Revision);
            f.Production.Cancel(); f.Production.CollectCraft(f.Source, "shelter.storage", revision);
            Assert.IsTrue(f.Production.Upgrade(f.Source));
            Assert.AreEqual(CraftReason.Accepted, f.Production.StartCraft(f.Request(2), f.Source).reason);
            Assert.AreEqual(f.Recipe.duration * .5, f.Production.Capture().crafting.job.duration);
            var restored = f.Create(); Assert.IsTrue(restored.CanRestore(f.Production.Capture(), 0));
        }
        [Test] public void UnsupportedDurableCommandIsRejectedBeforeAnyAuthoritativeMutation()
        {
            long revision = f.Source.Revision;
            var request = new CraftRequest("shelter.s005", f.Production.CraftEpoch, 1, f.Recipe.recipeId,
                f.Recipe.definitionRevision, "shelter.storage", revision, requireDurableGeneration: true);
            Assert.AreEqual(CraftReason.DurabilityUnavailable, f.Production.StartCraft(request, f.Source).reason);
            Assert.AreEqual(revision, f.Source.Revision); Quantities(6, 4, 2); Escrow(0, 0, 0);
            Assert.AreEqual(0, f.Production.CraftHighWater); Assert.AreEqual(0, f.Production.Capture().craftingVersion);
        }
    }
}
