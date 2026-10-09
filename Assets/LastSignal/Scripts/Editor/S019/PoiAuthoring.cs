using System;
using LastSignal.Authoring;
using LastSignal.Loot;
using LastSignal.Persistence;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal.Editor.S019
{
    public static class PoiAuthoring
    {
        public const string TemplatePath = "Assets/LastSignal/Prefabs/S019/S019RuralStore.prefab";
        [MenuItem("Last Signal/S019/Create new POI from pilot template")]
        public static void CreateMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Create POIs in an editable scene outside PlayMode and Prefab Mode.");
            var template = AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePath);
            Selection.activeGameObject = CreateNew(template, SceneManager.GetActiveScene());
        }
        // Only NEW instances are rekeyed. No scene-open/import callback can change released identities.
        public static GameObject CreateNew(GameObject template, Scene scene)
        {
            if (!template || !template.GetComponent<PoiAuthoringLayout>() || !PrefabUtility.IsPartOfPrefabAsset(template))
                throw new ArgumentException("Choose a POI prefab asset with PoiAuthoringLayout.", nameof(template));
            if (!scene.IsValid() || !scene.isLoaded) throw new ArgumentException("Loaded destination scene required.", nameof(scene));
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Create new POI");
            GameObject instance = null;
            try
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(template, scene);
                Undo.RegisterCreatedObjectUndo(instance, "Create new POI");
                foreach (var entity in instance.GetComponentsInChildren<PersistentEntityId>(true)) Assign(entity, "id", Guid.NewGuid().ToString("N"));
                foreach (var point in instance.GetComponentsInChildren<LootSpawnPoint>(true)) Assign(point, "stableId", Guid.NewGuid().ToString("N"));
                EditorSceneManager.MarkSceneDirty(scene); Undo.CollapseUndoOperations(group); return instance;
            }
            catch { Undo.RevertAllDownToGroup(group); if (instance) UnityEngine.Object.DestroyImmediate(instance); throw; }
        }
        static void Assign(Component component, string field, string value)
        {
            Undo.RecordObject(component, "Assign NEW POI identity");
            var so = new SerializedObject(component); so.FindProperty(field).stringValue = value; so.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
    }
}
