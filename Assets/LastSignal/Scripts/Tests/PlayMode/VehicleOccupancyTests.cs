#if UNITY_EDITOR
using System.Collections;
using LastSignal.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public sealed class VehicleOccupancyTests
    {
        SessionFlow flow;
        VehicleActor car;
        GameObject leftWall, rightWall;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/LastSignal/Scenes/Validation/VehiclePlayableAcceptance.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            flow = Object.FindAnyObjectByType<SessionFlow>(); flow.Resume();
            car = flow.GetComponent<VehicleWorld>().Actor;
            Assert.IsNotNull(car);
            for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
            var player = flow.Player; var capsule = player.GetComponent<CharacterController>(); capsule.enabled = false;
            player.transform.position = car.transform.TransformPoint(new Vector3(-2, 0, .15f)) + Vector3.up * .1f;
            capsule.enabled = true; Physics.SyncTransforms(); yield return null;
        }
        [UnityTest] public IEnumerator EnterExitPreservesPlayerAndGatesOnFootAuthority()
        {
            var player = flow.Player;
            Assert.IsTrue(car.TryInteract()); Assert.IsFalse(car.TryInteract());
            Assert.AreSame(player, flow.Player); Assert.IsTrue(car.Occupied);
            Assert.IsFalse(player.GetComponent<CharacterController>().enabled);
            Assert.IsFalse(player.GetComponent<FirstPersonMotor>().enabled);
            Assert.IsFalse(player.GetComponent<PlayerInputReader>().GameplayActive);
            Assert.IsTrue(player.GetComponent<PlayerInputReader>().DrivingActive);
            Assert.That(Vector3.Distance(player.GetComponent<FirstPersonLook>().View.transform.position,
                car.transform.Find("DriverCameraAnchor").position), Is.LessThan(.01f));
            Assert.IsTrue(car.TryIgnition()); double fuel = car.Resources.FuelLiters;
            yield return new WaitForSeconds(.1f); Assert.Less(car.Resources.FuelLiters, fuel);
            flow.Pause(); foreach (var wheel in car.GetComponentsInChildren<WheelCollider>()) Assert.AreEqual(0, wheel.motorTorque);
            Assert.IsFalse(player.GetComponent<PlayerInputReader>().DrivingActive);
            flow.Resume(); Assert.IsTrue(player.GetComponent<PlayerInputReader>().DrivingActive);
            Assert.IsTrue(car.TryExit()); Assert.IsFalse(car.Occupied);
            Assert.IsTrue(player.GetComponent<CharacterController>().enabled);
            Assert.IsTrue(player.GetComponent<FirstPersonMotor>().enabled);
            Assert.IsTrue(player.GetComponent<PlayerInputReader>().GameplayActive);
        }
        [UnityTest] public IEnumerator CharacterVisualFollowsCommittedSeatAndRestoresOnExit()
        {
            var player=flow.Player;
            var presenter=player.GetComponentInChildren<PlayerLocomotionPresenter>();
            Assert.That(presenter,Is.Not.Null);
            var animator=presenter.GetComponent<Animator>();
            Assert.That(car.TryEnter(),Is.True);
            yield return null;yield return null;
            Assert.That(animator.GetBool("Seated"),Is.True);
            Assert.That(Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.Hips).position,player.transform.position),Is.LessThan(.12f));
            var rig=presenter.GetComponent<CharacterWeaponRig>();
            Assert.That(!rig.WeaponVisual||!rig.WeaponVisual.activeSelf,Is.True,"Vehicle holsters the world weapon too.");
            flow.Pause();yield return null;
            Assert.That(animator.GetBool("Seated"),Is.True,"Input pause is not a seat exit.");
            flow.Resume();Assert.That(car.TryExit(),Is.True);
            yield return null;yield return null;
            Assert.That(animator.GetBool("Seated"),Is.False);
            Assert.That(presenter.transform.localPosition.sqrMagnitude,Is.LessThan(.0001f));
            Assert.That(player.GetComponent<CharacterController>().enabled,Is.True);
            Assert.That(player.GetComponent<FirstPersonMotor>().enabled,Is.True);
        }
        [UnityTest] public IEnumerator CharacterAndVehicleInspectionHaveExclusiveCameraOwnership()
        {
            Assert.That(car.TryEnter(),Is.True);yield return null;
            var diagnostic=new GameObject("Vehicle camera ownership fixture");
            var vehicleView=diagnostic.AddComponent<VehicleInspectionView>();
            try
            {
                yield return null;
                var characterView=flow.Player.GetComponent<CharacterInspectionCamera>();
                var firstPerson=flow.Player.GetComponent<FirstPersonLook>().View;
                Assert.That(vehicleView.SetInspection(true),Is.True);yield return null;
                Assert.That(vehicleView.Inspecting,Is.True);Assert.That(firstPerson.enabled,Is.False);
                Assert.That(vehicleView.Inspection.cullingMask&((1<<2)|(1<<29)),Is.Zero);
                characterView.SetInspection(true);Assert.That(characterView.Inspecting,Is.False);
                vehicleView.SetInspection(false);Assert.That(firstPerson.enabled,Is.True);
                characterView.SetInspection(true);Assert.That(characterView.Inspecting,Is.True);
                Assert.That(vehicleView.SetInspection(true),Is.False);yield return null;
                Assert.That(vehicleView.Inspecting,Is.False);Assert.That(firstPerson.enabled,Is.False);
                characterView.SetInspection(false);Assert.That(firstPerson.enabled,Is.True);
                Assert.That(car.TryExit(),Is.True);
            }
            finally{Object.Destroy(diagnostic);}
        }
        GameObject Wall(string anchor)
        {
            var go = new GameObject("Vehicle blocked exit fixture"); go.transform.position = car.transform.Find(anchor).position + Vector3.up;
            go.AddComponent<BoxCollider>().size = new Vector3(1, 2, 1); return go;
        }
        [UnityTest] public IEnumerator BlockedExitsRetainDriverAndFallbackIsSafe()
        {
            Assert.IsTrue(car.TryEnter()); leftWall = Wall("DriverExit"); rightWall = Wall("PassengerExit"); Physics.SyncTransforms();
            Assert.IsFalse(car.TryExit()); Assert.IsTrue(car.Occupied);
            rightWall.SetActive(false); Object.Destroy(rightWall); yield return new WaitForFixedUpdate(); Physics.SyncTransforms();
            Assert.IsTrue(car.TryExit(), "speed=" + car.GetComponent<Rigidbody>().linearVelocity + " ready=" + car.Ready);
            Assert.That(Vector3.Distance(flow.Player.transform.position, car.transform.Find("PassengerExit").position), Is.LessThan(.4f));
        }
        [UnityTest] public IEnumerator UnsafeSpeedRejectsExitAndSessionEndRemovesVehicle()
        {
            Assert.IsTrue(car.TryEnter()); car.GetComponent<Rigidbody>().linearVelocity = Vector3.forward * 3;
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.IsFalse(car.TryExit()); Assert.IsTrue(car.Occupied);
            flow.ReturnToMenu(); yield return null; yield return null;
            Assert.IsFalse(car); Assert.IsNull(flow.Player);
        }
        [UnityTest] public IEnumerator OccupiedDiskSaveRestoresSamePickupResourcesPoseAndDriverWithEngineOff()
        {
            var inventory = flow.Player.GetComponent<LastSignal.Inventory.PlayerInventory>();
            var ammo = UnityEditor.AssetDatabase.LoadAssetAtPath<LastSignal.Inventory.Data.ItemDefinition>("Assets/LastSignal/Data/Items/Definitions/ammo.rifle.asset");
            inventory.TryAdd(ammo, 10); Assert.AreEqual(10, car.Resources.Deposit(inventory, ammo, 10).Moved);
            car.Resources.ApplyImpact(5); Assert.IsTrue(car.TryEnter()); Assert.IsTrue(car.TryIgnition());
            yield return new WaitForSeconds(.1f);
            var save = flow.GetComponent<LastSignal.Persistence.SaveSession>(); flow.Pause();
            double fuel = car.Resources.FuelLiters, condition = car.Resources.Condition; var position = car.transform.position;
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "veh-" + System.Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var result = save.Save(path); Assert.IsTrue(result.Success, result.Message);
                flow.ReturnToMenu(); yield return null; yield return save.Load(path);
                Assert.IsTrue(save.LastResult.Success, save.LastResult.Message);
                car = flow.GetComponent<VehicleWorld>().Actor;
                Assert.IsTrue(car.Occupied); Assert.IsFalse(car.Resources.EngineRunning);
                Assert.AreEqual(fuel, car.Resources.FuelLiters, 1e-9); Assert.AreEqual(condition, car.Resources.Condition, 1e-12);
                Assert.AreEqual(10, car.Resources.CargoQuantity(ammo));
                Assert.That(Vector3.Distance(position, car.transform.position), Is.LessThan(.02f));
                Assert.IsTrue(flow.Player.GetComponent<PlayerInputReader>().DrivingActive);
            }
            finally { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); if (System.IO.File.Exists(path + ".bak")) System.IO.File.Delete(path + ".bak"); }
        }
        [UnityTest] public IEnumerator PhysicalKeyboardDrivesAndHeldThrottleCannotResumeAfterPause()
        {
            var originalSettings = InputSystem.settings;
            var testSettings = Object.Instantiate(originalSettings);
            bool originalRunInBackground = Application.runInBackground;
            Keyboard keyboard = null;
            try
            {
                // The Editor otherwise routes synthetic keyboard events away from the player
                // when Game View is unfocused. Scope this to a disposable settings clone;
                // keep the production focus policy and PlayerInputReader gates unchanged.
                testSettings.hideFlags = HideFlags.HideAndDontSave;
                testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                Application.runInBackground = true;
                InputSystem.settings = testSettings;
                keyboard = InputSystem.AddDevice<Keyboard>();
                Assert.IsTrue(car.TryEnter()); Assert.IsTrue(car.TryIgnition());
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null;
                var trace = new System.Text.StringBuilder();
                AppendDriveState(trace, "neutral", keyboard);
                var origin = car.transform.position;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                for (int sample = 1; sample <= 4; sample++)
                {
                    yield return new WaitForSeconds(.5f);
                    AppendDriveState(trace, "drive " + sample, keyboard);
                    Assert.IsTrue(keyboard.wKey.isPressed, "Synthetic W did not reach the player input update.\n" + trace);
                    Assert.Greater(flow.Player.GetComponent<PlayerInputReader>().VehicleIntent.Throttle, .9f, trace.ToString());
                }
                Assert.That(Vector3.Distance(origin, car.transform.position), Is.GreaterThan(.3f), trace.ToString());
                flow.Pause(); flow.Resume(); yield return new WaitForFixedUpdate(); yield return null;
                Assert.IsTrue(keyboard.wKey.isPressed, "Resume safety must be checked with W still held.");
                foreach (var wheel in car.GetComponentsInChildren<WheelCollider>()) Assert.AreEqual(0, wheel.motorTorque);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return new WaitForSeconds(.1f);
                float torque = 0; foreach (var wheel in car.GetComponentsInChildren<WheelCollider>()) torque += wheel.motorTorque;
                Assert.Greater(torque, 0);
            }
            finally
            {
                if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
                InputSystem.settings = originalSettings;
                Application.runInBackground = originalRunInBackground;
                Object.Destroy(testSettings);
            }
        }
        void AppendDriveState(System.Text.StringBuilder trace, string phase, Keyboard keyboard)
        {
            var reader = flow.Player.GetComponent<PlayerInputReader>();
            var intent = reader.VehicleIntent;
            var body = car.GetComponent<Rigidbody>();
            trace.AppendLine($"{phase}: ready={car.Ready} occupied={car.Occupied} driving={reader.DrivingActive} engine={car.Resources.EngineRunning} fuel={car.Resources.FuelLiters:F3} condition={car.Resources.Condition:F3} W={keyboard.wKey.isPressed} throttle={intent.Throttle} brake={intent.ServiceBrake} reverse={intent.Reverse} handbrake={intent.Handbrake} position={body.position:F4} velocity={body.linearVelocity:F4} kinematic={body.isKinematic} constraints={body.constraints}");
            foreach (var wheel in car.GetComponentsInChildren<WheelCollider>())
            {
                bool grounded = wheel.GetGroundHit(out var hit);
                trace.AppendLine($"  {wheel.name}: motor={wheel.motorTorque:F2} brake={wheel.brakeTorque:F2} rpm={wheel.rpm:F2} grounded={grounded} surface={(hit.collider ? hit.collider.name : "none")} force={hit.force:F2} grip={wheel.forwardFriction.stiffness:F3}/{wheel.sidewaysFriction.stiffness:F3}");
            }
            foreach (var device in InputSystem.devices)
                if (device is Keyboard keys)
                    foreach (var key in keys.allKeys)
                        if (key.isPressed) trace.AppendLine($"  held device={device.deviceId} key={key.name}");
        }
        [UnityTest] public IEnumerator CabinRainProtectionAndResidentPressureUseExistingAuthorities()
        {
            Assert.IsTrue(car.TryEnter()); Assert.IsTrue(car.RainCovered);
            Assert.IsTrue(flow.GetComponent<LastSignal.WorldTime.WorldClock>().QueryRoof());
            var population = flow.GetComponent<LastSignal.AI.WorldPopulationManager>();
            long before = population.LastNoiseSequence;
            var producer = new VehicleNoiseProducer(flow.Noise, 1001, new VehicleTuning());
            Assert.IsTrue(producer.Horn(car.transform.position));
            Assert.IsTrue(flow.Noise.LastTrace.PressureForwarded);
            Assert.AreEqual(before + 1, population.LastNoiseSequence);
            Assert.Greater(population.GetPressure("resident").Pressure, 0);
            Assert.IsNotNull(population.GetSaveSnapshot().residentPressure);
            yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (leftWall) Object.Destroy(leftWall); if (rightWall) Object.Destroy(rightWall);
            if (flow) flow.ReturnToMenu(); Time.timeScale = 1; yield return null;
        }
    }
}
#endif
