Shader "LastSignal/HDRP/Ballistic"
{
    Properties { _BaseColor("Color", Color) = (1,1,1,1) _Mark("Entry mark", Float) = 0 }
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
            #include "Assets/LastSignal/Scripts/Runtime/Rendering/HdrpCustomLighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float _Mark;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD1; float3 positionRWS:TEXCOORD2; float4 color:COLOR; float2 uv:TEXCOORD0; };
            V HdrpVert(A v)
            {
                V o;
                o.positionRWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionCS = TransformWorldToHClip(o.positionRWS);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }
            float4 HdrpFrag(V i) : SV_Target0
            {
                if (_Mark > 0.5)
                {
                    float2 p = i.uv * 2.0 - 1.0;
                    float angle = atan2(p.y, p.x);
                    float edge = 0.74 + 0.1 * sin(angle * 7.0) + 0.06 * cos(angle * 13.0);
                    float alpha = 1.0 - smoothstep(edge - 0.2, edge, length(p));
                    LS_HdrpSurface s = (LS_HdrpSurface)0;
                    s.albedo = _BaseColor.rgb;
                    s.normalWS = normalize(i.normalWS);
                    s.smoothness = 0.15;
                    s.occlusion = 1.0;
                    s.alpha = alpha * _BaseColor.a;
                    float3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionRWS);
                    float3 lit = LS_EvaluateHdrpLit(i.positionCS.xy, i.positionRWS, viewDirWS, s);
                    return float4(lit, s.alpha);
                }
                float4 unlit = i.color * _BaseColor;
                return float4(unlit.rgb * GetCurrentExposureMultiplier(), unlit.a);
            }
            ENDHLSL
        }
    }
}
