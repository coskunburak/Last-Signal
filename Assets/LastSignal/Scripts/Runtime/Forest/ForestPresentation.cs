using LastSignal.Atmosphere;
using LastSignal.WorldTime;
using UnityEngine;
namespace LastSignal.Forest
{
    /// <summary>One visual bridge for the resident forest, no individual tree updates or material clones.</summary>
    public sealed class ForestPresentation : MonoBehaviour
    {
        public WorldClock clock;
        public TownAtmosphere atmosphere;
        static readonly int Phase=Shader.PropertyToID("_LSForestWindPhase"),Wet=Shader.PropertyToID("_LSForestWetness");
        Vector4 originalPhase;float originalWet;bool owns;
        void LateUpdate()
        {
            if(!clock || clock.Simulation==null){Release();return;}
            if(!owns){originalPhase=Shader.GetGlobalVector(Phase);originalWet=Shader.GetGlobalFloat(Wet);owns=true;}
            // Integer cycles per authoritative day make midnight seamless. No independent wind clock or RNG.
            float day=(float)(clock.Simulation.TimeOfDay/86400),rain=atmosphere?atmosphere.RainBlend:(float)clock.Simulation.RainPresentation;
            Shader.SetGlobalVector(Phase,new Vector4(day*Mathf.PI*240,day*Mathf.PI*62,Mathf.Lerp(.07f,.12f,rain),0));
            Shader.SetGlobalFloat(Wet,atmosphere?atmosphere.SurfaceWetness:rain);
        }
        void Release(){if(!owns)return;Shader.SetGlobalVector(Phase,originalPhase);Shader.SetGlobalFloat(Wet,originalWet);owns=false;}
        void OnDisable()=>Release();
    }
}
