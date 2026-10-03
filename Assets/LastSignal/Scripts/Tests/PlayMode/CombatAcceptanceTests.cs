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
            InputFixtureIsolation.DisableLiveActions();
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
            session.Resume(); 
            var myPi = Object.FindAnyObjectByType<PlayerInputReader>();
            if (myPi) {
                var myF = typeof(PlayerInputReader).GetField("instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var myA = (UnityEngine.InputSystem.InputActionAsset)myF.GetValue(myPi);
                if (myA != null) {
                    myA.Disable();
                    var devs = new System.Collections.Generic.List<UnityEngine.InputSystem.InputDevice>();
                    if (mouse != null) devs.Add(mouse);
                    if (keyboard != null) devs.Add(keyboard);
                    myA.devices = new UnityEngine.InputSystem.Utilities.ReadOnlyArray<UnityEngine.InputSystem.InputDevice>(devs.ToArray());
                    myA.Enable();
                }
                myPi.SetGameplay(true);
            }

            yield return null;

            var player = session.Player;
            combat = player.GetComponent<PlayerCombatController>();
            hud = Object.FindAnyObjectByType<AcceptanceHud>();
            
            // The same default weapon used by the normal S001 route must already be equipped.
            var instance = combat.ActiveWeapon;
            Assert.That(instance, Is.Not.Null);
            
            // Fast-forward equip time so it's ready
            yield return WaitForGameplaySeconds(instance.Definition.EquipSeconds + 0.1f);

            var targets = Object.FindObjectsByType<DamageableTarget>(FindObjectsSortMode.None);
            foreach (var t in targets) if (t.name == "CombatTarget_10m") target10m = t;
            
            Debug.Log("B0A_ORDER UnitySetUp ready devices=" + InputSystem.devices.Count + " active=" + player.GetComponent<PlayerInputReader>().GameplayActive + " mouse=" + mouse);
            yield return null;
        }

        // Oyun zamanını bekler; duraklamış bir oturum test turunu süresiz kilitlemez.
        public static IEnumerator WaitForGameplaySeconds(float seconds)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + Mathf.Max(10f, seconds * 4f);
            float elapsed = 0;
            while (elapsed < seconds)
            {
                Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline),
                    $"Oyun zamanı ilerlemedi. timeScale={Time.timeScale}, odak={Application.isFocused}. Beklenen süre: {seconds}s.");
                yield return null;
                elapsed += Time.deltaTime;
            }
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
            InputFixtureIsolation.DisableLiveActions();
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
                Release(mouse.leftButton); yield return new WaitForSeconds(0.2f);
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
