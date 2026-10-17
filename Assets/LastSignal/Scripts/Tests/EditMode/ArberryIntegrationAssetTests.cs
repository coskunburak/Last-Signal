using System;
using System.IO;
using System.Linq;
using LastSignal.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class ArberryIntegrationAssetTests
    {
        [Test] public void OrmConversionPreservesMetalAndOcclusionAndInvertsRoughness()
        {
            var p=ArberryCharacterAuthoring.PackOrm(new Color32(180,230,12,255));
            Assert.That(p.r,Is.EqualTo(12));Assert.That(p.g,Is.EqualTo(180));Assert.That(p.a,Is.EqualTo(25));
            Assert.That(ArberryCharacterAuthoring.PackOrm(new Color32(255,0,0,0)).a,Is.EqualTo(255));
            Assert.That(ArberryCharacterAuthoring.PackOrm(new Color32(255,255,0,0)).a,Is.Zero);
        }
        [Test] public void SourceImporterUsesOwnHumanoidAndPreservesRuntimeBones()
        {
            Assert.That(File.Exists(ArberryCharacterAuthoring.SourcePath),Is.True);
            var mi=AssetImporter.GetAtPath(ArberryCharacterAuthoring.SourcePath) as ModelImporter;
            Assert.That(mi,Is.Not.Null);Assert.That(mi.animationType,Is.EqualTo(ModelImporterAnimationType.Human));
            Assert.That(mi.avatarSetup,Is.EqualTo(ModelImporterAvatarSetup.CreateFromThisModel));Assert.That(mi.optimizeGameObjects,Is.False);
            Assert.That(mi.GetExternalObjectMap(),Has.Count.EqualTo(12),"Existing user material remaps must remain intact.");
            var avatar=AssetDatabase.LoadAllAssetsAtPath(ArberryCharacterAuthoring.SourcePath).OfType<Avatar>().Single();
            Assert.That(avatar.isValid&&avatar.isHuman,Is.True);
        }
        [Test] public void ArberryAssemblyHasCompleteBonesSkinsAndNoGameplayAuthority()
        {
            var root=PrefabUtility.LoadPrefabContents(ArberryCharacterAuthoring.BodyPath);
            try {
                var a=root.GetComponent<Animator>();Assert.That(a.avatar.isHuman&&a.avatar.isValid,Is.True);
                Assert.That(AssetDatabase.GetAssetPath(a.avatar),Is.EqualTo(ArberryCharacterAuthoring.SourcePath));
                Assert.That(root.transform.localScale,Is.EqualTo(Vector3.one));Assert.That(a.applyRootMotion,Is.False);
                Assert.That(AssetDatabase.GetAssetPath(a.runtimeAnimatorController),Is.EqualTo(DieselCharacterAuthoring.ControllerPath));
                foreach(var bone in new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.UpperChest,HumanBodyBones.Neck,HumanBodyBones.Head,HumanBodyBones.LeftUpperArm,HumanBodyBones.RightUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.RightLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightHand,HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot,HumanBodyBones.LeftToes,HumanBodyBones.RightToes})Assert.That(a.GetBoneTransform(bone),Is.Not.Null,bone.ToString());
                var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);Assert.That(skins,Has.Length.EqualTo(11));
                Assert.That(skins.Select(r=>r.name).Distinct().Count(),Is.EqualTo(11));
                foreach(var r in skins){Assert.That(r.rootBone,Is.Not.Null);Assert.That(r.bones,Has.Length.EqualTo(160));Assert.That(r.bones.All(b=>b&&b.IsChildOf(root.transform)),Is.True,r.name);Assert.That(r.sharedMaterials,Has.Length.EqualTo(r.sharedMesh.subMeshCount));Assert.That(r.sharedMaterials.All(m=>m&&m.GetTexture("_BaseMap")),Is.True,r.name);}
                Assert.That(root.GetComponentsInChildren<FirstPersonMotor>(true),Is.Empty);Assert.That(root.GetComponentsInChildren<WeaponController>(true),Is.Empty);
                Assert.That(new CharacterHandPose(a,false).Valid&&new CharacterHandPose(a,true).Valid,Is.True);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        [Test] public void MaterialsUseBoundedTypedTexturesAndCorrectUrpPacking()
        {
            string[] names={"Arms","Backpack2","Body2","Body_Arkit_Eye","Brows_Leashes","Gloves1","Hair3","Head","Jacket1","Jeans1","Mouth","Shoes1"};
            foreach(string name in names){
                var m=AssetDatabase.LoadAssetAtPath<Material>(ArberryCharacterAuthoring.Root+"/Materials/"+name+".mat");Assert.That(m,Is.Not.Null,name);Assert.That(m.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));
                Check(m,"_BaseMap",true,TextureImporterType.Default);Check(m,"_BumpMap",false,TextureImporterType.NormalMap);
                Assert.That(m.IsKeywordEnabled("_NORMALMAP"),Is.True);
                if(name=="Hair3"||name=="Brows_Leashes"){Assert.That(m.GetFloat("_AlphaClip"),Is.EqualTo(1));Assert.That(m.IsKeywordEnabled("_ALPHATEST_ON"),Is.True);}
                else if(name!="Body_Arkit_Eye") {Check(m,"_MetallicGlossMap",false,TextureImporterType.Default);Assert.That(m.GetTexture("_OcclusionMap"),Is.SameAs(m.GetTexture("_MetallicGlossMap")));Assert.That(m.GetFloat("_SmoothnessTextureChannel"),Is.Zero);}
            }
        }
        static void Check(Material m,string property,bool srgb,TextureImporterType type)
        {
            var texture=m.GetTexture(property);Assert.That(texture,Is.Not.Null,m.name+property);
            var path=AssetDatabase.GetAssetPath(texture);Assert.That(path,Does.StartWith(ArberryCharacterAuthoring.Root+"/Textures/"));
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);Assert.That(importer.sRGBTexture,Is.EqualTo(srgb),path);Assert.That(importer.textureType,Is.EqualTo(type),path);Assert.That(importer.isReadable,Is.False);Assert.That(importer.mipmapEnabled,Is.True);Assert.That(importer.maxTextureSize,Is.LessThanOrEqualTo(2048));
        }
        [Test] public void CharacterOverridesPreserveEveryWeaponIdentityAndVariantGeometry()
        {
            var rig=AssetDatabase.LoadAssetAtPath<GameObject>(ArberryCharacterAuthoring.BodyPath).GetComponent<CharacterWeaponRig>();
            var diesel=AssetDatabase.LoadAssetAtPath<GameObject>(DieselCharacterAuthoring.BodyPath).GetComponent<CharacterWeaponRig>();
            Assert.That(rig.Profiles,Has.Length.EqualTo(5));
            foreach(var source in diesel.Profiles.Concat(new[]{"DXT","Glock17","Custom9mm"}.Select(n=>AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>("Assets/LastSignal/Art/TriplePistols/"+n+"/Pose.asset")))){
                var p=rig.ResolvePresentation(source);Assert.That(p,Is.Not.SameAs(source));Assert.That(p.Firearm,Is.SameAs(source.Firearm));Assert.That(p.Melee,Is.SameAs(source.Melee));Assert.That(p.ValidGripData,Is.True,p.name);
                var sockets=p.WorldPrefab.GetComponent<CharacterWeaponSockets>();Assert.That(sockets,Is.Not.Null,p.name);Assert.That(sockets.ValidFor(p),Is.True,p.name);
                Assert.That(p.WorldPrefab.GetComponentsInChildren<WeaponController>(true),Is.Empty);Assert.That(p.WorldPrefab.GetComponentsInChildren<MeleeWeaponController>(true),Is.Empty);
                if(p.Pistol){
                    // Arberry's authored TPS support contact lives in a dedicated visual prefab.
                    // Preserve the weapon variant's geometry/materials, not the old prefab identity.
                    Assert.That(p.WorldTwoHandedPistol,Is.True,p.name);
                    Assert.That(p.WorldPrefab,Is.Not.SameAs(source.WorldPrefab),p.name);
                    Assert.That(AssetDatabase.GetAssetPath(p.WorldPrefab),Does.StartWith(ArberryCharacterAuthoring.Root+"/Prefabs/TPS_"));
                    Assert.That(sockets.HasWorldPistolSupport,Is.True,p.name);
                    Assert.That(p.WorldPrefab.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh),
                        Is.EquivalentTo(source.WorldPrefab.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh)),p.name+" static variant geometry");
                    Assert.That(p.WorldPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r=>r.sharedMesh),
                        Is.EquivalentTo(source.WorldPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r=>r.sharedMesh)),p.name+" skinned variant geometry");
                    Assert.That(p.WorldPrefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials),
                        Is.EquivalentTo(source.WorldPrefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials)),p.name+" variant materials");
                    Assert.That(p.WorldPistolFingers.Valid,Is.True);Assert.That(p.WorldPistolFingers.Rig,Does.StartWith("Arberry"));Assert.That(p.WorldPistolFingers,Is.Not.SameAs(source.WorldPistolFingers));
                }
            }
            Assert.That(rig.ResolvePresentation(null),Is.Null);
            Assert.That(diesel.GetComponent<Animator>().avatar.isValid,Is.True,"Independent Diesel fallback must remain intact.");
        }
        [Test] public void ProductionReferencesAndComparisonFixtureExist()
        {
            var player=AssetDatabase.LoadAssetAtPath<GameObject>(DieselCharacterAuthoring.PlayerPath);
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(player.transform.Find("WorldBody").gameObject),Is.EqualTo(ArberryCharacterAuthoring.BodyPath));
            Assert.That(player.GetComponentsInChildren<CharacterViewmodelPalette>(true),Is.Empty,"Actual Arberry FPS meshes share source materials; no palette substitute.");
            Assert.That(player.GetComponent<CharacterController>().height,Is.EqualTo(1.8f).Within(.01f));
            Assert.That(File.Exists("Assets/LastSignal/Scenes/Validation/ArberryMaterialComparison.unity"),Is.True);
            var fixture=AssetDatabase.LoadAssetAtPath<GameObject>(ArberryCharacterAuthoring.Root+"/Prefabs/Arberry_MaterialComparison.prefab");
            Assert.That(fixture.transform.Find("Baseline").gameObject.activeSelf,Is.False);Assert.That(fixture.transform.Find("Candidate").gameObject.activeSelf,Is.True);
            foreach(string name in new[]{"NeutralDaylight","Overcast","Shelter","NightFlashlight"})Assert.That(fixture.transform.Find(name),Is.Not.Null);
        }
    }
}
