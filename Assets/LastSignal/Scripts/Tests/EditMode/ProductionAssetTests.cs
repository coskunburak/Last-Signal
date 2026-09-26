using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public class ProductionAssetTests
    {
        const string Rifle = "Assets/LastSignal/Assets/MR POLY/Low Poly Weapons Set/Models/Assault Rifle.fbx";
        const string Val = "Assets/LastSignal/Assets/VAL.fbx";
        [Test]
        public void ProductionPrefabUsesSourceMeshesAndOneAnimatedMagazine()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Weapon_AssaultRifle.prefab");
            var meshes = prefab.GetComponentsInChildren<MeshFilter>();
            Assert.That(meshes.Count(m => m.sharedMesh.name == "Magazine.001"), Is.EqualTo(1));
            foreach (var mesh in meshes) Assert.That(AssetDatabase.GetAssetPath(mesh.sharedMesh), Is.EqualTo(Rifle));
            var mag = meshes.Single(m => m.sharedMesh.name == "Magazine.001");
            Assert.That(mag.transform.parent.name, Is.EqualTo("mag"));
            Assert.That(meshes.Single(m => m.sharedMesh.name == "M4A1 Carbine").transform.parent.name, Is.EqualTo("wpn_body"));
            Assert.That(prefab.GetComponentsInChildren<SkinnedMeshRenderer>().All(r => AssetDatabase.GetAssetPath(r.sharedMesh) == Val), Is.True);
            Assert.That(prefab.GetComponentsInChildren<Renderer>().Any(r => r.name == "VAL_Model"), Is.False);
            Assert.That(prefab.GetComponentsInChildren<Camera>().All(c => !c.enabled), Is.True);
        }
        [Test]
        public void ProductionAnimatorReferencesEveryRequiredRealClip()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Weapon_AssaultRifle.prefab");
            var clips = prefab.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips;
            foreach (string action in new[] { "idle", "draw", "shoot", "reload", "reload_full", "hide" })
            {
                var clip = clips.Single(c => c.name == "LVA4_Armature|wpn_val_" + action);
                Assert.That(AssetDatabase.GetAssetPath(clip), Is.EqualTo(Val));
                Assert.That(AnimationUtility.GetCurveBindings(clip).Any(b => b.path.EndsWith("wpn_body/mag")), Is.True);
            }
            Assert.That(clips.Single(c => c.name.EndsWith("wpn_val_idle")).isLooping, Is.True);
        }
    }
}
