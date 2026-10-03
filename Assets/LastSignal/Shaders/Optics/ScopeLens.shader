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
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+10" }
        Pass
        {
            Name "ScopeLens"
            Tags { "LightMode"="UniversalForwardOnly" }
            ZWrite On
            ZTest LEqual
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
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
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = (input.positionOS.xy - _LensCenter.xy) /
                    (2 * max(_LensHalfSize.xy, float2(0.0001,0.0001))) + 0.5;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 centered = input.uv - 0.5;
                float aspect = _LensHalfSize.x / max(_LensHalfSize.y, 0.0001);
                float r = length(centered * float2(aspect, 1));
                float aa = max(fwidth(r), 0.0001);
                float dotMask = 1 - smoothstep(_ReticleShape.x-aa, _ReticleShape.x+aa, r);
                float ring = 1-smoothstep(_ReticleShape.z-aa, _ReticleShape.z+aa,
                    abs(r-_ReticleShape.y));
                ring *= step(0.001, _ReticleShape.y);
                float reticle = max(dotMask,ring);
                float outline = max(1-smoothstep(_ReticleShape.x+aa, _ReticleShape.x+3*aa,r),
                    (1-smoothstep(_ReticleShape.z+aa,_ReticleShape.z+3*aa,
                    abs(r-_ReticleShape.y))) * step(0.001,_ReticleShape.y));
                float shadowRadius = length(centered*2 + _Pupil.xy*0.65);
                float pupil = 1-smoothstep(0.72,1.05,shadowRadius);
                half3 scene = SAMPLE_TEXTURE2D(_ScopeTex,sampler_ScopeTex,input.uv).rgb;
                scene *= 1-outline*0.8;
                scene = lerp(scene,_ReticleColor.rgb,reticle);
                return half4(scene * pupil * saturate(_Visibility),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ZTest LEqual
            ColorMask R
            Cull Back
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _LensCenter;
                float4 _LensHalfSize;
                float4 _Pupil;
                float4 _ReticleColor;
                float4 _ReticleShape;
                float _Visibility;
            CBUFFER_END
            float4 DepthVert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }
            half DepthFrag(float4 positionCS : SV_POSITION) : SV_Target
            {
                return positionCS.z;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode"="DepthNormalsOnly" }
            ZWrite On
            ZTest LEqual
            Cull Back
            HLSLPROGRAM
            #pragma vertex NormalVert
            #pragma fragment NormalFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _LensCenter;
                float4 _LensHalfSize;
                float4 _Pupil;
                float4 _ReticleColor;
                float4 _ReticleShape;
                float _Visibility;
            CBUFFER_END
            struct NInput { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct NOutput { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };
            NOutput NormalVert(NInput input)
            {
                NOutput output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 NormalFrag(NOutput input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct = saturate(PackNormalOctQuadEncode(n)*0.5+0.5);
                    return half4(PackFloat2To888(oct),0);
                #else
                    return half4(n,0);
                #endif
            }
            ENDHLSL
        }
    }
}
