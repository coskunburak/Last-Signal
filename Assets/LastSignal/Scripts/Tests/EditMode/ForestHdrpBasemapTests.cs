using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastSignal.Tests
{
    public sealed class ForestHdrpBasemapTests
    {
        Material material;
        Texture2D control, diffuse, mask;

        static Texture2D Solid(Color value)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            texture.SetPixel(0, 0, value); texture.Apply(); return texture;
        }

        [SetUp]
        public void SetUp()
        {
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null), "GPU validation requires a graphics device.");
            var shader = Shader.Find("Hidden/LastSignal/Forest/Terrain Basemap Gen");
            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.isSupported, Is.True);
            material = new Material(shader);
            control = Solid(new Color(0, 0, 1, 0));
            diffuse = Solid(new Color(.2f, .4f, .6f, .5f));
            mask = Solid(new Color(.6f, .7f, .8f, .9f));
            material.SetTexture("_Control", control);
            material.SetFloat("_NumLayersCount", 4);
            for (int i = 0; i < 4; i++)
            {
                material.SetTexture("_Splat" + i, diffuse);
                material.SetTexture("_Mask" + i, mask);
                material.SetVector("_Splat" + i + "_ST", new Vector4(1, 1, 0, 0));
                material.SetVector("_DiffuseRemapScale" + i, Vector4.one);
                material.SetVector("_MaskMapRemapScale" + i, Vector4.one);
                material.SetVector("_MaskMapRemapOffset" + i, Vector4.zero);
                material.SetFloat("_Smoothness" + i, .4f);
                material.SetFloat("_SmoothnessSource" + i, 0);
                material.SetFloat("_LayerHasMask" + i, 0);
            }
        }

        Color DrawPass(int pass)
        {
            var target = RenderTexture.GetTemporary(8, 8, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var previous = RenderTexture.active;
            bool previousSrgb = GL.sRGBWrite;
            Texture2D result = null;
            GL.PushMatrix();
            try
            {
                RenderTexture.active = target; GL.sRGBWrite = false;
                GL.Clear(false, true, Color.clear); GL.LoadOrtho();
                Assert.That(material.SetPass(pass), Is.True, "Basemap pass failed to bind/compile.");
                GL.Begin(GL.QUADS);
                GL.TexCoord2(0, 0); GL.Vertex3(0, 0, 0);
                GL.TexCoord2(1, 0); GL.Vertex3(1, 0, 0);
                GL.TexCoord2(1, 1); GL.Vertex3(1, 1, 0);
                GL.TexCoord2(0, 1); GL.Vertex3(0, 1, 0);
                GL.End();
                result = new Texture2D(8, 8, TextureFormat.RGBAFloat, false, true);
                result.ReadPixels(new Rect(0, 0, 8, 8), 0, 0); result.Apply();
                return result.GetPixel(4, 4);
            }
            finally
            {
                GL.PopMatrix(); GL.sRGBWrite = previousSrgb; RenderTexture.active = previous;
                if (result) Object.DestroyImmediate(result);
                RenderTexture.ReleaseTemporary(target);
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in new Object[] { material, control, diffuse, mask })
                if (item) Object.DestroyImmediate(item);
        }

        [Test]
        public void BaseMapKeepsRgbAlbedoAndAlphaSmoothnessContract()
        {
            var value = DrawPass(0);
            Assert.That(value.r, Is.EqualTo(.2f).Within(.005f));
            Assert.That(value.g, Is.EqualTo(.4f).Within(.005f));
            Assert.That(value.b, Is.EqualTo(.6f).Within(.005f));
            Assert.That(value.a, Is.EqualTo(.2f).Within(.005f));
        }

        [Test]
        public void ThirdLayerUsesItsOwnMetallicMaskRemap()
        {
            material.EnableKeyword("_MASKMAP"); material.SetFloat("_LayerHasMask2", 1);
            material.SetVector("_MaskMapRemapScale2", new Vector4(.5f, 1, 1, 1));
            material.SetVector("_MaskMapRemapOffset2", new Vector4(.1f, 0, 0, 0));
            material.SetVector("_MaskMapRemapScale3", new Vector4(.1f, 1, 1, 1));
            Assert.That(DrawPass(1).r, Is.EqualTo(.4f).Within(.005f));
        }

        [Test]
        public void ZeroHeightTransitionProducesFiniteHeightBlendedOutput()
        {
            material.EnableKeyword("_MASKMAP"); material.EnableKeyword("_TERRAIN_BLEND_HEIGHT");
            material.SetFloat("_HeightTransition", 0);
            control.SetPixel(0, 0, new Color(.25f, .25f, .25f, .25f)); control.Apply();
            var value = DrawPass(0);
            Assert.That(value.r, Is.EqualTo(.2f).Within(.005f));
            Assert.That(value.a, Is.EqualTo(.2f).Within(.005f));
        }
    }
}
