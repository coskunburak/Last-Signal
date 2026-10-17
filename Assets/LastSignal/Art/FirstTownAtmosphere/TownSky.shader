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
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
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
            V vert(A v){V o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.dir=v.positionOS.xyz;return o;}
            half4 frag(V i):SV_Target
            {
                float3 d=normalize(i.dir);
                float2 uv=float2(.5-atan2(d.z,d.x)/(2*PI)+_Rotation/360,1-acos(clamp(d.y,-1,1))/PI);
                float sunU=.5-atan2(_SunDirection.z,_SunDirection.x)/(2*PI);
                float2 goldenUV=float2(uv.x-sunU+.5,uv.y);
                half3 day=SAMPLE_TEXTURE2D(_DayTex,sampler_DayTex,uv).rgb*_Exposure.x;
                half3 sunset=SAMPLE_TEXTURE2D(_SunsetTex,sampler_SunsetTex,goldenUV).rgb*_Exposure.z;
                half3 night=SAMPLE_TEXTURE2D(_NightTex,sampler_NightTex,uv).rgb*_Exposure.w;
                half3 overcast=SAMPLE_TEXTURE2D(_OvercastTex,sampler_OvercastTex,uv).rgb*_Exposure.y;
                half3 sky=lerp(night,lerp(day,sunset,_Gold),_Day);
                // Use the sunless day panorama for blue hour: do not leave a photographed
                // sunset disc above the horizon after the authoritative sun has set.
                float2 sunXZ=_SunDirection.xz/max(.001,length(_SunDirection.xz));
                float2 viewXZ=d.xz/max(.001,length(d.xz));
                float horizon=saturate(1-abs(d.y)*8);
                float afterglow=pow(saturate(dot(viewXZ,sunXZ)*.5+.5),4)*horizon;
                half3 twilight=day*_TwilightTint.rgb+half3(.10,.035,.012)*afterglow;
                sky=lerp(sky,twilight,saturate(_Twilight));
                sky=lerp(sky,overcast*lerp(half3(.025,.04,.065),half3(1,1,1),_Day),smoothstep(.25,1,_Cloud)*.88);
                // Only the cloud detail drifts; stars and photographed horizons stay anchored.
                float2 cloudUV=d.xz/max(.18,d.y)*.12;
                half cloud=SAMPLE_TEXTURE2D(_CloudTex,sampler_CloudTex,cloudUV+float2(_Drift*2,_Drift*.4)).a*3;
                cloud*=SAMPLE_TEXTURE2D(_CloudTex,sampler_CloudTex,cloudUV*.63-float2(_Drift*.7,0)).a*3;
                half veil=smoothstep(.18,.6,cloud)*smoothstep(0,.25,d.y)*_Cloud*.28;
                sky=lerp(sky,lerp(half3(.03,.045,.07),half3(.55,.59,.62),_Day),veil);
                float disc=smoothstep(.99975,.99992,dot(d,normalize(_SunDirection.xyz)));
                sky+=disc*half3(6,5.1,3.8)*_Day*(1-_Gold)*(1-_Cloud)*(1-_Rain)*(1-_Twilight);
                sky*=1-_Rain*.24;
                return half4(sky,1);
            }
            ENDHLSL
        }
    }
}
