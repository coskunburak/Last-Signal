Shader "LastSignal/HDRP/BloodSurface"
{
    Properties
    {
        _BaseMap("Vendor green-channel splat mask", 2D) = "white" {}
        _BaseColor("Blood", Color) = (.23,.027,.023,.8)
        _Smoothness("Wetness", Range(0,1)) = .48
    }
    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode"="ForwardOnly" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex HdrpVert
            #pragma fragment HdrpFrag
            #pragma multi_compile_instancing
            #include "Assets/LastSignal/Scripts/Runtime/Rendering/HdrpCustomLighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _Smoothness;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionRWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID };
            Varyings HdrpVert(Attributes v)
            {
                Varyings o; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v,o);
                o.positionRWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionRWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = v.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                return o;
            }
            float4 HdrpFrag(Varyings i) : SV_Target0
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float mask = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).g;
                float alpha = smoothstep(0.08, 0.38, mask) * _BaseColor.a;
                clip(alpha - 0.005);
                LS_HdrpSurface s = (LS_HdrpSurface)0;
                s.albedo = _BaseColor.rgb;
                s.normalWS = normalize(i.normalWS);
                s.smoothness = _Smoothness;
                s.occlusion = 1.0;
                s.alpha = alpha;
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionRWS);
                float3 color = LS_EvaluateHdrpLit(i.positionCS.xy, i.positionRWS, viewDirWS, s);
                return float4(color, alpha);
            }
            ENDHLSL
        }
    }
}
