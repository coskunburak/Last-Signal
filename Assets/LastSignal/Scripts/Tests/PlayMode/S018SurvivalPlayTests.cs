#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Persistence;
using LastSignal.Slice;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public sealed class S018SurvivalPlayTests
    {
        S012Acceptance driver;
        SessionFlow flow;
        PlayerInventory inventory;
        PlayerSurvival survival;
        SaveSession saves;
        string directory;
        ItemDefinition Item(string id) => saves.Catalog.GetItem(new StableItemId(id));
        int Slot(ItemDefinition item)
        { for (int i = 0; i < inventory.Capacity; i++) if (inventory.GetSlot(i).Item == item) return i; return -1; }
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/Production/S013Cabin.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            driver = new GameObject("S018 test fixture").AddComponent<S012Acceptance>(); driver.Bind();
            flow = driver.flow; saves = flow.GetComponent<SaveSession>();
            yield return driver.relay.ClearThreat();
            flow.Resume(); flow.Player.GetComponent<PlayerInputReader>().SetGameplay(true);
            inventory = flow.Player.GetComponent<PlayerInventory>(); survival = flow.Player.GetComponent<PlayerSurvival>();
            Assert.IsNotNull(survival); directory = Path.Combine(Application.temporaryCachePath, "s018-test-" + Guid.NewGuid().ToString("N"));
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            if (flow) flow.ReturnToMenu(); if (driver) Object.Destroy(driver.gameObject);
            yield return null; Time.timeScale = 1;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        [UnityTest] public IEnumerator CharacterPresentationBindsLateSurvivalAndUnsubscribesWhenDisabled()
        {
            yield return new WaitForSeconds(.8f);
            var presenter=flow.Player.GetComponentInChildren<PlayerLocomotionPresenter>();
            var animator=presenter.GetComponent<Animator>();
            presenter.BindSurvival(survival);presenter.BindSurvival(survival);
            var water=Item("drink.water");inventory.TryAdd(water,3);survival.State.Advance(21600);
            Assert.That(survival.Use(Slot(water),inventory.Revision),Is.True);
            yield return null;animator.Update(.3f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(1).IsName("Consume")||animator.GetNextAnimatorStateInfo(1).IsName("Consume"),Is.True);
            presenter.enabled=false;animator.Play("Character Actions.No Action",1,0);animator.Update(0);
            survival.State.Advance(21600);
            Assert.That(survival.Use(Slot(water),inventory.Revision),Is.True);
            animator.Update(.3f);Assert.That(animator.GetCurrentAnimatorStateInfo(1).IsName("No Action"),Is.True);
            presenter.enabled=true;
            var bandage=Item("medical.bandage");inventory.TryAdd(bandage,1);
            flow.Player.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo{Amount=1,Category=DamageCategory.Melee});
            Assert.That(survival.Use(Slot(bandage),inventory.Revision),Is.True);
            presenter.Present(.1f);Assert.That(animator.GetBool("Treating"),Is.True);
            survival.CancelTreatment("Character lifecycle fixture");presenter.Present(.1f);
            Assert.That(animator.GetBool("Treating"),Is.False);
        }
        [UnityTest] public IEnumerator ConsumeIsAtomicAndStaleOrFullRequestsCostNothing()
        {
            var water = Item("drink.water"); inventory.TryAdd(water, 2); survival.State.Drink(100);
            int slot = Slot(water), before = inventory.GetTotalQuantity(water);
            Assert.IsFalse(survival.Use(slot, inventory.Revision)); Assert.AreEqual(before, inventory.GetTotalQuantity(water));
            survival.State.Advance(21600); long oldRevision = inventory.Revision;
            inventory.TryAdd(Item("medical.bandage"), 1);
            Assert.IsFalse(survival.Use(slot, oldRevision)); Assert.AreEqual(before, inventory.GetTotalQuantity(water));
            double hydration = survival.State.Hydration; bool observed = false;
            Action observer = () => { observed = true; Assert.AreEqual(before - 1, inventory.GetTotalQuantity(water)); Assert.AreEqual(hydration + 40, survival.State.Hydration); Assert.AreEqual(SaveError.Busy, saves.Capture(out _).Error); };
            inventory.InventoryChanged += observer;
            try { Assert.IsTrue(survival.Use(slot, inventory.Revision)); } finally { inventory.InventoryChanged -= observer; }
            Assert.IsTrue(observed); yield return null;
        }
        [UnityTest] public IEnumerator BandageCommitsOnceAndBlocksCombatDuringApplication()
        {
            var bandage = Item("medical.bandage"); inventory.TryAdd(bandage, 2);
            flow.Player.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo { Amount = 1, Category = DamageCategory.Melee });
            Assert.IsTrue(survival.Use(Slot(bandage), inventory.Revision)); Assert.IsFalse(survival.Use(Slot(bandage), inventory.Revision));
            Assert.AreEqual(SaveError.Busy, saves.Capture(out _).Error);
            Assert.IsFalse(flow.Player.GetComponent<PlayerCombatController>().Firearm.IsSimulationActive);
            survival.TickTreatment(2.99f); Assert.AreEqual(2, inventory.GetTotalQuantity(bandage)); Assert.Greater(survival.State.Bleeding, 0);
            survival.TickTreatment(.02f); Assert.AreEqual(1, inventory.GetTotalQuantity(bandage)); Assert.AreEqual(0, survival.State.Bleeding);
            survival.TickTreatment(10); Assert.AreEqual(1, inventory.GetTotalQuantity(bandage)); yield return null;
        }
        [UnityTest] public IEnumerator DamageAndPauseCancelBandageWithoutConsumption()
        {
            var bandage = Item("medical.bandage"); inventory.TryAdd(bandage, 1); var health = flow.Player.GetComponent<PlayerHealth>();
            health.TakeDamage(new DamageInfo { Amount = 1, Category = DamageCategory.Melee });
            Assert.IsTrue(survival.Use(Slot(bandage), inventory.Revision)); survival.TickTreatment(2.99f);
            health.TakeDamage(new DamageInfo { Amount = 1, Category = DamageCategory.Melee }); survival.TickTreatment(.02f);
            Assert.AreEqual(1, inventory.GetTotalQuantity(bandage)); Assert.IsFalse(survival.ApplyingTreatment);
            Assert.IsTrue(survival.Use(Slot(bandage), inventory.Revision)); flow.Pause(); survival.TickTreatment(0);
            Assert.IsFalse(survival.ApplyingTreatment); Assert.AreEqual(1, inventory.GetTotalQuantity(bandage)); yield return null;
        }
        [UnityTest] public IEnumerator PackOwnsOneItemAndFullShrinkRefusesWithoutLoss()
        {
            var pack = Item(PlayerSurvival.BackpackId); Assert.AreEqual(1, inventory.GetTotalQuantity(pack));
            Assert.IsTrue(flow.InventoryView.Open()); flow.InventoryView.OnSlotClicked(Slot(pack)); flow.InventoryView.UseSelected();
            Assert.AreSame(pack, survival.Backpack); Assert.AreEqual(32, inventory.Capacity); Assert.AreEqual(0, inventory.GetTotalQuantity(pack));
            Assert.AreEqual(32, flow.InventoryView.GetComponentsInChildren<LastSignal.Inventory.UI.InventorySlotUI>(false).Length);
            inventory.TryAdd(pack, 32); int count = inventory.GetTotalQuantity(pack);
            Assert.IsFalse(survival.UnequipBackpack()); Assert.AreEqual(count, inventory.GetTotalQuantity(pack)); Assert.AreEqual(32, inventory.Capacity);
            inventory.Clear(); Assert.IsTrue(survival.UnequipBackpack()); Assert.AreEqual(24, inventory.Capacity); Assert.AreEqual(1, inventory.GetTotalQuantity(pack));
            yield return null;
        }
        [UnityTest] public IEnumerator SaveReloadKeepsResourcesWoundAndEquipmentWithoutStarterDuplication()
        {
            var pack = Item(PlayerSurvival.BackpackId); Assert.IsTrue(survival.Use(Slot(pack), inventory.Revision));
            survival.State.Advance(7200); survival.State.AddWound();
            double water = survival.State.Hydration;
            var path = Path.Combine(directory, "current.json"); Assert.IsTrue(saves.Save(path).Success, saves.LastResult.Message);
            for (int i = 0; i < 2; i++)
            {
                flow.ReturnToMenu(); yield return null; yield return saves.Load(path);
                Assert.IsTrue(saves.LastResult.Success, saves.LastResult.Message);
                survival = flow.Player.GetComponent<PlayerSurvival>(); inventory = flow.Player.GetComponent<PlayerInventory>();
                Assert.AreEqual(water, survival.State.Hydration); Assert.AreEqual(1, survival.State.Bleeding);
                Assert.AreEqual(pack, survival.Backpack); Assert.AreEqual(32, inventory.Capacity); Assert.AreEqual(0, inventory.GetTotalQuantity(pack));
            }
        }
        [UnityTest] public IEnumerator ChosenStackIsConsumedAndNutritionUsesItsOwnResource()
        {
            var water = Item("drink.water"); inventory.TryAdd(water, 4);
            int first = Slot(water), second = -1;
            for (int i = first + 1; i < inventory.Capacity; i++) if (inventory.GetSlot(i).Item == water) { second = i; break; }
            Assert.GreaterOrEqual(second, 0); survival.State.Advance(21600);
            Assert.IsTrue(survival.Use(second, inventory.Revision)); Assert.AreEqual(3, inventory.GetSlot(first).Quantity); Assert.IsTrue(inventory.GetSlot(second).IsEmpty);
            var food = Item("food.canned"); inventory.TryAdd(food, 1); double hydration = survival.State.Hydration;
            Assert.IsTrue(survival.Use(Slot(food), inventory.Revision)); Assert.AreEqual(100, survival.State.Nutrition); Assert.AreEqual(hydration, survival.State.Hydration);
            yield return null;
        }
        [UnityTest] public IEnumerator PauseFreezesWorldResourcesAndStaleTreatmentCannotCommit()
        {
            var clock = flow.GetComponent<LastSignal.WorldTime.WorldClock>(); double time = clock.Simulation.Seconds;
            double water = survival.State.Hydration; flow.Pause(); yield return null; yield return null;
            Assert.AreEqual(time, clock.Simulation.Seconds); Assert.AreEqual(water, survival.State.Hydration);
            flow.Resume(); var bandage = Item("medical.bandage"); inventory.TryAdd(bandage, 1);
            flow.Player.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo { Amount = 1, Category = DamageCategory.Melee });
            Assert.IsTrue(survival.Use(Slot(bandage), inventory.Revision));
            inventory.TryAdd(Item("drink.water"), 1); survival.TickTreatment(3);
            Assert.IsFalse(survival.ApplyingTreatment); Assert.AreEqual(1, inventory.GetTotalQuantity(bandage)); Assert.Greater(survival.State.Bleeding, 0);
        }
        [UnityTest] public IEnumerator DeadPlayerCannotConsumeOrEquip()
        {
            inventory.TryAdd(Item("drink.water"), 1); survival.State.Advance(7200);
            flow.Player.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo { Amount = 10000 });
            Assert.IsFalse(survival.Use(Slot(Item("drink.water")), inventory.Revision));
            Assert.IsFalse(survival.Use(Slot(Item(PlayerSurvival.BackpackId)), inventory.Revision));
            Assert.AreEqual(1, inventory.GetTotalQuantity(Item("drink.water"))); yield return null;
        }
    }
}
#endif
