using System;
using System.IO;
using LastSignal.Loot;
using LastSignal.Objectives;
using LastSignal.Persistence;
using LastSignal.Shelter;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSignal.Editor
{
    public static class S012Authoring
    {
        public const string Scene = "Assets/LastSignal/Scenes/IntegratedGraybox.unity";
        const string Folder = "Assets/LastSignal/S012";
        static Material material;
        static GameObject Box(string name, Vector3 at, Vector3 size, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent); go.transform.position = at; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }
        static void Sign(string text, Vector3 at)
        {
            var go = new GameObject("S012 route sign", typeof(TextMesh)); go.transform.position = at;
            var label = go.GetComponent<TextMesh>(); label.text = text; label.characterSize = .13f;
            label.fontSize = 48; label.anchor = TextAnchor.MiddleCenter;
            // Back face gives the same information to the return journey.
            var back = Object.Instantiate(go); back.transform.Rotate(0,180,0);
        }
        static void Building(string name, Vector3 at, float width = 12, float depth = 10)
        {
            var root = new GameObject(name).transform;
            Box("West wall", at + new Vector3(-width/2,1.5f,0),new Vector3(.4f,3,depth),root);
            Box("East wall", at + new Vector3(width/2,1.5f,0),new Vector3(.4f,3,depth),root);
            foreach (float side in new[]{-1f,1f})
                foreach(float half in new[]{-1f,1f})
                    Box("Open entrance flank",at + new Vector3(half*(width/4+1),1.5f,side*depth/2),new Vector3(width/2-2,3,.4f),root);
            Box("Rain roof",at + Vector3.up*3.2f,new Vector3(width+.5f,.3f,depth+.5f),root).AddComponent<LastSignal.WorldTime.RainRoof>();
            Sign(name,at + new Vector3(0,3.7f,-depth/2));
        }
        static void Shift(GameObject go,Vector3 delta) { if(!go) throw new InvalidOperationException("Missing source object"); go.transform.position += delta; }
        public static void Author()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit PlayMode first.");
            if (File.Exists(Scene)) throw new InvalidOperationException("Candidate already exists; edit deliberately, never regenerate over user work.");
            for(int i=0;i<EditorSceneManager.sceneCount;i++) if(EditorSceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Unsaved scene requires preservation.");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            if(!AssetDatabase.CopyAsset(S011Authoring.Scene,Scene)) throw new InvalidOperationException("Scene copy failed.");
            var s = EditorSceneManager.OpenScene(Scene);
            material = AssetDatabase.LoadAssetAtPath<Material>("Assets/LastSignal/Shelter/Terminal.mat");
            var flow = Object.FindAnyObjectByType<SessionFlow>();
            var mission = flow.GetComponent<RelayMission>();
            var navigation = GameObject.Find("NavigationWorld");
            var surface = navigation.GetComponent<NavMeshSurface>();
            surface.RemoveData(); surface.navMeshData = null;
            // Replace only copied diagnostic geometry; original acceptance scene stays intact.
            for(int i=navigation.transform.childCount-1;i>=0;i--) Object.DestroyImmediate(navigation.transform.GetChild(i).gameObject);
            Box("S012 ground 400 x 400 m",new Vector3(-200,-.25f,0),new Vector3(400,.5f,400),navigation.transform);
            Box("North boundary",new Vector3(-200,2,200),new Vector3(400,4,1),navigation.transform);
            Box("South boundary",new Vector3(-200,2,-200),new Vector3(400,4,1),navigation.transform);
            Box("West boundary",new Vector3(-400,2,0),new Vector3(1,4,400),navigation.transform);
            Box("East boundary",new Vector3(0,2,0),new Vector3(1,4,400),navigation.transform);
            var cabinDelta = new Vector3(-345,0,-149);
            Shift(GameObject.Find("Shelter / physically enclosed cabin"),cabinDelta);
            Shift(GameObject.Find("PlayerSpawn"),cabinDelta);
            GameObject.Find("PlayerSpawn").transform.position=flow.GetComponent<ShelterLoop>().InsideAnchor.position;
            Shift(GameObject.Find("Shelter rest bed"),cabinDelta);
            foreach(var socket in flow.GetComponent<ShelterSite>().sockets) Shift(socket.gameObject,cabinDelta);
            foreach(var label in Object.FindObjectsByType<TextMesh>()) if(label.name=="Socket sign") Shift(label.gameObject,cabinDelta);
            Shift(mission.radio.gameObject,cabinDelta); Shift(mission.recoveryPoint.gameObject,cabinDelta);
            mission.relay.transform.position = new Vector3(-40,1.2f,-130);
            foreach(var label in Object.FindObjectsByType<TextMesh>()) if(label.name=="Radio route sign")
                label.transform.position=(label.text.StartsWith("RADIO")?mission.radio.transform.position:mission.relay.transform.position)+Vector3.up*.6f;
            // Retain original IDs and profiles, relocate the existing five-point resource groups.
            Shift(GameObject.Find("Kitchen"),new Vector3(-150,0,-48));
            Shift(GameObject.Find("Clinic"),new Vector3(-270,0,31));
            Shift(GameObject.Find("Workshop"),new Vector3(-90,0,110));
            Shift(GameObject.Find("Security"),new Vector3(-180,0,-91));
            GameObject.Find("Gas station guaranteed fuse source").transform.position=new Vector3(-100,.1f,108);
            GameObject.Find("Gas station maintenance sign").transform.position=new Vector3(-100,2,114);
            GameObject.Find("ZombieSpawn").transform.position=new Vector3(-190,0,-76);
            GameObject.Find("Persistent acceptance door").transform.position=new Vector3(-188,.05f,-100);
            GameObject.Find("Two-cell expedition gate").transform.position=new Vector3(-110,1,112);
            // Existing streamed cells remain explicit optional expeditions; no walk-through access before CellReady.
            Building("MARKET / supplies",new Vector3(-160,0,-40),18,16);
            Building("CLINIC / recovery supplies",new Vector3(-280,0,35),18,16);
            Building("MAINTENANCE / fuse and crafting supplies",new Vector3(-100,0,110),18,16);
            Building("SECURITY / ammunition",new Vector3(-190,0,-95),18,16);
            Sign("RELAY / return to cabin radio after repair",new Vector3(-40,3,-128));
            Sign("CABIN <- southwest\nMARKET -> east\nCLINIC / covered approach -> north",new Vector3(-300,2,-110));
            Sign("MAINTENANCE -> northeast\nRELAY -> southeast\nCABIN <- southwest",new Vector3(-150,2,40));
            // Two long sight barriers make the western route materially different from the open direct road.
            Box("Western route sight barrier A",new Vector3(-250,1.8f,-25),new Vector3(2,3.6f,90),navigation.transform);
            Box("Western route sight barrier B",new Vector3(-220,1.8f,90),new Vector3(2,3.6f,65),navigation.transform);
            Sign("WESTERN COVERED ROUTE\nClinic / maintenance",new Vector3(-300,2,-65));
            Sign("DIRECT ROAD\nSecurity / market / maintenance",new Vector3(-240,2,-110));
            var saves=new SerializedObject(flow.GetComponent<SaveSession>());
            saves.FindProperty("worldId").stringValue="world.s012.graybox";
            saves.FindProperty("contentVersion").stringValue="s012-graybox-v1"; saves.ApplyModifiedPropertiesWithoutUndo();
            // Scene-authored clue uses the same progression authority and retains the old scene's default wording.
            var clue=new SerializedObject(mission); clue.FindProperty("maintenanceClue").stringValue=
                "Maintenance cache: northeast of the cabin, beyond the market. Bring its spare fuse and a wrench to the relay in the southeast. The western clinic route offers cover; the direct road passes security. Return to the cabin radio after repair.";
            clue.ApplyModifiedPropertiesWithoutUndo();
            surface.collectObjects=CollectObjects.All; surface.useGeometry=UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            if(!surface.navMeshData) throw new InvalidOperationException("S012 navigation bake failed.");
            AssetDatabase.CreateAsset(surface.navMeshData,Folder+"/ResidentNavigation.asset");
            if(!mission.Validate()||!flow.GetComponent<SaveSession>().ValidateAuthoring().Success) throw new InvalidOperationException("S012 composition invalid.");
            EditorSceneManager.MarkSceneDirty(s); EditorSceneManager.SaveScene(s); AssetDatabase.SaveAssets();
        }
        public static void BuildCurrent()
        {
            // Batch -executeMethod entry; every candidate gets a new output path.
            var run=DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ",System.Globalization.CultureInfo.InvariantCulture);
            Build(Path.Combine("Builds/S012",run));
        }
        public static void Build(string output)
        {
            Directory.CreateDirectory(output);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{Scene},locationPathName=Path.Combine(output,"LastSignal.app"),target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});
            File.WriteAllText(Path.Combine(output,"build.txt"),$"Result={report.summary.result}\nErrors={report.summary.totalErrors}\nWarnings={report.summary.totalWarnings}\nDuration={report.summary.totalTime}\nUnity={Application.unityVersion}\nPath={report.summary.outputPath}\n");
            if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("S012 build failed.");
        }
    }
}
