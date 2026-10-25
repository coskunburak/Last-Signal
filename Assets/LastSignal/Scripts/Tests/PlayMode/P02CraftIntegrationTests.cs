#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Persistence;
using LastSignal.Shelter;
using LastSignal.WorldTime;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public sealed class P02CraftIntegrationTests
    {
        SessionFlow flow; ShelterSite site; ShelterLoop loop; WorldClock clock; SaveSession saves;
        RecipeDefinition recipe; ItemCatalog catalog; string directory;
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/Validation/WorldPopulationAcceptance.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            flow = Object.FindAnyObjectByType<SessionFlow>(); site = flow.GetComponent<ShelterSite>(); loop = flow.GetComponent<ShelterLoop>();
            clock = flow.GetComponent<WorldClock>(); saves = flow.GetComponent<SaveSession>(); flow.Resume();
            Assert.IsNotNull(site.Production);
            recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>("Assets/LastSignal/Data/Crafting/PrototypeLogToPlank.asset");
            Assert.IsNotNull(recipe);
            catalog = Object.Instantiate(saves.Catalog);
            var items = new List<ItemDefinition>(catalog.EditorItems);
            void Add(ItemDefinition item) { if (!items.Contains(item)) items.Add(item); }
            foreach (var a in recipe.inputIngredients) Add(a.item);
            foreach (var a in recipe.outputDefinitions) Add(a.item);
            foreach (var tool in recipe.requiredTools) Add(tool);
            var data = new SerializedObject(catalog); var entries = data.FindProperty("items"); entries.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++) entries.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            var saveData = new SerializedObject(saves); saveData.FindProperty("catalog").objectReferenceValue = catalog; saveData.ApplyModifiedPropertiesWithoutUndo();
            site.multiRecipes = new[] { recipe }; site.Begin();
            Assert.IsTrue(site.ValidateCraftCatalog(catalog).Valid);
            Assert.IsTrue(site.Production.Claim()); var donor = new InventoryContainer(2); donor.TryAdd(site.material, site.moduleCost * 2);
            Assert.IsTrue(site.Production.Install(ShelterModule.Storage, donor)); Assert.IsTrue(site.Production.Install(ShelterModule.Workbench, donor));
            foreach (var enemy in Object.FindObjectsByType<ZombieHealth>(FindObjectsSortMode.None)) if (enemy.IsAlive) enemy.TakeDamage(new DamageInfo { Amount = 1000 });
            var input = recipe.inputIngredients[0];
            Assert.AreEqual(input.quantity * 2, loop.Inventory.TryAdd(input.item, input.quantity * 2));
            Assert.AreEqual(input.quantity * 2, ItemTransferService.Deposit(loop.Inventory, loop.Storage, input.item, input.quantity * 2).Moved);
            foreach (var tool in recipe.requiredTools) Assert.AreEqual(1, loop.Storage.TryAdd(tool, 1));
            directory = Path.Combine(Application.temporaryCachePath, "S005-" + Guid.NewGuid().ToString("N"));
            Open();
            int count = 1 + site.additionalRecipes.Length;
            for (int i = 0; i < count; i++) site.Act(9);
            Assert.AreSame(recipe, site.SelectedMultiRecipe);
        }
        void Open()
        {
            var socket = Array.Find(site.sockets, s => s.module == ShelterModule.Workbench);
            var capsule = flow.Player.GetComponent<CharacterController>(); capsule.enabled = false;
            // Match the grounded approach used by S010IntegrationTests. A socket's
            // display height is not the player's foot height and can overlap the cabin.
            flow.Player.transform.position = new Vector3(socket.transform.position.x + 1, .05f, socket.transform.position.z);
            capsule.enabled = true; Physics.SyncTransforms();
            Assert.IsTrue(site.Open(socket));
        }
        void AssertCheckpointPositionClear()
        {
            var capsule = flow.Player.GetComponent<CharacterController>();
            var center = flow.Player.transform.TransformPoint(capsule.center);
            float offset = Mathf.Max(0, capsule.height * .5f - capsule.radius);
            Assert.IsFalse(Physics.CheckCapsule(center + Vector3.up * offset, center - Vector3.up * offset,
                capsule.radius * .95f, ~((1 << 8) | (1 << 2)), QueryTriggerInteraction.Ignore),
                "Craft checkpoint fixture must satisfy the existing SaveSession capsule clearance contract.");
        }
        CraftRequest Request(long nonce = 1) => new CraftRequest(site.shelterId, site.Production.CraftEpoch, nonce, recipe.recipeId, recipe.definitionRevision, "shelter.storage", loop.Storage.Revision);
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (flow) flow.ReturnToMenu(); yield return null;
            if (catalog) Object.Destroy(catalog); if (directory != null && Directory.Exists(directory)) Directory.Delete(directory, true);
            Time.timeScale = 1;
        }
        [UnityTest] public IEnumerator WorkbenchUsesRealInventoryPauseAndOneCollection()
        {
            var request = Request(); Assert.AreEqual(CraftReason.Accepted, site.SubmitCraft(request).reason);
            Assert.AreEqual(recipe.inputIngredients[0].quantity, loop.Storage.GetTotalQuantity(recipe.inputIngredients[0].item));
            yield return null; yield return null;
            Assert.AreEqual(0, site.Production.Progress, "Open modal pauses canonical world time.");
            Assert.AreEqual(CraftReason.Accepted, site.SubmitCraft(request).reason);
            site.Close(); double start = clock.Simulation.Seconds; clock.Simulation.AdvanceUntil(start + recipe.duration, 0);
            Assert.AreEqual(CraftStatus.CompletedWaitingOutput, site.Production.Status); Open();
            Assert.AreEqual("Accepted", site.Act(4)); int output = loop.Storage.GetTotalQuantity(recipe.outputDefinitions[0].item);
            Assert.AreEqual(recipe.outputDefinitions[0].quantity, output); Assert.That(site.Act(4), Does.StartWith("Rejected"));
            Assert.AreEqual(output, loop.Storage.GetTotalQuantity(recipe.outputDefinitions[0].item));
        }
        [UnityTest] public IEnumerator SaveSessionRestoresEscrowAndLostResponseRetryExactlyOnce()
        {
            var request = Request(); Assert.AreEqual(CraftReason.Accepted, site.SubmitCraft(request).reason);
            long savedRevision = loop.Storage.Revision;
            site.Close(); flow.Pause(); string path = Path.Combine(directory, "running.json");
            AssertCheckpointPositionClear();
            Assert.IsTrue(saves.Save(path).Success, saves.LastResult.Message);
            flow.ReturnToMenu(); yield return null; yield return saves.Load(path);
            Assert.IsTrue(saves.LastResult.Success, saves.LastResult.Message);
            Open(); Assert.AreEqual(savedRevision, loop.Storage.Revision);
            Assert.AreEqual(CraftReason.Accepted, site.SubmitCraft(request).reason);
            Assert.AreEqual(recipe.inputIngredients[0].quantity, loop.Storage.GetTotalQuantity(recipe.inputIngredients[0].item));
            Assert.AreEqual(CraftStatus.Running, site.Production.Status);
            Assert.That(site.Act(3), Does.StartWith("Done")); Assert.AreEqual("Accepted", site.Act(4));
            Assert.AreEqual(recipe.inputIngredients[0].quantity * 2, loop.Storage.GetTotalQuantity(recipe.inputIngredients[0].item));
            var stale = new CraftRequest(site.shelterId, site.Production.CraftEpoch, 2, recipe.recipeId,
                recipe.definitionRevision, "shelter.storage", savedRevision);
            Assert.AreEqual(CraftReason.StaleSource, site.SubmitCraft(stale).reason);
            Assert.AreEqual(recipe.inputIngredients[0].quantity * 2, loop.Storage.GetTotalQuantity(recipe.inputIngredients[0].item));
        }
        [UnityTest] public IEnumerator TemporaryStationDisablePreservesEscrowAndSuspendsProgress()
        {
            Assert.AreEqual(CraftReason.Accepted, site.SubmitCraft(Request()).reason);
            site.Close(); flow.Pause(); var owner = site.Production; var before = owner.Capture();
            site.enabled = false; double start = clock.Simulation.Seconds; clock.Simulation.AdvanceUntil(start + recipe.duration, 0);
            yield return null; site.enabled = true;
            Assert.AreSame(owner, site.Production); Assert.AreEqual(0, site.Production.Progress);
            Assert.AreEqual(before.crafting.job.id, site.Production.Capture().crafting.job.id);
            Assert.AreEqual(clock.Simulation.Seconds, site.Production.Capture().lastProcessed);
            Assert.IsTrue(flow.Paused, "The fixture deliberately paused before disabling the station.");
            flow.Resume();
            Assert.IsFalse(flow.Paused);
            Open(); Assert.That(site.Act(3), Does.StartWith("Done")); Assert.AreEqual("Accepted", site.Act(4));
        }
        [UnityTest] public IEnumerator DeadActorCannotSubmitAndExistingJobKeepsItsEscrow()
        {
            Assert.AreEqual(CraftReason.Accepted, site.SubmitCraft(Request()).reason); int input = loop.Storage.GetTotalQuantity(recipe.inputIngredients[0].item);
            site.Close(); flow.Player.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo { Amount = 1000 });
            yield return null;
            Assert.IsTrue(flow.PlayerDead); Assert.AreEqual(CraftReason.Busy, site.SubmitCraft(Request(2)).reason);
            Assert.AreEqual(input, loop.Storage.GetTotalQuantity(recipe.inputIngredients[0].item)); Assert.IsTrue(site.Production.HasMultiJob);
        }
    }
}
#endif
