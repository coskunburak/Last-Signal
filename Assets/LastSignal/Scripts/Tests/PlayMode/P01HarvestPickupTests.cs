using System;
using System.Collections;
using System.Reflection;
using LastSignal.Gathering;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>Real WorldItem/InventoryContainer path; isolated from scene authoring and full-session hydration.</summary>
    public sealed class P01HarvestPickupTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        GameObject player,root;
        ItemDefinition log;
        PlayerInventory inventory;
        WorldItem item;
        ResourceNodeState node;
        bool allowed;
        [SetUp] public void Setup()
        {
            log=ScriptableObject.CreateInstance<ItemDefinition>();
            typeof(ItemDefinition).GetField("stableId",Private).SetValue(log,new StableItemId("material.log"));
            typeof(ItemDefinition).GetField("maxStack",Private).SetValue(log,8);
            typeof(ItemDefinition).GetField("massGrams",Private).SetValue(log,2000);
            player=new GameObject("harvest inventory fixture"); inventory=player.AddComponent<PlayerInventory>(); inventory.Initialize(1);
            root=new GameObject("harvest pickup fixture"); item=root.AddComponent<WorldItem>(); item.Configure(log,3);
            node=new ResourceNodeState { id="harvest:pickup",reward=RewardLocation.World,
                pickup=new WorldItemSnapshot { id="harvest:pickup:yield:1",quantity=3,origin=WorldItemOrigin.Harvest,disposition=EntityDisposition.Present } };
            typeof(WorldItem).GetMethod("AssignPersistentIdentity",Private).Invoke(item,new object[] {node.pickup.id,WorldItemOrigin.Harvest});
            typeof(WorldItem).GetMethod("BindPickupGuard",Private).Invoke(item,new object[] {
                new Func<WorldItem,PlayerInventory,bool>((p,i)=>allowed && i==inventory && p.PersistentId==node.pickup.id && p.Quantity==node.pickup.quantity) });
            typeof(WorldItem).GetMethod("BindPersistence",Private).Invoke(item,new object[] {
                new Action<string,int>((id,q)=> { Assert.AreEqual(node.pickup.id,id); node.pickup.quantity=q;
                    if(q==0) { node.reward=RewardLocation.Consumed; node.pickup.disposition=EntityDisposition.Consumed; } }) });
            allowed=true;
        }
        [TearDown] public void Cleanup()
        { if(root) UnityEngine.Object.DestroyImmediate(root); if(player) UnityEngine.Object.DestroyImmediate(player); if(log) UnityEngine.Object.DestroyImmediate(log); }
        [UnityTest] public IEnumerator FullInventoryRetainsStableWorldPickupAndDoesNotNotifyMutation()
        {
            Assert.AreEqual(8,inventory.TryAdd(log,8)); long revision=inventory.Revision;
            Assert.IsFalse(item.TryInteract()); Assert.AreEqual(3,item.Quantity); Assert.AreEqual(3,node.pickup.quantity);
            Assert.AreEqual(revision,inventory.Revision); Assert.AreEqual(WorldItemOrigin.Harvest,item.Origin);
            yield return null; Assert.IsNotNull(item);
        }
        [UnityTest] public IEnumerator PartialCapacityThenCompletePickupConservesTenLogsWithoutRepeatReward()
        {
            Assert.AreEqual(7,inventory.TryAdd(log,7)); Assert.IsTrue(item.TryInteract());
            Assert.AreEqual(8,inventory.GetTotalQuantity(log)); Assert.AreEqual(2,item.Quantity); Assert.AreEqual(2,node.pickup.quantity);
            Assert.IsFalse(item.TryInteract());
            Assert.IsTrue(inventory.TryRemove(0,2)); // Two logs consumed by an existing inventory operation.
            Assert.IsTrue(item.TryInteract()); Assert.IsFalse(item.TryInteract());
            Assert.AreEqual(8,inventory.GetTotalQuantity(log)); Assert.AreEqual(0,node.pickup.quantity);
            Assert.AreEqual(RewardLocation.Consumed,node.reward); Assert.AreEqual(EntityDisposition.Consumed,node.pickup.disposition);
            Assert.AreEqual(16000,inventory.TotalMassGrams);
            yield return null; Assert.IsTrue(item==null);
        }
        [UnityTest] public IEnumerator GuardRejectsUnavailableOwnerBeforeInventoryMutation()
        {
            allowed=false; Assert.IsFalse(item.TryInteract()); Assert.AreEqual(0,inventory.GetTotalQuantity(log)); Assert.AreEqual(3,item.Quantity);
            allowed=true; Assert.IsTrue(item.TryInteract()); Assert.AreEqual(3,inventory.GetTotalQuantity(log));
            yield return null; Assert.IsTrue(item==null);
        }
    }
}
