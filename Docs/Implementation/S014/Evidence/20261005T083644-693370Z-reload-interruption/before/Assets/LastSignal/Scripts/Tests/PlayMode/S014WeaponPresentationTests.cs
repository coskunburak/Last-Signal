#if UNITY_EDITOR
using System.Collections;
using LastSignal.Inventory;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    public sealed class S014WeaponPresentationTests : InputTestFixture
    {
        GameObject player;
        PlayerInputReader input;
        PlayerCombatController combat;
        WeaponController weapon;
        Animator animator;

        public override void Setup()
        {
            InputFixtureIsolation.DisableLiveActions();
            base.Setup();
            Time.timeScale = 1;
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/LastSignal/Prefabs/Player/Player.prefab"), new Vector3(500, 100, 500), Quaternion.identity);
            player.GetComponent<FirstPersonMotor>().enabled = false;
            var inventory = player.GetComponent<PlayerInventory>();
            if (!inventory) inventory = player.AddComponent<PlayerInventory>();
            inventory.Initialize(24);
            input = player.GetComponent<PlayerInputReader>();
            input.SetGameplay(true);
            combat = player.GetComponent<PlayerCombatController>();
        }

        public override void TearDown()
        {
            if (player) Object.DestroyImmediate(player);
            Time.timeScale = 1;
            InputFixtureIsolation.DisableLiveActions();
            base.TearDown();
        }

        IEnumerator PrepareReload(bool empty)
        {
            yield return null;
            weapon = combat.Firearm;
            Assert.That(weapon, Is.Not.Null);
            animator = weapon.GetComponentInChildren<Animator>(true);
            yield return WaitReady();
            var inventory = player.GetComponent<PlayerInventory>();
            Assert.That(inventory.TryAdd(weapon.Definition.Ammunition, 60), Is.EqualTo(60));
            // Seed the authoritative state without rays/noise, then use the real controller reload.
            int shots = empty ? weapon.RuntimeState.CurrentMagazine : 1;
            for (int i = 0; i < shots; i++)
            {
                weapon.RuntimeState.Tick(weapon.Definition.FireCooldownSeconds + .01f);
                Assert.That(weapon.RuntimeState.TryConsumeShot(), Is.True);
            }
            weapon.OnReloadRequested();
            Assert.That(weapon.RuntimeState.State, Is.EqualTo(WeaponState.Reloading));
            yield return new WaitForSeconds(.15f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName(empty ? "EmptyReload" : "Reload"), Is.True);
        }

        IEnumerator WaitReady()
        {
            float deadline = Time.realtimeSinceStartup + 5;
            while (weapon.RuntimeState.State != WeaponState.Ready ||
                !animator.GetCurrentAnimatorStateInfo(0).IsName("Ready") || animator.IsInTransition(0))
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Production rifle did not return to Ready.");
                yield return null;
            }
        }

        [UnityTest] public IEnumerator TacticalReloadPausesWithGameplayInput() => PausedReload(false);
        [UnityTest] public IEnumerator EmptyReloadPausesWithGameplayInput() => PausedReload(true);

        IEnumerator PausedReload(bool empty)
        {
            yield return PrepareReload(empty);
            input.SetGameplay(false); // A modal lock without pausing the world's time.
            yield return null;
            float timer = weapon.RuntimeState.StateTimer;
            float pose = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            int total = weapon.RuntimeState.TotalAmmo;
            int magazine = weapon.RuntimeState.CurrentMagazine;
            yield return new WaitForSeconds(.3f);
            Assert.That(weapon.RuntimeState.StateTimer, Is.EqualTo(timer).Within(.001f));
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.EqualTo(pose).Within(.001f),
                "Reload presentation must not finish ahead of its frozen ammo transaction.");
            Assert.That(weapon.RuntimeState.CurrentMagazine, Is.EqualTo(magazine));
            Assert.That(weapon.RuntimeState.TotalAmmo, Is.EqualTo(total));
            input.SetGameplay(true);
            yield return WaitReady();
            Assert.That(weapon.RuntimeState.CurrentMagazine, Is.EqualTo(weapon.Definition.MagazineCapacity));
            Assert.That(weapon.RuntimeState.TotalAmmo, Is.EqualTo(total));
        }

        [UnityTest] public IEnumerator SwitchingBeforeReloadCommitClearsPendingPresentation()
        {
            yield return PrepareReload(false);
            int total = weapon.RuntimeState.TotalAmmo;
            int magazine = weapon.RuntimeState.CurrentMagazine;
            Assert.That(weapon.RuntimeState.ReloadCommitted, Is.False);
            Assert.That(combat.SelectSlot(PlayerCombatController.CombatSlot.Melee), Is.True);
            Assert.That(weapon.RuntimeState.CurrentMagazine, Is.EqualTo(magazine));
            Assert.That(combat.SelectSlot(PlayerCombatController.CombatSlot.Firearm), Is.True);
            yield return WaitReady();
            yield return new WaitForSeconds(.15f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Ready"), Is.True);
            Assert.That(weapon.RuntimeState.CurrentMagazine, Is.EqualTo(magazine));
            Assert.That(weapon.RuntimeState.TotalAmmo, Is.EqualTo(total));
        }
    }
}
#endif
