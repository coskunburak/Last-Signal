Shader "LastSignal/Ballistic"
{
    Properties { _BaseColor("Color", Color) = (1,1,1,1) _Mark("Entry mark", Float) = 0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor; float _Mark;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD1; float3 positionWS:TEXCOORD2; half4 color:COLOR; float2 uv:TEXCOORD0; };
            V Vert(A v) { V o; o.positionWS=TransformObjectToWorld(v.positionOS.xyz); o.normalWS=TransformObjectToWorldNormal(v.normalOS); o.positionCS=TransformWorldToHClip(o.positionWS); o.color=v.color; o.uv=v.uv; return o; }
            half4 Frag(V i):SV_Target
            {
                if (_Mark > .5)
                {
                    float2 p = i.uv * 2 - 1;
                    float angle = atan2(p.y,p.x);
                    float edge = .74 + .1*sin(angle*7) + .06*cos(angle*13);
                    float alpha = 1-smoothstep(edge-.2,edge,length(p));
                    half3 normal = normalize(i.normalWS);
                    Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                    half3 illumination = SampleSH(normal) + light.color * saturate(dot(normal,light.direction)) * light.shadowAttenuation;
                    #if defined(_ADDITIONAL_LIGHTS)
                    uint count = GetAdditionalLightsCount();
                    InputData inputData = (InputData)0;
                    inputData.positionWS = i.positionWS;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                    #if USE_CLUSTER_LIGHT_LOOP
                    for(uint lightIndex=0;lightIndex<min(URP_FP_DIRECTIONAL_LIGHTS_COUNT,MAX_VISIBLE_LIGHTS);lightIndex++)
                    {
                        Light extra=GetAdditionalLight(lightIndex,i.positionWS);
                        illumination += extra.color*saturate(dot(normal,extra.direction))*extra.distanceAttenuation*extra.shadowAttenuation;
                    }
                    #endif
                    LIGHT_LOOP_BEGIN(count)
                        Light extra=GetAdditionalLight(lightIndex,i.positionWS);
                        illumination += extra.color*saturate(dot(normal,extra.direction))*extra.distanceAttenuation*extra.shadowAttenuation;
                    LIGHT_LOOP_END
                    #endif
                    return half4(_BaseColor.rgb*illumination,alpha*_BaseColor.a);
                }
                return i.color * _BaseColor;
            }
            ENDHLSL
        }
    }
}
