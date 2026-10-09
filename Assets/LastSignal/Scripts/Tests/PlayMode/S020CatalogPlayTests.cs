#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using LastSignal.Loot;
using LastSignal.Shelter;
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
    public sealed class S020CatalogPlayTests
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
            driver = new GameObject("S020 test fixture").AddComponent<S012Acceptance>(); driver.Bind();
            flow = driver.flow; saves = flow.GetComponent<SaveSession>();
            yield return driver.relay.ClearThreat();
            flow.Resume(); flow.Player.GetComponent<PlayerInputReader>().SetGameplay(true);
            inventory = flow.Player.GetComponent<PlayerInventory>(); survival = flow.Player.GetComponent<PlayerSurvival>();
            Assert.IsNotNull(survival); directory = Path.Combine(Application.temporaryCachePath, "s020-test-" + Guid.NewGuid().ToString("N"));
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            if (flow) flow.ReturnToMenu(); if (driver) Object.Destroy(driver.gameObject);
            yield return null; Time.timeScale = 1;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        [UnityTest] public IEnumerator AuthoredEssentialsActuallySpawnAndConsumedSourceDoesNotRegenerate()
        {
            var loot = flow.GetComponent<LootPopulationService>();
            foreach (var id in new[] { "s006.kitchen.0", "s006.clinic.0", "s006.workshop.4" })
            {
                var result = loot.Results.Single(r => r.PointId == id);
                Assert.AreEqual(LootOutcome.Spawned, result.Selection.Outcome, result.Diagnostic);
            }
            const string consumed = "loot:s006.kitchen.0";
            var water = Object.FindObjectsByType<WorldItem>(FindObjectsSortMode.None).Single(w => w.PersistentId == consumed);
            Assert.IsTrue(water.TryInteract()); yield return null;
            int before = inventory.GetTotalQuantity(Item("drink.water"));
            string path = Path.Combine(directory, "essentials.json"); Assert.IsTrue(saves.Save(path).Success, saves.LastResult.Message);
            flow.ReturnToMenu(); yield return null; yield return saves.Load(path);
            Assert.IsTrue(saves.LastResult.Success, saves.LastResult.Message);
            Assert.IsFalse(Object.FindObjectsByType<WorldItem>(FindObjectsSortMode.None).Any(w => w.PersistentId == consumed));
            Assert.AreEqual(before, flow.Player.GetComponent<PlayerInventory>().GetTotalQuantity(Item("drink.water")));
        }
        [UnityTest] public IEnumerator SceneWorkbenchSelectorStartsCancelsAndRefundsNewRecipe()
        {
            var site = flow.GetComponent<ShelterSite>(); var loop = flow.GetComponent<ShelterLoop>();
            Assert.IsTrue(site.Production.Claim()); var modules = new InventoryContainer(2); modules.TryAdd(site.material, 4);
            Assert.IsTrue(site.Production.Install(ShelterModule.Storage, modules));
            Assert.IsTrue(site.Production.Install(ShelterModule.Workbench, modules));
            loop.Storage.TryAdd(Item("material.cloth"), 4);
            var controller = flow.Player.GetComponent<CharacterController>(); controller.enabled = false;
            flow.Player.transform.position = site.sockets[2].transform.position + Vector3.right;
            controller.enabled = true; Physics.SyncTransforms();
            Assert.IsTrue(site.Open(site.sockets[2]));
            Assert.That(site.Act(9), Does.StartWith("Selected:")); Assert.AreEqual("recipe.bench.rag", site.SelectedRecipe.recipeId);
            Assert.That(site.Act(2), Does.StartWith("Done")); Assert.AreEqual(2, loop.Storage.GetTotalQuantity(Item("material.cloth")));
            Assert.That(site.Act(9), Does.Contain("current job"));
            Assert.That(site.Act(3), Does.StartWith("Done")); Assert.That(site.Act(4), Does.StartWith("Done"));
            Assert.That(site.Act(4), Does.StartWith("Rejected")); Assert.AreEqual(4, loop.Storage.GetTotalQuantity(Item("material.cloth")));
            site.Close(); yield return null;
        }
        [UnityTest] public IEnumerator NewFoodAndDrinkUseAuthoredEffectsAndSelectedStackOnce()
        {
            var food = Item("food.crackers"); var soda = Item("drink.soda");
            inventory.TryAdd(food, 5); inventory.TryAdd(soda, 1); survival.State.Advance(43200);
            double water = survival.State.Hydration, nutrition = survival.State.Nutrition;
            Assert.IsTrue(survival.Use(Slot(food), inventory.Revision));
            Assert.AreEqual(4, inventory.GetTotalQuantity(food)); Assert.AreEqual(nutrition + 20, survival.State.Nutrition);
            Assert.AreEqual(water, survival.State.Hydration);
            Assert.IsTrue(survival.Use(Slot(soda), inventory.Revision));
            Assert.AreEqual(0, inventory.GetTotalQuantity(soda)); Assert.AreEqual(water + 25, survival.State.Hydration);
            Assert.AreEqual(nutrition + 20, survival.State.Nutrition); yield return null;
        }
        [UnityTest] public IEnumerator RagTreatsOneWoundAndPressureDressingTreatsRemainingWithoutHealing()
        {
            var rag = Item("medical.rag"); var dressing = Item("medical.pressure-dressing");
            inventory.TryAdd(rag, 1); inventory.TryAdd(dressing, 1);
            Assert.IsFalse(survival.Use(Slot(rag), inventory.Revision)); Assert.AreEqual(1, inventory.GetTotalQuantity(rag));
            survival.State.AddWound(); survival.State.AddWound(); survival.State.AddWound();
            float hp = flow.Player.GetComponent<PlayerHealth>().CurrentHealth;
            Assert.IsTrue(survival.Use(Slot(rag), inventory.Revision)); survival.TickTreatment(4.99f);
            Assert.AreEqual(3, survival.State.Bleeding); survival.TickTreatment(.02f);
            Assert.AreEqual(2, survival.State.Bleeding); Assert.AreEqual(0, inventory.GetTotalQuantity(rag));
            Assert.IsTrue(survival.Use(Slot(dressing), inventory.Revision)); survival.TickTreatment(1);
            Assert.AreEqual(0, survival.State.Bleeding); Assert.AreEqual(0, inventory.GetTotalQuantity(dressing));
            Assert.AreEqual(hp, flow.Player.GetComponent<PlayerHealth>().CurrentHealth); yield return null;
        }
        [UnityTest] public IEnumerator InterruptedNewTreatmentLeavesWoundsAndInventoryIntact()
        {
            var dressing = Item("medical.pressure-dressing"); inventory.TryAdd(dressing, 1); survival.State.AddWound();
            Assert.IsTrue(survival.Use(Slot(dressing), inventory.Revision)); survival.TickTreatment(.5f);
            flow.Pause(); survival.TickTreatment(1);
            Assert.IsFalse(survival.ApplyingTreatment); Assert.AreEqual(1, inventory.GetTotalQuantity(dressing));
            Assert.AreEqual(1, survival.State.Bleeding); yield return null;
        }
        [UnityTest] public IEnumerator RepeatedPackSwapAndFullDowngradePreserveHealthAndOwnership()
        {
            var small = Item(PlayerSurvival.BackpackId); var large = Item("equipment.expedition-pack");
            inventory.TryAdd(large, 1); float hp = flow.Player.GetComponent<PlayerHealth>().CurrentHealth;
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(survival.Use(Slot(large), inventory.Revision)); Assert.AreEqual(survival.BaseCapacity + 12, inventory.Capacity);
                Assert.AreEqual(0, inventory.GetTotalQuantity(large)); Assert.AreEqual(1, inventory.GetTotalQuantity(small));
                Assert.IsTrue(survival.Use(Slot(small), inventory.Revision)); Assert.AreEqual(survival.BaseCapacity + 8, inventory.Capacity);
                Assert.AreEqual(1, inventory.GetTotalQuantity(large)); Assert.AreEqual(0, inventory.GetTotalQuantity(small));
            }
            Assert.IsTrue(survival.Use(Slot(large), inventory.Revision)); inventory.TryAdd(Item("tool.wrench"), 256);
            int tools = inventory.GetTotalQuantity(Item("tool.wrench")); long revision = inventory.Revision;
            Assert.IsFalse(survival.Use(Slot(small), revision)); Assert.IsFalse(survival.UnequipBackpack());
            Assert.AreEqual(revision, inventory.Revision); Assert.AreSame(large, survival.Backpack);
            Assert.AreEqual(tools, inventory.GetTotalQuantity(Item("tool.wrench"))); Assert.AreEqual(1, inventory.GetTotalQuantity(small));
            Assert.AreEqual(hp, flow.Player.GetComponent<PlayerHealth>().CurrentHealth); yield return null;
        }
        [UnityTest] public IEnumerator LargePackAppliesAndRemovesActualRecoveryCost()
        {
            var pack = Item("equipment.expedition-pack"); inventory.TryAdd(pack, 1);
            var stamina = flow.Player.GetComponent<PlayerStamina>();
            Assert.IsTrue(survival.Use(Slot(pack), inventory.Revision));
            stamina.Restore(20, false, 0); stamina.Tick(1, false);
            Assert.AreEqual(20 + PlayerStamina.RegenRate * .85f, stamina.CurrentStamina, .001f);
            Assert.IsTrue(survival.UnequipBackpack()); stamina.Restore(20, false, 0); stamina.Tick(1, false);
            Assert.AreEqual(20 + PlayerStamina.RegenRate, stamina.CurrentStamina, .001f); yield return null;
        }
        [UnityTest] public IEnumerator NewEquipmentAndItemQuantitiesSurviveRepeatedSessionLoad()
        {
            var pack = Item("equipment.expedition-pack"); inventory.TryAdd(pack, 1);
            Assert.IsTrue(survival.Use(Slot(pack), inventory.Revision)); inventory.TryAdd(Item("food.crackers"), 7);
            var path = Path.Combine(directory, "catalog.json"); Assert.IsTrue(saves.Save(path).Success, saves.LastResult.Message);
            for (int i = 0; i < 2; i++)
            {
                flow.ReturnToMenu(); yield return null; yield return saves.Load(path);
                Assert.IsTrue(saves.LastResult.Success, saves.LastResult.Message);
                inventory = flow.Player.GetComponent<PlayerInventory>(); survival = flow.Player.GetComponent<PlayerSurvival>();
                Assert.AreSame(pack, survival.Backpack); Assert.AreEqual(survival.BaseCapacity + 12, inventory.Capacity);
                Assert.AreEqual(7, inventory.GetTotalQuantity(Item("food.crackers"))); Assert.AreEqual(0, inventory.GetTotalQuantity(pack));
                Assert.AreEqual(1, inventory.GetTotalQuantity(Item(PlayerSurvival.BackpackId)));
            }
        }
        [UnityTest] public IEnumerator ProductionSceneExposesNewRecipesAndNewWorldPickupUsesRealInventory()
        {
            var site = flow.GetComponent<LastSignal.Shelter.ShelterSite>();
            Assert.AreEqual(3, site.additionalRecipes.Length); Assert.IsTrue(site.Validate());
            var definition = Item("medical.pressure-dressing");
            var world = Object.Instantiate(definition.WorldPrefab).GetComponent<WorldItem>();
            try
            {
                int before = inventory.GetTotalQuantity(definition); Assert.IsTrue(world.TryInteract());
                Assert.AreEqual(before + 1, inventory.GetTotalQuantity(definition));
                Assert.IsFalse(world.TryInteract()); Assert.AreEqual(before + 1, inventory.GetTotalQuantity(definition));
                Assert.IsTrue(flow.InventoryView.Open()); flow.InventoryView.OnSlotClicked(Slot(definition));
                Assert.That(flow.InventoryView.FeedbackMessage, Does.Contain(definition.Description));
                Assert.That(flow.InventoryView.FeedbackMessage, Does.Contain("90 g/adet"));
            }
            finally { if (world) Object.Destroy(world.gameObject); }
            yield return null;
        }
    }
}
#endif
