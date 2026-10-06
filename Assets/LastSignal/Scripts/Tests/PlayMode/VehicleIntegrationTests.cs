#if UNITY_EDITOR
using System.Collections;
using LastSignal.Vehicles;
using LastSignal.Vehicles.MotionCoreIntegration;
using LastSignal.Noise;
using LastSignal.WorldCells;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public sealed class VehicleIntegrationTests
    {
        GameObject car;
        SessionFlow flow;
        WheelCollider[] wheels;
        MotionCoreVehiclePhysicsAdapter CreateCar()
        {
            car = new GameObject("VEH-001 isolated physics fixture"); car.SetActive(false);
            car.transform.position = new Vector3(9000, 2, 9000);
            var adapter = car.AddComponent<MotionCoreVehiclePhysicsAdapter>();
            var serialized = new SerializedObject(adapter); var axles = serialized.FindProperty("axles"); axles.arraySize = 2;
            wheels = new WheelCollider[4];
            for (int i = 0; i < 4; i++)
            {
                var child = new GameObject("Wheel " + i); child.transform.SetParent(car.transform, false);
                child.transform.localPosition = new Vector3(i % 2 == 0 ? -.8f : .8f, 0, i < 2 ? 1.4f : -1.4f);
                wheels[i] = child.AddComponent<WheelCollider>(); wheels[i].radius = .4f;
                var axle = axles.GetArrayElementAtIndex(i / 2);
                axle.FindPropertyRelative(i % 2 == 0 ? "leftWheel" : "rightWheel").objectReferenceValue = wheels[i];
                axle.FindPropertyRelative("handbrake").boolValue = i >= 2;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo(); car.SetActive(true);
            car.GetComponent<Rigidbody>().useGravity = false;
            return adapter;
        }
        static IEnumerator Step() { yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate(); }
        void NoDrive() { foreach (var wheel in wheels) Assert.AreEqual(0, wheel.motorTorque); }
        float DriveTorque() { float sum = 0; foreach (var wheel in wheels) sum += wheel.motorTorque; return sum; }
        [UnityTest] public IEnumerator MC_001_ServiceBrakeNeverProducesReverseWithEngineOffOrOn()
        {
            var adapter = CreateCar();
            foreach (bool engine in new[] { false, true })
            {
                adapter.SetControl(new VehicleControlIntent(1, 1, 0, false, 1), engine);
                yield return Step(); NoDrive();
                foreach (var wheel in wheels) Assert.Greater(wheel.brakeTorque, 0);
            }
            adapter.SetControl(new VehicleControlIntent(0, 0, 0, false, 1), false);
            yield return Step(); NoDrive();
        }
        [UnityTest] public IEnumerator MC_002_ReverseRequiresExplicitIntentAndEnabledEngine()
        {
            var adapter = CreateCar(); adapter.SetControl(default, true); yield return Step(); NoDrive();
            adapter.SetControl(new VehicleControlIntent(0, 0, 0, false, 1), true);
            yield return Step(); Assert.Less(DriveTorque(), 0);
            adapter.SetControl(new VehicleControlIntent(1, 0, 0, false), true);
            yield return Step(); Assert.Greater(DriveTorque(), 0);
        }
        [UnityTest] public IEnumerator MC_003_DisableAndResumeHeldInputRequiresNeutral()
        {
            var adapter = CreateCar(); var gate = new VehicleInputGate();
            var held = new VehicleControlIntent(1, 0, 0, false);
            gate.Sample(default, true); adapter.SetControl(gate.Sample(held, true), true);
            yield return Step(); Assert.Greater(DriveTorque(), 0);
            gate.Sample(held, false); adapter.ResetTransientInput(); NoDrive();
            adapter.SetControl(gate.Sample(held, true), true); yield return Step(); NoDrive();
            gate.Sample(default, true); adapter.SetControl(gate.Sample(held, true), true);
            yield return Step(); Assert.Greater(DriveTorque(), 0);
            adapter.SendMessage("OnApplicationFocus", false); NoDrive();
            yield return Step(); NoDrive();
            adapter.SetControl(held, true); yield return Step();
            adapter.SendMessage("OnApplicationPause", true); NoDrive();
            adapter.enabled = false; NoDrive();
        }
        [UnityTest] public IEnumerator MC_004_HandbrakeLocksAuthoredRearAxle()
        {
            var adapter = CreateCar(); adapter.SetControl(new VehicleControlIntent(1, 0, 0, true), true);
            yield return Step();
            for (int i = 2; i < 4; i++) { Assert.AreEqual(0, wheels[i].motorTorque); Assert.GreaterOrEqual(wheels[i].brakeTorque, 4500); }
        }
        [Test] public void MC_005_CoreAssemblyHasNoMotionCoreReference()
        {
            foreach (var reference in typeof(VehicleResources).Assembly.GetReferencedAssemblies())
                StringAssert.DoesNotContain("MotionCore", reference.Name);
        }
        [UnityTest] public IEnumerator VEH_NOISE_003_HornAndEngineUseCanonicalPressureAdapter()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/LastSignal/Scenes/Validation/WorldPopulationAcceptance.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            flow = Object.FindAnyObjectByType<SessionFlow>(); Assert.IsNotNull(flow);
            flow.Resume(); var cells = flow.GetComponent<WorldCellManager>(); string id = null;
            foreach (var candidate in cells.DefinedCellIds) { id = candidate; break; }
            Assert.IsNotNull(id); Assert.IsTrue(cells.Request(id));
            float timeout = Time.realtimeSinceStartup + 10;
            while (cells.State(id) != CellState.Ready && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.AreEqual(CellState.Ready, cells.State(id));
            var position = cells.Content(id).entry.position;
            var population = flow.GetComponent<LastSignal.AI.WorldPopulationManager>();
            long before = population.LastNoiseSequence;
            var producer = new VehicleNoiseProducer(flow.Noise, 1001, new VehicleTuning());
            Assert.IsTrue(producer.Horn(position)); Assert.IsTrue(flow.Noise.LastTrace.PressureForwarded);
            Assert.AreEqual(before + 1, population.LastNoiseSequence);
            producer.Advance(1, true, .5f, position);
            Assert.AreEqual(GameplayNoiseCategory.VehicleEngine, flow.Noise.LastTrace.Event.Category);
            Assert.IsTrue(flow.Noise.LastTrace.PressureForwarded); Assert.AreEqual(before + 2, population.LastNoiseSequence);
            Assert.IsTrue(producer.Impact(position, 1)); Assert.IsTrue(flow.Noise.LastTrace.PressureForwarded);
            Assert.AreEqual(before + 3, population.LastNoiseSequence);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { if (car) Object.Destroy(car); if (flow) flow.ReturnToMenu(); Time.timeScale = 1; yield return null; }
    }
}
#endif
