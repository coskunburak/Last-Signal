using System;
using System.Reflection;
using LastSignal.Editor.Rendering;
using NUnit.Framework;

namespace LastSignal.Tests
{
    public sealed class HdrpUrpComponentCleanupTests
    {
        // Text-only fixtures exercise preservation without opening or modifying an asset.
        const string Legacy = "a79441f348de89743a2939f4d699eac1";
        const string Native = "23c1ce4fb46143f46bc5cb5224c934f6";
        const string Unrelated = "--- !u!114 &4\nMonoBehaviour:\n  m_GameObject: {fileID: 1}\n  PistolGrip: {fileID: 2}\n";
        const string Camera = "--- !u!20 &2\nCamera:\n  m_GameObject: {fileID: 1}\n  field of view: 60\n  m_HDR: 1\n  m_AllowMSAA: 1\n";
        static string Fixture(bool saved) => "%YAML 1.1\n--- !u!1 &1\nGameObject:\n  m_Component:\n  - component: {fileID: 2}\n  - component: {fileID: 4}\n  - component: {fileID: " + (saved ? "5" : "3") + "}\n  m_Layer: 0\n" +
            Camera + Unrelated + "--- !u!114 &" + (saved ? "5" : "3") + "\nMonoBehaviour:\n  m_GameObject: {fileID: 1}\n  m_Script: {fileID: 11500000, guid: " + (saved ? Native : Legacy) + ", type: 3}\n";

        static string Merge(string before, string saved, int cameras = 1, int lights = 0, int newLights = 0)
        {
            var type = typeof(HdrpUrpComponentCleanup);
            var itemType = type.GetNestedType("Item", BindingFlags.NonPublic);
            var item = Activator.CreateInstance(itemType, true);
            itemType.GetField("path").SetValue(item, "fixture.prefab");
            itemType.GetField("cameras").SetValue(item, cameras);
            itemType.GetField("lights").SetValue(item, lights);
            itemType.GetField("newLights").SetValue(item, newLights);
            var entry = Activator.CreateInstance(type.GetNestedType("Entry", BindingFlags.NonPublic), true);
            try
            {
                return (string)type.GetMethod("PreserveUnrelatedContent", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new[] { before, saved, item, entry });
            }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PreservesUnrelatedRecordsAndCameraLensWhileReplacingPipelineData(bool crlf)
        {
            string before = Fixture(false);
            string saved = Fixture(true).Replace(Unrelated, Unrelated + "  PistolSupport: {fileID: 0}\n")
                .Replace("field of view: 60", "field of view: 65");
            string expected = Fixture(true).Replace("m_HDR: 1", "m_HDR: 0").Replace("m_AllowMSAA: 1", "m_AllowMSAA: 0");
            if (crlf) { before = before.Replace("\n", "\r\n"); saved = saved.Replace("\n", "\r\n"); expected = expected.Replace("\n", "\r\n"); }
            Assert.That(Merge(before, saved), Is.EqualTo(expected));
        }

        [Test]
        public void RejectsUnrelatedObjectDeletion()
        {
            Assert.Throws<InvalidOperationException>(() => Merge(Fixture(false), Fixture(true).Replace(Unrelated, "")));
        }

        [Test]
        public void RejectsUnrelatedComponentDetachment()
        {
            Assert.Throws<InvalidOperationException>(() => Merge(Fixture(false), Fixture(true).Replace("  - component: {fileID: 4}\n", "")));
        }

        [Test]
        public void RejectsNewCameraOnWrongOwner()
        {
            string saved = Fixture(true).Replace("--- !u!114 &5\nMonoBehaviour:\n  m_GameObject: {fileID: 1}",
                "--- !u!114 &5\nMonoBehaviour:\n  m_GameObject: {fileID: 999}");
            Assert.Throws<InvalidOperationException>(() => Merge(Fixture(false), saved));
        }

        [Test]
        public void RejectsUnexpectedAddedObject()
        {
            Assert.Throws<InvalidOperationException>(() => Merge(Fixture(false), Fixture(true) + "--- !u!4 &6\nTransform:\n  m_GameObject: {fileID: 1}\n"));
        }

        const string LegacyLight = "474bcb49853aa07438625e644c072ee6";
        const string NativeLight = "7a68c43fe1f2a47cfa234b5eeaa98012";
        const string Light = "--- !u!108 &2\nLight:\n  m_GameObject: {fileID: 1}\n  m_Intensity: 3\n  m_Range: 8\n";
        const string Physics = "--- !u!54 &4\nRigidbody:\n  m_GameObject: {fileID: 1}\n  m_Mass: 40\n  m_IsKinematic: 1\n";
        static string LightFixture(bool saved) => Fixture(saved).Replace(Legacy, LegacyLight).Replace(Native, NativeLight)
            .Replace(Camera, Light).Replace(Unrelated, Physics);

        [Test]
        public void NewLightDataPreservesLightSettingsAndPhysicsRecords()
        {
            var saved = LightFixture(true).Replace("m_Intensity: 3", "m_Intensity: 1000")
                .Replace("m_IsKinematic: 1", "m_IsKinematic: 0");
            Assert.That(Merge(LightFixture(false), saved, 0, 1, 1), Is.EqualTo(LightFixture(true)));
        }

        [Test]
        public void RejectsUnplannedLightAddition()
        {
            Assert.Throws<InvalidOperationException>(() => Merge(LightFixture(false), LightFixture(true), 0, 1, 0));
        }

        [Test]
        public void RejectsLightOnWrongOwner()
        {
            var saved = LightFixture(true).Replace("--- !u!114 &5\nMonoBehaviour:\n  m_GameObject: {fileID: 1}",
                "--- !u!114 &5\nMonoBehaviour:\n  m_GameObject: {fileID: 999}");
            Assert.Throws<InvalidOperationException>(() => Merge(LightFixture(false), saved, 0, 1, 1));
        }

        static string ReuseRemovedId(string saved) => saved.Replace("&5\n", "&3\n").Replace("{fileID: 5}", "{fileID: 3}");

        [Test]
        public void ReusedLightIdPreservesLightAndPhysics()
        {
            string expected = ReuseRemovedId(LightFixture(true));
            string saved = expected.Replace("m_Intensity: 3", "m_Intensity: 1000").Replace("m_IsKinematic: 1", "m_IsKinematic: 0");
            Assert.That(Merge(LightFixture(false), saved, 0, 1, 1), Is.EqualTo(expected));
        }

        [Test]
        public void ReusedCameraIdPreservesLensAndUnrelatedData()
        {
            string saved = ReuseRemovedId(Fixture(true)).Replace(Unrelated, Unrelated + "  PistolSupport: {fileID: 0}\n");
            string expected = ReuseRemovedId(Fixture(true)).Replace("m_HDR: 1", "m_HDR: 0").Replace("m_AllowMSAA: 1", "m_AllowMSAA: 0");
            Assert.That(Merge(Fixture(false), saved), Is.EqualTo(expected));
        }

        [Test]
        public void ReusedIdRejectsDifferentOwner()
        {
            string saved = ReuseRemovedId(LightFixture(true)).Replace("--- !u!114 &3\nMonoBehaviour:\n  m_GameObject: {fileID: 1}",
                "--- !u!114 &3\nMonoBehaviour:\n  m_GameObject: {fileID: 999}");
            Assert.Throws<InvalidOperationException>(() => Merge(LightFixture(false), saved, 0, 1, 1));
        }

        [Test]
        public void ReusedIdRejectsDifferentComponentKind()
        {
            string saved = ReuseRemovedId(LightFixture(true)).Replace(NativeLight, Native);
            Assert.Throws<InvalidOperationException>(() => Merge(LightFixture(false), saved, 0, 1, 1));
        }
    }
}
