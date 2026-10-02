using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastSignal.Editor
{
    public static class ZombieBloodVfxAuthoring
    {
        public const string Output = "Assets/LastSignal/Blood VFX/ProjectOwned";
        const string Vendor = "Assets/LastSignal/Blood VFX/Vefects/Free Blood VFX/VFX/";
        const string Runtime = "Assets/Resources/LS_Zombie_Runtime.prefab";
        [MenuItem("Last Signal/Zombie/Author Blood VFX")]
        public static void Create()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            if (AssetDatabase.LoadAssetAtPath<ZombieBloodVfxProfile>(Output + "/ZombieBlood.asset"))
                throw new InvalidOperationException("Blood assets already exist. Edit existing assets; do not overwrite tuning.");
            System.IO.Directory.CreateDirectory(Output);
            AssetDatabase.Refresh();
            var profile = ScriptableObject.CreateInstance<ZombieBloodVfxProfile>();
            profile.burstPrefab = Particle("PS_LS_Blood_SeverBurst", false, false);
            profile.spurtPrefab = Particle("PS_LS_Blood_ArterialSpurt", true, false);
            profile.dripPrefab = Particle("PS_LS_Blood_Drip", true, true);
            profile.surfacePrefab = Surface();
            profile.pressure = new AnimationCurve(new Keyframe(0, 1), new Keyframe(.15f, .9f), new Keyframe(.5f, .48f), new Keyframe(.8f, .16f), new Keyframe(1, 0));
            AssetDatabase.CreateAsset(profile, Output + "/ZombieBlood.asset");
            var root = PrefabUtility.LoadPrefabContents(Runtime);
            try
            {
                var animator = root.GetComponentInChildren<Animator>(true);
                var presenter = root.GetComponent<ZombieBloodVfxPresenter>();
                if (!presenter) presenter = root.AddComponent<ZombieBloodVfxPresenter>();
                presenter.Configure(profile, new[] {
                    Anchor(animator, ZombieBodyPart.Head, HumanBodyBones.Neck, HumanBodyBones.Head, HumanBodyBones.Head, "NeckStumpVfxAnchor"),
                    Anchor(animator, ZombieBodyPart.LeftArm, HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, "LeftShoulderStumpVfxAnchor"),
                    Anchor(animator, ZombieBodyPart.RightArm, HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, "RightShoulderStumpVfxAnchor"),
                    Anchor(animator, ZombieBodyPart.LeftHand, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, HumanBodyBones.LeftHand, "LeftWristStumpVfxAnchor"),
                    Anchor(animator, ZombieBodyPart.RightHand, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, HumanBodyBones.RightHand, "RightWristStumpVfxAnchor") });
                PrefabUtility.SaveAsPrefabAsset(root, Runtime);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("LS_BLOOD_AUTHORING_COMPLETE");
        }
        static ZombieBloodVfxPresenter.AnchorBinding Anchor(Animator animator, ZombieBodyPart part,
            HumanBodyBones parentBone, HumanBodyBones jointBone, HumanBodyBones endBone, string name)
        {
            var parent = animator.GetBoneTransform(parentBone);
            var joint = animator.GetBoneTransform(jointBone);
            var end = animator.GetBoneTransform(endBone);
            if (!parent || !joint || !end) throw new InvalidOperationException("Missing blood bone " + part);
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            var direction = end == joint ? joint.position - parent.position : end.position - joint.position;
            t.SetPositionAndRotation(joint.position, Quaternion.LookRotation(direction.normalized));
            return new ZombieBloodVfxPresenter.AnchorBinding { part = part, anchor = t };
        }
        static GameObject Particle(string name, bool continuous, bool drip)
        {
            string path = Vendor + "Particles/Once/" + (continuous && !drip ? "VFX_Splat_Directional_01_Floor_Once.prefab" : "VFX_Splat_01_Floor_Once.prefab");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!source) throw new InvalidOperationException("Vendor source missing: " + path);
            var go = UnityEngine.Object.Instantiate(source);
            go.name = name;
            try
            {
                var systems = go.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var p in systems)
                {
                    p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = p.main;
                    bool visible = p.emission.enabled;
                    main.playOnAwake = false; main.loop = continuous && visible; main.duration = continuous ? 10 : .8f;
                    main.stopAction = ParticleSystemStopAction.None;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                    main.maxParticles = continuous ? (drip ? 24 : 48) : 32;
                    main.startLifetime = drip ? .8f : .65f;
                    main.startSpeed = drip ? .15f : continuous ? 1.6f : 2.8f;
                    main.startSize3D = false; main.startSize = new ParticleSystem.MinMaxCurve(drip ? .018f : .035f, drip ? .035f : .095f);
                    main.gravityModifier = drip ? 1 : .65f;
                    main.startRotation3D = false; main.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f);
                    var emission = p.emission;
                    emission.rateOverTime = continuous && visible ? 1 : 0;
                    emission.rateOverDistance = 0;
                    emission.SetBursts(visible && !continuous ? new[] { new ParticleSystem.Burst(0, 18) } : Array.Empty<ParticleSystem.Burst>());
                    var shape = p.shape; shape.enabled = visible; shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = drip ? 4 : continuous ? 18 : 65; shape.radius = drip ? .007f : .025f;
                    var collision = p.collision; collision.enabled = false;
                    var sub = p.subEmitters; sub.enabled = false;
                    var renderer = p.GetComponent<ParticleSystemRenderer>();
                    renderer.renderMode = ParticleSystemRenderMode.Billboard;
                    renderer.alignment = ParticleSystemRenderSpace.View;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    if (renderer.sharedMaterial)
                    {
                        var material = new Material(renderer.sharedMaterial) { name = "M_" + name, enableInstancing = true };
                        material.SetFloat("_DepthFade", .025f); material.SetFloat("_EmissionMultiply", 0); material.SetFloat("_Smoothness", .4f);
                        material.SetFloat("_LUTDesaturate", .2f); material.SetFloat("_LUTValueIntensity", .65f);
                        AssetDatabase.CreateAsset(material, Output + "/M_" + name + "_" + p.name + ".mat");
                        renderer.sharedMaterial = material;
                    }
                    p.transform.localRotation = Quaternion.identity;
                    p.transform.localPosition = Vector3.zero;
                    p.transform.localScale = Vector3.one;
                }
                return PrefabUtility.SaveAsPrefabAsset(go, Output + "/" + name + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        static GameObject Surface()
        {
            var vendor = AssetDatabase.LoadAssetAtPath<Material>(Vendor + "Materials/M_VFX_Fake_Blood_01_Lit.mat");
            var material = new Material(Shader.Find("LastSignal/BloodSurface")) { name = "M_LS_BloodSurface", enableInstancing = true };
            material.SetTexture("_BaseMap", vendor.GetTexture("_ErosionTexture"));
            material.SetColor("_BaseColor", new Color(.23f, .027f, .023f, .8f));
            AssetDatabase.CreateAsset(material, Output + "/M_LS_BloodSurface.mat");
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad); go.name = "LS_BloodSurface"; go.layer = 2;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var result = PrefabUtility.SaveAsPrefabAsset(go, Output + "/LS_BloodSurface.prefab");
            UnityEngine.Object.DestroyImmediate(go);
            return result;
        }
        [MenuItem("Last Signal/Zombie/Validate Blood VFX")]
        public static void Validate()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(Runtime);
            var presenter = root.GetComponent<ZombieBloodVfxPresenter>();
            if (!presenter || !presenter.Profile || !presenter.Profile.IsValid) throw new InvalidOperationException("Invalid blood profile/presenter.");
            var bindings = new SerializedObject(presenter).FindProperty("anchors");
            int mask = 0;
            for (int i = 0; i < bindings.arraySize; i++)
            {
                var entry = bindings.GetArrayElementAtIndex(i);
                int part = entry.FindPropertyRelative("part").enumValueIndex;
                if ((mask & (1 << part)) != 0 || !entry.FindPropertyRelative("anchor").objectReferenceValue)
                    throw new InvalidOperationException("Duplicate or missing blood anchor");
                mask |= 1 << part;
            }
            foreach (var part in new[] { ZombieBodyPart.Head, ZombieBodyPart.LeftArm, ZombieBodyPart.RightArm, ZombieBodyPart.LeftHand, ZombieBodyPart.RightHand })
                if (!presenter.AnchorFor(part)) throw new InvalidOperationException("Missing blood anchor " + part);
            foreach (var prefab in new[] { presenter.Profile.burstPrefab, presenter.Profile.spurtPrefab, presenter.Profile.dripPrefab, presenter.Profile.surfacePrefab })
                foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                    if (renderer.GetComponent<ParticleSystem>() && !renderer.GetComponent<ParticleSystem>().emission.enabled) continue;
                    else if (!renderer.sharedMaterial || !renderer.sharedMaterial.shader || !renderer.sharedMaterial.shader.isSupported)
                        throw new InvalidOperationException("Missing/unsupported blood material " + prefab.name);
            Debug.Log("LS_BLOOD_VALIDATION_PASS — manual appearance pending");
        }
    }
}
