using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public class AttachManager
{
    [MenuItem("Tools/Attach Manager")]
    public static void Attach()
    {
        string[] scenes = { "Assets/LastSignal/Scenes/WorldTimeAcceptance.unity", "Assets/LastSignal/Scenes/ZombieAcceptance.unity", "Assets/LastSignal/Scenes/CombatAcceptance.unity" };
        foreach (string p in AssetDatabase.FindAssets("t:Scene"))
        {
            string path = AssetDatabase.GUIDToAssetPath(p);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool dirty = false;
            foreach (var go in Object.FindObjectsOfType<GameObject>(true))
            {
                if (go.GetComponent<LastSignal.SessionFlow>() != null)
                {
                    if (go.GetComponent<LastSignal.AI.WorldPopulationManager>() == null)
                    {
                        go.AddComponent<LastSignal.AI.WorldPopulationManager>();
                        dirty = true;
                    }
                    var pop = go.GetComponent<LastSignal.AI.WorldPopulationManager>();
                    // assign prefab? We need to find zombie prefab
                    var zombiePrefab = AssetDatabase.LoadAssetAtPath<LastSignal.ZombieController>("Assets/Resources/LS_Zombie_Runtime.prefab");
                    if (zombiePrefab != null)
                    {
                        var so = new SerializedObject(pop);
                        so.Update();
                        so.FindProperty("zombiePrefab").objectReferenceValue = zombiePrefab;
                        so.ApplyModifiedProperties();
                        dirty = true;
                    }
                }
            }
            if (dirty) EditorSceneManager.SaveScene(scene);
        }
    }
}
