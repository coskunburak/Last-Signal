using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LastSignal.Tests
{
    public class ZombieAssetTests
    {
        const string Root = "Assets/LastSignal/Assets/Zombie/Enemies/Zombie";
        static GameObject Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/LS_Zombie_Shirtless_Visual.prefab");

        [Test]
        public void ZombiePresentationHasValidRigAndNoGameplayOrMissingComponents()
        {
            Assert.That(Prefab, Is.Not.Null);
            var animator = Prefab.GetComponentInChildren<Animator>(true);
            Assert.That(animator.avatar.isValid && animator.avatar.isHuman, Is.True);
            Assert.That(animator.applyRootMotion, Is.False);
            Assert.That(Prefab.GetComponentsInChildren<Component>(true).All(c => c && (c is Transform || c is Animator || c is SkinnedMeshRenderer)), Is.True);
            Assert.That(Prefab.transform.localScale, Is.EqualTo(Vector3.one));
            var skin = Prefab.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.That(AssetDatabase.GetAssetPath(skin.sharedMesh), Does.Contain("/NewPunch/ShirtlessZombieFree/"));
            Assert.That(Prefab.GetComponentsInChildren<Transform>(true).Any(t => t.name == "HeadReference"), Is.True);
        }

        [Test]
        public void RequiredStatesUseRealHumanoidClipsAndCorrectLoopsWithoutGameplayTransitions()
        {
            var controller = (AnimatorController)Prefab.GetComponentInChildren<Animator>().runtimeAnimatorController;
            var machine = controller.layers[0].stateMachine;
            foreach (var name in new[] { "Idle", "Locomotion", "Run", "Attack", "AttackAlternate",
                "HitReact", "Death", "FlyingBackDeath" })
            {
                var state = machine.states.Single(s => s.state.name == name).state;
                var clip = state.motion as AnimationClip;
                Assert.That(clip, Is.Not.Null); Assert.That(clip.humanMotion, Is.True);
                Assert.That(clip.length, Is.GreaterThan(.4f));
                Assert.That(clip.isLooping, Is.EqualTo(name == "Idle" || name == "Locomotion" || name == "Run"));
                Assert.That(state.transitions, Is.Empty); Assert.That(clip.events, Is.Empty);
            }
            Assert.That(machine.defaultState.name, Is.EqualTo("Idle"));
            Assert.That(controller.parameters.Select(p => p.name), Is.EquivalentTo(new[] { "HitReactSpeed" }));
            Assert.That(controller.parameters.Single().type, Is.EqualTo(AnimatorControllerParameterType.Float));
            Assert.That(controller.parameters.Single().defaultFloat, Is.EqualTo(1));
            Assert.That(machine.anyStateTransitions, Is.Empty);
            Assert.That(controller.layers.Length, Is.EqualTo(2));
            Assert.That(controller.layers[1].avatarMask, Is.Not.Null);
            var definition = AssetDatabase.LoadAssetAtPath<ZombieDefinition>(Root + "/Shambler.asset");
            var hitState = controller.layers[1].stateMachine.states.Single(s => s.state.name == "HitReact").state;
            Assert.That(hitState.motion, Is.EqualTo(definition.HitReactClip));
            Assert.That(hitState.speedParameterActive, Is.True);
            Assert.That(hitState.speedParameter, Is.EqualTo("HitReactSpeed"));
            Assert.That(AssetDatabase.GetAssetPath(definition.HitReactClip),
                Is.EqualTo("Assets/LastSignal/Assets/Kevin Iglesias/Zombie Animations/Animations/Zombie@Damage01.fbx"));
            Assert.That(AssetDatabase.GetAssetPath(definition.FlyingBackDeathClip),
                Is.EqualTo("Assets/LastSignal/Assets/Animation/Zombie Animation/Flying Back Death.fbx"));
        }

        [Test]
        public void MaterialUsesLinearPackedMapsAndCorrectNormalImporter()
        {
            var material = Prefab.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial;
            Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
            Assert.That(material.shader.isSupported, Is.True);
            foreach (var key in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap", "_OcclusionMap" }) Assert.That(material.GetTexture(key), Is.Not.Null);
            var normal = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetTexture("_BumpMap")));
            Assert.That(normal.textureType, Is.EqualTo(TextureImporterType.NormalMap));
            var mask = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetTexture("_MetallicGlossMap")));
            Assert.That(mask.sRGBTexture, Is.False);
        }

        [Test]
        public void AnimationSourceAvatarsBelongToTheirOwnSkeleton()
        {
            const string source = "Assets/LastSignal/Assets/Kevin Iglesias/Zombie Animations";
            foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { source + "/Animations" }))
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(importer.animationType, Is.EqualTo(ModelImporterAnimationType.Human));
                Assert.That(importer.sourceAvatar.isValid && importer.sourceAvatar.isHuman, Is.True);
                Assert.That(AssetDatabase.GetAssetPath(importer.sourceAvatar), Is.EqualTo(source + "/Model/ZombieModel.fbx"));
            }
        }
    }
}
