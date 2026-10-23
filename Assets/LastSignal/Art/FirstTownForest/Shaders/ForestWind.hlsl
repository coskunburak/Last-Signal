#ifndef LS_FOREST_WIND
#define LS_FOREST_WIND
float4 _LSForestWindPhase;
float3 ForestWind(float3 p)
{
    #if defined(_LS_FOREST_STATIC)
    return p;
    #endif
    float3 origin=GetAbsolutePositionWS(TransformObjectToWorld(float3(0,0,0)));
    float anchor=saturate(max(0,p.y-.15)*.18);
    float wave=sin(_LSForestWindPhase.x+origin.x*.11+origin.z*.17);
    float gust=.7+.3*sin(_LSForestWindPhase.y+origin.x*.019);
    p.xz+=float2(1,.35)*wave*gust*anchor*_LSForestWindPhase.z;
    return p;
}
#endif
