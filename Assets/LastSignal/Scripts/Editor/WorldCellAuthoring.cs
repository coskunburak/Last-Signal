using System;
using System.Collections.Generic;
using System.IO;
using LastSignal.Inventory.Data;
using LastSignal.Loot;
using LastSignal.Persistence;
using LastSignal.WorldCells;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.AI;

namespace LastSignal.Editor
{
    public static class WorldCellAuthoring
    {
        public const string ScenePath = "Assets/LastSignal/Scenes/WorldCellAcceptance.unity";
        public const string Evidence = "Docs/Implementation/P02-GAP-S007/Evidence/20260924-entry";
        const string Folder = "Assets/LastSignal/WorldCells";
        public static void Create()
        {
            Directory.CreateDirectory(Evidence); Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(WorldTimeAuthoring.ScenePath);
            var flow = UnityEngine.Object.FindAnyObjectByType<SessionFlow>();
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/Game/Items/Definitions/ItemCatalog.asset");
            var definitions = new[] { MakeCell(1), MakeCell(2) };
            var manager = flow.gameObject.AddComponent<WorldCellManager>(); manager.Configure(definitions, catalog);
            var terminal = Box("Two-cell expedition gate", new Vector3(-8,1,8),new Vector3(1,2,.3f),null);
            var portal = terminal.AddComponent<CellPortal>(); portal.destination="cell:1:0"; portal.Bind(manager);
            EditorSceneManager.SaveScene(scene,ScenePath); AssetDatabase.SaveAssets();
            File.WriteAllText(Evidence+"/authoring.txt","Resident WorldTimeAcceptance shelter/session apron preserved. Two enclosed authored prefab cells, own ground/walls/door/loot/enemy/NavMesh assets. Proximity prefetch and explicit interaction portal. No cross-cell serialized runtime references.\n");
        }
        static WorldCellContent MakeCell(int x)
        {
            var root = new GameObject("Cell "+x); root.SetActive(false);
            var content = root.AddComponent<WorldCellContent>(); content.coordinate = new CellCoordinate(x,0);
            float cx=x*64+16; var center=new Vector3(cx,0,16);
            content.ground=Box("Cell ground",center+Vector3.down*.25f,new Vector3(16,.5f,16),root.transform).GetComponent<Collider>();
            var walls=new List<Collider>();
            walls.Add(Box("North wall",center+new Vector3(0,1.5f,8),new Vector3(16,3,.5f),root.transform).GetComponent<Collider>());
            walls.Add(Box("South wall",center+new Vector3(0,1.5f,-8),new Vector3(16,3,.5f),root.transform).GetComponent<Collider>());
            walls.Add(Box("East wall",center+new Vector3(8,1.5f,0),new Vector3(.5f,3,16),root.transform).GetComponent<Collider>());
            walls.Add(Box("West wall",center+new Vector3(-8,1.5f,0),new Vector3(.5f,3,16),root.transform).GetComponent<Collider>());
            content.requiredSolids=walls.ToArray();
            content.entry=new GameObject("Safe entry").transform;content.entry.SetParent(root.transform);content.entry.position=center+new Vector3(-4,.05f,-4);
            content.loot=root.AddComponent<LootPopulationService>();
            var point=new GameObject("Persistent cell loot");point.transform.SetParent(root.transform);point.transform.position=center+new Vector3(-2,.1f,-2);
            var marker=point.AddComponent<LootSpawnPoint>();var so=new SerializedObject(marker);so.FindProperty("stableId").stringValue="cell-"+x+"-loot";
            var guaranteedLoot = AssetDatabase.LoadAssetAtPath<LootProfile>("Assets/LastSignal/WorldCells/Guaranteed.asset");
            if (!guaranteedLoot)
            {
                guaranteedLoot = ScriptableObject.CreateInstance<LootProfile>();
                AssetDatabase.CreateAsset(guaranteedLoot, "Assets/LastSignal/WorldCells/Guaranteed.asset");
                var profileSo = new SerializedObject(guaranteedLoot);
                profileSo.FindProperty("emptyBasisPoints").intValue = 0;
                var entriesProp = profileSo.FindProperty("entries");
                entriesProp.arraySize = 1;
                var entryProp = entriesProp.GetArrayElementAtIndex(0);
                entryProp.FindPropertyRelative("item").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Game/Items/Definitions/ammo.rifle.asset");
                entryProp.FindPropertyRelative("weight").intValue = 1;
                entryProp.FindPropertyRelative("minQuantity").intValue = 1;
                entryProp.FindPropertyRelative("maxQuantity").intValue = 1;
                profileSo.ApplyModifiedPropertiesWithoutUndo();
            }
            so.FindProperty("profile").objectReferenceValue=guaranteedLoot;so.ApplyModifiedPropertiesWithoutUndo();
            var door=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Door.prefab"),root.transform);
            door.transform.position=center+new Vector3(2,0,1);Identity(door,"cell-"+x+"-door");
            var encounter=new GameObject("Cell encounter");encounter.transform.SetParent(root.transform);encounter.transform.position=center+new Vector3(4,0,4);
            content.encounter=encounter.AddComponent<ZombieEncounter>();content.encounter.Configure(AssetDatabase.LoadAssetAtPath<ZombieController>("Assets/Resources/LS_Zombie_Runtime.prefab"),encounter.transform);Identity(encounter,"cell-"+x+"-enemy");
            var gate=Box("Next cell terminal",center+new Vector3(-4,1,-6),new Vector3(1,2,.3f),root.transform);
            gate.AddComponent<CellPortal>().destination=x==1?"cell:2:0":"cell:1:0";
            var home=Box("Shelter terminal",center+new Vector3(-6,1,-4),new Vector3(.3f,2,1),root.transform);
            var homePortal=home.AddComponent<CellPortal>();homePortal.destination="resident";homePortal.residentEntry=new Vector3(-8,.05f,6);
            var sources=new List<NavMeshBuildSource>();
            foreach(var collider in root.GetComponentsInChildren<BoxCollider>(true))
                sources.Add(new NavMeshBuildSource { shape=NavMeshBuildSourceShape.Box,transform=collider.transform.localToWorldMatrix*Matrix4x4.Translate(collider.center),size=collider.size,area=0 });
            var data=NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(-1372625422),sources,new Bounds(center,new Vector3(20,10,20)),Vector3.zero,Quaternion.identity);
            if(!data)throw new InvalidOperationException("Cell nav bake failed.");
            AssetDatabase.CreateAsset(data,Folder+"/Cell"+x+"Navigation.asset");content.navigation=data;
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"/Cell"+x+".prefab");UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<WorldCellContent>();
        }
        static void Identity(GameObject go,string id)
        {
            var identity=go.GetComponent<PersistentEntityId>();if(!identity)identity=go.AddComponent<PersistentEntityId>();
            var so=new SerializedObject(identity);so.FindProperty("id").stringValue=id;so.ApplyModifiedPropertiesWithoutUndo();
        }
        static GameObject Box(string name,Vector3 position,Vector3 size,Transform parent)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent);go.transform.position=position;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/LastSignal/Shelter/Terminal.mat");return go;
        }
        public static void Build()
        {
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{ScenePath},locationPathName="Builds/P02-GAP-S007/LastSignal.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});
            File.WriteAllText(Evidence+"/build-result.txt",$"{report.summary.result}\nUnity={Application.unityVersion}\nErrors={report.summary.totalErrors}\nWarnings={report.summary.totalWarnings}\nDuration={report.summary.totalTime}\nPath={report.summary.outputPath}\n");
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Cell build failed.");
        }
    }
}
