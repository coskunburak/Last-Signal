using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastSignal.Editor
{
    public static class S001Project
    {
        public const string ScenePath = "Assets/LastSignal/Scenes/S001Acceptance.unity";
        public const string PlayerPath = "Assets/LastSignal/Prefabs/Player.prefab";
        public const string DoorPath = "Assets/LastSignal/Prefabs/Door.prefab";
        static string Evidence => Environment.GetEnvironmentVariable("LAST_SIGNAL_EVIDENCE") ?? "Docs/Implementation/S001/Evidence/20260917-S001";
        static Material floor, concrete, teal, amber, red;

        // Scene-only recovery: never regenerate the shared player, door or materials.
        public static void RecoverMissingScene()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Exit Play Mode and save the current scene before recovery.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Refusing to overwrite " + ScenePath);
            floor = ExistingMaterial("Floor"); concrete = ExistingMaterial("Concrete");
            teal = ExistingMaterial("Passable"); amber = ExistingMaterial("Door"); red = ExistingMaterial("Blocked");
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath) || !AssetDatabase.LoadAssetAtPath<GameObject>(DoorPath))
                throw new InvalidOperationException("Recovery requires existing player and door prefabs.");
            Directory.CreateDirectory("Assets/LastSignal/Scenes");
            CreateScene();
        }

        static Material ExistingMaterial(string name)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/LastSignal/Materials/" + name + ".mat");
            if (!material) throw new InvalidOperationException("Missing existing material: " + name);
            return material;
        }

        [MenuItem("Last Signal/S001/Create acceptance assets")]
        public static void CreateAssets()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before authoring.");
            if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene first.");
            Directory.CreateDirectory("Assets/LastSignal/Prefabs");
            Directory.CreateDirectory("Assets/LastSignal/Scenes");
            Directory.CreateDirectory("Assets/LastSignal/Materials");
            floor = Material("Floor", new Color(.13f, .18f, .21f));
            concrete = Material("Concrete", new Color(.44f, .5f, .51f));
            teal = Material("Passable", new Color(.08f, .62f, .57f));
            amber = Material("Door", new Color(.95f, .55f, .12f));
            red = Material("Blocked", new Color(.78f, .18f, .16f));
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (!actions || actions.FindAction("Player/Pause") == null) throw new InvalidOperationException("Player/Pause missing.");
            var player = new GameObject("Player");
            player.SetActive(false);
            player.layer = 2; // Ignore Raycast: world queries still include ALL blocking world colliders.
            var capsule = player.AddComponent<CharacterController>();
            capsule.height = 1.8f; capsule.radius = .3f; capsule.center = Vector3.up * .9f;
            capsule.stepOffset = .3f; capsule.slopeLimit = 45; capsule.skinWidth = .025f; capsule.minMoveDistance = 0;
            var cameraObject = new GameObject("View", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera"; cameraObject.layer = 2;
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = Vector3.up * 1.62f;
            var camera = cameraObject.GetComponent<Camera>();
            camera.nearClipPlane = .05f; camera.farClipPlane = 150; camera.fieldOfView = 75;
            var input = player.AddComponent<PlayerInputReader>(); input.Configure(actions);
            var stance = player.AddComponent<PlayerStance>(); stance.Configure(input, capsule, camera.transform);
            player.AddComponent<FirstPersonLook>().Configure(input, camera);
            player.AddComponent<FirstPersonMotor>().Configure(input, capsule, stance);
            player.AddComponent<InteractionController>().Configure(input, camera.transform);
            player.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPath);
            UnityEngine.Object.DestroyImmediate(player);

            var door = new GameObject("Door");
            var hinge = new GameObject("Hinge").transform; hinge.SetParent(door.transform, false);
            var leaf = Box("Leaf", new Vector3(.65f, 1.1f, 0), new Vector3(1.3f, 2.1f, .12f), amber);
            leaf.transform.SetParent(hinge, false);
            door.AddComponent<DoorInteractable>().Configure(hinge, leaf.GetComponent<BoxCollider>());
            PrefabUtility.SaveAsPrefabAsset(door, DoorPath);
            UnityEngine.Object.DestroyImmediate(door);
            AssetDatabase.SaveAssets();
            CreateScene();
            Validate();
        }

        static Material Material(string name, Color color)
        {
            string path = "Assets/LastSignal/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }
        static GameObject Box(string name, Vector3 position, Vector3 size, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name; obj.transform.position = position; obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }
        static void Label(string text, Vector3 position)
        {
            var obj = new GameObject(text, typeof(TextMesh)); obj.transform.position = position;
            var label = obj.GetComponent<TextMesh>(); label.text = text; label.characterSize = .12f;
            label.fontSize = 40; label.anchor = TextAnchor.MiddleCenter; label.color = Color.white;
        }
        static void Ramp(string name, Vector3 start, float degrees, float length, Material material)
        {
            float radians = degrees * Mathf.Deg2Rad;
            var ramp = Box(name, start + new Vector3(0, Mathf.Sin(radians) * length * .5f - .1f, Mathf.Cos(radians) * length * .5f),
                new Vector3(2.4f, .2f, length), material);
            ramp.transform.rotation = Quaternion.Euler(-degrees, 0, 0);
        }
        static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.55f, .6f, .65f);
            var light = new GameObject("Sun", typeof(Light)); light.transform.rotation = Quaternion.Euler(50, -30, 0);
            light.GetComponent<Light>().type = LightType.Directional; light.GetComponent<Light>().intensity = 1.5f;
            Box("Ground 40 x 40 m", new Vector3(0, -.25f, 10), new Vector3(40, .5f, 40), floor);
            Box("North wall", new Vector3(0, 2, 30), new Vector3(40, 4, .3f), concrete);
            Box("South wall", new Vector3(0, 2, -10), new Vector3(40, 4, .3f), concrete);
            Box("West wall", new Vector3(-20, 2, 10), new Vector3(.3f, 4, 40), concrete);
            Box("East wall", new Vector3(20, 2, 10), new Vector3(.3f, 4, 40), concrete);
            Box("Step 0.15 m", new Vector3(-10, .075f, 3), new Vector3(2.4f, .15f, 1.5f), teal);
            Box("Step 0.25 m rise", new Vector3(-10, .2f, 4.5f), new Vector3(2.4f, .4f, 1.5f), teal);
            Label("01 / STEPS  0.15 + 0.25 m", new Vector3(-10, 2.5f, 6));
            Ramp("Ramp 30 degrees", new Vector3(-5, 0, 3), 30, 5, teal);
            Label("02 / SLOPE  30 deg", new Vector3(-5, 3.5f, 8));
            Ramp("Ramp 60 degrees - blocked", new Vector3(0, 0, 5), 60, 4, red);
            Label("03 / LIMIT  60 deg", new Vector3(0, 4.3f, 8));
            Box("Corridor left", new Vector3(4.4f, 1, 5), new Vector3(.2f, 2, 5), concrete);
            Box("Corridor right", new Vector3(5.6f, 1, 5), new Vector3(.2f, 2, 5), concrete);
            Label("04 / NARROW  1.0 m", new Vector3(5, 2.5f, 8));
            Box("Tunnel roof clearance 1.3 m", new Vector3(10, 1.45f, 5), new Vector3(2.4f, .3f, 4), amber);
            Box("Tunnel left", new Vector3(8.7f, .75f, 5), new Vector3(.2f, 1.5f, 4), concrete);
            Box("Tunnel right", new Vector3(11.3f, .75f, 5), new Vector3(.2f, 1.5f, 4), concrete);
            Label("05 / CROUCH  1.3 m", new Vector3(10, 2.5f, 8));
            Ramp("Access to drop deck 30 deg", new Vector3(-10, 0, 14), 30, 6, teal);
            Box("Drop platform 3 m", new Vector3(-10, 2.9f, 21), new Vector3(3, .2f, 4), teal);
            Label("06 / DROP  3 m", new Vector3(-10, 4.5f, 23));
            var door = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DoorPath));
            door.name = "Door - visible"; door.transform.position = new Vector3(-.65f, 0, 1.8f);
            Box("Door threshold 0.10 m", new Vector3(0, .05f, 1.8f), new Vector3(1.6f, .1f, .35f), concrete);
            // Leaf begins 0.05 m above floor; threshold ends before leaf swing footprint.
            door.transform.position += Vector3.up * .1f;
            Label("E / OPEN - CLOSE", new Vector3(0, 2.65f, 2));
            var hidden = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DoorPath));
            hidden.name = "Door - occluded"; hidden.transform.position = new Vector3(14, 0, 15);
            Box("Interaction blocker", new Vector3(14.65f, 1.5f, 14.5f), new Vector3(3, 3, .3f), red);
            Label("07 / OCCLUSION", new Vector3(14.5f, 3.5f, 15));
            var spawn = new GameObject("Spawn - feet at 0.05 m").transform; spawn.position = new Vector3(0, .05f, 0);
            var session = new GameObject("Session", typeof(SessionFlow)).GetComponent<SessionFlow>();
            session.Configure(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath), spawn,
                new[] { door.GetComponent<DoorInteractable>(), hidden.GetComponent<DoorInteractable>() });
            CreateHud(session);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("S001 scene saved: " + ScenePath + "; dirty=" + scene.isDirty);
        }
        static Text Text(Transform parent, string name, string value, Vector2 anchor, Vector2 position, Vector2 size, int fontSize)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var text = obj.GetComponent<Text>(); text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize; text.alignment = TextAnchor.MiddleCenter; text.color = Color.white; text.raycastTarget = false;
            return text;
        }
        static Button Button(Transform parent, string label, float y)
        {
            var obj = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(280, 50); rect.anchoredPosition = new Vector2(0, y);
            obj.GetComponent<Image>().color = new Color(.08f, .35f, .36f);
            Text(obj.transform, "Label", label, new Vector2(.5f, .5f), Vector2.zero, new Vector2(280, 50), 22);
            return obj.GetComponent<Button>();
        }
        static void CreateHud(SessionFlow session)
        {
            var obj = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            obj.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = obj.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 900); scaler.matchWidthOrHeight = .5f;
            Text(obj.transform, "Title", "LAST SIGNAL  /  S001 MOVEMENT LAB", new Vector2(.5f, 1), new Vector2(0, -40), new Vector2(1200, 60), 25);
            var status = Text(obj.transform, "Controls", "", new Vector2(.5f, 0), new Vector2(0, 45), new Vector2(1400, 60), 19);
            var prompt = Text(obj.transform, "Prompt", "", new Vector2(.5f, .5f), new Vector2(0, -65), new Vector2(600, 60), 24);
            var crosshair = Text(obj.transform, "Crosshair", "+", new Vector2(.5f, .5f), Vector2.zero, new Vector2(40, 40), 25);
            var panel = new GameObject("Menu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(obj.transform, false); panel.GetComponent<RectTransform>().sizeDelta = new Vector2(520, 380);
            panel.GetComponent<Image>().color = new Color(.03f, .06f, .08f, .98f);
            var title = Text(panel.transform, "Title", "", new Vector2(.5f, .5f), new Vector2(0, 130), new Vector2(500, 60), 28);
            var start = Button(panel.transform, "Oyunu başlat", 30);
            var resume = Button(panel.transform, "Devam et", 30);
            var menu = Button(panel.transform, "Ana menüye dön", -40);
            obj.AddComponent<AcceptanceHud>().Configure(session, prompt, status, crosshair, panel, title, start, resume, menu);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
        [MenuItem("Last Signal/S001/Validate serialized assets")]
        public static void Validate()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            int missing = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
            foreach (var path in new[] { PlayerPath, DoorPath })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!prefab) throw new Exception("Missing prefab " + path);
                foreach (var component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (!component) { missing++; continue; }
                    var so = new SerializedObject(component); var property = so.GetIterator();
                    while (property.NextVisible(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                            throw new Exception("Missing reference: " + path + " " + component.GetType().Name + "." + property.propertyPath);
                }
            }
            if (missing != 0 || scene.isDirty) throw new Exception("Invalid scene: missing=" + missing + " dirty=" + scene.isDirty);
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Evidence + "/asset-validation.txt", "PASS: " + Application.unityVersion + "\nProject=" + Directory.GetCurrentDirectory() +
                "\nScene=" + scene.path + "\nMissing scripts=0; required prefab references present; scene dirty=false\n");
            Debug.Log("S001_ASSET_VALIDATION PASS");
        }
        [MenuItem("Last Signal/S001/Build macOS Standalone")]
        public static void Build()
        {
            Validate();
            var scenes = new[] { 
                "Assets/LastSignal/Scenes/S001Acceptance.unity",
                "Assets/LastSignal/Scenes/CombatAcceptance.unity"
            };
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                target = BuildTarget.StandaloneOSX,
                locationPathName = "Builds/S001/Last Signal.app",
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            File.WriteAllText(Evidence + "/build-result.txt", report.summary.result + "\n" + report.summary.outputPath +
                "\nerrors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings + "\nUTC=" + DateTime.UtcNow.ToString("O"));
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("S001 build failed.");
        }
    }
}
