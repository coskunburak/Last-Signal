using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine.SceneManagement;

namespace LastSignal.Tests
{
    public class MovementAcceptanceTests
    {
        GameObject player, floor;
        PlayerInputReader input;
        FirstPersonMotor motor;
        PlayerStance stance;
        Keyboard keyboard;
        Mouse mouse;
        bool previousRunInBackground;
        readonly System.Collections.Generic.List<GameObject> fixtures = new System.Collections.Generic.List<GameObject>();
        readonly System.Collections.Generic.List<InputDevice> suspendedDevices = new System.Collections.Generic.List<InputDevice>();
        InputSettings settings;
        InputSettings.BackgroundBehavior previousBackground;
        InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
        InputFixtureIsolation.SceneScope sceneScope;
        [SetUp] public void Setup()
        {
            sceneScope = new InputFixtureIsolation.SceneScope();
            InputFixtureIsolation.DisableLiveActions();
            // Keep the native clock and manager across asynchronous player-loop frames.
            // A replacement test runtime starts its clock at zero, while Editor transition
            // timestamps remain on the native timeline and may discard otherwise valid events.
            settings = InputSystem.settings;
            previousBackground = settings.backgroundBehavior;
            previousEditorInput = settings.editorInputBehaviorInPlayMode;
            suspendedDevices.Clear();
            foreach (var device in InputSystem.devices)
                if (device.enabled) suspendedDevices.Add(device);
            foreach (var device in suspendedDevices) InputSystem.DisableDevice(device);
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Time.timeScale = 1;
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            floor = Box("Test ground", new Vector3(0, -.25f, 0), new Vector3(100, .5f, 100));
#if UNITY_EDITOR
            player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Player/Player.prefab"), new Vector3(0, .05f, 0), Quaternion.identity); player.AddComponent<LastSignal.Inventory.PlayerInventory>().Initialize(24);
#endif
            input = player.GetComponent<PlayerInputReader>();
            stance = player.GetComponent<PlayerStance>();
            motor = player.GetComponent<FirstPersonMotor>(); motor.enabled = false;
            input.SetGameplay(true);
            Physics.SyncTransforms();
            motor.Simulate(Vector2.zero, false, .2f);
        }
        [TearDown] public void TearDown()
        {
            if (player) Object.DestroyImmediate(player);
            foreach (var obj in fixtures) if (obj) Object.DestroyImmediate(obj);
            fixtures.Clear();
            // Only destroy fixture-owned objects; the active scene can also own the test runner.
            Time.timeScale = 1;
            Application.runInBackground = previousRunInBackground;
            Cursor.lockState = CursorLockMode.None;
            InputFixtureIsolation.DisableLiveActions();
            try
            {
                if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
                if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            }
            finally
            {
                if (settings)
                {
                    settings.backgroundBehavior = previousBackground;
                    settings.editorInputBehaviorInPlayMode = previousEditorInput;
                }
                foreach (var device in suspendedDevices)
                    if (device.added) InputSystem.EnableDevice(device);
                suspendedDevices.Clear();
                sceneScope?.Dispose(); sceneScope = null;
            }
        }
        static void Set<TValue>(InputControl<TValue> control, TValue value) where TValue : struct
        {
            using (StateEvent.From(control.device, out var eventPtr))
            {
                control.WriteValueIntoEvent(value, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
        }
        static void Press(UnityEngine.InputSystem.Controls.ButtonControl control) => Set(control, 1f);
        static void Release(UnityEngine.InputSystem.Controls.ButtonControl control) => Set(control, 0f);

        GameObject Box(string name, Vector3 position, Vector3 scale)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
            obj.transform.position = position; obj.transform.localScale = scale;
            fixtures.Add(obj); return obj;
        }
        void Place(Vector3 position, float yaw = 0)
        {
            var capsule = player.GetComponent<CharacterController>(); capsule.enabled = false;
            player.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0)); capsule.enabled = true;
            Physics.SyncTransforms();
        }
        void Walk(Vector2 direction, float seconds, int fps = 60, bool sprint = false)
        { for (int i = 0; i < seconds * fps; i++) motor.Simulate(direction, sprint, 1f / fps); }
        DoorInteractable Door(Vector3 position)
        {
#if UNITY_EDITOR
            var obj = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Interactions/Door.prefab"), position, Quaternion.identity);
            fixtures.Add(obj); Physics.SyncTransforms(); return obj.GetComponent<DoorInteractable>();
#else
            return null;
#endif
        }
        [TestCase(30)] [TestCase(60)] [TestCase(120)]
        public void WalkingDistanceAndDiagonalCapAreFrameIndependent(int fps)
        {
            Vector3 start = player.transform.position;
            Walk(Vector2.up, 2, fps);
            float straight = Vector3.Distance(new Vector3(start.x, 0, start.z), new Vector3(player.transform.position.x, 0, player.transform.position.z));
            Assert.That(straight, Is.EqualTo(6.4f).Within(.05f));
            Place(start); Walk(Vector2.one, 2, fps);
            Assert.That(new Vector2(player.transform.position.x, player.transform.position.z).magnitude, Is.EqualTo(straight).Within(.05f));
        }
        [Test]
        public void YawSprintAndCrouchSpeedUseConfiguredPolicy()
        {
            Place(new Vector3(0, .03f, 0), 90); Walk(Vector2.up, 1, sprint: true);
            Assert.That(player.transform.position.x, Is.EqualTo(5.5f).Within(.05f));
            Assert.That(Mathf.Abs(player.transform.position.z), Is.LessThan(.05f));
            Assert.That(stance.TrySetCrouching(true), Is.True);
            float start = player.transform.position.x; Walk(Vector2.up, 1, sprint: true);
            Assert.That(player.transform.position.x - start, Is.EqualTo(1.6f).Within(.05f));
        }
        [UnityTest]
        public IEnumerator ValViewmodelUsesSprintClipOnlyWhileActuallySprinting()
        {
            yield return null; // PlayerCombatController creates the rifle in Start.
            var rifle = player.GetComponent<PlayerCombatController>().Firearm;
            Assert.That(rifle, Is.Not.Null);
            float deadline = Time.realtimeSinceStartup + 5f;
            while (rifle.RuntimeState.State != WeaponState.Ready && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(rifle.RuntimeState.State, Is.EqualTo(WeaponState.Ready));
            var animator = rifle.GetComponentInChildren<Animator>(true);
            Assert.That(animator, Is.Not.Null);
            deadline = Time.realtimeSinceStartup + 5f;
            while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Ready") && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Ready"), Is.True,
                "Rifle equip animation must finish before testing the Ready-to-Sprint transition.");

            motor.Simulate(Vector2.up, true, 1f / 60f);
            Assert.That(motor.IsSprinting, Is.True);
            yield return new WaitForSeconds(.2f);
            Assert.That(animator.GetBool("Sprinting"), Is.True);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Sprint"), Is.True);

            motor.Simulate(Vector2.zero, false, 1f / 60f);
            Assert.That(motor.IsSprinting, Is.False);
            yield return new WaitForSeconds(.2f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Ready"), Is.True);
        }
        [Test]
        public void PitchClampsInBothDirections()
        {
            var look = player.GetComponent<FirstPersonLook>();
            look.ApplyLook(new Vector2(0, 100000)); Assert.That(look.Pitch, Is.EqualTo(-85));
            look.ApplyLook(new Vector2(0, -100000)); Assert.That(look.Pitch, Is.EqualTo(85));
        }
        [UnityTest]
        public IEnumerator CeilingRejectsStandThenAllowsNewRequestWithoutFootDrift()
        {
            Assert.That(stance.TrySetCrouching(true), Is.True);
            yield return new WaitForSeconds(.2f);
            Assert.That(player.GetComponent<CharacterController>().height, Is.EqualTo(1.2f).Within(.001f));
            var roof = Box("Roof", new Vector3(0, 1.45f, 0), new Vector3(3, .3f, 3)); Physics.SyncTransforms();
            Vector3 foot = player.transform.position;
            for (int i = 0; i < 20; i++)
            {
                Assert.That(stance.TrySetCrouching(false), Is.False);
                Assert.That(player.GetComponent<CharacterController>().height, Is.EqualTo(1.2f));
            }
            Assert.That(player.transform.position, Is.EqualTo(foot));
            Object.DestroyImmediate(roof); Physics.SyncTransforms();
            Assert.That(stance.TrySetCrouching(false), Is.True);
            yield return new WaitForSeconds(.2f);
            Assert.That(player.GetComponent<CharacterController>().height, Is.EqualTo(1.8f).Within(.001f));
            Assert.That(player.GetComponent<FirstPersonLook>().View.transform.localPosition.y, Is.EqualTo(1.62f).Within(.001f));
        }
        [Test]
        public void StepsThresholdWallAndDropUseRealCollision()
        {
            Box("step .15", new Vector3(0, .075f, 1.5f), new Vector3(3, .15f, 1));
            Box("step .25 rise", new Vector3(0, .2f, 2.5f), new Vector3(3, .4f, 1)); Physics.SyncTransforms();
            Walk(Vector2.up, .8f); Assert.That(player.transform.position.y, Is.GreaterThan(.35f));
            Walk(Vector2.up, 1); Assert.That(player.transform.position.y, Is.LessThan(.1f)); Assert.That(motor.Grounded, Is.True);
            Box("Wall", new Vector3(0, 2, 7), new Vector3(3, 4, .2f)); Physics.SyncTransforms();
            Walk(Vector2.up, 2); Assert.That(player.transform.position.z, Is.LessThan(6.8f));
            Place(new Vector3(5, 3, 0)); Walk(Vector2.zero, 2);
            Assert.That(motor.Grounded, Is.True); Assert.That(player.transform.position.y, Is.InRange(-.05f, .1f));
        }
        [TestCase(30, true)] [TestCase(60, false)]
        public void SlopeLimitAcceptsGentleAndRejectsSteep(float degrees, bool passable)
        {
            float radians = degrees * Mathf.Deg2Rad;
            var ramp = Box("Ramp", new Vector3(0, Mathf.Sin(radians) * 2.5f - .1f, 1 + Mathf.Cos(radians) * 2.5f), new Vector3(3, .2f, 5));
            ramp.transform.rotation = Quaternion.Euler(-degrees, 0, 0); Physics.SyncTransforms();
            Walk(Vector2.up, 1.1f);
            if (passable) { Assert.That(player.transform.position.y, Is.GreaterThan(.7f)); Assert.That(player.transform.position.z, Is.GreaterThan(2.5f)); }
            else { Assert.That(player.transform.position.y, Is.LessThan(.35f)); Assert.That(player.transform.position.z, Is.LessThan(1.4f)); }
        }
        [UnityTest]
        public IEnumerator InteractionRevalidatesRangeOcclusionDisabledAndDestroyedTargets()
        {
            var interaction = player.GetComponent<InteractionController>();
            var door = Door(new Vector3(-.65f, 0, 2));
            yield return null;
            Assert.That(interaction.Resolve(), Is.SameAs(door));
            var wall = Box("Blocker", new Vector3(0, 1, 1), new Vector3(3, 2, .1f)); Physics.SyncTransforms();
            Assert.That(interaction.TryInteract(), Is.False); Assert.That(door.AcceptedRequests, Is.Zero);
            Object.DestroyImmediate(wall); Physics.SyncTransforms();
            door.transform.position += Vector3.forward * 2; Physics.SyncTransforms();
            Assert.That(interaction.Resolve(), Is.Null); Assert.That(interaction.TryInteract(), Is.False);
            door.transform.position -= Vector3.forward * 2; Physics.SyncTransforms();
            door.enabled = false; Assert.That(interaction.Resolve(), Is.Null);
            door.enabled = true; Assert.That(interaction.TryInteract(), Is.True);
            Assert.That(interaction.TryInteract(), Is.False); Assert.That(door.AcceptedRequests, Is.EqualTo(1));
            Object.DestroyImmediate(door.gameObject); yield return null;
            Assert.That(interaction.Resolve(), Is.Null); Assert.That(interaction.Prompt, Is.Empty);
        }
        [UnityTest]
        public IEnumerator DoorOpensClosesAndStopsForObstruction()
        {
            var door = Door(new Vector3(5, 0, 2));
            Assert.That(door.TryInteract(), Is.True); Assert.That(door.TryInteract(), Is.False);
            yield return new WaitForSeconds(1);
            Assert.That(door.IsOpen, Is.True); Assert.That(door.Prompt, Does.Contain("Kapat"));
            Assert.That(door.TryInteract(), Is.True); yield return new WaitForSeconds(1);
            Assert.That(door.IsOpen, Is.False); Assert.That(door.AcceptedRequests, Is.EqualTo(2));
            var blocker = Box("Door swing obstruction", new Vector3(5.8f, 1, 1.5f), new Vector3(.4f, 2, .4f)); Physics.SyncTransforms();
            door.TryInteract(); yield return new WaitForSeconds(1);
            Assert.That(door.Obstructed, Is.True); Assert.That(door.IsOpen, Is.False);
            Object.DestroyImmediate(blocker); Physics.SyncTransforms(); yield return new WaitForSeconds(1);
            Assert.That(door.IsOpen, Is.True);
        }
        [UnityTest]
        public IEnumerator MouseAndCrouchActionsReachCameraAndCapsule()
        {
            int crouchRequests = 0;
            input.CrouchRequested += () => crouchRequests++;
            yield return null; yield return null;
            Set(mouse.delta, new Vector2(100, 50)); yield return null;
            Assert.That(player.GetComponent<FirstPersonLook>().Pitch, Is.EqualTo(-6).Within(.1f));
            Assert.That(player.transform.eulerAngles.y, Is.EqualTo(12).Within(.1f));
            Press(keyboard.cKey); yield return null;
            Assert.That(stance.IsCrouching, Is.True);
            Assert.That(crouchRequests, Is.EqualTo(1));
            yield return new WaitForSeconds(.2f);
            Assert.That(player.GetComponent<CharacterController>().height, Is.EqualTo(1.2f).Within(.001f));
            // Native timestamped events are processed by the player loop, with no mock
            // runtime clock or manual InputSystem.Update mixed into asynchronous frames.
            Release(keyboard.cKey); yield return null;
            Assert.That(keyboard.cKey.isPressed, Is.False, "C must be released before a second toggle.");
            Assert.That(input.CrouchHeld, Is.False, "Input reader must observe the released C key.");
            Assert.That(crouchRequests, Is.EqualTo(1), "Release must not toggle stance.");
            yield return null;
            Press(keyboard.cKey); yield return null;
            Assert.That(crouchRequests, Is.EqualTo(2));
            Assert.That(stance.IsCrouching, Is.False,
                $"StandBlocked={stance.StandBlocked}, CanStand={stance.CanStand()}, " +
                $"GameplayActive={input.GameplayActive}, CrouchHeld={input.CrouchHeld}, " +
                $"requests={crouchRequests}, sliding={motor.IsSliding}, " +
                $"position={player.transform.position}, scene={SceneManager.GetActiveScene().name}");
            Release(keyboard.cKey); yield return null;
        }
        [UnityTest]
        public IEnumerator PauseFocusAndHeldKeyReturnNeverLeakGameplay()
        {
            var door = Door(new Vector3(-.65f, 0, 2));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift)); yield return null;
            Assert.That(input.Move.y, Is.GreaterThan(.9f)); Assert.That(input.SprintHeld, Is.True);
            input.NotifyFocusLost(); Set(mouse.delta, new Vector2(300, 100)); Press(keyboard.eKey); yield return null;
            Assert.That(input.Move, Is.EqualTo(Vector2.zero)); Assert.That(input.Look, Is.EqualTo(Vector2.zero));
            Assert.That(input.SprintHeld, Is.False); Assert.That(door.AcceptedRequests, Is.Zero);
            input.SetGameplay(true); yield return null; yield return null;
            Assert.That(input.Move, Is.EqualTo(Vector2.zero)); Assert.That(input.SprintHeld, Is.False);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null;
            Press(keyboard.wKey); yield return null; Assert.That(input.Move.y, Is.GreaterThan(.9f));
        }
        [UnityTest]
        public IEnumerator TenEnableDisableCyclesHaveOneInputRequest()
        {
            int requests = 0; input.InteractRequested += Count;
            for (int i = 0; i < 10; i++)
            {
                input.enabled = false; input.enabled = true; input.SetGameplay(true);
                yield return null; yield return null;
                Press(keyboard.eKey); yield return null; Release(keyboard.eKey);
                Assert.That(requests, Is.EqualTo(i + 1), "cycle " + i);
            }
            input.InteractRequested -= Count;
            void Count() => requests++;
        }
        [UnityTest]
        public IEnumerator YIntegratedParkourUsesAuthoredGeometry()
        {
            Object.DestroyImmediate(player); Object.DestroyImmediate(floor);
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/Validation/S001Acceptance.unity", new LoadSceneParameters(LoadSceneMode.Single));
#endif
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                if ((root.hideFlags & HideFlags.DontSave) == 0) fixtures.Add(root);
            yield return null;
            var session = Object.FindAnyObjectByType<SessionFlow>();
            session.Resume();
            player = session.Player;
            input = player.GetComponent<PlayerInputReader>(); input.SetGameplay(false);
            motor = player.GetComponent<FirstPersonMotor>(); motor.enabled = false;
            stance = player.GetComponent<PlayerStance>();
            Place(new Vector3(-10, .04f, 1)); Walk(Vector2.up, 1.2f);
            Assert.That(player.transform.position.y, Is.GreaterThan(.35f), "authored steps");
            Place(new Vector3(-5, .04f, 1.5f)); Walk(Vector2.up, 1.1f);
            Assert.That(player.transform.position.y, Is.GreaterThan(.7f), "authored 30 degree ramp");
            Place(new Vector3(0, .04f, 3)); Walk(Vector2.up, 1.1f);
            Assert.That(player.transform.position.z, Is.LessThan(5.4f), "authored steep ramp");
            Assert.That(player.transform.position.y, Is.LessThan(.35f));
            Place(new Vector3(5, .04f, 2)); Walk(Vector2.up, 2);
            Assert.That(player.transform.position.z, Is.GreaterThan(8), "authored narrow passage");
            Place(new Vector3(10, .04f, 2)); Walk(Vector2.up, .5f);
            Assert.That(player.transform.position.z, Is.LessThan(3), "standing tunnel entry blocked");
            stance.TrySetCrouching(true);
            yield return new WaitForSeconds(.2f);
            Walk(Vector2.up, 1);
            Assert.That(player.transform.position.z, Is.GreaterThan(3.5f));
            Assert.That(stance.TrySetCrouching(false), Is.False, "authored low ceiling");
            Walk(Vector2.up, 3); Assert.That(stance.TrySetCrouching(false), Is.True);
            Place(new Vector3(-10, .04f, 13)); Walk(Vector2.up, 2.7f);
            Assert.That(player.transform.position.y, Is.GreaterThan(2.8f), "drop deck access");
            Walk(Vector2.up, 2.5f);
            Assert.That(motor.Grounded, Is.True); Assert.That(player.transform.position.y, Is.LessThan(.1f), "safe landing");
            Place(new Vector3(0, .04f, 0)); input.SetGameplay(true); yield return null; yield return null;
            var interaction = player.GetComponent<InteractionController>();
            Assert.That(interaction.TryInteract(), Is.True); yield return new WaitForSeconds(1);
            Walk(Vector2.up, 1.2f); Assert.That(player.transform.position.z, Is.GreaterThan(3.5f), "authored door threshold");
            Place(new Vector3(14.65f, .04f, 13));
            Assert.That(interaction.TryInteract(), Is.False, "authored occlusion blocker");
            Debug.Log("S001_AUTHORED_PARKOUR PASS: steps, 30/60 slopes, corridor, tunnel/stand, deck/drop, threshold, occlusion.");
            var scene = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("AfterParkour"); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(scene);
        }
        [UnityTest]
        public IEnumerator ZIntegratedSceneTenMenuSessionsDoNotRetainPlayerOrCallbacks()
        {
            Object.DestroyImmediate(player); Object.DestroyImmediate(floor);
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/Validation/S001Acceptance.unity", new LoadSceneParameters(LoadSceneMode.Single));
#endif
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                if ((root.hideFlags & HideFlags.DontSave) == 0) fixtures.Add(root);
            yield return null;
            var session = Object.FindAnyObjectByType<SessionFlow>();
            for (int i = 0; i < 10; i++)
            {
                if (session.InMenu) session.BeginSession();
                yield return null; yield return null;
                var current = session.Player; var reader = current.GetComponent<PlayerInputReader>();
                // Batch execution has no OS focus; explicitly resume the same public UI operation.
                session.Resume(); yield return null; yield return null;
                var door = current.GetComponent<InteractionController>().Resolve() as DoorInteractable;
                Assert.That(door, Is.Not.Null, "visible door cycle " + i);
                Press(keyboard.eKey); yield return null; Release(keyboard.eKey);
                Assert.That(door.AcceptedRequests, Is.EqualTo(1), "one input cycle " + i);
                yield return new WaitForSeconds(1);
                Vector3 doorDirection = door.GetComponentInChildren<BoxCollider>().bounds.center - current.transform.position;
                doorDirection.y = 0; current.transform.rotation = Quaternion.LookRotation(doorDirection);
                Physics.SyncTransforms();
                Press(keyboard.eKey); yield return null; Release(keyboard.eKey);
                Assert.That(door.AcceptedRequests, Is.EqualTo(2));
                yield return new WaitForSeconds(1);
                Assert.That(door.IsOpen, Is.False);
                current.transform.rotation = Quaternion.identity;
                var look = current.GetComponent<FirstPersonLook>();
                Vector3 before = current.transform.position;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S)); yield return new WaitForSeconds(.2f);
                Debug.Log("SESSION_INPUT cycle=" + i + " key=" + keyboard.sKey.isPressed + " active=" + reader.GameplayActive + " move=" + reader.Move + " before=" + before + " after=" + current.transform.position);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                Assert.That(current.transform.position.z, Is.LessThan(before.z));
                session.Pause(); before = current.transform.position; float pitch = look.Pitch;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.E)); Set(mouse.delta, new Vector2(100, 100)); yield return null;
                Assert.That(reader.Move, Is.EqualTo(Vector2.zero)); Assert.That(look.Pitch, Is.EqualTo(pitch));
                Assert.That(current.transform.position, Is.EqualTo(before)); Assert.That(door.AcceptedRequests, Is.EqualTo(2));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                session.ReturnToMenu(); session.ReturnToMenu(); yield return null;
                Assert.That(current == null, Is.True);
                Assert.That(Object.FindObjectsByType<PlayerInputReader>().Length, Is.Zero);
                Press(keyboard.eKey); Release(keyboard.eKey); Assert.That(door.AcceptedRequests, Is.EqualTo(2));
            }
            Object.DestroyImmediate(session.gameObject);
        }
    }
}
