using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using LastSignal;
using UnityEngine.SceneManagement;

namespace LastSignal.Tests
{
    public class RuntimeSmokeTests
    {
        Mouse mouse;
        InputFixtureIsolation.SceneScope sceneScope;
        InputFixtureIsolation.NativeInputScope inputScope;
        static void Press(UnityEngine.InputSystem.Controls.ButtonControl button) => InputFixtureIsolation.QueueButton(button, true);
        static void Release(UnityEngine.InputSystem.Controls.ButtonControl button) => InputFixtureIsolation.QueueButton(button, false);

        [SetUp] public void Setup()
        {
            sceneScope = new InputFixtureIsolation.SceneScope();
            inputScope = new InputFixtureIsolation.NativeInputScope();
            Time.timeScale = 1;
            mouse = inputScope.Mouse;
        }

        [TearDown] public void TearDown()
        {
            try
            {
                var session = Object.FindAnyObjectByType<SessionFlow>();
                if (session) session.ReturnToMenu();
            }
            finally
            {
                Time.timeScale = 1;
                try { inputScope?.Dispose(); inputScope = null; }
                finally { sceneScope?.Dispose(); sceneScope = null; }
            }
        }

        [UnityTest]
        public IEnumerator SmokeTest_ThreeSessionCycles()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/Validation/CombatAcceptance.unity", new LoadSceneParameters(LoadSceneMode.Single));
            
            for (int i = 0; i < 3; i++)
            {
                var session = Object.FindAnyObjectByType<SessionFlow>();
                session.BeginSession();
                session.Resume();
                yield return null;
                
                
                // 1. One Player, One Camera
                var players = Object.FindObjectsByType<PlayerStance>(FindObjectsSortMode.None);
                var cameras = Object.FindObjectsByType<FirstPersonLook>(FindObjectsSortMode.None);
                Assert.That(players.Length, Is.EqualTo(1), "Should be exactly one Player");
                Assert.That(cameras.Length, Is.EqualTo(1), "Should be exactly one Camera");

                var player = session.Player;
                var combat = player.GetComponent<PlayerCombatController>();
                var target10m = GameObject.Find("CombatTarget_10m").GetComponent<DamageableTarget>();

                // 2. Rifle ready
                Assert.That(combat.ActiveWeapon, Is.Not.Null, "Rifle should be ready");
                yield return new WaitForSeconds(combat.ActiveWeapon.Definition.EquipSeconds + 0.1f);
                Assert.That(combat.ActiveWeapon.RuntimeState.State, Is.EqualTo(WeaponState.Ready));
                Assert.That(player.GetComponent<PlayerInputReader>().GameplayActive, Is.True);

                // 3. Fire consumes one round, target damaged
                int initialAmmo = combat.ActiveWeapon.RuntimeState.CurrentMagazine;
                float initialHealth = target10m.CurrentHealth;

                Press(mouse.leftButton);
                yield return null;
                Release(mouse.leftButton);
                yield return null;

                Assert.That(combat.ActiveWeapon.RuntimeState.CurrentMagazine, Is.EqualTo(initialAmmo - 1), "Fire should consume one round");
                Assert.That(target10m.CurrentHealth, Is.LessThan(initialHealth), "Target should be damaged");

                // 4. Reload: explicit S007 inventory fixture; no hidden weapon reserve.
                player.GetComponent<LastSignal.Inventory.PlayerInventory>().TryAdd(combat.ActiveWeapon.Definition.Ammunition, 1);
                combat.ActiveWeapon.OnReloadRequested();
                yield return new WaitForSeconds(combat.ActiveWeapon.Definition.TacticalReloadSeconds + 0.1f);
                Assert.That(combat.ActiveWeapon.RuntimeState.CurrentMagazine, Is.GreaterThan(initialAmmo - 1), "Reload should transfer ammo");

                // 5. Pause / Resume
                session.Pause();
                yield return null;
                Assert.That(Time.timeScale, Is.EqualTo(0f), "Time should pause");
                session.Resume();
                yield return null;
                Assert.That(Time.timeScale, Is.EqualTo(1f), "Time should resume");

                // 6. ReturnToMenu
                session.ReturnToMenu();
                yield return null;

                players = Object.FindObjectsByType<PlayerStance>(FindObjectsSortMode.None);
                Assert.That(players.Length, Is.EqualTo(0), "Player should be destroyed");
            }
        }
    }
}
