using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class ScopeOpticTests
    {
        const string Folder = "Assets/LastSignal/Models/Weapons/Optics/";

        [Test]
        public void FovPreservesSpecifiedAngularMagnification()
        {
            foreach (float distance in new[] { .15f, .223796f, .35f })
            foreach (float power in new[] { 1f, 3f, 6f })
            {
                const float halfHeight = .014638f;
                float fov = ScopeOpticDefinition.CalculateVerticalFov(halfHeight, distance, power);
                float actual = (halfHeight / distance) / Mathf.Tan(fov * Mathf.Deg2Rad / 2);
                Assert.That(actual, Is.EqualTo(power).Within(.0001f));
            }
        }

        [Test]
        public void RearLensIsSeparatedAndOpaqueBodyTrianglesRemainUnchanged()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ThirdParty/Weapons/MRPoly/Low Poly Weapons Set/Models/Assault Rifle.fbx")
                .GetComponent<MeshFilter>().sharedMesh;
            var body = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "MRPoly_Body_Optics.asset");
            var rear = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "MRPoly_RearLens.asset");
            Assert.That(body, Is.Not.Null, "Önce manuel optik kurulumu yapılmalıdır.");
            Assert.That(rear, Is.Not.Null);
            for (int s = 0; s < 3; s++) CollectionAssert.AreEqual(source.GetTriangles(s), body.GetTriangles(s));
            Assert.That(body.GetIndexCount(3), Is.EqualTo(24));
            Assert.That(rear.GetIndexCount(0), Is.EqualTo(24));
            Assert.That(rear.vertices.All(v => Mathf.Abs(v.z + .120204f) < .00001f), Is.True);
            Assert.That(rear.normals.All(n => n.z < -.99f), Is.True);
        }

        [Test]
        public void PrefabHasOneConfiguredScopeAndNoAuthoredExtraCamera()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Resources/Weapon_AssaultRifle.prefab");
            var optics = root.GetComponents<ScopeOpticPresenter>();
            Assert.That(optics.Length, Is.EqualTo(1));
            Assert.That(optics[0].Definition, Is.Not.Null);
            Assert.That(optics[0].Lens, Is.Not.Null);
            Assert.That(optics[0].Lens.transform.parent.name, Is.EqualTo("MRPoly_RifleBody"));
            Assert.That(root.GetComponentsInChildren<Camera>(true).All(c => !c.enabled), Is.True);
            Assert.That(root.GetComponentsInChildren<Transform>(true).All(t => t.gameObject.layer == 29), Is.True);
            Assert.That(LayerMask.LayerToName(29), Is.EqualTo("FirstPersonViewmodel"));
            Assert.That(ShaderUtil.ShaderHasError(optics[0].Lens.sharedMaterial.shader), Is.False);
        }
    }
}
