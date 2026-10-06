Shader "LastSignal/BloodSurface"
{
    Properties
    {
        _BaseMap("Vendor green-channel splat mask", 2D) = "white" {}
        _BaseColor("Blood", Color) = (.23,.027,.023,.8)
        _Smoothness("Wetness", Range(0,1)) = .48
    }
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
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _Smoothness;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID };
            Varyings Vert(Attributes v)
            {
                Varyings o; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v,o);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv,_BaseMap); return o;
            }
            half3 Shade(Light light, half3 n, half3 view)
            {
                half diffuse = saturate(dot(n,light.direction));
                half spec = pow(saturate(dot(n,normalize(light.direction+view))),lerp(8,64,_Smoothness)) * .12 * _Smoothness;
                return (_BaseColor.rgb*diffuse+spec) * light.color * light.distanceAttenuation * light.shadowAttenuation;
            }
            half4 Frag(Varyings i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half mask = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).g;
                half alpha = smoothstep(.08,.38,mask) * _BaseColor.a; clip(alpha-.005);
                half3 n=normalize(i.normalWS), view=GetWorldSpaceNormalizeViewDir(i.positionWS);
                half3 color=SampleSH(n)*_BaseColor.rgb;
                color+=Shade(GetMainLight(TransformWorldToShadowCoord(i.positionWS)),n,view);
                #ifdef _ADDITIONAL_LIGHTS
                uint count=GetAdditionalLightsCount();
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                #if USE_CLUSTER_LIGHT_LOOP
                for(uint lightIndex=0;lightIndex<min(URP_FP_DIRECTIONAL_LIGHTS_COUNT,MAX_VISIBLE_LIGHTS);lightIndex++)
                    color+=Shade(GetAdditionalLight(lightIndex,i.positionWS),n,view);
                #endif
                LIGHT_LOOP_BEGIN(count)
                    color+=Shade(GetAdditionalLight(lightIndex,i.positionWS),n,view);
                LIGHT_LOOP_END
                #endif
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
}
