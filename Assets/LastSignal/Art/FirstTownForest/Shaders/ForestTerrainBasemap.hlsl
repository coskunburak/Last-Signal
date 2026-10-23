// Adapted from Unity 17.5 terrain blend math. See UNITY_LICENSE.md.
#ifndef LASTSIGNAL_TERRAIN_BASEMAP_INCLUDED
#define LASTSIGNAL_TERRAIN_BASEMAP_INCLUDED
// Terrain baking uses Unity's blit/GL matrices, not HDRP's camera-relative
// _ViewProjMatrix. This engine include has no render-pipeline package dependency.
#include "UnityCG.cginc"
#define LS_BASEMAP_SAMPLE(textureName, samplerName, uv) tex2D(textureName, uv)
CBUFFER_START(_Terrain)
    half _NormalScale0, _NormalScale1, _NormalScale2, _NormalScale3;
    half _Metallic0, _Metallic1, _Metallic2, _Metallic3;
    half _Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3;
    half4 _DiffuseRemapScale0, _DiffuseRemapScale1, _DiffuseRemapScale2, _DiffuseRemapScale3;
    half4 _MaskMapRemapOffset0, _MaskMapRemapOffset1, _MaskMapRemapOffset2, _MaskMapRemapOffset3;
    half4 _MaskMapRemapScale0, _MaskMapRemapScale1, _MaskMapRemapScale2, _MaskMapRemapScale3;

    float4 _Control_ST;
    float4 _Control_TexelSize;
    half _DiffuseHasAlpha0, _DiffuseHasAlpha1, _DiffuseHasAlpha2, _DiffuseHasAlpha3;
    half _LayerHasMask0, _LayerHasMask1, _LayerHasMask2, _LayerHasMask3;
    half _SmoothnessSource0, _SmoothnessSource1, _SmoothnessSource2, _SmoothnessSource3;
    half4 _Splat0_ST, _Splat1_ST, _Splat2_ST, _Splat3_ST;
    half _HeightTransition;
    half _NumLayersCount;
    float _TerrainBasemapDistance;

    float4 _TerrainHeightmapScale;       // float4(hmScale.x, hmScale.y / (float)(kMaxHeight), hmScale.z, 0.0f)
CBUFFER_END
sampler2D _Control;
sampler2D _Splat0;
sampler2D _Splat1;
sampler2D _Splat2;
sampler2D _Splat3;
sampler2D _Mask0;
sampler2D _Mask1;
sampler2D _Mask2;
sampler2D _Mask3;
struct Attributes { float4 positionOS : POSITION; float2 texcoord : TEXCOORD0; };
struct Varyings { float4 clipPos : SV_POSITION; float4 uvMainAndLM : TEXCOORD0; float4 uvSplat01 : TEXCOORD1; float4 uvSplat23 : TEXCOORD2; };
void SplatmapMix(float4 uvMainAndLM, float4 uvSplat01, float4 uvSplat23, inout half4 splatControl, out half weight, out half4 mixedDiffuse, out half4 defaultSmoothness, inout half3 mixedNormal)
{
    half4 diffAlbedo[4];

    diffAlbedo[0] = LS_BASEMAP_SAMPLE(_Splat0, sampler_Splat0, uvSplat01.xy);
    diffAlbedo[1] = LS_BASEMAP_SAMPLE(_Splat1, sampler_Splat0, uvSplat01.zw);
    diffAlbedo[2] = LS_BASEMAP_SAMPLE(_Splat2, sampler_Splat0, uvSplat23.xy);
    diffAlbedo[3] = LS_BASEMAP_SAMPLE(_Splat3, sampler_Splat0, uvSplat23.zw);

    half4 diffuseAlpha = half4(diffAlbedo[0].a, diffAlbedo[1].a, diffAlbedo[2].a, diffAlbedo[3].a);
    half4 smoothnessConstants = half4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3);
    half4 smoothnessSources = half4(_SmoothnessSource0, _SmoothnessSource1, _SmoothnessSource2, _SmoothnessSource3);

    // Process each layer's smoothness based on its source mode.
    defaultSmoothness.r = (smoothnessSources.r < 0.5) ? diffuseAlpha.r * smoothnessConstants.r : // kConstantMultipliedByDiffuseAlpha
                          (smoothnessSources.r < 1.5) ? diffuseAlpha.r :                         // kDiffuseAlphaChannel
                          smoothnessConstants.r;                                                 // kConstant

    defaultSmoothness.g = (smoothnessSources.g < 0.5) ? diffuseAlpha.g * smoothnessConstants.g :
                          (smoothnessSources.g < 1.5) ? diffuseAlpha.g :
                          smoothnessConstants.g;

    defaultSmoothness.b = (smoothnessSources.b < 0.5) ? diffuseAlpha.b * smoothnessConstants.b :
                          (smoothnessSources.b < 1.5) ? diffuseAlpha.b :
                          smoothnessConstants.b;

    defaultSmoothness.a = (smoothnessSources.a < 0.5) ? diffuseAlpha.a * smoothnessConstants.a :
                          (smoothnessSources.a < 1.5) ? diffuseAlpha.a :
                          smoothnessConstants.a;

#ifndef _TERRAIN_BLEND_HEIGHT // density blending
    if(_NumLayersCount <= 4)
    {
        // 20.0 is the number of steps in inputAlphaMask (Density mask. We decided 20 empirically)
        half4 opacityAsDensity = saturate((half4(diffAlbedo[0].a, diffAlbedo[1].a, diffAlbedo[2].a, diffAlbedo[3].a) - (1 - splatControl)) * 20.0);
        opacityAsDensity += 0.001h * splatControl;      // if all weights are zero, default to what the blend mask says
        half4 useOpacityAsDensityParam = { _DiffuseRemapScale0.w, _DiffuseRemapScale1.w, _DiffuseRemapScale2.w, _DiffuseRemapScale3.w }; // 1 is off
        splatControl = lerp(opacityAsDensity, splatControl, useOpacityAsDensityParam);
    }
#endif

    // Now that splatControl has changed, we can compute the final weight and normalize
    weight = dot(splatControl, 1.0h);

#ifdef TERRAIN_SPLAT_ADDPASS
    clip(weight <= 0.005h ? -1.0h : 1.0h);
#endif

#ifndef _TERRAIN_BASEMAP_GEN
    // Normalize weights before lighting and restore weights in final modifier functions so that the overal
    // lighting result can be correctly weighted.
    splatControl /= (weight + HALF_MIN);
#endif

    mixedDiffuse = 0.0h;
    mixedDiffuse += diffAlbedo[0] * half4(_DiffuseRemapScale0.rgb * splatControl.rrr, 1.0h);
    mixedDiffuse += diffAlbedo[1] * half4(_DiffuseRemapScale1.rgb * splatControl.ggg, 1.0h);
    mixedDiffuse += diffAlbedo[2] * half4(_DiffuseRemapScale2.rgb * splatControl.bbb, 1.0h);
    mixedDiffuse += diffAlbedo[3] * half4(_DiffuseRemapScale3.rgb * splatControl.aaa, 1.0h);

    // No normal output is baked by either basemap pass.
}
void HeightBasedSplatModify(inout half4 splatControl, in half4 masks[4])
{
    // heights are in mask blue channel, we multiply by the splat Control weights to get combined height
    half4 splatHeight = half4(masks[0].b, masks[1].b, masks[2].b, masks[3].b) * splatControl.rgba;
    half maxHeight = max(splatHeight.r, max(splatHeight.g, max(splatHeight.b, splatHeight.a)));

    // Ensure that the transition height is not zero.
    half transition = max(_HeightTransition, 1e-5);

    // This sets the highest splat to "transition", and everything else to a lower value relative to that, clamping to zero
    // Then we clamp this to zero and normalize everything
    half4 weightedHeights = splatHeight + transition - maxHeight.xxxx;
    weightedHeights = max(0, weightedHeights);

    // We need to add an epsilon here for active layers (hence the blendMask again)
    // so that at least a layer shows up if everything's too low.
    weightedHeights = (weightedHeights + 1e-6) * splatControl;

    // Normalize (and clamp to epsilon to keep from dividing by zero)
    half sumHeight = max(dot(weightedHeights, half4(1, 1, 1, 1)), 1e-6);
    splatControl = weightedHeights / sumHeight.xxxx;
}
#endif
