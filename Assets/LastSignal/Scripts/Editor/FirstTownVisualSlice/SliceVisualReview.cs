using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastSignal.Atmosphere;
using LastSignal.VisualSlice;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace LastSignal.Editor.FirstTownVisualSlice
{
    /// <summary>Explicit owner-operated renders; never opens PlayMode or changes the clock/save data.</summary>
    public sealed class SliceVisualReview : EditorWindow
    {
        int viewIndex,conditionIndex;
        bool compareBeforeLighting;
        FirstTownSliceProfile profile;
        readonly string[] conditions={"Day 08:20","Sunset 17:40","Overcast 12:00","Rain lighting and wet surfaces 12:00","Night 23:00","Night + comparison flashlight","Dusk 18:30","Damp dusk 18:30 (wetness only)"};
        readonly float[] hours={8+20/60f,17+40/60f,12,12,23,23,18.5f,18.5f};
        readonly float[] cloud={.2f,.2f,.92f,1,.25f,.25f,.35f,.60f};
        readonly float[] rainAmounts={0,0,0,1,0,0,0,0},wetnessLevels={0,0,0,1,0,0,0,.6f};
        void OnEnable(){profile=AssetDatabase.LoadAssetAtPath<FirstTownSliceProfile>(SliceAuthoring.Root+"/Profile.asset");}
        [MenuItem("Last Signal/First Town/Visual Slice/2 Visual comparison studio")]
        public static void Open()=>GetWindow<SliceVisualReview>("FirstTown visual comparison");
        void OnGUI()
        {
            EditorGUILayout.LabelField("1920 × 1080 • 75° player FOV • fixed poses",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Owner-operated art comparison. Captures are EDITOR_RENDER_ONLY, not gameplay acceptance. Rain preset samples wetness and lighting; actual rain, indoor protection, driving and flashlight gameplay must be reviewed in PlayMode by you.",MessageType.Info);
            profile=(FirstTownSliceProfile)EditorGUILayout.ObjectField("Pose profile",profile,typeof(FirstTownSliceProfile),false);
            if(!profile && GUILayout.Button("Load / create fixed pose profile")){SliceAuthoring.Guard();profile=SliceAuthoring.Profile();AssetDatabase.SaveAssets();}
            if(!profile)return;
            viewIndex=EditorGUILayout.Popup("View",Mathf.Clamp(viewIndex,0,profile.views.Length-1),profile.views.Select(p=>p.name).ToArray());
            conditionIndex=EditorGUILayout.Popup("Condition",conditionIndex,conditions);
            compareBeforeLighting=EditorGUILayout.Toggle("Archived lighting comparison",compareBeforeLighting);
            if(GUILayout.Button("Frame this pose in Scene View"))
            {
                var p=profile.views[viewIndex];var sv=SceneView.lastActiveSceneView;
                if(sv){sv.LookAt(p.target,Quaternion.LookRotation(p.target-p.position),Vector3.Distance(p.position,p.target),p.orthographic,true);sv.Repaint();}
            }
            using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                if(GUILayout.Button("Capture selected comparison + settings manifest"))Capture();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime evidence",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("During your PlayMode session, stand at the profile's coordinate and use the same camera FOV. Capture the current Game View with HUD first. For an environment-only runtime view, turn off Game View Gizmos and HUD through the normal presentation controls, then capture again. The fixed Editor render does not prove navigation or player-camera behavior.",MessageType.None);
        }
        [Serializable] sealed class CaptureRecord
        {
            public string evidence="EDITOR_RENDER_ONLY",scene,sceneHash,profileHash,view,condition,utc;
            public Vector3 position,target;public float fieldOfView,hour,clouds,rain,wetness;
            public int width=1920,height=1080;public string quality="VISUAL_REVIEW_PENDING",rainParticles="NOT_RENDERED",flashlight="COMPARISON_FIXTURE_ONLY",navigation="NOT_RUN";
            public string lightingProfile,lightingProfileHash,skyMaterial,lightingVariant;
            public Vector4 skyExposure;public float sunIntensity,moonIntensity,sunShadowStrength,fogDensity,postExposure;
            public Color ambient,fogColor;public int enabledPracticalLights;
            public string reflections="EXISTING_PROBE_NOT_REFRESHED";
            public string captureRevision="LIGHTING03_SRGB",projectColorSpace,renderTargetGraphicsFormat,outputEncoding;
            public bool renderTargetSrgb;
            public PracticalRecord[] practicalLights;
            public MeshRecord[] canopyMeshes;
        }
        [Serializable] sealed class MeshRecord
        {
            public string asset,assetHash;
            public Bounds loadedBounds;
            public int vertices;
        }
        [Serializable] sealed class PracticalRecord
        {
            public string name;public Vector3 position;public bool enabled,active;
            public float intensity,range;public string shadows;public Color color;
        }
        void Capture()
        {
            SliceAuthoring.Guard();var scene=SceneManager.GetActiveScene();
            if(scene.path!=SliceAuthoring.Source && scene.path!=SliceAuthoring.Stage)throw new InvalidOperationException("Open FirstTownCore or the visual slice stage.");
            if(compareBeforeLighting && scene.path!=SliceAuthoring.Stage)throw new InvalidOperationException("The archived lighting profile belongs to the existing visual slice. Open that stage for an A/B lighting comparison.");
            var d=SliceAuthoring.All<TownAtmosphere>(scene).Single();var pose=profile.views[viewIndex];
            var originalLighting=d.lightingProfile;
            var snapshot=new EnvironmentSnapshot(d);GameObject cameraObject=null;RenderTexture rt=null;Texture2D image=null,readback=null;Material sky=null;
            VolumeProfile originalVolume=d.volume?d.volume.sharedProfile:null,previewVolume=null;
            var clones=new List<Material>();var rendererMaterials=new Dictionary<Renderer,Material[]>();
            var enabled=new Dictionary<Renderer,bool>();var lights=new List<LightSnapshot>();
            try
            {
                if(compareBeforeLighting)
                {
                    d.lightingProfile=AssetDatabase.LoadAssetAtPath<TownLightingProfile>(SliceAuthoring.Root+"/LightingReview/BeforeLighting.asset");
                    if(!d.lightingProfile)throw new FileNotFoundException("Archived pre-overhaul lighting profile missing; do not substitute a different baseline.");
                }
                float hour=hours[conditionIndex],rain=rainAmounts[conditionIndex],wet=wetnessLevels[conditionIndex];
                cameraObject=new GameObject("Temporary visual comparison camera"){hideFlags=HideFlags.HideAndDontSave};
                var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.transform.position=pose.position;camera.transform.LookAt(pose.target);
                camera.fieldOfView=pose.fieldOfView;camera.orthographic=pose.orthographic;camera.orthographicSize=pose.orthographicSize;
                camera.aspect=16f/9;camera.nearClipPlane=.08f;camera.farClipPlane=700;camera.allowHDR=true;
                camera.cullingMask=~((1<<5)|(1<<30));camera.clearFlags=CameraClearFlags.Skybox;
                var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.Medium;
                d.SampleLighting(hour/24,rain,cloud[conditionIndex]);sky=new Material(d.sky){hideFlags=HideFlags.HideAndDontSave};RenderSettings.skybox=sky;
                if(originalVolume)
                {
                    previewVolume=ScriptableObject.CreateInstance<VolumeProfile>();previewVolume.hideFlags=HideFlags.HideAndDontSave;
                    foreach(var c in originalVolume.components)if(c)previewVolume.components.Add(Object.Instantiate(c));
                    d.SamplePostProcessing(previewVolume,hour/24,rain,cloud[conditionIndex]);
                    d.volume.sharedProfile=previewVolume;
                }
                d.SampleSky(sky,hour/24,rain,cloud[conditionIndex],hour/24);
                Shader.SetGlobalFloat("_LSForestWetness",wet);Shader.SetGlobalVector("_LSForestWindPhase",new Vector4(hour/24*Mathf.PI*240,hour/24*Mathf.PI*62,Mathf.Lerp(.07f,.12f,rain),0));
                var replacement=new Dictionary<Material,Material>();
                foreach(var surface in d.surfaces)
                {
                    if(!surface.material || replacement.ContainsKey(surface.material))continue;
                    var m=new Material(surface.material){hideFlags=HideFlags.HideAndDontSave};clones.Add(m);replacement.Add(surface.material,m);
                    d.SampleSurface(m,surface,m.GetColor("_BaseColor"),m.GetFloat("_Smoothness"),wet);
                }
                foreach(var r in d.wetRenderers)
                {
                    if(!r || rendererMaterials.ContainsKey(r))continue;var original=r.sharedMaterials;rendererMaterials.Add(r,original);
                    r.sharedMaterials=original.Select(m=>m && replacement.TryGetValue(m,out var value)?value:m).ToArray();
                    if(d.surfaces.Any(s=>s.wetOnly && original.Contains(s.material))){enabled.Add(r,r.enabled);r.enabled=wet>.02f;}
                }
                foreach(var lamp in d.lamps)if(lamp.light)lights.Add(new LightSnapshot(lamp.light));
                d.SamplePracticalLights(pose.position,hour/24,cloud[conditionIndex],rain);
                if(conditionIndex==5)
                {
                    var flashlight=cameraObject.AddComponent<Light>();flashlight.type=LightType.Spot;flashlight.range=12;flashlight.spotAngle=50;flashlight.innerSpotAngle=27;flashlight.intensity=3;flashlight.color=new Color(.88f,.93f,1);flashlight.shadows=LightShadows.Soft;
                }
                // Keep the external target floating point: this URP version also uses
                // its format for intermediate rendering. An 8-bit target would clip HDR
                // before tonemapping. Encode the final linear pixels only at PNG export.
                rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear){hideFlags=HideFlags.HideAndDontSave};
                if(!rt.Create())throw new InvalidOperationException("Cannot create the HDR comparison target.");
                if(rt.sRGB)throw new InvalidOperationException("Expected a linear readback target; refusing double-encoded PNG output.");
                camera.targetTexture=rt;
                // Unity 6 URP's supported explicit render-request path, only on the user's button press.
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};
                if(!RenderPipeline.SupportsRenderRequest(camera,request))throw new InvalidOperationException("Current renderer cannot service a single-camera capture. Use the fixed camera in Game View manually.");
                RenderPipeline.SubmitRenderRequest(camera,request);
                var previous=RenderTexture.active;
                try
                {
                    RenderTexture.active=rt;
                    readback=new Texture2D(1920,1080,TextureFormat.RGBAFloat,false,true){hideFlags=HideFlags.HideAndDontSave};
                    readback.ReadPixels(new Rect(0,0,1920,1080),0,0);readback.Apply();
                    var pixels=readback.GetPixels();var display=new Color32[pixels.Length];
                    bool linear=QualitySettings.activeColorSpace==ColorSpace.Linear;
                    for(int i=0;i<pixels.Length;i++)
                    {
                        // Convert before 8-bit quantization to preserve dark detail.
                        var c=linear?pixels[i].gamma:pixels[i];c.a=1;display[i]=c;
                    }
                    image=new Texture2D(1920,1080,TextureFormat.RGB24,false,false){hideFlags=HideFlags.HideAndDontSave};
                    image.SetPixels32(display);image.Apply();
                }
                finally{RenderTexture.active=previous;}
                string folder=SliceAuthoring.Evidence+"/Captures";Directory.CreateDirectory(folder);
                string stem=folder+"/"+(scene.path==SliceAuthoring.Source?"baseline":"stage")+(compareBeforeLighting?"-lighting-before":"-lighting-current")+"-view"+(viewIndex+1)+"-condition"+(conditionIndex+1)+"-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
                File.WriteAllBytes(stem+".png",image.EncodeToPNG());
                var record=new CaptureRecord{scene=scene.path,sceneHash=SliceAuthoring.Hash(scene.path),profileHash=SliceAuthoring.Hash(AssetDatabase.GetAssetPath(profile)),view=pose.name,condition=conditions[conditionIndex],utc=DateTime.UtcNow.ToString("O"),position=pose.position,target=pose.target,fieldOfView=pose.fieldOfView,hour=hour,clouds=cloud[conditionIndex],rain=rain,wetness=wet,lightingProfile=AssetDatabase.GetAssetPath(d.lightingProfile),skyMaterial=AssetDatabase.GetAssetPath(d.sky),skyExposure=sky.GetVector("_Exposure"),sunIntensity=d.sun.intensity,ambient=RenderSettings.ambientLight};
                record.lightingVariant=compareBeforeLighting?"ARCHIVED_PRE_OVERHAUL":"CURRENT";
                if(d.lightingProfile)record.lightingProfileHash=SliceAuthoring.Hash(AssetDatabase.GetAssetPath(d.lightingProfile));
                record.moonIntensity=d.moon?d.moon.intensity:0;record.sunShadowStrength=d.sun.shadowStrength;
                record.fogDensity=RenderSettings.fogDensity;record.fogColor=RenderSettings.fogColor;
                record.enabledPracticalLights=d.lamps.Count(l=>l.light && l.light.enabled);
                record.projectColorSpace=QualitySettings.activeColorSpace.ToString();record.renderTargetSrgb=rt.sRGB;
                record.renderTargetGraphicsFormat=rt.graphicsFormat.ToString();record.outputEncoding="DISPLAY_SRGB_BYTES";
                record.practicalLights=d.lamps.Where(l=>l.light && (l.light.transform.position-pose.position).sqrMagnitude<10000)
                    .Select(l=>new PracticalRecord{name=l.light.name,position=l.light.transform.position,enabled=l.light.enabled,
                        active=l.light.gameObject.activeInHierarchy,intensity=l.light.intensity,range=l.light.range,
                        shadows=l.light.shadows.ToString(),color=l.light.color}).ToArray();
                // Mesh assets can change without changing the scene hash. Record the
                // actually loaded bounds too, so a stale import is visible in evidence.
                record.canopyMeshes=SliceAuthoring.All<MeshFilter>(scene).Where(f=>f.sharedMesh)
                    .Select(f=>f.sharedMesh).Distinct()
                    .Where(m=>AssetDatabase.GetAssetPath(m).StartsWith(SliceAuthoring.Root+"/Refinement05/",StringComparison.Ordinal))
                    .Select(m=>new MeshRecord{asset=AssetDatabase.GetAssetPath(m),assetHash=SliceAuthoring.Hash(AssetDatabase.GetAssetPath(m)),loadedBounds=m.bounds,vertices=m.vertexCount}).ToArray();
                if(previewVolume && previewVolume.TryGet<ColorAdjustments>(out var grade))record.postExposure=grade.postExposure.value;
                File.WriteAllText(stem+".json",JsonUtility.ToJson(record,true));Debug.Log("Editor comparison captured: "+stem+".png. Runtime acceptance NOT_RUN.");
            }
            finally
            {
                foreach(var pair in rendererMaterials)if(pair.Key)pair.Key.sharedMaterials=pair.Value;
                foreach(var pair in enabled)if(pair.Key)pair.Key.enabled=pair.Value;
                foreach(var l in lights)l.Restore();d.RestorePreviewFixtures();snapshot.Restore();d.lightingProfile=originalLighting;
                if(d.volume)d.volume.sharedProfile=originalVolume;
                if(previewVolume){foreach(var c in previewVolume.components)Object.DestroyImmediate(c);Object.DestroyImmediate(previewVolume);}
                if(cameraObject)Object.DestroyImmediate(cameraObject);if(rt){rt.Release();Object.DestroyImmediate(rt);}if(image)Object.DestroyImmediate(image);if(readback)Object.DestroyImmediate(readback);if(sky)Object.DestroyImmediate(sky);foreach(var m in clones)Object.DestroyImmediate(m);
                SceneView.RepaintAll();
            }
        }
        sealed class LightSnapshot
        {
            readonly Light light;readonly bool enabled;readonly float intensity,shadowStrength;readonly Color color;readonly Quaternion rotation;readonly LightShadows shadows;
            internal LightSnapshot(Light l){light=l;enabled=l.enabled;intensity=l.intensity;color=l.color;rotation=l.transform.rotation;shadows=l.shadows;shadowStrength=l.shadowStrength;}
            internal void Restore(){if(!light)return;light.enabled=enabled;light.intensity=intensity;light.color=color;light.transform.rotation=rotation;light.shadows=shadows;light.shadowStrength=shadowStrength;}
        }
        sealed class EnvironmentSnapshot
        {
            readonly Material sky=RenderSettings.skybox;readonly bool fog=RenderSettings.fog;readonly FogMode mode=RenderSettings.fogMode;readonly Color fogColor=RenderSettings.fogColor,ambient=RenderSettings.ambientLight;
            readonly float density=RenderSettings.fogDensity,ambientIntensity=RenderSettings.ambientIntensity,reflection=RenderSettings.reflectionIntensity,wet=Shader.GetGlobalFloat("_LSForestWetness");
            readonly Vector4 wind=Shader.GetGlobalVector("_LSForestWindPhase");readonly AmbientMode ambientMode=RenderSettings.ambientMode;readonly SphericalHarmonicsL2 probe=RenderSettings.ambientProbe;
            readonly LightSnapshot sun,moon;
            internal EnvironmentSnapshot(TownAtmosphere d){sun=new LightSnapshot(d.sun);if(d.moon)moon=new LightSnapshot(d.moon);}
            internal void Restore(){RenderSettings.skybox=sky;RenderSettings.fog=fog;RenderSettings.fogMode=mode;RenderSettings.fogColor=fogColor;RenderSettings.fogDensity=density;RenderSettings.ambientMode=ambientMode;RenderSettings.ambientLight=ambient;RenderSettings.ambientIntensity=ambientIntensity;RenderSettings.ambientProbe=probe;RenderSettings.reflectionIntensity=reflection;Shader.SetGlobalFloat("_LSForestWetness",wet);Shader.SetGlobalVector("_LSForestWindPhase",wind);sun.Restore();moon?.Restore();}
        }
    }
}
