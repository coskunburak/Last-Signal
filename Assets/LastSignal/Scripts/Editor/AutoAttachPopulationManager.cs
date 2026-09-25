using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class AutoAttachPopulationManager
{
    static AutoAttachPopulationManager()
    {
        EditorApplication.delayCall += Attach;
    }

    static void Attach()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (SessionState.GetBool("AutoAttachPopulationManager_Run", false)) return;
        
        bool anyDirty = false;
        string[] guids = AssetDatabase.FindAssets("t:Scene");
        foreach (string p in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(p);
            if (!path.StartsWith("Assets/LastSignal/Scenes/")) continue;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            bool dirty = false;
            foreach (var go in Object.FindObjectsOfType<GameObject>(true))
            {
                if (go.scene == scene && go.GetComponent<LastSignal.SessionFlow>() != null)
                {
                    if (go.GetComponent<LastSignal.AI.WorldPopulationManager>() == null)
                    {
                        go.AddComponent<LastSignal.AI.WorldPopulationManager>();
                        dirty = true;
                    }
                    var pop = go.GetComponent<LastSignal.AI.WorldPopulationManager>();
                    var zombiePrefab = AssetDatabase.LoadAssetAtPath<LastSignal.ZombieController>("Assets/LastSignal/Enemies/Zombie/Prefabs/LS_Zombie_Runtime.prefab");
                    if (zombiePrefab != null)
                    {
                        var so = new SerializedObject(pop);
                        so.Update();
                        var prop = so.FindProperty("zombiePrefab");
                        if (prop != null && prop.objectReferenceValue != zombiePrefab)
                        {
                            prop.objectReferenceValue = zombiePrefab;
                            so.ApplyModifiedProperties();
                            dirty = true;
                        }
                    }
                }
            }
            if (dirty) 
            {
                EditorSceneManager.SaveScene(scene);
                anyDirty = true;
            }
            if (scene != EditorSceneManager.GetActiveScene())
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
        
        SessionState.SetBool("AutoAttachPopulationManager_Run", true);
    }
}
