Shader "LastSignal/Optics/ScopeLens"
{
    Properties
    {
        _ScopeTex("Scope Texture", 2D) = "black" {}
        _Visibility("Visibility", Range(0,1)) = 0
        _LensCenter("Lens Center", Vector) = (0,0,0,0)
        _LensHalfSize("Lens Half Size", Vector) = (0.01,0.01,0,0)
        _Pupil("Pupil", Vector) = (0,0,0,0)
        [HDR] _ReticleColor("Reticle Color", Color) = (2,0.12,0.03,1)
        _ReticleShape("Dot, Ring Radius, Ring Width", Vector) = (0.006,0.075,0.004,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "RenderType"="HDUnlitShader" "Queue"="Geometry+10" }
        Pass
        {
            Name "DepthForwardOnly"
            Tags { "LightMode"="DepthForwardOnly" }
            Stencil
            {
                WriteMask 8
                Ref 0
                Comp Always
                Pass Replace
            }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex HdrpDepthVert
            #pragma fragment HdrpDepthFrag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _LensCenter;
                float4 _LensHalfSize;
                float4 _Pupil;
                float4 _ReticleColor;
                float4 _ReticleShape;
                float _Visibility;
            CBUFFER_END
            float4 HdrpDepthVert(float4 positionOS : POSITION) : SV_POSITION
            {
                float3 posRWS = TransformObjectToWorld(positionOS.xyz);
                return TransformWorldToHClip(posRWS);
            }
            void HdrpDepthFrag() {}
            ENDHLSL
        }
        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode"="ForwardOnly" }
            Stencil
            {
                WriteMask 6
                Ref 0
                Comp Always
                Pass Replace
            }
            ZWrite On
            ZTest LEqual
            Cull Back
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex HdrpVert
            #pragma fragment HdrpFrag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            TEXTURE2D(_ScopeTex); SAMPLER(sampler_ScopeTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _LensCenter;
                float4 _LensHalfSize;
                float4 _Pupil;
                float4 _ReticleColor;
                float4 _ReticleShape;
                float _Visibility;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings HdrpVert(Attributes input)
            {
                Varyings output;
                float3 posRWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(posRWS);
                output.uv = (input.positionOS.xy - _LensCenter.xy) /
                    (2.0 * max(_LensHalfSize.xy, float2(0.0001, 0.0001))) + 0.5;
                return output;
            }
            float4 HdrpFrag(Varyings input) : SV_Target0
            {
                float2 centered = input.uv - 0.5;
                float aspect = _LensHalfSize.x / max(_LensHalfSize.y, 0.0001);
                float r = length(centered * float2(aspect, 1.0));
                float aa = max(fwidth(r), 0.0001);
                float dotMask = 1.0 - smoothstep(_ReticleShape.x - aa, _ReticleShape.x + aa, r);
                float ring = 1.0 - smoothstep(_ReticleShape.z - aa, _ReticleShape.z + aa,
                    abs(r - _ReticleShape.y));
                ring *= step(0.001, _ReticleShape.y);
                float reticle = max(dotMask, ring);
                float outline = max(1.0 - smoothstep(_ReticleShape.x + aa, _ReticleShape.x + 3.0 * aa, r),
                    (1.0 - smoothstep(_ReticleShape.z + aa, _ReticleShape.z + 3.0 * aa,
                    abs(r - _ReticleShape.y))) * step(0.001, _ReticleShape.y));
                float shadowRadius = length(centered * 2.0 + _Pupil.xy * 0.65);
                float pupil = 1.0 - smoothstep(0.72, 1.05, shadowRadius);
                float3 scene = SAMPLE_TEXTURE2D(_ScopeTex, sampler_ScopeTex, input.uv).rgb;
                scene *= 1.0 - outline * 0.8;
                float3 reticleHdr = _ReticleColor.rgb * GetCurrentExposureMultiplier();
                scene = lerp(scene, reticleHdr, reticle);
                return float4(scene * pupil * saturate(_Visibility), 1.0);
            }
            ENDHLSL
        }
    }

}
