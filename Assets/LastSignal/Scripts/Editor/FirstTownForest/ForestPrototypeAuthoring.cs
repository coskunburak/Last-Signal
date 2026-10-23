using System;
using System.IO;
using System.Linq;
using LastSignal.Forest;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LastSignal.Editor.FirstTownForest
{
    public static class ForestPrototypeAuthoring
    {
        public const string ScenePath = "Assets/LastSignal/Scenes/Validation/ForestPrototype120.unity";
        public const string SettingsPath = ForestPrototypeAssets.Root + "/Settings.asset";
        public const string Evidence = "Docs/Implementation/ForestPrototype120/Evidence";
        public const string Menu = "Last Signal/Forest 120/";

        static void Guard()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Exit Play Mode and Prefab Mode before forest authoring.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scene edits first. Forest authoring does not discard unsaved work.");
            if (!(GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset)) throw new InvalidOperationException("Use the project's existing HDRP quality level. No pipeline is changed by this tool.");
        }
        [MenuItem(Menu + "1 Validate source assets")]
        public static void ValidateSources()
        {
            foreach (var p in new[] { ForestPrototypeAssets.Trees + "/Spruce/Spruce 1.prefab", ForestPrototypeAssets.Trees + "/Spruce/Spruce 4.prefab",
                ForestPrototypeAssets.Trees + "/Spruce/Spruce 6.prefab", ForestPrototypeAssets.Trees + "/Spruce/Spruce 8.prefab", ForestPrototypeAssets.Trees + "/Spruce/Spruce 9.prefab",
                ForestPrototypeAssets.Trees + "/Birch/Birch 2.prefab", ForestPrototypeAssets.Fern + "/fern_02_4k.blend", ForestPrototypeAssets.Fir + "/fir_tree_01_4k.blend",
                ForestSources.Kit + "Prefabs/Bush.prefab", ForestSources.Kit + "Prefabs/Rock.prefab", ForestSources.Kit + "Prefabs/Grass.prefab", ForestSources.Kit + "Prefabs/Pine.prefab" })
                ForestPrototypeAssets.Load<GameObject>(p);
            foreach (string shader in new[] { "HDRP/Lit", "HDRP/TerrainLit", "Shader Graphs/Bark_HDRP", "Shader Graphs/Leaves_HDRP" }) ForestPrototypeAssets.ShaderNamed(shader);
            foreach (string suffix in new[] { "diff_4k.jpg", "rough_4k.jpg", "nor_gl_4k.exr", "disp_4k.png" }) ForestPrototypeAssets.Load<Texture2D>(ForestPrototypeAssets.Ground + suffix);
            Debug.Log("Forest120 source references resolved. Rendering, gameplay and performance still require manual acceptance.");
        }
        [MenuItem(Menu + "2 Create or regenerate isolated forest")]
        public static void Generate()
        {
            Guard();
            string sourceHash = ForestAuthoring.Hash(ForestAuthoring.SourceScene);
            var previous = SceneManager.GetActiveScene(); var target = SceneManager.GetSceneByPath(ScenePath);
            if (!target.IsValid() && File.Exists(ScenePath)) target = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            ForestPrototypeState existing = null;
            if (target.IsValid())
            {
                existing = ForestAuthoring.All<ForestPrototypeState>(target).SingleOrDefault();
                if (!existing) throw new InvalidOperationException("Target scene exists without ForestPrototypeState ownership. It will not be overwritten.");
            }
            string checkpoint = "Temp/Forest120-" + Guid.NewGuid().ToString("N") + ".unity";
            if (existing && !EditorSceneManager.SaveScene(target, checkpoint, true)) throw new IOException("Could not checkpoint the existing forest.");
            ForestPrototypeAssets.Folder(ForestPrototypeAssets.Root);
            var settings = AssetDatabase.LoadAssetAtPath<ForestPrototypeSettings>(SettingsPath);
            if (!settings) { settings = ScriptableObject.CreateInstance<ForestPrototypeSettings>(); AssetDatabase.CreateAsset(settings, SettingsPath); }
            string folder = ForestPrototypeAssets.Root + "/Generations/G" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            ForestPrototypeAssets.Folder(folder);
            var staging = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(staging); bool swapped = false;
            try
            {
                var root = new GameObject("ForestPrototype120 — authoring owner"); var state = root.AddComponent<ForestPrototypeState>();
                var snapshot = Object.Instantiate(settings); snapshot.name = "Authored settings snapshot";
                // Artist objects outside Generated participate as exclusion footprints; nothing is moved or deleted.
                if (existing)
                {
                    var blockers = ForestAuthoring.All<Collider>(target).Where(c => c.enabled && !c.isTrigger && (!existing.generated || !c.transform.IsChildOf(existing.generated.transform)));
                    snapshot.exclusions = snapshot.exclusions.Concat(blockers.Select(c => c.bounds)).ToArray();
                }
                AssetDatabase.CreateAsset(snapshot, folder + "/SettingsSnapshot.asset"); state.settings = snapshot; state.generationFolder = folder;
                var assets = new ForestPrototypeAssets(folder); assets.Build();
                state.generated = new GameObject("Generated — replaceable"); state.generated.transform.SetParent(root.transform);
                state.terrain = ForestPrototypeTerrain.Build(snapshot, assets, state.generated.transform, folder);
                ForestPrototypeTerrain.Populate(state, assets);
                var lighting = ForestPrototypeAtmosphere.Build(state.generated.transform, folder);
                CreateRig(state, lighting); Boundary(state);
                var surface = state.generated.AddComponent<NavMeshSurface>(); surface.collectObjects = CollectObjects.Children;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; surface.layerMask = ~(1 << 2);
                state.navigationNeedsBake = true;
                if (ForestAuthoring.Hash(ForestAuthoring.SourceScene) != sourceHash) throw new InvalidOperationException("FirstTownCore changed during authoring. Reconcile before retrying.");
                // Save only this operation's assets; unrelated dirty assets may belong to the user.
                foreach (var guid in AssetDatabase.FindAssets("", new[] { folder }))
                    AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
                AssetDatabase.SaveAssetIfDirty(settings);
                if (existing)
                {
                    var old = existing.generated;
                    state.generated.transform.SetParent(null);
                    SceneManager.MoveGameObjectToScene(state.generated, target);
                    state.generated.transform.SetParent(existing.transform, false);
                    EditorUtility.CopySerialized(state, existing);
                    existing.generated.GetComponentInChildren<ForestPrototypeExplorer>().state = existing;
                    existing.generated.GetComponentInChildren<ForestPrototypeCapture>().state = existing;
                    Object.DestroyImmediate(root); if (old) Object.DestroyImmediate(old);
                    swapped = true; EditorSceneManager.CloseScene(staging, true); SceneManager.SetActiveScene(target);
                    state = existing;
                }
                else { target = staging; SceneManager.SetActiveScene(target); }
                EditorSceneManager.MarkSceneDirty(target);
                if (!EditorSceneManager.SaveScene(target, ScenePath)) throw new IOException("Could not save the authored forest scene.");
                Directory.CreateDirectory(Evidence);
                File.WriteAllText(Evidence + "/authoring.json", JsonUtility.ToJson(new AuthoringReport(state, sourceHash), true));
                // A dedicated scene owns the lighting. Other saved scenes remain on disk, not loaded additively in gameplay.
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Selection.activeObject = ForestAuthoring.All<ForestPrototypeState>(SceneManager.GetActiveScene()).Single().gameObject;
                Debug.Log("Forest120 authored. No tests, NavMesh bake, visual acceptance or profiling were run.");
            }
            catch
            {
                if (staging.IsValid() && staging.isLoaded) EditorSceneManager.CloseScene(staging, true);
                if (swapped)
                {
                    if (target.IsValid() && target.isLoaded) EditorSceneManager.CloseScene(target, true);
                    File.Copy(checkpoint, ScenePath, true); AssetDatabase.ImportAsset(ScenePath);
                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                }
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                throw;
            }
        }
        static void CreateRig(ForestPrototypeState state, ForestPrototypeLighting lighting)
        {
            var validation = ForestPrototypeTerrain.Child("SpawnAndValidation", state.generated.transform);
            var player = ForestPrototypeTerrain.Child("Player — existing movement components", validation).gameObject;
            player.layer = 2; state.player = player.transform;
            player.transform.position = ForestPrototypeTerrain.Ground(state.terrain, state.settings.spawn) + Vector3.up * .12f;
            player.transform.rotation = Quaternion.Euler(0, 28, 0);
            var capsule = player.AddComponent<CharacterController>(); capsule.height = 1.8f; capsule.radius = .32f; capsule.center = Vector3.up * .9f;
            capsule.slopeLimit = 40; capsule.stepOffset = .25f; capsule.skinWidth = .025f;
            var input = player.AddComponent<PlayerInputReader>(); input.Configure(ForestPrototypeAssets.Load<InputActionAsset>("Assets/LastSignal/Settings/Input/InputSystem_Actions.inputactions")); input.ExternalUiOwner = true;
            var camera = ForestPrototypeTerrain.Child("GameplayCamera", player.transform).gameObject.AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.transform.localPosition = Vector3.up * 1.62f; camera.fieldOfView = 75; camera.nearClipPlane = .06f; camera.farClipPlane = 280;
            camera.gameObject.AddComponent<AudioListener>(); var hd = camera.gameObject.AddComponent<HDAdditionalCameraData>(); hd.antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
            hd.allowDynamicResolution = false;
            var stance = player.AddComponent<PlayerStance>(); var motor = player.AddComponent<FirstPersonMotor>(); var look = player.AddComponent<FirstPersonLook>();
            stance.Configure(input, capsule, camera.transform); motor.Configure(input, capsule, stance); look.Configure(input, camera);
            state.gameplayCamera = camera;
            var shots = ForestPrototypeTerrain.Child("Six player-height viewpoints", validation);
            Vector2[] positions = { state.settings.spawn, new Vector2(57, 51), new Vector2(24, 82), new Vector2(53, 60), new Vector2(57, 59), new Vector2(57, 51) };
            Vector3[] angles = { new Vector3(0, 28, 0), new Vector3(0, 36, 0), new Vector3(0, 65, 0), new Vector3(55, 42, 0), new Vector3(-12, 31, 0), new Vector3(0, 36, 0) };
            string[] names = { "01 Forest Trail", "02 Sunlit Clearing", "03 Dense Woodland", "04 Ground Close-Up", "05 Hero Tree", "06 Overcast Forest" };
            state.shots = new Transform[6];
            for (int i = 0; i < 6; i++)
            {
                var marker = ForestPrototypeTerrain.Child(names[i], shots); var p = FindClearPosition(state, positions[i]);
                marker.position = ForestPrototypeTerrain.Ground(state.terrain, p) + Vector3.up * 1.74f; marker.rotation = Quaternion.Euler(angles[i]); state.shots[i] = marker;
            }
            var explorer = player.AddComponent<ForestPrototypeExplorer>(); explorer.state = state; explorer.input = input; explorer.motor = motor; explorer.look = look; explorer.lighting = lighting;
            var capture = player.AddComponent<ForestPrototypeCapture>(); capture.state = state; capture.enabled = false;
        }
        static Vector2 FindClearPosition(ForestPrototypeState state, Vector2 desired)
        {
            var obstacles = state.generated.GetComponentsInChildren<Collider>().Where(c => !(c is TerrainCollider) && !(c is CharacterController)).ToArray();
            for (int i = 0; i < 128; i++)
            {
                float radius = i == 0 ? 0 : .3f * Mathf.Sqrt(i); float angle = i * 2.399963f;
                var p = desired + radius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var box = new Bounds(ForestPrototypeTerrain.Ground(state.terrain, p) + Vector3.up * .95f, new Vector3(1, 1.8f, 1));
                if (!obstacles.Any(c => c.bounds.Intersects(box))) return p;
            }
            throw new InvalidOperationException("No clear camera landing near " + desired);
        }
        static void Boundary(ForestPrototypeState state)
        {
            var bounds = ForestPrototypeTerrain.Child("Tile boundary — outer edge only", state.generated.transform);
            foreach (var p in new[] { new Vector3(-.5f, 10, 60), new Vector3(120.5f, 10, 60), new Vector3(60, 10, -.5f), new Vector3(60, 10, 120.5f) })
            {
                var g = ForestPrototypeTerrain.Child("Outer terrain limit", bounds); g.position = p;
                var c = g.gameObject.AddComponent<BoxCollider>(); c.size = p.x < 0 || p.x > 120 ? new Vector3(1, 30, 122) : new Vector3(122, 30, 1);
            }
        }
        [MenuItem(Menu + "3 Validate authored references")]
        public static void ValidateAuthored()
        {
            var state = Current();
            if (!state.terrain || !state.generated || !state.settings || !state.gameplayCamera || state.shots.Length != 6) throw new InvalidOperationException("Missing authored references.");
            if (state.terrain.terrainData.size != new Vector3(120, 18, 120)) throw new InvalidOperationException("Incorrect terrain size.");
            foreach (var filter in state.generated.GetComponentsInChildren<MeshFilter>(true))
                if (!filter.sharedMesh) throw new InvalidOperationException("Missing mesh on " + filter.name);
            foreach (var renderer in state.generated.GetComponentsInChildren<Renderer>(true))
                foreach (var material in renderer.sharedMaterials)
                    if (!material || !material.shader || !material.shader.isSupported || ShaderUtil.ShaderHasError(material.shader))
                        throw new InvalidOperationException("Missing/unsupported material on " + renderer.name);
            foreach (var t in state.generated.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0) throw new InvalidOperationException("Missing script: " + t.name);
            if (state.generated.GetComponentsInChildren<Rigidbody>(true).Length != 0) throw new InvalidOperationException("Decorative forest unexpectedly contains rigid bodies.");
            Debug.Log("Forest120 authored references resolved. This is not visual or gameplay acceptance.");
        }
        static ForestPrototypeState Current()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open " + ScenePath);
            return ForestAuthoring.All<ForestPrototypeState>(scene).Single();
        }
        [MenuItem(Menu + "4 Bake prototype navigation (explicit)")]
        public static void BakeNavigation()
        {
            Guard(); var s = Current(); s.generated.GetComponent<NavMeshSurface>().BuildNavMesh();
            s.navigationNeedsBake = false; EditorSceneManager.MarkSceneDirty(s.gameObject.scene); EditorSceneManager.SaveScene(s.gameObject.scene);
        }
        [MenuItem(Menu + "Lighting/Morning")]
        public static void Morning() => SetLighting(false);
        [MenuItem(Menu + "Lighting/Overcast")]
        public static void Overcast() => SetLighting(true);
        static void SetLighting(bool overcast)
        {
            var s = Current(); var l = s.generated.GetComponentInChildren<ForestPrototypeLighting>();
            Undo.RecordObjects(new Object[] { l, l.sun, l.sun.transform, l.volume, l.sun.GetComponent<HDAdditionalLightData>() }, "Forest lighting preset");
            l.Apply(overcast); EditorSceneManager.MarkSceneDirty(s.gameObject.scene);
        }
        [Serializable] sealed class AuthoringReport
        {
            public string scene = ScenePath, productionHash, generation, digest;
            public string tests = "NOT_RUN", visual = "NOT_RUN", performance = "NOT_RUN", navigation = "NEEDS_MANUAL_BAKE";
            public int trees, ferns, shrubs, rocks, grass;
            public AuthoringReport(ForestPrototypeState s, string hash)
            { productionHash = hash; generation = s.generationFolder; digest = s.placementDigest; trees = s.canopyCount; ferns = s.fernCount; shrubs = s.shrubCount; rocks = s.rockCount; grass = s.detailCount; }
        }
    }
}
