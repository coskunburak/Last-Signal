using System;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace LastSignal.Editor
{
    // Separate runtime wrapper preserves the B0B presentation-only prefab and its contracts.
    public static class ZombieRuntimeAuthoring
    {
        public const string PrefabPath = ZombieAcceptanceAuthoring.Root + "/Prefabs/LS_Zombie_Runtime.prefab";
        public static void Compose()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring.");
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))
            {
                var root = new GameObject("LS_Zombie_Runtime"); root.SetActive(false);
                try
                {
                    // Ignore Raycast excludes the body from existing rifle queries, not physical collision.
                    root.layer = 2;
                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ZombieAcceptanceAuthoring.Root + "/Prefabs/LS_Zombie_Shambler.prefab"));
                    visual.transform.SetParent(root.transform, false);
                    var agent = root.AddComponent<NavMeshAgent>(); agent.enabled = false;
                    agent.agentTypeID = ZombieAcceptanceAuthoring.AgentType(); agent.radius = .32f; agent.height = 1.81f;
                    agent.baseOffset = 0; agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                    var body = root.AddComponent<CapsuleCollider>(); body.radius = .32f; body.height = 1.81f; body.center = Vector3.up * .905f;
                    var controller = root.AddComponent<ZombieController>();
                    controller.Configure(AssetDatabase.LoadAssetAtPath<ZombieDefinition>(ZombieAcceptanceAuthoring.DefinitionPath));
                    var melee = new GameObject("MeleeOrigin").transform; melee.SetParent(root.transform, false);
                    melee.localPosition = new Vector3(0, 1.05f, 0); controller.ConfigureCombat(melee);
                    root.AddComponent<ZombieAnimationPresenter>().Configure(visual.GetComponentInChildren<Animator>());
                    root.SetActive(true); PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            Integrate(ZombieAcceptanceAuthoring.ScenePath, new Vector3(-5,0,-4), false);
            AssetDatabase.SaveAssets();
        }
        public static void IntegrateNormal() => Integrate("Assets/Scenes/SampleScene.unity", new Vector3(-5,0,7), true);
        static void Integrate(string path, Vector3 position, bool bake)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring.");
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            var previous = SceneManager.GetActiveScene(); SceneManager.SetActiveScene(scene);
            try
            {
                SessionFlow session = null;
                foreach (var root in scene.GetRootGameObjects())
                    if (root.TryGetComponent<SessionFlow>(out var found)) session = found;
                if (!session) throw new InvalidOperationException("Scene has no owned SessionFlow.");
                var encounter = session.GetComponent<ZombieEncounter>();
                if (!encounter)
                {
                    encounter = session.gameObject.AddComponent<ZombieEncounter>();
                    var spawn = new GameObject("SessionZombieSpawn").transform; spawn.SetParent(session.transform);
                    spawn.position = position; spawn.rotation = Quaternion.Euler(0,bake?180:0,0);
                    encounter.Configure(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<ZombieController>(),spawn);
                }
                session.ConfigureEncounter(encounter);
                if (bake)
                {
                    GameObject world = null;
                    foreach (var root in scene.GetRootGameObjects()) if (root.name == "SessionNavigationWorld") world = root;
                    if (!world)
                    {
                        world = new GameObject("SessionNavigationWorld");
                        // CollectObjects.All spans the entire main Editor stage, including other open scenes.
                        // Own only this scene's collider roots; preserve world transforms and object references.
                        foreach (var root in scene.GetRootGameObjects())
                            if (root != world && root != session.gameObject && root.GetComponentsInChildren<Collider>().Length > 0)
                                root.transform.SetParent(world.transform, true);
                    }
                    var prior = session.GetComponent<NavMeshSurface>();
                    if (prior) UnityEngine.Object.DestroyImmediate(prior);
                    var surface = world.GetComponent<NavMeshSurface>();
                    if (!surface) surface = world.AddComponent<NavMeshSurface>();
                    ZombieAcceptanceAuthoring.ConfigureSurface(surface);
                    if (!surface.navMeshData)
                    {
                        surface.BuildNavMesh();
                        surface.navMeshData.name = "NormalNavMesh";
                        var asset = ZombieAcceptanceAuthoring.Root + "/NormalNavMesh.asset";
                        var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(asset);
                        if (existing)
                        {
                            var generated = surface.navMeshData; surface.RemoveData();
                            EditorUtility.CopySerialized(generated, existing); surface.navMeshData = existing;
                            UnityEngine.Object.DestroyImmediate(generated); EditorUtility.SetDirty(existing); surface.AddData();
                        }
                        else AssetDatabase.CreateAsset(surface.navMeshData, asset);
                    }
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { SceneManager.SetActiveScene(previous); if(opened)EditorSceneManager.CloseScene(scene,true); }
        }
    }
}
