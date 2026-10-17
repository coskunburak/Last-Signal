using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastSignal.Forest;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace LastSignal.Editor.FirstTownVisualSlice
{
    internal sealed class SliceAssets
    {
        internal const string Town="Assets/LastSignal/Art/FirstTown/Prefabs/", Kit="Assets/LastSignal/Art/S013/Production/";
        internal const string Gas="Assets/ThirdParty/VGtiXN/Countryside gas station/Prefabs/";
        internal readonly string folder;
        internal readonly Dictionary<string,GameObject> prefabs=new Dictionary<string,GameObject>();
        internal readonly Dictionary<string,string> provenance=new Dictionary<string,string>();
        readonly Dictionary<Material,Material> normalized=new Dictionary<Material,Material>();
        readonly Dictionary<string,Material> palette=new Dictionary<string,Material>();
        int meshSerial;
        internal SliceAssets(string f,ForestAuthoringState forest)
        {
            folder=f;
            foreach(string key in new[]{"Pine","Birch","Bush","Grass","Rock","Log"})Register(key,forest.generationFolder+"/"+key+".prefab",true);
            foreach(string key in new[]{"Wreck","Pallet","Fence","Awning"})Register(key,Town+key+".prefab",false);
            Register("Bin",Gas+"Trash_can/Trash_can.prefab",false);
            Register("Tyre",Gas+"wheel.prefab",false);
            Register("Extinguisher",Gas+"Fire_extinguisher.prefab",false);
            Register("ElectricPanel",Gas+"Electric_panel.prefab",false);
        }
        internal static T Load<T>(string p) where T:Object => AssetDatabase.LoadAssetAtPath<T>(p) ?? throw new FileNotFoundException("Required asset missing: "+p);
        internal static Bounds BoundsOf(GameObject g)
        {var rs=g.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)throw new InvalidOperationException("No renderer: "+g.name);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
        void Register(string key,string path,bool nature)
        {
            var source=Load<GameObject>(path); provenance.Add(key,path);
            // Reuse approved wind/LOD prefabs without duplicating their mesh/material assets.
            if(nature || path.StartsWith(Town,StringComparison.Ordinal)){prefabs.Add(key,source);return;}
            var g=Object.Instantiate(source);
            try
            {
                g.name=key;
                foreach(var t in g.GetComponentsInChildren<Transform>(true))GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                foreach(var c in g.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(c);
                foreach(var c in g.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(c);
                foreach(var c in g.GetComponentsInChildren<AudioSource>(true))Object.DestroyImmediate(c);
                foreach(var c in g.GetComponentsInChildren<Light>(true))Object.DestroyImmediate(c);
                foreach(var r in g.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(Normalize).ToArray();
                prefabs.Add(key,PrefabUtility.SaveAsPrefabAsset(g,folder+"/"+key+".prefab"));
            }
            finally{Object.DestroyImmediate(g);}
        }
        Material Normalize(Material m)
        {
            if(!m)throw new InvalidOperationException("Missing material in selected source.");
            if(m.shader && (m.shader.name.StartsWith("Universal Render Pipeline/") || m.shader.name.StartsWith("LastSignal/")))return m;
            if(normalized.TryGetValue(m,out var existing))return existing;
            var result=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=m.name+" Slice",enableInstancing=true};
            foreach(var pair in new[]{new[]{"_MainTex","_BaseMap"},new[]{"_BumpMap","_BumpMap"},new[]{"_MetallicGlossMap","_MetallicGlossMap"},new[]{"_OcclusionMap","_OcclusionMap"}})
                if(m.HasProperty(pair[0]) && m.GetTexture(pair[0])){result.SetTexture(pair[1],m.GetTexture(pair[0]));result.SetTextureScale(pair[1],m.GetTextureScale(pair[0]));result.SetTextureOffset(pair[1],m.GetTextureOffset(pair[0]));}
            result.SetColor("_BaseColor",m.HasProperty("_Color")?m.GetColor("_Color"):Color.white);
            result.SetFloat("_Metallic",m.HasProperty("_Metallic")?m.GetFloat("_Metallic"):0);
            result.SetFloat("_Smoothness",m.HasProperty("_Glossiness")?Mathf.Min(.4f,m.GetFloat("_Glossiness")):.18f);
            if(result.GetTexture("_BumpMap"))result.EnableKeyword("_NORMALMAP");
            if(result.GetTexture("_MetallicGlossMap"))result.EnableKeyword("_METALLICSPECGLOSSMAP");
            if(result.GetTexture("_OcclusionMap"))result.EnableKeyword("_OCCLUSIONMAP");
            if(m.IsKeywordEnabled("_ALPHATEST_ON")){result.SetFloat("_AlphaClip",1);result.SetFloat("_Cutoff",m.HasProperty("_Cutoff")?m.GetFloat("_Cutoff"):.5f);result.EnableKeyword("_ALPHATEST_ON");result.renderQueue=2450;}
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m,out string guid,out long id);
            AssetDatabase.CreateAsset(result,folder+"/Source_"+guid+"_"+id+".mat");normalized.Add(m,result);return result;
        }
        internal Material Mat(string name)
        {
            if(palette.TryGetValue(name,out var result))return result;
            result=Load<Material>(Kit+"Materials/"+name+".mat");palette.Add(name,result);return result;
        }
        internal Material Asphalt(Material source)
        {
            if(palette.TryGetValue("Asphalt",out var result))return result;
            result=new Material(source){name="Slice worn asphalt",enableInstancing=true};
            // The gas-station Road atlas has disconnected UV islands and cannot tile
            // across a continuous road. Use the audited aggregate-asphalt set instead.
            result.SetTexture("_BaseMap",Load<Texture2D>("Assets/ThirdParty/asphalt_02_4k.blend/textures/asphalt_02_diff_4k.jpg"));
            result.SetTexture("_MainTex",result.GetTexture("_BaseMap"));
            result.SetTexture("_BumpMap",Load<Texture2D>(SliceAuthoring.Root+"/SurfaceRefinement04/AsphaltNormal.exr"));
            foreach(string property in new[]{"_BaseMap","_MainTex","_BumpMap"})
            {result.SetTextureScale(property,Vector2.one*1.5f);result.SetTextureOffset(property,Vector2.zero);}
            result.SetTexture("_MetallicGlossMap",null);result.DisableKeyword("_METALLICSPECGLOSSMAP");
            result.SetFloat("_Metallic",0);result.SetFloat("_BumpScale",.65f);result.EnableKeyword("_NORMALMAP");
            result.SetFloat("_Smoothness",.18f);
            var c=result.GetColor("_BaseColor");result.SetColor("_BaseColor",new Color(Mathf.Max(c.r,.58f),Mathf.Max(c.g,.59f),Mathf.Max(c.b,.58f),c.a));
            AssetDatabase.CreateAsset(result,folder+"/Asphalt.mat");palette.Add("Asphalt",result);return result;
        }
        internal Mesh Save(Mesh m,string name)
        {m.name=name;m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();AssetDatabase.CreateAsset(m,folder+"/Mesh_"+(meshSerial++).ToString("D3")+".asset");return m;}
        internal GameObject Place(string key,Transform parent,Vector3 p,float size,float yaw,bool collision=false,bool heightBased=false)
        {
            var g=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[key],parent);g.name=key+" / authored";g.transform.localPosition=Vector3.zero;g.transform.localRotation=Quaternion.identity;
            var b=BoundsOf(g);float measure=heightBased?b.size.y:Mathf.Max(b.size.x,b.size.z);
            if(measure<.01f)throw new InvalidOperationException("Invalid source bounds: "+key);
            g.transform.localScale*=size/measure;g.transform.rotation=Quaternion.Euler(0,yaw,0);
            b=BoundsOf(g);g.transform.position+=p-new Vector3(b.center.x,b.min.y,b.center.z);
            foreach(var c in g.GetComponentsInChildren<Collider>(true))c.enabled=collision;
            foreach(var r in g.GetComponentsInChildren<Renderer>(true))GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.OccludeeStatic);
            foreach(var t in g.GetComponentsInChildren<Transform>(true))PrefabUtility.RecordPrefabInstancePropertyModifications(t);
            foreach(var c in g.GetComponentsInChildren<Collider>(true))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            return g;
        }
    }
}
