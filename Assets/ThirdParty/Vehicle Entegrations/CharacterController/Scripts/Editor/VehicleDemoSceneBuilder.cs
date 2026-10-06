using MotionCore.Vehicle.Controllers;
using MotionCore.Vehicle.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MotionCore.Vehicle.Editor
{
    public static class VehicleDemoSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/VehicleDemo.unity";

        [MenuItem("Tools/MotionCore Auto/Build Vehicle Demo Scene")]
        public static void BuildDemoScene()
        {
            Scene previousScene = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            scene.name = "VehicleDemo";

            DemoVehicleFactory.CreateLighting();
            CreateTrack();
            VehicleControllerBase vehicle = DemoVehicleFactory.BuildVehicle<BasicCarController>(
                "Demo Basic Vehicle", new Vector3(0f, 1f, -42f), new Color(0.04f, 0.28f, 0.9f));
            DemoVehicleFactory.CreateChaseCamera("Vehicle Chase Camera", vehicle, 68f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();

            if (previousScene.IsValid())
            {
                SceneManager.SetActiveScene(previousScene);
            }

            EditorSceneManager.CloseScene(scene, true);
            Debug.Log($"Vehicle demo scene saved to {ScenePath}");
        }

        private static void CreateTrack()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Demo Asphalt";
            ground.transform.position = new Vector3(0f, -0.08f, 0f);
            ground.transform.localScale = new Vector3(80f, 0.15f, 140f);
            ground.GetComponent<Renderer>().sharedMaterial = DemoVehicleFactory.CreateMaterial("Demo Asphalt Material", new Color(0.08f, 0.085f, 0.09f));

            CreateBarrier("Left Barrier", new Vector3(-18f, 0.7f, 0f), new Vector3(0.5f, 1.4f, 115f));
            CreateBarrier("Right Barrier", new Vector3(18f, 0.7f, 0f), new Vector3(0.5f, 1.4f, 115f));

            for (int i = -5; i <= 5; i++)
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = $"Lane Marker {i + 6:00}";
                marker.transform.position = new Vector3(0f, 0.02f, i * 10f);
                marker.transform.localScale = new Vector3(0.28f, 0.03f, 4f);
                marker.GetComponent<Renderer>().sharedMaterial = DemoVehicleFactory.CreateMaterial("Demo Lane Marker Material", new Color(0.95f, 0.9f, 0.68f));
            }
        }

        private static void CreateBarrier(string name, Vector3 position, Vector3 scale)
        {
            GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrier.name = name;
            barrier.transform.position = position;
            barrier.transform.localScale = scale;
            barrier.GetComponent<Renderer>().sharedMaterial = DemoVehicleFactory.CreateMaterial("Demo Barrier Material", new Color(0.7f, 0.72f, 0.76f));
        }

        private static void AddSceneToBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == ScenePath)
                {
                    scenes[i].enabled = true;
                    EditorBuildSettings.scenes = scenes;
                    return;
                }
            }

            ArrayUtility.Add(ref scenes, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes;
        }
    }
}
