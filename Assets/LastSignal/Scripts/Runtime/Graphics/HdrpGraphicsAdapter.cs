using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace LastSignal.RenderingQuality
{
    /// <summary>Owns runtime-only HDRP overrides. Never clones or mutates the pipeline asset.</summary>
    public sealed class HdrpGraphicsAdapter
    {
        sealed class CameraState
        {
            public Camera camera; public HDAdditionalCameraData hd; public bool custom;
            public FrameSettings frames; public FrameSettingsOverrideMask mask;
        }
        sealed class LightState
        {
            public Light light; public HDAdditionalLightData hd; public LightShadows shadows;
            public bool useOverride; public int resolution, level;
        }
        readonly List<CameraState> cameras = new List<CameraState>();
        readonly List<LightState> lights = new List<LightState>();
        readonly Dictionary<int, Volume> volumes = new Dictionary<int, Volume>();
        readonly Transform owner;
        VolumeProfile profile;
        HDShadowSettings shadow;
        public string Status { get; private set; } = "";
        public bool Supported => GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset;
        public bool FogSupported => GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset p && p.currentPlatformRenderPipelineSettings.supportVolumetrics;
        public bool AoSupported => GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset p && p.currentPlatformRenderPipelineSettings.supportSSAO;
        public HdrpGraphicsAdapter(Transform parent)
        {
            owner = parent;
            profile = ScriptableObject.CreateInstance<VolumeProfile>(); profile.name = "Runtime graphics quality";
            profile.hideFlags = HideFlags.DontSave;
            shadow = profile.Add<HDShadowSettings>(false);
        }
        public void Register(Camera c)
        {
            if (!c || c.cameraType != CameraType.Game) return;
            foreach (var item in cameras) if (item.camera == c) return;
            var hd = c.GetComponent<HDAdditionalCameraData>(); if (!hd) return;
            cameras.Add(new CameraState { camera = c, hd = hd, custom = hd.customRenderingSettings,
                frames = hd.renderingPathCustomFrameSettings, mask = hd.renderingPathCustomFrameSettingsOverrideMask });
        }
        public void Register(Light light)
        {
            if (!light) return;
            foreach (var item in lights) if (item.light == light) return;
            var hd = light.GetComponent<HDAdditionalLightData>();
            lights.Add(new LightState { light = light, hd = hd, shadows = light.shadows,
                useOverride = hd && hd.shadowResolution.useOverride, resolution = hd ? hd.shadowResolution.@override : 0,
                level = hd ? hd.shadowResolution.level : 0 });
        }
        static void Flag(HDAdditionalCameraData hd, FrameSettingsField field, bool value)
        {
            hd.renderingPathCustomFrameSettings.SetEnabled(field, value);
            hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field] = true;
        }
        public void Apply(GraphicsSettingsState state, GraphicsQualityProfiles profiles)
        {
            if (!Supported) { Status = "HDRP pipeline etkin değil; HDRP kontrolleri kullanılamıyor."; return; }
            Status = "";
            shadow.maxShadowDistance.Override(state.shadowDistance);
            bool enabled = state.globalShadows != ShadowQuality.Off;
            var usedLayers = new HashSet<int>(); // Settings/lifecycle only, not per-frame.
            for (int i = cameras.Count - 1; i >= 0; i--)
            {
                var item = cameras[i];
                if (!item.camera || !item.hd) { cameras.RemoveAt(i); continue; }
                var hd = item.hd;
                // Preserve unrelated camera settings and artistic volume parameters.
                hd.customRenderingSettings = true;
                Flag(hd, FrameSettingsField.ShadowMaps, enabled);
                if (!enabled) Flag(hd, FrameSettingsField.ScreenSpaceShadows, false);
                else
                {
                    hd.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.ScreenSpaceShadows, item.frames.IsEnabled(FrameSettingsField.ScreenSpaceShadows));
                    hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.ScreenSpaceShadows] = item.mask.mask[(uint)FrameSettingsField.ScreenSpaceShadows];
                }
                Flag(hd, FrameSettingsField.ContactShadows, enabled && state.globalShadows == ShadowQuality.High);
                Flag(hd, FrameSettingsField.Volumetrics, state.volumetricFog && FogSupported);
                Flag(hd, FrameSettingsField.SSAO, state.ambientOcclusion && AoSupported);
                int bits = hd.volumeLayerMask.value, layer = -1;
                for (int n = 0; n < 32; n++) if ((bits & (1 << n)) != 0) { layer = n; break; }
                if (layer < 0) { Status = "Kamera Volume Layer Mask boş; gölge mesafesi uygulanamıyor."; continue; }
                usedLayers.Add(layer);
                if (!volumes.TryGetValue(layer, out var volume) || !volume)
                {
                    var go = new GameObject("Runtime graphics distance", typeof(Volume));
                    go.hideFlags = HideFlags.DontSave; go.transform.SetParent(owner, false); go.layer = layer;
                    volume = go.GetComponent<Volume>(); volume.isGlobal = true; volume.priority = float.MaxValue;
                    volume.weight = 1; volume.sharedProfile = profile; volumes[layer] = volume;
                }
                volume.enabled = true;
            }
            foreach (var pair in volumes) if (pair.Value) pair.Value.enabled = usedLayers.Contains(pair.Key);
            var pipeline = (HDRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            var limits = pipeline.currentPlatformRenderPipelineSettings.hdShadowInitParams;
            int resolution = state.globalShadows == ShadowQuality.Low ? profiles.lowShadowResolution :
                state.globalShadows == ShadowQuality.Medium ? profiles.mediumShadowResolution : profiles.highShadowResolution;
            for (int i = lights.Count - 1; i >= 0; i--)
            {
                var item = lights[i];
                if (!item.light) { lights.RemoveAt(i); continue; }
                // Camera frame settings gate shadows; atmosphere retains authority over lamp shadow budgets.
                if (!item.hd) continue;
                int cap = item.light.type == LightType.Directional ? limits.maxDirectionalShadowMapResolution :
                    item.light.type == LightType.Rectangle ? limits.maxAreaShadowMapResolution : limits.maxPunctualShadowMapResolution;
                item.hd.SetShadowResolution(Mathf.Min(resolution, cap)); item.hd.SetShadowResolutionOverride(true);
            }
        }
        // Explicit diagnostics only. Evaluate the same mask/anchor in an isolated stack; do not replace the camera stack.
        public bool TryReadEffectiveDistance(Camera camera, out float distance)
        {
            distance = 0;
            var hd = camera ? camera.GetComponent<HDAdditionalCameraData>() : null;
            if (!Supported || !hd || hd.volumeLayerMask.value == 0) return false;
            var stack = VolumeManager.instance.CreateStack();
            try
            {
                VolumeManager.instance.Update(stack, hd.volumeAnchorOverride ? hd.volumeAnchorOverride : camera.transform, hd.volumeLayerMask);
                var settings = stack.GetComponent<HDShadowSettings>();
                if (!settings) return false;
                distance = settings.maxShadowDistance.value; return true;
            }
            finally { VolumeManager.instance.DestroyStack(stack); }
        }
        public void Restore()
        {
            foreach (var item in cameras) if (item.hd)
            {
                item.hd.customRenderingSettings = item.custom;
                // Restore only fields owned by this adapter, retaining other runtime camera changes.
                foreach (var field in new[] { FrameSettingsField.ScreenSpaceShadows, FrameSettingsField.ShadowMaps, FrameSettingsField.ContactShadows, FrameSettingsField.Volumetrics, FrameSettingsField.SSAO })
                {
                    item.hd.renderingPathCustomFrameSettings.SetEnabled(field, item.frames.IsEnabled(field));
                    item.hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field] = item.mask.mask[(uint)field];
                }
            }
            foreach (var item in lights) if (item.light)
            {

                if (item.hd) { item.hd.SetShadowResolution(item.resolution); item.hd.SetShadowResolutionLevel(item.level); item.hd.SetShadowResolutionOverride(item.useOverride); }
            }
            foreach (var volume in volumes.Values) if (volume) volume.enabled = false;
        }
        public void Dispose()
        {
            Restore();
            foreach (var volume in volumes.Values) if (volume) Object.Destroy(volume.gameObject);
            foreach (var component in profile.components) if (component) Object.Destroy(component);
            Object.Destroy(profile); cameras.Clear(); lights.Clear(); volumes.Clear();
        }
    }
}
