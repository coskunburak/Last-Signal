using System;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Shelter;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public class ShelterDomainTests
    {
        GameObject owner;
        PlayerInventory player;
        ShelterStorage stash;
        ItemDefinition ammo, scrap;
        [SetUp] public void Setup()
        {
            owner = new GameObject("S008 test carrier"); player = owner.AddComponent<PlayerInventory>(); player.Initialize(3);
            stash = new ShelterStorage(4);
            ammo = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Game/Items/Definitions/ammo.rifle.asset");
            scrap = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Game/Items/Definitions/material.scrap.asset");
        }
        [TearDown] public void Cleanup() => Object.DestroyImmediate(owner);
        int Total => player.GetTotalQuantity(ammo) + stash.GetTotalQuantity(ammo);
        [Test] public void EmptyBoundedStorageUsesSameStackRules()
        {
            Assert.AreEqual(4, stash.Capacity); Assert.IsTrue(stash.GetSlot(0).IsEmpty);
            Assert.AreEqual(240, stash.TryAdd(ammo, 300));
            for (int i=0;i<4;i++) Assert.AreEqual(60, stash.GetSlot(i).Quantity);
            Assert.AreEqual(0,stash.TryAdd(ammo,1)); Assert.IsFalse(stash.TryRemove(ammo,241));
            Assert.IsTrue(stash.TryRemove(ammo,240)); Assert.AreEqual(0,stash.GetTotalQuantity(ammo));
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(-100)]
        public void InvalidQuantityIsNoOp(int quantity)
        {
            player.TryAdd(ammo,20); Assert.AreEqual(0,stash.TryAdd(ammo,quantity));
            Assert.AreEqual(TransferReason.InvalidRequest,ItemTransferService.Deposit(player,stash,ammo,quantity).Reason);Assert.AreEqual(20,Total);
        }
        [Test] public void NullDefinitionAndMissingOwnerRejected()
        {
            Assert.AreEqual(0,stash.TryAdd(null,10)); Assert.IsFalse(stash.TryRemove(null,10));
            Assert.AreEqual(0,ItemTransferService.Deposit(player,stash,null,10).Moved);
            Assert.AreEqual(0,ItemTransferService.Withdraw(stash,null,ammo,10).Moved);
            Assert.Throws<ArgumentOutOfRangeException>(()=>new ShelterStorage(0));
        }
        [TestCase(1)] [TestCase(60)] [TestCase(125)]
        public void BidirectionalMultiStackExact(int amount)
        {
            player.TryAdd(ammo,130); var receipt=ItemTransferService.Deposit(player,stash,ammo,amount);
            Assert.AreEqual(amount,receipt.Moved);Assert.AreEqual(0,receipt.Remaining);Assert.AreEqual(130,Total);
            receipt=ItemTransferService.Withdraw(stash,player,ammo,amount);
            Assert.AreEqual(amount,receipt.Moved);Assert.AreEqual(130,player.GetTotalQuantity(ammo));Assert.AreEqual(130,Total);
        }
        [Test] public void PartialDepositRetainsOverflow()
        {
            stash=new ShelterStorage(1);stash.TryAdd(ammo,53);player.TryAdd(ammo,20);
            var r=ItemTransferService.Deposit(player,stash,ammo,20);
            Assert.AreEqual(7,r.Moved);Assert.AreEqual(13,r.Remaining);Assert.AreEqual(TransferReason.Partial,r.Reason);
            Assert.AreEqual(13,player.GetTotalQuantity(ammo));Assert.AreEqual(73,Total);
            r=ItemTransferService.Deposit(player,stash,ammo,13);Assert.AreEqual(TransferReason.DestinationFull,r.Reason);Assert.AreEqual(73,Total);
        }
        [Test] public void PartialWithdrawalRetainsStoredRemainder()
        {
            player.Initialize(1);player.TryAdd(ammo,50);stash.TryAdd(ammo,30);
            var r=ItemTransferService.Withdraw(stash,player,ammo,20);
            Assert.AreEqual(10,r.Moved);Assert.AreEqual(10,r.Remaining);Assert.AreEqual(20,stash.GetTotalQuantity(ammo));Assert.AreEqual(80,Total);
        }
        [Test] public void RequestBeyondSourceMovesAvailableOnly()
        {
            player.TryAdd(ammo,7);var r=ItemTransferService.Deposit(player,stash,ammo,20);
            Assert.AreEqual(7,r.Moved);Assert.AreEqual(13,r.Remaining);Assert.AreEqual(7,Total);
            Assert.AreEqual(TransferReason.SourceEmpty,ItemTransferService.Deposit(player,stash,ammo,1).Reason);
        }
        [Test] public void ObserversSeeBothCommittedOwnersAndCannotReenter()
        {
            player.TryAdd(ammo,40);int a=0,b=0;
            player.InventoryChanged+=()=>{a++;Assert.AreEqual(40,Total);Assert.AreEqual(20,stash.GetTotalQuantity(ammo));Assert.AreEqual(TransferReason.Busy,ItemTransferService.Deposit(player,stash,ammo,1).Reason);};
            stash.Changed+=()=>{b++;Assert.AreEqual(40,Total);Assert.AreEqual(20,player.GetTotalQuantity(ammo));};
            Assert.AreEqual(20,ItemTransferService.Deposit(player,stash,ammo,20).Moved);Assert.AreEqual(1,a);Assert.AreEqual(1,b);
        }
        [Test] public void AmmoLoopPreservesMagazineBoundaryAndConserves()
        {
            var definition=AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/LastSignal/Scripts/Runtime/Combat/WeaponDefinition_AssaultRifle.asset");
            var weapon=new WeaponRuntimeState(definition,12,player);weapon.TryEquip();weapon.Tick(1);weapon.TryCompleteEquip();
            stash.TryAdd(ammo,40);ItemTransferService.Withdraw(stash,player,ammo,20);Assert.AreEqual(12,weapon.CurrentMagazine);
            Assert.IsTrue(weapon.TryBeginReload());weapon.Tick(10);Assert.IsTrue(weapon.TryCommitReload());weapon.TryCompleteReload();
            Assert.AreEqual(30,weapon.CurrentMagazine);Assert.AreEqual(2,weapon.ReserveAmmo);
            for(int i=0;i<4;i++){weapon.Tick(1);Assert.IsTrue(weapon.TryConsumeShot());}
            ItemTransferService.Deposit(player,stash,ammo,2);Assert.AreEqual(22,stash.GetTotalQuantity(ammo));Assert.AreEqual(26,weapon.CurrentMagazine);Assert.AreEqual(0,weapon.ReserveAmmo);Assert.AreEqual(52,Total+weapon.CurrentMagazine+4);
        }
        [Test] public void DeterministicConservationSoakWithIndependentItems()
        {
            player.TryAdd(ammo,120);stash.TryAdd(ammo,120);stash.TryAdd(scrap,5);
            var random=new System.Random(8008);
            for(int i=0;i<1000;i++)
            {
                if(i%2==0)ItemTransferService.Deposit(player,stash,ammo,random.Next(1,150));else ItemTransferService.Withdraw(stash,player,ammo,random.Next(1,150));
                Assert.AreEqual(240,Total);Assert.AreEqual(5,stash.GetTotalQuantity(scrap));
                for(int s=0;s<stash.Capacity;s++){var slot=stash.GetSlot(s);Assert.That(slot.Quantity,Is.GreaterThanOrEqualTo(0));if(!slot.IsEmpty)Assert.That(slot.Quantity,Is.LessThanOrEqualTo(slot.Item.MaxStack));}
            }
        }
    }
}
