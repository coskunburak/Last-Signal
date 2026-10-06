using System;
using System.IO;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Loot;
using LastSignal.Objectives;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSignal.Editor
{
    public static class S011Authoring
    {
        public const string Scene = "Assets/LastSignal/Scenes/Production/RelayExpedition.unity";
        const string Evidence = "Docs/Implementation/S011/Evidence/20260929-closure/";
        static void Set(Object target, string name, Object value)
        { var s = new SerializedObject(target); s.FindProperty(name).objectReferenceValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
        public static void Author()
        {
            if (!File.Exists(Scene)) AssetDatabase.CopyAsset(S010Authoring.Scene, Scene);
            var scene = EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
            var flow = Object.FindAnyObjectByType<SessionFlow>();
            var mission = flow.GetComponent<RelayMission>(); if (!mission) mission = flow.gameObject.AddComponent<RelayMission>();
            var scrap = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/LastSignal/Data/Items/Definitions/material.scrap.asset");
            var tool = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/LastSignal/Data/Items/Definitions/tool.wrench.asset");
            const string path = "Assets/LastSignal/Data/Items/Definitions/quest.relay-fuse.asset";
            var fuse = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (!fuse) { fuse = ScriptableObject.CreateInstance<ItemDefinition>(); AssetDatabase.CreateAsset(fuse, path); }
            var data = new SerializedObject(fuse); data.FindProperty("stableId").FindPropertyRelative("id").stringValue = RelayProgression.FuseId;
            data.FindProperty("displayName").stringValue = "Relay maintenance fuse"; data.FindProperty("maxStack").intValue = 1;
            data.FindProperty("description").stringValue = "Unique relay spare. Dropped fuse can be recalled at the cabin radio; repair consumes it once.";
            data.ApplyModifiedPropertiesWithoutUndo();
            var item = Object.Instantiate(scrap.WorldPrefab); item.name = "Relay fuse"; item.GetComponent<WorldItem>().Configure(fuse, 1);
            var prefab = PrefabUtility.SaveAsPrefabAsset(item, "Assets/LastSignal/Prefabs/Shelter/RelayFuse.prefab"); Object.DestroyImmediate(item); Set(fuse, "worldPrefab", prefab);
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/LastSignal/Data/Items/Definitions/ItemCatalog.asset");
            bool found = false; foreach (var entry in catalog.EditorItems) if (entry == fuse) found = true;
            if (!found) { var c = new SerializedObject(catalog); var entries = c.FindProperty("items"); entries.InsertArrayElementAtIndex(entries.arraySize); entries.GetArrayElementAtIndex(entries.arraySize-1).objectReferenceValue = fuse; c.ApplyModifiedPropertiesWithoutUndo(); }
            mission.fuse = fuse; mission.tool = tool;
            if (!mission.radio) mission.radio = Point(mission, RelayPointKind.Radio, "cabin.radio.v1", new Vector3(-15, 1.2f, 9));
            if (!mission.relay) mission.relay = Point(mission, RelayPointKind.Relay, "overlook.relay.v1", new Vector3(8, 1.2f, -6));
            if (!mission.recoveryPoint) { var tray = new GameObject("Radio fuse recovery tray"); tray.transform.position = new Vector3(-15, .1f, 8); mission.recoveryPoint = tray.transform; }
            mission.radio.transform.position = new Vector3(-15,1.2f,9);
            mission.relay.transform.position = new Vector3(8,1.2f,-6);
            mission.recoveryPoint.position = new Vector3(-15,.1f,8);
            foreach (var sign in Object.FindObjectsByType<TextMesh>()) if (sign.name == "Radio route sign")
            {
                sign.transform.position = (sign.text.StartsWith("RADIO") ? mission.radio.transform.position : mission.relay.transform.position) + Vector3.up*.6f;
                sign.transform.rotation = Quaternion.Euler(0,270,0);
            }
            var pointObject = GameObject.Find("Gas station guaranteed fuse source");
            if (!pointObject) { pointObject = new GameObject("Gas station guaranteed fuse source"); pointObject.transform.position = new Vector3(11, .1f, 10); pointObject.AddComponent<LootSpawnPoint>(); }
            if (!GameObject.Find("Gas station maintenance sign"))
            {
                var sign = new GameObject("Gas station maintenance sign",typeof(TextMesh)); sign.transform.position = pointObject.transform.position + new Vector3(0,1.5f,1);
                var text = sign.GetComponent<TextMesh>(); text.text="GAS STATION\nMAINTENANCE CACHE\nRelay spare fuse"; text.fontSize=48; text.characterSize=.06f; text.anchor=TextAnchor.MiddleCenter;
            }
            var point = pointObject.GetComponent<LootSpawnPoint>();
            var profile = AssetDatabase.LoadAssetAtPath<LootProfile>("Assets/LastSignal/Data/Shelter/S011-Fuse.asset");
            if (!profile) { profile = ScriptableObject.CreateInstance<LootProfile>(); AssetDatabase.CreateAsset(profile,"Assets/LastSignal/Data/Shelter/S011-Fuse.asset"); }
            var p = new SerializedObject(profile); p.FindProperty("emptyBasisPoints").intValue = 0; var rows = p.FindProperty("entries"); rows.arraySize = 1;
            var row = rows.GetArrayElementAtIndex(0); row.FindPropertyRelative("item").objectReferenceValue=fuse; row.FindPropertyRelative("weight").intValue=1;
            row.FindPropertyRelative("minQuantity").intValue=1; row.FindPropertyRelative("maxQuantity").intValue=1; p.ApplyModifiedPropertiesWithoutUndo();
            var pointData = new SerializedObject(point); pointData.FindProperty("stableId").stringValue="relay.fuse-source.v1"; pointData.FindProperty("profile").objectReferenceValue=profile; pointData.ApplyModifiedPropertiesWithoutUndo();
            if (!mission.Validate() || !point.Validate(out _)) throw new InvalidOperationException("Invalid S011 authoring.");
            EditorUtility.SetDirty(mission); AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory(Evidence); File.WriteAllText(Evidence+"authoring.txt", "RelayExpedition extends S010 scene; source S010 scene unchanged. Stable radio/relay/fuse source; guaranteed single nonstacking fuse; existing wrench and threat authority.\n");
        }
        static RelayPoint Point(RelayMission mission, RelayPointKind kind, string id, Vector3 position)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=id; go.transform.position=position; go.transform.localScale=new Vector3(.5f,.5f,.5f);
            var point=go.AddComponent<RelayPoint>(); point.mission=mission; point.kind=kind; point.stableId=id;
            var sign=new GameObject("Radio route sign",typeof(TextMesh)); sign.transform.position=position+Vector3.up*.6f;
            var text=sign.GetComponent<TextMesh>(); text.text=kind==RelayPointKind.Radio ? "RADIO / E\nMaintenance note" : "RELAY / E\nFuse + wrench"; text.fontSize=40; text.characterSize=.06f; text.anchor=TextAnchor.MiddleCenter;
            return point;
        }
        public static void Build()
        {
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{Scene},locationPathName="Builds/S011/LastSignal.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.Development });
            File.WriteAllText(Evidence+"build.txt",$"Result={report.summary.result}\nErrors={report.summary.totalErrors}\nWarnings={report.summary.totalWarnings}\nDuration={report.summary.totalTime}\nUnity={Application.unityVersion}\n");
            if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("S011 build failed.");
        }
    }
}
