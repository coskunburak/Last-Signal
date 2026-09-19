using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LastSignal.Tests
{
    public class ZombieAssetTests
    {
        const string Root = "Assets/LastSignal/Enemies/Zombie";
        static GameObject Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/LS_Zombie_Shambler.prefab");

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
            Assert.That(AssetDatabase.GetAssetPath(skin.sharedMesh), Does.Contain("/SZombie/"));
            Assert.That(Prefab.GetComponentsInChildren<Transform>(true).Any(t => t.name == "HeadReference"), Is.True);
        }

        [Test]
        public void RequiredStatesUseRealHumanoidClipsAndCorrectLoopsWithoutGameplayTransitions()
        {
            var controller = (AnimatorController)Prefab.GetComponentInChildren<Animator>().runtimeAnimatorController;
            var machine = controller.layers[0].stateMachine;
            foreach (var name in new[] { "Idle", "Locomotion", "Attack", "HitReact", "Death" })
            {
                var state = machine.states.Single(s => s.state.name == name).state;
                var clip = state.motion as AnimationClip;
                Assert.That(clip, Is.Not.Null); Assert.That(clip.humanMotion, Is.True);
                Assert.That(clip.length, Is.GreaterThan(.4f));
                Assert.That(clip.isLooping, Is.EqualTo(name == "Idle" || name == "Locomotion"));
                Assert.That(state.transitions, Is.Empty); Assert.That(clip.events, Is.Empty);
            }
            Assert.That(machine.defaultState.name, Is.EqualTo("Idle"));
            Assert.That(controller.parameters, Is.Empty);
            Assert.That(machine.anyStateTransitions, Is.Empty);
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
