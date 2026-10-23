// Explicit SCN-S001 candidate calibration; never invoked on import or through a regeneration menu.
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using LastSignal.Atmosphere;
using LastSignal.Rendering;
internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/LastSignal/Scenes/Production/FirstTownMain_HDRP.unity" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Requires saved main candidate outside Play mode.");
        EditorSceneManager.SaveScene(scene, "Docs/Implementation/Scenes/SCN-S001/Evidence/BeforeLighting.unity", true);
        var a = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TownAtmosphere>(true)).Single();
        var lighting = a.lightingProfile;
        var profile = a.volume.sharedProfile;
        foreach (var asset in new UnityEngine.Object[] {lighting, profile})
            if (!AssetDatabase.GetAssetPath(asset).StartsWith("Assets/LastSignal/Art/FirstTownMain_HDRP/")) throw new InvalidOperationException("Asset must be isolated.");
        result.RegisterObjectModification(lighting);
        lighting.usePhysicalHdrpLighting = true;
        lighting.hdrpSunLuxPerUnit = 42000; lighting.hdrpMoonLuxPerUnit = .5f;
        lighting.hdrpDayExposure = 12.8f; lighting.hdrpOvercastExposure = 11.5f; lighting.hdrpNightExposure = 4.5f;
        lighting.daylight.bloom = .025f; lighting.sunset.bloom = .035f; lighting.dusk.bloom = .025f; lighting.night.bloom = .02f;
        result.RegisterObjectModification(profile);
        foreach (var c in profile.components) result.RegisterObjectModification(c);
        RenderPipelineAdapter.EnsureAtmosphereVolumeComponents(profile);
        foreach (var c in profile.components)
            if (!AssetDatabase.Contains(c)) { c.name = c.GetType().Name; AssetDatabase.AddObjectToAsset(c, profile); result.RegisterObjectCreation(c); }
        profile.TryGet<PhysicallyBasedSky>(out var sky); sky.aerosolDensity.Override(.035f); sky.aerosolAnisotropy.Override(.65f);
        profile.TryGet<Fog>(out var fog); fog.depthExtent.Override(96); fog.maxFogDistance.Override(350);
        fog.directionalLightsOnly.Override(true); fog.globalLightProbeDimmer.Override(.75f);
        profile.TryGet<HDShadowSettings>(out var shadows); shadows.maxShadowDistance.Override(80);
        profile.TryGet<ScreenSpaceAmbientOcclusion>(out var ao); ao.intensity.Override(.55f); ao.radius.Override(.55f);
        profile.TryGet<ContactShadows>(out var contact); contact.length.Override(.18f); contact.maxDistance.Override(20);
        profile.TryGet<ScreenSpaceReflection>(out var ssr); ssr.minSmoothness = .6f;
        const float day = .3555556f, clouds = .65f;
        var phase = lighting.Evaluate(day, clouds, 0);
        foreach (var light in new[] {a.sun, a.moon}) if (light)
        {
            result.RegisterObjectModification(light);
            var hd = light.GetComponent<HDAdditionalLightData>();
            if (!hd) { hd = light.gameObject.AddComponent<HDAdditionalLightData>(); result.RegisterObjectCreation(hd); }
            else result.RegisterObjectModification(hd);
        }
        a.sun.intensity = phase.keyIntensity; a.sun.color = phase.keyColor;
        if (a.moon) a.moon.intensity = phase.moonIntensity;
        RenderPipelineAdapter.ApplyAtmosphereLightingAndVolume(profile, a.sun, a.moon, day, TownLightingProfile.Altitude(day), 1, 0, clouds, 0,
            phase.fogDensity, phase.fog, phase.ambient, lighting.hdrpSunLuxPerUnit, lighting.hdrpMoonLuxPerUnit);
        a.SamplePostProcessing(profile, day, 0, clouds);
        profile.TryGet<ColorAdjustments>(out var grade); grade.saturation.Override(-6);
        foreach (var layer in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Terrain>(true)).SelectMany(t => t.terrainData.terrainLayers).Distinct())
        {
            if (!AssetDatabase.GetAssetPath(layer).StartsWith("Assets/LastSignal/Art/FirstTownMain_HDRP/PrototypeVisuals/")) continue;
            result.RegisterObjectModification(layer);
            // Unity 6.5 adds explicit smoothness source: diffuse alpha=1 ignored the low authored constant.
            var so = new SerializedObject(layer); so.FindProperty("m_SmoothnessSource").intValue = 2; so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(layer);
        }
        int wet = 0;
        foreach (var renderer in a.wetRenderers.Where(r => r))
            if (a.surfaces.Any(s => s.wetOnly && renderer.sharedMaterials.Contains(s.material)))
            { result.RegisterObjectModification(renderer); renderer.enabled = false; wet++; }
        foreach (var c in profile.components) EditorUtility.SetDirty(c);
        EditorUtility.SetDirty(lighting); EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        SceneView.lastActiveSceneView.LookAtDirect(new Vector3(-222, 5, -112), Quaternion.Euler(8, 325, 0), 16);
        SceneView.lastActiveSceneView.Repaint();
        result.Log("Saved candidate physical lighting and explicit TerrainLayer constant smoothness. Dry puddle renderers hidden=" + wet + "; sunLux=" + a.sun.intensity + "; exposure=" + lighting.HdrpExposure(day, clouds, 0));
    }
}
