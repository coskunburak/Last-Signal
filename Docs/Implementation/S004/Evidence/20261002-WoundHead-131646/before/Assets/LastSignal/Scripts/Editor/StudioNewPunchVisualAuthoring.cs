using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Editor
{
    public static class StudioNewPunchVisualAuthoring
    {
        public const string SourcePath = "Assets/LastSignal/Assets/Zombie/NewPunch/ShirtlessZombieFree/Prefabs/ShirtlessZombie_BodyParts_FREE_URP.prefab";
        public const string ControllerPath = "Assets/LastSignal/Assets/Zombie/Enemies/Zombie/Animations/Controllers/AC_Zombie_Shambler.controller";
        public const string OutputPath = "Assets/LastSignal/Assets/Zombie/Enemies/Zombie/Prefabs/LS_Zombie_Shirtless_Visual.prefab";
        public const string RuntimePath = "Assets/Resources/LS_Zombie_Runtime.prefab";
        const string WoundTexturePath = "Assets/LastSignal/Assets/Zombie/Enemies/Zombie/Textures/T_Zombie_TorsoWound.png";
        const string WoundMaterialPath = "Assets/LastSignal/Assets/Zombie/Enemies/Zombie/Materials/M_Zombie_TorsoWound_URP.mat";
        const string WoundMeshPath = "Assets/LastSignal/Assets/Zombie/Enemies/Zombie/Prefabs/SM_Zombie_TorsoWound_Quad.asset";

        [MenuItem("Last Signal/Zombie/Create Shirtless Visual Candidate")]
        public static void CreateVisualCandidate()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before visual authoring.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(OutputPath))
                throw new InvalidOperationException("Candidate already exists; inspect it before replacing it: " + OutputPath);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            if (!source || !controller) throw new InvalidOperationException("Missing source prefab or Last Signal controller.");
            var instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (!instance) throw new InvalidOperationException("Unable to instantiate selected source prefab.");
            try
            {
                instance.name = "LS_Zombie_Shirtless_Visual";
                var animator = instance.GetComponent<Animator>();
                if (!animator || !animator.avatar || !animator.avatar.isValid || !animator.avatar.isHuman)
                    throw new InvalidOperationException("Selected source has no valid Humanoid Avatar.");
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                // The vendor component changes a shared material in OnValidate. The project-owned
                // visual uses the authored URP materials directly and has no vendor behavior script.
                foreach (var component in instance.GetComponents<MonoBehaviour>())
                    if (component && component.GetType().Name == "FreeZombie_EyesGlow")
                        UnityEngine.Object.DestroyImmediate(component);
                AddReference(animator, HumanBodyBones.Head, "HeadReference");
                AddReference(animator, HumanBodyBones.Chest, "ChestReference");
                AddReference(animator, HumanBodyBones.LeftFoot, "FeetReference");
                var result = PrefabUtility.SaveAsPrefabAsset(instance, OutputPath);
                if (!result) throw new InvalidOperationException("Failed to save project-owned visual candidate.");
                AssetDatabase.SaveAssets();
                Debug.Log("Created project-owned visual candidate: " + OutputPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        static void AddReference(Animator animator, HumanBodyBones boneId, string name)
        {
            var bone = animator.GetBoneTransform(boneId);
            if (!bone) throw new InvalidOperationException("Missing Humanoid bone " + boneId);
            var marker = new GameObject(name).transform;
            marker.SetParent(bone, false);
        }

        [MenuItem("Last Signal/Zombie/Integrate Shirtless Visual In Runtime")]
        public static void IntegrateRuntimeVisual()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before prefab integration.");
            var visual = AssetDatabase.LoadAssetAtPath<GameObject>(OutputPath);
            if (!visual) throw new InvalidOperationException("Create and inspect the project-owned visual candidate first.");
            var root = PrefabUtility.LoadPrefabContents(RuntimePath);
            try
            {
                Transform oldVisual = null;
                foreach (Transform child in root.transform)
                {
                    if (!child.GetComponentInChildren<Animator>(true)) continue;
                    if (oldVisual) throw new InvalidOperationException("Runtime has multiple visual roots.");
                    oldVisual = child;
                }
                if (!oldVisual) throw new InvalidOperationException("Runtime has no visual root.");
                if (oldVisual.name == "LS_Zombie_Shirtless_Visual")
                    throw new InvalidOperationException("Selected visual is already integrated.");
                var attached = PrefabUtility.InstantiatePrefab(visual, root.transform) as GameObject;
                if (!attached) throw new InvalidOperationException("Could not instantiate project-owned visual.");
                attached.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                attached.transform.localScale = Vector3.one;
                var animator = attached.GetComponent<Animator>();
                if (!animator || !animator.avatar || !animator.avatar.isValid || !animator.avatar.isHuman ||
                    !animator.runtimeAnimatorController || animator.applyRootMotion)
                    throw new InvalidOperationException("Visual Avatar/controller/root-motion contract is invalid.");
                var skins = attached.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (skins.Length != 15) throw new InvalidOperationException("Unexpected Shirtless body-parts renderer count.");
                var bounds = skins[0].bounds;
                foreach (var skin in skins) bounds.Encapsulate(skin.bounds);
                if (bounds.size.y < 1.4f || bounds.size.y > 2.3f || bounds.min.y < -.3f || bounds.min.y > .3f)
                    throw new InvalidOperationException("Visual bounds do not match the current 1.81 m gameplay capsule: " + bounds);
                var zones = new List<ZombieHitRegion>(root.GetComponentsInChildren<ZombieHitRegion>(true));
                if (zones.Count != 11) throw new InvalidOperationException("Expected eleven existing gameplay hit zones.");
                foreach (var zone in zones) RebindZone(zone, animator);
                root.GetComponent<ZombieAnimationPresenter>().Configure(animator);
                UnityEngine.Object.DestroyImmediate(oldVisual.gameObject);
                if (!PrefabUtility.SaveAsPrefabAsset(root, RuntimePath))
                    throw new InvalidOperationException("Unable to save runtime integration.");
                AssetDatabase.SaveAssets();
                Debug.Log("Integrated selected project-owned visual; gameplay root and eleven hit zones retained. Bounds=" + bounds);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void RebindZone(ZombieHitRegion zone, Animator animator)
        {
            var name = zone.gameObject.name;
            HumanBodyBones bone, end = HumanBodyBones.LastBone;
            switch (name)
            {
                case "Damage_Head": bone = HumanBodyBones.Head; break;
                case "Damage_Chest": bone = HumanBodyBones.Chest; break;
                case "Damage_Pelvis": bone = HumanBodyBones.Hips; break;
                case "Damage_LeftUpperArm": bone = HumanBodyBones.LeftUpperArm; end = HumanBodyBones.LeftLowerArm; break;
                case "Damage_LeftLowerArm": bone = HumanBodyBones.LeftLowerArm; end = HumanBodyBones.LeftHand; break;
                case "Damage_RightUpperArm": bone = HumanBodyBones.RightUpperArm; end = HumanBodyBones.RightLowerArm; break;
                case "Damage_RightLowerArm": bone = HumanBodyBones.RightLowerArm; end = HumanBodyBones.RightHand; break;
                case "Damage_LeftUpperLeg": bone = HumanBodyBones.LeftUpperLeg; end = HumanBodyBones.LeftLowerLeg; break;
                case "Damage_LeftLowerLeg": bone = HumanBodyBones.LeftLowerLeg; end = HumanBodyBones.LeftFoot; break;
                case "Damage_RightUpperLeg": bone = HumanBodyBones.RightUpperLeg; end = HumanBodyBones.RightLowerLeg; break;
                case "Damage_RightLowerLeg": bone = HumanBodyBones.RightLowerLeg; end = HumanBodyBones.RightFoot; break;
                default: throw new InvalidOperationException("Unexpected existing hit zone: " + name);
            }
            var anchor = animator.GetBoneTransform(bone);
            if (!anchor) throw new InvalidOperationException("Missing Humanoid bone: " + bone);
            var transform = zone.transform;
            transform.SetParent(anchor, false);
            if (end == HumanBodyBones.LastBone) return;
            var tip = animator.GetBoneTransform(end);
            if (!tip) throw new InvalidOperationException("Missing Humanoid tip bone: " + end);
            var local = anchor.InverseTransformPoint(tip.position);
            if (local.sqrMagnitude < .0001f) throw new InvalidOperationException("Collapsed bone segment: " + bone);
            transform.localPosition = local * .5f;
            transform.localRotation = Quaternion.LookRotation(local.normalized);
            var box = zone.GetComponent<BoxCollider>();
            var size = box.size; size.z = local.magnitude; box.size = size;
        }

        [MenuItem("Last Signal/Zombie/Bind Phase-1 Body Parts")]
        public static void BindPhaseOneBodyParts()
        {
            var root = PrefabUtility.LoadPrefabContents(RuntimePath);
            try
            {
                var health = root.GetComponent<ZombieHealth>();
                var animator = root.GetComponentInChildren<Animator>(true);
                if (!health || !animator || animator.gameObject.name != "LS_Zombie_Shirtless_Visual")
                    throw new InvalidOperationException("Selected Shirtless visual must be integrated first.");
                var skins = new Dictionary<string, SkinnedMeshRenderer>();
                foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (!skins.TryAdd(skin.name, skin)) throw new InvalidOperationException("Duplicate body mesh: " + skin.name);
                var zones = new Dictionary<string, Collider>();
                foreach (var zone in root.GetComponentsInChildren<ZombieHitRegion>(true))
                    zones.Add(zone.name, zone.HitCollider);
                zones["Damage_LeftHand"] = EnsureHandZone(animator, health, HumanBodyBones.LeftHand,
                    "Damage_LeftHand", ZombieBodyPart.LeftHand);
                zones["Damage_RightHand"] = EnsureHandZone(animator, health, HumanBodyBones.RightHand,
                    "Damage_RightHand", ZombieBodyPart.RightHand);
                var bindings = new[]
                {
                    Bind(ZombieBodyPart.Head, 75, true, animator.GetBoneTransform(HumanBodyBones.Head),
                        new Vector3(0, 0, 0), new Vector3(.24f, .28f, .24f), 2,
                        new[] { skins["Head"] }, new[] { zones["Damage_Head"] }),
                    Bind(ZombieBodyPart.LeftArm, 50, false, animator.GetBoneTransform(HumanBodyBones.LeftUpperArm),
                        new Vector3(0, -.25f, 0), new Vector3(.22f, .65f, .22f), 2.5f,
                        new[] { skins["ArmL"], skins["ForeArmL"], skins["HandL"] },
                        new[] { zones["Damage_LeftUpperArm"], zones["Damage_LeftLowerArm"], zones["Damage_LeftHand"] }),
                    Bind(ZombieBodyPart.RightArm, 50, false, animator.GetBoneTransform(HumanBodyBones.RightUpperArm),
                        new Vector3(0, -.25f, 0), new Vector3(.22f, .65f, .22f), 2.5f,
                        new[] { skins["ArmR"], skins["ForeArmR"], skins["HandR"] },
                        new[] { zones["Damage_RightUpperArm"], zones["Damage_RightLowerArm"], zones["Damage_RightHand"] }),
                    Bind(ZombieBodyPart.LeftHand, 35, false, animator.GetBoneTransform(HumanBodyBones.LeftHand),
                        Vector3.zero, new Vector3(.13f, .16f, .13f), 1,
                        new[] { skins["HandL"] }, new[] { zones["Damage_LeftHand"] }),
                    Bind(ZombieBodyPart.RightHand, 35, false, animator.GetBoneTransform(HumanBodyBones.RightHand),
                        Vector3.zero, new Vector3(.13f, .16f, .13f), 1,
                        new[] { skins["HandR"] }, new[] { zones["Damage_RightHand"] })
                };
                var sever = root.GetComponent<ZombieDismemberment>();
                if (!sever) sever = root.AddComponent<ZombieDismemberment>();
                sever.Configure(bindings, 40);
                if (!PrefabUtility.SaveAsPrefabAsset(root, RuntimePath))
                    throw new InvalidOperationException("Could not save Phase-1 body bindings.");
                AssetDatabase.SaveAssets();
                Debug.Log("Bound Phase-1 head, arms, hands and torso damage state to the canonical runtime prefab.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [MenuItem("Last Signal/Zombie/Bind Torso Wound Visual")]
        public static void BindTorsoWoundVisual()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(WoundTexturePath);
            if (!texture) throw new InvalidOperationException("Missing project-owned torso wound texture.");
            var importer = AssetImporter.GetAtPath(WoundTexturePath) as TextureImporter;
            if (!importer) throw new InvalidOperationException("Missing torso wound texture importer.");
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 512;
            importer.SaveAndReimport();
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(WoundTexturePath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(WoundMaterialPath);
            if (!material)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (!shader) throw new InvalidOperationException("Missing URP Unlit shader.");
                material = new Material(shader) { name = "M_Zombie_TorsoWound_URP" };
                AssetDatabase.CreateAsset(material, WoundMaterialPath);
            }
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            EditorUtility.SetDirty(material);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(WoundMeshPath);
            if (!mesh)
            {
                mesh = new Mesh { name = "SM_Zombie_TorsoWound_Quad" };
                mesh.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0),
                    new Vector3(-.5f, .5f, 0), new Vector3(.5f, .5f, 0) };
                mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
                mesh.triangles = new[] { 0, 1, 2, 2, 1, 3 };
                mesh.RecalculateNormals();
                AssetDatabase.CreateAsset(mesh, WoundMeshPath);
            }
            var root = PrefabUtility.LoadPrefabContents(RuntimePath);
            try
            {
                var sever = root.GetComponent<ZombieDismemberment>();
                var animator = root.GetComponentInChildren<Animator>(true);
                if (!sever || !animator) throw new InvalidOperationException("Production anatomy must be bound first.");
                var chest = animator.GetBoneTransform(HumanBodyBones.Chest);
                if (!chest) throw new InvalidOperationException("Humanoid chest bone is missing.");
                var wound = chest.Find("TorsoWound_Visual");
                if (!wound)
                {
                    var go = new GameObject("TorsoWound_Visual");
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, root.scene);
                    wound = go.transform;
                    wound.SetParent(chest, false);
                }
                wound.position = chest.position + animator.transform.forward * .15f + Vector3.up * .02f;
                wound.rotation = Quaternion.LookRotation(animator.transform.forward, Vector3.up);
                wound.localScale = new Vector3(.23f, .29f, 1);
                var filter = wound.GetComponent<MeshFilter>();
                if (!filter) filter = wound.gameObject.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                var renderer = wound.GetComponent<MeshRenderer>();
                if (!renderer) renderer = wound.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                wound.gameObject.SetActive(false);
                var serialized = new SerializedObject(sever);
                serialized.FindProperty("torsoWoundVisual").objectReferenceValue = wound.gameObject;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (!PrefabUtility.SaveAsPrefabAsset(root, RuntimePath))
                    throw new InvalidOperationException("Could not save torso wound visual.");
                AssetDatabase.SaveAssets();
                Debug.Log("Bound project-owned URP torso wound visual to production prefab.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static ZombieDismemberment.PartBinding Bind(ZombieBodyPart part, float threshold, bool fatal,
            Transform anchor, Vector3 center, Vector3 size, float impulse,
            SkinnedMeshRenderer[] renderers, Collider[] colliders)
        {
            if (!anchor) throw new InvalidOperationException("Missing detach anchor for " + part);
            return new ZombieDismemberment.PartBinding
            {
                part = part, severThreshold = threshold, fatalOnSever = fatal,
                detachAnchor = anchor, detachedColliderCenter = center,
                detachedColliderSize = size, impulse = impulse,
                attachedRenderers = renderers, hitColliders = colliders
            };
        }

        static Collider EnsureHandZone(Animator animator, ZombieHealth health, HumanBodyBones bone,
            string name, ZombieBodyPart part)
        {
            var anchor = animator.GetBoneTransform(bone);
            if (!anchor) throw new InvalidOperationException("Missing hand bone " + bone);
            var child = anchor.Find(name);
            if (!child) { child = new GameObject(name).transform; child.SetParent(anchor, false); }
            child.gameObject.layer = ZombieDamageAuthoring.HitLayer;
            var collider = child.GetComponent<BoxCollider>();
            if (!collider) collider = child.gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = false;
            collider.center = new Vector3(0, 0, .055f);
            collider.size = new Vector3(.12f, .07f, .16f);
            var region = child.GetComponent<ZombieHitRegion>();
            if (!region) region = child.gameObject.AddComponent<ZombieHitRegion>();
            region.Configure(health, DamageRegion.Body, 1, collider, part);
            return collider;
        }
    }
}
