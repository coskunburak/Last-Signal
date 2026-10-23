Shader "LastSignal/VFX/Blood/ParticleSplat"
{
    // Project-owned dual-pipeline replacement for the URP-only vendor graph
    // "Vefects/SH_Vefects_URP_Free_Blood_01_Lit" used by the three PS_LS_Blood_* splat materials.
    // Property names and defaults intentionally match the vendor graph so existing material
    // values (LUT, erosion, distortion, opacity mask, blend/depth state) are preserved verbatim.
    Properties
    {
        _FlatColor("Flat Color", Range(0, 1)) = 0
        _Metallic("Metallic", Float) = 0
        _Smoothness("Smoothness", Float) = 1
        _EmissionMultiply("Emission Multiply", Float) = 0
        _OpacityMultiply("Opacity Multiply", Float) = 1
        _DepthFade("Depth Fade", Float) = 0
        [Header(LUT)] _LUT("LUT", 2D) = "white" {}
        _LUTAmplitude("LUT Amplitude", Float) = 1
        _LUTOffset("LUT Offset", Float) = 0
        _LUTSpeed("LUT Speed", Float) = 0
        _LUTHueShift("LUT Hue Shift", Float) = 0
        _LUTHueShiftSpeed("LUT Hue Shift Speed", Float) = 0
        _LUTDesaturate("LUT Desaturate", Range(0, 1)) = 0
        _LUTValueIntensity("LUT Value Intensity", Float) = 1
        [ToggleUI] _LUTErodes("LUT Erodes", Float) = 0
        [ToggleUI] _LUTErodesMasks("LUT Erodes Masks", Float) = 0
        [ToggleUI] _LUTInvert("LUT Invert", Float) = 0
        [Header(Normal)] _NormalTexture("Normal Texture", 2D) = "white" {}
        _NormalIntensity("Normal Intensity", Float) = 1
        [Header(Distortion)] _DistortionTexture("Distortion Texture", 2D) = "white" {}
        _NoiseDistortionUVS("Noise Distortion UV S", Vector) = (1, 1, 0, 0)
        _NoiseDistortionUVPan("Noise Distortion UV Pan", Vector) = (0, 0, 0, 0)
        _DistortionIntensity("Distortion Intensity", Float) = 0.1
        [Header(Erosion)] _ErosionTexture("Erosion Texture", 2D) = "white" {}
        _ErosionUVS("Erosion UV S", Vector) = (1, 1, 0, 0)
        _ErosionUVPan("Erosion UV Pan", Vector) = (0, 0, 0, 0)
        _ErosionIntensity("Erosion Intensity", Float) = 0
        _ErosionSmoothness("Erosion Smoothness", Float) = 1
        [Header(Opacity Mask)] _OpacityMask("Opacity Mask", 2D) = "white" {}
        _OpacityMaskErosion("Opacity Mask Erosion", Float) = 0
        _OpacityMaskErosionSmoothness("Opacity Mask Erosion Smoothness", Float) = 1
        [Header(Render State)] _Cull("Cull", Float) = 2
        _Src("Src", Float) = 5
        _Dst("Dst", Float) = 10
        _ZWrite("ZWrite", Float) = 0
        _ZTest("ZTest", Float) = 4
    }

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }
            Blend [_Src] [_Dst], One OneMinusSrcAlpha
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex HdrpVert
            #pragma fragment HdrpFrag
            #pragma multi_compile_instancing

            #include "Assets/LastSignal/Scripts/Runtime/Rendering/HdrpCustomLighting.hlsl"
            #include "Assets/LastSignal/Shaders/VFX/Blood/LS_BloodParticleSplatCommon.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float4 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionRWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 uv : TEXCOORD2;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings HdrpVert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.positionRWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionRWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float4 HdrpFrag(Varyings i) : SV_Target0
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float depthFade01 = 1.0;
                if (_DepthFade > 0.0)
                {
                    float sceneEyeDepth = LinearEyeDepth(LoadCameraDepth(uint2(i.positionCS.xy)), _ZBufferParams);
                    depthFade01 = saturate((sceneEyeDepth - i.positionCS.w) / _DepthFade);
                }

                LS_BloodSplatSample splat = LS_EvaluateBloodSplat(i.uv, i.color, depthFade01, _Time.y);
                clip(splat.alpha - 0.002);

                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionRWS);
                float3 geometricNormal = SafeNormalize(i.normalWS);
                geometricNormal = dot(geometricNormal, viewDirWS) < 0.0 ? -geometricNormal : geometricNormal;

                LS_HdrpSurface s = (LS_HdrpSurface)0;
                s.albedo = splat.baseColor;
                s.normalWS = LS_BloodSplatPerturbNormal(geometricNormal, i.positionRWS, i.uv.xy, splat.normalTS);
                s.emission = splat.baseColor * _EmissionMultiply;
                s.metallic = saturate(_Metallic);
                s.smoothness = saturate(_Smoothness);
                s.occlusion = 1.0;
                s.alpha = splat.alpha;

                float3 color = LS_EvaluateHdrpLit(i.positionCS.xy, i.positionRWS, viewDirWS, s);
                return float4(color, splat.alpha);
            }
            ENDHLSL
        }
    }



    Fallback Off
}
