using UnityEngine;
using LastSignal.WorldTime;
namespace LastSignal.Audio
{
    /// <summary>Scene-owned presentation only. Fixed voice budget; no access to gameplay noise emission.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(SessionFlow))]
    public sealed class ProductionAudio : MonoBehaviour
    {
        public const int VoiceBudget=16;
        [SerializeField] AudioCatalog catalog;
        [SerializeField] Vector3 cabinCenter=new Vector3(-360,1.5f,-140), cabinSize=new Vector3(6,3,6);
        [SerializeField] AudioSurface[] groundAreas=System.Array.Empty<AudioSurface>();
        public void ConfigureGroundAreas(AudioSurface[] areas) => groundAreas=areas;
        readonly AudioSource[] sources=new AudioSource[VoiceBudget];
        readonly AudioLowPassFilter[] filters=new AudioLowPassFilter[VoiceBudget];
        readonly Transform[] owners=new Transform[VoiceBudget];
        readonly AudioCue[] playing=new AudioCue[VoiceBudget];
        readonly float[] gains=new float[VoiceBudget], cutoff=new float[VoiceBudget];
        readonly int[] previous=new int[(int)AudioCue.Relief+1];
        readonly RaycastHit[] hits=new RaycastHit[16];
        readonly AudioSource[] loops=new AudioSource[8];
        static readonly AudioCue[] LoopCues={AudioCue.Wind,AudioCue.Rain,AudioCue.HeavyRain,AudioCue.Night,AudioCue.Interior,AudioCue.Exploration,AudioCue.Threat,AudioCue.Relief};
        readonly System.Random random=new System.Random(15015);
        readonly AudioTension tension=new AudioTension();
        SessionFlow flow;
        WorldClock clock;
        WorldSimulation simulation;
        Transform listener;
        InteractionController interaction;
        float nextOcclusion, duckUntil, elapsed;
        int occlusionCursor;
        bool initialized;
        public AudioPreferences Preferences { get; } = new AudioPreferences();
        public AudioCatalog Catalog => catalog;
        public AudioCue LastCue { get; private set; }
        public int CueCount { get; private set; }
        public AudioCue MusicState => tension.State;
        public string Caption { get; private set; } = "";
        float captionUntil;
        public bool Active => initialized && isActiveAndEnabled && flow && flow.Player && !flow.Restoring && !flow.PlayerDead && !flow.Paused && flow.isActiveAndEnabled;
        public void Configure(AudioCatalog value) => catalog=value;
        void Awake()
        {
            flow=GetComponent<SessionFlow>(); clock=GetComponent<WorldClock>();
            Preferences.Load();
            for(int i=0;i<previous.Length;i++) previous[i]=-1;
            if(!catalog) { Debug.LogError("S015 audio catalog missing.",this); enabled=false; return; }
            for(int i=0;i<VoiceBudget;i++)
            {
                sources[i]=CreateSource("Cue "+i);
                filters[i]=sources[i].gameObject.AddComponent<AudioLowPassFilter>(); cutoff[i]=22000;
            }
            for(int i=0;i<loops.Length;i++)
            {
                loops[i]=CreateSource(LoopCues[i].ToString());
                var cue=catalog.Get(LoopCues[i]);
                if(cue==null || cue.clips.Length==0) continue;
                loops[i].clip=cue.clips[0]; loops[i].loop=true; loops[i].spatialBlend=0;
                loops[i].outputAudioMixerGroup=catalog.Group(cue.bus); loops[i].priority=cue.priority; loops[i].volume=0;
            }
            initialized=true;
        }
        AudioSource CreateSource(string label)
        {
            var go=new GameObject("Audio "+label); go.transform.SetParent(transform,false);
            var source=go.AddComponent<AudioSource>(); source.playOnAwake=false; source.dopplerLevel=0;
            return source;
        }
        void OnEnable() { if(initialized && flow && flow.Player) BeginSession(); }
        void Start() => ApplyPreferences();
        public void ApplyPreferences() { if(catalog && !Preferences.Apply(catalog.mixer)) Debug.LogWarning("S015 mixer parameter mapping incomplete.",this); }
        public void BeginSession()
        {
            EndSession(); if(!initialized || !flow.Player) return;
            var camera=flow.Player.GetComponentInChildren<Camera>(); listener=camera?camera.transform:flow.Player.transform;
            interaction=flow.Player.GetComponent<InteractionController>();
            if(interaction) interaction.InteractionSucceeded+=OnInteraction;
            simulation=clock?clock.Simulation:null;
            var vehicle=flow.GetComponent<Vehicles.VehicleWorld>()?.Actor;
            if(vehicle) foreach(var source in vehicle.GetComponentsInChildren<AudioSource>(true)) source.outputAudioMixerGroup=catalog.effects;
        }
        public void EndSession()
        {
            if(interaction) interaction.InteractionSucceeded-=OnInteraction;
            interaction=null; listener=null; simulation=null; elapsed=0; tension.Reset(); Caption=""; duckUntil=0;
            for(int i=0;i<sources.Length;i++) { if(sources[i]) sources[i].Stop(); owners[i]=null; }
            for(int i=0;i<loops.Length;i++) if(loops[i]) loops[i].Stop();
        }
        void OnDisable() => EndSession();
        void OnInteraction() => Play(AudioCue.Confirm,flow.Player.transform,.5f);
        public void Threat(Transform actor)
        { if(Active && listener && actor && (actor.position-listener.position).sqrMagnitude<24*24) tension.Signal(elapsed); }
        public void StopOwner(Transform actor)
        { for(int i=0;i<sources.Length;i++) if(owners[i]==actor) { if(sources[i]) sources[i].Stop(); owners[i]=null; } }
        public static string Direction(Vector3 local)
        { return Mathf.Abs(local.x)>Mathf.Abs(local.z) ? (local.x<0?"Sol":"Sağ") : (local.z<0?"Arka":"Ön"); }
        public bool Play(AudioCue id, Transform owner, float scale=1)
        {
            if(!Active || !listener || !owner) return false;
            var cue=catalog.Get(id);
            if(cue==null || cue.clips==null || cue.clips.Length==0) return false;
            float distance=(owner.position-listener.position).sqrMagnitude;
            if(cue.spatial && distance>cue.range*cue.range) return false;
            bool critical=id==AudioCue.ZombieTelegraph;
            // Captions share the audible cue's authored range, independent of device volume and voice stealing.
            if(critical || id==AudioCue.ZombieAlert)
            {
                Caption=(critical?"Saldırı hazırlığı":"Yakın hırıltı")+" · "+Direction(listener.InverseTransformDirection(owner.position-listener.position));
                captionUntil=elapsed+1.4f;
                if(critical) duckUntil=elapsed+.9f;
            }
            int slot=-1, activeCategory=0;
            int start=critical?0:4;
            for(int i=0;i<VoiceBudget;i++) if(sources[i].isPlaying && playing[i]==id) activeCategory++;
            if(!critical && activeCategory>=(id==AudioCue.ZombieIdle?2:4))
            {
                if(id!=AudioCue.Fire) return false;
                // Sustained fire replaces the oldest tail instead of dropping accepted shots.
                for(int i=start;i<VoiceBudget;i++) if(sources[i].isPlaying && playing[i]==id && (slot<0 || sources[i].time>sources[slot].time)) slot=i;
            }
            if(slot<0) for(int i=start;i<VoiceBudget;i++) if(!sources[i].isPlaying) { slot=i; break; }
            if(slot<0)
            {
                int least=-1;
                for(int i=start;i<VoiceBudget;i++) if((critical || sources[i].priority>cue.priority) && (least<0 || sources[i].priority>sources[least].priority)) least=i;
                slot=least;
            }
            if(slot<0) return false;
            int index=AudioCatalog.Variation(cue.clips.Length,previous[(int)id],random.Next());
            var clip=cue.clips[index]; if(!clip) return false;
            previous[(int)id]=index;
            var source=sources[slot]; source.Stop(); owners[slot]=owner; playing[slot]=id;
            source.transform.position=owner.position+Vector3.up*.8f;
            source.outputAudioMixerGroup=catalog.Group(cue.bus); source.clip=clip; source.loop=false;
            source.spatialBlend=cue.spatial?1:0; source.rolloffMode=AudioRolloffMode.Linear; source.minDistance=1; source.maxDistance=cue.range;
            source.priority=cue.priority; source.pitch=1+((float)random.NextDouble()*2-1)*cue.pitchVariation;
            gains[slot]=Mathf.Clamp01(cue.gain*scale*(.93f+(float)random.NextDouble()*.07f)); source.volume=gains[slot];
            cutoff[slot]=22000; filters[slot].cutoffFrequency=22000;
            source.Play(); LastCue=id; CueCount++; return true;
        }
        public void Step(Transform actor,float scale,bool zombie)
        {
            if(!Active || !listener || (actor.position-listener.position).sqrMagnitude>(zombie?14*14:3*3)) return;
            int count=Physics.RaycastNonAlloc(actor.position+Vector3.up*.35f,Vector3.down,hits,1.2f,~((1<<8)|(1<<2)),QueryTriggerInteraction.Ignore);
            Collider surface=null; float nearest=float.MaxValue;
            if(count<hits.Length) for(int i=0;i<count;i++) if(!hits[i].transform.IsChildOf(actor) && (hits[i].distance<nearest-.01f || Mathf.Abs(hits[i].distance-nearest)<=.01f && AudioSurface.Resolve(hits[i].collider)==AudioSurfaceKind.Wood)) { nearest=hits[i].distance; surface=hits[i].collider; }
            var kind=AudioSurface.Resolve(surface);
            if(kind==AudioSurfaceKind.Default || kind==AudioSurfaceKind.Grass)
                for(int i=0;i<groundAreas.Length;i++) if(groundAreas[i] && groundAreas[i].ContainsArea(actor.position)) { kind=groundAreas[i].surface; break; }
            var cue=AudioSurface.Cue(kind);
            if(catalog.Get(cue)==null) cue=AudioCue.StepDefault;
            Play(cue,actor,scale*(zombie?.7f:1));
        }
        public static float ShelterBlend(Vector3 position, Vector3 center, Vector3 size)
        {
            var d=position-center; var half=size*.5f;
            return Mathf.Clamp01(Mathf.Min(half.x-Mathf.Abs(d.x),half.y-Mathf.Abs(d.y),half.z-Mathf.Abs(d.z))+.5f);
        }
        void LateUpdate()
        {
            if(!initialized) return;
            if(clock && simulation!=clock.Simulation) { BeginSession(); }
            if(!Active || !listener)
            {
                for(int i=0;i<loops.Length;i++) if(loops[i].isPlaying) loops[i].Pause();
                for(int i=0;i<sources.Length;i++) if(sources[i].isPlaying) { sources[i].Stop(); owners[i]=null; }
                Caption=""; return;
            }
            float dt=Mathf.Min(Time.deltaTime,.1f); elapsed+=dt; tension.Tick(elapsed);
            if(elapsed>captionUntil) Caption="";
            float indoor=ShelterBlend(listener.position,cabinCenter,cabinSize);
            float rain=clock && clock.Simulation!=null?(float)clock.Simulation.RainPresentation:0;
            float hour=clock && clock.Simulation!=null?(float)(clock.Simulation.TimeOfDay/3600):12;
            float night=1-Mathf.Clamp01(Mathf.Min(hour-5,20-hour));
            float duck=elapsed<duckUntil?.55f:1;
            for(int i=0;i<loops.Length;i++)
            {
                var source=loops[i]; if(!source.clip) continue;
                float weight=i==0?(1-indoor*.7f):i==1?rain*(1-rain*.6f)*(1-indoor*.8f):i==2?rain*rain*(1-indoor*.88f):i==3?night*(1-indoor*.8f):i==4?indoor:LoopCues[i]==tension.State?1:0;
                float target=catalog.Get(LoopCues[i]).gain*weight*(i<5?duck:1);
                source.volume=Mathf.MoveTowards(source.volume,target,dt*.18f);
                if(source.volume>.001f) { source.UnPause(); if(!source.isPlaying) source.Play(); }
                else if(source.isPlaying) source.Stop();
            }
            // At most four geometry rays each 100ms, never per-source per-frame.
            if(elapsed>=nextOcclusion)
            {
                nextOcclusion=elapsed+.1f;
                for(int n=0;n<4;n++)
                {
                    int i=occlusionCursor++%VoiceBudget;
                    if(!sources[i].isPlaying || !owners[i] || sources[i].spatialBlend==0) continue;
                    Vector3 delta=owners[i].position+Vector3.up*.8f-listener.position;
                    bool blocked=Physics.Raycast(listener.position,delta.normalized,delta.magnitude,~((1<<8)|(1<<2)),QueryTriggerInteraction.Ignore);
                    cutoff[i]=blocked?3200:22000;
                }
            }
            for(int i=0;i<sources.Length;i++)
            {
                if(!sources[i].isPlaying) continue;
                if(!owners[i] || !owners[i].gameObject.activeInHierarchy) { sources[i].Stop(); continue; }
                sources[i].transform.position=owners[i].position+Vector3.up*.8f;
                filters[i].cutoffFrequency=Mathf.Lerp(filters[i].cutoffFrequency,cutoff[i],1-Mathf.Exp(-dt*8));
                sources[i].volume=Mathf.Lerp(sources[i].volume,gains[i]*(cutoff[i]<10000?.65f:1)*(playing[i]==AudioCue.Fire && elapsed<duckUntil?.7f:1),1-Mathf.Exp(-dt*8));
            }
        }
    }
}
