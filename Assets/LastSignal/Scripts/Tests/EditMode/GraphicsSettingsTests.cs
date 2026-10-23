using LastSignal.RenderingQuality;
using NUnit.Framework;

namespace LastSignal.Tests
{
    public sealed class GraphicsSettingsTests
    {
        [Test]
        public void ProfilesRoundTripAndCopiesDoNotModifySource()
        {
            var profiles = GraphicsQualityProfiles.Load();
            foreach (var preset in new[] { GraphicsPreset.Low, GraphicsPreset.Medium, GraphicsPreset.High, GraphicsPreset.Ultra })
            {
                var state = profiles.Get(preset);
                var decoded = GraphicsSettingsPersistence.Decode(GraphicsSettingsPersistence.Encode(state), profiles);
                Assert.That(decoded.preset, Is.EqualTo(preset)); Assert.That(decoded.SameValues(state), Is.True);
                state.shadowDistance = 10; Assert.That(profiles.Get(preset).shadowDistance, Is.GreaterThan(10));
            }
            var low = profiles.Get(GraphicsPreset.Low); var high = profiles.Get(GraphicsPreset.High);
            Assert.That(low.treeShadows, Is.EqualTo(ShadowQuality.Off));
            Assert.That(low.shadowDistance, Is.LessThan(high.shadowDistance));
            Assert.That(low.lodBias, Is.LessThan(high.lodBias));
        }
        [TestCase("")]
        [TestCase("broken")]
        [TestCase("{\"version\":99,\"state\":{}}")]
        [TestCase("{\"version\":1,\"state\":{\"preset\":400,\"shadowDistance\":40,\"lodBias\":1}}")]
        [TestCase("{\"version\":1,\"state\":{\"shadowDistance\":999,\"lodBias\":1}}")]
        public void CorruptOrUnsupportedPreferencesRecoverToSafeDefault(string json)
        {
            var p = GraphicsQualityProfiles.Load();
            Assert.That(GraphicsSettingsPersistence.Decode(json, p).SameValues(p.Get(GraphicsPreset.Low)), Is.True);
        }
        [Test]
        public void EditedNamedProfileBecomesCustomWithoutLosingIndependentValues()
        {
            var p = GraphicsQualityProfiles.Load(); var state = p.Get(GraphicsPreset.High);
            state.treeShadows = ShadowQuality.Off;
            var decoded = GraphicsSettingsPersistence.Decode(GraphicsSettingsPersistence.Encode(state), p);
            Assert.That(decoded.preset, Is.EqualTo(GraphicsPreset.Custom));
            Assert.That(decoded.treeShadows, Is.EqualTo(ShadowQuality.Off));
            Assert.That(decoded.globalShadows, Is.EqualTo(ShadowQuality.High));
            Assert.That(decoded.shadowDistance, Is.EqualTo(state.shadowDistance));
        }
    }
}
