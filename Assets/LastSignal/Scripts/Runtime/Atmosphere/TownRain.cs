using UnityEngine;
namespace LastSignal.Atmosphere
{
    /// <summary>Bounded local rain, world-space particles and roof-aware surface impacts. No gameplay weather writes.</summary>
    public sealed class TownRain : MonoBehaviour
    {
        public ParticleSystem near, splash;
        public ParticleSystem[] middle = System.Array.Empty<ParticleSystem>();
        readonly System.Random random=new System.Random(8137);
        float poll;
        bool covered;
        const int EnvironmentMask=~((1<<8)|(1<<2));
        public void Present(Vector3 player,float intensity,bool protectedFromRain,float dt,bool snap)
        {
            if(!near || !splash)return;
            if(protectedFromRain && !covered)near.Clear();
            covered=protectedFromRain;
            near.transform.position=player+Vector3.up*9;
            Rate(near,covered?0:intensity*340);
            if(!splash.isPlaying)splash.Play();
            poll-=dt;
            if(poll>0 && !snap)return;
            poll=.2f;
            int index=0;
            for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
            {
                if(x==0 && z==0)continue;
                if(index>=middle.Length)break;
                var ps=middle[index++]; if(!ps)continue;
                var p=player+new Vector3(x*17,0,z*17);
                // Spawn over the highest surface, never below a roof. Particle collision kills on the actual footprint.
                if(Physics.Raycast(p+Vector3.up*32,Vector3.down,out var hit,64,EnvironmentMask,QueryTriggerInteraction.Ignore))p.y=hit.point.y;
                ps.transform.position=p+Vector3.up*10; Rate(ps,intensity*100);
            }
            if(intensity<.05f)return;
            for(int i=0;i<4;i++)
            {
                var p=player+new Vector3((float)(random.NextDouble()-.5)*26,35,(float)(random.NextDouble()-.5)*26);
                if(!Physics.Raycast(p,Vector3.down,out var hit,65,EnvironmentMask,QueryTriggerInteraction.Ignore) || hit.normal.y<.75f)continue;
                // Top-down impacts include roofs, but never room floors hidden beneath them.
                var emit=new ParticleSystem.EmitParams { position=hit.point+hit.normal*.035f,
                    startSize=.12f+(float)random.NextDouble()*.2f, startLifetime=.25f+(float)random.NextDouble()*.15f,
                    startColor=new Color(.7f,.8f,.84f,intensity*.35f), velocity=Vector3.zero };
                splash.Emit(emit,1);
            }
        }
        static void Rate(ParticleSystem ps,float rate)
        {
            var emission=ps.emission; emission.rateOverTime=rate;
            if(rate>.01f && !ps.isPlaying)ps.Play();
            else if(rate<=.01f && ps.isEmitting)ps.Stop(false,ParticleSystemStopBehavior.StopEmitting);
        }
        public void Stop()
        {
            if(near)near.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            if(splash)splash.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach(var ps in middle)if(ps)ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            poll=0; covered=false;
        }
        void OnDisable()=>Stop();
    }
}
