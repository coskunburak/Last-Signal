using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace LastSignal.Editor
{
    /// <summary>B0B asset authoring and reproducible visual-only acceptance. No gameplay dependencies.</summary>
    public static class ZombieAssetIntegration
    {
        public const string Root = "Assets/LastSignal/Enemies/Zombie";
        public const string Model = "Assets/LastSignal/Assets/SZombie/SZombie_Variant_1/Unity/SK_SZombie_Variant_1.fbx";
        public const string Sources = "Assets/LastSignal/Assets/Kevin Iglesias/Zombie Animations";
        public const string Textures = "Assets/LastSignal/Assets/SZombie/SZombie_Variant_1/Textures/T_SZombie_Variant_1_";
        public const string Prefab = Root + "/Prefabs/LS_Zombie_Shambler.prefab";
        public const string Controller = Root + "/Animations/Controllers/AC_Zombie_Shambler.controller";
        public const string ScenePath = Root + "/Scenes/ZombieAssetAcceptance.unity";
        public const string Evidence = "Docs/Implementation/S004/Evidence/20260918-B0B";
        public static readonly string[] States = { "Idle", "Locomotion", "Attack", "HitReact", "Death", "IdleVariation", "DeathAlternate" };
        public static readonly string[] Clips = { "Zombie@Idle01", "Zombie@Walk01", "Zombie@Attack01", "Zombie@Damage01", "Zombie@Death01_A", "Zombie@Idle01_Action01", "Zombie@Death01_B" };

        public static AnimationClip Clip(string name)
        {
            if (name == "LS_Zombie_Death") return AssetDatabase.LoadAssetAtPath<AnimationClip>(Root + "/Animations/LS_Zombie_Death.anim");
            return AssetDatabase.FindAssets("t:AnimationClip", new[] { Sources }).Select(AssetDatabase.GUIDToAssetPath)
                .Distinct().SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<AnimationClip>().Single(c => c.name == name);
        }

        public static void AdaptDeath()
        {
            // Target-specific floor correction of existing motion, never vendor curve replacement.
            // Preserve every muscle/rotation curve and add measured vertical clearance only at landing.
            var source = Clip("Zombie@Death01_A");
            var path = Root + "/Animations/LS_Zombie_Death.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (!clip) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
            EditorUtility.CopySerialized(source, clip); clip.name = "LS_Zombie_Death";
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.y");
            var original = AnimationUtility.GetEditorCurve(source, binding);
            var sourceRows = File.ReadAllLines(Evidence + "/motion-samples.csv").Skip(1).Select(s => s.Split(',')).Where(c => c[0] == source.name).ToArray();
            var correction = new AnimationCurve();
            // This avatar's Mecanim body translation uses normalized human scale.
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            float scale = instance.GetComponentInChildren<Animator>().humanScale; UnityEngine.Object.DestroyImmediate(instance);
            foreach (var row in sourceRows)
            {
                float time = float.Parse(row[1], System.Globalization.CultureInfo.InvariantCulture);
                float minimum = float.Parse(row[16], System.Globalization.CultureInfo.InvariantCulture);
                float offset = Mathf.Max(0, .005f - minimum);
                correction.AddKey(time, original.Evaluate(time) + offset / scale);
            }
            for (int i = 0; i < correction.length; i++) { AnimationUtility.SetKeyLeftTangentMode(correction, i, AnimationUtility.TangentMode.ClampedAuto); AnimationUtility.SetKeyRightTangentMode(correction, i, AnimationUtility.TangentMode.ClampedAuto); }
            AnimationUtility.SetEditorCurve(clip, binding, correction);
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = false; AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
            controller.layers[0].stateMachine.states.Single(s => s.state.name == "Death").state.motion = clip;
            EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
        }

        [MenuItem("Last Signal/Zombie Assets/Build presentation assets")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            foreach (var dir in new[] { "Materials", "Textures", "Prefabs", "Animations/Controllers", "Scenes" }) Directory.CreateDirectory(Root + "/" + dir);
            Directory.CreateDirectory(Evidence);
            AssetDatabase.Refresh();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Avatar>().Single();
            if (!avatar.isValid || !avatar.isHuman) throw new InvalidOperationException("Hotstrike Humanoid is invalid.");

            // Decode original PNG values without sRGB sampling. Keep vendor files and importers untouched.
            var metallic = LoadPng("Metallic"); var smooth = LoadPng("Smoothness"); var ao = LoadPng("Oclussion");
            var rough = LoadPng("Roughness"); var vendorMask = LoadPng("Unity_Mask");
            var mp = metallic.GetPixels32(); var sp = smooth.GetPixels32(); var ap = ao.GetPixels32();
            var rp = rough.GetPixels32(); var vp = vendorMask.GetPixels32();
            double inverseError = 0, redError = 0, greenError = 0, alphaError = 0;
            var packed = new Color32[mp.Length];
            for (int i = 0; i < packed.Length; i++)
            {
                packed[i] = new Color32(mp[i].r, ap[i].r, 0, sp[i].r);
                inverseError += Math.Abs(sp[i].r + rp[i].r - 255);
                redError += Math.Abs(vp[i].r - mp[i].r); greenError += Math.Abs(vp[i].g - ap[i].r); alphaError += Math.Abs(vp[i].a - sp[i].r);
            }
            File.WriteAllText(Evidence + "/texture-channel-verification.txt", "Decoded source PNG byte comparisons; mean absolute channel error / 255\nSmoothness vs 1-Roughness=" + inverseError / packed.Length / 255 + "\nVendor mask R vs Metallic=" + redError / packed.Length / 255 + "\nG vs AO=" + greenError / packed.Length / 255 + "\nA vs Smoothness=" + alphaError / packed.Length / 255 + "\nAdapter explicitly packs metallic R, occlusion G, smoothness A.\n");
            var output = new Texture2D(metallic.width, metallic.height, TextureFormat.RGBA32, false, true);
            output.SetPixels32(packed); output.Apply();
            string maskPath = Root + "/Textures/T_Zombie_MetallicAO_Smoothness.png";
            File.WriteAllBytes(maskPath, output.EncodeToPNG());
            string normalPath = Root + "/Textures/T_Zombie_Normal.png";
            File.Copy(Textures + "OpenGL_Normal.png", normalPath, true);
            foreach (var texture in new[] { metallic, smooth, ao, rough, vendorMask, output }) UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(maskPath); AssetDatabase.ImportAsset(normalPath);
            var maskImporter = (TextureImporter)AssetImporter.GetAtPath(maskPath);
            maskImporter.sRGBTexture = false; maskImporter.alphaSource = TextureImporterAlphaSource.FromInput; maskImporter.SaveAndReimport();
            var normalImporter = (TextureImporter)AssetImporter.GetAtPath(normalPath);
            normalImporter.textureType = TextureImporterType.NormalMap; normalImporter.sRGBTexture = false; normalImporter.SaveAndReimport();
            var matPath = Root + "/Materials/M_Zombie_Shambler_URP.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, matPath); }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "A_Albedo.png"));
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath)); mat.SetFloat("_BumpScale", 1);
            mat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath));
            mat.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath));
            mat.SetFloat("_Metallic", 1); mat.SetFloat("_Smoothness", 1); mat.SetFloat("_OcclusionStrength", 1);
            mat.SetFloat("_Surface", 0); mat.SetFloat("_AlphaClip", 0); mat.SetFloat("_SmoothnessTextureChannel", 0);
            mat.EnableKeyword("_NORMALMAP"); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); mat.EnableKeyword("_OCCLUSIONMAP");
            EditorUtility.SetDirty(mat);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
            if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(Controller);
            var machine = controller.layers[0].stateMachine;
            foreach (var unused in machine.states.Where(s => s.state.name == "DeathAlternate").ToArray()) machine.RemoveState(unused.state);
            for (int i = 0; i < States.Length; i++)
            {
                if (States[i] == "DeathAlternate") continue; // Raw alternate failed the floor gate; preview/audit only.
                var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == States[i]);
                if (!state) state = machine.AddState(States[i], new Vector3(260 * (i % 3), 100 * (i / 3), 0));
                state.motion = i == 4 && Clip("LS_Zombie_Death") ? Clip("LS_Zombie_Death") : Clip(Clips[i]); state.writeDefaultValues = false;
                if (i == 0) machine.defaultState = state;
            }
            // Explicit states only. Future gameplay chooses them; Animator cannot invent combat decisions.
            EditorUtility.SetDirty(controller);
            var root = new GameObject("LS_Zombie_Shambler");
            try
            {
                var visual = new GameObject("VisualRoot").transform; visual.SetParent(root.transform, false);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model), visual);
                var animator = model.GetComponent<Animator>(); animator.avatar = avatar; animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                foreach (var sk in model.GetComponentsInChildren<SkinnedMeshRenderer>()) sk.sharedMaterials = new[] { mat };
                Anchor("HeadReference", animator.GetBoneTransform(HumanBodyBones.Head));
                Anchor("ChestReference", animator.GetBoneTransform(HumanBodyBones.UpperChest));
                Anchor("FeetReference", root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
        }

        static void Anchor(string name, Transform parent) { new GameObject(name).transform.SetParent(parent, false); }
        static Texture2D LoadPng(string suffix) { var t = new Texture2D(2, 2, TextureFormat.RGBA32, false, true); t.LoadImage(File.ReadAllBytes(Textures + suffix + ".png")); return t; }

        [MenuItem("Last Signal/Zombie Assets/Create acceptance scene")]
        public static void CreateScene()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var previous = SceneManager.GetActiveScene(); SceneManager.SetActiveScene(scene);
            try
            {
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), scene);
                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); floor.name = "NeutralGround";
                UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
                var floorPath = Root + "/Materials/M_AcceptanceGround.mat";
                var floorMat = AssetDatabase.LoadAssetAtPath<Material>(floorPath);
                if (!floorMat) { floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(floorMat, floorPath); }
                floorMat.color = new Color(.22f, .24f, .26f); floorMat.SetFloat("_Smoothness", .15f); floor.GetComponent<Renderer>().sharedMaterial = floorMat;
                var light = new GameObject("NeutralKey").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 2.2f; light.shadows = LightShadows.Soft; light.transform.rotation = Quaternion.Euler(40, -35, 0);
                var fill = new GameObject("NeutralFill").AddComponent<Light>(); fill.type = LightType.Directional; fill.intensity = .65f; fill.transform.rotation = Quaternion.Euler(25, 140, 0);
                var camera = new GameObject("AcceptanceCamera").AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.1f, .12f, .15f);
                camera.transform.position = new Vector3(3, 1.5f, 4); camera.transform.LookAt(new Vector3(0, .95f, 0)); camera.fieldOfView = 35;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.45f, .45f, .45f);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); }
            AssetDatabase.SaveAssets();
        }

        public static void Render(Camera camera, string path, int width = 960, int height = 960)
        {
            var priorTarget = camera.targetTexture; var priorActive = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try { camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG()); }
            finally { camera.targetTexture = priorTarget; RenderTexture.active = priorActive; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(image); }
        }

        public static void Capture(string clipName, float time, string name, int angle = 0)
        {
            var otherLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.enabled).ToArray();
            foreach (var light in otherLights) light.enabled = false;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            var previous = SceneManager.GetActiveScene(); SceneManager.SetActiveScene(scene);
            PlayableGraph graph = default;
            try
            {
                var animator = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Animator>()).Single();
                var camera = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>()).Single();
                foreach (var root in scene.GetRootGameObjects()) foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
                camera.cullingMask = 1 << 31;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                graph = PlayableGraph.Create("B0B visual sampling"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var playable = AnimationClipPlayable.Create(graph, Clip(clipName)); playable.SetApplyFootIK(false);
                AnimationPlayableOutput.Create(graph, "Retarget", animator).SetSourcePlayable(playable);
                graph.Play(); playable.SetTime(time); graph.Evaluate(0);
                camera.transform.position = angle == 1 ? new Vector3(4, 1.25f, 0) : angle == 2 ? new Vector3(0, 1.25f, -4) : new Vector3(0, 1.25f, 4);
                camera.transform.LookAt(new Vector3(0, .9f, 0));
                if (clipName.Contains("Death"))
                {
                    var focus = new Vector3(0, .65f, -.6f);
                    camera.transform.position = focus + (angle == 1 ? new Vector3(4.8f, 1.3f, 0) : angle == 2 ? new Vector3(0, 1.3f, -4.8f) : new Vector3(0, 1.3f, 4.8f));
                    camera.transform.LookAt(focus);
                }
                Render(camera, Evidence + "/" + name + ".png", name.StartsWith("frames/") ? 480 : 960, name.StartsWith("frames/") ? 480 : 960);
            }
            finally { if (graph.IsValid()) graph.Destroy(); SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); foreach (var light in otherLights) if (light) light.enabled = true; }
        }

        public static void CaptureSequences()
        {
            Directory.CreateDirectory(Evidence + "/frames");
            foreach (var name in Clips.Concat(new[] { "LS_Zombie_Death" }))
                for (int angle = 0; angle < 3; angle++)
                    for (int frame = 0; frame < 7; frame++)
                        Capture(name, Clip(name).length * frame / 6f, "frames/" + name.Replace('@', '_') + "_" + angle + "_" + frame, angle);
        }

        public static void AuditMotion()
        {
            var sb = new StringBuilder("clip,time,rootX,rootY,rootZ,rootYaw,hipsY,headY,leftFootX,leftFootY,leftFootZ,rightFootX,rightFootY,rightFootZ,leftHandZ,rightHandZ,meshMinY,meshMaxY\n");
            var summary = new StringBuilder();
            foreach (var clipName in Clips.Concat(new[] { "Zombie@Walk01 [RM]", "LS_Zombie_Death" }).Where(c => c != "LS_Zombie_Death" || Clip(c)))
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
                PlayableGraph graph = default;
                try
                {
                    var animator = go.GetComponentInChildren<Animator>(); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    var clip = Clip(clipName); graph = PlayableGraph.Create("B0B measurement"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var p = AnimationClipPlayable.Create(graph, clip); p.SetApplyFootIK(false);
                    AnimationPlayableOutput.Create(graph, "Retarget", animator).SetSourcePlayable(p); graph.Play(); graph.Evaluate(0);
                    Vector3 firstLeft = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
                    float min = float.MaxValue, max = float.MinValue;
                    for (int i = 0; i <= 120; i++)
                    {
                        float time = clip.length * i / 120f; p.SetTime(time); graph.Evaluate(0);
                        Vector3 left = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position, right = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
                        float lo = float.MaxValue, hi = float.MinValue;
                        foreach (var sk in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            var mesh = new Mesh(); sk.BakeMesh(mesh);
                            foreach (var v in mesh.vertices) { float y = sk.transform.TransformPoint(v).y; lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y); }
                            UnityEngine.Object.DestroyImmediate(mesh);
                        }
                        min = Mathf.Min(min, lo); max = Mathf.Max(max, hi);
                        var pos = animator.transform.position;
                        sb.AppendLine(string.Join(",", new object[] { clipName, time, pos.x, pos.y, pos.z, animator.transform.eulerAngles.y, animator.GetBoneTransform(HumanBodyBones.Hips).position.y, animator.GetBoneTransform(HumanBodyBones.Head).position.y, left.x, left.y, left.z, right.x, right.y, right.z, animator.GetBoneTransform(HumanBodyBones.LeftHand).position.z, animator.GetBoneTransform(HumanBodyBones.RightHand).position.z, lo, hi }));
                    }
                    summary.AppendLine(clipName + " meshMinY=" + min + " meshMaxY=" + max + " leftFootLoopGap=" + Vector3.Distance(firstLeft, animator.GetBoneTransform(HumanBodyBones.LeftFoot).position));
                    // Measure actual Mecanim root deltas on the target rig, separate from production policy.
                    animator.applyRootMotion = true; p.SetTime(0); graph.Evaluate(0);
                    Vector3 displacement = Vector3.zero; float yaw = 0;
                    for (int i = 1; i <= 120; i++) { graph.Evaluate(clip.length / 120f); displacement += animator.deltaPosition; yaw += Mathf.DeltaAngle(0, animator.deltaRotation.eulerAngles.y); }
                    summary.AppendLine("Root probe: delta=" + displacement.ToString("F6") + " yaw=" + yaw + " average=" + (displacement / clip.length).ToString("F6") + " importedAverage=" + clip.averageSpeed.ToString("F6"));
                }
                finally { if (graph.IsValid()) graph.Destroy(); UnityEngine.Object.DestroyImmediate(go); }
            }
            File.WriteAllText(Evidence + "/motion-samples.csv", sb.ToString()); File.WriteAllText(Evidence + "/motion-summary.txt", summary.ToString());
        }
    }
}
