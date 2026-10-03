using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using LastSignal.Editor;

namespace LastSignal.Tests
{
    public sealed class ScopeAuthoringTests
    {
        [Test]
        public void ReapplyingOpticsPreservesSourceAimAndAssetIdentity()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Weapon_AssaultRifle.prefab");
            Assert.That(prefab.GetComponent<ScopeOpticPresenter>(), Is.Not.Null,
                "Önce Install on Existing Rifle Prefab çalıştırılmalıdır.");
            byte[] sourceBytes = File.ReadAllBytes(ScopeOpticAuthoring.SourcePath);
            string guid = AssetDatabase.AssetPathToGUID(ScopeOpticAuthoring.BodyPath);
            var root = Object.Instantiate(prefab);
            try
            {
                var weapon = root.GetComponent<WeaponController>();
                Vector3 aim = weapon.AimReference.localPosition;
                Quaternion rotation = weapon.AimReference.localRotation;
                var animator = root.GetComponentInChildren<Animator>(true).runtimeAnimatorController;
                ScopeOpticAuthoring.Apply(root);
                ScopeOpticAuthoring.Apply(root);
                Assert.That(root.GetComponents<ScopeOpticPresenter>().Length, Is.EqualTo(1));
                Assert.That(weapon.AimReference.localPosition, Is.EqualTo(aim));
                Assert.That(weapon.AimReference.localRotation, Is.EqualTo(rotation));
                Assert.That(root.GetComponentInChildren<Animator>(true).runtimeAnimatorController, Is.SameAs(animator));
                Assert.That(AssetDatabase.AssetPathToGUID(ScopeOpticAuthoring.BodyPath), Is.EqualTo(guid));
                CollectionAssert.AreEqual(sourceBytes, File.ReadAllBytes(ScopeOpticAuthoring.SourcePath));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
