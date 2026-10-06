using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LastSignal.Art.Editor
{
    // Evaluation only. Never authors or overwrites an integrated gameplay scene.
    public static class S013AssetEvaluation
    {
        public const string Root = "Assets/LastSignal/Art/S013/Evaluation";
        public const string ScenePath = "Assets/LastSignal/Scenes/Validation/S013AssetComparison.unity";
        const string Vendor = "Assets/ThirdParty/EnvironmentAsset/";
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        [Serializable] public class MeshRecord { public string path, name; public int vertices, submeshes; public long triangles; public bool uv, normals, tangents; public Vector3 size; }
        [Serializable] public class PrefabRecord { public string path; public Vector3 size, rootScale; public int renderers, colliders, lodGroups, missingScripts, missingMaterials; public string[] shaders; }
        [Serializable] public class TextureRecord { public string path, format; public int width, height, maxSize; public bool mipmaps, srgb; public long runtimeBytes; }
        [Serializable] public class Audit { public string utc, unity, pipeline; public List<MeshRecord> meshes=new List<MeshRecord>(); public List<PrefabRecord> prefabs=new List<PrefabRecord>(); public List<TextureRecord> textures=new List<TextureRecord>(); }
        public static void AuditAssets(string output)
        {
            var a=new Audit { utc=DateTime.UtcNow.ToString("O"), unity=Application.unityVersion, pipeline=GraphicsSettings.currentRenderPipeline.name };
            foreach(var p in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith(Vendor,StringComparison.Ordinal) && new[]{".fbx",".prefab",".png",".tga",".tif",".jpeg"}.Contains(Path.GetExtension(p).ToLowerInvariant())))
            {
                foreach(var mesh in AssetDatabase.LoadAllAssetsAtPath(p).OfType<Mesh>())
                {
                    long tris=0; for(int i=0;i<mesh.subMeshCount;i++) if(mesh.GetTopology(i)==MeshTopology.Triangles) tris+=(long)mesh.GetIndexCount(i)/3;
                    a.meshes.Add(new MeshRecord { path=p,name=mesh.name,vertices=mesh.vertexCount,submeshes=mesh.subMeshCount,triangles=tris,size=mesh.bounds.size,uv=mesh.HasVertexAttribute(VertexAttribute.TexCoord0),normals=mesh.HasVertexAttribute(VertexAttribute.Normal),tangents=mesh.HasVertexAttribute(VertexAttribute.Tangent) });
                }
                if(p.EndsWith(".prefab",StringComparison.OrdinalIgnoreCase)||p.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase))
                {
                    var g=AssetDatabase.LoadAssetAtPath<GameObject>(p); if(!g)continue;
                    var renderers=g.GetComponentsInChildren<Renderer>(true); var bounds=BoundsOf(g);
                    a.prefabs.Add(new PrefabRecord { path=p,size=bounds.size,rootScale=g.transform.localScale,renderers=renderers.Length,colliders=g.GetComponentsInChildren<Collider>(true).Length,lodGroups=g.GetComponentsInChildren<LODGroup>(true).Length,missingScripts=g.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),missingMaterials=renderers.Sum(r=>r.sharedMaterials.Count(m=>!m)),shaders=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m).Select(m=>m.shader?m.shader.name:"MISSING").Distinct().ToArray() });
                }
                var tx=AssetDatabase.LoadAssetAtPath<Texture2D>(p); if(tx){var ti=AssetImporter.GetAtPath(p) as TextureImporter;a.textures.Add(new TextureRecord{path=p,width=tx.width,height=tx.height,format=tx.format.ToString(),maxSize=ti?ti.maxTextureSize:0,mipmaps=ti&&ti.mipmapEnabled,srgb=ti&&ti.sRGBTexture,runtimeBytes=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tx)});}
            }
            File.WriteAllText(output,JsonUtility.ToJson(a,true));
        }
        static Bounds BoundsOf(GameObject g)
        {
            var lod=g.GetComponentInChildren<LODGroup>();
            var rs=lod&&lod.lodCount>0?lod.GetLODs()[0].renderers:g.GetComponentsInChildren<Renderer>(true); var b=rs.Length>0?rs[0].bounds:new Bounds(g.transform.position,Vector3.zero); foreach(var r in rs)b.Encapsulate(r.bounds);return b;
        }
        static Material Normalize(Material source)
        {
            if(!source)throw new InvalidOperationException("Missing source material");
            var key=AssetDatabase.GetAssetPath(source)+"/"+source.name;
            if(Materials.TryGetValue(key,out var cached))return cached;
            var path=Root+"/Materials/"+source.name+"_"+Hash128.Compute(key)+".mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing){Materials[key]=existing;return existing;}
            bool urp=source.shader&&source.shader.name.StartsWith("Universal Render Pipeline/");
            var m=urp?new Material(source):new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.name=source.name+"_Evaluation";
            if(!urp)
            {
                var tex=source.HasProperty("_MainTex")?source.GetTexture("_MainTex"):null;
                m.SetTexture("_BaseMap",tex);m.SetColor("_BaseColor",Color.white);
                if(source.HasProperty("_MainTex")){m.SetTextureScale("_BaseMap",source.GetTextureScale("_MainTex"));m.SetTextureOffset("_BaseMap",source.GetTextureOffset("_MainTex"));}
                m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",0.12f);
                var n=source.name.ToLowerInvariant(); bool alpha=n.Contains("leaves")||n=="plant"||n.Contains("leaf");
                m.SetFloat("_AlphaClip",alpha?1:0);m.SetFloat("_Cutoff",0.4f);m.SetFloat("_Cull",alpha?0:2);
                if(alpha){m.EnableKeyword("_ALPHATEST_ON");m.SetOverrideTag("RenderType","TransparentCutout");m.renderQueue=2450;}
                if(source.HasProperty("_BumpMap")&&source.GetTexture("_BumpMap")){m.SetTexture("_BumpMap",source.GetTexture("_BumpMap"));m.EnableKeyword("_NORMALMAP");}
            }
            m.enableInstancing=true;AssetDatabase.CreateAsset(m,path);Materials[key]=m;return m;
        }
        static Material Solid(string name,Color color)
        {
            var p=Root+"/Materials/"+name+".mat"; var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(m)return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",0.1f);AssetDatabase.CreateAsset(m,p);return m;
        }
        static GameObject Place(string path,Transform bay,Vector3 position,bool normalize=true)
        {
            var src=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!src)throw new InvalidOperationException("Missing candidate "+path);
            var g=UnityEngine.Object.Instantiate(src,bay);g.name=Path.GetFileNameWithoutExtension(path);g.transform.localPosition=position;
            // Keep import scale and orientation, make the floor contact explicit.
            var b=BoundsOf(g);g.transform.position+=new Vector3(bay.position.x+position.x-b.center.x,-b.min.y,bay.position.z+position.z-b.center.z);
            foreach(var c in g.GetComponentsInChildren<MonoBehaviour>(true))if(c)UnityEngine.Object.DestroyImmediate(c);
            foreach(var c in g.GetComponentsInChildren<Camera>(true))UnityEngine.Object.DestroyImmediate(c);
            foreach(var c in g.GetComponentsInChildren<Light>(true))UnityEngine.Object.DestroyImmediate(c);
            if(normalize)foreach(var r in g.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(Normalize).ToArray();
            return g;
        }
        static void Cube(string name,Transform parent,Vector3 pos,Vector3 size,Material mat)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;
        }
        static Transform Bay(string name,float x)
        {
            var g=new GameObject(name);g.transform.position=new Vector3(x,0,0);
            Cube("Neutral ground",g.transform,new Vector3(0,-0.1f,0),new Vector3(36,0.2f,32),Solid("NeutralGround",new Color(.28f,.31f,.32f)));
            Cube("1.8m height reference",g.transform,new Vector3(-8,.9f,-4),new Vector3(.6f,1.8f,.35f),Solid("ScaleAmber",new Color(.64f,.39f,.12f)));
            return g.transform;
        }
        static void GroundSample(Transform bay,string texture)
        {
            var m=Solid(bay.name+"Ground",Color.white);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture));m.SetTextureScale("_BaseMap",new Vector2(2,2));EditorUtility.SetDirty(m);
            Cube("Ground texture swatch 4m",bay,new Vector3(-4,.025f,-5),new Vector3(4,.05f,4),m);
        }
        [MenuItem("Last Signal/S013/Create isolated asset comparison")]
        public static void CreateComparison()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first");
            if(File.Exists(ScenePath))throw new InvalidOperationException("Comparison exists; refusing overwrite");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene; preserve manual work first");
            Directory.CreateDirectory(Root+"/Materials");AssetDatabase.Refresh();
            var previous=SceneManager.GetActiveScene();var s=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(s);
            RenderSettings.skybox=null;RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.48f,.5f,.53f);
            var sun=new GameObject("Fixed comparison sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.3f;sun.color=new Color(1,.96f,.89f);sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(48,-35,0);RenderSettings.sun=sun;
            var volume=new GameObject("Fixed ACES exposure 0").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=ScriptableObject.CreateInstance<VolumeProfile>();var tone=volume.sharedProfile.Add<Tonemapping>();tone.mode.Override(TonemappingMode.ACES);AssetDatabase.CreateAsset(volume.sharedProfile,Root+"/ComparisonVolume.asset");
            var a=Bay("A Silver Cats",0);string ap=Vendor+"Silver_Cats/Hand_Painted_Nature_Kit_LITE/";
            Place(ap+"Prefabs/Pine_Tree.prefab",a,new Vector3(-4,0,3));Place(ap+"Prefabs/Cedar_Tree_03.prefab",a,new Vector3(5,0,4));Place(ap+"Prefabs/Fern.prefab",a,new Vector3(3,0,-5));Place(ap+"Prefabs/Stump.prefab",a,new Vector3(6,0,-5));GroundSample(a,ap+"Textures/Forest/Forest_Grass_02_d.png");
            var b=Bay("B Fantasy Landscape",50);string bp=Vendor+"FantasyEnvironments/Environments/";
            Place(bp+"Ambient-Occlusion-Trees/Prefabs/Pine_tree1.prefab",b,new Vector3(-4,0,3));Place(bp+"Ambient-Occlusion-Trees/Prefabs/Birch_tree1.prefab",b,new Vector3(5,0,4));Place(bp+"Prefabs/Bush1.prefab",b,new Vector3(3,0,-5));Place(bp+"Prefabs/Rock1.prefab",b,new Vector3(6,0,-5));
            var ground=AssetDatabase.FindAssets("t:TerrainLayer",new[]{bp}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<TerrainLayer>).FirstOrDefault(t=>t.diffuseTexture);if(ground)GroundSample(b,AssetDatabase.GetAssetPath(ground.diffuseTexture));
            var c=Bay("C Holotna",100);string cp=Vendor+"HolotnaMountainURP/Prefabs/";
            Place(cp+"Tree01A.prefab",c,new Vector3(-4,0,3),false);Place(cp+"Bush01.prefab",c,new Vector3(3,0,-5),false);Place(cp+"Rock01.prefab",c,new Vector3(6,0,-5),false);Place(cp+"Bridge01.prefab",c,new Vector3(5,0,7),false);
            var d=Bay("D Barn geometry - materials pending",150);Place(Vendor+"BarnExtracted/barn_1.FBX",d,Vector3.zero);
            var e=Bay("E Cupboard source identity pending",200);Place(Vendor+"cupboard_pack.fbx",e,Vector3.zero);
            foreach(var bay in new[]{a,b,c,d,e})Place("Assets/LastSignal/Prefabs/Enemies/Zombie/LS_Zombie_Shirtless_Visual.prefab",bay,new Vector3(-7,0,-2),false);
            var camera=new GameObject("ComparisonCamera 1920x1080 FOV50").AddComponent<Camera>();camera.fieldOfView=50;camera.nearClipPlane=.1f;camera.farClipPlane=90;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.32f,.38f,.42f);camera.transform.position=new Vector3(0,8,-25);camera.transform.LookAt(new Vector3(0,4,2));camera.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(s,ScenePath);SceneManager.SetActiveScene(previous);
            AddReferenceDisplay();CleanReferenceDisplay();GroundCandidates();
        }
        public static void AddReferenceDisplay()
        {
            var s=SceneManager.GetSceneByPath(ScenePath);if(!s.isLoaded||s.isDirty)throw new InvalidOperationException("Open clean comparison first");
            var previous=SceneManager.GetActiveScene();SceneManager.SetActiveScene(s);
            foreach(var bay in s.GetRootGameObjects().Where(g=>g.name.StartsWith("A ")||g.name.StartsWith("B ")||g.name.StartsWith("C ")||g.name.StartsWith("D ")||g.name.StartsWith("E ")))
            {
                if(bay.transform.Find("FPSArms"))continue;
                Place("Assets/LastSignal/Prefabs/Combat/FPSArms.prefab",bay.transform,new Vector3(-3,0,-8),false);
                Place("Assets/LastSignal/Prefabs/Combat/MRPoly/World/Assault Rifle (Black).prefab",bay.transform,new Vector3(-1,0,-8),false);
                var zombie=bay.transform.Find("LS_Zombie_Shirtless_Visual");if(zombie)zombie.localPosition+=new Vector3(0,0,-5);
                var rock=bay.transform.Find("Rock1");if(rock)rock.localPosition+=new Vector3(4,0,3);
            }
            var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(new Color(.35f,.38f,.42f));RenderSettings.ambientProbe=probe;
            EditorSceneManager.SaveScene(s);SceneManager.SetActiveScene(previous);
        }
        public static void CleanReferenceDisplay()
        {
            var s=SceneManager.GetSceneByPath(ScenePath);if(!s.isLoaded||s.isDirty)throw new InvalidOperationException("Open clean comparison first");
            foreach(var root in s.GetRootGameObjects())
            {
                var hands=root.transform.Find("FPSArms");if(!hands)continue;
                foreach(var r in hands.GetComponentsInChildren<Renderer>(true))if(r.name=="Environment")UnityEngine.Object.DestroyImmediate(r.gameObject);
                var rs=hands.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                hands.position+=root.transform.position+new Vector3(-3,.95f,-8)-b.center;
            }
            EditorSceneManager.SaveScene(s);
        }
        public static void GroundCandidates()
        {
            var s=SceneManager.GetSceneByPath(ScenePath);if(!s.isLoaded||s.isDirty)throw new InvalidOperationException("Open clean comparison first");
            foreach(var root in s.GetRootGameObjects())foreach(Transform child in root.transform)
            {
                if(!child.GetComponentInChildren<LODGroup>())continue;
                var b=BoundsOf(child.gameObject);child.position-=Vector3.up*b.min.y;
            }
            EditorSceneManager.SaveScene(s);
        }
        static void WriteFrame(Camera camera,string path,Vector3 position,Vector3 target)
        {
            if(File.Exists(path))throw new IOException("Evidence exists "+path);
            camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
            var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);var old=RenderTexture.active;Texture2D tx=null;
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tx=new Texture2D(1920,1080,TextureFormat.RGB24,false);tx.ReadPixels(new Rect(0,0,1920,1080),0,0);tx.Apply();File.WriteAllBytes(path,tx.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=old;if(tx)UnityEngine.Object.DestroyImmediate(tx);UnityEngine.Object.DestroyImmediate(rt);}
        }
        public static void Capture(string output)
        {
            Directory.CreateDirectory(output);
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve unsaved work before capture");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
                var camera=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).Single();camera.farClipPlane=100;
                var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(new Color(.35f,.38f,.42f));RenderSettings.ambientProbe=probe;
                var bays=s.GetRootGameObjects().Where(g=>g.name.Length>2&&g.name[1]==' ').ToArray();
                foreach(var pair in new[]{("A",0f),("B",50f),("C",100f),("D",150f),("E",200f)})
                {
                    foreach(var bay in bays)bay.SetActive(bay.name.StartsWith(pair.Item1+" "));
                    WriteFrame(camera,Path.Combine(output,pair.Item1+"-full.png"),new Vector3(pair.Item2,15,-45),new Vector3(pair.Item2,12,2));
                    WriteFrame(camera,Path.Combine(output,pair.Item1+"-detail.png"),new Vector3(pair.Item2,3,-16),new Vector3(pair.Item2,1,-4));
                }
                File.WriteAllText(Path.Combine(output,"capture-settings.json"),"{\"width\":1920,\"height\":1080,\"fov\":50,\"tonemapping\":\"ACES\",\"postExposure\":0,\"sunIntensity\":1.3,\"fullPosition\":[0,15,-45],\"fullTarget\":[0,12,2],\"detailPosition\":[0,3,-16],\"detailTarget\":[0,1,-4],\"bayOffsetX\":[0,50,100,150,200]}");
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
