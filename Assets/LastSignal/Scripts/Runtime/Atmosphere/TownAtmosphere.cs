using System;
using System.Collections.Generic;
using LastSignal.Rendering;
using LastSignal.WorldTime;
using LastSignal.Vehicles;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastSignal.Atmosphere
{
    /// <summary>Session-owned presentation. Never ticks, changes or persists simulation state.</summary>
    [DisallowMultipleComponent]
    public sealed class TownAtmosphere : MonoBehaviour
    {
        [Serializable] public struct Surface
        {
            public Material material;
            public bool wetOnly;
            [Range(0,1)] public float darkening, smoothness;
        }
        [Serializable] public struct Lamp
        {
            public Light light;
            public float intensity;
            public bool interior, shadows;
        }
        public Material sky;
        public TownLightingProfile lightingProfile;
        public Light sun, moon;
        public Volume volume;
        public TownRain rain;
        public ReflectionProbe exteriorReflection;
        float reflectionCooldown;
        int reflectionRender=-1;
        public Surface[] surfaces = Array.Empty<Surface>();
        public Renderer[] wetRenderers = Array.Empty<Renderer>();
        public Lamp[] lamps = Array.Empty<Lamp>();
        [Range(4,24)] public int localLightBudget = 12;
        readonly Dictionary<Material,Material> replacements = new Dictionary<Material,Material>();
        readonly Dictionary<Renderer,bool> wetOnlyRenderers = new Dictionary<Renderer,bool>();
        readonly Dictionary<Renderer,Material[]> originals = new Dictionary<Renderer,Material[]>();
        readonly List<Material> wet = new List<Material>();
        readonly List<Color> dryColors = new List<Color>();
        readonly List<float> drySmoothness = new List<float>();
        readonly List<Surface> responses = new List<Surface>();
        WorldClock clock;
        WorldSimulation observedSimulation;
        double startingSeconds;
        VehicleWorld vehicleWorld;
        VehicleActor vehicle;
        Material liveSky, oldSky;
        VolumeProfile liveVolume, oldVolume;
        Color oldFogColor, oldAmbient, oldSunColor;
        SphericalHarmonicsL2 oldProbe;
        AmbientMode oldAmbientMode;
        FogMode oldFogMode;
        float oldFogDensity, oldAmbientIntensity, oldSunIntensity, oldReflection, elapsed, rainBlend, wetBlend;
        bool oldFog, bound;
        Quaternion oldSunRotation;
        float[] originalLightIntensities, distances, lampScores;
        Color[] originalLightColors;
        bool[] selectedLampShadows;
        FixtureEmission[] fixtures;
        float oldSunShadowStrength, oldMoonIntensity;
        Quaternion oldMoonRotation;
        bool PhaseLighting => lightingProfile && lightingProfile.usePhaseLighting;
        bool[] originalLightEnabled;
        LightShadows[] originalShadows;
        RenderPipelineAdapter.CameraPostState cameraState;
        public bool IsBound => bound;
        public float RainBlend => rainBlend;
        public float SurfaceWetness => wetBlend;

        public void Bind(WorldClock source)
        {
            Unbind(); clock=source;
            if (!clock || !sky || !sun) return;
            bound=true; oldSky=RenderSettings.skybox; oldFog=RenderSettings.fog;
            oldFogMode=RenderSettings.fogMode; oldFogDensity=RenderSettings.fogDensity; oldFogColor=RenderSettings.fogColor;
            oldAmbient=RenderSettings.ambientLight; oldAmbientMode=RenderSettings.ambientMode;
            oldAmbientIntensity=RenderSettings.ambientIntensity; oldProbe=RenderSettings.ambientProbe;
            oldReflection=RenderSettings.reflectionIntensity;
            oldSunIntensity=sun.intensity; oldSunRotation=sun.transform.rotation; oldSunColor=sun.color;
            oldSunShadowStrength=sun.shadowStrength;
            if(moon){oldMoonIntensity=moon.intensity;oldMoonRotation=moon.transform.rotation;}
            liveSky=new Material(sky) { name="FirstTown sky (session)" }; RenderSettings.skybox=liveSky;
            if (volume && volume.sharedProfile)
            {
                oldVolume=volume.sharedProfile;
                liveVolume=ScriptableObject.CreateInstance<VolumeProfile>();
                foreach(var c in oldVolume.components) if(c) liveVolume.components.Add(Instantiate(c));
                RenderPipelineAdapter.EnsureAtmosphereVolumeComponents(liveVolume, lightingProfile && lightingProfile.usePhysicalHdrpLighting);
                volume.sharedProfile=liveVolume;
            }
            foreach(var s in surfaces) if(s.material && !replacements.ContainsKey(s.material)) AddMaterial(s.material,s);
            foreach(var r in wetRenderers) Replace(r);
            originalLightIntensities=new float[lamps.Length]; originalLightEnabled=new bool[lamps.Length];
            originalShadows=new LightShadows[lamps.Length]; distances=new float[lamps.Length]; originalLightColors=new Color[lamps.Length];
            for(int i=0;i<lamps.Length;i++) if(lamps[i].light)
            { originalLightIntensities[i]=lamps[i].light.intensity; originalLightEnabled[i]=lamps[i].light.enabled; originalShadows[i]=lamps[i].light.shadows; originalLightColors[i]=lamps[i].light.color; }
            vehicleWorld=clock.Flow.GetComponent<VehicleWorld>();
            Present(0,true);
        }
        void AddMaterial(Material material, Surface response)
        {
            var clone=new Material(material) { name=material.name+" (weather session)" };
            replacements.Add(material,clone); wet.Add(clone); responses.Add(response);
            dryColors.Add(RenderPipelineAdapter.GetMaterialBaseColor(clone)); drySmoothness.Add(RenderPipelineAdapter.GetMaterialSmoothness(clone));
        }
        void Replace(Renderer r)
        {
            if(!r || originals.ContainsKey(r))return;
            var a=r.sharedMaterials; var changed=(Material[])a.Clone(); bool any=false;
            for(int i=0;i<a.Length;i++) if(a[i] && replacements.TryGetValue(a[i],out var m)){ changed[i]=m; any=true; }
            if(any){ originals.Add(r,a); r.sharedMaterials=changed;
                foreach(var surface in surfaces) if(surface.wetOnly && Array.IndexOf(a,surface.material)>=0){ wetOnlyRenderers[r]=r.enabled; break; }
            }
        }
        public void Present(float deltaTime, bool snap=false)
        {
            if(!bound || !clock || clock.Simulation==null)return;
            var s=clock.Simulation;
            if(observedSimulation!=s){ observedSimulation=s; startingSeconds=s.Capture().settings.startingSeconds; snap=true; }
            if(!snap && (clock.Flow.Paused || clock.Flow.Restoring || clock.Flow.PlayerDead)) return;
            float dt=Mathf.Min(deltaTime,.1f), day=(float)(s.TimeOfDay/86400);
            float altitude=Mathf.Sin((day-.25f)*Mathf.PI*2);
            float daylight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.08f,.28f,altitude));
            float gold=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.02f,.45f,Mathf.Abs(altitude))))*daylight;
            float targetRain=(float)s.RainPresentation;
            rainBlend=snap?targetRain:Mathf.Lerp(rainBlend,targetRain,1-Mathf.Exp(-dt/2.5f));
            // A deterministic visual reconstruction from the authoritative interval, independent of player wetness.
            double age=s.Seconds-s.WeatherStarted;
            float targetWet=s.Raining ? Mathf.Clamp01((float)age/900) :
                s.WeatherStarted<=0 || s.WeatherStarted==startingSeconds ? 0 : Mathf.Clamp01(1-(float)age/2400);
            wetBlend=snap?targetWet:Mathf.Lerp(wetBlend,targetWet,1-Mathf.Exp(-dt/2));
            // Presentation-only cloud swell; no weather enum, random draws or save fields.
            float clouds=Mathf.Lerp(.12f,.52f,.5f+.5f*Mathf.Sin((float)(s.Seconds%21600)/21600*Mathf.PI*2));
            float approach=1-Mathf.Clamp01((float)(s.NextWeather-s.Seconds)/600);
            clouds=Mathf.Clamp01(Mathf.Max(clouds,approach*.78f)+rainBlend*.75f);
            ApplyLighting(day,altitude,daylight,gold,clouds,rainBlend);
            SampleSky(liveSky,day,rainBlend,clouds,(float)(s.Seconds%864000)/86400);
            SamplePostProcessing(liveVolume,day,rainBlend,clouds);
            reflectionCooldown-=dt;
            if(clock.Flow.Player)
            {
                var p=clock.Flow.Player.transform.position;
                if(exteriorReflection && reflectionCooldown<=0 && !clock.ProtectedFromRain && (reflectionRender<0 || exteriorReflection.IsFinishedRendering(reflectionRender)))
                {
                    exteriorReflection.transform.position=p+Vector3.up*2;
                    reflectionRender=exteriorReflection.RenderProbe(); reflectionCooldown=30;
                }
                if(rain) rain.Present(p,rainBlend,clock.ProtectedFromRain,dt,snap);
                elapsed-=dt;
                if(elapsed<=0 || snap)
                {
                    elapsed=.2f; ApplyMaterials(); SamplePracticalLights(p,day,clouds,rainBlend);
                    BindVehicle(); BindCamera();
                }
            }
        }
        // Explicit sample used by the owner-operated Editor comparison fixture; no simulation mutation.
        public void SampleLighting(float day, float rainAmount, float clouds)
        {
            float altitude=Mathf.Sin((day-.25f)*Mathf.PI*2);
            float daylight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.08f,.28f,altitude));
            float gold=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.02f,.45f,Mathf.Abs(altitude))))*daylight;
            ApplyLighting(day,altitude,daylight,gold,clouds,Mathf.Clamp01(rainAmount));
        }
        void ApplyLighting(float day,float altitude,float daylight,float gold,float clouds,float rainAmount)
        {
            sun.transform.rotation=Quaternion.Euler(day*360-90,-30,0);
            if(PhaseLighting)
            {
                var p=lightingProfile.Evaluate(day,clouds,rainAmount);
                sun.color=p.keyColor;sun.intensity=p.keyIntensity;sun.shadowStrength=p.shadowStrength;
                if(moon){moon.transform.rotation=Quaternion.Euler(day*360+90,-30,0);moon.intensity=p.moonIntensity;}
                RenderSettings.ambientMode=AmbientMode.Custom;
                var sh=new SphericalHarmonicsL2();sh.AddAmbientLight(p.ambient);sh.AddDirectionalLight(Vector3.up,p.skyFill,p.fillStrength);
                RenderSettings.ambientProbe=sh;RenderSettings.ambientLight=p.ambient;RenderSettings.ambientIntensity=1;
                RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
                RenderSettings.fogDensity=p.fogDensity;RenderSettings.fogColor=p.fog;
                RenderSettings.reflectionIntensity=p.reflection;
                RenderPipelineAdapter.ApplyAtmosphereLightingAndVolume(liveVolume,sun,moon,day,altitude,daylight,gold,clouds,rainAmount,RenderSettings.fogDensity,RenderSettings.fogColor,RenderSettings.ambientLight,
                lightingProfile && lightingProfile.usePhysicalHdrpLighting ? lightingProfile.hdrpSunLuxPerUnit : 0,
                lightingProfile ? lightingProfile.hdrpMoonLuxPerUnit : .5f);
                return;
            }
            sun.color=Color.Lerp(new Color(1,.97f,.89f),new Color(1,.64f,.36f),gold);
            sun.intensity=Mathf.Max(0,altitude)*(lightingProfile?lightingProfile.sunIntensity:1.65f)*Mathf.Lerp(1,.22f,clouds)*Mathf.Lerp(1,.48f,rainAmount);
            if(moon){moon.transform.rotation=Quaternion.Euler(day*360+90,-30,0); moon.intensity=(1-daylight)*.16f*Mathf.Lerp(1,.35f,clouds);}
            RenderSettings.ambientMode=AmbientMode.Custom;
            var ambient=Color.Lerp(lightingProfile?lightingProfile.nightAmbient:new Color(.055f,.073f,.105f),lightingProfile?lightingProfile.dayAmbient:new Color(.34f,.39f,.44f),daylight)*Mathf.Lerp(1,.73f,rainAmount);
            var probe=new SphericalHarmonicsL2(); probe.AddAmbientLight(ambient);
            probe.AddDirectionalLight(Vector3.up,Color.Lerp(lightingProfile?lightingProfile.nightSkyFill:new Color(.10f,.13f,.21f),lightingProfile?lightingProfile.daySkyFill:new Color(.40f,.48f,.58f),daylight),lightingProfile?lightingProfile.skyFillIntensity:.3f);
            RenderSettings.ambientProbe=probe; RenderSettings.ambientLight=ambient; RenderSettings.ambientIntensity=1;
            RenderSettings.fog=true; RenderSettings.fogMode=FogMode.ExponentialSquared;
            float dawn=1-Mathf.Clamp01(Mathf.Abs(day-.28f)/.09f);
            RenderSettings.fogDensity=.0014f+dawn*.0022f+rainAmount*.0025f+(1-daylight)*.0007f;
            RenderSettings.fogColor=Color.Lerp(new Color(.035f,.052f,.077f),Color.Lerp(new Color(.51f,.60f,.66f),new Color(.67f,.55f,.42f),gold*.5f),daylight);
            RenderSettings.fogColor=Color.Lerp(RenderSettings.fogColor,Color.Lerp(new Color(.04f,.055f,.07f),new Color(.37f,.43f,.47f),daylight),rainAmount);
            RenderSettings.reflectionIntensity=Mathf.Lerp(.18f,.75f,daylight)*Mathf.Lerp(1,.7f,rainAmount);
            RenderPipelineAdapter.ApplyAtmosphereLightingAndVolume(liveVolume,sun,moon,day,altitude,daylight,gold,clouds,rainAmount,RenderSettings.fogDensity,RenderSettings.fogColor,RenderSettings.ambientLight,
                lightingProfile && lightingProfile.usePhysicalHdrpLighting ? lightingProfile.hdrpSunLuxPerUnit : 0,
                lightingProfile ? lightingProfile.hdrpMoonLuxPerUnit : .5f);
        }
        // Runtime and the owner-operated comparison window share these exact samplers.
        public void SampleSky(Material target,float day,float rainAmount,float clouds,float drift)
        {
            if(!target)return;
            float a=TownLightingProfile.Altitude(day);
            float light=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.08f,.28f,a));
            float gold=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.02f,.45f,Mathf.Abs(a))))*light;
            float twilight=0;
            if(PhaseLighting)
            {
                var p=lightingProfile.Evaluate(day,clouds,rainAmount);light=p.skyDay;gold=p.skyGold;twilight=p.twilight;
                // Replace the photographed golden-hour sky before its sun would remain above our horizon.
                var weights=lightingProfile.Weights(day);
                float belowGolden=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.12f,a));
                twilight=Mathf.Max(twilight,belowGolden*(1-weights.w));
                target.SetVector("_Exposure",lightingProfile.skyExposure);
                target.SetColor("_TwilightTint",lightingProfile.twilightTint);
            }
            target.SetFloat("_Day",light);target.SetFloat("_Gold",gold);target.SetFloat("_Twilight",twilight);
            target.SetFloat("_Cloud",clouds);target.SetFloat("_Rain",rainAmount);target.SetFloat("_Drift",drift);
            target.SetVector("_SunDirection",-sun.transform.forward);
        }
        public void SamplePostProcessing(VolumeProfile target,float day,float rainAmount,float clouds)
        {
            if(!target)return;
            float light=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.08f,.28f,TownLightingProfile.Altitude(day)));
            var phase=PhaseLighting?lightingProfile.Evaluate(day,clouds,rainAmount):default;
            RenderPipelineAdapter.SampleAtmospherePostProcessing(
                target,
                PhaseLighting,
                PhaseLighting?phase.exposure:Mathf.Lerp(.15f,0,light),
                -Mathf.Clamp01(rainAmount)*7 - (lightingProfile && lightingProfile.usePhysicalHdrpLighting ? 6 : 0),
                phase.contrast,
                phase.bloom,
                lightingProfile && lightingProfile.usePhysicalHdrpLighting
                    ? (float?)lightingProfile.HdrpExposure(day, clouds, rainAmount) : null);
        }
        public void SampleSurface(Material target,Surface response,Color dryColor,float dryGloss,float wetness)
        {
            wetness=Mathf.Clamp01(wetness);
            float dark=response.darkening,gloss=Mathf.Max(dryGloss,response.smoothness);
            if(PhaseLighting)
            {
                dark=Mathf.Min(dark,lightingProfile.maximumWetDarkening);
                if(response.material==lightingProfile.roadMaterial)gloss=Mathf.Max(gloss,lightingProfile.roadWetSmoothness);
                gloss=Mathf.Max(dryGloss,Mathf.Min(gloss,response.wetOnly?lightingProfile.puddleWetCeiling:lightingProfile.opaqueWetCeiling));
            }
            float factor=1-wetness*dark;
            RenderPipelineAdapter.SetMaterialWetSurface(target,new Color(dryColor.r*factor,dryColor.g*factor,dryColor.b*factor,dryColor.a*(response.wetOnly?wetness:1)),Mathf.Lerp(dryGloss,gloss,wetness),wetness);
        }
        void ApplyMaterials()
        {
            foreach(var pair in wetOnlyRenderers)if(pair.Key)pair.Key.enabled=wetBlend>.02f;
            for(int i=0;i<wet.Count;i++)SampleSurface(wet[i],responses[i],dryColors[i],drySmoothness[i],wetBlend);
        }
        void EnsureLampBuffers()
        {
            if(distances==null || distances.Length!=lamps.Length)distances=new float[lamps.Length];
            if(lampScores!=null && lampScores.Length==lamps.Length)return;
            RestorePreviewFixtures();
            lampScores=new float[lamps.Length];selectedLampShadows=new bool[lamps.Length];fixtures=new FixtureEmission[lamps.Length];
            for(int i=0;i<lamps.Length;i++)if(lamps[i].light)fixtures[i]=new FixtureEmission(lamps[i].light);
        }
        public void SamplePracticalLights(Vector3 position,float day,float clouds,float rainAmount)
        {
            if(!PhaseLighting)
            {
                if(distances==null || distances.Length!=lamps.Length)distances=new float[lamps.Length];
                float light=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.08f,.28f,TownLightingProfile.Altitude(day)));
                ApplyLegacyLamps(position,light,rainAmount);return;
            }
            EnsureLampBuffers();
            var phase=lightingProfile.Evaluate(day,clouds,rainAmount);var weights=lightingProfile.Weights(day);
            for(int i=0;i<lamps.Length;i++)
            {
                var item=lamps[i];distances[i]=item.light && item.light.gameObject.activeInHierarchy?(item.light.transform.position-position).sqrMagnitude:float.MaxValue;
                var practical=default(TownLightingProfile.Practical);
                bool custom=item.light && lightingProfile.TryPractical(item.light.name,out practical);
                float priority=custom?Mathf.Max(.25f,practical.priority):1;
                lampScores[i]=distances[i]<10000?distances[i]/(priority*priority):float.MaxValue;
                selectedLampShadows[i]=item.shadows || (custom && practical.shadows);
            }
            for(int i=0;i<lamps.Length;i++)
            {
                var item=lamps[i];if(!item.light)continue;
                bool custom=lightingProfile.TryPractical(item.light.name,out var practical);
                float gain=custom?Mathf.Max(0,practical.gain):1;
                float level=custom?Mathf.Clamp01(Vector4.Dot(weights,practical.levels)):(item.interior?1:phase.exterior);
                int rank=0;
                for(int j=0;j<lamps.Length;j++)if(lampScores[j]<lampScores[i] || (lampScores[j]==lampScores[i] && j<i))rank++;
                item.light.enabled=rank<localLightBudget && distances[i]<10000;
                item.light.intensity=RenderPipelineAdapter.ScalePracticalLightIntensity(item.intensity*gain*level*Mathf.Lerp(1,.92f,rainAmount));
                if(custom)item.light.color=practical.color;
                int shadowRank=0;
                for(int j=0;j<lamps.Length;j++)if(selectedLampShadows[j] && distances[j]<900 &&
                    (lampScores[j]<lampScores[i] || (lampScores[j]==lampScores[i] && j<i)))shadowRank++;
                item.light.shadows=item.light.enabled && selectedLampShadows[i] && distances[i]<900 && shadowRank<2?LightShadows.Soft:LightShadows.None;
                // Visible bulb emission is phase-driven, not switched off when its real-time light is budget-culled.
                fixtures[i]?.Apply(item.light.color*(lightingProfile.fixtureEmission*gain*level));
            }
        }
        sealed class FixtureEmission
        {
            readonly Renderer renderer;
            readonly ShadowCastingMode savedShadows;
            readonly MaterialPropertyBlock saved=new MaterialPropertyBlock(),working=new MaterialPropertyBlock();
            internal FixtureEmission(Light light)
            {
                var fixture=light.transform.Find("Fixture");renderer=fixture?fixture.GetComponent<Renderer>():null;
                if(renderer){renderer.GetPropertyBlock(saved);savedShadows=renderer.shadowCastingMode;}
            }
            internal void Apply(Color color)
            {
                if(!renderer)return;
                // This small luminous mesh encloses its own light origin. It must not
                // occlude that light; surrounding architecture still casts normal shadows.
                renderer.shadowCastingMode=ShadowCastingMode.Off;
                RenderPipelineAdapter.ApplyFixtureEmission(renderer,working,color);
            }
            internal void Restore()
            {if(renderer){renderer.SetPropertyBlock(saved.isEmpty?null:saved);renderer.shadowCastingMode=savedShadows;}}
        }
        public void RestorePreviewFixtures()
        {
            if(fixtures!=null)foreach(var fixture in fixtures)fixture?.Restore();
            fixtures=null;lampScores=null;selectedLampShadows=null;
        }
        void ApplyLegacyLamps(Vector3 p,float daylight,float rainAmount)
        {
            // Stable nearest-light selection, no allocations or sorting each frame. Only two shadowed fixtures.
            for(int i=0;i<lamps.Length;i++) distances[i]=lamps[i].light?(lamps[i].light.transform.position-p).sqrMagnitude:float.MaxValue;

            for(int i=0;i<lamps.Length;i++)
            {
                var item=lamps[i]; if(!item.light)continue;
                int rank=0; for(int j=0;j<lamps.Length;j++) if(distances[j]<distances[i] || (distances[j]==distances[i] && j<i))rank++;
                item.light.enabled=rank<localLightBudget && distances[i]<10000;
                item.light.intensity=RenderPipelineAdapter.ScalePracticalLightIntensity(item.intensity*(item.interior?1:Mathf.Lerp(1,.08f,daylight)*(1-rainAmount*.1f)));
                int shadowRank=0;
                for(int j=0;j<lamps.Length;j++) if(lamps[j].shadows && (distances[j]<distances[i] || (distances[j]==distances[i] && j<i)))shadowRank++;
                item.light.shadows=item.light.enabled && item.shadows && distances[i]<900 && shadowRank<2?LightShadows.Soft:LightShadows.None;
            }
        }
        void BindVehicle()
        {
            if(!vehicleWorld || vehicleWorld.Actor==vehicle)return;
            vehicle=vehicleWorld.Actor; if(!vehicle)return;
            foreach(var r in vehicle.GetComponentsInChildren<Renderer>(true))
            {
                foreach(var m in r.sharedMaterials)
                    if(m && RenderPipelineAdapter.IsOpaqueMetallicVehicleLitMaterial(m) && !replacements.ContainsKey(m))
                        AddMaterial(m,new Surface { material=m,darkening=.12f,smoothness=.68f });
                Replace(r);
            }
        }
        void BindCamera()
        {
            if(cameraState.Bound || !Camera.main)return;
            cameraState=RenderPipelineAdapter.EnableGameplayPostAndAA(Camera.main);
        }
        public void Unbind()
        {
            if(!bound)return;
            if(rain)rain.Stop();
            foreach(var pair in originals)if(pair.Key)pair.Key.sharedMaterials=pair.Value;
            foreach(var pair in wetOnlyRenderers)if(pair.Key)pair.Key.enabled=pair.Value; wetOnlyRenderers.Clear();
            foreach(var m in wet)if(m)Destroy(m);
            originals.Clear(); replacements.Clear(); wet.Clear(); dryColors.Clear(); drySmoothness.Clear(); responses.Clear();
            RenderSettings.skybox=oldSky; RenderSettings.fog=oldFog; RenderSettings.fogMode=oldFogMode;
            RenderSettings.fogColor=oldFogColor; RenderSettings.fogDensity=oldFogDensity;
            RenderSettings.ambientMode=oldAmbientMode; RenderSettings.ambientLight=oldAmbient; RenderSettings.ambientIntensity=oldAmbientIntensity;
            RenderSettings.ambientProbe=oldProbe; RenderSettings.reflectionIntensity=oldReflection;
            if(sun){sun.intensity=oldSunIntensity; sun.color=oldSunColor; sun.transform.rotation=oldSunRotation;sun.shadowStrength=oldSunShadowStrength;}
            if(moon){moon.intensity=oldMoonIntensity;moon.transform.rotation=oldMoonRotation;}
            for(int i=0;i<lamps.Length;i++)if(lamps[i].light){ lamps[i].light.intensity=originalLightIntensities[i]; lamps[i].light.enabled=originalLightEnabled[i]; lamps[i].light.shadows=originalShadows[i];lamps[i].light.color=originalLightColors[i]; }
            RestorePreviewFixtures();
            RenderPipelineAdapter.RestoreCameraPostAndAA(ref cameraState);
            if(volume && liveVolume)volume.sharedProfile=oldVolume;
            if(liveVolume){foreach(var c in liveVolume.components)Destroy(c); Destroy(liveVolume);}
            if(liveSky)Destroy(liveSky);
            clock=null; observedSimulation=null; vehicle=null; bound=false; elapsed=0; reflectionCooldown=0; reflectionRender=-1; rainBlend=wetBlend=0;
        }
        void OnDisable()=>Unbind();
    }
}
