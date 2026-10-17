using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace LastSignal.Editor.FirstTownVisualSlice
{
    /// <summary>Measured UV architecture and road geometry, persisted as owned meshes.</summary>
    internal sealed class SliceMesh
    {
        readonly List<Vector3> vertices=new List<Vector3>(); readonly List<Vector2> uv=new List<Vector2>(); readonly List<int> triangles=new List<int>();
        internal void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float tile=2)
        { Quad(a,b,c,d,Vector2.zero,new Vector2(0,Vector3.Distance(a,b)/tile),new Vector2(Vector3.Distance(b,c)/tile,Vector3.Distance(a,b)/tile),new Vector2(Vector3.Distance(b,c)/tile,0)); }
        internal void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector2 ua,Vector2 ub,Vector2 uc,Vector2 ud)
        {int i=vertices.Count;vertices.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{ua,ub,uc,ud});triangles.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});}
        internal void Triangle(Vector3 a,Vector3 b,Vector3 c)
        {int i=vertices.Count;vertices.AddRange(new[]{a,b,c});uv.AddRange(new[]{new Vector2(a.x,a.y),new Vector2(b.x,b.y),new Vector2(c.x,c.y)});triangles.AddRange(new[]{i,i+1,i+2});}
        internal void Box(Vector3 p,Vector3 size,Quaternion rotation)
        {
            var e=size*.5f;
            Vector3 P(float x,float y,float z)=>p+rotation*Vector3.Scale(e,new Vector3(x,y,z));
            Quad(P(-1,-1,-1),P(-1,1,-1),P(1,1,-1),P(1,-1,-1));
            Quad(P(1,-1,1),P(1,1,1),P(-1,1,1),P(-1,-1,1));
            Quad(P(-1,-1,1),P(-1,1,1),P(-1,1,-1),P(-1,-1,-1));
            Quad(P(1,-1,-1),P(1,1,-1),P(1,1,1),P(1,-1,1));
            Quad(P(-1,1,-1),P(-1,1,1),P(1,1,1),P(1,1,-1));
            Quad(P(-1,-1,1),P(-1,-1,-1),P(1,-1,-1),P(1,-1,1));
        }
        internal void Beam(Vector3 a,Vector3 b,float width,float depth)
        {Box((a+b)*.5f,new Vector3(width,depth,Vector3.Distance(a,b)),Quaternion.LookRotation(b-a));}
        internal GameObject Create(string name,Transform parent,SliceAssets art,Material material,bool collision=false)
        {
            if(vertices.Count==0)return null;
            var mesh=new Mesh{indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);
            art.Save(mesh,name);var g=new GameObject(name);g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=material;
            if(collision)g.AddComponent<MeshCollider>().sharedMesh=mesh;
            GameObjectUtilityFlags(g);return g;
        }
        internal GameObject CreateOwned(string name,Transform parent,string assetPath,Material material)
        {
            if(vertices.Count==0)return null;
            var mesh=new Mesh{indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);
            mesh.name=name;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            UnityEditor.AssetDatabase.CreateAsset(mesh,assetPath);
            var g=new GameObject(name);g.transform.SetParent(parent,false);
            g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=material;
            GameObjectUtilityFlags(g);return g;
        }
        static void GameObjectUtilityFlags(GameObject g)=>UnityEditor.GameObjectUtility.SetStaticEditorFlags(g,UnityEditor.StaticEditorFlags.OccludeeStatic);
    }
}
