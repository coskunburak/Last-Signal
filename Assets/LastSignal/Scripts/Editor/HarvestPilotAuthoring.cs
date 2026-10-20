using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LastSignal.Forest;
using LastSignal.Gathering;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Persistence;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal.Editor
{
    /// <summary>Owner-invoked only. New isolated assets/scene; never modifies a source forest generation.</summary>
    public static class HarvestPilotAuthoring
    {
        const string LogSource="Assets/LastSignal/Art/FirstTownForest/Generations/G20261016082700052/Log.prefab";
        [MenuItem("Last Signal/BLD/P01/Create Isolated S003 Pilot From Three Selected Trees")]
        public static void CreatePilot()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stop play/compilation before authoring.");
            var scene=SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.isDirty || string.IsNullOrEmpty(scene.path) || SceneManager.sceneCount!=1 ||
                scene.name!="FirstTownCore_VisualSlice")
                throw new InvalidOperationException("Owner must save/checkpoint and open only FirstTownCore_VisualSlice. No implicit saving.");
            var selected=Selection.gameObjects.OrderBy(Hierarchy,StringComparer.Ordinal).ToArray();
            if (selected.Length!=3) throw new InvalidOperationException("Select exactly two Pine and one Birch prefab roots.");
            var sources=new List<(int[] path,string locator,bool birch,Vector3 pos,Quaternion rot,Vector3 scale)>();
            int birches=0;
            foreach (var tree in selected)
            {
                var forest=tree.GetComponentInParent<ForestAuthoringState>();
                var prefab=PrefabUtility.GetCorrespondingObjectFromSource(tree);
                string asset=prefab ? AssetDatabase.GetAssetPath(prefab) : "";
                bool birch=Path.GetFileNameWithoutExtension(asset).StartsWith("Birch",StringComparison.OrdinalIgnoreCase);
                bool pine=Path.GetFileNameWithoutExtension(asset).StartsWith("Pine",StringComparison.OrdinalIgnoreCase);
                if (tree.scene!=scene || !forest || !forest.generated || !tree.transform.IsChildOf(forest.generated.transform) ||
                    tree==forest.generated || (forest.heroTrees && tree.transform.IsChildOf(forest.heroTrees)) ||
                    PrefabUtility.GetNearestPrefabInstanceRoot(tree)!=tree || (!birch && !pine) ||
                    !tree.GetComponentInChildren<Renderer>() || !tree.GetComponentInChildren<Collider>() ||
                    tree.GetComponentInParent<ResourceNodeView>())
                    throw new InvalidOperationException("Selection must be existing generated Pine/Birch roots with collision, outside HeroTrees.");
                if (selected.Any(other=>other!=tree && Vector3.Distance(other.transform.position,tree.transform.position)<7))
                    throw new InvalidOperationException("Pilot trees require at least seven metres separation for independent drop searches.");
                birches+=birch?1:0;
                sources.Add((Indices(tree.transform),Hierarchy(tree),birch,tree.transform.position,tree.transform.rotation,tree.transform.lossyScale));
            }
            if (birches!=1) throw new InvalidOperationException("Pilot requires two Pine and one Birch.");
            var save=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SaveSession>(true)).Single();
            if (!save.Catalog || save.Catalog.GetItem(new StableItemId("material.log")))
                throw new InvalidOperationException("A valid catalog without material.log is required. Existing authored pilots are never overwritten.");
            var playerPrefab=new SerializedObject(save.GetComponent<SessionFlow>()).FindProperty("playerPrefab").objectReferenceValue as GameObject;
            var combat=playerPrefab ? playerPrefab.GetComponent<PlayerCombatController>() : null;
            var heavy=combat ? new SerializedObject(combat).FindProperty("heavyMelee").objectReferenceValue as MeleeWeaponController : null;
            if (!heavy || !heavy.Definition || heavy.Definition.WeaponId!=MeleeWeaponDefinition.AxeId || !heavy.HasValidAuthoring)
                throw new InvalidOperationException("Pilot requires the existing owned heavy-melee axe on the player prefab. No synthetic inventory tool grant.");
            var sourceLog=AssetDatabase.LoadAssetAtPath<GameObject>(LogSource);
            if (!sourceLog) throw new InvalidOperationException("Expected local decorative log source is missing; no placeholder substitution.");
            var sourcePath=scene.path; var sourceHash=Hash(sourcePath);
            var stamp=DateTime.UtcNow.ToString("yyyyMMddHHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8);
            var data="Assets/LastSignal/Data/Gathering/Pilot-"+stamp;
            var prefabs="Assets/LastSignal/Prefabs/Gathering/Pilot-"+stamp;
            var stage="Assets/LastSignal/Scenes/Validation/FirstTownHarvest_"+stamp+".unity";
            Folder(data); Folder(prefabs);
            // Source is copied, never saved or promoted. Any later error leaves an isolated, reviewable draft.
            if (File.Exists(stage) || !EditorSceneManager.SaveScene(scene,stage,true) || Hash(sourcePath)!=sourceHash)
                throw new InvalidOperationException("Source checkpoint/copy failed. Do not proceed.");
            scene=EditorSceneManager.OpenScene(stage,OpenSceneMode.Single);
            save=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SaveSession>(true)).Single();
            var item=ScriptableObject.CreateInstance<ItemDefinition>();
            var itemSO=new SerializedObject(item);
            itemSO.FindProperty("stableId").FindPropertyRelative("id").stringValue="material.log";
            itemSO.FindProperty("displayName").stringValue="Wood Log";
            itemSO.FindProperty("description").stringValue="Finite harvested timber. S003 pilot; crafting recipes are not enabled.";
            itemSO.FindProperty("category").enumValueIndex=(int)ItemCategory.Material;
            itemSO.FindProperty("maxStack").intValue=8; itemSO.FindProperty("massGrams").intValue=2000;
            var scrap=save.Catalog.GetItem(new StableItemId("material.scrap"));
            if (scrap) itemSO.FindProperty("icon").objectReferenceValue=scrap.Icon; // Explicit provisional icon; S004 art gate.
            itemSO.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.CreateAsset(item,data+"/Log.asset");
            var wrapper=new GameObject("HarvestLog");
            try
            {
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(sourceLog); visual.transform.SetParent(wrapper.transform,false);
                var renderers=visual.GetComponentsInChildren<Renderer>();
                if (renderers.Length==0) throw new InvalidOperationException("Log requires visible local geometry.");
                var bounds=renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                if (bounds.size.x<=0 || bounds.size.y<=0 || bounds.size.z<=0) throw new InvalidOperationException("Invalid log bounds.");
                float scale=Mathf.Min(1/bounds.size.x,.4f/bounds.size.y,.4f/bounds.size.z);
                visual.transform.localScale*=scale; visual.transform.localPosition=-bounds.center*scale;
                foreach (var collider in visual.GetComponentsInChildren<Collider>()) collider.enabled=false;
                var box=wrapper.AddComponent<BoxCollider>(); box.size=new Vector3(1,.4f,.4f);
                wrapper.AddComponent<WorldItem>().Configure(item,3);
                var prefab=PrefabUtility.SaveAsPrefabAsset(wrapper,prefabs+"/HarvestLog.prefab");
                itemSO.Update(); itemSO.FindProperty("worldPrefab").objectReferenceValue=prefab; itemSO.ApplyModifiedPropertiesWithoutUndo();
            }
            finally { UnityEngine.Object.DestroyImmediate(wrapper); }
            var catalog=UnityEngine.Object.Instantiate(save.Catalog);
            var catalogSO=new SerializedObject(catalog); var entries=catalogSO.FindProperty("items");
            entries.InsertArrayElementAtIndex(entries.arraySize); entries.GetArrayElementAtIndex(entries.arraySize-1).objectReferenceValue=item;
            catalogSO.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.CreateAsset(catalog,data+"/PilotCatalog.asset");
            var pineDefinition=Definition("resource.pine",item,data+"/Pine.asset");
            var birchDefinition=Definition("resource.birch",item,data+"/Birch.asset");
            var manifest=ScriptableObject.CreateInstance<HarvestManifest>();
            manifest.manifestId="harvest-manifest:"+Guid.NewGuid().ToString("N");
            manifest.sourceScene=sourcePath; manifest.sourceSha256=sourceHash; manifest.anchors=new HarvestManifest.Anchor[3];
            var gathering=save.gameObject.AddComponent<GatheringWorld>(); gathering.residentViews=new ResourceNodeView[3];
            var stagedTrees=sources.Select(source=>At(scene,source.path)).ToArray();
            var overlay=new GameObject("Authored harvest pilot — excluded from forest generation");
            for (int i=0;i<sources.Count;i++)
            {
                var source=sources[i]; var tree=stagedTrees[i];
                if (PrefabUtility.IsPartOfPrefabInstance(tree)) PrefabUtility.UnpackPrefabInstance(tree,PrefabUnpackMode.Completely,InteractionMode.UserAction);
                var root=new GameObject("Harvest anchor "+(i+1)); root.transform.SetParent(overlay.transform,false);
                root.transform.SetPositionAndRotation(source.pos,source.rot); tree.transform.SetParent(root.transform,true);
                var view=root.AddComponent<ResourceNodeView>(); view.nodeId="harvest:"+Guid.NewGuid().ToString("N");
                view.standingVisual=tree; view.world=gathering;
                var retry=root.AddComponent<SphereCollider>(); retry.radius=.25f; retry.center=Vector3.up*.3f; retry.enabled=false;
                view.retryCollider=retry; gathering.residentViews[i]=view;
                manifest.anchors[i]=new HarvestManifest.Anchor { nodeId=view.nodeId,cellId="resident",sourceHierarchy=source.locator,
                    position=source.pos,rotation=source.rot,scale=source.scale,definition=source.birch?birchDefinition:pineDefinition };
            }
            AssetDatabase.CreateAsset(manifest,data+"/Manifest.asset"); gathering.manifest=manifest;
            var saveSO=new SerializedObject(save); saveSO.FindProperty("catalog").objectReferenceValue=catalog;
            saveSO.FindProperty("saveSlot").stringValue="harvest_"+stamp; saveSO.ApplyModifiedPropertiesWithoutUndo();
            var cells=save.GetComponent<LastSignal.WorldCells.WorldCellManager>();
            if (cells) { var so=new SerializedObject(cells); so.FindProperty("catalog").objectReferenceValue=catalog; so.ApplyModifiedPropertiesWithoutUndo(); }
            AssetDatabase.SaveAssetIfDirty(item); // Only the newly created Log gained a post-creation prefab reference; preserve unrelated dirty assets.
            if (!EditorSceneManager.SaveScene(scene) || Hash(sourcePath)!=sourceHash)
                throw new InvalidOperationException("Stage save/source fingerprint requires owner inspection.");
            Selection.activeGameObject=gathering.gameObject;
            Debug.Log("S003_STAGE_PREPARED sourceSHA256="+sourceHash+" scene="+stage+" manifest="+data+"/Manifest.asset clearanceApproved=false. Owner QA NOT_RUN.");
        }
        static ResourceNodeDefinition Definition(string id,ItemDefinition output,string path)
        { var d=ScriptableObject.CreateInstance<ResourceNodeDefinition>(); d.definitionId=id; d.output=output; AssetDatabase.CreateAsset(d,path); return d; }
        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent=Path.GetDirectoryName(path).Replace('\\','/'); Folder(parent);
            AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
        static int[] Indices(Transform t)
        { var result=new List<int>(); for (;t;t=t.parent) result.Add(t.GetSiblingIndex()); result.Reverse(); return result.ToArray(); }
        static GameObject At(Scene scene,int[] path)
        { var t=scene.GetRootGameObjects()[path[0]].transform; for (int i=1;i<path.Length;i++) t=t.GetChild(path[i]); return t.gameObject; }
        static string Hierarchy(GameObject go)
        { string path=""; for (var t=go.transform;t;t=t.parent) path="/"+t.name+"["+t.GetSiblingIndex()+"]"+path; return path; }
        static string Hash(string path)
        { using (var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant(); }
    }

    [CustomEditor(typeof(GatheringWorld))]
    public sealed class GatheringWorldInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var world=(GatheringWorld)target;
            EditorGUILayout.HelpBox(world.Feedback,world.RecoveryRequired?MessageType.Error:MessageType.Info);
            EditorGUILayout.LabelField("Ready",world.Ready.ToString());
            if (Application.isPlaying && GUILayout.Button("Log Current Harvest Snapshot (read only)"))
            {
                try { Debug.Log("S003_SNAPSHOT "+JsonUtility.ToJson(world.Capture(),true)+" save="+world.GetComponent<SaveSession>().DefaultPath); }
                catch (InvalidOperationException e) { Debug.LogWarning("S003_SNAPSHOT_UNAVAILABLE "+e.Message); }
            }
        }
    }
}
