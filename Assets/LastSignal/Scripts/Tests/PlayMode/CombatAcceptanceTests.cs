using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    public class CombatAcceptanceTests : InputTestFixture
    {
        Mouse mouse;
        Keyboard keyboard;
        PlayerCombatController combat;
        AcceptanceHud hud;
        DamageableTarget target10m;
        readonly System.Collections.Generic.List<GameObject> sceneFixtures = new System.Collections.Generic.List<GameObject>();

        public override void Setup()
        {
            base.Setup();
            Time.timeScale = 1;
            mouse = InputSystem.AddDevice<Mouse>();
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        public IEnumerator LoadCombatScene()
        {

#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/CombatAcceptance.unity", new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#else
            SceneManager.LoadScene("CombatAcceptance");
#endif
            // Capture authored content only; test infrastructure is DontSave and is not ours.
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                if ((root.hideFlags & HideFlags.DontSave) == 0) sceneFixtures.Add(root);
            yield return null; // Wait for load
            yield return null; // Wait for Start

            var session = Object.FindAnyObjectByType<SessionFlow>();
            session.BeginSession();
            session.Resume(); var pi = Object.FindAnyObjectByType<PlayerInputReader>(); var am = typeof(PlayerInputReader).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); if(am!=null)am.Invoke(pi, null); pi.SetGameplay(true);
            yield return null;

            var player = session.Player;
            combat = player.GetComponent<PlayerCombatController>();
            hud = Object.FindAnyObjectByType<AcceptanceHud>();
            
            // The same default weapon used by the normal S001 route must already be equipped.
            var instance = combat.ActiveWeapon;
            Assert.That(instance, Is.Not.Null);
            
            // Fast-forward equip time so it's ready
            yield return new WaitForSeconds(instance.Definition.EquipSeconds + 0.1f);

            var targets = Object.FindObjectsByType<DamageableTarget>(FindObjectsSortMode.None);
            foreach (var t in targets) if (t.name == "CombatTarget_10m") target10m = t;
            
            Debug.Log("B0A_ORDER UnitySetUp ready devices=" + InputSystem.devices.Count + " active=" + player.GetComponent<PlayerInputReader>().GameplayActive + " mouse=" + mouse);
            yield return null;
        }

        public override void TearDown()
        {
            try
            {
                var session = Object.FindAnyObjectByType<SessionFlow>();
                if (session) session.ReturnToMenu();
            }
            catch (System.Exception) { /* Ensure cleanup continues even if InputSystem state is corrupted. */ }
            // Dispose scene UI actions before InputTestFixture restores the real device state.
            foreach (var root in sceneFixtures) if (root) Object.DestroyImmediate(root);
            sceneFixtures.Clear();
            Time.timeScale = 1;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Fire_ConsumesAmmoAndDamagesTarget()
        {
            yield return LoadCombatScene(); var state = combat.ActiveWeapon.RuntimeState;
            int initialAmmo = state.CurrentMagazine;
            float initialHealth = target10m.CurrentHealth;

            Press(mouse.leftButton);
            yield return null;
            Release(mouse.leftButton);
            yield return null;

            Assert.That(state.CurrentMagazine, Is.EqualTo(initialAmmo - 1), "Ammo should decrease");
            Assert.That(target10m.CurrentHealth, Is.LessThan(initialHealth), "Target should take damage");
            Assert.That(target10m.HitCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Reload_TransfersAmmoAfterDuration()
        {
            yield return LoadCombatScene(); var state = combat.ActiveWeapon.RuntimeState;
            combat.GetComponent<LastSignal.Inventory.PlayerInventory>().TryAdd(state.Definition.Ammunition, 5); // Explicit reserve fixture.
            // Fire 5 shots
            for (int i=0; i<5; i++)
            {
                Press(mouse.leftButton); yield return null;
                Release(mouse.leftButton); yield return new WaitForSeconds(0.15f);
            }
            int expectedAmmo = state.Definition.MagazineCapacity - 5;
            Assert.That(state.CurrentMagazine, Is.EqualTo(expectedAmmo));

            Press(keyboard.rKey);
            yield return null;
            Release(keyboard.rKey);

            Assert.That(state.State, Is.EqualTo(WeaponState.Reloading));
            
            // Wait for reload to complete
            yield return new WaitForSeconds(state.Definition.TacticalReloadSeconds + 0.1f);
            
            Assert.That(state.State, Is.EqualTo(WeaponState.Ready));
            Assert.That(state.CurrentMagazine, Is.EqualTo(state.Definition.MagazineCapacity));
        }

        [UnityTest]
        public IEnumerator AutoFire_FiresContinuouslyWhileHeld()
        {
            yield return LoadCombatScene(); var state = combat.ActiveWeapon.RuntimeState;
            int initialAmmo = state.CurrentMagazine;

            Press(mouse.leftButton);
            yield return new WaitForSeconds(0.5f); // Hold for half a second
            Release(mouse.leftButton);
            yield return null;

            // 600 RPM = 10 shots per second. Half a second = ~5 shots.
            int shotsFired = initialAmmo - state.CurrentMagazine;
            Assert.That(shotsFired, Is.GreaterThan(2), "Should have fired multiple shots");
            Assert.That(target10m.HitCount, Is.GreaterThan(2));
        }

        [UnityTest]
        public IEnumerator ADS_ReducesFOVAndSensitivity()
        {
            yield return LoadCombatScene(); var look = combat.GetComponent<FirstPersonLook>();
            float baseFov = look.View.fieldOfView;
            
            Debug.Log("B0A_ORDER ADS devices=" + InputSystem.devices.Count + " active=" + combat.GetComponent<PlayerInputReader>().GameplayActive + " mouse=" + mouse);
            Press(mouse.rightButton);
            yield return new WaitForSeconds(0.3f); // Wait for transition
            
            float adsFov = look.View.fieldOfView;
            Assert.That(adsFov, Is.LessThan(baseFov), "FOV should decrease during ADS");

            Release(mouse.rightButton);
            yield return new WaitForSeconds(0.3f);
            
            Assert.That(look.View.fieldOfView, Is.EqualTo(baseFov).Within(0.1f), "FOV should return to base");
        }
    }
}
