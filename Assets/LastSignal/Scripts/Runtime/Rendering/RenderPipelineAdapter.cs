using System;
using UnityEngine;
using UnityEngine.Rendering;
#if USING_HDRP
using UnityEngine.Rendering.HighDefinition;
using HdrpTonemapping = UnityEngine.Rendering.HighDefinition.Tonemapping;
using HdrpTonemappingMode = UnityEngine.Rendering.HighDefinition.TonemappingMode;
using HdrpColorAdjustments = UnityEngine.Rendering.HighDefinition.ColorAdjustments;
using HdrpBloom = UnityEngine.Rendering.HighDefinition.Bloom;
using HdrpVignette = UnityEngine.Rendering.HighDefinition.Vignette;
using HdrpFog = UnityEngine.Rendering.HighDefinition.Fog;
using HdrpExposure = UnityEngine.Rendering.HighDefinition.Exposure;
using HdrpVisualEnvironment = UnityEngine.Rendering.HighDefinition.VisualEnvironment;
using HdrpPhysicallyBasedSky = UnityEngine.Rendering.HighDefinition.PhysicallyBasedSky;
using HdrpVolumetricClouds = UnityEngine.Rendering.HighDefinition.VolumetricClouds;
using HdrpShadowSettings = UnityEngine.Rendering.HighDefinition.HDShadowSettings;
using HdrpSSAO = UnityEngine.Rendering.HighDefinition.ScreenSpaceAmbientOcclusion;
using HdrpSSR = UnityEngine.Rendering.HighDefinition.ScreenSpaceReflection;
using HdrpContactShadows = UnityEngine.Rendering.HighDefinition.ContactShadows;
#endif
using Object = UnityEngine.Object;

namespace LastSignal.Rendering
{
    public enum PipelineKind
    {
        None = 0,
        URP = 1,
        HDRP = 2,
        Unknown = 3
    }

    /// <summary>
    /// HDRP camera, lighting, volume and material integration.
    /// Legacy pipeline identifiers remain for migration diagnostics and recovery tooling;
    /// runtime rendering does not depend on the Universal pipeline assembly.
    /// </summary>
    public static class RenderPipelineAdapter
    {
        public const string UrpLitShaderName = "Universal Render Pipeline/Lit";
        public const string HdrpLitShaderName = "HDRP/Lit";
        public const string UrpTerrainLitShaderName = "Universal Render Pipeline/Terrain/Lit";
        public const string HdrpTerrainLitShaderName = "HDRP/TerrainLit";

        // EV100 value where ConvertEV100ToExposure(ev100, 0.65f) == 1.0f in HDRP, keeping pre-exposure at 1:1 unit scale.
        public const float HdrpUnitExposureEv100 = -0.2630344f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int LegacyColorId = Shader.PropertyToID("_Color");
        static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        static readonly int GlossinessId = Shader.PropertyToID("_Glossiness");
        static readonly int SmoothnessRemapMaxId = Shader.PropertyToID("_SmoothnessRemapMax");
        static readonly int CoatMaskId = Shader.PropertyToID("_CoatMask");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");

        public struct CameraPostState
        {
#if USING_HDRP
            internal HDAdditionalCameraData hdrpData;
            internal HDAdditionalCameraData.AntialiasingMode oldHdrpAA;
            internal HDAdditionalCameraData.SMAAQualityLevel oldHdrpAAQuality;
#endif
            public bool Bound
            {
                get
                {
#if USING_HDRP
                    if (hdrpData) return true;
#endif
                    return false;
                }
            }
        }

        public static PipelineKind ActivePipeline
        {
            get
            {
                var current = QualitySettings.renderPipeline ? QualitySettings.renderPipeline : GraphicsSettings.currentRenderPipeline;
                if (!current) return PipelineKind.None;
#if USING_HDRP
                if (current is HDRenderPipelineAsset) return PipelineKind.HDRP;
#endif
                string typeName = current.GetType().Name;
                if (typeName.IndexOf("HDRenderPipeline", StringComparison.OrdinalIgnoreCase) >= 0) return PipelineKind.HDRP;
                if (typeName.IndexOf("UniversalRenderPipeline", StringComparison.OrdinalIgnoreCase) >= 0) return PipelineKind.URP;
                return PipelineKind.Unknown;
            }
        }

        public static bool IsHDRP => IsHdrpActive;
        public static bool IsURP => ActivePipeline == PipelineKind.URP;
        public static bool IsUrpActive => ActivePipeline == PipelineKind.URP;

        public static bool IsHdrpActive
        {
            get
            {
#if USING_HDRP
                var current = QualitySettings.renderPipeline ? QualitySettings.renderPipeline : GraphicsSettings.currentRenderPipeline;
                return current is HDRenderPipelineAsset;
#else
                return false;
#endif
            }
        }

        static bool UseHdrpPath
        {
            get
            {
                return IsHdrpActive;
            }
        }

        public static Shader FindStandardLitShader() => Shader.Find(HdrpLitShaderName);

        public static Shader FindTerrainLitShader() => Shader.Find(HdrpTerrainLitShaderName);

        public static bool IsPipelineFamilyShader(Shader shader)
        {
            if (!shader) return false;
#if USING_HDRP
            if (shader.name == HdrpLitShaderName || shader.name.StartsWith("HDRP/", StringComparison.Ordinal))
                return true;
#endif
            return false;
        }

        public static bool IsSupportedPipelineMaterial(Material m)
        {
            if (!m || !m.shader) return false;
            string tag = m.GetTag("RenderPipeline", false, "");
#if USING_HDRP
            if (tag == "HDRenderPipeline" || m.shader.name == HdrpLitShaderName || m.shader.name.StartsWith("HDRP/", StringComparison.Ordinal))
                return true;
#endif
            return false;
        }

        public static bool IsOpaqueMetallicVehicleLitMaterial(Material m)
        {
            if (!m || !m.shader) return false;
#if USING_HDRP
            if (m.shader.name == HdrpLitShaderName)
            {
                float surfaceType = m.HasProperty("_SurfaceType") ? m.GetFloat("_SurfaceType") : 0f;
                return surfaceType == 0f && m.HasProperty("_Metallic") && m.GetFloat("_Metallic") > .2f;
            }
#endif
            return false;
        }

        public static bool IsOpaqueUnclippedStandardLitMaterial(Material m)
        {
            if (!m || !m.shader) return false;
#if USING_HDRP
            if (m.shader.name == HdrpLitShaderName)
            {
                float surfaceType = m.HasProperty("_SurfaceType") ? m.GetFloat("_SurfaceType") : 0f;
                float alphaCutoffEnable = m.HasProperty("_AlphaCutoffEnable") ? m.GetFloat("_AlphaCutoffEnable") : 0f;
                return surfaceType == 0f && alphaCutoffEnable <= .5f;
            }
#endif
            return false;
        }

        public static Color GetMaterialBaseColor(Material m)
        {
            if (!m) return Color.white;
            if (m.HasProperty(BaseColorId)) return m.GetColor(BaseColorId);
            if (m.HasProperty(LegacyColorId)) return m.GetColor(LegacyColorId);
            return Color.white;
        }

        public static float GetMaterialSmoothness(Material m)
        {
            if (!m) return 0.5f;
            if (m.HasProperty(SmoothnessId)) return m.GetFloat(SmoothnessId);
            if (m.HasProperty(GlossinessId)) return m.GetFloat(GlossinessId);
            return 0.5f;
        }

        public static void SetMaterialWetSurface(Material m, Color wetColor, float wetSmoothness, float wetness)
        {
            if (!m) return;
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, wetColor);
            if (m.HasProperty(LegacyColorId)) m.SetColor(LegacyColorId, wetColor);
            if (m.HasProperty(SmoothnessId)) m.SetFloat(SmoothnessId, wetSmoothness);
            if (m.HasProperty(SmoothnessRemapMaxId)) m.SetFloat(SmoothnessRemapMaxId, wetSmoothness);
#if USING_HDRP
            if (UseHdrpPath && m.HasProperty(CoatMaskId))
            {
                m.SetFloat(CoatMaskId, Mathf.Clamp01(wetness * 0.35f));
            }
#endif
        }

        public static float ScalePracticalLightIntensity(float urpIntensity)
        {
#if USING_HDRP
            if (UseHdrpPath)
                return urpIntensity * Mathf.PI;
#endif
            return urpIntensity;
        }

        public static void ApplyFixtureEmission(Renderer renderer, MaterialPropertyBlock block, Color hdrColor)
        {
            if (!renderer || block == null) return;
            var mat = renderer.sharedMaterial;
            if (!mat) return;
            bool supportsUrpEmission = mat.IsKeywordEnabled("_EMISSION") || mat.HasProperty(EmissionColorId);
            bool supportsHdrpEmission = mat.HasProperty(EmissiveColorId);
            if (!supportsUrpEmission && !supportsHdrpEmission) return;
            renderer.GetPropertyBlock(block);
            if (mat.HasProperty(EmissionColorId)) block.SetColor(EmissionColorId, hdrColor);
            if (supportsHdrpEmission) block.SetColor(EmissiveColorId, hdrColor);
            renderer.SetPropertyBlock(block);
        }

        public static CameraPostState EnableGameplayPostAndAA(Camera camera)
        {
            var state = default(CameraPostState);
            if (!camera) return state;
#if USING_HDRP
            if (UseHdrpPath)
            {
                var hd = camera.GetComponent<HDAdditionalCameraData>();
                if (!hd) hd = camera.gameObject.AddComponent<HDAdditionalCameraData>();
                state.hdrpData = hd;
                state.oldHdrpAA = hd.antialiasing;
                state.oldHdrpAAQuality = hd.SMAAQuality;
                hd.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                hd.SMAAQuality = HDAdditionalCameraData.SMAAQualityLevel.Medium;
                return state;
            }
#endif
            return state;
        }

        public static void RestoreCameraPostAndAA(ref CameraPostState state)
        {
#if USING_HDRP
            if (state.hdrpData)
            {
                state.hdrpData.antialiasing = state.oldHdrpAA;
                state.hdrpData.SMAAQuality = state.oldHdrpAAQuality;
            }
#endif
            state = default;
        }

        public static void EnableCameraPostProcessingOnly(Camera camera)
        {
            if (!camera) return;
#if USING_HDRP
            if (UseHdrpPath)
            {
                if (!camera.GetComponent<HDAdditionalCameraData>())
                    camera.gameObject.AddComponent<HDAdditionalCameraData>();
                return;
            }
#endif
        }

        public static void EnableGoldenCaptureCameraPostAndAA(Camera camera)
        {
            if (!camera) return;
#if USING_HDRP
            if (UseHdrpPath)
            {
                var hd = camera.GetComponent<HDAdditionalCameraData>();
                if (!hd) hd = camera.gameObject.AddComponent<HDAdditionalCameraData>();
                hd.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                return;
            }
#endif
        }

        public static void ConfigureScopeCamera(GameObject cameraObject, int rendererIndex)
        {
            if (!cameraObject) return;
#if USING_HDRP
            if (UseHdrpPath)
            {
                var hd = cameraObject.GetComponent<HDAdditionalCameraData>();
                if (!hd) hd = cameraObject.AddComponent<HDAdditionalCameraData>();
                // Keep volumeLayerMask = -1 in HDRP so the scope camera inherits VisualEnvironment (Sky), Fog, and Exposure pre-exposition,
                // while disabling FrameSettingsField.Postprocess so tonemapping/bloom/vignette are not applied twice.
                hd.volumeLayerMask = -1;
                hd.backgroundColorHDR = new Color(0.03f, 0.04f, 0.05f, 1.0f);
                hd.clearColorMode = HDAdditionalCameraData.ClearColorMode.Sky;
                hd.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
                hd.customRenderingSettings = true;
                hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.Postprocess] = true;
                hd.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.Postprocess, false);
                return;
            }
#endif
        }

        public static void ConfigureInspectionCamera(Camera inspectionCamera)
        {
            if (!inspectionCamera) return;
#if USING_HDRP
            if (UseHdrpPath)
            {
                if (!inspectionCamera.GetComponent<HDAdditionalCameraData>())
                    inspectionCamera.gameObject.AddComponent<HDAdditionalCameraData>();
                return;
            }
#endif
        }

        public static bool ApplyBenchmarkQualityTierOverride(string tier)
        {
#if USING_HDRP
            if (GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset hdrpSource)
            {
                var hdrpClone = Object.Instantiate(hdrpSource);
                QualitySettings.renderPipeline = hdrpClone;
                return true;
            }
#endif
            return false;
        }

        public static void EnsureAtmosphereVolumeComponents(VolumeProfile profile, bool preserveAuthored = false)
        {
            if (!profile) return;
#if USING_HDRP
            if (UseHdrpPath)
            {
                // A complete physical profile owns its authored sky/fog/shadow settings.
                // Runtime samplers update weather and exposure without resetting those settings each frame.
                if (preserveAuthored && profile.TryGet<HdrpVisualEnvironment>(out _) &&
                    profile.TryGet<HdrpExposure>(out _) && profile.TryGet<HdrpFog>(out _)) return;
                if (!profile.TryGet<HdrpVisualEnvironment>(out var visualEnv))
                    visualEnv = profile.Add<HdrpVisualEnvironment>(true);
                visualEnv.skyType.Override((int)SkyType.PhysicallyBased);
                visualEnv.cloudType.Override(0);
                visualEnv.skyAmbientMode.Override(SkyAmbientMode.Dynamic);

                if (!profile.TryGet<HdrpPhysicallyBasedSky>(out var pbrSky))
                    pbrSky = profile.Add<HdrpPhysicallyBasedSky>(true);
                pbrSky.type.Override(PhysicallyBasedSkyModel.EarthAdvanced);
                pbrSky.atmosphericScattering.Override(true);
                pbrSky.skyIntensityMode.Override(SkyIntensityMode.Multiplier);
                pbrSky.multiplier.Override(1.35f);
                pbrSky.updateMode.Override(EnvironmentUpdateMode.OnChanged);

                if (!profile.TryGet<HdrpFog>(out var fog))
                    fog = profile.Add<HdrpFog>(true);
                fog.enabled.Override(true);
                fog.colorMode.Override(FogColorMode.ConstantColor);
                fog.meanFreePath.Override(110f);
                fog.baseHeight.Override(8f);
                fog.maximumHeight.Override(68f);
                fog.enableVolumetricFog.Override(true);
                fog.anisotropy.Override(0.45f);
                fog.depthExtent.Override(96f);

                if (!profile.TryGet<HdrpVolumetricClouds>(out var clouds))
                    clouds = profile.Add<HdrpVolumetricClouds>(true);
                clouds.enable.Override(true);
                clouds.cloudControl.Override(HdrpVolumetricClouds.CloudControl.Simple);
                clouds.densityMultiplier.Override(0.38f);
                clouds.shapeFactor.Override(0.78f);
                clouds.erosionFactor.Override(0.75f);
                clouds.bottomAltitude.Override(1100f);
                clouds.altitudeRange.Override(2200f);
                clouds.shadows.Override(true);
                clouds.shadowOpacity.Override(0.35f);

                if (!profile.TryGet<HdrpExposure>(out var exposure))
                    exposure = profile.Add<HdrpExposure>(true);
                exposure.mode.Override(ExposureMode.Fixed);
                exposure.fixedExposure.Override(HdrpUnitExposureEv100);

                if (!profile.TryGet<HdrpShadowSettings>(out var shadows))
                    shadows = profile.Add<HdrpShadowSettings>(true);
                shadows.maxShadowDistance.Override(140f);
                shadows.cascadeShadowSplitCount.Override(4);

                if (!profile.TryGet<HdrpSSAO>(out var ssao))
                    ssao = profile.Add<HdrpSSAO>(true);
                ssao.intensity.Override(0.65f);
                ssao.directLightingStrength.Override(0.25f);
                ssao.radius.Override(1.25f);

                if (!profile.TryGet<HdrpSSR>(out var ssr))
                    ssr = profile.Add<HdrpSSR>(true);
                ssr.enabled.Override(true);
                ssr.minSmoothness = 0.38f;

                if (!profile.TryGet<HdrpContactShadows>(out var contactShadows))
                    contactShadows = profile.Add<HdrpContactShadows>(true);
                contactShadows.enable.Override(true);
                contactShadows.length.Override(0.35f);
                contactShadows.opacity.Override(0.75f);

                if (!profile.TryGet<HdrpTonemapping>(out var hdTone))
                    hdTone = profile.Add<HdrpTonemapping>(true);
                hdTone.mode.Override(HdrpTonemappingMode.ACES);

                if (!profile.TryGet<HdrpColorAdjustments>(out var hdColor))
                    hdColor = profile.Add<HdrpColorAdjustments>(true);

                if (!profile.TryGet<HdrpBloom>(out var hdBloom))
                    hdBloom = profile.Add<HdrpBloom>(true);
                hdBloom.intensity.Override(0.12f);
                hdBloom.threshold.Override(1.2f);
                hdBloom.scatter.Override(0.35f);

                if (!profile.TryGet<HdrpVignette>(out var hdVignette))
                    hdVignette = profile.Add<HdrpVignette>(true);
                hdVignette.intensity.Override(0.05f);
                hdVignette.smoothness.Override(0.35f);
            }
#endif
        }

        public static void ApplyAtmosphereLightingAndVolume(
            VolumeProfile targetVolume,
            Light sun,
            Light moon,
            float day,
            float altitude,
            float daylight,
            float gold,
            float clouds,
            float rainAmount,
            float fogDensity,
            Color fogColor,
            Color ambientColor,
            float sunLuxPerUnit = 0f,
            float moonLuxPerUnit = .5f)
        {
#if USING_HDRP
            if (!UseHdrpPath) return;

            if (sun)
            {
                var hdSun = sun.GetComponent<HDAdditionalLightData>();
                if (!hdSun) hdSun = sun.gameObject.AddComponent<HDAdditionalLightData>();
                hdSun.interactsWithSky = true;
                hdSun.volumetricDimmer = Mathf.Lerp(1.0f, 1.45f, Mathf.Clamp01(rainAmount * 0.7f + gold * 0.5f));
                // Compensate for HDRP's 1/PI Lambertian normalization while preserving day > night monotonic ordering.
                if (sunLuxPerUnit > 0)
                {
                    sun.lightUnit = UnityEngine.Rendering.LightUnit.Lux;
                    sun.intensity = Mathf.Max(0, sun.intensity * sunLuxPerUnit);
                }
                else sun.intensity = Mathf.Max(0.02f, sun.intensity * Mathf.PI);
            }

            if (moon)
            {
                var hdMoon = moon.GetComponent<HDAdditionalLightData>();
                if (!hdMoon) hdMoon = moon.gameObject.AddComponent<HDAdditionalLightData>();
                hdMoon.interactsWithSky = true;
                hdMoon.volumetricDimmer = 0.65f;
                if (sunLuxPerUnit > 0) moon.lightUnit = UnityEngine.Rendering.LightUnit.Lux;
                moon.intensity *= sunLuxPerUnit > 0 ? Mathf.Max(0, moonLuxPerUnit) : Mathf.PI;
            }

            if (!targetVolume) return;
            EnsureAtmosphereVolumeComponents(targetVolume, sunLuxPerUnit > 0);

            if (targetVolume.TryGet<HdrpPhysicallyBasedSky>(out var pbrSky))
            {
                pbrSky.skyIntensityMode.Override(SkyIntensityMode.Multiplier);
                float skyMul = Mathf.Lerp(0.24f, 1.65f, daylight) * Mathf.Lerp(1.0f, 0.58f, clouds) * Mathf.Lerp(1.0f, 0.72f, rainAmount);
                pbrSky.multiplier.Override(skyMul);
                pbrSky.groundTint.Override(Color.Lerp(new Color(0.04f, 0.05f, 0.06f), new Color(0.14f, 0.16f, 0.13f), daylight));
                pbrSky.horizonTint.Override(Color.Lerp(Color.white, new Color(1.0f, 0.82f, 0.66f), gold * 0.65f));
                pbrSky.zenithTint.Override(Color.Lerp(new Color(0.76f, 0.84f, 0.96f), new Color(0.66f, 0.72f, 0.76f), Mathf.Clamp01(rainAmount * 0.8f + clouds * 0.3f)));
            }

            if (targetVolume.TryGet<HdrpFog>(out var hdFog))
            {
                hdFog.enabled.Override(true);
                hdFog.colorMode.Override(FogColorMode.ConstantColor);
                hdFog.color.Override(fogColor);
                hdFog.tint.Override(fogColor);
                float mfp = Mathf.Clamp(0.32f / Mathf.Max(0.0005f, fogDensity), 38f, 240f);
                hdFog.meanFreePath.Override(mfp);
                hdFog.baseHeight.Override(sunLuxPerUnit > 0 ? 3f : 6.0f + rainAmount * 8.0f);
                hdFog.maximumHeight.Override(sunLuxPerUnit > 0 ? 32f : 55.0f + clouds * 35.0f);
                hdFog.enableVolumetricFog.Override(true);
                hdFog.albedo.Override(Color.Lerp(new Color(0.82f, 0.86f, 0.90f), new Color(0.92f, 0.84f, 0.74f), gold * 0.5f));
                hdFog.anisotropy.Override(Mathf.Lerp(0.35f, 0.62f, Mathf.Clamp01(gold + rainAmount * 0.35f)));
            }

            if (targetVolume.TryGet<HdrpVolumetricClouds>(out var hdClouds))
            {
                hdClouds.enable.Override(true);
                hdClouds.cloudControl.Override(HdrpVolumetricClouds.CloudControl.Simple);
                hdClouds.densityMultiplier.Override(Mathf.Lerp(0.22f, 0.92f, clouds));
                hdClouds.shapeFactor.Override(Mathf.Lerp(0.88f, 0.62f, clouds));
                hdClouds.erosionFactor.Override(Mathf.Lerp(0.82f, 0.55f, rainAmount));
                hdClouds.bottomAltitude.Override(Mathf.Lerp(1200f, 750f, rainAmount));
                hdClouds.altitudeRange.Override(Mathf.Lerp(2000f, 3200f, clouds));
                hdClouds.shadows.Override(true);
                hdClouds.shadowOpacity.Override(Mathf.Lerp(0.25f, 0.65f, clouds));
            }
#endif
        }

        public static void SampleAtmospherePostProcessing(
            VolumeProfile target,
            bool phaseLighting,
            float postExposure,
            float saturation,
            float contrast,
            float bloomIntensity,
            float? physicalExposureEv100 = null)
        {
            if (!target) return;
#if USING_HDRP
            if (UseHdrpPath)
            {
                EnsureAtmosphereVolumeComponents(target, physicalExposureEv100.HasValue);
                if (target.TryGet<HdrpExposure>(out var hdExposure))
                {
                    hdExposure.mode.Override(ExposureMode.Fixed);
                    hdExposure.fixedExposure.Override(physicalExposureEv100 ?? HdrpUnitExposureEv100);
                }
                if (target.TryGet<HdrpColorAdjustments>(out var hdGrade))
                {
                    hdGrade.postExposure.Override(postExposure);
                    hdGrade.saturation.Override(saturation);
                    if (phaseLighting) hdGrade.contrast.Override(contrast);
                }
                if (phaseLighting)
                {
                    if (target.TryGet<HdrpBloom>(out var hdBloom))
                    {
                        hdBloom.intensity.Override(bloomIntensity);
                        hdBloom.threshold.Override(1.25f);
                    }
                    if (target.TryGet<HdrpVignette>(out var hdVignette))
                        hdVignette.intensity.Override(.05f);
                }
                return;
            }
#endif
        }

        public static bool TryGetPostExposure(VolumeProfile profile, out float postExposure)
        {
            postExposure = 0f;
            if (!profile) return false;
#if USING_HDRP
            if (UseHdrpPath && profile.TryGet<HdrpColorAdjustments>(out var hdGrade))
            {
                postExposure = hdGrade.postExposure.value;
                return true;
            }
#endif
            return false;
        }

        public static void RenderSingleCameraToTarget(Camera camera, RenderTexture target)
        {
            if (!camera || !target) throw new ArgumentNullException();
#if USING_HDRP
            if (UseHdrpPath)
            {
                var prev = camera.targetTexture;
                try { camera.targetTexture = target; camera.Render(); }
                finally { camera.targetTexture = prev; }
                return;
            }
#endif
            throw new InvalidOperationException("Single-camera capture requires the active HDRP pipeline.");
        }

        public static void ConfigureControlledAcesVolumeProfile(VolumeProfile profile)
        {
            if (!profile) throw new ArgumentNullException(nameof(profile));
#if USING_HDRP
            if (UseHdrpPath)
            {
                EnsureAtmosphereVolumeComponents(profile);
                if (profile.TryGet<HdrpTonemapping>(out var hdTone))
                    hdTone.mode.Override(HdrpTonemappingMode.ACES);
                if (profile.TryGet<HdrpColorAdjustments>(out var hdColor))
                {
                    hdColor.postExposure.Override(0f);
                    hdColor.contrast.Override(4f);
                    hdColor.saturation.Override(0f);
                }
                if (profile.TryGet<HdrpBloom>(out var hdBloom))
                {
                    hdBloom.intensity.Override(.12f);
                    hdBloom.threshold.Override(1.2f);
                    hdBloom.scatter.Override(.35f);
                }
                if (profile.TryGet<HdrpVignette>(out var hdVignette))
                {
                    hdVignette.intensity.Override(.08f);
                    hdVignette.smoothness.Override(.35f);
                }
                return;
            }
#endif
        }

        public static bool IsValidControlledAcesVolumeProfile(VolumeProfile profile)
        {
            if (!profile) return false;
            for (int i = 0; i < profile.components.Count; i++)
                if (!profile.components[i]) return false;
#if USING_HDRP
            if (profile.TryGet<HdrpTonemapping>(out var hdTone) && hdTone.mode.value == HdrpTonemappingMode.ACES)
                return true;
#endif
            return false;
        }

        public static void AddAcesTonemappingOverride(VolumeProfile profile)
        {
            if (!profile) throw new ArgumentNullException(nameof(profile));
#if USING_HDRP
            if (UseHdrpPath)
            {
                profile.Add<HdrpTonemapping>().mode.Override(HdrpTonemappingMode.ACES);
                return;
            }
#endif
        }

        public static void ConfigureS013ProductionVolumeProfile(VolumeProfile profile, float saturation)
        {
            if (!profile) throw new ArgumentNullException(nameof(profile));
#if USING_HDRP
            if (UseHdrpPath)
            {
                EnsureAtmosphereVolumeComponents(profile);
                if (profile.TryGet<HdrpTonemapping>(out var hdTone))
                    hdTone.mode.Override(HdrpTonemappingMode.ACES);
                if (profile.TryGet<HdrpColorAdjustments>(out var hdColor))
                    hdColor.saturation.Override(saturation);
                return;
            }
#endif
        }
    }
}
