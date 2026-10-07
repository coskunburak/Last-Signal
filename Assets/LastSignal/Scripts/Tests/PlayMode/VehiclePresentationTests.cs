#if UNITY_EDITOR
using System.Collections;
using LastSignal.Vehicles;
using LastSignal.Vehicles.MotionCoreIntegration;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public sealed class VehiclePresentationTests
    {
        GameObject car, ground;
        MotionCoreVehiclePhysicsAdapter adapter;
        [SetUp] public void Setup()
        {
            Time.timeScale = 1;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/_Game/Vehicles/Prefabs/LS_Vehicle_UtilityPickup_01.prefab");
            car = Object.Instantiate(prefab, new Vector3(9000, .2f, 9000), Quaternion.identity);
            adapter = car.GetComponent<MotionCoreVehiclePhysicsAdapter>();
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(9000, -.5f, 9000);
            ground.transform.localScale = new Vector3(100, 1, 100);
            Physics.SyncTransforms();
        }
        [Test] public void ProductionPrefabBindsFourUniqueWheelsAndCockpit()
        {
            Assert.IsTrue(adapter.ValidateWheelPresentation(out var reason), reason);
            Assert.IsTrue(car.GetComponent<VehicleSteeringWheelPresenter>().Valid);
            string[] expected = { "FL_Mesh", "FR_Mesh", "BL_Mesh", "BR_Mesh" };
            for (int i = 0; i < 4; i++) Assert.AreEqual(expected[i], adapter.WheelVisual(i).name);
            Assert.AreEqual(1, car.GetComponents<MotionCore.Vehicle.Core.VehicleControllerBase>().Length);
        }
        [Test] public void MissingColliderFailsValidation()
        {
            var data = new SerializedObject(adapter);
            data.FindProperty("axles").GetArrayElementAtIndex(0).FindPropertyRelative("leftWheel").objectReferenceValue = null;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(adapter.ValidateWheelPresentation(out _));
        }
        [Test] public void DuplicateVisualFailsValidation()
        {
            var data = new SerializedObject(adapter);
            data.FindProperty("axles").GetArrayElementAtIndex(1).FindPropertyRelative("rightVisual").objectReferenceValue = adapter.WheelVisual(0);
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(adapter.ValidateWheelPresentation(out _));
        }
        [Test] public void BrakeAndReverseLampsUseSemanticIntent()
        {
            adapter.SetControl(new VehicleControlIntent(0, 1, 0, false), true);
            Assert.IsTrue(adapter.BrakeApplied); Assert.IsFalse(adapter.ReverseEngaged);
            adapter.SetControl(new VehicleControlIntent(0, 0, 0, false, 1), true);
            Assert.IsFalse(adapter.BrakeApplied); Assert.IsTrue(adapter.ReverseEngaged);
            adapter.ResetTransientInput(); Assert.IsFalse(adapter.ReverseEngaged);
        }
        [UnityTest] public IEnumerator RealWheelPoseDrivesSpinSteeringAndSuspensionPresentation()
        {
            yield return new WaitForSeconds(1);
            var origin = car.transform.position;
            adapter.SetControl(new VehicleControlIntent(1, 0, .3f, false), true);
            yield return new WaitForSeconds(2);
            yield return null; // Let the presentation LateUpdate finish in batchmode too.
            Assert.Greater(Vector3.Distance(origin, car.transform.position), .5f);
            Assert.Greater(adapter.Wheel(0).steerAngle, 0);
            Assert.AreEqual(0, adapter.Wheel(2).steerAngle);
            for (int i = 0; i < 4; i++)
            {
                Assert.Greater(adapter.Wheel(i).rpm, 0, "Forward spin wheel " + i);
                AssertPose(i);
            }
            var steering = car.GetComponent<VehicleSteeringWheelPresenter>();
            Assert.Greater(steering.VisualAngle, 0);
            Assert.AreEqual(VehicleSteeringWheelPresenter.EvaluateAngle(adapter.Wheel(0).steerAngle, adapter.Wheel(1).steerAngle, 32, 270), steering.VisualAngle, .01f);
            adapter.SetControl(new VehicleControlIntent(0, 1, 0, false), true);
            yield return new WaitForSeconds(2);
            adapter.SetControl(new VehicleControlIntent(0, 0, 0, false, 1), true);
            yield return new WaitForSeconds(3);
            yield return null;
            Assert.Less(adapter.ForwardSpeed, -.1f);
            for (int i = 0; i < 4; i++) { Assert.Less(adapter.Wheel(i).rpm, 0, "Reverse spin wheel " + i); AssertPose(i); }
            Assert.AreEqual(0, steering.VisualAngle, .01f);
        }
        void AssertPose(int i)
        {
            adapter.Wheel(i).GetWorldPose(out var position, out var rotation);
            Assert.Less(Vector3.Distance(position, adapter.WheelVisual(i).position), .03f, "Suspension position wheel " + i);
            Assert.Less(Quaternion.Angle(rotation, adapter.WheelVisual(i).rotation), 1, "Wheel rotation " + i);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { if (car) Object.Destroy(car); if (ground) Object.Destroy(ground); Time.timeScale = 1; yield return null; }
    }
}
#endif
