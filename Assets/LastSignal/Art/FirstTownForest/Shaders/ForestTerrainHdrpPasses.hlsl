#ifndef LASTSIGNAL_FOREST_TERRAIN_HDRP_PASSES_INCLUDED
#define LASTSIGNAL_FOREST_TERRAIN_HDRP_PASSES_INCLUDED

#include "Assets/LastSignal/Scripts/Runtime/Rendering/HdrpCustomLighting.hlsl"

float _LSForestWetness;

CBUFFER_START(UnityTerrain)
    float4 _Control_ST;
    float4 _Control_TexelSize;
    half _HeightTransition;
    half _NumLayersCount;
    float4 _Splat0_ST, _Splat1_ST, _Splat2_ST, _Splat3_ST;
    half _Metallic0, _Metallic1, _Metallic2, _Metallic3;
    half _Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3;
    half _NormalScale0, _NormalScale1, _NormalScale2, _NormalScale3;
    half4 _DiffuseRemapScale0, _DiffuseRemapScale1, _DiffuseRemapScale2, _DiffuseRemapScale3;
    half4 _MaskMapRemapOffset0, _MaskMapRemapOffset1, _MaskMapRemapOffset2, _MaskMapRemapOffset3;
    half4 _MaskMapRemapScale0, _MaskMapRemapScale1, _MaskMapRemapScale2, _MaskMapRemapScale3;
    half _LayerHasMask0, _LayerHasMask1, _LayerHasMask2, _LayerHasMask3;
#ifdef UNITY_INSTANCING_ENABLED
    float4 _TerrainHeightmapRecipSize;
#endif
    float4 _TerrainHeightmapScale;
CBUFFER_END

TEXTURE2D(_Control); SAMPLER(sampler_Control);
TEXTURE2D(_Splat0); SAMPLER(sampler_Splat0);
TEXTURE2D(_Splat1);
TEXTURE2D(_Splat2);
TEXTURE2D(_Splat3);
TEXTURE2D(_Normal0); SAMPLER(sampler_Normal0);
TEXTURE2D(_Normal1);
TEXTURE2D(_Normal2);
TEXTURE2D(_Normal3);
TEXTURE2D(_Mask0); SAMPLER(sampler_Mask0);
TEXTURE2D(_Mask1);
TEXTURE2D(_Mask2);
TEXTURE2D(_Mask3);
TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
TEXTURE2D(_MetallicTex); SAMPLER(sampler_MetallicTex);
TEXTURE2D(_TerrainHolesTexture); SAMPLER(sampler_TerrainHolesTexture);

#ifdef UNITY_INSTANCING_ENABLED
TEXTURE2D(_TerrainHeightmapTexture);
TEXTURE2D(_TerrainNormalmapTexture);
SAMPLER(sampler_TerrainNormalmapTexture);
UNITY_INSTANCING_BUFFER_START(Terrain)
    UNITY_DEFINE_INSTANCED_PROP(float4, _TerrainPatchInstanceData)
UNITY_INSTANCING_BUFFER_END(Terrain)
#endif

void LS_TerrainInstancing(inout float4 positionOS, inout float3 normalOS, inout float2 uv)
{
#ifdef UNITY_INSTANCING_ENABLED
    float2 patchVertex = positionOS.xy;
    float4 instanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);
    float2 sampleCoords = (patchVertex.xy + instanceData.xy) * instanceData.z;
    float height = UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(sampleCoords, 0)));
    positionOS.xz = sampleCoords * _TerrainHeightmapScale.xz;
    positionOS.y = height * _TerrainHeightmapScale.y;
    normalOS = _TerrainNormalmapTexture.Load(int3(sampleCoords, 0)).rgb * 2.0 - 1.0;
    uv = sampleCoords * _TerrainHeightmapRecipSize.zw;
#endif
}

struct HdrpTerrainAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct HdrpTerrainVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionRWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float2 uvMain : TEXCOORD2;
    float4 uvSplat01 : TEXCOORD3;
    float4 uvSplat23 : TEXCOORD4;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

HdrpTerrainVaryings HdrpTerrainVert(HdrpTerrainAttributes v)
{
    HdrpTerrainVaryings o = (HdrpTerrainVaryings)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    LS_TerrainInstancing(v.positionOS, v.normalOS, v.texcoord);
    o.positionRWS = TransformObjectToWorld(v.positionOS.xyz);
    o.positionCS = TransformWorldToHClip(o.positionRWS);
    o.normalWS = TransformObjectToWorldNormal(v.normalOS);
    o.uvMain = v.texcoord;
    o.uvSplat01.xy = v.texcoord * _Splat0_ST.xy + _Splat0_ST.zw;
    o.uvSplat01.zw = v.texcoord * _Splat1_ST.xy + _Splat1_ST.zw;
    o.uvSplat23.xy = v.texcoord * _Splat2_ST.xy + _Splat2_ST.zw;
    o.uvSplat23.zw = v.texcoord * _Splat3_ST.xy + _Splat3_ST.zw;
    return o;
}

void HdrpTerrainDepthFrag(HdrpTerrainVaryings IN)
{
    UNITY_SETUP_INSTANCE_ID(IN);
#ifdef _ALPHATEST_ON
    float hole = SAMPLE_TEXTURE2D(_TerrainHolesTexture, sampler_TerrainHolesTexture, IN.uvMain).r;
    clip(hole == 0.0f ? -1 : 1);
#endif
}

float4 HdrpTerrainForwardFrag(HdrpTerrainVaryings IN) : SV_Target0
{
    UNITY_SETUP_INSTANCE_ID(IN);
#ifdef _ALPHATEST_ON
    float hole = SAMPLE_TEXTURE2D(_TerrainHolesTexture, sampler_TerrainHolesTexture, IN.uvMain).r;
    clip(hole == 0.0f ? -1 : 1);
#endif

    float3 normalWS = normalize(IN.normalWS);
#if defined(UNITY_INSTANCING_ENABLED) && defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)
    float2 sampleCoords = (IN.uvMain / _TerrainHeightmapRecipSize.zw + 0.5f) * _TerrainHeightmapRecipSize.xy;
    float3 instNormalOS = normalize(SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, sampleCoords).rgb * 2.0 - 1.0);
    normalWS = TransformObjectToWorldNormal(instNormalOS);
#endif

#ifdef TERRAIN_SPLAT_BASEPASS
    float4 mainSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvMain);
    float3 albedo = mainSample.rgb;
    float smoothness = mainSample.a;
    float metallic = SAMPLE_TEXTURE2D(_MetallicTex, sampler_MetallicTex, IN.uvMain).r;
    float occlusion = 1.0;
#else
    float2 splatUV = (IN.uvMain * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
    float4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);
    float weight = dot(splatControl, 1.0);
    splatControl /= (weight + 1e-5);

    float4 diff0 = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, IN.uvSplat01.xy);
    float4 diff1 = SAMPLE_TEXTURE2D(_Splat1, sampler_Splat0, IN.uvSplat01.zw);
    float4 diff2 = SAMPLE_TEXTURE2D(_Splat2, sampler_Splat0, IN.uvSplat23.xy);
    float4 diff3 = SAMPLE_TEXTURE2D(_Splat3, sampler_Splat0, IN.uvSplat23.zw);

    float3 albedo = 0.0;
    albedo += diff0.rgb * (_DiffuseRemapScale0.rgb * splatControl.rrr);
    albedo += diff1.rgb * (_DiffuseRemapScale1.rgb * splatControl.ggg);
    albedo += diff2.rgb * (_DiffuseRemapScale2.rgb * splatControl.bbb);
    albedo += diff3.rgb * (_DiffuseRemapScale3.rgb * splatControl.aaa);

    float4 defaultSmoothness = float4(diff0.a * _Smoothness0, diff1.a * _Smoothness1, diff2.a * _Smoothness2, diff3.a * _Smoothness3);
    float4 defaultMetallic = float4(_Metallic0, _Metallic1, _Metallic2, _Metallic3);
    float4 defaultOcclusion = 1.0;

    #ifdef _MASKMAP
    float4 hasMask = float4(_LayerHasMask0, _LayerHasMask1, _LayerHasMask2, _LayerHasMask3);
    float4 m0 = SAMPLE_TEXTURE2D(_Mask0, sampler_Mask0, IN.uvSplat01.xy) * _MaskMapRemapScale0 + _MaskMapRemapOffset0;
    float4 m1 = SAMPLE_TEXTURE2D(_Mask1, sampler_Mask0, IN.uvSplat01.zw) * _MaskMapRemapScale1 + _MaskMapRemapOffset1;
    float4 m2 = SAMPLE_TEXTURE2D(_Mask2, sampler_Mask0, IN.uvSplat23.xy) * _MaskMapRemapScale2 + _MaskMapRemapOffset2;
    float4 m3 = SAMPLE_TEXTURE2D(_Mask3, sampler_Mask0, IN.uvSplat23.zw) * _MaskMapRemapScale3 + _MaskMapRemapOffset3;
    defaultSmoothness = lerp(defaultSmoothness, float4(m0.a, m1.a, m2.a, m3.a), hasMask);
    defaultMetallic = lerp(defaultMetallic, float4(m0.r, m1.r, m2.r, m3.r), hasMask);
    defaultOcclusion = lerp(defaultOcclusion, float4(m0.g, m1.g, m2.g, m3.g), hasMask);
    #endif

    float smoothness = dot(splatControl, defaultSmoothness);
    float metallic = dot(splatControl, defaultMetallic);
    float occlusion = dot(splatControl, defaultOcclusion);

    #ifdef _NORMALMAP
    float3 nrm = 0.0;
    nrm += splatControl.r * UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal0, sampler_Normal0, IN.uvSplat01.xy), _NormalScale0);
    nrm += splatControl.g * UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal1, sampler_Normal0, IN.uvSplat01.zw), _NormalScale1);
    nrm += splatControl.b * UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal2, sampler_Normal0, IN.uvSplat23.xy), _NormalScale2);
    nrm += splatControl.a * UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal3, sampler_Normal0, IN.uvSplat23.zw), _NormalScale3);
    nrm.z += 1e-5f;
    float3 normalTS = normalize(nrm);
    float3 tangentWS = normalize(cross(float3(0.0, 0.0, 1.0), normalWS));
    float3 bitangentWS = normalize(cross(normalWS, tangentWS));
    normalWS = normalize(normalTS.x * (-tangentWS) + normalTS.y * bitangentWS + normalTS.z * normalWS);
    #endif
#endif

    float wet = saturate(_LSForestWetness);
    albedo *= 1.0 - wet * 0.18;
    smoothness = lerp(smoothness, max(smoothness, 0.23), wet);

    LS_HdrpSurface s = (LS_HdrpSurface)0;
    s.albedo = albedo;
    s.normalWS = normalWS;
    s.metallic = metallic;
    s.smoothness = smoothness;
    s.occlusion = occlusion;
    s.alpha = 1.0;

    float3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionRWS);
    float3 lit = LS_EvaluateHdrpLit(IN.positionCS.xy, IN.positionRWS, viewDirWS, s);
#ifndef TERRAIN_SPLAT_BASEPASS
    // Four-layer rendering is unchanged. Additional groups contribute only their
    // control-map weight, instead of adding a full-strength lighting result.
    #ifdef LS_TERRAIN_ADDPASS
    clip(weight - 0.005);
    #endif
    if (_NumLayersCount > 4) lit *= weight;
#endif
    return float4(lit, 1.0);
}

#endif
