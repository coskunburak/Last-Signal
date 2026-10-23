using LastSignal.Editor.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class HdrpMaterialCoverageTests
    {
        Material material;

        [SetUp]
        public void SetUp()
        {
            var shader = Shader.Find("HDRP/Lit");
            Assert.That(shader, Is.Not.Null, "HDRP/Lit must resolve before auditing materials.");
            material = new Material(shader);
            material.SetFloat("_NormalMapSpace", 0);
            material.SetTexture("_NormalMap", null);
            material.SetTexture("_NormalMapOS", null);
            material.SetTexture("_DetailMap", null);
            material.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
            material.DisableKeyword("_NORMALMAP");
        }

        [TearDown]
        public void TearDown() { if (material) Object.DestroyImmediate(material); }

        [Test]
        public void TangentSpaceWithoutTextureIsValid()
        {
            Assert.That(HdrpActiveMaterialCoverageUtility.ValidateNormalMapKeywords(material), Is.Empty);
        }

        [Test]
        public void DetailOnlyTangentNormalsAreValid()
        {
            material.SetTexture("_DetailMap", Texture2D.grayTexture);
            material.EnableKeyword("_NORMALMAP");
            Assert.That(HdrpActiveMaterialCoverageUtility.ValidateNormalMapKeywords(material), Is.Empty);
        }

        [Test]
        public void ObjectSpaceNormalDoesNotRequireTangentTexture()
        {
            material.SetFloat("_NormalMapSpace", 1);
            material.DisableKeyword("_NORMALMAP_TANGENT_SPACE");
            material.SetTexture("_NormalMapOS", Texture2D.grayTexture);
            material.EnableKeyword("_NORMALMAP");
            Assert.That(HdrpActiveMaterialCoverageUtility.ValidateNormalMapKeywords(material), Is.Empty);
        }

        [Test]
        public void EnabledNormalWithoutAnySourceIsReported()
        {
            material.EnableKeyword("_NORMALMAP");
            Assert.That(HdrpActiveMaterialCoverageUtility.ValidateNormalMapKeywords(material),
                Does.Contain("Enabled _NORMALMAP without normal/detail texture"));
        }

        [Test]
        public void AssignedNormalWithDisabledSamplingIsReported()
        {
            material.SetTexture("_NormalMap", Texture2D.grayTexture);
            Assert.That(HdrpActiveMaterialCoverageUtility.ValidateNormalMapKeywords(material),
                Does.Contain("Assigned normal/detail map with disabled _NORMALMAP"));
        }

        [Test]
        public void WrongSpaceKeywordIsReported()
        {
            material.DisableKeyword("_NORMALMAP_TANGENT_SPACE");
            Assert.That(HdrpActiveMaterialCoverageUtility.ValidateNormalMapKeywords(material),
                Does.Contain("Normal map space keyword disagrees with _NormalMapSpace"));
        }
    }
}
