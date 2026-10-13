Shader "LastSignal/LocalizedWound"
{
    Properties
    {
        _BaseMap("Blood breakup", 2D) = "white" {}
        _BaseColor("Fresh blood", Color) = (.23,.027,.023,.95)
        _Smoothness("Wetness", Range(0,1)) = .42
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            Offset -1, -1
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
            float4 _WoundCenters[8], _WoundNormals[8], _WoundDetails[8];
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float3 rest:TEXCOORD2; float3 restNormal:TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float3 rest:TEXCOORD2; float3 restNormal:TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID };
            Varyings Vert(Attributes v)
            {
                Varyings o; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v,o);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.rest = v.rest; o.restNormal = v.restNormal; return o;
            }
            half3 Shade(Light light, half3 n, half3 view, half3 albedo, half wet)
            {
                half diffuse = saturate(dot(n,light.direction));
                half spec = pow(saturate(dot(n,normalize(light.direction+view))),lerp(12,72,wet)) * .13 * wet;
                return (albedo*diffuse+spec) * light.color * light.distanceAttenuation * light.shadowAttenuation;
            }
            half4 Frag(Varyings i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float alpha=0, cavity=0, rim=0, age=1, clothing=0, height=0;
                for (int w = 0; w < 8; w++)
                {
                    float radius = _WoundCenters[w].w;
                    if (radius <= 0) continue;
                    float3 normal = _WoundNormals[w].xyz;
                    float3 delta = i.rest - _WoundCenters[w].xyz;
                    float depth = dot(delta, normal);
                    float3 tangent = normalize(cross(abs(normal.y) > .9 ? float3(1,0,0) : float3(0,1,0), normal));
                    float3 bitangent = cross(normal, tangent);
                    float2 uv = float2(dot(delta,tangent),dot(delta,bitangent)) / radius;
                    float angle = _WoundNormals[w].w;
                    uv = float2(uv.x*cos(angle)-uv.y*sin(angle),uv.x*sin(angle)+uv.y*cos(angle));
                    if (abs(depth) > radius*.65 || dot(normalize(i.restNormal),normal) < .25) continue;
                    float phi=atan2(uv.y,uv.x);
                    float irregular=1 + .12*sin(phi*7+angle) + .08*sin(phi*13-angle);
                    float r=length(uv*float2(1,1.12))/irregular;
                    float breakup=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv*.45+.5).a;
                    float stain=(1-smoothstep(.62,1,r)) * lerp(.64,1,breakup);
                    float core=(1-smoothstep(.13,.33+_WoundDetails[w].x*.1,r));
                    float edge=(1-smoothstep(.05,.14,abs(r-.39)));
                    float coverage=max(stain*.82,core);
                    if (coverage>alpha)
                    {
                        alpha=coverage; cavity=core; rim=edge; age=_WoundDetails[w].y; clothing=_WoundDetails[w].z;
                        height=-core*.0018 + edge*.0007;
                    }
                }
                clip(alpha-.025);
                half3 n=normalize(i.normalWS), view=GetWorldSpaceNormalizeViewDir(i.positionWS);
                // Surface-gradient bump supplies recessed normals on triangles too coarse for small physical dents.
                float3 dpdx=ddx(i.positionWS), dpdy=ddy(i.positionWS);
                float3 r1=cross(dpdy,n), r2=cross(n,dpdx);
                float determinant=dot(dpdx,r1);
                float3 gradient=(r1*ddx(height)+r2*ddy(height))/max(abs(determinant),.00000001)*sign(determinant);
                n=normalize(n-gradient);
                half3 blood=lerp(_BaseColor.rgb,half3(.085,.012,.009),age);
                half3 tissue=lerp(blood,half3(.31,.085,.06),rim*.5*(1-clothing));
                float weave=sin(i.rest.x*2400)*sin(i.rest.y*2400)*.05;
                tissue=lerp(tissue,tissue*.66+weave*.1,clothing);
                half3 albedo=lerp(tissue,half3(.015,.003,.002),cavity*.95);
                half wet=_Smoothness*(1-age*.7)*(1-clothing*.55)*(1-cavity*.8);
                half3 color=SampleSH(n)*albedo*(1-cavity*.65);
                color+=Shade(GetMainLight(TransformWorldToShadowCoord(i.positionWS)),n,view,albedo,wet);
                #ifdef _ADDITIONAL_LIGHTS
                uint count=GetAdditionalLightsCount();
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                #if USE_CLUSTER_LIGHT_LOOP
                for(uint lightIndex=0;lightIndex<min(URP_FP_DIRECTIONAL_LIGHTS_COUNT,MAX_VISIBLE_LIGHTS);lightIndex++)
                    color+=Shade(GetAdditionalLight(lightIndex,i.positionWS),n,view,albedo,wet);
                #endif
                LIGHT_LOOP_BEGIN(count)
                    color+=Shade(GetAdditionalLight(lightIndex,i.positionWS),n,view,albedo,wet);
                LIGHT_LOOP_END
                #endif
                return half4(color,saturate(alpha*_BaseColor.a));
            }
            ENDHLSL
        }
    }
}
