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
                var previousWound = new SerializedObject(sever).FindProperty("torsoWoundVisual").objectReferenceValue as GameObject;
                sever.Configure(bindings, 40, previousWound);
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
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(WoundTexturePath);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!texture || !shader) throw new InvalidOperationException("Missing wound texture or URP Lit shader.");
            var root = PrefabUtility.LoadPrefabContents(RuntimePath);
            Mesh fitted = null;
            try
            {
                var sever = root.GetComponent<ZombieDismemberment>();
                var animator = root.GetComponentInChildren<Animator>(true);
                if (!sever || !animator) throw new InvalidOperationException("Production anatomy must be bound first.");
                var chest = animator.GetBoneTransform(HumanBodyBones.Chest);
                SkinnedMeshRenderer torso = null;
                foreach (var skin in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (skin.name == "Ribcage") { torso = skin; break; }
                if (!chest || !torso) throw new InvalidOperationException("Chest/Ribcage binding missing.");
                // Fit before modifying any saved assets. Failure leaves the existing presentation intact.
                fitted = FitTorsoWound(torso, chest.position + Vector3.up * .02f, animator.transform);
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(WoundMeshPath);
                if (!mesh) { mesh = fitted; AssetDatabase.CreateAsset(mesh, WoundMeshPath); fitted = null; }
                else { EditorUtility.CopySerialized(fitted, mesh); EditorUtility.SetDirty(mesh); }
                var material = AssetDatabase.LoadAssetAtPath<Material>(WoundMaterialPath);
                if (!material)
                {
                    material = new Material(shader) { name = "M_Zombie_TorsoWound_URP" };
                    AssetDatabase.CreateAsset(material, WoundMaterialPath);
                }
                material.shader = shader;
                material.shaderKeywords = Array.Empty<string>();
                material.SetTexture("_BaseMap", texture);
                material.SetColor("_BaseColor", new Color(.88f, .88f, .88f, 1));
                material.SetFloat("_Metallic", 0);
                material.SetFloat("_Smoothness", .38f);
                material.SetFloat("_Surface", 1);
                material.SetFloat("_Blend", 0);
                material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Back);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_SrcBlendAlpha", 1);
                material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0);
                material.SetFloat("_ReceiveShadows", 1);
                material.SetColor("_EmissionColor", Color.black);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetShaderPassEnabled("ShadowCaster", false);
                material.SetShaderPassEnabled("DepthOnly", false);
                EditorUtility.SetDirty(material);
                Transform wound = null;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "TorsoWound_Visual") { wound = t; break; }
                if (!wound)
                {
                    var go = new GameObject("TorsoWound_Visual");
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, root.scene);
                    wound = go.transform;
                }
                wound.SetParent(torso.transform, false);
                wound.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                wound.localScale = Vector3.one;
                var oldFilter = wound.GetComponent<MeshFilter>();
                var oldRenderer = wound.GetComponent<MeshRenderer>();
                if (oldFilter) UnityEngine.Object.DestroyImmediate(oldFilter);
                if (oldRenderer) UnityEngine.Object.DestroyImmediate(oldRenderer);
                var renderer = wound.GetComponent<SkinnedMeshRenderer>();
                if (!renderer) renderer = wound.gameObject.AddComponent<SkinnedMeshRenderer>();
                renderer.sharedMesh = mesh;
                renderer.bones = torso.bones;
                renderer.rootBone = torso.rootBone;
                renderer.localBounds = torso.localBounds;
                renderer.quality = torso.quality;
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = true;
                renderer.updateWhenOffscreen = false;
                wound.gameObject.SetActive(false);
                var serialized = new SerializedObject(sever);
                serialized.FindProperty("torsoWoundVisual").objectReferenceValue = wound.gameObject;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (!PrefabUtility.SaveAsPrefabAsset(root, RuntimePath))
                    throw new InvalidOperationException("Could not save torso wound visual.");
                AssetDatabase.SaveAssets();
                Debug.Log("LS_WOUND_AUTHORING_COMPLETE: surface-fitted skinned patch, 99 vertices / 160 triangles, URP Lit.");
            }
            finally
            {
                if (fitted) UnityEngine.Object.DestroyImmediate(fitted);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Editor-only projection; runtime uses ordinary GPU skinning and the existing gore gate.
        static Mesh FitTorsoWound(SkinnedMeshRenderer torso, Vector3 center, Transform visual)
        {
            const int columns = 9, rows = 11;
            var baked = new Mesh();
            var probe = new GameObject("Wound fitting probe") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                torso.BakeMesh(baked);
                probe.transform.SetPositionAndRotation(torso.transform.position, torso.transform.rotation);
                probe.transform.localScale = torso.transform.lossyScale;
                var collider = probe.AddComponent<MeshCollider>(); collider.sharedMesh = baked;
                Physics.SyncTransforms();
                var triangles = baked.triangles;
                var sourceWeights = torso.sharedMesh.boneWeights;
                var bindposes = torso.sharedMesh.bindposes;
                var bones = torso.bones;
                if (sourceWeights.Length != baked.vertexCount) throw new InvalidOperationException("Missing torso skin weights.");
                var vertices = new Vector3[columns * rows];
                var normals = new Vector3[vertices.Length];
                var weights = new BoneWeight[vertices.Length];
                var uv = new Vector2[vertices.Length];
                var indices = new int[(columns - 1) * (rows - 1) * 6];
                int cursor = 0;
                for (int y = 0; y < rows; y++) for (int x = 0; x < columns; x++)
                {
                    int i = y * columns + x;
                    uv[i] = new Vector2(x / (float)(columns - 1), y / (float)(rows - 1));
                    var origin = center + visual.right * ((uv[i].x - .5f) * .23f) +
                        visual.up * ((uv[i].y - .5f) * .29f) + visual.forward * .55f;
                    if (!collider.Raycast(new Ray(origin, -visual.forward), out var hit, .8f) ||
                        Vector3.Dot(hit.normal, visual.forward) < .2f)
                        throw new InvalidOperationException("Wound fit missed front torso at grid " + x + "," + y);
                    int t = hit.triangleIndex * 3;
                    weights[i] = BlendWoundWeights(sourceWeights[triangles[t]], sourceWeights[triangles[t + 1]],
                        sourceWeights[triangles[t + 2]], hit.barycentricCoordinate);
                    var w = weights[i];
                    var skin = new Matrix4x4();
                    AccumulateSkin(ref skin, bones[w.boneIndex0].localToWorldMatrix * bindposes[w.boneIndex0], w.weight0);
                    AccumulateSkin(ref skin, bones[w.boneIndex1].localToWorldMatrix * bindposes[w.boneIndex1], w.weight1);
                    AccumulateSkin(ref skin, bones[w.boneIndex2].localToWorldMatrix * bindposes[w.boneIndex2], w.weight2);
                    AccumulateSkin(ref skin, bones[w.boneIndex3].localToWorldMatrix * bindposes[w.boneIndex3], w.weight3);
                    vertices[i] = skin.inverse.MultiplyPoint3x4(hit.point + hit.normal * .0018f);
                    normals[i] = skin.transpose.MultiplyVector(hit.normal).normalized;
                    if (x == columns - 1 || y == rows - 1) continue;
                    indices[cursor++] = i; indices[cursor++] = i + 1; indices[cursor++] = i + columns;
                    indices[cursor++] = i + columns; indices[cursor++] = i + 1; indices[cursor++] = i + columns + 1;
                }
                var mesh = new Mesh { name = "SM_Zombie_TorsoWound_Quad", vertices = vertices, normals = normals,
                    uv = uv, triangles = indices, boneWeights = weights, bindposes = bindposes };
                mesh.RecalculateTangents(); mesh.RecalculateBounds();
                return mesh;
            }
            finally { UnityEngine.Object.DestroyImmediate(probe); UnityEngine.Object.DestroyImmediate(baked); }
        }
        static void AccumulateSkin(ref Matrix4x4 sum, Matrix4x4 matrix, float weight)
        { for (int i = 0; i < 16; i++) sum[i] += matrix[i] * weight; }
        static BoneWeight BlendWoundWeights(BoneWeight a, BoneWeight b, BoneWeight c, Vector3 blend)
        {
            var sums = new Dictionary<int, float>();
            void Add(int bone, float weight) { if (weight > 0) sums[bone] = (sums.TryGetValue(bone, out var old) ? old : 0) + weight; }
            void AddVertex(BoneWeight w, float value)
            { Add(w.boneIndex0, w.weight0 * value); Add(w.boneIndex1, w.weight1 * value); Add(w.boneIndex2, w.weight2 * value); Add(w.boneIndex3, w.weight3 * value); }
            AddVertex(a, blend.x); AddVertex(b, blend.y); AddVertex(c, blend.z);
            var sorted = new List<KeyValuePair<int, float>>(sums);
            sorted.Sort((left, right) => right.Value.CompareTo(left.Value));
            var ids = new int[4]; var values = new float[4]; float total = 0;
            for (int i = 0; i < Mathf.Min(4, sorted.Count); i++) { ids[i] = sorted[i].Key; values[i] = sorted[i].Value; total += values[i]; }
            if (total <= 0) throw new InvalidOperationException("Invalid wound skin weights.");
            return new BoneWeight { boneIndex0 = ids[0], boneIndex1 = ids[1], boneIndex2 = ids[2], boneIndex3 = ids[3],
                weight0 = values[0] / total, weight1 = values[1] / total, weight2 = values[2] / total, weight3 = values[3] / total };
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
