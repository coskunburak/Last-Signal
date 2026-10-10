using System.Linq;
using LastSignal.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class CharacterIntegrationAssetTests
    {
        [Test] public void ProductionPlayerHasExactlyOneDieselAndOneMovementAuthority()
        {
            var player=AssetDatabase.LoadAssetAtPath<GameObject>(DieselCharacterAuthoring.PlayerPath);
            Assert.That(player,Is.Not.Null);
            Assert.That(player.GetComponentsInChildren<FirstPersonMotor>(true),Has.Length.EqualTo(1));
            Assert.That(player.GetComponentsInChildren<CharacterController>(true),Has.Length.EqualTo(1));
            Assert.That(player.GetComponentsInChildren<PlayerLocomotionPresenter>(true),Has.Length.EqualTo(1));
            var body=player.transform.Find("WorldBody"); Assert.That(body,Is.Not.Null);
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(body.gameObject),Is.EqualTo(DieselCharacterAuthoring.BodyPath));
            Assert.That(body.GetComponent<Animator>().applyRootMotion,Is.False);
            Assert.That(player.GetComponent<CharacterInspectionCamera>(),Is.Not.Null);
            Assert.That(player.GetComponent<CharacterFirstPersonActions>(),Is.Not.Null);
            Assert.That(player.GetComponent<FirstPersonLook>().View.cullingMask&(1<<30),Is.Not.Zero,"World layer must participate in local shadows.");
        }
        [Test] public void DieselHasValidHumanoidAndCompleteArmsHandsAndLegs()
        {
            var root=PrefabUtility.LoadPrefabContents(DieselCharacterAuthoring.BodyPath);
            try
            {
                var animator=root.GetComponent<Animator>();
                Assert.That(animator.avatar,Is.Not.Null);Assert.That(animator.avatar.isHuman,Is.True);Assert.That(animator.avatar.isValid,Is.True);
                foreach(var bone in new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.UpperChest,
                    HumanBodyBones.Neck,HumanBodyBones.Head,HumanBodyBones.LeftShoulder,HumanBodyBones.RightShoulder,
                    HumanBodyBones.LeftUpperArm,HumanBodyBones.RightUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.RightLowerArm,
                    HumanBodyBones.LeftHand,HumanBodyBones.RightHand,HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg,
                    HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot,
                    HumanBodyBones.LeftThumbDistal,HumanBodyBones.RightThumbDistal,HumanBodyBones.LeftIndexDistal,HumanBodyBones.RightLittleDistal})
                    Assert.That(animator.GetBoneTransform(bone),Is.Not.Null,bone.ToString());
                var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                Assert.That(skins,Has.Length.EqualTo(9));
                Assert.That(root.GetComponentsInChildren<Camera>(true),Is.Empty);
                Assert.That(root.GetComponentsInChildren<Collider>(true),Is.Empty);
                foreach(var skin in skins)
                {
                    Assert.That(skin.sharedMesh,Is.Not.Null);Assert.That(skin.rootBone,Is.Not.Null);
                    Assert.That(skin.bones.All(b=>b&&b.IsChildOf(root.transform)),Is.True,skin.name);
                    Assert.That(skin.sharedMaterials.All(m=>m&&m.shader.name=="Universal Render Pipeline/Lit"),Is.True,skin.name);
                }
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        [Test] public void ReusedControllerHasReachableLocomotionActionsAndSingleIkPass()
        {
            var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(DieselCharacterAuthoring.ControllerPath);
            Assert.That(AssetDatabase.AssetPathToGUID(DieselCharacterAuthoring.ControllerPath),Is.EqualTo("a35d1340d221840209f9f1dc49574e2a"));
            Assert.That(c.parameters.Select(p=>p.name).Distinct().Count(),Is.EqualTo(c.parameters.Length));
            foreach(string p in new[]{"MoveX","MoveY","HorizontalSpeed","PlaybackRate","IsCrouching","IsSliding","Grounded","Sprinting","VerticalVelocity","MovementDirection","Dead","Seated","MeleeTime","ReloadTime"})
                Assert.That(c.parameters.Any(x=>x.name==p),Is.True,p);
            Assert.That(c.layers.Count(l=>l.iKPass),Is.EqualTo(1));
            var baseLayer=c.layers[0];Assert.That(baseLayer.iKPass,Is.True);
            foreach(string state in new[]{"Standing Idle","Standing Move","Crouch Locomotion","Slide","Character Jump","Character Fall","Character Land","Character Death","Character Seated"})
                Assert.That(baseLayer.stateMachine.states.Any(s=>s.state.name==state&&s.state.motion),Is.True,state);
            var actions=c.layers.Single(l=>l.name=="Character Actions");
            Assert.That(actions.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg),Is.False);
            Assert.That(actions.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Root),Is.False);
            Assert.That(actions.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm),Is.True);
            foreach(var clip in c.animationClips)Assert.That(clip.isHumanMotion,Is.True,AssetDatabase.GetAssetPath(clip));
            Assert.That(actions.stateMachine.states.Single(s=>s.state.name=="MeleeActive").state.timeParameter,Is.EqualTo("MeleeTime"));
        }
        [Test] public void WorldWeaponProfilesHaveNoGameplayOrViewmodelComponents()
        {
            var body=AssetDatabase.LoadAssetAtPath<GameObject>(DieselCharacterAuthoring.BodyPath);
            var profiles=body.GetComponent<CharacterWeaponRig>().Profiles;Assert.That(profiles,Has.Length.EqualTo(2));
            foreach(var profile in profiles)
            {
                Assert.That(profile.WorldPrefab,Is.Not.Null);Assert.That(profile.Firearm||profile.Melee,Is.True);
                Assert.That(profile.WorldPrefab.GetComponentsInChildren<WeaponController>(true),Is.Empty);
                Assert.That(profile.WorldPrefab.GetComponentsInChildren<MeleeWeaponController>(true),Is.Empty);
                Assert.That(profile.WorldPrefab.GetComponentsInChildren<Collider>(true),Is.Empty);
                if(profile.Firearm)Assert.That(profile.WorldPrefab.GetComponent<CharacterWeaponSockets>().HasFirearmGrips,Is.True);
            }
        }
        [Test] public void RifleHipAndAimGripsAreWithinBothDieselArmsReach()
        {
            var root=PrefabUtility.LoadPrefabContents(DieselCharacterAuthoring.BodyPath);
            GameObject weapon=null;
            try
            {
                var animator=root.GetComponent<Animator>();
                var profile=root.GetComponent<CharacterWeaponRig>().Profiles.Single(p=>p.Firearm);
                weapon=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(profile.WorldPrefab));
                weapon.transform.localScale*=profile.WorldScale;
                var sockets=weapon.GetComponent<CharacterWeaponSockets>();
                var chest=animator.GetBoneTransform(HumanBodyBones.Chest);
                var chestRotation=chest.rotation;
                var rightPalm=new CharacterHandPose(animator,false);
                var leftPalm=new CharacterHandPose(animator,true);
                Assert.That(rightPalm.Valid&&leftPalm.Valid,Is.True,"Both palms require actual finger landmark calibration.");
                foreach(bool aiming in new[]{false,true})
                {
                    chest.rotation=Quaternion.AngleAxis(profile.TorsoYaw,root.transform.up)*chestRotation;
                    var shoulder=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                    Vector3 stock=shoulder.position+root.transform.rotation*(aiming?profile.AimStockOffset:profile.HipStockOffset);
                    weapon.transform.rotation=root.transform.rotation*Quaternion.Euler(aiming?profile.AimEuler:profile.HipEuler);
                    weapon.transform.position+=stock-sockets.StockContact.position;
                    Assert.That(Vector3.Distance(stock,sockets.StockContact.position),Is.LessThan(.001f));
                    Assert.That(Vector3.Dot(sockets.Muzzle.position-stock,root.transform.forward),Is.GreaterThan(.4f));
                    foreach(bool left in new[]{false,true})
                    {
                    var upper=animator.GetBoneTransform(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
                    var lower=animator.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
                    var hand=animator.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
                    var grip=left?sockets.LeftGrip:sockets.RightGrip;
                    (left?leftPalm:rightPalm).WristPose(grip.position,grip.rotation,out var target,out _);
                    float reach=(Vector3.Distance(upper.position,lower.position)+Vector3.Distance(lower.position,hand.position))*profile.MaximumReach;
                    Assert.That(Vector3.Distance(upper.position,target),Is.LessThanOrEqualTo(reach),
                        (aiming?"ADS":"Hip")+" "+(left?"support":"trigger")+" grip is outside the authored Diesel arm reach.");
                    }
                }
            }
            finally{if(weapon)PrefabUtility.UnloadPrefabContents(weapon);PrefabUtility.UnloadPrefabContents(root);}
        }
        [Test] public void RifleContactsMatchStockPistolGripAndVerticalForegripGeometry()
        {
            var profile=AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(DieselCharacterAuthoring.Root+"/RiflePose.asset");
            var sockets=profile.WorldPrefab.GetComponent<CharacterWeaponSockets>();
            Assert.That(sockets.StockContact.localPosition.z,Is.LessThan(-.35f));
            Assert.That(sockets.RightGrip.localPosition.z,Is.InRange(-.18f,-.10f),"Pistol grip is behind the magazine, not on the front grip.");
            Assert.That(sockets.LeftGrip.localPosition.z,Is.InRange(.07f,.13f),"Support palm belongs on the vertical foregrip, not the barrel.");
            Assert.That(Vector3.Dot(sockets.RightGrip.up,Vector3.left),Is.GreaterThan(.95f));
            Assert.That(Vector3.Dot(sockets.LeftGrip.up,Vector3.right),Is.GreaterThan(.95f));
        }
        [Test] public void StandingLocomotionUsesCoherentDirectionsAndNoIdleActionOverride()
        {
            var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(DieselCharacterAuthoring.ControllerPath);
            var move=c.layers[0].stateMachine.states.Single(s=>s.state.name=="Standing Move").state;
            var tree=move.motion as BlendTree;
            Assert.That(tree,Is.Not.Null);Assert.That(tree.blendParameter,Is.EqualTo("MoveX"));
            Assert.That(tree.blendParameterY,Is.EqualTo("MoveY"));
            foreach(var child in tree.children)
            {
                Assert.That(child.motion,Is.TypeOf<AnimationClip>());
                Assert.That(child.timeScale,Is.GreaterThan(0),"Backward movement needs its directional clip, not reversed playback.");
            }
            Assert.That(AssetDatabase.GetAssetPath(tree.children.Single(x=>x.position==Vector2.up).motion),Does.EndWith("HumanM@Run01_Forward.fbx"));
            Assert.That(AssetDatabase.GetAssetPath(tree.children.Single(x=>x.position==Vector2.down).motion),Does.EndWith("HumanM@Run01_Backward.fbx"));
            Assert.That(c.layers.Single(l=>l.name=="Character Actions").defaultWeight,Is.Zero);
        }
    }
}
