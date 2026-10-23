Shader "LastSignal/FirstTown/AtmosphereSky"
{
    Properties
    {
        _DayTex("Sunless day",2D)="white"{}
        _OvercastTex("Overcast HDR",2D)="white"{}
        _SunsetTex("Golden hour HDR",2D)="white"{}
        _NightTex("Night HDR",2D)="black"{}
        _CloudTex("Cloud detail",2D)="gray"{}
        _Day("Daylight",Range(0,1))=1
        _Gold("Golden hour",Range(0,1))=0
        _Twilight("Blue hour blend",Range(0,1))=0
        _TwilightTint("Blue hour sky transmission",Color)=(.12,.20,.32,1)
        _Cloud("Cloud cover",Range(0,1))=.25
        _Rain("Rain",Range(0,1))=0
        _Drift("Cloud drift",Float)=0
        _Rotation("Panorama alignment",Float)=0
        _Exposure("Day, overcast, sunset, night calibration",Vector)=(1,.8,.7,.18)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="HDRenderPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode"="ForwardOnly" }
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex HdrpVert
            #pragma fragment HdrpFrag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            TEXTURE2D(_DayTex); SAMPLER(sampler_DayTex);
            TEXTURE2D(_OvercastTex); SAMPLER(sampler_OvercastTex);
            TEXTURE2D(_SunsetTex); SAMPLER(sampler_SunsetTex);
            TEXTURE2D(_NightTex); SAMPLER(sampler_NightTex);
            TEXTURE2D(_CloudTex); SAMPLER(sampler_CloudTex);
            CBUFFER_START(UnityPerMaterial)
            float _Day,_Gold,_Cloud,_Rain,_Drift,_Rotation,_Twilight;
            float4 _Exposure,_SunDirection,_TwilightTint;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; };
            struct V { float4 positionCS:SV_POSITION; float3 dir:TEXCOORD0; };
            V HdrpVert(A v)
            {
                V o;
                float3 posRWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(posRWS);
                o.dir = v.positionOS.xyz;
                return o;
            }
            float4 HdrpFrag(V i) : SV_Target0
            {
                float3 d=normalize(i.dir);
                float2 uv=float2(.5-atan2(d.z,d.x)/(2*PI)+_Rotation/360,1-acos(clamp(d.y,-1,1))/PI);
                float sunU=.5-atan2(_SunDirection.z,_SunDirection.x)/(2*PI);
                float2 goldenUV=float2(uv.x-sunU+.5,uv.y);
                float3 day=SAMPLE_TEXTURE2D(_DayTex,sampler_DayTex,uv).rgb*_Exposure.x;
                float3 sunset=SAMPLE_TEXTURE2D(_SunsetTex,sampler_SunsetTex,goldenUV).rgb*_Exposure.z;
                float3 night=SAMPLE_TEXTURE2D(_NightTex,sampler_NightTex,uv).rgb*_Exposure.w;
                float3 overcast=SAMPLE_TEXTURE2D(_OvercastTex,sampler_OvercastTex,uv).rgb*_Exposure.y;
                float3 sky=lerp(night,lerp(day,sunset,_Gold),_Day);
                float2 sunXZ=_SunDirection.xz/max(.001,length(_SunDirection.xz));
                float2 viewXZ=d.xz/max(.001,length(d.xz));
                float horizon=saturate(1-abs(d.y)*8);
                float afterglow=pow(saturate(dot(viewXZ,sunXZ)*.5+.5),4)*horizon;
                float3 twilight=day*_TwilightTint.rgb+float3(.10,.035,.012)*afterglow;
                sky=lerp(sky,twilight,saturate(_Twilight));
                sky=lerp(sky,overcast*lerp(float3(.025,.04,.065),float3(1,1,1),_Day),smoothstep(.25,1,_Cloud)*.88);
                float2 cloudUV=d.xz/max(.18,d.y)*.12;
                float cloud=SAMPLE_TEXTURE2D(_CloudTex,sampler_CloudTex,cloudUV+float2(_Drift*2,_Drift*.4)).a*3;
                cloud*=SAMPLE_TEXTURE2D(_CloudTex,sampler_CloudTex,cloudUV*.63-float2(_Drift*.7,0)).a*3;
                float veil=smoothstep(.18,.6,cloud)*smoothstep(0,.25,d.y)*_Cloud*.28;
                sky=lerp(sky,lerp(float3(.03,.045,.07),float3(.55,.59,.62),_Day),veil);
                float disc=smoothstep(.99975,.99992,dot(d,normalize(_SunDirection.xyz)));
                sky+=disc*float3(6,5.1,3.8)*_Day*(1-_Gold)*(1-_Cloud)*(1-_Rain)*(1-_Twilight);
                sky*=1-_Rain*.24;
                return float4(sky * GetCurrentExposureMultiplier(), 1.0);
            }
            ENDHLSL
        }
    }

}
