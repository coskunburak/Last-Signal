using LastSignal.Forest;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace LastSignal.Editor.FirstTownForest
{
    internal static class ForestPrototypeAtmosphere
    {
        internal static ForestPrototypeLighting Build(Transform parent, string folder)
        {
            var root = ForestPrototypeTerrain.Child("Lighting", parent);
            var sun = ForestPrototypeTerrain.Child("Sun", root).gameObject.AddComponent<Light>();
            sun.type = LightType.Directional; sun.shadows = LightShadows.Soft;
            var hd = sun.gameObject.AddComponent<HDAdditionalLightData>(); hd.SetShadowResolution(2048);
            hd.useContactShadow.useOverride = true; hd.useContactShadow.@override = true;
            var volume = ForestPrototypeTerrain.Child("HDRP atmosphere", root).gameObject.AddComponent<Volume>();
            volume.isGlobal = true; volume.priority = 30;
            var lighting = root.gameObject.AddComponent<ForestPrototypeLighting>(); lighting.sun = sun; lighting.volume = volume;
            lighting.morning = Profile(folder, false); lighting.overcast = Profile(folder, true); lighting.Apply(false);
            return lighting;
        }
        static VolumeProfile Profile(string folder, bool cloudy)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>(); profile.name = cloudy ? "Overcast" : "Morning";
            var environment = profile.Add<VisualEnvironment>(true);
            environment.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
            if (cloudy)
            {
                environment.skyType.Override((int)SkyType.Gradient);
                var sky = profile.Add<GradientSky>(true);
                sky.top.Override(new Color(.47f, .54f, .61f)); sky.middle.Override(new Color(.67f, .7f, .72f)); sky.bottom.Override(new Color(.18f, .19f, .17f));
                sky.skyIntensityMode.Override(SkyIntensityMode.Exposure); sky.exposure.Override(11.8f);
            }
            else
            {
                environment.skyType.Override((int)SkyType.PhysicallyBased);
                var sky = profile.Add<PhysicallyBasedSky>(true); sky.groundTint.Override(new Color(.12f, .13f, .1f));
                sky.aerosolDensity.Override(.035f); sky.aerosolAnisotropy.Override(.65f);
            }
            var exposure = profile.Add<Exposure>(true); exposure.mode.Override(ExposureMode.Fixed); exposure.fixedExposure.Override(cloudy ? 11.5f : 12.8f);
            var tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
            var fog = profile.Add<Fog>(true); fog.enabled.Override(true); fog.enableVolumetricFog.Override(true);
            fog.meanFreePath.Override(cloudy ? 130 : 240); fog.baseHeight.Override(3); fog.maximumHeight.Override(32);
            fog.depthExtent.Override(96); fog.maxFogDistance.Override(350); fog.anisotropy.Override(cloudy ? .15f : .42f);
            fog.directionalLightsOnly.Override(true);
            fog.albedo.Override(new Color(.84f, .87f, .85f)); fog.globalLightProbeDimmer.Override(.75f);
            var shadows = profile.Add<HDShadowSettings>(true); shadows.maxShadowDistance.Override(80);
            var ao = profile.Add<ScreenSpaceAmbientOcclusion>(true); ao.intensity.Override(.55f); ao.radius.Override(.55f);
            var contact = profile.Add<ContactShadows>(true); contact.enable.Override(true); contact.length.Override(.18f); contact.maxDistance.Override(20);
            var color = profile.Add<ColorAdjustments>(true); color.saturation.Override(-6); color.postExposure.Override(0);
            var bloom = profile.Add<Bloom>(true); bloom.intensity.Override(.025f);
            AssetDatabase.CreateAsset(profile, folder + "/" + profile.name + ".asset");
            foreach (var c in profile.components) { c.name = c.GetType().Name; AssetDatabase.AddObjectToAsset(c, profile); }
            return profile;
        }
    }
}
