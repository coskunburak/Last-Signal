using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastSignal.Atmosphere;
using LastSignal.Audio;
using LastSignal.WorldTime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace LastSignal.Editor.FirstTown
{
    /// <summary>Explicit scene authoring only. Never enters play, bakes, builds or runs tests.</summary>
    public static class FirstTownAtmosphereAuthoring
    {
        const string Root="Assets/LastSignal/Art/FirstTownAtmosphere";
        const string Label="FirstTown Atmosphere";
        static T Load<T>(string path) where T:Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing "+path);
        static IEnumerable<T> All<T>(Scene s) where T:Component => s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true));
        static void Set(Object target,string field,Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        static Material SaveMaterial(Material m,string name)
        {
            string path=Root+"/"+name+".mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(existing){EditorUtility.CopySerialized(m,existing);Object.DestroyImmediate(m);return existing;}
            m.name=name;AssetDatabase.CreateAsset(m,path);return m;
        }
        [MenuItem("Last Signal/First Town/Atmosphere/Install atmosphere (no bake)")]
        public static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage()!=null)throw new InvalidOperationException("Exit PlayMode / prefab mode.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save modified scenes before authoring.");
            var scene=EditorSceneManager.OpenScene(FirstTownAuthoring.ScenePath,OpenSceneMode.Single);
            if(All<TownAtmosphere>(scene).Any())throw new InvalidOperationException("Atmosphere already installed. Tune scene/profile instead of regenerating.");
            var clock=All<WorldClock>(scene).Single(); var view=All<WorldTimePresentation>(scene).Single();
            var shader=Shader.Find("LastSignal/FirstTown/AtmosphereSky"); if(!shader)throw new InvalidOperationException("Town sky shader not imported.");
            var sky=SaveMaterial(new Material(shader),"TownSky");
            sky.SetTexture("_DayTex",Load<Texture2D>("Assets/Fantasy Skybox FREE/Panoramics/FS003/FS003_Day_Sunless.png"));
            sky.SetTexture("_OvercastTex",Load<Texture2D>("Assets/kloppenheim_01_puresky_4k.exr"));
            sky.SetTexture("_SunsetTex",Load<Texture2D>("Assets/qwantani_sunset_puresky_4k.exr"));
            sky.SetTexture("_NightTex",Load<Texture2D>("Assets/qwantani_night_puresky_4k.exr"));
            sky.SetTexture("_CloudTex",Load<Texture2D>("Assets/RainSnowCloudEffect/Textures/T_CloudLayer.png"));
            sky.SetVector("_Exposure",new Vector4(1,.8f,.7f,.18f)); sky.SetVector("_SunDirection",new Vector4(.4f,.5f,.2f,0));
            var root=new GameObject(Label);var director=root.AddComponent<TownAtmosphere>();
            director.sky=sky; director.sun=view.Sun;
            director.sun.shadows=LightShadows.Soft; director.sun.shadowStrength=.85f;
            var moon=new GameObject("Moon — soft readability").AddComponent<Light>();moon.transform.SetParent(root.transform);
            moon.type=LightType.Directional; moon.color=new Color(.52f,.64f,.84f);moon.intensity=0;moon.shadows=LightShadows.None;director.moon=moon;
            var volume=All<Volume>(scene).Single(v=>v.isGlobal); director.volume=volume;
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"/TownVolume.asset");
            if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Root+"/TownVolume.asset");}
            else { foreach(var c in profile.components)if(c)Object.DestroyImmediate(c,true);profile.components.Clear(); }
            var tone=profile.Add<Tonemapping>(true);tone.mode.value=TonemappingMode.ACES;
            var color=profile.Add<ColorAdjustments>(true);color.postExposure.value=0;color.contrast.value=4;color.saturation.value=0;
            var bloom=profile.Add<Bloom>(true);bloom.intensity.value=.12f;bloom.threshold.value=1.2f;bloom.scatter.value=.35f;
            var vignette=profile.Add<Vignette>(true);vignette.intensity.value=.08f;vignette.smoothness.value=.35f;
            foreach(var c in profile.components){c.name=c.GetType().Name;AssetDatabase.AddObjectToAsset(c,profile);}
            EditorUtility.SetDirty(profile);volume.sharedProfile=profile;volume.name="FirstTown controlled ACES";
            foreach(var old in All<LastSignal.Slice.S013EnvironmentPresentation>(scene).ToArray())Object.DestroyImmediate(old);
            if(view.Rain){view.Rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);view.Rain.gameObject.SetActive(false);}
            director.rain=Rain(root.transform);
            var lamps=new List<TownAtmosphere.Lamp>();
            foreach(var l in All<Light>(scene).Where(l=>l.type!=LightType.Directional))
                lamps.Add(new TownAtmosphere.Lamp { light=l,intensity=l.intensity,interior=l.name.Contains("Shelter"),shadows=l.name.Contains("Shelter") });
            Lamp(root.transform,lamps,"Safehouse gate",new Vector3(-180,3.8f,-141),new Color(1,.65f,.34f),2.8f,15,false);
            Lamp(root.transform,lamps,"Safehouse yard",new Vector3(-194,4,-167),new Color(1,.72f,.44f),2.2f,16,false);
            Lamp(root.transform,lamps,"Gas forecourt",new Vector3(72,4.6f,-83),new Color(1,.84f,.64f),4,23,false);
            Lamp(root.transform,lamps,"Gas approach",new Vector3(51,4.5f,-108),new Color(1,.69f,.4f),2.5f,18,false);
            Lamp(root.transform,lamps,"Town center west",new Vector3(-13,4.5f,32),new Color(1,.76f,.51f),2.4f,18,false);
            Lamp(root.transform,lamps,"Town hall approach",new Vector3(20,4.5f,58),new Color(1,.81f,.62f),2.8f,18,false);
            Lamp(root.transform,lamps,"Industrial loading",new Vector3(165,6,-66),new Color(.79f,.85f,.91f),4,25,false);
            Lamp(root.transform,lamps,"Industrial gate",new Vector3(128,5,-93),new Color(1,.76f,.46f),3,20,false);
            Lamp(root.transform,lamps,"Clinic entrance",new Vector3(-55,3.6f,-49),new Color(.76f,1,.85f),2.5f,14,false);
            Lamp(root.transform,lamps,"Motel court",new Vector3(80,4.2f,-185),new Color(1,.60f,.32f),3.2f,22,false);
            Lamp(root.transform,lamps,"Motel highway",new Vector3(31,5,-189),new Color(1,.72f,.42f),2.6f,19,false);
            Lamp(root.transform,lamps,"Checkpoint",new Vector3(14,5,-136),new Color(.83f,.88f,1),2.5f,20,false);
            Lamp(root.transform,lamps,"Residential junction",new Vector3(125,4.4f,115),new Color(1,.77f,.51f),2.2f,18,false);
            Lamp(root.transform,lamps,"School path",new Vector3(-45,4.5f,126),new Color(.91f,.88f,.72f),2.2f,17,false);
            Lamp(root.transform,lamps,"Forest ranger porch",new Vector3(-197,3.5f,124),new Color(1,.60f,.29f),1.5f,12,false);
            Lamp(root.transform,lamps,"Farm yard",new Vector3(180,4,202),new Color(1,.72f,.43f),1.8f,14,false);
            // Every playable modular room receives a bounded downlight beneath its actual RainRoof.
            foreach(var roof in All<RainRoof>(scene).Where(r=>r.name=="Rain roof"))
            {
                var p=roof.transform.position; p.y-=.8f;
                string name=roof.transform.parent.name;
                Color tint=name.Contains("clinic")?new Color(.87f,.97f,.88f):name.Contains("dispatch") || name.Contains("service")?new Color(.83f,.88f,.93f):new Color(1,.78f,.52f);
                Lamp(root.transform,lamps,name+" interior",p,tint,2.5f,10,true);
            }
            director.lamps=lamps.ToArray();
            WetSurfaces(scene,director);
            Set(view,"atmosphere",director);
            var audio=All<ProductionAudio>(scene).Single();Set(audio,"atmosphere",director);
            var audioSO=new SerializedObject(audio);audioSO.FindProperty("useWorldRoofAmbience").boolValue=true;audioSO.ApplyModifiedPropertiesWithoutUndo();
            var probeGO=new GameObject("Local exterior reflection — bounded runtime update");probeGO.transform.SetParent(root.transform);probeGO.transform.position=new Vector3(0,2,0);
            var probe=probeGO.AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;probe.resolution=128;probe.size=new Vector3(130,35,130);
            probe.boxProjection=true;probe.blendDistance=20;probe.nearClipPlane=.5f;probe.farClipPlane=180;probe.cullingMask=~((1<<8)|(1<<2));probe.intensity=.65f;
            director.exteriorReflection=probe;
            RenderSettings.skybox=sky;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.0018f;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.35f,.42f,.5f);RenderSettings.ambientEquatorColor=new Color(.25f,.28f,.3f);RenderSettings.ambientGroundColor=new Color(.13f,.13f,.12f);
            EditorUtility.SetDirty(director);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Docs/Implementation/FirstTownAtmosphere");
            File.WriteAllText("Docs/Implementation/FirstTownAtmosphere/authoring.json",JsonUtility.ToJson(new Report {status="IMPLEMENTED",lights=lamps.Count,wetMaterials=director.surfaces.Length,wetRenderers=director.wetRenderers.Length},true));
            Debug.Log("FirstTown atmosphere authored. No tests, PlayMode, bake, build or performance capture executed.");
        }
        [Serializable] sealed class Report {public string status,visual="NOT_RUN",gameplay="NOT_RUN",performance="NOT_RUN";public int lights,wetMaterials,wetRenderers;}
        static void Lamp(Transform parent,List<TownAtmosphere.Lamp> list,string name,Vector3 p,Color color,float power,float range,bool interior)
        {
            var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=p;go.transform.rotation=Quaternion.Euler(90,0,0);
            var light=go.AddComponent<Light>();light.type=LightType.Spot;light.spotAngle=interior?125:115;light.innerSpotAngle=interior?80:70;
            light.color=color;light.intensity=power;light.range=range;light.shadows=LightShadows.None;
            list.Add(new TownAtmosphere.Lamp {light=light,intensity=power,interior=interior,shadows=interior});
            // Visible fixture, no collider, navigation or gameplay changes.
            var fixture=GameObject.CreatePrimitive(PrimitiveType.Cylinder);fixture.name="Fixture";fixture.transform.SetParent(go.transform,false);
            fixture.transform.localScale=new Vector3(.28f,.06f,.28f);Object.DestroyImmediate(fixture.GetComponent<Collider>());
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Fixture.mat");
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetColor("_BaseColor",new Color(.25f,.22f,.17f));mat.SetFloat("_Metallic",.6f);mat=SaveMaterial(mat,"Fixture");}
            fixture.GetComponent<Renderer>().sharedMaterial=mat;
        }
        static TownRain Rain(Transform parent)
        {
            var root=new GameObject("Roof-aware rain");root.transform.SetParent(parent);var rain=root.AddComponent<TownRain>();
            var mat=SaveMaterial(new Material(Load<Material>("Assets/RainSnowCloudEffect/Materials/M_Rain_URP.mat")),"RainStreak");
            var splash=SaveMaterial(new Material(Load<Material>("Assets/RainSnowCloudEffect/Materials/M_RainSplash_URP.mat")),"RainSplash");
            rain.near=Particles(root.transform,"Near drops",mat,650,13,false);
            rain.middle=new ParticleSystem[8];for(int i=0;i<8;i++)rain.middle[i]=Particles(root.transform,"Middle veil "+i,mat,180,16,false);
            rain.splash=Particles(root.transform,"Surface impacts",splash,64,1,true);return rain;
        }
        static ParticleSystem Particles(Transform parent,string name,Material material,int limit,float width,bool splash)
        {
            var go=new GameObject(name);go.layer=2;go.transform.SetParent(parent);var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.playOnAwake=false;main.loop=true;main.maxParticles=limit;main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startLifetime=splash?.35f:.8f;main.startSpeed=0;main.startSize=splash?.18f:.055f;main.startColor=new Color(.72f,.81f,.88f,splash?.32f:.26f);
            var emission=ps.emission;emission.rateOverTime=0;
            var shape=ps.shape;shape.enabled=!splash;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(width,.2f,width);
            var velocity=ps.velocityOverLifetime;velocity.enabled=!splash;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=1.1f;velocity.y=-17;velocity.z=.45f;
            var collision=ps.collision;collision.enabled=!splash;collision.type=ParticleSystemCollisionType.World;collision.mode=ParticleSystemCollisionMode.Collision3D;
            collision.quality=ParticleSystemCollisionQuality.Medium;collision.collidesWith=~((1<<8)|(1<<2));collision.enableDynamicColliders=false;collision.lifetimeLoss=1;collision.maxCollisionShapes=64;
            var fade=ps.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();
            gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(splash?.7f:0,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(0,1)});fade.color=gradient;
            var size=ps.sizeOverLifetime;size.enabled=splash;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.3f,1,1));
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.renderMode=splash?ParticleSystemRenderMode.HorizontalBillboard:ParticleSystemRenderMode.Stretch;
            renderer.lengthScale=splash?1:3;renderer.velocityScale=.025f;
            return ps;
        }
        static string SourceName(Material m)
        {
            if(!m.name.StartsWith("Source_"))return m.name.ToLowerInvariant();
            var bits=m.name.Split('_');return bits.Length>1?Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(bits[1])).ToLowerInvariant():"";
        }
        static bool Response(Material m,out TownAtmosphere.Surface s)
        {
            s=default;if(!m || m.shader.name!="Universal Render Pipeline/Lit" || m.GetFloat("_Surface")!=0 || m.GetFloat("_AlphaClip")>.5f)return false;
            string n=SourceName(m);
            if(n.Contains("glass") || n.Contains("leaf") || n.Contains("leaves") || n.Contains("bark") || n.Contains("interior") || n.Contains("cloth") || n.Contains("water"))return false;
            if(n.Contains("road") || n.Contains("asphalt")){s.darkening=.27f;s.smoothness=.68f;}
            else if(n.Contains("metal") || n.Contains("iron") || n.Contains("rust") || n.Contains("container")){s.darkening=.12f;s.smoothness=.62f;}
            else if(n.Contains("brick") || n.Contains("concrete") || n.Contains("stone") || n=="clinic" || n=="school"){s.darkening=.2f;s.smoothness=.36f;}
            else if(n.Contains("wood") || n.Contains("timber") || n.Contains("fence")){s.darkening=.23f;s.smoothness=.30f;}
            else if(n.Contains("roof")){s.darkening=.16f;s.smoothness=.49f;}
            else if(n.Contains("ground") || n.Contains("path")){s.darkening=.18f;s.smoothness=.28f;}
            else return false;
            s.material=m;return true;
        }
        static void WetSurfaces(Scene scene,TownAtmosphere director)
        {
            var mapping=new Dictionary<Material,Material>();var responses=new List<TownAtmosphere.Surface>();var renderers=new List<Renderer>();
            var roofBounds=All<RainRoof>(scene).SelectMany(r=>r.GetComponentsInChildren<Collider>()).Where(c=>c.enabled).Select(c=>c.bounds).ToArray();
            foreach(var r in All<MeshRenderer>(scene).Where(r=>r.enabled && r.gameObject.activeInHierarchy && !r.GetComponentInParent<Canvas>()))
            {
                var b=r.bounds;
                // Fully enclosed surfaces stay dry; roof surfaces and large exterior shells are eligible.
                if(roofBounds.Any(roof=>b.max.y<roof.min.y-.05f && b.min.x>roof.min.x && b.max.x<roof.max.x && b.min.z>roof.min.z && b.max.z<roof.max.z))continue;
                var mats=r.sharedMaterials;bool changed=false;
                for(int i=0;i<mats.Length;i++)
                {
                    var original=mats[i];if(!Response(original,out var response))continue;
                    if(!mapping.TryGetValue(original,out var variant))
                    {
                        string id=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original));
                        variant=SaveMaterial(new Material(original),"Wet_"+id);mapping.Add(original,variant);response.material=variant;responses.Add(response);
                    }
                    mats[i]=variant;changed=true;
                }
                if(changed){r.sharedMaterials=mats;renderers.Add(r);PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
            }
            director.surfaces=responses.ToArray();director.wetRenderers=renderers.ToArray();
        }
    }
}
