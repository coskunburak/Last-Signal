using NUnit.Framework;
using UnityEngine;
using LastSignal.Art.Editor;

namespace LastSignal.Art.Tests
{
    public class S013PrefabValidationTests
    {
        GameObject root;
        Material material;
        [TearDown] public void Cleanup()
        {
            if (root) Object.DestroyImmediate(root);
            if (material) Object.DestroyImmediate(material);
        }
        [Test] public void InvalidSourceReportsIndependentFailures()
        {
            root = new GameObject("InvalidSource");
            root.transform.localScale = new Vector3(-1, 1, 1);
            root.AddComponent<MeshFilter>();
            root.AddComponent<MeshRenderer>().sharedMaterials = new Material[] { null };
            var failures = S013PrefabValidation.Inspect(root, true, false);
            CollectionAssert.Contains(failures, "ROOT_SCALE");
            CollectionAssert.Contains(failures, "NONPOSITIVE_SCALE:InvalidSource");
            CollectionAssert.Contains(failures, "MISSING_MESH:InvalidSource");
            CollectionAssert.Contains(failures, "MISSING_MATERIAL:InvalidSource");
            CollectionAssert.Contains(failures, "REQUIRED_COLLIDER");
            CollectionAssert.Contains(failures, "LICENSE_NOT_APPROVED");
        }
        [Test] public void DecorativeAssetDoesNotNeedColliderButBlockingAssetDoes()
        {
            root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(root.GetComponent<Collider>());
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            root.GetComponent<Renderer>().sharedMaterial = material;
            Assert.That(S013PrefabValidation.Inspect(root, false, true), Is.Empty);
            CollectionAssert.Contains(S013PrefabValidation.Inspect(root, true, true), "REQUIRED_COLLIDER");
        }
        [Test] public void CompatibleGeometryDoesNotImplyLicenseApproval()
        {
            root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            root.GetComponent<Renderer>().sharedMaterial = material;
            CollectionAssert.AreEqual(new[] { "LICENSE_NOT_APPROVED" }, S013PrefabValidation.Inspect(root, true, false));
        }
    }
}
