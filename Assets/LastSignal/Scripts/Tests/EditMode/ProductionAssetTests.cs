using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public class ProductionAssetTests
    {
        const string Rifle = "Assets/ThirdParty/Weapons/MRPoly/Low Poly Weapons Set/Models/Assault Rifle.fbx";
        const string Val = "Assets/ThirdParty/Characters/VAL.fbx";
        [Test]
        public void ProductionPrefabUsesSourceMeshesAndOneAnimatedMagazine()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Resources/Weapon_AssaultRifle.prefab");
            var meshes = prefab.GetComponentsInChildren<MeshFilter>();
            Assert.That(meshes.Count(m => m.sharedMesh.name == "Magazine.001"), Is.EqualTo(1));
            foreach (var mesh in meshes)
            {
                string expected = mesh.name == "MRPoly_RifleBody"
                    ? "Assets/LastSignal/Models/Weapons/Optics/MRPoly_Body_Optics.asset"
                    : mesh.name == "ScopeLens"
                        ? "Assets/LastSignal/Models/Weapons/Optics/MRPoly_RearLens.asset" : Rifle;
                Assert.That(AssetDatabase.GetAssetPath(mesh.sharedMesh), Is.EqualTo(expected));
            }
            var mag = meshes.Single(m => m.sharedMesh.name == "Magazine.001");
            Assert.That(mag.transform.parent.name, Is.EqualTo("mag"));
            Assert.That(meshes.Single(m => m.name == "MRPoly_RifleBody").transform.parent.name, Is.EqualTo("wpn_body"));
            Assert.That(prefab.GetComponentsInChildren<SkinnedMeshRenderer>().All(r => AssetDatabase.GetAssetPath(r.sharedMesh) == Val), Is.True);
            Assert.That(prefab.GetComponentsInChildren<Renderer>().Any(r => r.name == "VAL_Model"), Is.False);
            Assert.That(prefab.GetComponentsInChildren<Camera>().All(c => !c.enabled), Is.True);
        }
        [Test]
        public void ProductionAnimatorReferencesEveryRequiredRealClip()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Resources/Weapon_AssaultRifle.prefab");
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
