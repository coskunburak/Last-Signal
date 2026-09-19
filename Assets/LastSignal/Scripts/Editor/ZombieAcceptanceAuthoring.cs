using System;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace LastSignal.Editor
{
    public static class ZombieAcceptanceAuthoring
    {
        public const string ScenePath = "Assets/LastSignal/Scenes/ZombieAcceptance.unity";
        public const string Root = "Assets/LastSignal/Enemies/Zombie";
        public const string DefinitionPath = Root + "/Shambler.asset";
        public const string Evidence = "Docs/Implementation/S004/Evidence/20260918-P2";
        public static int AgentType()
        {
            for (int i = 0; i < NavMesh.GetSettingsCount(); i++)
            {
                var s = NavMesh.GetSettingsByIndex(i);
                if (NavMesh.GetSettingsNameFromID(s.agentTypeID) == "LastSignal Shambler") return s.agentTypeID;
            }
            var settings = NavMesh.CreateSettings(); settings.agentRadius = .32f; settings.agentHeight = 1.81f;
            settings.agentClimb = .25f; settings.agentSlope = 40;
            var project = new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("NavMeshProjectSettings"));
            var entries = project.FindProperty("m_Settings");
            int index = entries.arraySize - 1;
            var entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("agentRadius").floatValue = settings.agentRadius;
            entry.FindPropertyRelative("agentHeight").floatValue = settings.agentHeight;
            entry.FindPropertyRelative("agentClimb").floatValue = settings.agentClimb;
            entry.FindPropertyRelative("agentSlope").floatValue = settings.agentSlope;
            var names = project.FindProperty("m_SettingNames"); names.arraySize = entries.arraySize;
            names.GetArrayElementAtIndex(index).stringValue = "LastSignal Shambler";
            project.ApplyModifiedPropertiesWithoutUndo();
            return settings.agentTypeID;
        }
        [MenuItem("Last Signal/Zombie AI/Create acceptance arena")]
        public static void CreateArena()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Arena already exists; preserve authored work.");
            Directory.CreateDirectory(Evidence);
            var definition = ScriptableObject.CreateInstance<ZombieDefinition>();
            if (!AssetDatabase.LoadAssetAtPath<ZombieDefinition>(DefinitionPath)) AssetDatabase.CreateAsset(definition, DefinitionPath);
            else UnityEngine.Object.DestroyImmediate(definition);
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var world = new GameObject("NavigationWorld");
                Box(world.transform, "Ground", new Vector3(0, -.25f, 3), new Vector3(24, .5f, 26));
                Box(world.transform, "CornerWall", new Vector3(0, 1.5f, 4), new Vector3(.5f, 3, 8));
                Box(world.transform, "CornerReturn", new Vector3(2, 1.5f, 8), new Vector3(4.5f, 3, .5f));
                Box(world.transform, "LowCrate", new Vector3(-5, .4f, 3), new Vector3(2, .8f, 2));
                Box(world.transform, "RouteObstacle", new Vector3(6, .4f, 0), new Vector3(2, .8f, 3));
                Box(world.transform, "DisconnectedIsland", new Vector3(17, -.25f, 4), new Vector3(4, .5f, 4));
                var surface = world.AddComponent<NavMeshSurface>(); ConfigureSurface(surface); Bake(surface, "Acceptance");
                var light = new GameObject("Sun").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 2; light.transform.rotation = Quaternion.Euler(50, -30, 0);
                RenderSettings.ambientLight = new Color(.45f, .45f, .45f);
                var spawn = new GameObject("PlayerSpawn").transform; spawn.position = new Vector3(-5, .05f, 7); spawn.rotation = Quaternion.Euler(0, 180, 0);
                var session = new GameObject("Session").AddComponent<SessionFlow>();
                session.Configure(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Player.prefab"), spawn, Array.Empty<DoorInteractable>());
                var zombieSpawn = new GameObject("ZombieSpawn").transform; zombieSpawn.position = new Vector3(-5, 0, -4);
                var camera = new GameObject("EvidenceCamera").AddComponent<Camera>(); camera.enabled = false;
                camera.transform.position = new Vector3(-12, 12, -14); camera.transform.LookAt(new Vector3(0, 0, 3));
                EditorSceneManager.SaveScene(scene, ScenePath);
                var mesh = NavMesh.CalculateTriangulation();
                File.WriteAllText(Evidence + "/navmesh.txt", "Agent=" + surface.agentTypeID + " radius=.32 height=1.81 climb=.25 slope=40\nCollect=Children mask=Default geometry=PhysicsColliders voxel=.08 tile=128\nVertices=" + mesh.vertices.Length + " triangles=" + mesh.indices.Length / 3 + "\nGround 24x26, disconnected island x=17; wall/return, two low obstacles.\n");
                if (mesh.indices.Length == 0) throw new InvalidOperationException("Arena bake produced no triangles.");
            }
            finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); }
            AssetDatabase.SaveAssets();
        }
        public static void ConfigureSurface(NavMeshSurface surface)
        {
            surface.agentTypeID = AgentType(); surface.collectObjects = CollectObjects.Children;
            surface.layerMask = 1; surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize = true; surface.voxelSize = .08f; surface.overrideTileSize = true; surface.tileSize = 128;
        }
        public static void Bake(NavMeshSurface surface, string name)
        {
            surface.BuildNavMesh();
            string path = Root + "/" + name + "NavMesh.asset";
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(path)) throw new InvalidOperationException("Refusing to overwrite existing bake: " + path);
            AssetDatabase.CreateAsset(surface.navMeshData, path);
        }
        static void Box(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent);
            go.transform.position = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/M_AcceptanceGround.mat");
        }
    }
}
