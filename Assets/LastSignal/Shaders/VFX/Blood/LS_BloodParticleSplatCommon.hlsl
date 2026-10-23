#ifndef LASTSIGNAL_BLOOD_PARTICLE_SPLAT_COMMON_INCLUDED
#define LASTSIGNAL_BLOOD_PARTICLE_SPLAT_COMMON_INCLUDED

// Project-owned, pipeline-agnostic surface math for blood particle splats.
// Ports the authoring contract of the vendor Amplify graph
// "Vefects/SH_Vefects_URP_Free_Blood_01_Lit" (Assets/ThirdParty, read-only) so that the
// project-owned materials under Assets/LastSignal/Materials/VFX/Blood keep their exact
// property values and render under both HDRP 17.5 and URP 17.5.
//
// Particle vertex streams (PS_LS_Blood_*.prefab, m_VertexStreams 0001030422):
//   POSITION, NORMAL, COLOR, TEXCOORD0.xy = UV, TEXCOORD0.zw = Custom1.xy
//   Custom1.x -> additional erosion threshold, Custom1.y -> erosion smoothness reduction.
// The caller must include Common.hlsl (core) and its pipeline variables before this file.

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

TEXTURE2D(_LUT);                SAMPLER(sampler_LUT);
TEXTURE2D(_NormalTexture);      SAMPLER(sampler_NormalTexture);
TEXTURE2D(_DistortionTexture);  SAMPLER(sampler_DistortionTexture);
TEXTURE2D(_ErosionTexture);     SAMPLER(sampler_ErosionTexture);
TEXTURE2D(_OpacityMask);        SAMPLER(sampler_OpacityMask);

CBUFFER_START(UnityPerMaterial)
    float4 _OpacityMask_ST;
    float4 _NoiseDistortionUVS;
    float4 _NoiseDistortionUVPan;
    float4 _ErosionUVS;
    float4 _ErosionUVPan;
    float _FlatColor;
    float _Metallic;
    float _Smoothness;
    float _EmissionMultiply;
    float _OpacityMultiply;
    float _DepthFade;
    float _LUTAmplitude;
    float _LUTOffset;
    float _LUTSpeed;
    float _LUTHueShift;
    float _LUTHueShiftSpeed;
    float _LUTDesaturate;
    float _LUTValueIntensity;
    float _LUTErodes;
    float _LUTErodesMasks;
    float _LUTInvert;
    float _NormalIntensity;
    float _DistortionIntensity;
    float _ErosionIntensity;
    float _ErosionSmoothness;
    float _OpacityMaskErosion;
    float _OpacityMaskErosionSmoothness;
CBUFFER_END

struct LS_BloodSplatSample
{
    float3 baseColor;
    float3 normalTS;
    float alpha;
};

// uv: TEXCOORD0 (xy = UV, zw = Custom1.xy). vertexColor: particle colour.
// depthFade01: saturate((sceneEyeDepth - fragmentEyeDepth) / _DepthFade), computed by the caller.
LS_BloodSplatSample LS_EvaluateBloodSplat(float4 uv, float4 vertexColor, float depthFade01, float time)
{
    LS_BloodSplatSample o;

    float2 erosionUV = time * _ErosionUVPan.xy + uv.xy * _ErosionUVS.xy;
    float2 distortionUV = time * _NoiseDistortionUVPan.xy + uv.xy * _NoiseDistortionUVS.xy;
    float2 distortion = (SAMPLE_TEXTURE2D(_DistortionTexture, sampler_DistortionTexture, distortionUV).rg - 0.5) * 2.0
                        * (_DistortionIntensity * 0.01);
    float2 distortedErosionUV = erosionUV + distortion;

    float erosionValue = saturate(SAMPLE_TEXTURE2D(_ErosionTexture, sampler_ErosionTexture, distortedErosionUV).g);
    float erosionThreshold = _ErosionIntensity + uv.z;
    float eroded = saturate(smoothstep(erosionThreshold,
                                       erosionThreshold + (_ErosionSmoothness - uv.w),
                                       erosionValue));

    float2 maskUV = uv.xy * _OpacityMask_ST.xy + _OpacityMask_ST.zw;
    float maskValue = saturate(SAMPLE_TEXTURE2D(_OpacityMask, sampler_OpacityMask, maskUV).g);
    float maskEroded = saturate(smoothstep(_OpacityMaskErosion,
                                           _OpacityMaskErosion + _OpacityMaskErosionSmoothness,
                                           maskValue));
    float combinedMask = saturate(eroded * maskEroded);

    float lutInput = (_LUTErodes > 0.5) ? eroded : erosionValue;
    lutInput = (_LUTErodesMasks > 0.5) ? combinedMask : lutInput;
    lutInput = (_LUTInvert > 0.5) ? (1.0 - lutInput) : lutInput;

    float lutCoord = lutInput * _LUTAmplitude + _LUTOffset + time * _LUTSpeed;
    float3 lutColor = SAMPLE_TEXTURE2D(_LUT, sampler_LUT, float2(lutCoord, lutCoord)).rgb;
    float3 hsv = RgbToHsv(lutColor);
    hsv.x = hsv.x + _LUTHueShift + time * _LUTHueShiftSpeed;
    hsv.z *= _LUTValueIntensity;
    float3 shifted = HsvToRgb(float3(frac(hsv.x), hsv.y, hsv.z));
    float luma = dot(shifted, float3(0.299, 0.587, 0.114));
    float3 desaturated = lerp(shifted, luma.xxx, _LUTDesaturate);

    o.baseColor = lerp(desaturated * vertexColor.rgb, vertexColor.rgb, _FlatColor);

    o.alpha = saturate(saturate(saturate(combinedMask * depthFade01) * _OpacityMultiply) * vertexColor.a);

    // Vendor normal texture is imported as a default sRGB texture (textureType 0, sRGB on);
    // re-encode to gamma before decoding to a tangent-space vector.
    float3 packedNormal = FastLinearToSRGB(SAMPLE_TEXTURE2D(_NormalTexture, sampler_NormalTexture, distortedErosionUV).rgb);
    float3 decodedNormal = SafeNormalize(packedNormal * 2.0 - 1.0);
    o.normalTS = SafeNormalize(lerp(float3(0.0, 0.0, 1.0), decodedNormal, saturate(_NormalIntensity * o.alpha)));
    return o;
}

// Billboard particles do not stream tangents; derive a cotangent frame from screen derivatives.
float3 LS_BloodSplatPerturbNormal(float3 normalWS, float3 positionWS, float2 uv, float3 normalTS)
{
    float3 dp1 = ddx(positionWS);
    float3 dp2 = ddy(positionWS);
    float2 duv1 = ddx(uv);
    float2 duv2 = ddy(uv);
    float3 N = SafeNormalize(normalWS);
    float3 dp2perp = cross(dp2, N);
    float3 dp1perp = cross(N, dp1);
    float3 T = dp2perp * duv1.x + dp1perp * duv2.x;
    float3 B = dp2perp * duv1.y + dp1perp * duv2.y;
    float invMax = rsqrt(max(max(dot(T, T), dot(B, B)), 1e-12));
    T *= invMax;
    B *= invMax;
    return SafeNormalize(normalTS.x * T + normalTS.y * B + normalTS.z * N);
}

#endif
