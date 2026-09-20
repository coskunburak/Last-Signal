using System.Reflection;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Loot;
using NUnit.Framework;
using UnityEngine;

namespace LastSignal.Tests
{
    public class LootProfileTests
    {
        LootProfile profile;
        ItemDefinition item;
        GameObject prefab;
        static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        [SetUp] public void Setup()
        {
            prefab = new GameObject("LootTestPrefab"); prefab.AddComponent<WorldItem>();
            item = ScriptableObject.CreateInstance<ItemDefinition>();
            Set(item, "stableId", new StableItemId("test.item")); Set(item, "worldPrefab", prefab);
            profile = ScriptableObject.CreateInstance<LootProfile>();
            Set(profile, "emptyBasisPoints", 0); Entries(1, 1, 3);
        }
        void Entries(int weight, int min, int max) => Set(profile, "entries", new[] { new LootEntry(item, weight, min, max) });
        [TearDown] public void Cleanup() { Object.DestroyImmediate(profile); Object.DestroyImmediate(item); Object.DestroyImmediate(prefab); }
        [Test] public void SameSeedAndPointAreExact()
        {
            for (int i = 0; i < 100; i++) { var a=profile.Select(i,"point-a"); var b=profile.Select(i,"point-a"); Assert.AreEqual(a.Outcome,b.Outcome); Assert.AreEqual(a.Item,b.Item); Assert.AreEqual(a.Quantity,b.Quantity); }
        }
        [Test] public void UnrelatedPointDoesNotChangeExistingPoint()
        { var a=profile.Select(123,"a"); profile.Select(123,"unrelated"); Assert.AreEqual(a.Quantity,profile.Select(123,"a").Quantity); }
        [Test] public void QuantityInclusiveAndVariesWithSeed()
        {
            var seen=new System.Collections.Generic.HashSet<int>();
            for(int i=0;i<1000;i++){var s=profile.Select(i,"a");Assert.That(s.Quantity,Is.InRange(1,3));seen.Add(s.Quantity);}
            Assert.AreEqual(3,seen.Count);
        }
        [Test] public void FixedQuantityAndWorldStackAboveMaxStackSupported()
        { Entries(1,100,100); Assert.AreEqual(100,profile.Select(7,"a").Quantity); }
        [TestCase(0,LootOutcome.Spawned)] [TestCase(10000,LootOutcome.Empty)]
        public void EmptyEdges(int chance,LootOutcome outcome)
        {Set(profile,"emptyBasisPoints",chance);for(int i=0;i<100;i++)Assert.AreEqual(outcome,profile.Select(i,"a").Outcome);}
        [TestCase(-1)] [TestCase(10001)] public void InvalidEmptyChance(int chance)
        {Set(profile,"emptyBasisPoints",chance);Assert.IsFalse(profile.Validate(out _));}
        [TestCase(0,1,1)] [TestCase(-1,1,1)] [TestCase(1,0,1)] [TestCase(1,2,1)]
        public void InvalidEntryRejected(int weight,int min,int max)
        {Entries(weight,min,max);Assert.AreEqual(LootOutcome.Invalid,profile.Select(1,"a").Outcome);}
        [Test] public void EmptyEntriesRejected(){Set(profile,"entries",new LootEntry[0]);Assert.IsFalse(profile.Validate(out _));}
        [Test] public void MissingItemRejected(){Set(profile,"entries",new[]{new LootEntry(null,1,1,1)});Assert.IsFalse(profile.Validate(out _));}
        [Test] public void MissingPrefabRejected(){Set(item,"worldPrefab",null);Assert.IsFalse(profile.Validate(out _));}
        [Test] public void DuplicateItemRejected(){Set(profile,"entries",new[]{new LootEntry(item,1,1,1),new LootEntry(item,2,1,2)});Assert.IsFalse(profile.Validate(out _));}
        [Test] public void LargeWeightAndQuantityDoNotOverflow(){Entries(int.MaxValue,1,int.MaxValue);Assert.That(profile.Select(1,"a").Quantity,Is.InRange(1,int.MaxValue));}
        [Test] public void GlobalRandomUnaffected()
        {var before=Random.state;profile.Select(1,"a");Assert.AreEqual(before,Random.state);}
        [Test] public void RandomBoundRejectsZero(){var r=new LootRandom(1,"a");Assert.Throws<System.ArgumentOutOfRangeException>(()=>r.Below(0));}
        [Test] public void IntermediateEmptyIsDeterministicAndMixed()
        {Set(profile,"emptyBasisPoints",3500);int empty=0;for(int i=0;i<10000;i++)if(profile.Select(i,"a").Outcome==LootOutcome.Empty)empty++;Assert.That(empty,Is.InRange(3200,3800));}
    }
}
