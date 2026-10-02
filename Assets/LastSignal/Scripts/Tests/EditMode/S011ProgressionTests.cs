using System;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Objectives;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace LastSignal.Tests
{
    public sealed class S011ProgressionTests
    {
        ItemDefinition fuse, tool;
        InventoryContainer inventory;
        RelayProgression progress;
        [SetUp] public void Setup()
        {
            fuse = Definition(RelayProgression.FuseId); tool = Definition("tool.wrench"); inventory = new InventoryContainer(4); progress = new RelayProgression();
        }
        static ItemDefinition Definition(string id)
        {
            var d=ScriptableObject.CreateInstance<ItemDefinition>(); var s=new SerializedObject(d); s.FindProperty("stableId").FindPropertyRelative("id").stringValue=id; s.FindProperty("maxStack").intValue=1; s.ApplyModifiedPropertiesWithoutUndo(); return d;
        }
        [TearDown] public void Cleanup() { UnityEngine.Object.DestroyImmediate(fuse); UnityEngine.Object.DestroyImmediate(tool); }
        void Acquire() { inventory.TryAdd(fuse,1); inventory.TryAdd(tool,1); progress.ObserveInventory(inventory,fuse,tool); }
        [TestCase(false)] [TestCase(true)] public void BothOrdersReconcileWithoutSecondFuse(bool early)
        {
            if (early) Acquire(); progress.DiscoverRadio(); if (!early) Acquire();
            Assert.AreEqual(ObjectiveStage.RequirementsMet,progress.Stage(RelayBeat.Repair)); Assert.IsTrue(progress.Repair(inventory,fuse,tool,100));
            Assert.AreEqual(0,inventory.GetTotalQuantity(fuse)); Assert.AreEqual(1,progress.RewardCount); Assert.AreEqual(1,progress.Phase);
        }
        [Test] public void CompletedReceiptCannotReplayAfterDetachedRestore()
        {
            Acquire(); progress.DiscoverRadio(); Assert.IsTrue(progress.Repair(inventory,fuse,tool,5));
            for(int i=0;i<50;i++) { var copy=new RelayProgression(); copy.Restore(progress.Capture(),10); progress=copy; progress.DiscoverRadio(); progress.ObserveInventory(inventory,fuse,tool); Assert.IsFalse(progress.Repair(inventory,fuse,tool,10)); }
            Assert.AreEqual(1,progress.Capture().completionCount); Assert.AreEqual(RelayProgression.RewardReceipt,progress.Capture().rewardReceipt); Assert.AreEqual(ObjectiveStage.Completed,progress.Stage(RelayBeat.Repair));
        }
        [Test] public void PreviouslyAcquiredIsNotCurrentlyHeld()
        {
            Acquire(); inventory.TryRemove(fuse,1); progress.DiscoverRadio(); Assert.AreEqual(ObjectiveStage.Completed,progress.Stage(RelayBeat.Fuse)); Assert.IsFalse(progress.Repair(inventory,fuse,tool,1)); Assert.AreEqual(0,progress.RewardCount);
        }
        [Test] public void CommittedInventoryObserverSeesCoherentRewardAndFuse()
        {
            Acquire(); progress.DiscoverRadio(); bool observed=false;
            inventory.InventoryChanged += () => { observed=true; Assert.IsTrue(progress.Repaired); Assert.AreEqual(1,progress.RewardCount); Assert.AreEqual(RelayProgression.RewardReceipt,progress.Capture().rewardReceipt); Assert.AreEqual(0,inventory.GetTotalQuantity(fuse)); Assert.IsTrue(LastSignal.Persistence.OwnershipTransaction.Active); };
            Assert.IsTrue(progress.Repair(inventory,fuse,tool,10)); Assert.IsTrue(observed); Assert.IsFalse(LastSignal.Persistence.OwnershipTransaction.Active);
        }
        [Test] public void DuplicateDiscoveryEventsAreBoundedFacts()
        {
            int changes=0; progress.Changed+=()=>changes++; Acquire(); for(int i=0;i<1000;i++) { progress.DiscoverRadio(); progress.ObserveInventory(inventory,fuse,tool); } Assert.AreEqual(2,changes);
        }
        [Test] public void RepeatedCommittedFactsHaveBoundedAllocationAndMeasuredCost()
        {
            Acquire(); progress.DiscoverRadio(); progress.ObserveInventory(inventory,fuse,tool);
            long allocated=GC.GetAllocatedBytesForCurrentThread(); var watch=System.Diagnostics.Stopwatch.StartNew();
            for(int i=0;i<100000;i++) { progress.DiscoverRadio(); progress.ObserveInventory(inventory,fuse,tool); }
            watch.Stop(); long bytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
            TestContext.WriteLine("100000 duplicate radio/inventory fact pairs: "+watch.Elapsed.TotalMilliseconds.ToString("F3")+" ms; allocated="+bytes+" bytes (includes stopwatch). No receipt collection grows.");
            Assert.Less(bytes,4096);
        }
        [Test] public void MissingToolAndClueNeverConsumeFuse()
        {
            inventory.TryAdd(fuse,1); Assert.IsFalse(progress.Repair(inventory,fuse,tool,0)); progress.DiscoverRadio(); Assert.IsFalse(progress.Repair(inventory,fuse,tool,0)); Assert.AreEqual(1,inventory.GetTotalQuantity(fuse));
        }
        [Test] public void LegacyAbsentSectionStartsLockedAndReconcilesCarriedFuse()
        {
            progress.Restore(null,0); Acquire(); Assert.AreEqual(ObjectiveStage.Locked,progress.Stage(RelayBeat.Fuse)); progress.DiscoverRadio(); Assert.AreEqual(ObjectiveStage.Completed,progress.Stage(RelayBeat.Fuse));
        }
        [Test] public void RestoreDoesNotEmitCompletionOrRetainMutableDto()
        {
            Acquire(); progress.DiscoverRadio(); progress.Repair(inventory,fuse,tool,2); var snapshot=progress.Capture(); var next=new RelayProgression(); int events=0; next.Changed+=()=>events++; next.Restore(snapshot,2); snapshot.phase=0; Assert.AreEqual(1,next.Phase); Assert.AreEqual(0,events);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)] public void MalformedCompletionFailsClosed(int variant)
        {
            Acquire(); progress.DiscoverRadio(); progress.Repair(inventory,fuse,tool,2); var snapshot=progress.Capture();
            switch(variant) { case 0:snapshot.rewardCount=2;break;case 1:snapshot.rewardReceipt=null;break;case 2:snapshot.phase=0;break;case 3:snapshot.acquired=false;break;case 4:snapshot.completedAt=double.NaN;break;case 5:snapshot.definitionRevision=2;break;case 6:snapshot.definitionId="renamed";break;case 7:snapshot.repairReceipt="bad";break; }
            Assert.IsFalse(RelayProgression.Valid(snapshot,3)); Assert.Throws<ArgumentException>(()=>progress.Restore(snapshot,3)); Assert.AreEqual(1,progress.RewardCount);
        }
        [Test] public void EmptyObjectIsOnlyLegacyNormalizationShape()
        {
            Assert.IsTrue(RelayProgression.Empty(new RelaySnapshot())); Assert.IsFalse(RelayProgression.Valid(new RelaySnapshot(),0));
            Assert.IsFalse(RelayProgression.Empty(new RelaySnapshot { rewardCount=1 }));
        }
        [Test] public void ListenIsKnowledgeOnlyAndNeverIssuesSecondReward()
        {
            progress.Listen(); Assert.IsFalse(progress.Listened); Acquire(); progress.DiscoverRadio(); progress.Repair(inventory,fuse,tool,0); progress.Listen(); progress.Listen(); Assert.AreEqual(1,progress.RewardCount); Assert.AreEqual(ObjectiveStage.Completed,progress.Stage(RelayBeat.Listen));
        }
    }
}
