#ifndef LS_FOREST_INPUT
#define LS_FOREST_INPUT
#define InitializeStandardLitSurfaceData ForestDrySurface
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#undef InitializeStandardLitSurfaceData
float _LSForestWetness;
void InitializeStandardLitSurfaceData(float2 uv,out SurfaceData surfaceData)
{
    ForestDrySurface(uv,surfaceData);
    surfaceData.albedo*=1-saturate(_LSForestWetness)*.14;
    surfaceData.smoothness=lerp(surfaceData.smoothness,max(surfaceData.smoothness,.32),saturate(_LSForestWetness));
}
#endif
