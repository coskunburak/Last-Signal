using System.Linq;
using LastSignal.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastSignal.Tests
{
    public sealed class ArberryFirstPersonAssetTests
    {
        [TestCase("Rifle")][TestCase("DXT")][TestCase("Glock17")][TestCase("Custom9mm")]
        [TestCase("Crowbar")][TestCase("Machete")][TestCase("WoodenAxe")]
        public void EveryWeaponHasSourceDerivedArberrySkinAndOneContactOwner(string key)
        {
            var root=PrefabUtility.LoadPrefabContents(ArberryFirstPersonAuthoring.WeaponPath(key));
            try{
                var grips=root.GetComponents<FirstPersonWeaponGrip>();Assert.That(grips,Has.Length.EqualTo(1));var grip=grips[0];
                Assert.That(grip.ValidAuthoring,Is.True,key);Assert.That(grip.AnatomicalRoot,Is.Not.Null);
                Assert.That(grip.Profile.ValidGripData,Is.True,key);
                var worldRig=new SerializedObject(AssetDatabase.LoadAssetAtPath<GameObject>(ArberryCharacterAuthoring.BodyPath).GetComponent<CharacterWeaponRig>());
                Assert.That(grip.RightPalmDepth,Is.InRange(.03f,.04f),"Use the measured glove surface, not the legacy 12 mm palm.");
                Assert.That(grip.LeftPalmDepth,Is.InRange(.03f,.04f));
                Assert.That(grip.RightPalmDepth,Is.EqualTo(worldRig.FindProperty("rightPalmDepth").floatValue).Within(.000001f));
                Assert.That(grip.LeftPalmDepth,Is.EqualTo(worldRig.FindProperty("leftPalmDepth").floatValue).Within(.000001f));
                Assert.That(grip.Profile.FpsWeaponOwnsContacts,Is.True);Assert.That(grip.RightHand.name,Is.EqualTo("hand_r"));Assert.That(grip.LeftHand.name,Is.EqualTo("hand_l"));
                Assert.That(grip.AnatomicalRoot.IsChildOf(grip.Visual),Is.False);Assert.That(grip.Visual.IsChildOf(grip.RightHand),Is.False);
                Assert.That(root.GetComponentsInChildren<Animator>(true).All(a=>!a.enabled&&a.runtimeAnimatorController==null),Is.True);
                var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);Assert.That(skins,Has.Length.EqualTo(3));
                CollectionAssert.AreEquivalent(new[]{"Body3","Gloves","Jacket"},skins.Select(s=>s.name));
                foreach(var skin in skins){
                    Assert.That(skin.enabled,Is.True);Assert.That(skin.sharedMesh.subMeshCount,Is.EqualTo(1));Assert.That(skin.sharedMesh.vertexCount,Is.GreaterThan(0));
                    Assert.That(AssetDatabase.GetAssetPath(skin.sharedMesh),Does.StartWith(ArberryCharacterAuthoring.Root+"/Meshes/FPS_"));
                    Assert.That(skin.bones.All(b=>b&&b.IsChildOf(grip.AnatomicalRoot)),Is.True);Assert.That(skin.sharedMesh.bindposes.Length,Is.EqualTo(skin.bones.Length));
                    string mat=skin.name=="Body3"?"Arms":skin.name=="Gloves"?"Gloves1":"Jacket1";
                    Assert.That(skin.sharedMaterial,Is.SameAs(AssetDatabase.LoadAssetAtPath<Material>(ArberryCharacterAuthoring.Root+"/Materials/"+mat+".mat")));
                    Assert.That(skin.shadowCastingMode,Is.EqualTo(ShadowCastingMode.Off));Assert.That(skin.gameObject.layer,Is.EqualTo(29));
                }
                if(key=="Rifle"){
                    Assert.That(grip.Profile.TwoHandedFirearm,Is.True);Assert.That(grip.Sockets.HasFirearmGrips,Is.True);Assert.That(grip.Sockets.Magazine,Is.Not.Null);
                    Assert.That(grip.Sockets.ReloadReference.IsChildOf(grip.Sockets.Magazine),Is.True);
                    var weapon=root.GetComponent<WeaponController>();Assert.That(weapon.AimReference,Is.SameAs(grip.Sockets.AimReference));
                    Assert.That(root.GetComponent<ScopeOpticPresenter>().Lens.transform.IsChildOf(grip.Visual),Is.True);
                }
                if(grip.Profile.Pistol){
                    // FPS primary offsets require dedicated DXT/Custom9mm calibration. World
                    // contacts retain their original poses; runtime mesh contact has its own test.
                    var worldPose=AssetDatabase.LoadAssetAtPath<PistolFingerPose>(ArberryCharacterAuthoring.Root+"/Profiles/"+key+"_Fingers.asset");
                    bool dedicated=key=="DXT" || key=="Custom9mm";
                    var fpsPose=dedicated?AssetDatabase.LoadAssetAtPath<PistolFingerPose>(ArberryCharacterAuthoring.Root+"/Profiles/"+key+"_FpsFingers.asset"):worldPose;
                    Assert.That(worldPose,Is.Not.Null,key);Assert.That(fpsPose,Is.Not.Null,key);
                    Assert.That(worldPose.Valid && fpsPose.Valid,Is.True,key);
                    Assert.That(grip.Profile.WorldPistolFingers,Is.SameAs(worldPose),"Preserve the world calibration.");
                    Assert.That(grip.Profile.FpsPistolFingers,Is.SameAs(fpsPose),"Bind the reviewed FPS calibration.");
                    if(dedicated){
                        Assert.That(fpsPose,Is.Not.SameAs(worldPose));
                        for(int i=0;i<6;i++){
                            Assert.That(fpsPose.GripRelative[i],Is.EqualTo(worldPose.GripRelative[i]),"Preserve thumb/index grip.");
                            Assert.That(fpsPose.TriggerRelative[i],Is.EqualTo(worldPose.TriggerRelative[i]),"Preserve thumb/index trigger.");
                        }
                        for(int i=6;i<15;i++)
                            Assert.That(fpsPose.GripRelative[i],Is.EqualTo(fpsPose.TriggerRelative[i]),"Trigger pull must retain the calibrated wrap.");
                    }
                    foreach(var variant in root.GetComponent<PistolVariantPresenter>().Variants){var mapped=grip.ResolveProfile(variant.Pose);Assert.That(mapped.FpsPistolFingers.Rig,Does.StartWith("Arberry"));Assert.That(mapped.Firearm,Is.SameAs(variant.Pose.Firearm));}
                }
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        [Test] public void PlayerUsesArberryViewmodelsWithOriginalGameplayDefinitionsAndFallbackExists()
        {
            var player=AssetDatabase.LoadAssetAtPath<GameObject>(DieselCharacterAuthoring.PlayerPath);var so=new SerializedObject(player.GetComponent<PlayerCombatController>());
            for(int i=0;i<ArberryFirstPersonAuthoring.Fields.Length;i++){
                var reference=(Component)so.FindProperty(ArberryFirstPersonAuthoring.Fields[i]).objectReferenceValue;
                Assert.That(AssetDatabase.GetAssetPath(reference),Is.EqualTo(ArberryFirstPersonAuthoring.WeaponPath(ArberryFirstPersonAuthoring.Keys[i])));
                var old=AssetDatabase.LoadAssetAtPath<GameObject>(ArberryFirstPersonAuthoring.Originals[i]);Assert.That(old,Is.Not.Null);
                if(i<2)Assert.That(reference.GetComponent<WeaponController>().Definition,Is.SameAs(old.GetComponent<WeaponController>().Definition));
                else Assert.That(reference.GetComponent<MeleeWeaponController>().Definition,Is.SameAs(old.GetComponent<MeleeWeaponController>().Definition));
            }
        }
    }
}
