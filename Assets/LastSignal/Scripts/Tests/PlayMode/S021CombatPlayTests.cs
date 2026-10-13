#if UNITY_EDITOR
using System.Collections;
using System.IO;
using LastSignal.Inventory;
using LastSignal.Persistence;
using LastSignal.Slice;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    public sealed class S021CombatPlayTests
    {
        GameObject player;
        PlayerCombatController combat;
        PlayerInputReader input;
        InputFixtureIsolation.SceneScope sceneScope;
        InputFixtureIsolation.NativeInputScope inputScope;

        [SetUp] public void Setup()
        {
            sceneScope = new InputFixtureIsolation.SceneScope();
            InputFixtureIsolation.DisableLiveActions();
            inputScope = new InputFixtureIsolation.NativeInputScope();
            Time.timeScale = 1;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Player/Player.prefab");
            Assert.That(prefab, Is.Not.Null);
            player = Object.Instantiate(prefab, new Vector3(500, 100, 500), Quaternion.identity);
            player.GetComponent<FirstPersonMotor>().enabled = false;
            // SessionFlow adds the inventory at spawn; standalone prefab fixtures must do the same.
            var inventory = player.GetComponent<PlayerInventory>();
            if (!inventory) inventory = player.AddComponent<PlayerInventory>();
            inventory.Initialize(24);
            input = player.GetComponent<PlayerInputReader>();
            input.SetGameplay(true);
            combat = player.GetComponent<PlayerCombatController>();
        }
        [TearDown] public void TearDown()
        {
            if (player) Object.DestroyImmediate(player);
            Time.timeScale = 1;
            InputFixtureIsolation.DisableLiveActions();
            try { inputScope?.Dispose(); inputScope = null; } finally { sceneScope?.Dispose(); sceneScope = null; }
        }
        IEnumerator WaitReady(WeaponController weapon)
        {
            float deadline = Time.realtimeSinceStartup + 5;
            while (weapon.RuntimeState.State != WeaponState.Ready)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Weapon never reached Ready.");
                yield return null;
            }
        }

        [UnityTest] public IEnumerator ActualPlayerCanSwitchAllFiveWeaponsWithoutLosingMagazines()
        {
            yield return null;
            Assert.That(combat.Firearms.Count, Is.EqualTo(2));
            Assert.That(combat.MeleeWeapons.Count, Is.EqualTo(3));
            var rifle = combat.Rifle;
            var pistol = combat.FindFirearm("weapon.pistol");
            Assert.That(rifle && pistol, Is.True);
            int rifleMagazine = rifle.RuntimeState.CurrentMagazine;
            Assert.That(combat.SelectFirearm("weapon.pistol"), Is.True);
            yield return WaitReady(pistol);
            Assert.That(combat.ActiveWeapon, Is.SameAs(pistol));
            Assert.That(combat.SelectMeleeWeapon(MeleeWeaponDefinition.BladeId), Is.True);
            Assert.That(combat.Melee.Definition.WeaponId, Is.EqualTo(MeleeWeaponDefinition.BladeId));
            Assert.That(combat.SelectMeleeWeapon(MeleeWeaponDefinition.AxeId), Is.True);
            Assert.That(combat.Melee.Definition.Heavy, Is.True);
            Assert.That(combat.SelectFirearm("weapon.rifle"), Is.True);
            yield return WaitReady(rifle);
            Assert.That(rifle.RuntimeState.CurrentMagazine, Is.EqualTo(rifleMagazine));
            Assert.That(combat.SelectMeleeWeapon(MeleeWeaponDefinition.CrowbarId), Is.True);
            Assert.That(combat.Melee.Definition.WeaponId, Is.EqualTo(MeleeWeaponDefinition.CrowbarId));
        }

        [UnityTest] public IEnumerator PistolFiresOnlyItsOwnRoundAndSwitchInterruptsReload()
        {
            yield return null;
            var rifle = combat.Rifle;
            var pistol = combat.FindFirearm("weapon.pistol");
            Assert.That(combat.SelectFirearm("weapon.pistol"), Is.True);
            yield return WaitReady(pistol);
            int rifleMagazine = rifle.RuntimeState.CurrentMagazine;
            int pistolMagazine = pistol.RuntimeState.CurrentMagazine;
            int accepted = 0;
            pistol.ShotFired += _ => accepted++;
            pistol.OnFirePressed(); pistol.OnFireReleased();
            Assert.That(accepted, Is.EqualTo(1));
            Assert.That(pistol.RuntimeState.CurrentMagazine, Is.EqualTo(pistolMagazine - 1));
            Assert.That(rifle.RuntimeState.CurrentMagazine, Is.EqualTo(rifleMagazine));
            var inventory = player.GetComponent<PlayerInventory>();
            Assert.That(inventory.TryAdd(pistol.Definition.Ammunition, 3), Is.EqualTo(3));
            pistol.OnReloadRequested();
            Assert.That(pistol.RuntimeState.State, Is.EqualTo(WeaponState.Reloading));
            Assert.That(combat.SelectFirearm("weapon.rifle"), Is.True);
            Assert.That(pistol.RuntimeState.CurrentMagazine, Is.EqualTo(pistolMagazine - 1));
            Assert.That(inventory.GetTotalQuantity(pistol.Definition.Ammunition), Is.EqualTo(3));
            yield return WaitReady(rifle);
            Assert.That(combat.SelectFirearm("weapon.pistol"), Is.True);
            yield return WaitReady(pistol);
            pistol.OnReloadRequested();
            input.SetGameplay(false);
            yield return null;
            float timer = pistol.RuntimeState.StateTimer;
            yield return new WaitForSeconds(.2f);
            Assert.That(pistol.RuntimeState.StateTimer, Is.EqualTo(timer).Within(.001f));
            input.SetGameplay(true);
        }

        [UnityTest] public IEnumerator MeleeAttackUsesActualViewmodelAndSingleActiveWindow()
        {
            yield return null;
            Assert.That(combat.SelectMeleeWeapon(MeleeWeaponDefinition.BladeId), Is.True);
            var blade = combat.Melee;
            Assert.That(blade.HasValidAuthoring, Is.True);
            Assert.That(blade.TryAttack(), Is.True);
            Assert.That(blade.TryAttack(), Is.False);
            float cost = blade.Definition.StaminaCost;
            Assert.That(player.GetComponent<PlayerStamina>().CurrentStamina, Is.EqualTo(PlayerStamina.Maximum - cost).Within(.01f));
            blade.Cancel();
            Assert.That(blade.Simulation.State, Is.EqualTo(MeleeState.Ready));
        }

        [UnityTest] public IEnumerator WorldRigProfilesAndAudioBindingsMatchSelectedWeapons()
        {
            yield return null;
            var body = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Art/CharacterIntegration/Diesel_WorldBody.prefab");
            Assert.That(body, Is.Not.Null);
            var rig = body.GetComponent<CharacterWeaponRig>();
            Assert.That(rig, Is.Not.Null);
            Assert.That(rig.Profiles, Has.Length.EqualTo(5));
            foreach (var profile in rig.Profiles)
            {
                Assert.That(profile && profile.WorldPrefab, Is.True);
                Assert.That(profile.Firearm || profile.Melee, Is.True);
            }
            var pistol = combat.FindFirearm("weapon.pistol");
            Assert.That(pistol.GetComponent<WeaponAudioPresenter>(), Is.Not.Null);
            Assert.That(combat.FindMelee(MeleeWeaponDefinition.BladeId).GetComponent<MeleeWeaponPresenter>(), Is.Not.Null);
            Assert.That(combat.FindMelee(MeleeWeaponDefinition.AxeId).GetComponent<MeleeWeaponPresenter>(), Is.Not.Null);
        }

        [UnityTest] public IEnumerator MultiColliderZombieReceivesOneCommittedMeleeHit()
        {
            yield return null;
            var eye = player.GetComponent<FirstPersonLook>().View.transform;
            var target = new GameObject("S021 zombie target");
            try
            {
                target.transform.position = eye.position + eye.forward * 1.05f;
                var health = target.AddComponent<ZombieHealth>();
                for (int i = 0; i < 2; i++)
                {
                    var part = new GameObject("hit region " + i);
                    part.transform.SetParent(target.transform, false);
                    part.transform.localPosition = new Vector3(i == 0 ? -.15f : .15f, 0, 0);
                    var collider = part.AddComponent<BoxCollider>();
                    collider.size = new Vector3(.25f, .5f, .25f);
                    part.AddComponent<ZombieHitRegion>().Configure(health, DamageRegion.Body, 1, collider);
                }
                Physics.SyncTransforms();
                var resolver = new MeleeAttackResolver();
                var blade = combat.FindMelee(MeleeWeaponDefinition.BladeId).Definition;
                Assert.That(resolver.Resolve(eye, eye, blade, player, 83001), Is.True);
                Assert.That(health.DamageTransactions, Is.EqualTo(1));
                Assert.That(resolver.Resolve(eye, eye, blade, player, 83001), Is.False);
                Assert.That(health.DamageTransactions, Is.EqualTo(1));
                health.SetDamageEnabled(false);
                Assert.That(resolver.Resolve(eye, eye, blade, player, 83002), Is.False);
                Assert.That(health.DamageTransactions, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(target); }
        }

        [UnityTest] public IEnumerator ActualSessionRoundTripsTwoWeaponsAndSelectionWithoutAmmoGrowth()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/LastSignal/Scenes/Production/S013Cabin.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var driver = new GameObject("S021 save fixture").AddComponent<S012Acceptance>();
            driver.Bind();
            var flow = driver.flow;
            var saves = flow.GetComponent<SaveSession>();
            string directory = Path.Combine(Application.temporaryCachePath, "s021-save-" + System.Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory, "weapons.json");
            try
            {
                yield return driver.relay.ClearThreat();
                flow.Resume();
                flow.Player.GetComponent<PlayerInputReader>().SetGameplay(true);
                var owner = flow.Player.GetComponent<PlayerCombatController>();
                Assert.That(owner.SelectFirearm("weapon.pistol"), Is.True);
                var pistol = owner.Firearm;
                yield return WaitReady(pistol);
                pistol.RuntimeState.TryConsumeShot();
                var inv = flow.Player.GetComponent<PlayerInventory>();
                Assert.That(inv.TryAdd(pistol.Definition.Ammunition, 6), Is.EqualTo(6));
                int magazine = pistol.RuntimeState.CurrentMagazine;
                int reserve = inv.GetTotalQuantity(pistol.Definition.Ammunition);
                Assert.That(owner.SelectMeleeWeapon(MeleeWeaponDefinition.AxeId), Is.True);
                Assert.That(saves.Save(path).Success, Is.True, saves.LastResult.Message);
                for (int i = 0; i < 2; i++)
                {
                    flow.ReturnToMenu(); yield return null;
                    yield return saves.Load(path);
                    Assert.That(saves.LastResult.Success, Is.True, saves.LastResult.Message);
                    owner = flow.Player.GetComponent<PlayerCombatController>();
                    inv = flow.Player.GetComponent<PlayerInventory>();
                    Assert.That(owner.SelectedSlot, Is.EqualTo(PlayerCombatController.CombatSlot.Melee));
                    Assert.That(owner.Melee.Definition.WeaponId, Is.EqualTo(MeleeWeaponDefinition.AxeId));
                    Assert.That(owner.Firearm.Definition.WeaponId, Is.EqualTo("weapon.pistol"));
                    Assert.That(owner.Firearm.RuntimeState.CurrentMagazine, Is.EqualTo(magazine));
                    Assert.That(inv.GetTotalQuantity(owner.Firearm.Definition.Ammunition), Is.EqualTo(reserve));
                }
            }
            finally
            {
                if (flow) flow.ReturnToMenu();
                if (driver) Object.DestroyImmediate(driver.gameObject);
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
#endif
