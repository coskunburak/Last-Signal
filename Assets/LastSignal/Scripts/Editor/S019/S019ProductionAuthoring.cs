using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LastSignal.Authoring;
using LastSignal.Loot;
using LastSignal.Persistence;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LastSignal.Editor.S019
{
    // Explicit user-run batch authoring only; never an import/scene-open callback.
    public static class S019ProductionAuthoring
    {
        public const string ScenePath = "Assets/LastSignal/Scenes/S019/S019Pilot.unity";
        public const string NavigationPath = "Assets/LastSignal/Data/S019/S019PilotNavigation.asset";
        public const string SourceScene = "Assets/LastSignal/Scenes/Validation/PersistenceAcceptance.unity";
        public const string PilotSlot = "s019-pilot";
        public static readonly string[] Families = { "EnvironmentColor", "StaticMesh", "Audio" };
        public static readonly string[] Exemplars = {
            "Assets/LastSignal/Art/S013/Production/Textures/Wood.png",
            "Assets/LastSignal/Models/Weapons/Crowbar/Source/Crowbar.obj",
            "Assets/ThirdParty/S15 Sound Pack/FreeWeaponSFX_v1.1/AssaultRifle/Foley/sfx_wpn_ar_foley_aim_02.wav"
        };
        public static string CopyPath(int index) => ContentImportPolicy.Root + Families[index] + "/S019Exemplar" + Path.GetExtension(Exemplars[index]);
        public static string PresetPath(int index) => ContentImportPolicy.Presets + Families[index] + ".preset";
        [Serializable] public sealed class Evidence
        {
            public string technicalAuthoring = "NOT_RUN", manualAcceptance = "NOT_RUN", runtimeSaveAcceptance = "NOT_RUN";
            public string previousAuthoringTime = "UNKNOWN", manualAuthoringTime = "NOT_MEASURED", scene = ScenePath;
            public double elapsedSeconds;
            public bool reusedScene;
            public int nativePresets, reimports, validNavigationRoutes;
            public string error;
        }
        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use the user-run Tools/s019-author.py command with Unity closed.");
            string evidencePath = Environment.GetEnvironmentVariable("LASTSIGNAL_S019_AUTHOR_RESULT");
            if (string.IsNullOrEmpty(evidencePath) || !Path.IsPathRooted(evidencePath)) throw new InvalidOperationException("Absolute authoring result path required.");
            var result = new Evidence(); var created = new List<string>(); var watch = Stopwatch.StartNew();
            try
            {
                // Preflight all dependencies before creating anything. No fallback/third-party install.
                foreach (var path in Exemplars) if (!AssetImporter.GetAtPath(path)) throw new FileNotFoundException("Missing approved exemplar", path);
                if (!AssetDatabase.LoadAssetAtPath<GameObject>(PoiAuthoring.TemplatePath) || !File.Exists(SourceScene)) throw new InvalidOperationException("Pilot template/source scene missing.");
                for (int i = 0; i < Families.Length; i++)
                {
                    string presetPath = PresetPath(i), copyPath = CopyPath(i);
                    var preset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);
                    if (!preset)
                    {
                        RefuseExistingFile(presetPath);
                        preset = ContentImportPolicy.CreateScopedPreset(AssetImporter.GetAtPath(Exemplars[i]), Families[i]);
                        created.Add(presetPath); AssetDatabase.CreateAsset(preset, presetPath);
                    }
                    if (!File.Exists(copyPath))
                    {
                        created.Add(copyPath);
                        if (!AssetDatabase.CopyAsset(Exemplars[i], copyPath)) throw new IOException("Cannot copy exemplar: " + copyPath);
                    }
                    AssetDatabase.ImportAsset(copyPath, ImportAssetOptions.ForceSynchronousImport);
                    VerifyReimport(copyPath, preset, result);
                    result.nativePresets++;
                }
                result.reusedScene = File.Exists(ScenePath);
                if (result.reusedScene) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                else CreatePilot(created);
                var scene = SceneManager.GetActiveScene();
                var layouts = Components<PoiAuthoringLayout>(scene); Require(layouts.Length == 1, "Exactly one pilot layout is required.");
                ValidatePilot(scene, layouts[0], result);
                AssetDatabase.SaveAssets();
                result.technicalAuthoring = "PASS";
            }
            catch (Exception error)
            {
                result.technicalAuthoring = "FAIL"; result.error = error.ToString();
                // Never save an existing/source scene during failure cleanup. Delete only assets
                // first created by this invocation; preserve all pre-existing user content.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                foreach (var path in created.AsEnumerable().Reverse()) AssetDatabase.DeleteAsset(path);
                throw;
            }
            finally
            {
                result.elapsedSeconds = watch.Elapsed.TotalSeconds;
                File.WriteAllText(evidencePath, JsonUtility.ToJson(result, true));
            }
        }
        static void RefuseExistingFile(string path)
        { if (File.Exists(path)) throw new IOException("Existing invalid asset will not be overwritten: " + path); }
        public static string ImporterSnapshot(AssetImporter importer, bool excludeControlled, string family)
        {
            var preset = new Preset(importer);
            try
            {
                var leaves = ContentImportPolicy.Fields(family);
                return string.Join("\n", preset.PropertyModifications.Where(p => !excludeControlled ||
                    !leaves.Any(leaf => p.propertyPath == leaf || p.propertyPath.EndsWith("." + leaf, StringComparison.Ordinal)))
                    .Select(p => p.propertyPath + "=" + p.value + "|" + ObjectReference(p.objectReference))
                    .OrderBy(x => x, StringComparer.Ordinal));
            }
            finally { Object.DestroyImmediate(preset); }
        }
        static string ObjectReference(Object obj)
        {
            if (!obj) return "";
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out long id)) throw new InvalidOperationException("Importer contains an unsaved reference.");
            return guid + ":" + id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        static void VerifyReimport(string path, Preset preset, Evidence result)
        {
            string family = ContentImportPolicy.Profile(path);
            var importer = AssetImporter.GetAtPath(path); Require(importer && preset.CanBeAppliedTo(importer), "Preset/importer type mismatch: " + path);
            string protectedBefore = ImporterSnapshot(importer, true, family);
            Require(ContentImportPolicy.Apply(path, importer, preset), "Preset has no applicable controlled fields: " + path);
            string expected = ImporterSnapshot(importer, false, family);
            for (int repeat = 0; repeat < 2; repeat++)
            {
                importer.SaveAndReimport(); // Explicit invocation, never called by a postprocessor.
                importer = AssetImporter.GetAtPath(path);
                Require(ImporterSnapshot(importer, true, family) == protectedBefore, "Protected importer settings changed: " + path);
                Require(ImporterSnapshot(importer, false, family) == expected, "Non-idempotent import: " + path);
                result.reimports++;
            }
        }
        static T[] Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        static void CreatePilot(List<string> created)
        {
            RefuseExistingFile(NavigationPath);
            var scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
            var flows = Components<SessionFlow>(scene); var saves = Components<SaveSession>(scene);
            Require(flows.Length == 1 && saves.Length == 1, "Source requires exactly one existing session/save owner.");
            var oldEncounters = Components<ZombieEncounter>(scene);
            Require(oldEncounters.Length == 1, "Source one-encounter topology changed; refusing to guess.");
            var pilot = PoiAuthoring.CreateNew(AssetDatabase.LoadAssetAtPath<GameObject>(PoiAuthoring.TemplatePath), scene);
            pilot.transform.position = new Vector3(6, .1f, 6);
            var layout = pilot.GetComponent<PoiAuthoringLayout>();
            flows[0].ConfigureEncounter(layout.encounter);
            Object.DestroyImmediate(oldEncounters[0]); // Only in the unsaved isolated scene copy.
            var saveData = new SerializedObject(saves[0]);
            saveData.FindProperty("worldId").stringValue = "world.s019.pilot.v1";
            saveData.FindProperty("saveSlot").stringValue = PilotSlot;
            saveData.ApplyModifiedPropertiesWithoutUndo();
            var surface = layout.navigationGeometry.GetComponent<NavMeshSurface>();
            foreach (var old in Components<NavMeshSurface>(scene)) if (old != surface) Object.DestroyImmediate(old);
            // One bake for the copied playable scene includes ground, shelter and pilot geometry.
            surface.collectObjects = CollectObjects.All; surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize = true; surface.voxelSize = .08f;
            surface.overrideTileSize = true; surface.tileSize = 128;
            surface.BuildNavMesh(); Require(surface.navMeshData, "Pilot navigation bake produced no data.");
            created.Add(NavigationPath); AssetDatabase.CreateAsset(surface.navMeshData, NavigationPath);
            PrefabUtility.RecordPrefabInstancePropertyModifications(surface);
            PrefabUtility.RecordPrefabInstancePropertyModifications(pilot.transform);
            var loot = flows[0].GetComponent<LootPopulationService>(); Require(loot, "Existing loot owner missing.");
            int seed = Enumerable.Range(12345, 10000).First(value => layout.lootAnchors.All(p => p.Profile.Select(value, p.StableId).Outcome == LootOutcome.Spawned));
            var lootData = new SerializedObject(loot); lootData.FindProperty("overrideSeed").boolValue = true; lootData.FindProperty("explicitSeed").intValue = seed; lootData.ApplyModifiedPropertiesWithoutUndo();
            Require(saves[0].ValidateAuthoring().Success, "Existing SaveSession authoring contract rejected the isolated pilot.");
            EditorSceneManager.MarkSceneDirty(scene);
            created.Add(ScenePath); Require(EditorSceneManager.SaveScene(scene, ScenePath), "Cannot save isolated pilot scene.");
        }
        static void ValidatePilot(Scene scene, PoiAuthoringLayout layout, Evidence result)
        {
            Require(scene.path == ScenePath, "Unexpected pilot scene path.");
            ContentBuildValidation.RequireValid(ContentValidation.Roots(scene.GetRootGameObjects(), scene.path));
            Require(Components<SessionFlow>(scene).Length == 1 && Components<LootPopulationService>(scene).Length == 1 && Components<ZombieEncounter>(scene).Length == 1, "Pilot must reuse exactly one of each existing gameplay owner.");
            var saves = Components<SaveSession>(scene); Require(saves.Length == 1 && saves[0].SaveSlot == PilotSlot && saves[0].ValidateAuthoring().Success, "Pilot save isolation/composition invalid.");
            var surface = layout.navigationGeometry.GetComponent<NavMeshSurface>();
            Require(surface && surface.navMeshData && AssetDatabase.GetAssetPath(surface.navMeshData) == NavigationPath, "Baked native navigation reference missing.");
            surface.AddData();
            var filter = new NavMeshQueryFilter { agentTypeID = surface.agentTypeID, areaMask = NavMesh.AllAreas };
            Require(NavMesh.SamplePosition(layout.entry.position, out var entry, .6f, filter), "Entry not on pilot navigation.");
            var targets = new List<Transform> { layout.retreat, layout.encounter.transform };
            targets.AddRange(layout.lootAnchors.Select(x => x.transform));
            var loop = saves[0].GetComponent<LastSignal.Shelter.ShelterLoop>(); targets.Add(loop.OutsideAnchor);
            foreach (var target in targets)
            {
                Require(NavMesh.SamplePosition(target.position, out var end, .6f, filter), "Marker not on navigation: " + target.name);
                var path = new NavMeshPath();
                Require(NavMesh.CalculatePath(entry.position, end.position, filter, path) && path.status == NavMeshPathStatus.PathComplete, "Incomplete route to " + target.name);
                result.validNavigationRoutes++;
            }
        }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
