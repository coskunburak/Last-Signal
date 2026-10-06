using System;
using System.IO;
using LastSignal.Vehicles;
using LastSignal.Vehicles.MotionCoreIntegration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LastSignal.VehicleAuthoring
{
    public static class VehicleDrivingAuthoring
    {
        const string Prefab = "Assets/LastSignal/_Game/Vehicles/Prefabs/LS_Vehicle_UtilityPickup_01.prefab";
        const string Audio = "Assets/ThirdParty/Vehicle Entegrations/Essentials_Series_NOX_SOUND/Vehicle_Essentials_NOX_SOUND/Vehicle_Essential_Car/";
        [MenuItem("Last Signal/VEH-001/Update Driving Presentation")]
        public static void UpdatePresentation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Editor must be idle.");
            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var adapter = root.GetComponent<MotionCoreVehiclePhysicsAdapter>();
                if (!adapter || !adapter.ValidateWheelPresentation(out var reason))
                    throw new InvalidOperationException("Invalid four-wheel mapping.");
                var steering = Find(root, "SteeringWheel");
                // Keep seat and eye on the same vertical axis so yaw cannot orbit the eye through a pillar.
                Find(root, "DriverSeatAnchor").localPosition = new Vector3(-.53f, .85f, -.1f);
                Find(root, "DriverCameraAnchor").localPosition = new Vector3(-.53f, 1.75f, -.1f);
                var pivot = Find(root, "SteeringWheelPivot", false);
                if (!pivot)
                {
                    pivot = new GameObject("SteeringWheelPivot").transform;
                    pivot.SetParent(steering.parent, false);
                    // Rim center and plane axis measured from the source vertices, in source meters.
                    pivot.position = steering.TransformPoint(new Vector3(-.0007464f, .08939754f, -.25195767f));
                    steering.SetParent(pivot, true);
                }
                var presenter = root.GetComponent<VehicleSteeringWheelPresenter>();
                if (!presenter) presenter = root.AddComponent<VehicleSteeringWheelPresenter>();
                var data = new SerializedObject(presenter);
                data.FindProperty("pivot").objectReferenceValue = pivot;
                data.FindProperty("frontLeft").objectReferenceValue = adapter.Wheel(0);
                data.FindProperty("frontRight").objectReferenceValue = adapter.Wheel(1);
                data.FindProperty("localAxis").vector3Value = new Vector3(0, .3310264f, -.9436214f);
                data.FindProperty("roadWheelFullLock").floatValue = 32;
                data.FindProperty("visualFullLock").floatValue = 270;
                data.ApplyModifiedPropertiesWithoutUndo();
                if (!root.GetComponent<VehicleSurfaceProvider>()) root.AddComponent<VehicleSurfaceProvider>();
                var lamps = root.GetComponent<VehicleLightPresenter>();
                if (!lamps) lamps = root.AddComponent<VehicleLightPresenter>();
                var lampData = new SerializedObject(lamps);
                ConfigureLamps(root, lampData, "Brake", new Color(1, .025f, .01f), .83f);
                ConfigureLamps(root, lampData, "Reverse", new Color(1, .94f, .8f), .64f);
                lampData.ApplyModifiedPropertiesWithoutUndo();
                var audio = new SerializedObject(root.GetComponent<VehicleAudioPresenter>());
                audio.FindProperty("loadedEngine").objectReferenceValue = Clip("Vehicle_Car_Engine_2000_RPM_Front_Exterior_Loop_Mono.wav");
                audio.FindProperty("rolling").objectReferenceValue = Clip("Vehicle_Car_Drive_Exterior_Short_Loop_Mono.wav");
                audio.ApplyModifiedPropertiesWithoutUndo();
                Validate(root);
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
                AssetDatabase.SaveAssets();
                Debug.Log("VEH-001 owned prefab driving presentation updated. Visual/handling acceptance remains NOT_RUN.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        static AudioClip Clip(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + name);
            if (!clip) throw new FileNotFoundException(name);
            return clip;
        }
        static void ConfigureLamps(GameObject root, SerializedObject data, string kind, Color color, float x)
        {
            string field = char.ToLowerInvariant(kind[0]) + kind.Substring(1);
            var lights = data.FindProperty(field + "Lights"); lights.arraySize = 2;
            var lenses = data.FindProperty(field + "Lenses"); lenses.arraySize = 2;
            string path = "Assets/LastSignal/_Game/Vehicles/Materials/M_Vehicle_" + kind + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetColor("_BaseColor", color); material.SetColor("_EmissionColor", color * 2);
                material.EnableKeyword("_EMISSION"); AssetDatabase.CreateAsset(material, path);
            }
            for (int i = 0; i < 2; i++)
            {
                string name = kind + "Lens_" + (i == 0 ? "L" : "R");
                var lens = Find(root, name, false);
                if (!lens)
                {
                    lens = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                    lens.name = name; lens.SetParent(root.transform, false);
                    Object.DestroyImmediate(lens.GetComponent<Collider>());
                    lens.localPosition = new Vector3(i == 0 ? -x : x, 1.2f, -2.36f);
                    lens.localScale = new Vector3(.14f, .12f, .035f);
                }
                var renderer = lens.GetComponent<Renderer>(); renderer.sharedMaterial = material; renderer.enabled = false;
                var light = lens.GetComponent<Light>(); if (!light) light = lens.gameObject.AddComponent<Light>();
                light.type = LightType.Point; light.color = color; light.range = 2.5f; light.intensity = .8f;
                light.shadows = LightShadows.None; light.enabled = false;
                lights.GetArrayElementAtIndex(i).objectReferenceValue = light;
                lenses.GetArrayElementAtIndex(i).objectReferenceValue = renderer;
            }
        }
        public static void Validate(GameObject root)
        {
            var adapter = root.GetComponent<MotionCoreVehiclePhysicsAdapter>();
            if (!adapter || !adapter.ValidateWheelPresentation(out _)) throw new InvalidOperationException("Invalid wheel mapping.");
            var body = root.GetComponent<Rigidbody>();
            if (!body || !float.IsFinite(body.mass) || body.mass <= 0 || root.transform.localScale != Vector3.one)
                throw new InvalidOperationException("Invalid vehicle body mass/scale.");
            Find(root, "DriverSeatAnchor"); Find(root, "DriverCameraAnchor");
            var steering = root.GetComponent<VehicleSteeringWheelPresenter>();
            if (!steering || !steering.Valid) throw new InvalidOperationException("Cockpit steering is required.");
        }
        [MenuItem("Last Signal/VEH-001/Add Resident Driving Route")]
        public static void AddResidentRoute()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty ||
                scene.path != "Assets/LastSignal/Scenes/Production/S013Cabin.unity")
                throw new InvalidOperationException("Open the saved production scene with no pending scene edits.");
            if (GameObject.Find("VEH-001 Resident Driving Route")) return;
            var root = new GameObject("VEH-001 Resident Driving Route");
            Undo.RegisterCreatedObjectUndo(root, "Add resident driving route");
            var asphalt = RouteMaterial("Asphalt", new Color(.15f, .16f, .16f));
            var gravel = RouteMaterial("Gravel", new Color(.34f, .29f, .22f));
            var curb = RouteMaterial("Curb", new Color(.45f, .44f, .4f));
            RouteBlock(root, "Resident hardstanding", new Vector3(-300, .01f, -125), new Vector3(100, .02f, 10), asphalt);
            var loose = RouteBlock(root, "Gravel contact zone", new Vector3(-300, .035f, -125), new Vector3(20, .03f, 10), gravel);
            loose.AddComponent<VehicleSurface>();
            RouteBlock(root, "Low suspension curb 12cm", new Vector3(-277, .08f, -125), new Vector3(.35f, .12f, 10), curb);
            RouteBlock(root, "Parking hardstanding", new Vector3(-258, .01f, -125), new Vector3(14, .02f, 14), asphalt);
            RouteBlock(root, "Low speed impact post", new Vector3(-255, .45f, -130), new Vector3(.6f, .9f, .6f), curb);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        static Material RouteMaterial(string name, Color color)
        {
            string path = "Assets/LastSignal/_Game/Vehicles/Materials/M_Route_" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", .08f);
            AssetDatabase.CreateAsset(material, path); return material;
        }
        static GameObject RouteBlock(GameObject root, string name, Vector3 position, Vector3 size, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube); block.name = name;
            block.transform.SetParent(root.transform); block.transform.position = position; block.transform.localScale = size;
            block.GetComponent<Renderer>().sharedMaterial = material; return block;
        }
        static Transform Find(GameObject root, string name, bool required = true)
        {
            Transform found = null;
            foreach (var candidate in root.GetComponentsInChildren<Transform>(true))
                if (candidate.name == name)
                {
                    if (found) throw new InvalidOperationException("Ambiguous transform " + name);
                    found = candidate;
                }
            if (!found && required) throw new InvalidOperationException("Missing " + name);
            return found;
        }
    }
}
