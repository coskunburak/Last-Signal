using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastSignal.AI;
using LastSignal.Authoring;
using LastSignal.Editor.S019;
using LastSignal.Inventory.Data;
using LastSignal.Loot;
using LastSignal.Persistence;
using LastSignal.Shelter;
using LastSignal.WorldCells;
using LastSignal.WorldTime;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LastSignal.Editor.S023
{
    // Explicit user-run native authoring. No import hook, migration or runtime owner.
    public static class S023RegionAuthoring
    {
        public const string ScenePath = "Assets/LastSignal/Scenes/S023/S023RegionPilot.unity";
        public const string SourceScene = "Assets/LastSignal/Scenes/Production/S013Cabin.unity";
        public const string Slot = "s023-pilot", World = "world.s023.logistics.v1";
        public const string Depot = "cell:3:0", Yard = "cell:4:0";
        public const int Seed = 23023;
        public const string Data = "Assets/LastSignal/Data/S023/";
        public const string Prefabs = "Assets/LastSignal/Prefabs/S023/";
        const string Kit = "Assets/LastSignal/Art/S013/Production/";
        public static string CellPath(int x) => Prefabs + (x == 3 ? "MaintenanceDepot" : "LoadingYard") + ".prefab";
        public static string NavPath(int x) => Data + "Cell" + x + "Navigation.asset";
        public static readonly string[] ProfileNames = { "Maintenance", "Checkpoint", "Shipment" };
        public static string[] Outputs => new[] { ScenePath, CellPath(3), CellPath(4), NavPath(3), NavPath(4) }
            .Concat(ProfileNames.Select(n => Data + n + ".asset")).ToArray();
        [Serializable] public sealed class RouteRecord { public string cell, target; public float metres; }
        [Serializable] public sealed class DependencyRecord
        {
            public string owner; public string[] dependencies;
            public int renderers, sharedMaterials, sharedMeshes;
        }
        [Serializable] public sealed class Report
        {
            public string authoring = "NOT_RUN", gameplay = "NOT_RUN", visual = "NOT_RUN", performance = "NOT_MEASURED";
            public string scene = ScenePath, error;
            public bool reused;
            public double nativeAuthoringSeconds;
            public List<RouteRecord> routes = new List<RouteRecord>();
            public List<DependencyRecord> assets = new List<DependencyRecord>();
        }

        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Close Unity and use Tools/s023-author.py. Existing open scenes must not be discarded.");
            string output = Environment.GetEnvironmentVariable("LASTSIGNAL_S023_AUTHOR_RESULT");
            Require(!string.IsNullOrEmpty(output) && Path.IsPathRooted(output), "Absolute result path required.");
            var report = new Report(); var created = new List<string>();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                report.reused = File.Exists(ScenePath);
                if (!report.reused)
                {
                    foreach (var path in Outputs) Require(!File.Exists(path) && !File.Exists(path + ".meta"), "Partial/pre-existing output must be reviewed, never overwritten: " + path);
                    Create(created);
                }
                Validate(report);
                report.authoring = "PASS"; // Native authoring checks only, never gameplay acceptance.
            }
            catch (Exception error)
            {
                report.authoring = "FAIL"; report.error = error.ToString();
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                foreach (var path in created.AsEnumerable().Reverse()) AssetDatabase.DeleteAsset(path);
                throw;
            }
            finally
            {
                report.nativeAuthoringSeconds = watch.Elapsed.TotalSeconds;
                File.WriteAllText(output, JsonUtility.ToJson(report, true));
            }
        }

        static void Create(List<string> created)
        {
            var template = Load<GameObject>(PoiAuthoring.TemplatePath);
            foreach (string name in new[] { "Metal", "Stone", "Iron", "RoofMetal", "GroundPath", "Wood", "PaintedTimber" }) Load<Material>(Kit + "Materials/" + name + ".mat");
            foreach (string name in new[] { "Wall_2m", "Window_2m" }) Load<GameObject>(Kit + "Prefabs/" + name + ".prefab");
            Load<GameObject>("Assets/LastSignal/Prefabs/Interactions/Door.prefab");
            var scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
            var save = Only<SaveSession>(scene); var manager = Only<WorldCellManager>(scene);
            var flow = Only<SessionFlow>(scene); var population = Only<WorldPopulationManager>(scene);
            Require(save.ValidateAuthoring().Success, "Source scene save authoring invalid; source remains unchanged.");
            var anchor = flow.GetComponent<ShelterLoop>().OutsideAnchor;
            Require(anchor, "Resident return anchor missing.");
            Vector3 home = anchor.position;
            var defs = Definitions(manager);
            Require(defs.Length == 2 && defs.All(d => d) && !defs.Any(d => d.coordinate.x == 3 || d.coordinate.x == 4), "Source topology changed; review before generating.");
            var maintenance = Profile("Maintenance", 0, new[] { "material.scrap" }, new[] { 1 }, new[] { 1 }, new[] { 2 }, created);
            var checkpoint = Profile("Checkpoint", 5000, new[] { "material.cloth" }, new[] { 1 }, new[] { 1 }, new[] { 1 }, created);
            var shipment = Profile("Shipment", 2500, new[] { "material.scrap", "fuel.generator" }, new[] { 4, 1 }, new[] { 1, 1 }, new[] { 2, 1 }, created);
            var depot = CreateCell(scene, template, 3, home, maintenance, checkpoint, created);
            var yard = CreateCell(scene, template, 4, home, shipment, null, created);
            manager.Configure(defs.Concat(new[] { depot, yard }).ToArray(), save.Catalog);
            Set(save, "worldId", World); Set(save, "saveSlot", Slot);
            Set(flow.GetComponent<LootPopulationService>(), "overrideSeed", true);
            Set(flow.GetComponent<LootPopulationService>(), "explicitSeed", Seed);
            // Keep the authoritative S022 ledger/materializer, bounded pilot pressure.
            Set(population, "compositionSeed", Seed); Set(population, "initialPopulationPerCell", 3);
            var capture = flow.gameObject.AddComponent<LastSignal.Regions.S023Capture>();
            capture.sourceRevision = Environment.GetEnvironmentVariable("LASTSIGNAL_S023_SOURCE_HEAD") ?? "UNKNOWN";
            capture.sourceDirty = Environment.GetEnvironmentVariable("LASTSIGNAL_S023_SOURCE_DIRTY") != "false";
            var gate = Box("S023 Maintenance district gate", null, home + new Vector3(2, 1, 0), new Vector3(.4f, 2, .4f), "Metal");
            gate.AddComponent<CellPortal>().destination = Depot;
            Sign(gate.transform, "MAINTENANCE\nDISTRICT  >", new Vector3(0, .6f, -.24f));
            EditorSceneManager.MarkSceneDirty(scene);
            created.Add(ScenePath); Require(EditorSceneManager.SaveScene(scene, ScenePath), "Cannot save isolated scene.");
            AssetDatabase.SaveAssets();
        }

        static LootProfile Profile(string name, int empty, string[] ids, int[] weights, int[] min, int[] max, List<string> created)
        {
            var profile = ScriptableObject.CreateInstance<LootProfile>(); profile.name = name;
            var so = new SerializedObject(profile); so.FindProperty("emptyBasisPoints").intValue = empty;
            var entries = so.FindProperty("entries"); entries.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("item").objectReferenceValue = Load<ItemDefinition>("Assets/LastSignal/Data/Items/Definitions/" + ids[i] + ".asset");
                entry.FindPropertyRelative("weight").intValue = weights[i];
                entry.FindPropertyRelative("minQuantity").intValue = min[i]; entry.FindPropertyRelative("maxQuantity").intValue = max[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo(); Require(profile.Validate(out var error), error);
            string path = Data + name + ".asset"; created.Add(path); AssetDatabase.CreateAsset(profile, path); return profile;
        }

        static WorldCellContent CreateCell(Scene scene, GameObject template, int x, Vector3 home, LootProfile main, LootProfile minor, List<string> created)
        {
            var root = PoiAuthoring.CreateNew(template, scene);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            // Reuse S019 identity/ownership layout; replace this NEW instance's rural layout.
            foreach (Transform child in root.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            root.name = x == 3 ? "S023 Maintenance Depot and Checkpoint" : "S023 Loading Yard";
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); root.transform.localScale = Vector3.one;
            var layout = root.GetComponent<PoiAuthoringLayout>();
            var navigationRoot = Group("Navigation geometry", root.transform, Vector3.zero);
            var geometry = Group("Region geometry", navigationRoot, new Vector3(x * CellCoordinate.Size, 0, 0));
            layout.navigationGeometry = navigationRoot;
            var content = root.AddComponent<WorldCellContent>(); content.coordinate = new CellCoordinate(x, 0);
            content.loot = root.AddComponent<LootPopulationService>(); content.lurkerAmbushContext = x == 3;
            content.ground = Box("Concrete apron", geometry, new Vector3(32, -.25f, 32), new Vector3(56, .5f, 56), "Stone").GetComponent<Collider>();
            Box("North perimeter", geometry, new Vector3(32, 1.6f, 60), new Vector3(56, 3.2f, .5f), "Metal");
            Box("South perimeter", geometry, new Vector3(32, 1.6f, 4), new Vector3(56, 3.2f, .5f), "Metal");
            Box("West perimeter", geometry, new Vector3(4, 1.6f, 32), new Vector3(.5f, 3.2f, 56), "Metal");
            Box("East perimeter", geometry, new Vector3(60, 1.6f, 32), new Vector3(.5f, 3.2f, 56), "Metal");
            layout.entry = Marker("Entry", root.transform, geometry.position + new Vector3(9, .05f, 10));
            layout.retreat = Marker("Retreat", root.transform, geometry.position + new Vector3(9, .05f, 18)); content.entry = layout.entry;
            var points = new List<LootSpawnPoint>();
            var doors = new List<DoorInteractable>();
            if (x == 3)
            {
                DepotGeometry(geometry, doors);
                points.Add(Loot(root.transform, geometry.position + new Vector3(36, .1f, 34), "s023.depot.scrap", main));
                points.Add(Loot(root.transform, geometry.position + new Vector3(14, .1f, 31), "s023.checkpoint.cloth", minor));
                Sign(geometry, "SERVICE 03", new Vector3(34, 3, 23.7f));
                Sign(geometry, "CHECKPOINT", new Vector3(13, 2.6f, 27.7f));
            }
            else
            {
                // Staggered shipping stacks create a long exposed lane and a covered western detour.
                foreach (var p in new[] { new Vector3(24, 1.4f, 24), new Vector3(40, 1.4f, 34), new Vector3(26, 1.4f, 43) })
                    Box("Abandoned shipping stack", geometry, p, new Vector3(8, 2.8f, 3), "Metal");
                Box("Maintenance gantry left", geometry, new Vector3(43, 3, 43), new Vector3(.6f, 6, .6f), "Iron");
                Box("Maintenance gantry right", geometry, new Vector3(53, 3, 43), new Vector3(.6f, 6, .6f), "Iron");
                Box("Gantry landmark", geometry, new Vector3(48, 6, 43), new Vector3(11, .8f, .8f), "PaintedTimber");
                Box("Unfinished repair bench", geometry, new Vector3(49, .55f, 47), new Vector3(3, 1.1f, 1), "Iron");
                points.Add(Loot(root.transform, geometry.position + new Vector3(48, .1f, 46), "s023.yard.shipment", main));
                Sign(geometry, "LOADING 04\nDISPATCH CLOSED", new Vector3(48, 5.2f, 42.5f));
            }
            // Readable path with a physical destination at each end; signs are environmental, not HUD.
            Box("Direct approach strip", geometry, new Vector3(24, .006f, 18), new Vector3(30, .012f, 1.3f), "GroundPath", false);
            Box("Covered western retreat strip", geometry, new Vector3(9, .006f, 31), new Vector3(1.3f, .012f, 28), "GroundPath", false);
            Box("Retreat sight break", geometry, new Vector3(13, 1.5f, 19), new Vector3(5, 3, .4f), "Metal");
            var spawn = Marker("Encounter spawn", root.transform, geometry.position + (x == 3 ? new Vector3(40, .05f, 33) : new Vector3(48, .05f, 39)));
            var encounter = spawn.gameObject.AddComponent<ZombieEncounter>();
            var zombie = Load<GameObject>("Assets/LastSignal/Prefabs/Enemies/Zombie/LS_Zombie_" + (x == 3 ? "Lurker" : "Runner") + ".prefab").GetComponent<ZombieController>();
            Require(zombie, "S022 encounter prefab missing controller."); encounter.Configure(zombie, spawn);
            Identity(encounter.gameObject, "s023." + x + ".encounter"); content.encounter = layout.encounter = encounter;
            layout.lootAnchors = points.ToArray();
            layout.productionNotes = "S023 isolated logistics pilot. One authored encounter per cell; S022 population remains session-owned. Native/gameplay/visual acceptance are distinct. IDs are released after first authoring; reruns validate without rekeying.";
            Portal(geometry, "SHELTER RETURN", new Vector3(7, 1, 10), "resident", home);
            Portal(geometry, x == 3 ? "LOADING YARD >" : "< MAINTENANCE", new Vector3(54, 1, 52), x == 3 ? Yard : Depot, home);
            var surface = navigationRoot.gameObject.AddComponent<NavMeshSurface>();
            surface.agentTypeID = zombie.GetComponent<NavMeshAgent>().agentTypeID;
            surface.collectObjects = CollectObjects.Children; surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize = true; surface.voxelSize = .08f; surface.overrideTileSize = true; surface.tileSize = 128;
            var doorColliders = doors.SelectMany(d => d.GetComponentsInChildren<Collider>()).ToArray();
            foreach (var c in doorColliders) c.enabled = false;
            surface.BuildNavMesh(); Require(surface.navMeshData, "Native bake returned no navigation for cell " + x);
            var data = surface.navMeshData; surface.RemoveData(); surface.enabled = false;
            // WorldCellContent alone installs/removes this absolute-world-space data at runtime.
            foreach (var c in doorColliders) c.enabled = true;
            foreach (var c in doorColliders.OfType<BoxCollider>())
            {
                var obstacle = c.gameObject.AddComponent<NavMeshObstacle>(); obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.center = c.center; obstacle.size = c.size; obstacle.carving = true; obstacle.carveOnlyStationary = true;
            }
            created.Add(NavPath(x)); AssetDatabase.CreateAsset(data, NavPath(x)); content.navigation = data;
            content.requiredSolids = geometry.GetComponentsInChildren<Collider>().Where(c => !c.GetComponentInParent<DoorInteractable>() && c != content.ground).ToArray();
            root.SetActive(false); created.Add(CellPath(x));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, CellPath(x)); Require(prefab, "Cannot save cell prefab.");
            Object.DestroyImmediate(root); return prefab.GetComponent<WorldCellContent>();
        }

        static void DepotGeometry(Transform g, List<DoorInteractable> doors)
        {
            // 16 x 12 m depot. Front door plus 4 m west service opening avoid a single choke point.
            Box("Depot east", g, new Vector3(44, 1.7f, 30), new Vector3(.4f, 3.4f, 12), "Metal");
            Box("Depot north", g, new Vector3(36, 1.7f, 36), new Vector3(16, 3.4f, .4f), "Metal");
            Box("Depot west rear", g, new Vector3(28, 1.7f, 34), new Vector3(.4f, 3.4f, 4), "Metal");
            Box("Depot west front", g, new Vector3(28, 1.7f, 26), new Vector3(.4f, 3.4f, 4), "Metal");
            Box("Depot front left", g, new Vector3(30.4f, 1.7f, 24), new Vector3(4.8f, 3.4f, .4f), "Metal");
            Box("Depot front right", g, new Vector3(39.15f, 1.7f, 24), new Vector3(9.7f, 3.4f, .4f), "Metal");
            Box("Door lintel", g, new Vector3(33.55f, 2.9f, 24), new Vector3(1.5f, 1, .4f), "Iron");
            var roof = Box("Depot rain roof", g, new Vector3(36, 3.5f, 30), new Vector3(16.6f, .2f, 12.6f), "RoofMetal"); roof.AddComponent<RainRoof>();
            var door = Instance("Assets/LastSignal/Prefabs/Interactions/Door.prefab", g, new Vector3(32.9f, 0, 24));
            door.name = "Maintenance service door"; Identity(door, "s023.depot.service-door"); doors.Add(door.GetComponent<DoorInteractable>());
            foreach (var renderer in door.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = Load<Material>(Kit + "Materials/Metal.mat");
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            Box("Interrupted repair bay divider", g, new Vector3(37, 1.2f, 29), new Vector3(.4f, 2.4f, 5), "Iron");
            Box("Workbench", g, new Vector3(42, .5f, 32), new Vector3(1, 1, 3), "Wood");
            // Small open checkpoint uses unscaled S013 modules, no second gameplay owner.
            for (int i = 0; i < 3; i++) Instance(Kit + "Prefabs/Wall_2m.prefab", g, new Vector3(10 + i * 2, 0, 34));
            for (int i = 0; i < 3; i++)
            {
                var side = Instance(Kit + "Prefabs/Window_2m.prefab", g, new Vector3(10, 0, 28 + i * 2)); side.transform.localRotation = Quaternion.Euler(0, -90, 0); PrefabUtility.RecordPrefabInstancePropertyModifications(side.transform);
            }
            var checkpointRoof = Box("Checkpoint rain roof", g, new Vector3(13, 3.3f, 31), new Vector3(6.4f, .2f, 6.4f), "RoofMetal"); checkpointRoof.AddComponent<RainRoof>();
            Box("Abandoned checkpoint desk", g, new Vector3(12, .5f, 32), new Vector3(2, 1, .8f), "Wood");
        }

        public static WorldCellContent[] Definitions(WorldCellManager manager)
        {
            var p = new SerializedObject(manager).FindProperty("definitions");
            return Enumerable.Range(0, p.arraySize).Select(i => p.GetArrayElementAtIndex(i).objectReferenceValue as WorldCellContent).ToArray();
        }

        public static void Validate(Report report)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var save = Only<SaveSession>(scene); var manager = Only<WorldCellManager>(scene);
            Require(save.SaveSlot == Slot && new SerializedObject(save).FindProperty("worldId").stringValue == World, "Save isolation invalid.");
            Require(save.ValidateAuthoring().Success, "S023 SaveSession authoring invalid.");
            ContentBuildValidation.RequireValid(ContentValidation.Roots(scene.GetRootGameObjects(), scene.path));
            var defs = Definitions(manager); Require(defs.Length == 4 && defs.All(d => d), "Expected preserved two cells plus two S023 cells.");
            Require(defs.Select(d => d.coordinate.Id).Distinct().Count() == 4, "Duplicate cell coordinates.");
            // Check cross-cell identities too, using the S019 validator rather than a parallel framework.
            ContentBuildValidation.RequireValid(ContentValidation.Roots(defs.Select(d => d.gameObject), ScenePath));
            foreach (int x in new[] { 3, 4 })
            {
                var definition = defs.Single(d => d.coordinate.Equals(new CellCoordinate(x, 0)));
                Require(AssetDatabase.GetAssetPath(definition) == CellPath(x), "Incorrect S023 cell reference.");
                ValidateCell(definition, report);
                var renderers = definition.GetComponentsInChildren<Renderer>(true);
                report.assets.Add(new DependencyRecord {
                    owner = CellPath(x), dependencies = AssetDatabase.GetDependencies(CellPath(x), true).OrderBy(p => p, StringComparer.Ordinal).ToArray(),
                    renderers = renderers.Length,
                    sharedMaterials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m).Distinct().Count(),
                    sharedMeshes = definition.GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh).Where(m => m).Distinct().Count()
                });
            }
            foreach (var path in ProfileNames.Select(n => Data + n + ".asset"))
            {
                var profile = Load<LootProfile>(path); Require(profile.Validate(out var error), error);
                for (int i = 0; i < profile.EntryCount; i++) Require(save.Catalog.EditorItems.Contains(profile.GetEntry(i).Item), "Loot absent from authoritative catalog.");
            }
        }

        static void ValidateCell(WorldCellContent definition, Report report)
        {
            Require(definition.transform.position == Vector3.zero && definition.transform.localScale == Vector3.one, "Cell uses absolute-world navigation; root must remain identity.");
            Require(definition.navigation && AssetDatabase.GetAssetPath(definition.navigation) == NavPath(definition.coordinate.x), "Native navigation missing.");
            Require(definition.GetComponentsInChildren<ZombieEncounter>(true).Length == 1, "Existing cell persistence supports exactly one encounter.");
            Require(definition.GetComponentsInChildren<NavMeshSurface>(true).All(s => !s.enabled), "Surface would duplicate WorldCellContent navigation ownership.");
            foreach (var t in definition.GetComponentsInChildren<Transform>(true).Where(t => t.GetComponent<LootSpawnPoint>() || t.GetComponent<DoorInteractable>() || t.GetComponent<ZombieEncounter>() || t.GetComponent<CellPortal>()))
                Require(CellCoordinate.FromWorld(t.position).Equals(definition.coordinate), "Entity outside owning cell: " + t.name);
            foreach (var renderer in definition.GetComponentsInChildren<Renderer>(true))
                Require(renderer.sharedMaterials.All(m => m && m.shader && !m.shader.name.Contains("InternalErrorShader")), "Missing material/shader: " + renderer.name);
            // Instantiate for collision/placement queries only. Never start gameplay or an encounter.
            var instance = Object.Instantiate(definition); instance.gameObject.SetActive(true);
            try
            {
                instance.InstallNavigation(); Physics.SyncTransforms(); Require(instance.CollisionReady(), "Collision readiness failed.");
                var layout = instance.GetComponent<PoiAuthoringLayout>();
                var so = new SerializedObject(layout.encounter);
                var zombie = (ZombieController)so.FindProperty("prefab").objectReferenceValue;
                var filter = new NavMeshQueryFilter { agentTypeID = zombie.GetComponent<NavMeshAgent>().agentTypeID, areaMask = NavMesh.AllAreas };
                Require(layout.navigationGeometry.GetComponent<NavMeshSurface>().agentTypeID == filter.agentTypeID, "Navigation agent mismatch.");
                Require(NavMesh.SamplePosition(layout.entry.position, out var start, .6f, filter), "Entry off navigation.");
                var targets = new List<Transform> { layout.retreat, layout.encounter.transform };
                targets.AddRange(layout.lootAnchors.Select(p => p.transform));
                // Portal stand points stay away from the solid terminal, within interaction reach.
                foreach (var portal in instance.GetComponentsInChildren<CellPortal>())
                    targets.Add(Marker("Portal approach", instance.transform, new Vector3(portal.transform.position.x, .05f, portal.transform.position.z - 1.5f)));
                foreach (var target in targets)
                {
                    Require(NavMesh.SamplePosition(target.position, out var end, .6f, filter), "Target off navigation: " + target.name);
                    var route = new NavMeshPath(); Require(NavMesh.CalculatePath(start.position, end.position, filter, route) && route.status == NavMeshPathStatus.PathComplete, "Unreachable: " + target.name);
                    float length = 0; for (int i = 1; i < route.corners.Length; i++) length += Vector3.Distance(route.corners[i - 1], route.corners[i]);
                    report.routes.Add(new RouteRecord { cell = definition.coordinate.Id, target = target.name, metres = length });
                }
                foreach (var point in layout.lootAnchors)
                    for (int i = 0; i < point.Profile.EntryCount; i++)
                        Require(LootPopulationService.PlacementValid(point, point.Profile.GetEntry(i).Item.WorldPrefab, null, out var error), point.StableId + ": " + error);
            }
            finally { instance.Release(); Object.DestroyImmediate(instance.gameObject); }
        }

        static LootSpawnPoint Loot(Transform parent, Vector3 at, string id, LootProfile profile)
        {
            var point = Marker(id, parent, at).gameObject.AddComponent<LootSpawnPoint>();
            Set(point, "stableId", id); Set(point, "profile", profile); return point;
        }
        static void Portal(Transform parent, string label, Vector3 at, string destination, Vector3 home)
        {
            var go = Box(label, parent, at, new Vector3(.5f, 2, .5f), "Metal");
            var portal = go.AddComponent<CellPortal>(); portal.destination = destination; portal.residentEntry = home;
            Sign(go.transform, label, new Vector3(0, .6f, -.27f));
        }
        static void Identity(GameObject go, string id) => Set(go.GetComponent<PersistentEntityId>() ?? go.AddComponent<PersistentEntityId>(), "id", id);
        static Transform Marker(string name, Transform parent, Vector3 world) { var t = Group(name, parent, Vector3.zero); t.position = world; return t; }
        static Transform Group(string name, Transform parent, Vector3 local) { var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = local; return t; }
        static GameObject Instance(string path, Transform parent, Vector3 local)
        { var go = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(path), parent); go.transform.localPosition = local; PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform); return go; }
        static GameObject Box(string name, Transform parent, Vector3 local, Vector3 size, string material, bool collision = true)
        {
            var root = Group(name, parent, local).gameObject;
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube); visual.name = "Surface"; visual.transform.SetParent(root.transform, false); visual.transform.localScale = size;
            Object.DestroyImmediate(visual.GetComponent<Collider>()); visual.GetComponent<Renderer>().sharedMaterial = Load<Material>(Kit + "Materials/" + material + ".mat");
            if (collision) root.AddComponent<BoxCollider>().size = size; return root;
        }
        static void Sign(Transform parent, string text, Vector3 local)
        {
            var label = Group("Wayfinding " + text, parent, local).gameObject.AddComponent<TextMesh>();
            label.text = text; label.fontSize = 48; label.characterSize = .065f; label.anchor = TextAnchor.MiddleCenter;
            label.color = new Color(.82f, .69f, .42f);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.GetComponent<Renderer>().sharedMaterial = label.font.material;
        }
        static T Only<T>(Scene scene) where T : Component
        { var found = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray(); Require(found.Length == 1, "Expected exactly one " + typeof(T).Name); return found[0]; }
        static T Load<T>(string path) where T : Object { var asset = AssetDatabase.LoadAssetAtPath<T>(path); Require(asset, "Missing existing dependency: " + path); return asset; }
        static void Set(Object owner, string name, object value)
        {
            Require(owner, "Missing owner for " + name); var so = new SerializedObject(owner); var p = so.FindProperty(name);
            Require(p != null, "Serialized contract changed: " + name);
            if (value is string text) p.stringValue = text; else if (value is bool flag) p.boolValue = flag;
            else if (value is int number) p.intValue = number; else p.objectReferenceValue = (Object)value;
            so.ApplyModifiedPropertiesWithoutUndo(); if (PrefabUtility.IsPartOfPrefabInstance(owner)) PrefabUtility.RecordPrefabInstancePropertyModifications(owner);
        }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
