using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LastSignal.Shelter;
namespace LastSignal.Art.Editor
{
    public static class S013ProductionPolish
    {
        static readonly Vector3 O=new Vector3(-360,0,-140);
        static Material M(string name)=>AssetDatabase.LoadAssetAtPath<Material>(S013ProductionAuthoring.Root+"/Materials/"+name+".mat");
        static GameObject Box(string name,Transform root,Vector3 p,Vector3 size,Material m)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }
        public static void Apply()
        {
            var root=GameObject.Find("S013 Production Cabin");if(!root)throw new InvalidOperationException("Open S013 candidate");if(root.transform.Find("Production refinement"))throw new InvalidOperationException("Already refined");
            var detail=new GameObject("Production refinement").transform;detail.SetParent(root.transform,false);
            GameObject.Find("Station identity").transform.rotation=Quaternion.Euler(0,-90,0);
            GameObject.Find("Cabin soil clearing").transform.position=O+new Vector3(0,.025f,0);
            GameObject.Find("Entry path").transform.position=O+new Vector3(12,.057f,1);
            // Raise authored porch rendering above terrain without changing traversal collision.
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(S013ProductionAuthoring.Root+"/Meshes/Cabin_Detail_Wood.asset");var vs=mesh.vertices;for(int i=0;i<vs.Length;i++)if(vs[i].x>2.95f&&vs[i].y<.01f)vs[i].y+=.075f;mesh.vertices=vs;mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var terrainPath=S013ProductionAuthoring.Root+"/Materials/DistantGround.mat";var ground=new Material(M("GroundSoil"));ground.name="DistantGround";ground.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/EnvironmentAsset/FantasyEnvironments/Environments/Textures/ground_grass.png"));ground.SetColor("_BaseColor",new Color(.38f,.43f,.38f));ground.SetTextureScale("_BaseMap",new Vector2(180,180));AssetDatabase.CreateAsset(ground,terrainPath);GameObject.Find("S012 ground 400 x 400 m").GetComponent<Renderer>().sharedMaterial=ground;
            // Existing sockets keep identity and authority, gain purposeful furniture locations.
            var sockets=UnityEngine.Object.FindObjectsByType<ShelterSocket>(FindObjectsSortMode.None);
            foreach(var s in sockets)
            {
                var p=s.module==ShelterModule.Bed?new Vector3(.8f,1.35f,2.4f):s.module==ShelterModule.Storage?new Vector3(-.8f,1.35f,-.8f):new Vector3(.2f,1.35f,-2.4f);
                s.transform.position=O+p;s.GetComponent<Renderer>().enabled=false;
                Box(s.module+" service plate",detail,p,new Vector3(.05f,.23f,.28f),M("Rust"));
                Box(s.module+" bracket",detail,p+new Vector3(-.06f,-.20f,0),new Vector3(.08f,.22f,.10f),M("Iron"));
                var label=new GameObject(s.module+" label").AddComponent<TextMesh>();label.transform.SetParent(detail,false);label.transform.localPosition=p+new Vector3(.03f,0,0);label.transform.localEulerAngles=new Vector3(0,-90,0);label.text=s.module.ToString().ToUpperInvariant();label.characterSize=.032f;label.fontSize=64;label.anchor=TextAnchor.MiddleCenter;label.color=new Color(.87f,.80f,.64f);
            }
            Box("Workbench top",detail,new Vector3(.2f,.88f,-2.48f),new Vector3(1.5f,.10f,.70f),M("Wood"));foreach(float x in new[]{-.45f,.85f})foreach(float z in new[]{-2.22f,-2.73f})Box("Workbench leg",detail,new Vector3(x,.42f,z),new Vector3(.08f,.84f,.08f),M("Iron"));
            Box("Tool rail",detail,new Vector3(.2f,1.7f,-2.78f),new Vector3(1.4f,.06f,.06f),M("Iron"));
            for(int i=0;i<4;i++){Box("Hanging handtool",detail,new Vector3(-.3f+i*.27f,1.53f,-2.74f),new Vector3(.025f,.3f,.045f),M("Metal"));Box("Tool grip",detail,new Vector3(-.3f+i*.27f,1.36f,-2.74f),new Vector3(.045f,.13f,.05f),M("Wood"));}
            foreach(float z in new[]{-2.87f,2.87f})
            {foreach(float x in new[]{-.62f,.62f})Box("Interior window jamb",detail,new Vector3(x,1.85f,z),new Vector3(.09f,1.55f,.09f),M("Wood"));foreach(float y in new[]{1.09f,2.61f})Box("Interior window rail",detail,new Vector3(0,y,z),new Vector3(1.34f,.09f,.10f),M("Wood"));Box("Window crossbar",detail,new Vector3(0,1.85f,z),new Vector3(.045f,1.5f,.09f),M("Wood"));}
            Box("Interior ceiling",detail,new Vector3(0,3.12f,0),new Vector3(5.82f,.05f,5.82f),M("InteriorPlaster"));
            Box("Bed service support",detail,new Vector3(.74f,.95f,2.4f),new Vector3(.07f,.70f,.07f),M("DarkTimber"));
            Box("Storage service support",detail,new Vector3(-.87f,.72f,-.8f),new Vector3(.06f,1.15f,.06f),M("DarkTimber"));
            // Irregular low vegetation softens the clearing edge without changing navigation.
            var bush=AssetDatabase.LoadAssetAtPath<GameObject>(S013ProductionAuthoring.Root+"/Prefabs/Bush.prefab");for(int i=0;i<24;i++){float a=i*2.39996f;var p=new Vector3(Mathf.Cos(a)*21,0,Mathf.Sin(a)*18);if(p.x>8&&p.z>-3&&p.z<7)continue;var g=(GameObject)PrefabUtility.InstantiatePrefab(bush,detail);g.transform.localPosition=p;g.transform.localScale=Vector3.one*(.5f+i%3*.13f);g.transform.localRotation=Quaternion.Euler(0,i*57,0);}
            // Replace the periodic first-pass texture pattern with low-contrast brushed variation.
            foreach(var name in new[]{"Wood","PaintedTimber","InteriorPlaster","RoofMetal","DarkTimber","Cloth"})
            {
                const int n=512;var tx=new Texture2D(n,n,TextureFormat.RGB24,false);var px=new Color[n*n];bool timber=name.Contains("Timber")||name=="Wood";
                for(int y=0;y<n;y++)for(int x=0;x<n;x++)
                {float u=x/(float)n,v=y/(float)n;float f=.78f;float n1=Mathf.PerlinNoise(u*8+17,v*8+31);float n2=Mathf.PerlinNoise(u*27+11,v*3+8);f+=(n1-.5f)*.10f;if(timber)f+=(n2-.5f)*.16f+Mathf.Sin(u*180+Mathf.Sin(v*12)*2)*.013f;else f+=(n2-.5f)*.035f;px[y*n+x]=new Color(f*1.015f,f,f*.98f);}
                tx.SetPixels(px);tx.Apply();var path=S013ProductionAuthoring.Root+"/Textures/"+name+".png";File.WriteAllBytes(path,tx.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tx);AssetDatabase.ImportAsset(path);
            }
            EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);AssetDatabase.SaveAssets();
        }
    }
}
