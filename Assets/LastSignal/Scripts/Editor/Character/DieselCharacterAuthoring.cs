using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSignal.Editor
{
    /// <summary>Explicit, repeatable authoring. Never runs at import/startup or executes verification.</summary>
    public static class DieselCharacterAuthoring
    {
        public const string Root = "Assets/LastSignal/Art/CharacterIntegration";
        public const string DieselPath = "Assets/ThirdParty/Diesel.fbx";
        public const string BodyPath = Root + "/Diesel_WorldBody.prefab";
        public const string PlayerPath = "Assets/LastSignal/Prefabs/Player/Player.prefab";
        public const string ControllerPath = "Assets/LastSignal/Animations/WorldBody.controller";
        public const string Library1 = "Assets/ThirdParty/Animation/AnimationLibraries/Universal Animation Library[Standard] 2/Unity/UAL1_Standard.fbx";
        public const string Library2 = "Assets/ThirdParty/Animation/AnimationLibraries/Universal Animation Library 2[Standard] 2/Unity/UAL2_Standard.fbx";
        public const string HumanRoot = "Assets/ThirdParty/Zombies/ZOMBİE ATTACK ANIMATION/Human Animations/Animations/Male/";

        [MenuItem("Last Signal/Character/Author Diesel integration")]
        public static void Author()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Author outside Play Mode.");
            Directory.CreateDirectory(Root + "/Clips"); Directory.CreateDirectory(Root + "/Materials");
            AssetDatabase.Refresh();
            ExtractLibrary(Library1, "UAL1", new[] { "Walk_Loop", "Sprint_Loop", "Jump_Start", "Jump_Loop", "Jump_Land", "Interact", "PickUp_Table", "Sitting_Idle_Loop", "Driving_Loop", "Pistol_Idle_Loop" });
            ExtractLibrary(Library2, "UAL2", new[] { "Consume" });
            CharacterControllerAuthoring.Extend();
            CreateWorldWeapons();
            CreateBody(); AttachToPlayer(); MatchFPSPalette();
            AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath));
            foreach(string guid in AssetDatabase.FindAssets("",new[]{Root}))
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
            Debug.Log("Diesel authoring saved. No tests/builds or visual acceptance executed.");
        }
        static void ExtractLibrary(string source, string prefix, string[] names)
        {
            if (names.All(n => AssetDatabase.LoadAssetAtPath<AnimationClip>(Root + "/Clips/" + n + ".anim"))) return;
            // Temporary project-owned import: vendor FBX and importer settings remain untouched.
            string staging = Root + "/" + prefix + "_RetargetSource.fbx";
            if (!File.Exists(staging) && !AssetDatabase.CopyAsset(source, staging)) throw new IOException(source);
            var importer = (ModelImporter)AssetImporter.GetAtPath(staging);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(staging);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.humanDescription = Description(model, false);
            importer.importCameras = false; importer.importLights = false;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.loopTime = clip.name.EndsWith("_Loop", StringComparison.Ordinal);
                clip.lockRootRotation = true; clip.lockRootHeightY = true; clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true; clip.keepOriginalPositionY = true; clip.keepOriginalPositionXZ = true;
            }
            importer.clipAnimations = clips; importer.SaveAndReimport();
            var imported = AssetDatabase.LoadAllAssetsAtPath(staging).OfType<AnimationClip>().ToArray();
            foreach (string name in names)
            {
                var clip = imported.FirstOrDefault(c => c.name == "Armature|" + name);
                if (!clip || !clip.isHumanMotion) throw new InvalidOperationException("Humanoid extraction unavailable: " + name);
                var copy = Object.Instantiate(clip); copy.name = name;
                AnimationUtility.SetAnimationEvents(copy, Array.Empty<AnimationEvent>());
                Save(copy, Root + "/Clips/" + name + ".anim");
            }
            // Standalone muscle curves do not depend on the temporary avatar or mesh.
            AssetDatabase.DeleteAsset(staging);
        }
        public static HumanDescription Description(GameObject root, bool mixamo)
        {
            var mapping = new Dictionary<string, string> {
                {"Hips", mixamo?"Hips":"pelvis"}, {"Spine",mixamo?"Spine":"spine_01"},
                {"Chest",mixamo?"Spine1":"spine_02"}, {"UpperChest",mixamo?"Spine2":"spine_03"},
                {"Neck",mixamo?"Neck":"neck_01"}, {"Head",mixamo?"Head":"Head"}
            };
            foreach (string side in new[]{"Left", "Right"})
            {
                string suffix = side == "Left" ? "_l" : "_r";
                string[] humans = { "Shoulder", "UpperArm", "LowerArm", "Hand", "UpperLeg", "LowerLeg", "Foot", "Toes" };
                string[] mix = { "Shoulder", "Arm", "ForeArm", "Hand", "UpLeg", "Leg", "Foot", "ToeBase" };
                string[] ual = { "clavicle", "upperarm", "lowerarm", "hand", "thigh", "calf", "foot", "ball" };
                for (int i=0; i<humans.Length; i++) mapping[side+humans[i]] = mixamo ? side+mix[i] : ual[i]+suffix;
                string[] fingers={"Thumb","Index","Middle","Ring","Little"}, segments={"Proximal","Intermediate","Distal"};
                for(int f=0;f<fingers.Length;f++) for(int j=0;j<3;j++)
                    mapping[side+fingers[f]+segments[j]] = mixamo ? side+"Hand"+(f==4?"Pinky":fingers[f])+(j+1) : (f==4?"pinky":fingers[f].ToLowerInvariant())+"_0"+(j+1)+suffix;
            }
            var transforms = root.GetComponentsInChildren<Transform>(true);
            return new HumanDescription {
                human = mapping.Select(p => new HumanBone { humanName=HumanTrait.BoneName[(int)Enum.Parse(typeof(HumanBodyBones),p.Key)], boneName=(mixamo?"mixamorig:":"")+p.Value, limit=new HumanLimit { useDefaultValues=true } }).ToArray(),
                skeleton = transforms.Select(t => new SkeletonBone { name=t.name, position=t.localPosition, rotation=t.localRotation, scale=t.localScale }).ToArray(),
                upperArmTwist=.5f, lowerArmTwist=.5f, upperLegTwist=.5f, lowerLegTwist=.5f, armStretch=.02f, legStretch=.02f, feetSpacing=0, hasTranslationDoF=false
            };
        }
        static void CreateBody()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(DieselPath);
            if (!source) throw new InvalidOperationException("Diesel source missing.");
            var body = Object.Instantiate(source); body.name = "WorldBody";
            try
            {
                var keep = new HashSet<string> { "Armature", "retopo_body.002", "jacket.003", "shirt.003", "pants.003", "boots.003", "hair.003", "eyes.003", "beard.003", "mid_eyebrow.002" };
                foreach (Transform child in body.transform.Cast<Transform>().ToArray()) if (!keep.Contains(child.name)) Object.DestroyImmediate(child.gameObject);
                foreach(var component in body.GetComponentsInChildren<Camera>(true)) Object.DestroyImmediate(component);
                var avatar = AvatarBuilder.BuildHumanAvatar(body, Description(body, true)); avatar.name = "Diesel_Humanoid";
                if (!avatar.isHuman || !avatar.isValid) { Object.DestroyImmediate(avatar); throw new InvalidOperationException("Diesel humanoid authoring could not produce a valid avatar."); }
                Save(avatar, Root + "/Diesel_Humanoid.asset");
                var animator = body.AddComponent<Animator>(); animator.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(Root + "/Diesel_Humanoid.asset");
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
                animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                // Meshes share the Armature skeleton. Bounds must use the root bone's 100x coordinate space.
                foreach(var skin in body.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    skin.updateWhenOffscreen = false; skin.quality = SkinQuality.Bone4;
                    float scale = Mathf.Max(.001f, skin.rootBone.lossyScale.x);
                    skin.localBounds = new Bounds(Vector3.up * (-.05f / scale), new Vector3(2.6f, 2.8f, 2.6f) / scale);
                    skin.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    skin.receiveShadows = true;
                    skin.sharedMaterials = new[]{MaterialFor(skin.name)};
                }
                foreach(var t in body.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30;
                body.AddComponent<PlayerLocomotionPresenter>(); body.AddComponent<CharacterBodyVisibility>();
                body.AddComponent<CharacterWeaponRig>().Configure(new[]{
                    AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(Root + "/RiflePose.asset"),
                    AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(Root + "/CrowbarPose.asset") });
                body.AddComponent<CharacterSeatPresentation>();
                PrefabUtility.SaveAsPrefabAsset(body, BodyPath);
            }
            finally { Object.DestroyImmediate(body); }
        }
        static Material MaterialFor(string part)
        {
            string key = part.StartsWith("retopo") ? "Skin" : part.StartsWith("jacket") ? "Jacket" : part.StartsWith("shirt") ? "Shirt" :
                part.StartsWith("pants") ? "Pants" : part.StartsWith("boots") ? "Boots" : part.StartsWith("eyes") ? "Eyes" : "Hair";
            string[] keys={"Skin","Jacket","Shirt","Pants","Boots","Eyes","Hair"};
            string[] colors={"#A07860","#40564A","#75604B","#4D5351","#393732","#7A8076","#302D29"};
            string path=Root+"/Materials/Diesel_"+key+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) { mat=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,path); }
            ColorUtility.TryParseHtmlString(colors[Array.IndexOf(keys,key)],out var color);
            mat.SetColor("_BaseColor",color); mat.SetFloat("_Smoothness",key=="Eyes"?.3f:.16f); mat.SetFloat("_Metallic",0);
            EditorUtility.SetDirty(mat); return mat;
        }
        static void AttachToPlayer()
        {
            var player=PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                var old=player.transform.Find("WorldBody"); if(old) Object.DestroyImmediate(old.gameObject);
                var body=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BodyPath),player.transform);
                body.name="WorldBody"; body.transform.localPosition=Vector3.zero; body.transform.localRotation=Quaternion.identity;
                var view=player.GetComponent<FirstPersonLook>().View;
                view.cullingMask |= 1 << 30; // Per-camera owner visibility preserves other players and local shadows.
                var inspection=player.GetComponent<CharacterInspectionCamera>(); if(!inspection)inspection=player.AddComponent<CharacterInspectionCamera>();
                inspection.Configure(view,body.GetComponent<CharacterBodyVisibility>());
                var actions=player.GetComponent<CharacterFirstPersonActions>(); if(!actions)actions=player.AddComponent<CharacterFirstPersonActions>();
                actions.Configure(player.transform.Find("View/WeaponParent"),body.GetComponent<PlayerLocomotionPresenter>());
                PrefabUtility.SaveAsPrefabAsset(player,PlayerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
        }
        static void CreateWorldWeapons()
        {
            const string rifleSource="Assets/LastSignal/Prefabs/Combat/MRPoly/World/Assault Rifle (Black).prefab";
            var rifle=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(rifleSource)); rifle.name="Diesel_WorldRifle";
            try
            {
                foreach(var collider in rifle.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
                foreach(var t in rifle.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
                var sockets=rifle.AddComponent<CharacterWeaponSockets>();
                sockets.RightGrip=Socket(rifle.transform,"RightHandGrip",new Vector3(.079f,.092f,-.14f),new Vector3(12,0,90));
                sockets.LeftGrip=Socket(rifle.transform,"LeftHandSupport",new Vector3(.032f,.105f,.10f),new Vector3(0,0,-90));
                sockets.StockContact=Socket(rifle.transform,"StockContact",new Vector3(.06f,.19f,-.374f),Vector3.zero);
                sockets.AimReference=Socket(rifle.transform,"ADSReference",new Vector3(.057f,.25f,.02f),Vector3.zero);
                sockets.Muzzle=Socket(rifle.transform,"Muzzle",new Vector3(.055f,.21f,.48f),Vector3.zero);
                sockets.ReloadReference=Socket(rifle.transform,"ReloadReference",new Vector3(.035f,.045f,-.008f),new Vector3(0,0,-90));
                sockets.Magazine=rifle.transform.Find("Magazine.001");
                PrefabUtility.SaveAsPrefabAsset(rifle,Root+"/Diesel_WorldRifle.prefab");
            }
            finally { Object.DestroyImmediate(rifle); }
            var profile=ScriptableObject.CreateInstance<CharacterWeaponPoseProfile>();
            profile.Firearm=AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/LastSignal/Data/Combat/WeaponDefinition_AssaultRifle.asset");
            profile.WorldPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Diesel_WorldRifle.prefab");
            Save(profile,Root+"/RiflePose.asset");
            CreateMelee();
        }
        static void CreateMelee()
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Combat/Crowbar/Crowbar_Viewmodel.prefab");
            var mesh=source.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(m=>m.name.IndexOf("crowbar",StringComparison.OrdinalIgnoreCase)>=0);
            if(!mesh)throw new InvalidOperationException("Crowbar render mesh missing.");
            var go=new GameObject("Diesel_WorldCrowbar");
            try
            {
                go.layer=30;
                var visual=new GameObject("CrowbarMesh");visual.layer=30;visual.transform.SetParent(go.transform,false);
                visual.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;
                visual.AddComponent<MeshRenderer>().sharedMaterials=mesh.GetComponent<MeshRenderer>().sharedMaterials;
                // Retain the existing GripAnchor-to-mesh offset and dimensional scale, with no gameplay components.
                visual.transform.localPosition=mesh.transform.localPosition;
                visual.transform.localRotation=mesh.transform.localRotation;
                visual.transform.localScale=mesh.transform.lossyScale;
                PrefabUtility.SaveAsPrefabAsset(go,Root+"/Diesel_WorldCrowbar.prefab");
            }
            finally{Object.DestroyImmediate(go);}
            var profile=ScriptableObject.CreateInstance<CharacterWeaponPoseProfile>();
            profile.Melee=source.GetComponent<MeleeWeaponController>().Definition;
            profile.WorldPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Diesel_WorldCrowbar.prefab");
            profile.HandEuler=new Vector3(0,0,90); Save(profile,Root+"/CrowbarPose.asset");
        }
        static void MatchFPSPalette()
        {
            // Retain close-up textures/normal maps, only tint project-owned material copies.
            foreach(string path in new[]{"Assets/LastSignal/Prefabs/Resources/Weapon_AssaultRifle.prefab","Assets/LastSignal/Prefabs/Combat/Crowbar/Crowbar_Viewmodel.prefab"})
            {
                var prefab=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach(var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        string key=renderer.name=="Gorka"?"Jacket":renderer.name=="Arms"?"Skin":null;
                        if(key==null)continue;
                        var mats=renderer.sharedMaterials;
                        for(int i=0;i<mats.Length;i++)
                        {
                            if(!mats[i])continue;
                            string owned=Root+"/Materials/FPS_"+key+"_"+i+".mat";
                            var copy=AssetDatabase.LoadAssetAtPath<Material>(owned);
                            if(!copy){copy=new Material(mats[i]); AssetDatabase.CreateAsset(copy,owned);}
                            var color=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Diesel_"+key+".mat").GetColor("_BaseColor");
                            copy.SetColor("_BaseColor",color); copy.SetFloat("_Smoothness",.16f); EditorUtility.SetDirty(copy); mats[i]=copy;
                        }
                        renderer.sharedMaterials=mats;
                    }
                    PrefabUtility.SaveAsPrefabAsset(prefab,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(prefab);}
            }
        }
        static Transform Socket(Transform parent,string name,Vector3 position,Vector3 angles)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;t.localEulerAngles=angles;t.gameObject.layer=30;return t;}
        static void Save(Object value,string path)
        {
            var existing=AssetDatabase.LoadMainAssetAtPath(path);
            if(existing){EditorUtility.CopySerialized(value,existing);EditorUtility.SetDirty(existing);Object.DestroyImmediate(value);}
            else AssetDatabase.CreateAsset(value,path);
        }
    }
}
