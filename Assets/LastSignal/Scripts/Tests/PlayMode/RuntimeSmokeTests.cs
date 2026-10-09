using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using LastSignal;
using UnityEngine.SceneManagement;

namespace LastSignal.Tests
{
    public class RuntimeSmokeTests : InputTestFixture
    {
        Mouse mouse;
        Keyboard keyboard;

        InputFixtureIsolation.SceneScope sceneScope;
        public override void Setup()
        {
            sceneScope = new InputFixtureIsolation.SceneScope(); InputFixtureIsolation.DisableLiveActions();
            base.Setup();
            Time.timeScale = 1;
            mouse = InputSystem.AddDevice<Mouse>();
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        public override void TearDown()
        {
            InputFixtureIsolation.DisableLiveActions();
            Time.timeScale = 1;
            try { base.TearDown(); } finally { sceneScope?.Dispose(); sceneScope = null; }
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
