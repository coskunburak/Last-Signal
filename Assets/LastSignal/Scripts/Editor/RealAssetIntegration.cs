using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LastSignal.Editor
{
    /// <summary>Reproducible, project-owned adapter. Never changes the source meshes or animation curves.</summary>
    public static class RealAssetIntegration
    {
        public const string Val = "Assets/LastSignal/VAL.fbx";
        public const string Rifle = "Assets/LastSignal/MR POLY/Low Poly Weapons Set/Models/Assault Rifle.fbx";
        public const string Prefab = "Assets/LastSignal/Prefabs/Combat/Weapon_AssaultRifle.prefab";
        public const string Evidence = "Docs/Implementation/Combat/Evidence/20260917-RealAssets";
        const string Controller = "Assets/LastSignal/Animations/VAL_MRPoly.controller";

        public static AnimationClip Clip(string name) => AssetDatabase.LoadAllAssetsAtPath(Val)
            .OfType<AnimationClip>().First(c => c.name == "LVA4_Armature|wpn_val_" + name);
        static Transform Bone(GameObject rig, string name) => rig.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
        static Transform Child(string name, Transform parent, Vector3 position)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = position; return t;
        }
        static void Set(UnityEngine.Object obj, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(obj); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
        static Material Material(string name)
        {
            string path = "Assets/LastSignal/Materials/" + name + "_URP.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material) return material;
            var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/LastSignal/MR POLY/Low Poly Weapons Set/Materials/" + name + ".mat");
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", source.GetColor("_Color"));
            material.SetFloat("_Metallic", source.GetFloat("_Metallic"));
            material.SetFloat("_Smoothness", source.GetFloat("_Glossiness"));
            if (name == "Glass_01_Mat")
            {
                material.SetColor("_BaseColor", new Color(.35f, .65f, .7f, .08f));
                material.SetFloat("_Surface", 1); material.SetFloat("_ZWrite", 0);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
                material.SetOverrideTag("RenderType", "Transparent");
            }
            AssetDatabase.CreateAsset(material, path); return material;
        }
        static void RifleMaterials(GameObject model)
        {
            var black = Material("Color_Black_Metal_01_Mat");
            var gray = Material("Color_Gray_Metal_01_Mat");
            var dark = Material("Color_Black_01_Mat");
            model.GetComponent<Renderer>().sharedMaterials = new[] { black, dark, gray, Material("Glass_01_Mat") };
            model.transform.Find("Magazine.001").GetComponent<Renderer>().sharedMaterials = new[] { dark, black };
            model.transform.Find("Trigger").GetComponent<Renderer>().sharedMaterials = new[] { dark };
        }
        static AnimatorController MakeController()
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
            if (ctrl) return ctrl;
            ctrl = AnimatorController.CreateAnimatorControllerAtPath(Controller);
            ctrl.AddParameter("WeaponState", AnimatorControllerParameterType.Int);
            ctrl.AddParameter("AimAmount", AnimatorControllerParameterType.Float);
            var sm = ctrl.layers[0].stateMachine;
            var idle = sm.AddState("Ready"); idle.motion = Clip("idle"); sm.defaultState = idle;
            foreach (var pair in new[] { ("Equip", "draw"), ("Fire", "shoot"), ("Reload", "reload"), ("EmptyReload", "reload_full"), ("Unequip", "hide") })
            {
                ctrl.AddParameter(pair.Item1, AnimatorControllerParameterType.Trigger);
                var state = sm.AddState(pair.Item1); state.motion = Clip(pair.Item2);
                var enter = sm.AddAnyStateTransition(state); enter.AddCondition(AnimatorConditionMode.If, 0, pair.Item1);
                enter.duration = pair.Item1 == "Fire" ? 0 : .035f; enter.hasFixedDuration = true; enter.canTransitionToSelf = pair.Item1 == "Fire";
                var exit = state.AddTransition(idle); exit.hasExitTime = true; exit.exitTime = 1; exit.duration = .035f; exit.hasFixedDuration = true;
            }
            return ctrl;
        }

        [MenuItem("Last Signal/S002/Build Real Asset Adapter")]
        public static void BuildAdapter()
        {
            var root = new GameObject("FP_AssaultRifle_VAL_MRPoly");
            try
            {
                var viewRoot = Child("ViewmodelRoot", root.transform, Vector3.zero);
                var rig = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Val), viewRoot);
                rig.name = "VAL_Armature";
                rig.transform.localRotation = Quaternion.Euler(0, 180, 0);
                rig.transform.localPosition = new Vector3(0, -.08805f, 0);
                foreach (var camera in rig.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
                rig.transform.Find("Environment").gameObject.SetActive(false);
                rig.transform.Find("VAL_Model").gameObject.SetActive(false);
                var animator = rig.GetComponent<Animator>();
                if (!animator) animator = rig.AddComponent<Animator>();
                animator.runtimeAnimatorController = MakeController(); animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                Clip("idle").SampleAnimation(rig, 0);

                // Place the actual MR POLY model in VAL coordinates before preserving its world pose under the bone.
                var rifle = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Rifle), rig.transform);
                rifle.name = "MRPoly_RifleBody";
                rifle.transform.localPosition = new Vector3(-.002f, -.19f, -.321f);
                rifle.transform.localRotation = Quaternion.Euler(0, 180, 0);
                rifle.transform.localScale = Vector3.one;
                RifleMaterials(rifle);
                var magazine = rifle.transform.Find("Magazine.001");
                rifle.transform.SetParent(Bone(rig, "wpn_body"), true);
                magazine.SetParent(Bone(rig, "mag"), true); magazine.name = "MRPoly_Magazine";
                var muzzle = Child("Muzzle", rifle.transform, new Vector3(.049187f, .195646f, .478153f));
                var sight = Child("AimReference", rifle.transform, new Vector3(.049196f, .266454f, -.120204f));
                var casing = Child("CasingEjection", rifle.transform, new Vector3(.079f, .203f, -.050f));
                Child("RightHandReference", Bone(rig, "hand.R"), Vector3.zero);
                Child("LeftHandReference", Bone(rig, "hand.L"), Vector3.zero);
                var weapon = root.AddComponent<WeaponController>();
                Set(weapon, "definition", AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/LastSignal/Runtime/Combat/WeaponDefinition_AssaultRifle.asset"));
                Set(weapon, "muzzle", muzzle); Set(weapon, "aimReference", sight);
                var view = root.AddComponent<WeaponViewPresenter>(); view.Configure(weapon, null, viewRoot);
                var so = new SerializedObject(view);
                so.FindProperty("hipPosition").vector3Value = new Vector3(.02f, -.045f, 0);
                so.FindProperty("alignToSight").boolValue = true;
                so.FindProperty("sightDistance").floatValue = .22f;
                so.FindProperty("recoilKickback").floatValue = .002f;
                so.ApplyModifiedPropertiesWithoutUndo();
                root.AddComponent<WeaponAnimationPresenter>().Configure(weapon, animator);
                root.AddComponent<WeaponRecoilController>().Configure(weapon, null);
                var vfx = root.AddComponent<WeaponVfxPresenter>(); vfx.Configure(weapon); Set(vfx, "casingEjectionPoint", casing);
                var flashObject = Child("MuzzleFlash", muzzle, Vector3.zero).gameObject;
                var flash = flashObject.AddComponent<ParticleSystem>(); flash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = flash.main; main.playOnAwake = false; main.loop = false; main.duration = .07f;
                main.startLifetime = .035f; main.startSpeed = .4f; main.startSize = .07f;
                main.startColor = new Color(1, .7f, .2f); main.maxParticles = 8;
                main.scalingMode = ParticleSystemScalingMode.Shape;
                var emission = flash.emission; emission.rateOverTime = 0; emission.SetBursts(new[] { new ParticleSystem.Burst(0, 3) });
                var shape = flash.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 12; shape.radius = .008f;
                var flashMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/LastSignal/Materials/MuzzleFlash.mat");
                if (!flashMaterial) { flashMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); flashMaterial.SetColor("_BaseColor", new Color(1, .65f, .12f)); AssetDatabase.CreateAsset(flashMaterial, "Assets/LastSignal/Materials/MuzzleFlash.mat"); }
                flash.GetComponent<ParticleSystemRenderer>().sharedMaterial = flashMaterial;
                Set(vfx, "muzzleFlash", flash);
                foreach (var r in rig.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
                AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            Debug.Log("REAL_ASSET_ADAPTER built from VAL and MR POLY source meshes.");
        }

        [MenuItem("Last Signal/S002/Integrate Production Player")]
        public static void IntegratePlayer()
        {
            const string path = "Assets/LastSignal/Prefabs/Player.prefab";
            var player = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var combat = player.GetComponent<PlayerCombatController>();
                Set(combat, "startingWeapon", AssetDatabase.LoadAssetAtPath<GameObject>(Prefab).GetComponent<WeaponController>());
                var camera = player.GetComponentInChildren<Camera>(); camera.nearClipPlane = .01f; camera.cullingMask &= ~(1 << 30);
                if (!player.transform.Find("WorldBody"))
                {
                    var body = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/tt-3d/LowPolySci-FiStarterPack/Character/Models/space_crew_man.fbx"), player.transform);
                    body.name = "WorldBody";
                    foreach (var t in body.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30;
                    var anim = body.GetComponent<Animator>();
                    var hand = anim && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.RightHand) : null;
                    var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Rifle), hand ? hand : body.transform);
                    model.name = "World_AssaultRifle_MRPoly"; RifleMaterials(model);
                    model.transform.localPosition = Vector3.zero;
                    foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30;
                    // World body is kept as a distinct rig for later locomotion/weapon-pose animation.
                    PrefabUtility.SaveAsPrefabAsset(body, "Assets/LastSignal/Prefabs/Combat/WorldBody_tt3d.prefab");
                }
                PrefabUtility.SaveAsPrefabAsset(player, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
            EditorBuildSettings.scenes = new[] {
                new EditorBuildSettingsScene("Assets/LastSignal/Scenes/S001Acceptance.unity", true),
                new EditorBuildSettingsScene("Assets/LastSignal/Scenes/CombatAcceptance.unity", true) };
            AssetDatabase.SaveAssets();
        }

        public static void Render(Camera camera, string name, int width = 1280, int height = 720)
        {
            Directory.CreateDirectory(Evidence);
            var previous = camera.targetTexture; var active = RenderTexture.active;
            var rt = new RenderTexture(width, height, 24); var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
                File.WriteAllBytes(Evidence + "/" + name + ".png", tex.EncodeToPNG());
            }
            finally { camera.targetTexture = previous; RenderTexture.active = active; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex); }
        }
        public static void Preview(string clip, float time, string name)
        {
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            var cameraObject = new GameObject("AdapterPreviewCamera");
            try
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
                var rig = root.transform.Find("ViewmodelRoot/VAL_Armature").gameObject;
                root.transform.Find("ViewmodelRoot").localPosition = new Vector3(.02f, -.045f, 0);
                Clip(clip).SampleAnimation(rig, time);
                var cam = cameraObject.AddComponent<Camera>(); cam.cullingMask = 1 << 31;
                cam.nearClipPlane = .01f; cam.fieldOfView = 75; cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(.18f, .22f, .27f);
                Render(cam, name);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(cameraObject); }
        }
    }
}
