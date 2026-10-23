#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using LastSignal.Forest;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>User-run authored scene checks. Not a visual or performance acceptance test.</summary>
    public sealed class ForestPrototypePlayTests
    {
        const string Path = "Assets/LastSignal/Scenes/Validation/ForestPrototype120.unity";
        ForestPrototypeState state;
        FirstPersonMotor motor;
        PlayerInputReader input;
        CharacterController controller;
        ForestPrototypeExplorer explorer;
        float oldTime;
        [UnitySetUp] public IEnumerator Setup()
        {
            Assert.That(System.IO.File.Exists(Path), Is.True, "Run Forest 120/Create or regenerate isolated forest first.");
            oldTime = Time.timeScale; Time.timeScale = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Path, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            state = Object.FindAnyObjectByType<ForestPrototypeState>();
            explorer = state.player.GetComponent<ForestPrototypeExplorer>(); explorer.enabled = false;
            input = state.player.GetComponent<PlayerInputReader>(); input.SetGameplay(true);
            motor = state.player.GetComponent<FirstPersonMotor>(); controller = state.player.GetComponent<CharacterController>();
            motor.enabled = false; Physics.SyncTransforms();
            for (int i = 0; i < 60; i++) motor.Simulate(Vector2.zero, false, 1f / 60);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (input) input.SetGameplay(false); Time.timeScale = oldTime; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            yield return null;
        }
        [UnityTest] public IEnumerator SpawnWalkSprintAndCrouchStayOnTerrain()
        {
            Assert.That(motor.Grounded, Is.True);
            var start = state.player.position;
            for (int i = 0; i < 60; i++) motor.Simulate(Vector2.up, false, 1f / 60);
            float walk = Vector3.Distance(start, state.player.position); Assert.That(walk, Is.InRange(2.7f, 3.8f));
            start = state.player.position;
            for (int i = 0; i < 60; i++) motor.Simulate(Vector2.up, true, 1f / 60);
            Assert.That(Vector3.Distance(start, state.player.position), Is.GreaterThan(walk));
            var stance = state.player.GetComponent<PlayerStance>(); Assert.That(stance.TrySetCrouching(true), Is.True);
            yield return new WaitForSeconds(.2f);
            Assert.That(controller.height, Is.LessThan(1.8f)); Assert.That(motor.Grounded, Is.True);
            Assert.That(state.player.position.y, Is.EqualTo(state.terrain.SampleHeight(state.player.position)).Within(.18));
        }
        [UnityTest] public IEnumerator GroundedJumpRejectsAirJumpAndReturnsToGround()
        {
            motor.enabled = true; input.SetGameplay(true);
            Assert.That(motor.TryJump(float.NaN), Is.False); Assert.That(motor.TryJump(-1), Is.False);
            float start = state.player.position.y;
            Assert.That(motor.TryJump(1.05f), Is.True); Assert.That(motor.TryJump(1.05f), Is.False);
            // Advance motor time explicitly: frame-based waits can overshoot the ascent check.
            // Do not yield between steps, so the enabled motor cannot also advance in Update.
            for (int i = 0; i < 12; i++) motor.Simulate(Vector2.zero, false, 1f / 60);
            Assert.That(state.player.position.y, Is.GreaterThan(start + .45f)); Assert.That(motor.TryJump(1.05f), Is.False);
            for (int i = 0; i < 60; i++) motor.Simulate(Vector2.zero, false, 1f / 60);
            Assert.That(motor.Grounded, Is.True); Assert.That(state.player.position.y, Is.EqualTo(start).Within(.18));
            yield break;
        }
        [UnityTest] public IEnumerator TrunkColliderBlocksPlayerWithoutDynamicTreePhysics()
        {
            var tree = state.canopy.GetChild(0); var trunk = tree.GetComponent<CapsuleCollider>();
            controller.enabled = false;
            var p = tree.position + Vector3.back * 2.2f; p.y = state.terrain.SampleHeight(p) + .1f;
            state.player.SetPositionAndRotation(p, Quaternion.identity); controller.enabled = true; Physics.SyncTransforms();
            for (int i = 0; i < 180; i++) motor.Simulate(Vector2.up, false, 1f / 60);
            Assert.That(state.player.position.z, Is.LessThan(tree.position.z));
            Assert.That(Vector2.Distance(new Vector2(state.player.position.x, state.player.position.z), new Vector2(tree.position.x, tree.position.z)),
                Is.GreaterThan(trunk.radius * tree.lossyScale.x + controller.radius - .12f));
            Assert.That(state.generated.GetComponentsInChildren<Rigidbody>(), Is.Empty); yield return null;
        }
        [UnityTest] public IEnumerator AllViewpointsAndLightingPresetsRemainReachable()
        {
            for (int i = 0; i < 6; i++)
            {
                explorer.GoToShot(i); Physics.SyncTransforms();
                for (int step = 0; step < 30; step++) motor.Simulate(Vector2.zero, false, 1f / 60);
                Assert.That(motor.Grounded, Is.True, state.shots[i].name);
                Assert.That(Vector3.Distance(state.gameplayCamera.transform.position, state.shots[i].position), Is.LessThan(.25f));
                Assert.That(explorer.lighting.isOvercast, Is.EqualTo(i == 5)); yield return null;
            }
            explorer.lighting.Apply(false); Assert.That(explorer.lighting.isOvercast, Is.False);
        }
    }
}
#endif
