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
using UnityEditor.Build.Reporting;
using Unity.AI.Navigation;
using LastSignal.Persistence;
using LastSignal.WorldTime;
using Object=UnityEngine.Object;

namespace LastSignal.Art.Editor
{
    public static class S013ProductionAuthoring
    {
        public const string ScenePath="Assets/LastSignal/Scenes/Production/S013Cabin.unity";
        public const string Root="Assets/LastSignal/Art/S013/Production";
        const string Source="Assets/ThirdParty/EnvironmentAsset/FantasyEnvironments/Environments/";
        static readonly Vector3 Origin=new Vector3(-360,0,-140);
        static Dictionary<string,Material> materials=new Dictionary<string,Material>();
        static Dictionary<string,GameObject> prefabs=new Dictionary<string,GameObject>();
        static Color Hex(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}
        static Material Mat(string name, string color, float smoothness, float metallic=0)
        {
            var path=Root+"/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.SetColor("_BaseColor",Hex(color));m.SetFloat("_Smoothness",smoothness);m.SetFloat("_Metallic",metallic);m.enableInstancing=true;AssetDatabase.CreateAsset(m,path);}
            materials[name]=m;return m;
        }
        // Original, tileable painterly surface textures. No copied promotional artwork.
        static void Surface(string name,int mode)
        {
            string path=Root+"/Textures/"+name+".png";
            if(!File.Exists(path))
            {
                const int n=512;var t=new Texture2D(n,n,TextureFormat.RGB24,false);var px=new Color[n*n];
                for(int y=0;y<n;y++)for(int x=0;x<n;x++)
                {
                    float u=x/(float)n,v=y/(float)n;
                    float broad=Mathf.Sin(u*Mathf.PI*8+Mathf.Sin(v*Mathf.PI*2)*.65f)*.035f+Mathf.Sin(v*Mathf.PI*6)*.035f;
                    float brush=Mathf.Sin(u*Mathf.PI*58+Mathf.Sin(v*Mathf.PI*4)*2)*.016f+Mathf.Sin(u*Mathf.PI*114)*.009f;
                    float grain=mode==0?brush:Mathf.Sin(v*Mathf.PI*18+Mathf.Sin(u*Mathf.PI*4))*.015f;
                    float f=.72f+broad+grain;
                    if(mode==2)f=.70f+Mathf.Sin(u*Mathf.PI*32)*.018f+broad;
                    px[y*n+x]=new Color(f*1.03f,f,f*.94f);
                }
                t.SetPixels(px);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path);
                var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.sRGBTexture=true;ti.mipmapEnabled=true;ti.wrapMode=TextureWrapMode.Repeat;ti.maxTextureSize=512;ti.anisoLevel=4;ti.SaveAndReimport();
            }
            materials[name].SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));EditorUtility.SetDirty(materials[name]);
        }
        static GameObject Box(string name,Transform parent,Vector3 position,Vector3 size,Material m,bool collision=false)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;
            if(!collision)Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }
        static GameObject Group(string name,Transform parent=null){var g=new GameObject(name);if(parent)g.transform.SetParent(parent,false);return g;}
        static void Beam(string name,Transform parent,Vector3 a,Vector3 b,float width,float depth,Material m)
        {var g=Box(name,parent,(a+b)*.5f,new Vector3(width,(b-a).magnitude,depth),m);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
        static void Boards(Transform root, float from,float to,float bottom,float top,float z,Material m)
        {
            int count=Mathf.CeilToInt((to-from)/.2f);float w=(to-from)/count;
            for(int i=0;i<count;i++)Box("Weathered board",root,new Vector3(from+(i+.5f)*w,(top+bottom)*.5f,z),new Vector3(w-.008f,top-bottom,.038f),m);
        }
        static void Panel(Transform root,float x,float y,float w,float h)
        {
            Box("Wall core",root,new Vector3(x,y,0),new Vector3(w,h,.22f),materials["InteriorPlaster"],true);
            Boards(root,x-w/2,x+w/2,y-h/2,y+h/2,-.132f,materials["PaintedTimber"]);
            Box("Interior skirting",root,new Vector3(x,.10f,.135f),new Vector3(w,.18f,.065f),materials["DarkTimber"]);
        }
        static void Wall(string name,bool window,bool doorway)
        {
            var g=Group(name);float h=3.2f;
            if(!window&&!doorway)Panel(g.transform,1,h/2,2,h);
            else if(window)
            {
                Panel(g.transform,.2f,h/2,.4f,h);Panel(g.transform,1.8f,h/2,.4f,h);Panel(g.transform,1,.55f,1.2f,1.1f);Panel(g.transform,1,2.9f,1.2f,.6f);
                foreach(float x in new[]{.4f,1.6f})Box("Window jamb",g.transform,new Vector3(x,1.85f,-.19f),new Vector3(.10f,1.6f,.16f),materials["DarkTimber"]);
                foreach(float y in new[]{1.1f,2.6f})Box("Window rail",g.transform,new Vector3(1,y,-.19f),new Vector3(1.4f,.1f,.18f),materials["DarkTimber"]);
                Box("Opaque dusty glass",g.transform,new Vector3(1,1.85f,.015f),new Vector3(1.12f,1.4f,.025f),materials["DustyGlass"]);
                Box("Window mullion",g.transform,new Vector3(1,1.85f,-.21f),new Vector3(.06f,1.5f,.08f),materials["DarkTimber"]);
                Box("Window crossbar",g.transform,new Vector3(1,1.85f,-.21f),new Vector3(1.2f,.06f,.08f),materials["DarkTimber"]);
                Box("Window sill",g.transform,new Vector3(1,1.06f,-.28f),new Vector3(1.5f,.1f,.34f),materials["Wood"]);
            }
            else
            {
                Panel(g.transform,.175f,h/2,.35f,h);Panel(g.transform,1.825f,h/2,.35f,h);Panel(g.transform,1,2.8f,1.3f,.8f);
                foreach(float x in new[]{.35f,1.65f})Box("Door jamb",g.transform,new Vector3(x,1.2f,-.16f),new Vector3(.10f,2.4f,.14f),materials["DarkTimber"]);
                Box("Door lintel",g.transform,new Vector3(1,2.42f,-.16f),new Vector3(1.5f,.14f,.15f),materials["DarkTimber"]);
            }
            Box("Crown beam",g.transform,new Vector3(1,3.14f,-.08f),new Vector3(2,.16f,.35f),materials["DarkTimber"]);
            SaveCombined(g,name,true);
        }
        static void SaveCombined(GameObject g,string name,bool retainColliders)
        {
            // Merge by material. Decorative detail does not become hundreds of scene renderers.
            var filters=g.GetComponentsInChildren<MeshFilter>();var grouped=filters.GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial).ToArray();
            foreach(var group in grouped)
            {
                var mesh=new Mesh {name=name+"_"+group.Key.name,indexFormat=IndexFormat.UInt32};
                var parts=group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=g.transform.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray();mesh.CombineMeshes(parts,true,true);mesh.RecalculateBounds();
                // Planar world-density mapping per normal; 2m texture covers 512 pixels.
                var v=mesh.vertices;var ns=mesh.normals;var uv=new Vector2[v.Length];for(int i=0;i<v.Length;i++){var n=ns[i];uv[i]=Mathf.Abs(n.y)>.5f?new Vector2(v[i].x,v[i].z)*.5f:Mathf.Abs(n.x)>.5f?new Vector2(v[i].z,v[i].y)*.5f:new Vector2(v[i].x,v[i].y)*.5f;}mesh.uv=uv;mesh.RecalculateTangents();
                AssetDatabase.CreateAsset(mesh,Root+"/Meshes/"+mesh.name+".asset");var child=Group(group.Key.name,g.transform);child.AddComponent<MeshFilter>().sharedMesh=mesh;child.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
            }
            foreach(var f in filters)
            {
                var col=f.GetComponent<Collider>();if(retainColliders&&col){Object.DestroyImmediate(f.GetComponent<Renderer>());Object.DestroyImmediate(f);}else Object.DestroyImmediate(f.gameObject);
            }
            var p=PrefabUtility.SaveAsPrefabAsset(g,Root+"/Prefabs/"+name+".prefab");prefabs[name]=p;Object.DestroyImmediate(g);
        }
        static void BuildKit()
        {
            var filler=Group("Wall_1m");Panel(filler.transform,.5f,1.6f,1,3.2f);SaveCombined(filler,filler.name,true);
            Wall("Wall_2m",false,false);Wall("Window_2m",true,false);Wall("Doorway_2m",false,true);
            foreach(bool outer in new[]{false,true}){var g=Group(outer?"Corner_Outer":"Corner_Inner");Box("Corner post",g.transform,new Vector3(0,1.6f,0),new Vector3(.28f,3.2f,.28f),materials["DarkTimber"],true);Box("Corner facing",g.transform,new Vector3(.08f,1.6f,-.09f),new Vector3(.32f,3.2f,.07f),materials["PaintedTimber"]);SaveCombined(g,g.name,true);}
            var floor=Group("Floor_2m");for(int i=0;i<8;i++)Box("Floor plank",floor.transform,new Vector3((i+.5f)*.25f, -.055f,1),new Vector3(.244f,.11f,2),materials["Wood"]);var floorCol=Box("Floor collision",floor.transform,new Vector3(1,-.09f,1),new Vector3(2,.12f,2),materials["Wood"],true);SaveCombined(floor,floor.name,true);
            var roof=Group("Roof_2m");Box("Roof sheet",roof.transform,new Vector3(1,0,1.9f),new Vector3(2,.08f,3.8f),materials["RoofMetal"]);for(int i=0;i<=8;i++)Box("Standing seam",roof.transform,new Vector3(i*.25f,.065f,1.9f),new Vector3(.028f,.085f,3.8f),materials["RoofMetal"]);SaveCombined(roof,roof.name,false);
            var door=Group("Door_1_2m");Boards(door.transform,0,1.2f,0,2.3f,0,materials["Wood"]);foreach(float y in new[]{.22f,1.9f})Box("Brace",door.transform,new Vector3(.6f,y,-.045f),new Vector3(1.15f,.10f,.09f),materials["DarkTimber"]);Beam("Diagonal brace",door.transform,new Vector3(.1f,.3f,-.09f),new Vector3(1.1f,1.9f,-.09f),.09f,.07f,materials["DarkTimber"]);foreach(float y in new[]{.25f,1.92f})Box("Hinge",door.transform,new Vector3(.12f,y,-.11f),new Vector3(.22f,.09f,.04f),materials["Iron"]);Box("Latch",door.transform,new Vector3(1.02f,1.12f,-.13f),new Vector3(.055f,.22f,.05f),materials["Iron"]);SaveCombined(door,door.name,false);
        }
        static GameObject Instance(string name,Transform parent,Vector3 p,Vector3 e,bool collider=false)
        {
            var prefab=prefabs.ContainsKey(name)?prefabs[name]:AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+name+".prefab");var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);g.transform.localPosition=p;g.transform.localRotation=Quaternion.Euler(e);
            if(!collider)foreach(var c in g.GetComponentsInChildren<Collider>())c.enabled=false;return g;
        }
        static void Label(string name,string text,Transform parent,Vector3 p,Vector3 rotation,float size,Color color)
        {
            var g=Group(name,parent);g.transform.localPosition=p;g.transform.localEulerAngles=rotation;var tm=g.AddComponent<TextMesh>();tm.text=text;tm.fontSize=64;tm.characterSize=size;tm.anchor=TextAnchor.MiddleCenter;tm.color=color;
        }
        static void BuildCabin(Transform root)
        {
            // Local origin is cabin floor center, not the historic shelter root.
            for(int i=0;i<3;i++)
            {
                Instance(i==1?"Window_2m":"Wall_2m",root,new Vector3(-3+i*2,0,-3),Vector3.zero);
                Instance(i==1?"Window_2m":"Wall_2m",root,new Vector3(3-i*2,0,3),new Vector3(0,180,0));
                Instance("Wall_2m",root,new Vector3(-3,0,3-i*2),new Vector3(0,90,0));

            }
            Instance("Wall_1m",root,new Vector3(2.95f,0,-3),new Vector3(0,-90,0));
            Instance("Window_2m",root,new Vector3(2.95f,0,-2),new Vector3(0,-90,0));
            Instance("Doorway_2m",root,new Vector3(2.95f,0,0),new Vector3(0,-90,0));
            Instance("Wall_1m",root,new Vector3(2.95f,0,2),new Vector3(0,-90,0));
            // Entry terminal stays authoritative, with a visibly closed leaf at z=+1.
            Instance("Door_1_2m",root,new Vector3(2.98f,.04f,.4f),new Vector3(0,-90,0));
            foreach(float x in new[]{-3f,3f})foreach(float z in new[]{-3f,3f})Instance("Corner_Outer",root,new Vector3(x,0,z),Vector3.zero);
            for(int x=0;x<3;x++)for(int z=0;z<3;z++)Instance("Floor_2m",root,new Vector3(-3+x*2,.018f,-3+z*2),Vector3.zero);
            // Ridge runs east-west; each slope shares exact endpoints.
            for(int x=0;x<3;x++)
            {
                Instance("Roof_2m",root,new Vector3(-3+x*2,4.8f,0),new Vector3(28,0,0));
                Instance("Roof_2m",root,new Vector3(-1+x*2,4.8f,0),new Vector3(28,180,0));
            }
            var details=Group("Cabin structural detail",root);
            foreach(float x in new[]{-3.04f,3.04f})
            {
                // Stepped infill follows the roof slope; fine board rhythm avoids a flat primitive gable.
                for(int j=0;j<30;j++){float z=-3+(j+.5f)*.2f;float top=4.76f-Mathf.Abs(z)*Mathf.Tan(28*Mathf.Deg2Rad);Box("Gable board",details.transform,new Vector3(x,(top+3.15f)/2,z),new Vector3(.07f,Mathf.Max(.03f,top-3.15f),.194f),materials["PaintedTimber"]);}
                Beam("Gable fascia",details.transform,new Vector3(x,3.08f,-3.25f),new Vector3(x,4.84f,0),.16f,.16f,materials["DarkTimber"]);Beam("Gable fascia",details.transform,new Vector3(x,4.84f,0),new Vector3(x,3.08f,3.25f),.16f,.16f,materials["DarkTimber"]);
            }
            Box("Ridge cap",details.transform,new Vector3(0,4.9f,0),new Vector3(6.3f,.13f,.22f),materials["Iron"]);
            foreach(float z in new[]{-3.36f,3.36f})Box("Rain gutter",details.transform,new Vector3(0,3.02f,z),new Vector3(6.5f,.13f,.13f),materials["Iron"]);
            for(int i=0;i<12;i++)foreach(float z in new[]{-3.05f,3.05f})Box("Stone footing",details.transform,new Vector3(-2.75f+i*.5f,-.1f,z),new Vector3(.47f,.35f,.35f),materials[i%3==0?"StoneDark":"Stone"]);
            foreach(float x in new[]{-3.05f,3.05f})for(int i=0;i<12;i++)Box("Stone footing",details.transform,new Vector3(x,-.1f,-2.75f+i*.5f),new Vector3(.35f,.35f,.47f),materials[i%3==0?"StoneDark":"Stone"]);
            // Porch boards remain at existing ground level; no hidden step at transition anchors.
            for(int i=0;i<26;i++)Box("Porch plank",details.transform,new Vector3(4.3f,-.025f,-3.25f+i*.25f),new Vector3(2.7f,.045f,.239f),materials["Wood"]);
            foreach(float z in new[]{-3.2f,3.2f})
            {Box("Porch post",details.transform,new Vector3(5.5f,1.5f,z),new Vector3(.16f,3,.16f),materials["DarkTimber"]);Beam("Porch brace",details.transform,new Vector3(5.5f,2.2f,z),new Vector3(4.8f,2.95f,z),.13f,.13f,materials["DarkTimber"]);}
            Box("Porch header",details.transform,new Vector3(5.5f,2.98f,0),new Vector3(.2f,.22f,6.6f),materials["DarkTimber"]);
            var awning=Box("Porch canopy",details.transform,new Vector3(4.32f,3.2f,0),new Vector3(2.9f,.10f,6.7f),materials["RoofMetal"]);awning.transform.localEulerAngles=new Vector3(0,0,-7);
            Box("Chimney",details.transform,new Vector3(-1.9f,4.1f,1.8f),new Vector3(.55f,2.2f,.55f),materials["StoneDark"]);Box("Chimney cap",details.transform,new Vector3(-1.9f,5.25f,1.8f),new Vector3(.75f,.15f,.75f),materials["Iron"]);
            Box("Station sign back",details.transform,new Vector3(3.17f,2.75f,-.6f),new Vector3(.06f,.40f,2.1f),materials["DarkTimber"]);
            SaveCombined(details,"Cabin_Detail",false);Instance("Cabin_Detail",root,Vector3.zero,Vector3.zero);
            Label("Station identity","FIELD STATION  07",root,new Vector3(3.22f,2.76f,-.6f),new Vector3(0,90,0),.065f,Hex("#D7CEAA"));
        }
        static void BuildInterior(Transform root)
        {
            var g=Group("Interior craft detail",root);
            // Keep radio/socket and spawn clearance. Render-only furniture follows existing blockers.
            Box("Storage frame",g.transform,new Vector3(-1.55f,.65f,0),new Vector3(.5f,1.3f,1.6f),materials["DarkTimber"]);
            for(int i=0;i<4;i++){Box("Storage drawer",g.transform,new Vector3(-1.275f,.18f+i*.30f,0),new Vector3(.04f,.275f,1.48f),materials["Wood"]);Box("Drawer pull",g.transform,new Vector3(-1.23f,.18f+i*.30f,0),new Vector3(.04f,.035f,.28f),materials["Iron"]);}
            Box("Radio desk surface",g.transform,new Vector3(0,.89f,0),new Vector3(1.15f,.09f,.68f),materials["Wood"]);
            foreach(float x in new[]{-.48f,.48f})foreach(float z in new[]{-.25f,.25f})Box("Desk leg",g.transform,new Vector3(x,.42f,z),new Vector3(.065f,.84f,.065f),materials["Iron"]);
            Box("Radio shell",g.transform,new Vector3(0,1.2f,0),new Vector3(.52f,.49f,.51f),materials["Iron"]);
            Box("Radio grille",g.transform,new Vector3(0,1.2f,-.263f),new Vector3(.39f,.3f,.02f),materials["DarkTimber"]);
            for(int i=0;i<9;i++)Box("Radio grille slat",g.transform,new Vector3(-.17f+i*.04f,1.2f,-.28f),new Vector3(.011f,.27f,.016f),materials["Metal"]);
            Box("Radio frequency window",g.transform,new Vector3(.08f,1.36f,-.28f),new Vector3(.19f,.055f,.02f),materials["AmberGlass"]);
            Beam("Radio antenna",g.transform,new Vector3(.18f,1.45f,0),new Vector3(.22f,2.0f,.1f),.012f,.012f,materials["Metal"]);
            Box("Bed frame",g.transform,new Vector3(0,.27f,2),new Vector3(1.6f,.16f,1.5f),materials["DarkTimber"]);
            Box("Wool blanket",g.transform,new Vector3(0,.42f,1.9f),new Vector3(1.47f,.17f,1.22f),materials["Cloth"]);Box("Pillow",g.transform,new Vector3(0,.56f,2.4f),new Vector3(.74f,.18f,.30f),materials["Canvas"]);
            // Shallow north shelf stays away from workbench/radio sightlines.
            foreach(float y in new[]{1.4f,2.2f})Box("Wall shelf",g.transform,new Vector3(1.8f,y,2.68f),new Vector3(1.4f,.09f,.35f),materials["Wood"]);
            for(int i=0;i<5;i++)Box("Stored notebook",g.transform,new Vector3(1.35f+i*.12f,1.58f,2.68f),new Vector3(.065f,.27f,.19f),materials[i%2==0?"Cloth":"Canvas"]);
            Box("Stove",g.transform,new Vector3(-2.45f,.48f,2.4f),new Vector3(.55f,.9f,.5f),materials["Iron"]);Box("Stove front",g.transform,new Vector3(-2.45f,.5f,2.12f),new Vector3(.37f,.40f,.03f),materials["Rust"]);Box("Stove pipe",g.transform,new Vector3(-2.45f,2.05f,2.4f),new Vector3(.14f,2.2f,.14f),materials["Iron"]);
            // Rafters and small wear marks are structural, not interaction blockers.
            foreach(float x in new[]{-2.8f,-1.4f,0,1.4f,2.8f})Box("Ceiling joist",g.transform,new Vector3(x,3.0f,0),new Vector3(.13f,.16f,5.9f),materials["DarkTimber"]);
            SaveCombined(g,"Cabin_Interior",false);Instance("Cabin_Interior",root,Vector3.zero,Vector3.zero);
        }
        static Material Normalize(Material source)
        {
            string key="B_"+source.name;var path=Root+"/Materials/"+key+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=key;m.SetTexture("_BaseMap",source.HasProperty("_MainTex")?source.GetTexture("_MainTex"):source.mainTexture);
            bool leaf=source.name.ToLowerInvariant().Contains("leaves")||source.name=="Plant";m.SetColor("_BaseColor",leaf?new Color(.64f,.72f,.68f):new Color(.8f,.80f,.78f));m.SetFloat("_Smoothness",leaf?.13f:.16f);m.SetFloat("_Metallic",0);m.enableInstancing=true;
            if(leaf){m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.42f);m.SetFloat("_Cull",0);m.EnableKeyword("_ALPHATEST_ON");m.SetOverrideTag("RenderType","TransparentCutout");m.renderQueue=2450;}
            AssetDatabase.CreateAsset(m,path);return m;
        }
        static GameObject NaturePrefab(string relative,string name)
        {
            var g=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source+relative));g.name=name;
            foreach(var r in g.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(Normalize).ToArray();
            foreach(var col in g.GetComponentsInChildren<Collider>())Object.DestroyImmediate(col);
            var lod=g.GetComponent<LODGroup>();if(!lod)lod=g.AddComponent<LODGroup>();
            // Conservative distance cull only; no fictitious reduced mesh is claimed.
            lod.SetLODs(new[]{new LOD(.018f,g.GetComponentsInChildren<Renderer>())});lod.RecalculateBounds();
            var prefab=PrefabUtility.SaveAsPrefabAsset(g,Root+"/Prefabs/"+name+".prefab");Object.DestroyImmediate(g);return prefab;
        }
        static void PlaceNature(GameObject prefab,Transform parent,Vector3 p,float scale,float yaw)
        {
            var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);g.transform.localPosition=p;g.transform.localRotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=Vector3.one*scale;
        }
        static void Exterior(Transform root)
        {
            var pine=NaturePrefab("Ambient-Occlusion-Trees/Prefabs/Pine_tree1.prefab","Pine");var birch=NaturePrefab("Ambient-Occlusion-Trees/Prefabs/Birch_tree1.prefab","Birch");var bush=NaturePrefab("Prefabs/Bush1.prefab","Bush");var rock=NaturePrefab("Prefabs/Rock1.prefab","Rock");var grass=NaturePrefab("Prefabs/Grass1.prefab","Grass");
            // Trees are behind/aside the shelter, away from both authored route centerlines.
            var positions=new[]{new Vector3(-12,0,-8),new Vector3(-17,0,5),new Vector3(-7,0,14),new Vector3(6,0,15),new Vector3(15,0,13),new Vector3(20,0,-13),new Vector3(8,0,-17),new Vector3(-10,0,-19),new Vector3(-22,0,-12),new Vector3(-19,0,18),new Vector3(23,0,20),new Vector3(-27,0,2)};
            for(int i=0;i<positions.Length;i++)PlaceNature(i%3==0?birch:pine,root,positions[i],.48f+(i%4)*.065f,i*137.5f);
            for(int i=0;i<6;i++)PlaceNature(rock,root,new Vector3(-8-i%3*4,0,-10+i/3*21),.10f+(i%3)*.025f,i*59);
            for(int i=0;i<28;i++)
            {float angle=i*2.39996f;float radius=8+(i%4)*2.4f;var p=new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);if(p.x>2&&p.z>-4&&p.z<9)continue;PlaceNature(bush,root,p,.55f+(i%3)*.15f,i*73);}
            for(int i=0;i<72;i++){float angle=i*2.39996f;float radius=7+(i%9)*1.4f;var p=new Vector3(Mathf.Cos(angle)*radius,.025f,Mathf.Sin(angle)*radius);if(p.x>1&&p.z>-5&&p.z<8)continue;PlaceNature(grass,root,p,.75f+(i%4)*.2f,i*53);}
            var soil=Mat("GroundSoil","#939187",.06f);soil.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"Textures/ground_soil.png"));soil.SetTextureScale("_BaseMap",new Vector2(10,10));
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cylinder);ground.name="Cabin soil clearing";ground.transform.SetParent(root,false);ground.transform.localPosition=new Vector3(0,-.02f,0);ground.transform.localScale=new Vector3(44,.02f,39);ground.GetComponent<Renderer>().sharedMaterial=soil;Object.DestroyImmediate(ground.GetComponent<Collider>());
            var road=Mat("GroundPath","#96958B",.12f);road.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"Textures/ground_road.png"));road.SetTextureScale("_BaseMap",new Vector2(2,8));
            var path=Box("Entry path",root,new Vector3(12,.008f,1),new Vector3(18,.016f,3),road);path.transform.localEulerAngles=new Vector3(0,0,0);
            var props=Group("Exterior detail",root);
            // Low fence describes the clearing; no invisible collisions or mandatory route obstruction.
            for(int i=0;i<6;i++){float z=-10+i*1.2f;Box("Fence post",props.transform,new Vector3(-7,.65f,z),new Vector3(.12f,1.3f,.12f),materials["Wood"]);if(i<5)foreach(float y in new[]{.45f,.95f})Box("Fence rail",props.transform,new Vector3(-7,y,z+.6f),new Vector3(.07f,.12f,1.2f),materials["DarkTimber"]);}
            for(int i=0;i<6;i++)Box("Stacked firewood",props.transform,new Vector3(-2.4f+i%3*.42f,.18f+(i/3)*.25f,-3.55f),new Vector3(.38f,.23f,.6f),materials["Wood"]);
            Box("Porch lantern bracket",props.transform,new Vector3(3.4f,2.3f,2.2f),new Vector3(.4f,.05f,.06f),materials["Iron"]);Box("Porch lantern",props.transform,new Vector3(3.55f,2.05f,2.2f),new Vector3(.16f,.3f,.16f),materials["AmberGlass"]);
            SaveCombined(props,"Cabin_ExteriorProps",false);Instance("Cabin_ExteriorProps",root,Vector3.zero,Vector3.zero);
            var light=Group("Porch amber light",root).AddComponent<Light>();light.transform.localPosition=new Vector3(3.65f,2.1f,2.2f);light.type=LightType.Point;light.range=7;light.intensity=1.4f;light.color=Hex("#FFD19A");light.shadows=LightShadows.None;
        }
        public static void Author()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode");
            if(File.Exists(ScenePath)||Directory.Exists(Root))throw new InvalidOperationException("Production candidate exists; deliberate edits only, never regenerate");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve unsaved scene first");
            foreach(var folder in new[]{"Materials","Textures","Meshes","Prefabs"})Directory.CreateDirectory(Root+"/"+folder);AssetDatabase.Refresh();
            Mat("Wood","#9B8062",.16f);Mat("PaintedTimber","#788C87",.12f);Mat("DarkTimber","#4E4B3D",.12f);Mat("InteriorPlaster","#B4AB91",.1f);Mat("RoofMetal","#505F61",.27f,.45f);Mat("Iron","#303D3E",.32f,.6f);Mat("Metal","#707D7B",.38f,.75f);Mat("Rust","#8C503A",.10f);Mat("Stone","#797D72",.08f);Mat("StoneDark","#555F5C",.08f);Mat("DustyGlass","#536E72",.55f,.1f);Mat("Cloth","#6C7464",.07f);Mat("Canvas","#B3A383",.05f);var amber=Mat("AmberGlass","#DBAE62",.35f);amber.EnableKeyword("_EMISSION");amber.SetColor("_EmissionColor",Hex("#DDA254")*.8f);
            Surface("Wood",0);Surface("PaintedTimber",0);Surface("InteriorPlaster",1);Surface("RoofMetal",2);Surface("DarkTimber",0);Surface("Cloth",1);
            BuildKit();AssetDatabase.SaveAssets();
            if(!AssetDatabase.CopyAsset("Assets/LastSignal/Scenes/Production/IntegratedGraybox.unity",ScenePath))throw new IOException("Copy failed");
            var s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var shelter=GameObject.Find("Shelter / physically enclosed cabin");
            foreach(var r in shelter.GetComponentsInChildren<Renderer>())if(r.GetComponent<TextMesh>()==null)r.enabled=false;
            foreach(var name in new[]{"Shelter rest bed","cabin.radio.v1"}){var obj=GameObject.Find(name);if(obj&&obj.TryGetComponent<Renderer>(out var r))r.enabled=false;}
            // Keep old colliders and all gameplay components. Only graybox labels at this POI are suppressed.
            foreach(var tm in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))if(Vector3.Distance(tm.transform.position,Origin)<10)tm.GetComponent<Renderer>().enabled=false;
            var root=Group("S013 Production Cabin");root.transform.position=Origin;BuildCabin(root.transform);BuildInterior(root.transform);Exterior(root.transform);
            var light=shelter.GetComponentInChildren<Light>();light.color=Hex("#FFD4A1");light.intensity=2.2f;light.range=7;light.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.fog=true;RenderSettings.fogColor=Hex("#899B9D");RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.006f;
            var volume=Group("S013 fixed tonemapping").AddComponent<Volume>();volume.isGlobal=true;volume.priority=10;var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);profile.Add<ColorAdjustments>().saturation.Override(-14);AssetDatabase.CreateAsset(profile,Root+"/ProductionVolume.asset");volume.sharedProfile=profile;
            // Time and weather continue to own the sun/rain. This observer only supplies presentation defaults.
            root.AddComponent<LastSignal.Slice.S013EnvironmentPresentation>();
            var save=Object.FindAnyObjectByType<SaveSession>();var valid=save.ValidateAuthoring();if(!valid.Success)throw new InvalidOperationException(valid.Message);
            var cameras=Group("S013 Benchmark Cameras");
            string[] names={"Exterior","Approach","Interior","Interaction","Threat","Flashlight"};Vector3[] pos={new Vector3(17,5,-14),new Vector3(10,1.7f,1),new Vector3(1.9f,1.65f,-2),new Vector3(1.1f,1.55f,-1.5f),new Vector3(12,1.7f,4),new Vector3(1.9f,1.65f,1.6f)};Vector3[] targets={new Vector3(0,2,0),new Vector3(3,1.5f,1),new Vector3(-.5f,1.3f,1),new Vector3(0,1.2f,0),new Vector3(20,1,4),new Vector3(-1.5f,1.4f,-1)};
            for(int i=0;i<names.Length;i++){var c=Group(names[i],cameras.transform).AddComponent<Camera>();c.transform.position=Origin+pos[i];c.transform.LookAt(Origin+targets[i]);c.fieldOfView=65;c.nearClipPlane=.05f;c.farClipPlane=250;c.enabled=false;c.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;}
            EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();
        }
        public static void Build(string output,bool baseline=false,bool cleanCache=false)
        {
            if(Directory.Exists(output))throw new IOException("Immutable build path exists");Directory.CreateDirectory(output);
            var options=BuildOptions.Development | (cleanCache ? BuildOptions.CleanBuildCache : BuildOptions.None);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{baseline?"Assets/LastSignal/Scenes/Production/IntegratedGraybox.unity":ScenePath},locationPathName=Path.Combine(output,"LastSignal.app"),target=BuildTarget.StandaloneOSX,options=options});
            var warnings=report.steps?.SelectMany(step=>step.messages ?? Array.Empty<BuildStepMessage>())
                .Where(message=>message.type==LogType.Warning).Select(message=>message.content).ToArray() ?? Array.Empty<string>();
            File.WriteAllLines(Path.Combine(output,"warnings.txt"),warnings);
            File.WriteAllText(Path.Combine(output,"build.txt"),$"Result={report.summary.result}\nErrors={report.summary.totalErrors}\nWarnings={report.summary.totalWarnings}\nDuration={report.summary.totalTime}\nUnity={Application.unityVersion}\nScene={(baseline?"S012 baseline":ScenePath)}\nCleanCache={cleanCache}\n");if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Build failed");
        }
    }
}
