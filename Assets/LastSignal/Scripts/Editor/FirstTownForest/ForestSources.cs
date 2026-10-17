using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace LastSignal.Editor.FirstTownForest
{
    internal sealed class ForestSources
    {
        internal const string Kit="Assets/LastSignal/Art/S013/Production/";
        internal const string Vendor="Assets/ThirdParty/Environments/FantasyEnvironments/Environments/";
        readonly string folder;
        readonly Dictionary<Material,Material> materials=new Dictionary<Material,Material>();
        internal readonly Dictionary<string,GameObject> prefabs=new Dictionary<string,GameObject>();
        internal ForestSources(string output){folder=output;}
        internal static T Load<T>(string path) where T:Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing source: "+path);
        internal void Build()
        {
            foreach(var key in new[]{"Pine","Birch","Bush","Rock","Grass"})Make(key);
            Log();
        }
        void Make(string key)
        {
            var g=Object.Instantiate(Load<GameObject>(Kit+"Prefabs/"+key+".prefab"));
            try
            {
                g.name=key;
                foreach(var behaviour in g.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(behaviour);
                foreach(var rb in g.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(rb);
                int meshIndex=0;
                foreach(var filter in g.GetComponentsInChildren<MeshFilter>(true))
                {
                    // Source topology unchanged. Owned mesh bounds include the small vertex-wind excursion.
                    var mesh=Object.Instantiate(filter.sharedMesh);mesh.name=key+" bounded mesh";var b=mesh.bounds;b.Expand(.4f);mesh.bounds=b;
                    AssetDatabase.CreateAsset(mesh,folder+"/"+key+"-"+(meshIndex++)+".asset");filter.sharedMesh=mesh;
                }
                foreach(var r in g.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var slots=r.sharedMaterials;
                    for(int i=0;i<slots.Length;i++)
                    {
                        if(!materials.TryGetValue(slots[i],out var m))
                        {
                            m=new Material(slots[i]){name=slots[i].name+" Forest",enableInstancing=true};
                            m.shader=Shader.Find("LastSignal/Forest/Lit Wind") ?? throw new InvalidOperationException("Forest shader unavailable.");
                            if(key=="Rock")m.EnableKeyword("_LS_FOREST_STATIC");
                            AssetDatabase.CreateAsset(m,folder+"/"+slots[i].name+".mat");materials.Add(slots[i],m);
                        }
                        slots[i]=m;
                    }
                    r.sharedMaterials=slots;r.motionVectorGenerationMode=MotionVectorGenerationMode.ForceNoMotion;
                    r.shadowCastingMode=key=="Grass" || key=="Bush"?ShadowCastingMode.Off:ShadowCastingMode.On;
                    GameObjectUtility.SetStaticEditorFlags(r.gameObject,0); // Vertex wind needs original object space.
                }
                foreach(var c in g.GetComponentsInChildren<Collider>(true))if(c is MeshCollider || key=="Bush" || key=="Grass")Object.DestroyImmediate(c);
                var renderers=g.GetComponentsInChildren<Renderer>(true);
                foreach(var old in g.GetComponentsInChildren<LODGroup>(true))Object.DestroyImmediate(old);
                var lod=g.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(key=="Bush"?.035f:key=="Grass"?.06f:.035f,renderers){fadeTransitionWidth=.25f}});
                // URP Lit / Forest Lit contain LOD_FADE_CROSSFADE in forward, depth and shadow passes.
                lod.fadeMode=LODFadeMode.CrossFade;lod.animateCrossFading=true;lod.RecalculateBounds();
                prefabs[key]=PrefabUtility.SaveAsPrefabAsset(g,folder+"/"+key+".prefab");
            }
            finally{Object.DestroyImmediate(g);}
        }
        void Log()
        {
            // Reuse only the existing bark submesh; no fabricated white cylinder or new vendor geometry.
            var pine=prefabs["Pine"];var filter=pine.GetComponentInChildren<MeshFilter>();var source=filter.sharedMesh;
            var indices=source.GetIndices(0);var remap=new Dictionary<int,int>();var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new int[indices.Length];
            var sv=source.vertices;var sn=source.normals;var su=source.uv;
            for(int i=0;i<indices.Length;i++)
            {
                int old=indices[i];if(!remap.TryGetValue(old,out int mapped)){mapped=vertices.Count;remap.Add(old,mapped);vertices.Add(sv[old]);normals.Add(sn[old]);uv.Add(su[old]);}triangles[i]=mapped;
            }
            var mesh=new Mesh{name="Pine bark debris"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateTangents();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,folder+"/LogMesh.asset");
            var material=new Material(pine.GetComponentInChildren<MeshRenderer>().sharedMaterials[0]){name="Debris bark"};material.EnableKeyword("_LS_FOREST_STATIC");AssetDatabase.CreateAsset(material,folder+"/Log.mat");
            var go=new GameObject("Log");
            try
            {
                var visual=new GameObject("Existing bark geometry");visual.transform.SetParent(go.transform,false);visual.transform.localRotation=Quaternion.Euler(0,0,88);
                visual.AddComponent<MeshFilter>().sharedMesh=mesh;var r=visual.AddComponent<MeshRenderer>();r.sharedMaterial=material;
                // Small decorative fallen limbs remain collision-free and are excluded from travel corridors.
                var lod=go.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.04f,new Renderer[]{r}){fadeTransitionWidth=.25f}});lod.fadeMode=LODFadeMode.CrossFade;lod.animateCrossFading=true;lod.RecalculateBounds();
                prefabs["Log"]=PrefabUtility.SaveAsPrefabAsset(go,folder+"/Log.prefab");
            }
            finally{Object.DestroyImmediate(go);}
        }
        internal TerrainLayer[] Layers()
        {
            var result=new List<TerrainLayer>();
            foreach(string name in new[]{"grass","soil","road","rock"})
            {
                var layer=Object.Instantiate(Load<TerrainLayer>(Vendor+"TerrainLayers/layer_ground_"+name+".terrainlayer"));
                layer.name="Forest "+name;layer.tileSize=new Vector2(name=="rock"?6:4,name=="rock"?6:4);layer.smoothness=name=="rock"?.18f:.08f;
                AssetDatabase.CreateAsset(layer,folder+"/"+name+".terrainlayer");result.Add(layer);
            }
            return result.ToArray();
        }
        internal GameObject Place(string kind,Transform parent,Vector3 position,float scale,float angle)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[kind],parent);go.transform.position=position;go.transform.localScale=Vector3.one*scale;go.transform.rotation=Quaternion.Euler(0,angle,0);
            var rs=go.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
            // Bounds expansion is .2m local per side; compensate so actual source geometry sits slightly in soil.
            go.transform.position+=Vector3.up*(position.y-bounds.min.y-.2f*scale-.035f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);return go;
        }
    }
}
