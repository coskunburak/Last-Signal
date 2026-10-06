using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class MRPolyCatalogTests
    {
        const string Root = "Assets/LastSignal/Prefabs/Combat/MRPoly";

        [Test]
        public void EverySourceVariantHasAWorldPrefabWithValidMeshesAndUrpMaterials()
        {
            var source = Directory.GetFiles("Assets/ThirdParty/Weapons/MRPoly/Low Poly Weapons Set/Prefabs", "*.prefab", SearchOption.AllDirectories);
            Assert.That(source.Length, Is.EqualTo(16));
            foreach (var path in source)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/World/" + Path.GetFileName(path));
                Assert.That(prefab, Is.Not.Null, path);
                Assert.That(prefab.GetComponentsInChildren<Component>(true).All(c => c), Is.True, path);
                foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    Assert.That(filter.sharedMesh, Is.Not.Null, path);
                    var materials = filter.GetComponent<Renderer>().sharedMaterials;
                    Assert.That(materials.Length, Is.EqualTo(filter.sharedMesh.subMeshCount), path);
                    foreach (var material in materials)
                    {
                        Assert.That(material, Is.Not.Null, path);
                        Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"), path);
                    }
                }
            }
        }

        [Test]
        public void RifleVariantsRetainValidatedAdsOpticsAndAnimationWiring()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Resources/Weapon_AssaultRifle.prefab");
            var baseline = source.GetComponent<WeaponController>();
            var paths = Directory.GetFiles(Root + "/FirstPerson", "*.prefab");
            Assert.That(paths.Length, Is.EqualTo(7));
            foreach (var path in paths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var weapon = prefab.GetComponent<WeaponController>();
                Assert.That(weapon.Definition, Is.SameAs(baseline.Definition), path);
                Assert.That(weapon.AimReference.localPosition, Is.EqualTo(baseline.AimReference.localPosition), path);
                Assert.That(weapon.Muzzle, Is.Not.Null, path);
                Assert.That(prefab.GetComponent<ScopeOpticPresenter>(), Is.Not.Null, path);
                Assert.That(prefab.GetComponent<WeaponAnimationPresenter>(), Is.Not.Null, path);
                Assert.That(prefab.GetComponentsInChildren<Transform>(true).All(t => t.gameObject.layer == 29), Is.True, path);
                Assert.That(prefab.GetComponentsInChildren<Animator>(true).First().runtimeAnimatorController,
                    Is.SameAs(source.GetComponentsInChildren<Animator>(true).First().runtimeAnimatorController), path);
            }
        }
    }
}
