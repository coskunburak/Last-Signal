using MotionCore.Vehicle.Camera;
using MotionCore.Vehicle.Core;
using MotionCore.Vehicle.Input;
using UnityEditor;
using UnityEngine;

namespace MotionCore.Vehicle.Editor
{
    /// <summary>
    /// Shared builder used by the core/arcade/simulation demo scene tools to spawn
    /// a fully wired WheelCollider vehicle (rigidbody, body mesh, four wheel
    /// colliders + visuals on two axles, input, and tuned suspension/friction).
    /// </summary>
    public static class DemoVehicleFactory
    {
        private const float WheelRadius = 0.4f;
        private const float HalfTrack = 0.85f;
        private const float FrontZ = 1.3f;
        private const float RearZ = -1.3f;
        private const float HubHeight = -0.35f;

        public static T BuildVehicle<T>(string name, Vector3 position, Color paint) where T : VehicleControllerBase
        {
            GameObject root = new GameObject(name);
            root.transform.position = position;

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 1300f;
            body.linearDamping = 0.05f;
            body.angularDamping = 0.2f;

            T controller = root.AddComponent<T>();
            root.AddComponent<VehicleInput>();

            GameObject centerOfMass = new GameObject("Center Of Mass");
            centerOfMass.transform.SetParent(root.transform, false);
            centerOfMass.transform.localPosition = new Vector3(0f, -0.3f, 0f);

            GameObject bodyVisual = new GameObject("Body");
            bodyVisual.transform.SetParent(root.transform, false);
            CreateBox(bodyVisual.transform, "Chassis", new Vector3(0f, 0.05f, 0f), new Vector3(1.9f, 0.45f, 4.0f), paint);
            CreateBox(bodyVisual.transform, "Cabin", new Vector3(0f, 0.45f, -0.35f), new Vector3(1.5f, 0.55f, 1.9f), new Color(0.08f, 0.12f, 0.16f));

            BoxCollider chassisCollider = root.AddComponent<BoxCollider>();
            chassisCollider.center = new Vector3(0f, 0.1f, 0f);
            chassisCollider.size = new Vector3(1.9f, 0.7f, 4.0f);

            Material tire = CreateMaterial("Demo Tire", new Color(0.015f, 0.015f, 0.018f));

            WheelCollider frontLeft = CreateWheelCollider(root.transform, "FL Wheel Collider", new Vector3(-HalfTrack, HubHeight, FrontZ));
            WheelCollider frontRight = CreateWheelCollider(root.transform, "FR Wheel Collider", new Vector3(HalfTrack, HubHeight, FrontZ));
            WheelCollider rearLeft = CreateWheelCollider(root.transform, "RL Wheel Collider", new Vector3(-HalfTrack, HubHeight, RearZ));
            WheelCollider rearRight = CreateWheelCollider(root.transform, "RR Wheel Collider", new Vector3(HalfTrack, HubHeight, RearZ));

            Transform frontLeftVisual = CreateWheelVisual(root.transform, "FL Wheel", new Vector3(-HalfTrack, HubHeight, FrontZ), tire);
            Transform frontRightVisual = CreateWheelVisual(root.transform, "FR Wheel", new Vector3(HalfTrack, HubHeight, FrontZ), tire);
            Transform rearLeftVisual = CreateWheelVisual(root.transform, "RL Wheel", new Vector3(-HalfTrack, HubHeight, RearZ), tire);
            Transform rearRightVisual = CreateWheelVisual(root.transform, "RR Wheel", new Vector3(HalfTrack, HubHeight, RearZ), tire);

            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("centerOfMass").objectReferenceValue = centerOfMass.transform;
            serialized.FindProperty("bodyMass").floatValue = 1300f;
            serialized.FindProperty("drivetrain").enumValueIndex = (int)DrivetrainLayout.RearWheelDrive;

            SerializedProperty axles = serialized.FindProperty("axles");
            axles.arraySize = 2;
            ConfigureAxle(axles.GetArrayElementAtIndex(0), frontLeft, frontRight, frontLeftVisual, frontRightVisual, steering: true, handbrake: false);
            ConfigureAxle(axles.GetArrayElementAtIndex(1), rearLeft, rearRight, rearLeftVisual, rearRightVisual, steering: false, handbrake: true);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return controller;
        }

        public static void CreateChaseCamera(string name, VehicleControllerBase target, float fieldOfView)
        {
            GameObject cameraObject = new GameObject(name);
            UnityEngine.Camera camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 600f;
            camera.fieldOfView = fieldOfView;
            cameraObject.AddComponent<AudioListener>();

            VehicleChaseCamera chaseCamera = cameraObject.AddComponent<VehicleChaseCamera>();
            SerializedObject serializedCamera = new SerializedObject(chaseCamera);
            serializedCamera.FindProperty("target").objectReferenceValue = target;
            serializedCamera.ApplyModifiedPropertiesWithoutUndo();

            cameraObject.transform.position = new Vector3(0f, 6f, -52f);
            cameraObject.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
        }

        public static void CreateLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 0.85f;

            GameObject sun = new GameObject("Directional Light");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        }

        public static Material CreateMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            material.name = name;
            material.color = color;
            return material;
        }

        private static void ConfigureAxle(SerializedProperty axle, WheelCollider left, WheelCollider right, Transform leftVisual, Transform rightVisual, bool steering, bool handbrake)
        {
            axle.FindPropertyRelative("leftWheel").objectReferenceValue = left;
            axle.FindPropertyRelative("rightWheel").objectReferenceValue = right;
            axle.FindPropertyRelative("leftVisual").objectReferenceValue = leftVisual;
            axle.FindPropertyRelative("rightVisual").objectReferenceValue = rightVisual;
            axle.FindPropertyRelative("steering").boolValue = steering;
            axle.FindPropertyRelative("handbrake").boolValue = handbrake;
            axle.FindPropertyRelative("antiRollForce").floatValue = 4500f;
        }

        private static WheelCollider CreateWheelCollider(Transform parent, string name, Vector3 localPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            WheelCollider wheel = go.AddComponent<WheelCollider>();
            wheel.radius = WheelRadius;
            wheel.suspensionDistance = 0.3f;
            wheel.center = Vector3.zero;
            wheel.mass = 20f;
            wheel.wheelDampingRate = 0.3f;
            wheel.forceAppPointDistance = 0f;

            // Softer than a sim car so the chassis visibly rolls/dives/squats — that
            // weight-transfer read is a big part of the GTA-style feel.
            JointSpring spring = wheel.suspensionSpring;
            spring.spring = 30000f;
            spring.damper = 3800f;
            spring.targetPosition = 0.5f;
            wheel.suspensionSpring = spring;

            WheelFrictionCurve forward = wheel.forwardFriction;
            forward.extremumSlip = 0.4f;
            forward.extremumValue = 1f;
            forward.asymptoteSlip = 0.8f;
            forward.asymptoteValue = 0.5f;
            forward.stiffness = 1.5f;
            wheel.forwardFriction = forward;

            // Planted in normal driving; the handbrake drops rear grip for slides.
            WheelFrictionCurve sideways = wheel.sidewaysFriction;
            sideways.extremumSlip = 0.3f;
            sideways.extremumValue = 1f;
            sideways.asymptoteSlip = 0.6f;
            sideways.asymptoteValue = 0.75f;
            sideways.stiffness = 1.7f;
            wheel.sidewaysFriction = sideways;

            return wheel;
        }

        private static Transform CreateWheelVisual(Transform parent, string name, Vector3 localPosition, Material tire)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = localPosition;

            GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mesh.name = "Mesh";
            mesh.transform.SetParent(pivot.transform, false);
            // Cylinder length runs along its local Y; rotate so it becomes the axle.
            mesh.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            mesh.transform.localScale = new Vector3(WheelRadius * 2f, 0.15f, WheelRadius * 2f);
            mesh.GetComponent<Renderer>().sharedMaterial = tire;
            Object.DestroyImmediate(mesh.GetComponent<Collider>());

            return pivot.transform;
        }

        private static void CreateBox(Transform parent, string name, Vector3 localPosition, Vector3 size, Color color)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = CreateMaterial(name + " Material", color);
            Object.DestroyImmediate(box.GetComponent<Collider>());
        }
    }
}
