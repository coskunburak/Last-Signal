#ifndef LASTSIGNAL_HDRP_CUSTOM_LIGHTING_INCLUDED
#define LASTSIGNAL_HDRP_CUSTOM_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#define SHADOW_LOW
#ifndef AREA_SHADOW_LOW
#define AREA_SHADOW_LOW
#endif
#define SHADOW_DATA_NOT_GUARANTEED_SCALAR
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/HDShadow.hlsl"

struct LS_HdrpSurface
{
    float3 albedo;
    float3 normalWS;
    float3 emission;
    float metallic;
    float smoothness;
    float occlusion;
    float alpha;
};

float3 LS_EvaluateHdrpPunctualLights(
    float3 positionRWS,
    float3 normalWS,
    float3 viewDirWS,
    float3 diffuseColor,
    float3 fresnel0,
    float roughness,
    float specPower,
    float specNorm)
{
    float3 accum = 0.0;
    uint count = min(_PunctualLightCount, 8u);
    for (uint i = 0u; i < count; ++i)
    {
        LightData pl = _LightDatas[i];
        float3 toLight = pl.positionRWS - positionRWS;
        float distSq = max(dot(toLight, toLight), 0.0001);
        float rangeSq = max(pl.range * pl.range, 0.0001);
        if (distSq >= rangeSq)
            continue;

        float rcpDist = rsqrt(distSq);
        float3 L = toLight * rcpDist;
        float NdotL = saturate(dot(normalWS, L));
        if (NdotL <= 0.0)
            continue;

        float factor = distSq / rangeSq;
        float smoothWindow = saturate(1.0 - factor * factor);
        smoothWindow *= smoothWindow;
        float atten = smoothWindow / max(distSq, 0.04);

        if (pl.lightType == GPULIGHTTYPE_SPOT)
        {
            float cosAngle = dot(-L, pl.forward);
            float cone = saturate(cosAngle * pl.angleScale + pl.angleOffset);
            atten *= cone * cone;
        }

        float3 H = SafeNormalize(L + viewDirWS);
        float NdotH = saturate(dot(normalWS, H));
        float3 spec = fresnel0 * (pow(NdotH, specPower) * specNorm);
        float3 radiance = pl.color * (pl.lightDimmer * atten * NdotL);
        accum += (diffuseColor * INV_PI * pl.diffuseDimmer + spec * pl.specularDimmer) * radiance;
    }
    return accum;
}

float3 LS_EvaluateHdrpLit(
    float2 positionSS,
    float3 positionRWS,
    float3 viewDirWS,
    LS_HdrpSurface s)
{
    float3 N = SafeNormalize(s.normalWS);
    float3 V = SafeNormalize(viewDirWS);
    float roughness = clamp(1.0 - s.smoothness, 0.04, 1.0);
    float specPower = exp2(10.0 * s.smoothness + 1.0);
    float specNorm = (specPower + 2.0) * (0.125 * s.smoothness);
    float3 diffuseColor = s.albedo * (1.0 - s.metallic);
    float3 fresnel0 = lerp(float3(0.04, 0.04, 0.04), s.albedo, s.metallic);

    float3 directLighting = 0.0;
    if (_DirectionalLightCount > 0u)
    {
        DirectionalLightData dirLight = _DirectionalLightDatas[0];
        float3 L = -dirLight.forward;
        float NdotL = saturate(dot(N, L));
        if (NdotL > 0.0)
        {
            float shadowAtten = 1.0;
            if (_DirectionalShadowIndex >= 0 && dirLight.shadowIndex >= 0)
            {
                HDShadowContext sc = InitShadowContext();
                shadowAtten = GetDirectionalShadowAttenuation(sc, positionSS, positionRWS, N, dirLight.shadowIndex, L);
                shadowAtten = lerp(1.0, shadowAtten, dirLight.shadowDimmer);
            }

            float3 H = SafeNormalize(L + V);
            float NdotH = saturate(dot(N, H));
            float3 spec = fresnel0 * (pow(NdotH, specPower) * specNorm);
            float3 radiance = dirLight.color * (dirLight.lightDimmer * shadowAtten * NdotL);
            directLighting += (diffuseColor * INV_PI * dirLight.diffuseDimmer + spec * dirLight.specularDimmer) * radiance;
        }
    }

    directLighting += LS_EvaluateHdrpPunctualLights(positionRWS, N, V, diffuseColor, fresnel0, roughness, specPower, specNorm);

    float3 indirectDiffuse;
    float3 indirectSpec;
    if (_EnvLightSkyEnabled != 0)
    {
        indirectDiffuse = SampleSkyTexture(N, 6.0, 0).rgb * _IndirectDiffuseLightingMultiplier;
        float3 R = reflect(-V, N);
        indirectSpec = SampleSkyTexture(R, roughness * 6.0, 0).rgb * _ReflectionLightingMultiplier;
    }
    else
    {
        float hemi = N.y * 0.5 + 0.5;
        indirectDiffuse = lerp(float3(0.06, 0.07, 0.09), float3(0.24, 0.28, 0.34), hemi);
        indirectSpec = indirectDiffuse * 0.35;
    }

    float NdotV = saturate(dot(N, V));
    float fresnelTerm = Pow4(1.0 - NdotV);
    float3 envSpec = lerp(fresnel0, saturate(s.smoothness + fresnel0), fresnelTerm) * indirectSpec * (s.smoothness * 0.75 + 0.05);

    float3 total = directLighting + (diffuseColor * indirectDiffuse + envSpec) * s.occlusion + s.emission;
    return total * GetCurrentExposureMultiplier();
}

#endif
