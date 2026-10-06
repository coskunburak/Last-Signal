using System;
using System.Collections.Generic;
using System.IO;
using LastSignal.Inventory.Data;
using LastSignal.Vehicles;
using LastSignal.Vehicles.MotionCoreIntegration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LastSignal.VehicleAuthoring
{
    /// <summary>Explicit authoring command. Creates a separate candidate and test scene; never overwrites vendor or production scenes.</summary>
    public static class VehicleBenchmarkAuthoring
    {
        const string Root = "Assets/LastSignal/_Game/Vehicles";
        public const string PrefabPath = Root + "/Prefabs/LS_Vehicle_UtilityPickup_01.prefab";
        public const string ScenePath = "Assets/LastSignal/Scenes/Validation/VehicleBenchmark.unity";
        const string Vendor = "Assets/ThirdParty/Vehicle Entegrations/Pack_Pickup/Prefabs/Pickup.prefab";
        [MenuItem("Last Signal/VEH-001/Create Benchmark Candidate")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Editor must be idle.");
            if (File.Exists(PrefabPath) || File.Exists(ScenePath)) throw new IOException("Candidate exists; explicit review required before replacement.");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Vendor);
            if (!source) throw new InvalidOperationException("OlyPoly source missing.");
            Directory.CreateDirectory(Root + "/Prefabs"); Directory.CreateDirectory(Root + "/Definitions"); Directory.CreateDirectory(Root + "/Materials");
            AssetDatabase.Refresh();
            var original = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var definition = ScriptableObject.CreateInstance<VehicleDefinition>();
                var definitionData = new SerializedObject(definition);
                definitionData.FindProperty("fuelItem").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/LastSignal/Data/Items/Definitions/fuel.generator.asset");
                definitionData.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(definition, Root + "/Definitions/UtilityPickup.asset");
                var car = new GameObject("LS_Vehicle_UtilityPickup_01"); car.SetActive(false);
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
                PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                visual.name = "OlyPolyVisual"; visual.transform.SetParent(car.transform, false); visual.transform.localScale = Vector3.one * .5f;
                foreach (var script in visual.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(script);
                foreach (var collider in visual.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                foreach (var body in visual.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(body);
                var materials = new Dictionary<Material, Material>();
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
                {
                    var slots = renderer.sharedMaterials;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        var vendorMaterial = slots[i]; if (!vendorMaterial) throw new InvalidOperationException("Missing source material.");
                        if (!materials.TryGetValue(vendorMaterial, out var owned))
                        {
                            owned = new Material(Shader.Find("Universal Render Pipeline/Lit")); owned.name = "M_Vehicle_" + vendorMaterial.name;
                            if (vendorMaterial.mainTexture) owned.SetTexture("_BaseMap", vendorMaterial.mainTexture);
                            owned.SetColor("_BaseColor", new Color(.58f, .61f, .57f)); owned.SetFloat("_Smoothness", .3f);
                            if (vendorMaterial.name.IndexOf("Glass", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                owned.SetColor("_BaseColor", new Color(.3f, .37f, .4f, .22f)); owned.SetFloat("_Surface", 1);
                                owned.SetFloat("_SrcBlend", 5); owned.SetFloat("_DstBlend", 10); owned.SetFloat("_ZWrite", 0);
                                owned.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); owned.SetOverrideTag("RenderType", "Transparent"); owned.renderQueue = 3000;
                            }
                            AssetDatabase.CreateAsset(owned, Root + "/Materials/" + owned.name + ".mat"); materials.Add(vendorMaterial, owned);
                        }
                        slots[i] = owned;
                    }
                    renderer.sharedMaterials = slots;
                }
                var rb = car.AddComponent<Rigidbody>(); rb.mass = 1800;
                var adapter = car.AddComponent<MotionCoreVehiclePhysicsAdapter>();
                Box(car, new Vector3(0, .95f, 0), new Vector3(1.85f, .65f, 4.25f));
                Box(car, new Vector3(0, 1.65f, .25f), new Vector3(1.85f, 1.1f, 1.75f));
                var com = Anchor(car, "CenterOfMass", new Vector3(0, .65f, 0));
                Anchor(car, "DriverSeatAnchor", new Vector3(-.53f, .85f, .15f));
                Anchor(car, "DriverCameraAnchor", new Vector3(-.53f, 1.85f, .25f));
                Anchor(car, "DriverDoorway", new Vector3(-1.35f, 1, .15f));
                Anchor(car, "PassengerDoorway", new Vector3(1.35f, 1, .15f));
                Anchor(car, "DriverExit", new Vector3(-2, 0, .15f));
                Anchor(car, "PassengerExit", new Vector3(2, 0, .15f));
                Anchor(car, "TrunkAnchor", new Vector3(0, 1, -2.5f));
                Anchor(car, "FuelAnchor", new Vector3(-1.2f, 1, -1.5f));
                var data = new SerializedObject(adapter);
                data.FindProperty("bodyMass").floatValue = 1800;
                data.FindProperty("centerOfMass").objectReferenceValue = com;
                data.FindProperty("maxSpeedKph").floatValue = 64.8f;
                data.FindProperty("maxReverseSpeedKph").floatValue = 18;
                data.FindProperty("downforce").floatValue = 2;
                data.FindProperty("handbrakeGripFactor").floatValue = .9f;
                var axles = data.FindProperty("axles"); axles.arraySize = 2;
                string[] names = { "FL_Mesh", "FR_Mesh", "BL_Mesh", "BR_Mesh" };
                for (int i = 0; i < 4; i++)
                {
                    Transform wheelVisual = null;
                    foreach (var t in visual.GetComponentsInChildren<Transform>(true)) if (t.name == names[i]) wheelVisual = t;
                    if (!wheelVisual) throw new InvalidOperationException("Missing wheel: " + names[i]);
                    var wheel = Anchor(car, "Physics_" + names[i], car.transform.InverseTransformPoint(wheelVisual.position)).gameObject.AddComponent<WheelCollider>();
                    wheel.radius = .51f; wheel.suspensionDistance = .25f; wheel.mass = 25;
                    var spring = wheel.suspensionSpring; spring.spring = 35000; spring.damper = 4500; spring.targetPosition = .5f; wheel.suspensionSpring = spring;
                    var axle = axles.GetArrayElementAtIndex(i / 2); bool left = i % 2 == 0;
                    axle.FindPropertyRelative(left ? "leftWheel" : "rightWheel").objectReferenceValue = wheel;
                    axle.FindPropertyRelative(left ? "leftVisual" : "rightVisual").objectReferenceValue = wheelVisual;
                    axle.FindPropertyRelative("steering").boolValue = i < 2;
                    axle.FindPropertyRelative("handbrake").boolValue = i >= 2;
                    axle.FindPropertyRelative("antiRollForce").floatValue = 4500;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                car.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(car, PrefabPath); Object.DestroyImmediate(car);
                var testCar = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
                testCar.transform.position = new Vector3(0, .2f, 0);
                var ground = new Material(Shader.Find("Universal Render Pipeline/Lit")); ground.color = new Color(.18f, .21f, .2f);
                AssetDatabase.CreateAsset(ground, Root + "/Materials/M_BenchmarkGround.mat");
                Primitive("FlatAsphalt_Straight100m", new Vector3(0, -.15f, 45), new Vector3(24, .3f, 130), ground);
                Primitive("BrakingZone", new Vector3(0, .005f, 35), new Vector3(8, .01f, .1f), ground);
                for (int i = 0; i < 5; i++) Primitive("Slalom_" + i, new Vector3(i % 2 == 0 ? -3 : 3, .5f, 10 + i * 8), Vector3.one, ground);
                Primitive("CollisionBarrier", new Vector3(0, 1, 100), new Vector3(10, 2, .5f), ground);
                Primitive("ExitWallTest", new Vector3(-4, 1, 0), new Vector3(.3f, 2, 8), ground);
                Primitive("Curb", new Vector3(7, .1f, 10), new Vector3(3, .2f, .3f), ground);
                foreach (int angle in new[] { 10, 20 })
                {
                    var slope = Primitive("Slope" + angle, new Vector3(18 + angle, 2, 20), new Vector3(6, .3f, 16), ground);
                    slope.transform.rotation = Quaternion.Euler(-angle, 0, 0);
                    Primitive("SlopeApproach" + angle, new Vector3(18 + angle, -.15f, 0), new Vector3(7, .3f, 25), ground);
                }
                var sun = new GameObject("DayLighting").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.2f; sun.transform.rotation = Quaternion.Euler(45, -35, 0);
                var camera = new GameObject("BenchmarkInspectionCamera").AddComponent<Camera>(); camera.transform.position = new Vector3(-7, 4, -7); camera.transform.LookAt(new Vector3(0, 1, 0)); camera.fieldOfView = 60;
                // Inspection camera is a benchmark tool, not the production driving camera.
                EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.SaveAssets();
                Debug.Log("VEH-001 candidate authored. Driving, occupancy, art and surface acceptance remain NOT_RUN.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            }
        }
        static Transform Anchor(GameObject parent, string name, Vector3 position)
        { var child = new GameObject(name); child.transform.SetParent(parent.transform, false); child.transform.localPosition = position; return child.transform; }
        static void Box(GameObject owner, Vector3 center, Vector3 size)
        { var box = owner.AddComponent<BoxCollider>(); box.center = center; box.size = size; }
        static GameObject Primitive(string name, Vector3 position, Vector3 scale, Material material)
        { var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.position = position; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = material; return go; }
    }
}
