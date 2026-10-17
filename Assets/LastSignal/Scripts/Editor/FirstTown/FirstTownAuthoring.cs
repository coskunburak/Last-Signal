using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastSignal.AI;
using LastSignal.Audio;
using LastSignal.Loot;
using LastSignal.Objectives;
using LastSignal.Persistence;
using LastSignal.Shelter;
using LastSignal.Vehicles;
using LastSignal.WorldCells;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LastSignal.Editor.FirstTown
{
    /// <summary>Explicit native authoring only. No import callbacks, automatic tests, bake or builds.</summary>
    public static class FirstTownAuthoring
    {
        public const string ScenePath = "Assets/LastSignal/Scenes/Production/FirstTownCore.unity";
        public const string SourceScene = "Assets/LastSignal/Scenes/Production/S013Cabin.unity";
        public const string Data = "Assets/LastSignal/Data/FirstTown/";
        public const string NavPath = Data + "Navigation.asset";
        const string Evidence = "Docs/Implementation/FirstTown/Evidence";
        [Serializable] sealed class Report
        {
            public string operation, status = "NOT_RUN", scene = ScenePath, error;
            public string compilation = "NOT_RUN", gameplay = "NOT_RUN", visual = "NOT_RUN", performance = "NOT_RUN";
            public int doors, encounters, lootPoints, routes, renderers, materials, meshes;
            public string[] outputs;
        }
        static void Guard()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Exit PlayMode and Prefab Mode before authoring.");
            for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save or close modified scenes first. Authoring never discards unsaved work.");
        }
        static void Write(Report report)
        {
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Evidence + "/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + report.operation + ".json", JsonUtility.ToJson(report,true));
        }
        [MenuItem("Last Signal/First Town/1 Create new town (no bake)")]
        public static void Create()
        {
            Guard();
            var report=new Report { operation="authoring" }; var created=new List<string>();
            bool opened=false;
            try
            {
                if(File.Exists(ScenePath) || File.Exists(ScenePath+".meta")) throw new IOException("FirstTownCore already exists. Open it for editing; automatic regeneration is intentionally refused.");
                FirstTownAssets.Preflight();
                foreach(string type in new[]{"Shambler","Runner","Lurker"})
                    FirstTownAssets.Load<GameObject>("Assets/LastSignal/Prefabs/Enemies/Zombie/LS_Zombie_"+type+".prefab");
                FirstTownAssets.Load<GameObject>("Assets/LastSignal/Prefabs/Interactions/Door.prefab");
                FirstTownAssets.Folder(Data.TrimEnd('/'));
                var scene=EditorSceneManager.OpenScene(SourceScene,OpenSceneMode.Single); opened=true;
                var flow=Only<SessionFlow>(scene); var save=Only<SaveSession>(scene); var shelter=Only<ShelterLoop>(scene);
                var vehicle=Only<VehicleWorld>(scene); var cells=Only<WorldCellManager>(scene);
                var player=new SerializedObject(flow).FindProperty("playerPrefab").objectReferenceValue as GameObject;
                Require(player,"Existing player prefab reference is missing.");
                var spawn=new SerializedObject(flow).FindProperty("spawnPoint").objectReferenceValue as Transform;
                Require(spawn,"Existing spawn reference is missing.");
                var mission=Only<RelayMission>(scene);
                var cabin=scene.GetRootGameObjects().Single(r=>r.name=="S013 Production Cabin");
                var shell=scene.GetRootGameObjects().Single(r=>r.name=="Shelter / physically enclosed cabin");
                // Carry only the proven resident session/UI and safehouse composition into this unsaved scene copy.
                var keep=new HashSet<GameObject> { flow.gameObject, cabin, shell, spawn.root.gameObject, mission.radio.transform.root.gameObject,
                    mission.relay.transform.root.gameObject, mission.recoveryPoint.root.gameObject };
                foreach(var root in scene.GetRootGameObjects())
                    if(root.GetComponentInChildren<Canvas>(true) || root.name=="EventSystem" || root.name=="Sun" ||
                        root.name=="Local rain presentation" || root.name=="S013 fixed tonemapping" ||
                        root.name.StartsWith("Shelter socket /",StringComparison.Ordinal) || root.name=="Shelter rest bed" || root.name=="S015 Cabin Acoustics") keep.Add(root);
                var oldFuse=Components<LootSpawnPoint>(scene).SingleOrDefault(p=>p.StableId=="relay.fuse-source.v1");
                Require(oldFuse,"Existing guaranteed relay fuse source is required.");
                keep.Add(oldFuse.transform.root.gameObject);
                // Extract referenced anchors/interactables before removing the old collision shell.
                var retained=new[]{shelter.InsideAnchor,shelter.OutsideAnchor,shelter.StoragePoint.transform,shelter.ExitPoint.transform,shelter.ReturnPoint.transform};
                foreach(var t in retained) t.SetParent(null,true);
                foreach(var t in retained) keep.Add(t.gameObject);
                foreach(var root in scene.GetRootGameObjects()) if(!keep.Contains(root)) Object.DestroyImmediate(root);
                foreach(var portal in Components<CellPortal>(scene)) Object.DestroyImmediate(portal.gameObject);
                foreach(var surface in Components<NavMeshSurface>(scene)) Object.DestroyImmediate(surface);
                foreach(var old in Components<ZombieEncounter>(scene)) Object.DestroyImmediate(old);
                // Geometry/collider references belong to the scene copy only; no source prefab is modified.
                foreach(var renderer in shell.GetComponentsInChildren<Renderer>(true)) renderer.enabled=false;
                foreach(var collider in shell.GetComponentsInChildren<Collider>(true)) collider.enabled=false;
                foreach(var door in shell.GetComponentsInChildren<DoorInteractable>(true)) Object.DestroyImmediate(door);
                foreach(var presentation in cabin.GetComponents<MonoBehaviour>())
                    if(presentation && presentation.GetType().Name=="S013EnvironmentPresentation") Object.DestroyImmediate(presentation);
                var delta=new Vector3(180,0,-30);
                foreach(var root in keep)
                {
                    if(!root || root==flow.gameObject || root.GetComponentInChildren<Canvas>(true) || root.name=="EventSystem" || root.name=="Sun" ||
                        root.name=="Local rain presentation" || root.name=="S013 fixed tonemapping" || root==mission.relay.transform.root.gameObject || (oldFuse && root==oldFuse.transform.root.gameObject))continue;
                    root.transform.position+=delta;
                }
                foreach(var collider in cabin.GetComponentsInChildren<Collider>(true)) collider.enabled=true;
                var town=FirstTownGeometry.Group("FirstTownCore - 500m resident world",null);
                var art=new FirstTownAssets(created); art.Prepare(); var geometry=new FirstTownGeometry(art,town);
                geometry.GroundAndRoads(); geometry.Safehouse(cabin.transform); geometry.TownCenter(); geometry.HeroDistricts(); geometry.ForestAndLighting();
                // Place existing quest endpoints on the physical route; keep their published stable IDs.
                mission.relay.transform.position=new Vector3(195,1.2f,-112);
                if(oldFuse)
                {
                    // Retain the legacy quest source ID and exact support height. New supplies are separate.
                    oldFuse.transform.SetParent(town,true); oldFuse.transform.position=new Vector3(82,.01f+oldFuse.Clearance.y,-99);
                }
                mission.recoveryPoint.position=new Vector3(-180,.1f,-171);
                Set(mission,"maintenanceClue","Leave the field station through the north gate. Cross the south bridge, then follow the road east to the gas station maintenance cache. Bring its fuse and a wrench to the relay at the south edge of the industrial yard. Return along the same roads to the cabin radio.");
                // Legacy portal definitions are deliberately empty: this whole core is the resident world.
                cells.Configure(Array.Empty<WorldCellContent>(),save.Catalog);
                var population=Only<WorldPopulationManager>(scene); Set(population,"materializationEnabled",false);
                Set(save,"worldId","world.first-town.v1"); Set(save,"contentVersion","first-town-v1"); Set(save,"saveSlot","first-town-v1");
                Set(flow.GetComponent<LootPopulationService>(),"overrideSeed",true); Set(flow.GetComponent<LootPopulationService>(),"explicitSeed",241015);
                flow.Configure(player,spawn,Components<DoorInteractable>(scene));
                flow.ConfigureResidentEncounters(geometry.Encounters[0],geometry.Encounters.Skip(1).ToArray());
                shelter.ConfigurePhysicalTraversal(new Bounds(new Vector3(-180,4,-170),new Vector3(72,24,60))); Set(shelter,"storageCapacity",48);
                // Suppress obsolete leave/return prompts and signs; references remain for legacy validation.
                shelter.ExitPoint.gameObject.SetActive(false); shelter.ReturnPoint.gameObject.SetActive(false);
                Set(vehicle,"spawnPosition",new Vector3(-160,.2f,-156)); Set(vehicle,"spawnEuler",Vector3.zero);
                Set(vehicle,"residentBounds",new Bounds(new Vector3(0,10,0),new Vector3(500,100,500)));
                var audio=flow.GetComponent<ProductionAudio>();
                if(audio) { Set(audio,"cabinCenter",new Vector3(-180,1.5f,-170)); audio.ConfigureGroundAreas(Array.Empty<AudioSurface>()); }
                var nav=town.gameObject.AddComponent<NavMeshSurface>();
                nav.agentTypeID=art.Zombie("Shambler").GetComponent<NavMeshAgent>().agentTypeID;
                nav.collectObjects=CollectObjects.All; nav.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
                nav.layerMask=~((1<<8)|(1<<2)); nav.overrideVoxelSize=true; nav.voxelSize=.1f; nav.overrideTileSize=true; nav.tileSize=128;
                // Zero baked data here. User explicitly invokes BakeNavigation in a separate step.
                var markers=FirstTownGeometry.Group("98 Route acceptance markers",town);
                foreach(var route in geometry.Routes)
                { var marker=FirstTownGeometry.Group(route.name,markers); marker.position=route.position; }
                var sceneFolder=Path.GetDirectoryName(ScenePath).Replace('\\','/'); FirstTownAssets.Folder(sceneFolder);
                foreach(var component in Components<Component>(scene))
                    if(component && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                MarkStaticGeometry(scene);
                // Reusable district geometry; scene references remain authored scene objects, never cross-prefab IDs.
                FirstTownAssets.Folder("Assets/LastSignal/Prefabs/FirstTown");
                foreach(Transform district in town)
                {
                    if(!char.IsDigit(district.name[0]) || district.name.StartsWith("98") || district.name.StartsWith("99") || district.name.StartsWith("00") || district.name.StartsWith("01")) continue;
                    string path="Assets/LastSignal/Prefabs/FirstTown/"+district.name.Substring(3).Replace(' ','_')+".prefab";
                    Require(!File.Exists(path) && !File.Exists(path+".meta"),"Existing district output: "+path);
                    created.Add(path); Require(PrefabUtility.SaveAsPrefabAsset(district.gameObject,path),"Failed district prefab: "+path);
                }
                created.Add(ScenePath); Require(EditorSceneManager.SaveScene(scene,ScenePath),"Cannot save FirstTownCore.");
                AssetDatabase.SaveAssets(); report.outputs=created.ToArray(); report.status="IMPLEMENTED";
                Counts(scene,report);
                Debug.Log("FirstTownCore authored. Navigation, compilation acceptance, gameplay, visual and performance: NOT_RUN. Use First Town menus 2 and 3 manually.");
            }
            catch(Exception error)
            {
                report.status="FAIL"; report.error=error.ToString();
                if(opened) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                foreach(var path in created.AsEnumerable().Reverse()) AssetDatabase.DeleteAsset(path);
                throw;
            }
            finally { Write(report); }
        }
        static void MarkStaticGeometry(Scene scene)
        {
            foreach(var renderer in Components<MeshRenderer>(scene))
            {
                // Interactive leaves must remain movable. Only environment mesh renderers are batch candidates.
                if(renderer.GetComponentInParent<DoorInteractable>() || renderer.GetComponentInParent<Canvas>())continue;
                var flags=GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,flags | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
                if(PrefabUtility.IsPartOfPrefabInstance(renderer.gameObject))PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.gameObject);
            }
        }
        [MenuItem("Last Signal/First Town/1b Prepare static batching (no bake)")]
        public static void PrepareStaticBatching()
        {
            Guard(); var scene=OpenTown(); MarkStaticGeometry(scene);
            EditorSceneManager.MarkSceneDirty(scene); Require(EditorSceneManager.SaveScene(scene),"Cannot save town batching flags.");
            Debug.Log("FirstTown static environment batching flags authored. Door leaves remain dynamic. No bake, build or performance measurement executed.");
        }
        [MenuItem("Last Signal/First Town/2 Bake navigation (user only)")]
        public static void BakeNavigation()
        {
            Guard(); var report=new Report { operation="navigation" }; NavMeshData candidate=null;
            var disabled=new List<Collider>();
            try
            {
                var scene=OpenTown(); var surface=Only<NavMeshSurface>(scene);
                foreach(var door in Components<DoorInteractable>(scene)) foreach(var collider in door.GetComponentsInChildren<Collider>())
                    if(collider.enabled) { disabled.Add(collider); collider.enabled=false; }
                var previous=surface.navMeshData; surface.BuildNavMesh(); candidate=surface.navMeshData;
                Require(candidate,"Bake returned no navigation data.");
                if(previous && AssetDatabase.GetAssetPath(previous)==NavPath)
                {
                    surface.RemoveData(); EditorUtility.CopySerialized(candidate,previous); Object.DestroyImmediate(candidate); candidate=null;
                    surface.navMeshData=previous; surface.AddData(); EditorUtility.SetDirty(previous);
                }
                else
                {
                    Require(!File.Exists(NavPath),"Unowned existing navigation data at "+NavPath);
                    AssetDatabase.CreateAsset(candidate,NavPath); candidate=null;
                }
                foreach(var collider in disabled) if(collider)collider.enabled=true; disabled.Clear();
                EditorSceneManager.MarkSceneDirty(scene); Require(EditorSceneManager.SaveScene(scene),"Cannot save navigation reference."); AssetDatabase.SaveAssets();
                report.status="IMPLEMENTED"; report.outputs=new[]{NavPath,ScenePath};
            }
            catch(Exception error) { report.status="FAIL"; report.error=error.ToString(); throw; }
            finally { foreach(var collider in disabled)if(collider)collider.enabled=true; if(candidate && !AssetDatabase.Contains(candidate))Object.DestroyImmediate(candidate); Write(report); }
        }
        [MenuItem("Last Signal/First Town/3 Validate authored town (no play)")]
        public static void Validate()
        {
            Guard(); var report=new Report { operation="validation" };
            var obstacles=new List<NavMeshObstacle>();
            try
            {
                var scene=OpenTown(); var flow=Only<SessionFlow>(scene); var save=Only<SaveSession>(scene);
                foreach(var door in Components<DoorInteractable>(scene)) foreach(var obstacle in door.GetComponentsInChildren<NavMeshObstacle>())
                    if(obstacle.enabled) { obstacles.Add(obstacle); obstacle.enabled=false; }
                Require(save.SaveSlot=="first-town-v1","Wrong save slot."); Require(save.ValidateAuthoring().Success,save.ValidateAuthoring().Message);
                Require(Components<CellPortal>(scene).Length==0,"Portal remains in town.");
                Require(Only<ShelterLoop>(scene).PhysicalTraversal,"Physical shelter mode missing.");
                var defs=new SerializedObject(Only<WorldCellManager>(scene)).FindProperty("definitions"); Require(defs.arraySize==0,"Unexpected portal cell definitions.");
                var surface=Only<NavMeshSurface>(scene); Require(surface.navMeshData,"Run Bake navigation first."); surface.AddData();
                var filter=new NavMeshQueryFilter { agentTypeID=surface.agentTypeID,areaMask=NavMesh.AllAreas };
                var home=new SerializedObject(flow).FindProperty("spawnPoint").objectReferenceValue as Transform;
                Require(home && NavMesh.SamplePosition(home.position,out _,.6f,filter),"Home spawn is outside navigation.");
                NavMesh.SamplePosition(home.position,out var start,.6f,filter);
                var routes=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="98 Route acceptance markers");
                foreach(Transform point in routes)
                {
                    Require(NavMesh.SamplePosition(point.position,out var hit,.6f,filter),"Off-mesh route: "+point.name);
                    var path=new NavMeshPath(); Require(NavMesh.CalculatePath(start.position,hit.position,filter,path) && path.status==NavMeshPathStatus.PathComplete,"Disconnected route: "+point.name); report.routes++;
                }
                Physics.SyncTransforms();
                foreach(var point in Components<LootSpawnPoint>(scene))
                {
                    Require(point.Validate(out var error),point.name+": "+error);
                    var selection=point.Profile.Select(241015,point.StableId);
                    if(selection.Outcome==LootOutcome.Spawned) Require(LootPopulationService.PlacementValid(point,selection.Item.WorldPrefab,null,out error),point.StableId+": "+error);
                }
                foreach(var root in scene.GetRootGameObjects()) foreach(var t in root.GetComponentsInChildren<Transform>(true))
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script: "+t.name);
                foreach(var renderer in Components<Renderer>(scene)) foreach(var material in renderer.sharedMaterials)
                    Require(material && material.shader && material.shader.name!="Hidden/InternalErrorShader","Missing/error material on "+renderer.name);
                Counts(scene,report); report.status="PASS"; report.compilation="PASS";
                Debug.Log("FirstTown static/native route validation PASS. Gameplay, visual and performance acceptance remain NOT_RUN.");
            }
            catch(Exception error) { report.status="FAIL"; report.error=error.ToString(); throw; }
            finally { foreach(var obstacle in obstacles)if(obstacle)obstacle.enabled=true; Write(report); }
        }
        [MenuItem("Last Signal/First Town/4 Build development player (user only)")]
        public static void Build()
        {
            Guard();
            Require(File.Exists(ScenePath) && File.Exists(NavPath),"Author and bake the town before building.");
            Require(EditorUserBuildSettings.activeBuildTarget==BuildTarget.StandaloneOSX,"Select macOS as active target first; this command does not switch platforms.");
            string directory="Builds/FirstTown-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            Directory.CreateDirectory(directory);
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{ScenePath},locationPathName=directory+"/LastSignal-FirstTown.app",target=BuildTarget.StandaloneOSX,
                options=BuildOptions.Development
            });
            Require(result.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded,"FirstTown build failed: "+result.summary.result);
            Debug.Log("FirstTown development build: "+directory+". Runtime/performance acceptance NOT_RUN.");
        }
        static Scene OpenTown()
        {
            var scene=SceneManager.GetActiveScene();
            return scene.path==ScenePath?scene:EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        }
        static void Counts(Scene scene,Report report)
        {
            report.doors=Components<DoorInteractable>(scene).Length; report.encounters=Components<ZombieEncounter>(scene).Length;
            report.lootPoints=Components<LootSpawnPoint>(scene).Length;
            var renderers=Components<Renderer>(scene); report.renderers=renderers.Length;
            report.materials=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().Count();
            report.meshes=Components<MeshFilter>(scene).Select(m=>m.sharedMesh).Where(m=>m).Distinct().Count();
        }
        internal static T[] Components<T>(Scene scene) where T:Component => scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
        static T Only<T>(Scene scene) where T:Component
        { var found=Components<T>(scene); Require(found.Length==1,"Expected one "+typeof(T).Name+", found "+found.Length); return found[0]; }
        internal static void Require(bool condition,string message) { if(!condition)throw new InvalidOperationException(message); }
        internal static void Set(Object target,string name,object value)
        {
            if(!target)throw new InvalidOperationException("Missing target for "+name);
            var so=new SerializedObject(target); var p=so.FindProperty(name); if(p==null)throw new InvalidOperationException(target.name+" missing "+name);
            if(value is string s)p.stringValue=s; else if(value is bool b)p.boolValue=b; else if(value is int i)p.intValue=i;
            else if(value is Vector3 v)p.vector3Value=v; else if(value is Bounds bounds)p.boundsValue=bounds;
            else p.objectReferenceValue=value as Object;
            so.ApplyModifiedPropertiesWithoutUndo();
            if(PrefabUtility.IsPartOfPrefabInstance(target))PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
