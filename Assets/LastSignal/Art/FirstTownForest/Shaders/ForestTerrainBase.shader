// Adapted from Unity URP 17.5. See UNITY_LICENSE.md.
Shader "Hidden/LastSignal/Forest/Terrain Base"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _MainTex("Albedo(RGB), Smoothness(A)", 2D) = "white" {}
        _MetallicTex ("Metallic (R)", 2D) = "black" {}
        [HideInInspector] _TerrainHolesTexture("Holes Map (RGB)", 2D) = "white" {}
    }

    HLSLINCLUDE

    #pragma multi_compile_fragment __ _ALPHATEST_ON

    ENDHLSL

    SubShader
    {
        Tags { "Queue" = "Geometry-100" "RenderType" = "Opaque" "RenderPipeline" = "HDRenderPipeline" "IgnoreProjector" = "True" }
        LOD 200

        Pass
        {
            Name "DepthForwardOnly"
            Tags { "LightMode" = "DepthForwardOnly" }
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

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex HdrpTerrainVert
            #pragma fragment HdrpTerrainDepthFrag
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #define TERRAIN_SPLAT_BASEPASS 1
            #include "ForestTerrainHdrpPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex HdrpTerrainVert
            #pragma fragment HdrpTerrainDepthFrag
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #define TERRAIN_SPLAT_BASEPASS 1
            #include "ForestTerrainHdrpPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }
            Stencil
            {
                WriteMask 6
                Ref 0
                Comp Always
                Pass Replace
            }
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex HdrpTerrainVert
            #pragma fragment HdrpTerrainForwardFrag
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _TERRAIN_INSTANCED_PERPIXEL_NORMAL
            #define TERRAIN_SPLAT_BASEPASS 1
            #include "ForestTerrainHdrpPasses.hlsl"
            ENDHLSL
        }
    }


    FallBack "Hidden/HDRP/FallbackError"
    //CustomEditor "LitShaderGUI"
}
